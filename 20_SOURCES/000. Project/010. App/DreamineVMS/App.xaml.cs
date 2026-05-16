using Dreamine.Hybrid.Wpf.Interfaces;
using DreamineVMS.ViewModels;
using DreamineVMS.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace DreamineVMS;

/// <summary>
/// \brief DreamineVMS WPF Application 클래스입니다.
/// </summary>
public partial class App : Application, IDreamineServiceProviderAware
{
    /// <summary>
    /// \brief Host에서 주입한 ServiceProvider입니다.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <inheritdoc />
    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
    /// \brief 애플리케이션 종료 시 호출됩니다.
    /// </summary>
    /// <remarks>
    /// 이전 구현은 UI 스레드에서 StopAllAsync().GetAwaiter().GetResult()로 동기 대기했으나,
    /// StopAsync가 내부적으로 RuntimeState 변경 이벤트를 발생시키고 그 핸들러가
    /// Dispatcher.Invoke로 UI 스레드에 마샬링하려고 시도하면서 데드락이 발생했습니다.
    ///
    /// 현재 구현은 ffmpeg 프로세스 정리를 두 경로에 위임합니다:
    /// 1) Host의 정상 종료 흐름에서 FfmpegHlsStreamService.StopAsync가 호출되고
    ///    그 안에서 모든 ffmpeg 프로세스가 graceful 종료됩니다.
    /// 2) 비정상 종료 시에도 Job Object(KILL_ON_JOB_CLOSE) 덕분에 부모 프로세스가
    ///    사라지면 OS가 자동으로 자식 ffmpeg를 종료합니다.
    /// 따라서 OnExit에서 추가 동기 대기는 불필요하며, 오히려 데드락의 원인이 됩니다.
    /// </remarks>
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
