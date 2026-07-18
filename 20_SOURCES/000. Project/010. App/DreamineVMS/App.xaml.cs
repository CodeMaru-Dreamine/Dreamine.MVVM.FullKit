using Dreamine.Hybrid.Wpf.Interfaces;
using DreamineVMS.ViewModels;
using DreamineVMS.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace DreamineVMS;

/// <summary>
/// \if KO
/// <para>\brief DreamineVMS WPF Application 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app functionality and related state.</para>
/// \endif
/// </summary>
public partial class App : Application, IDreamineServiceProviderAware
{
    /// <summary>
    /// \if KO
    /// <para>\brief Host에서 주입한 ServiceProvider입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the service provider value.</para>
    /// \endif
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Service Provider 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the service provider value.</para>
    /// \endif
    /// </summary>
    /// <param name="serviceProvider">
    /// \if KO
    /// <para>service Provider에 사용할 <c>IServiceProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IServiceProvider</c> value used for service provider.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// \if KO
    /// <para>Startup 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the startup event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 On Startup 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the on startup operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                args.Exception.ToString(),
                "오류 발생 — DreamineVMS",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                args.ExceptionObject?.ToString() ?? "알 수 없는 오류",
                "치명적 오류 — DreamineVMS",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        };

        if (ServiceProvider is null)
        {
            throw new InvalidOperationException("ServiceProvider initialization failed.");
        }

        MainWindowViewModel viewModel = ServiceProvider.GetRequiredService<MainWindowViewModel>();
        MainWindow window = ServiceProvider.GetRequiredService<MainWindow>();
        window.DataContext = viewModel;

        MainWindow = window;
        window.Show();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 애플리케이션 종료 시 호출됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the exit event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    /// <remarks>
    /// \if KO
    /// <para>이전 구현은 UI 스레드에서 StopAllAsync().GetAwaiter().GetResult()로 동기 대기했으나, StopAsync가 내부적으로 RuntimeState 변경 이벤트를 발생시키고 그 핸들러가 Dispatcher.Invoke로 UI 스레드에 마샬링하려고 시도하면서 데드락이 발생했습니다. 현재 구현은 ffmpeg 프로세스 정리를 두 경로에 위임합니다: 1) Host의 정상 종료 흐름에서 FfmpegHlsStreamService.StopAsync가 호출되고 그 안에서 모든 ffmpeg 프로세스가 graceful 종료됩니다. 2) 비정상 종료 시에도 Job Object(KILL_ON_JOB_CLOSE) 덕분에 부모 프로세스가 사라지면 OS가 자동으로 자식 ffmpeg를 종료합니다. 따라서 OnExit에서 추가 동기 대기는 불필요하며, 오히려 데드락의 원인이 됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Describes behavior and usage considerations for this member.</para>
    /// \endif
    /// </remarks>
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
