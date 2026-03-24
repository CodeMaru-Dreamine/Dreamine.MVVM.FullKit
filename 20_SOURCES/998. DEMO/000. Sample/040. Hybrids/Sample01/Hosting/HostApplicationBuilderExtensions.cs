using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sample01.Hosting
{
    /// <summary>
    /// GenericHost와 WPF Application을 같은 프로세스에서 구동시키는 확장 메서드를 제공합니다.
    /// </summary>
    public static class HostApplicationBuilderExtensions
    {
        /// <summary>
        /// STA 스레드에서 WPF Application을 구동하고, GenericHost(HostedService 포함)를 함께 시작합니다.
        /// </summary>
        /// <param name="builder">호스트 빌더</param>
        /// <returns>WPF 실행 제어 객체</returns>
        /// <exception cref="ArgumentNullException">builder가 null인 경우</exception>
        /// <exception cref="InvalidOperationException">Application 생성에 실패한 경우</exception>
        public static IWpfAppRunner BuildApp(this HostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            IHost host = BuildAndStartHost(builder);
            ApplicationCreationContext context = CreateApplicationOnStaThread(host);

            if (context.FailedException is not null || context.Application is null)
            {
                StopAndDisposeHostImmediately(host);
                throw new InvalidOperationException("Application 생성 실패", context.FailedException);
            }

            return new WpfRunnerProxy(
                context.Application,
                context.StaThread,
                context.StartSignal,
                () => context.ExitCode,
                host);
        }

        /// <summary>
        /// Host를 빌드하고 즉시 시작합니다.
        /// </summary>
        /// <param name="builder">호스트 빌더</param>
        /// <returns>시작된 Host 인스턴스</returns>
        private static IHost BuildAndStartHost(HostApplicationBuilder builder)
        {
            IHost host = builder.Build();
            host.StartAsync().GetAwaiter().GetResult();
            return host;
        }

        /// <summary>
        /// 별도 STA 스레드에서 WPF Application 생성을 수행하고 생성 완료를 대기합니다.
        /// </summary>
        /// <param name="host">시작된 Host</param>
        /// <returns>Application 생성 결과 컨텍스트</returns>
        private static ApplicationCreationContext CreateApplicationOnStaThread(IHost host)
        {
            ApplicationCreationContext context = new();

            Thread staThread = new(() => RunWpfStaThread(host, context))
            {
                IsBackground = false,
                Name = "WPF_STA_Main"
            };

            staThread.SetApartmentState(ApartmentState.STA);
            context.StaThread = staThread;

            staThread.Start();
            context.CreatedSignal.Wait();

            return context;
        }

        /// <summary>
        /// WPF STA 메인 스레드 진입점입니다.
        /// </summary>
        /// <param name="host">시작된 Host</param>
        /// <param name="context">생성 컨텍스트</param>
        private static void RunWpfStaThread(IHost host, ApplicationCreationContext context)
        {
            try
            {
                Application app = GetOrCreateApplication();
                AssignServiceProviderIfSupported(app, host);
                HookApplicationExit(app, host);

                context.Application = app;
                context.CreatedSignal.Set();

                context.StartSignal.Wait();
                context.ExitCode = app.Run();
            }
            catch (Exception ex)
            {
                context.FailedException = ex;
                context.CreatedSignal.Set();
            }
        }

        /// <summary>
        /// 현재 Application을 재사용하거나, 없으면 새로 생성합니다.
        /// </summary>
        /// <returns>WPF Application 인스턴스</returns>
        private static Application GetOrCreateApplication()
        {
            Application? app = Application.Current;
            if (app is not null)
            {
                return app;
            }

            Type? applicationType = FindApplicationType();

            app = applicationType is not null
                ? (Application)Activator.CreateInstance(applicationType)!
                : new Application();

            TryInvokeInitializeComponent(app);
            return app;
        }

        /// <summary>
        /// 엔트리 어셈블리 기준으로 구체 Application 형식을 탐색합니다.
        /// </summary>
        /// <returns>Application 파생 형식, 없으면 null</returns>
        private static Type? FindApplicationType()
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            return assembly
                .GetTypes()
                .FirstOrDefault(type =>
                    typeof(Application).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type != typeof(Application));
        }

        /// <summary>
        /// Application에 InitializeComponent 메서드가 존재하면 호출합니다.
        /// </summary>
        /// <param name="app">대상 Application</param>
        private static void TryInvokeInitializeComponent(Application app)
        {
            const string InitializeComponentMethodName = "InitializeComponent";

            MethodInfo? initializeComponentMethod = app.GetType().GetMethod(
                InitializeComponentMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (initializeComponentMethod is null)
            {
                return;
            }

            // WPF 생성 코드에서 InitializeComponent가 internal/private일 수 있으므로
            // 현재 프로세스에서 생성한 Application 인스턴스에 한해 제한적으로 호출한다.
            initializeComponentMethod.Invoke(app, null);
        }

        /// <summary>
        /// 샘플 Application 형식이 지원하는 경우 ServiceProvider를 주입합니다.
        /// </summary>
        /// <param name="app">생성된 Application</param>
        /// <param name="host">시작된 Host</param>
        private static void AssignServiceProviderIfSupported(Application app, IHost host)
        {
            if (app is Sample01.App)
            {
                Sample01.App.ServiceProvider = host.Services;
            }
        }

        /// <summary>
        /// Application 종료 시 Host 정지 및 해제를 연결합니다.
        /// </summary>
        /// <param name="app">생성된 Application</param>
        /// <param name="host">시작된 Host</param>
        private static void HookApplicationExit(Application app, IHost host)
        {
            int hostStopped = 0;

            app.Exit += async (_, _) =>
            {
                if (Interlocked.Exchange(ref hostStopped, 1) == 1)
                {
                    return;
                }

                await StopAndDisposeHostAsync(host, TimeSpan.FromSeconds(5));
            };
        }

        /// <summary>
        /// Host를 안전하게 정지하고 해제합니다.
        /// </summary>
        /// <param name="host">대상 Host</param>
        /// <param name="timeout">정지 타임아웃</param>
        /// <returns>비동기 작업</returns>
        private static async Task StopAndDisposeHostAsync(IHost host, TimeSpan timeout)
        {
            try
            {
                await host.StopAsync(timeout);
            }
            catch
            {
                // 종료 중 예외가 발생해도 자원 해제는 계속 진행한다.
            }
            finally
            {
                host.Dispose();
            }
        }

        /// <summary>
        /// Host를 즉시 정지 후 해제합니다.
        /// </summary>
        /// <param name="host">대상 Host</param>
        private static void StopAndDisposeHostImmediately(IHost host)
        {
            try
            {
                host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            catch
            {
                // 생성 실패 정리 단계에서는 종료 예외를 무시하고 해제를 우선한다.
            }
            finally
            {
                host.Dispose();
            }
        }

        /// <summary>
        /// WPF Application 생성 결과를 전달하는 컨텍스트입니다.
        /// </summary>
        private sealed class ApplicationCreationContext
        {
            /// <summary>
            /// 생성 완료 신호입니다.
            /// </summary>
            public ManualResetEventSlim CreatedSignal { get; } = new(false);

            /// <summary>
            /// Run 시작 신호입니다.
            /// </summary>
            public ManualResetEventSlim StartSignal { get; } = new(false);

            /// <summary>
            /// 생성된 Application 인스턴스입니다.
            /// </summary>
            public Application? Application { get; set; }

            /// <summary>
            /// STA 스레드입니다.
            /// </summary>
            public Thread StaThread { get; set; } = null!;

            /// <summary>
            /// Application 생성 중 발생한 예외입니다.
            /// </summary>
            public Exception? FailedException { get; set; }

            /// <summary>
            /// Application 종료 코드입니다.
            /// </summary>
            public int ExitCode { get; set; }
        }

        /// <summary>
        /// WPF 실행 프록시입니다.
        /// </summary>
        private sealed class WpfRunnerProxy : IWpfAppRunner
        {
            private readonly Application _app;
            private readonly Thread _staThread;
            private readonly ManualResetEventSlim _startSignal;
            private readonly Func<int> _getExitCode;
            private readonly IHost _host;
            private int _hasRun;
            private int _stopped;

            /// <summary>
            /// WpfRunnerProxy 인스턴스를 초기화합니다.
            /// </summary>
            /// <param name="app">WPF Application</param>
            /// <param name="staThread">STA 스레드</param>
            /// <param name="startSignal">시작 신호</param>
            /// <param name="getExitCode">종료 코드 조회 함수</param>
            /// <param name="host">Host 인스턴스</param>
            public WpfRunnerProxy(
                Application app,
                Thread staThread,
                ManualResetEventSlim startSignal,
                Func<int> getExitCode,
                IHost host)
            {
                _app = app;
                _staThread = staThread;
                _startSignal = startSignal;
                _getExitCode = getExitCode;
                _host = host;
            }

            /// <summary>
            /// WPF 메시지 루프를 시작합니다.
            /// </summary>
            /// <exception cref="InvalidOperationException">이미 실행된 경우</exception>
            public void Run()
            {
                if (Interlocked.Exchange(ref _hasRun, 1) == 1)
                {
                    throw new InvalidOperationException("Application.Run()은 한 번만 호출할 수 있습니다.");
                }

                _startSignal.Set();
                _staThread.Join();
                Environment.ExitCode = _getExitCode();
            }

            /// <summary>
            /// WPF Application과 Host를 종료합니다.
            /// </summary>
            /// <param name="exitCode">종료 코드</param>
            public void Shutdown(int exitCode = 0)
            {
                if (Interlocked.Exchange(ref _stopped, 1) == 1)
                {
                    return;
                }

                _app.Dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        _app.Shutdown(exitCode);
                    }
                    catch
                    {
                        // 이미 종료 중이거나 Dispatcher 상태에 따라 예외가 발생할 수 있으나 종료 흐름은 유지한다.
                    }

                    await StopAndDisposeHostAsync(_host, TimeSpan.FromSeconds(5));
                }));
            }
        }
    }
}