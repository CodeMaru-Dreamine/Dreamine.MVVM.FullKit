/// \file HostApplicationBuilderExtensions.cs
/// \credit DotNet-Korea 루크 님 (아이디어 및 흰트 제공)
/// \brief HostApplicationBuilder에서 WPF + GenericHost를 함께 구동.
/// \details Host를 Build 후 반드시 StartAsync를 호출해야 IHostedService.StartAsync가 실행된다.
/// \author Dreamine
/// \date 2025-11-02
/// \version 1.0.2

using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace Sample01.Hosting
{
	/// \class HostApplicationBuilderExtensions
	/// \brief GenericHost와 WPF Application을 같은 프로세스에서 구동시키는 확장 메서드.
	public static class HostApplicationBuilderExtensions
	{
		/// <summary>STA 스레드에서 WPF Application을 구동하고, GenericHost(HostedService 포함)를 함께 시작</summary>
		public static IWpfAppRunner BuildApp(this HostApplicationBuilder builder)
		{
			// 1) Host 빌드 및 시작
			var host = builder.Build();
			host.StartAsync().GetAwaiter().GetResult();

			Application? app = null;
			Exception? failedEx = null;
			var created = new ManualResetEventSlim(false);
			var startSignal = new ManualResetEventSlim(false);
			int exitCode = 0;

			// 2) WPF는 별도 STA 스레드에서 실행
			var sta = new Thread(() =>
			{
				try
				{
					// (중요) Application 중복 생성 방지: 이미 있으면 재사용
					app = Application.Current;
					if (app is null)
					{
						// 엔트리 어셈블리에서 App 형식을 찾아 생성 시도
						var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
						var appType = asm
							.GetTypes()
							.FirstOrDefault(t =>
								typeof(Application).IsAssignableFrom(t) &&
								!t.IsAbstract &&
								t != typeof(Application));

						app = (Application)(appType is not null
							? Activator.CreateInstance(appType)!
							: new Application());

						// InitializeComponent 호출(있을 경우)
						app.GetType().GetMethod("InitializeComponent",
							BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
							?.Invoke(app, null);
					}

					// ServiceProvider 주입(InitializeComponent 및 OnStartup 전에 유효하도록).
					// App 형식이 Sample01.App일 때만 주입. (형식이 다르면 스킵)
					if (app is Sample01.App testApp)
					{
						Sample01.App.ServiceProvider = host.Services;
					}

					// Host 정지/해제 이중 호출 방지용 플래그
					var hostStopped = 0;

					// 종료 시 Host 정지/해제
					app.Exit += async (_, __) =>
					{
						if (Interlocked.Exchange(ref hostStopped, 1) == 1)
							return;

						try { await host.StopAsync(TimeSpan.FromSeconds(5)); }
						catch { /* 필요시 로깅 */ }
						finally { host.Dispose(); }
					};

					created.Set();        // 생성 완료 통지
					startSignal.Wait();   // Run 신호 대기
					exitCode = app.Run(); // 메시지 루프 진입(동기)
				}
				catch (Exception ex)
				{
					failedEx = ex;
					created.Set();
				}
			})
			{
				IsBackground = false,
				Name = "WPF_STA_Main"
			};

			// Start 이전에 STA 지정 (중요)
			sta.SetApartmentState(ApartmentState.STA);
			sta.Start();

			// STA 스레드에서 Application 생성 완료 대기
			created.Wait();

			if (failedEx is not null || app is null)
			{
				try { host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); } catch { /* ignore */ }
				host.Dispose();
				throw new InvalidOperationException("Application 생성 실패", failedEx);
			}

			return new WpfRunnerProxy(app, sta, startSignal, () => exitCode, host);
		}

		/// \class WpfRunnerProxy
		/// \brief WPF 실행 프록시(Host 수명 관리 포함)
		private sealed class WpfRunnerProxy : IWpfAppRunner
		{
			private readonly Application _app;
			private readonly Thread _staThread;
			private readonly ManualResetEventSlim _startSignal;
			private readonly Func<int> _getExitCode;
			private readonly IHost _host;
			private int _hasRun;
			private int _stopped;

			/// \brief 생성자
			public WpfRunnerProxy(Application app, Thread staThread, ManualResetEventSlim startSignal, Func<int> getExitCode, IHost host)
			{
				_app = app;
				_staThread = staThread;
				_startSignal = startSignal;
				_getExitCode = getExitCode;
				_host = host;
			}

			/// \inheritdoc />
			public void Run()
			{
				if (Interlocked.Exchange(ref _hasRun, 1) == 1)
					throw new InvalidOperationException("Application.Run()은 한 번만 호출할 수 있습니다.");

				_startSignal.Set();     // STA 스레드의 app.Run() 시작
				_staThread.Join();      // 종료까지 대기
				Environment.ExitCode = _getExitCode();
			}

			/// \inheritdoc />
			public void Shutdown(int exitCode = 0)
			{
				if (Interlocked.Exchange(ref _stopped, 1) == 1)
					return;

				_app.Dispatcher.BeginInvoke(new Action(async () =>
				{
					try { _app.Shutdown(exitCode); } catch { /* 필요시 로깅 */ }
					try { await _host.StopAsync(TimeSpan.FromSeconds(5)); }
					catch { /* 필요시 로깅 */ }
					finally { _host.Dispose(); }
				}));
			}
		}
	}
}
