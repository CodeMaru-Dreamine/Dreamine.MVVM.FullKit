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
/// \if KO
/// <para>\brief DreamineVMS 하이브리드 애플리케이션의 진입점입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates program functionality and related state.</para>
/// \endif
/// </summary>
public static class Program
{
    /// <summary>
    /// \if KO
    /// <para>\brief WPF + Blazor Server 기반 DreamineVMS 애플리케이션을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the main operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 설정 옵션 바인딩을 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register options operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterOptions(HostApplicationBuilder builder)
    {
        builder.Services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        builder.Services.Configure<VmsServerOptions>(
            builder.Configuration.GetSection("VmsServer"));

        builder.Services.Configure<FfmpegOptions>(
            builder.Configuration.GetSection("Ffmpeg"));
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Dreamine Hybrid WPF 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register dreamine hybrid operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterDreamineHybrid(HostApplicationBuilder builder)
    {
        builder.Services.AddDreamineHybridWpf();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief WPF Shell 및 ViewModel 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register auth services operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterAuthServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<VmsDatabase>();

        // 에이전트: 중앙 서버 통신 + HLS 세그먼트 업로더
        builder.Services.AddSingleton<AgentApiClient>();
        builder.Services.AddHostedService<HlsSegmentPusherService>();
    }

    /// <summary>
    /// \if KO
    /// <para>Register Application Services 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register application services operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>builder에 사용할 <c>HostApplicationBuilder</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterApplicationServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AgentSettingsWriter>();
        builder.Services.AddSingleton<AgentSettingsViewModel>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<MainWindowViewModel>();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Blazor ViewModel 및 Blazor Adapter 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register blazor view models operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterBlazorViewModels(HostApplicationBuilder builder)
    {
        builder.Services.AddScoped<LivePageViewModel>();
        builder.Services.AddScoped<DashboardPageViewModel>();
        builder.Services.AddScoped<VmsLocalDashboardViewModel>();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 카메라 Repository, Runtime State, Stream 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register camera services operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterCameraServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVmsCameraRepository, SqliteCameraRepository>();
        builder.Services.AddSingleton<ICameraRuntimeStateService, CameraRuntimeStateService>();

        builder.Services.AddSingleton<FfmpegHlsStreamService>();

        builder.Services.AddSingleton<ICameraStreamService>(
            serviceProvider => serviceProvider.GetRequiredService<FfmpegHlsStreamService>());
    }

    /// <summary>
    /// \if KO
    /// <para>\brief WPF와 Blazor가 공유하는 Hybrid State Store를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register hybrid state operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterHybridState(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IHybridStateStore<VmsDashboardState>>(
            new HybridStateStore<VmsDashboardState>(VmsDashboardState.Empty));
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Dashboard 상태 도메인 서비스를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register dashboard services operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterDashboardServices(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVmsDashboardStateService, VmsDashboardStateService>();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 백그라운드 Hosted Service를 등록합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the register hosted services operation.</para>
    /// \endif
    /// </summary>
    /// <param name="builder">
    /// \if KO
    /// <para>애플리케이션 Host Builder입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HostApplicationBuilder</c> value used for builder.</para>
    /// \endif
    /// </param>
    private static void RegisterHostedServices(HostApplicationBuilder builder)
    {
        builder.Services.AddHostedService(
            serviceProvider => serviceProvider.GetRequiredService<FfmpegHlsStreamService>());

        builder.Services.AddHostedService<VmsBlazorServerHostedService<AppShell>>();
    }
}
