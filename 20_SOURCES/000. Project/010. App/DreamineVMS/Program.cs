using Dreamine.Hybrid.Interfaces;
using Dreamine.Hybrid.State;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using DreamineVMS.Blazor;
using DreamineVMS.Blazor.ViewModels;
using DreamineVMS.Hosting;
using DreamineVMS.Options;
using DreamineVMS.Services.Agent;
using DreamineVMS.Services.Auth;  // VmsDatabase
using DreamineVMS.Services.Cameras;
using DreamineVMS.Services.Dashboard;
using DreamineVMS.Services.Runtime;
using DreamineVMS.Services.Streaming;
using DreamineVMS.States;
using DreamineVMS.ViewModels;
using DreamineVMS.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DreamineVMS;

/// <summary>
/// \brief DreamineVMS 하이브리드 애플리케이션의 진입점입니다.
/// </summary>
public static class Program
{
    /// <summary>
    /// \brief WPF + Blazor Server 기반 DreamineVMS 애플리케이션을 시작합니다.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

        RegisterOptions(builder);
        RegisterAuthServices(builder);

        // FFmpeg 경로 자동 확인 및 다운로드 (없으면 로컬 ffmpeg\ffmpeg.exe로 대체)
        string configuredFfmpegPath = builder.Configuration["Ffmpeg:Path"] ?? @"C:\ffmpeg\bin\ffmpeg.exe";
        var progress = new Progress<string>(msg => Console.WriteLine($"[FFmpeg] {msg}"));
        string resolvedFfmpegPath = FfmpegBootstrapper.EnsureAsync(configuredFfmpegPath, progress)
            .GetAwaiter().GetResult();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ffmpeg:Path"] = resolvedFfmpegPath
        });
        RegisterDreamineHybrid(builder);
        RegisterApplicationServices(builder);
        RegisterCameraServices(builder);
        RegisterHybridState(builder);
        RegisterDashboardServices(builder);
        RegisterBlazorViewModels(builder);
        RegisterHostedServices(builder);

        using IHost host = builder.Build();

        // 도메인 핵심 서비스를 미리 인스턴스화하고 Dashboard 상태 동기화를 시작합니다.
        _ = host.Services.GetRequiredService<IVmsCameraRepository>();
        _ = host.Services.GetRequiredService<ICameraRuntimeStateService>();
        _ = host.Services.GetRequiredService<FfmpegHlsStreamService>();

        IVmsDashboardStateService dashboardStateService =
            host.Services.GetRequiredService<IVmsDashboardStateService>();
        dashboardStateService.Start();

        host.RunDreamineWpfApp<App>();
    }

    /// <summary>
    /// \brief 설정 옵션 바인딩을 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterOptions(HostApplicationBuilder builder)
    {
        builder.Services.Configure<VmsServerOptions>(
            builder.Configuration.GetSection("VmsServer"));

        builder.Services.Configure<FfmpegOptions>(
            builder.Configuration.GetSection("Ffmpeg"));
    }

    /// <summary>
    /// \brief Dreamine Hybrid WPF 서비스를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterDreamineHybrid(HostApplicationBuilder builder)
    {
        builder.Services.AddDreamineHybridWpf();
    }

    /// <summary>
    /// \brief WPF Shell 및 ViewModel 서비스를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterAuthServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<VmsDatabase>();

        // 에이전트: 중앙 서버 통신 + HLS 세그먼트 업로더
        builder.Services.AddSingleton<AgentApiClient>();
        builder.Services.AddHostedService<HlsSegmentPusherService>();
    }

    private static void RegisterApplicationServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AgentSettingsWriter>();
        builder.Services.AddSingleton<AgentSettingsViewModel>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<MainWindowViewModel>();
    }

    /// <summary>
    /// \brief Blazor ViewModel 및 Blazor Adapter 서비스를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterBlazorViewModels(HostApplicationBuilder builder)
    {
        builder.Services.AddScoped<LivePageViewModel>();
        builder.Services.AddScoped<DashboardPageViewModel>();
        builder.Services.AddScoped<VmsLocalDashboardViewModel>();
    }

    /// <summary>
    /// \brief 카메라 Repository, Runtime State, Stream 서비스를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterCameraServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVmsCameraRepository, SqliteCameraRepository>();
        builder.Services.AddSingleton<ICameraRuntimeStateService, CameraRuntimeStateService>();

        builder.Services.AddSingleton<FfmpegHlsStreamService>();

        builder.Services.AddSingleton<ICameraStreamService>(
            serviceProvider => serviceProvider.GetRequiredService<FfmpegHlsStreamService>());
    }

    /// <summary>
    /// \brief WPF와 Blazor가 공유하는 Hybrid State Store를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterHybridState(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHybridStateStore<VmsDashboardState>>(
            new HybridStateStore<VmsDashboardState>(VmsDashboardState.Empty));
    }

    /// <summary>
    /// \brief Dashboard 상태 도메인 서비스를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterDashboardServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVmsDashboardStateService, VmsDashboardStateService>();
    }

    /// <summary>
    /// \brief 백그라운드 Hosted Service를 등록합니다.
    /// </summary>
    /// <param name="builder">애플리케이션 Host Builder입니다.</param>
    private static void RegisterHostedServices(HostApplicationBuilder builder)
    {
        builder.Services.AddHostedService(
            serviceProvider => serviceProvider.GetRequiredService<FfmpegHlsStreamService>());

        builder.Services.AddHostedService<VmsBlazorServerHostedService<AppShell>>();
    }
}
