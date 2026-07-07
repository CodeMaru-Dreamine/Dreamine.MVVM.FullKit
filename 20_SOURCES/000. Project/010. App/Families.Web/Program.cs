using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FamiliesApp.Blazor;
using FamiliesApp.Services;

namespace FamiliesApp;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

        int serverPort = GetInt(builder.Configuration, "FamilyServer:Port", 5080);
        bool listenAnyIp = GetBool(builder.Configuration, "FamilyServer:ListenAnyIp", true);
        AuthOptions authOptions =
            builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        string usersDbPath = ResolvePath(
            builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
            Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddDreamineIdentityWpfHost();

        var familyOpts = FamilyOptions.From(builder.Configuration);
        builder.Services.AddSingleton(familyOpts);
        builder.Services.AddSingleton<IFamilyTenantStore, JsonFamilyTenantStore>();
        builder.Services.AddSingleton<IPostStore, JsonPostStore>();
        builder.Services.AddSingleton<IAlbumStore, JsonAlbumStore>();
        builder.Services.AddSingleton<IGlobalSettingsStore, JsonGlobalSettingsStore>();
        builder.Services.AddSingleton<IMediaService, LocalMediaService>();
        builder.Services.AddSingleton<IReactionStore, JsonReactionStore>();

        builder.Services.AddSingleton<Views.MainWindow>();
        builder.Services.AddHostedService<GhostAccountCleanupService>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            options.SharedServiceTypes.Add(typeof(FamilyOptions));
            options.SharedServiceTypes.Add(typeof(IFamilyTenantStore));
            options.SharedServiceTypes.Add(typeof(IPostStore));
            options.SharedServiceTypes.Add(typeof(IAlbumStore));
            options.SharedServiceTypes.Add(typeof(IGlobalSettingsStore));
            options.SharedServiceTypes.Add(typeof(IMediaService));
            options.SharedServiceTypes.Add(typeof(IReactionStore));
            options.AddPhysicalStaticFiles(familyOpts.ResolvedDataPath, "/family-data");

            // InputFile(사진·동영상 업로드)은 SignalR 회선을 통해 청크 단위로 전송되는데,
            // 기본 SignalR 메시지 크기 제한(32KB)과 짧은 타임아웃 때문에 큰 파일은 매우 느리거나
            // 응답 없이 멈춘 것처럼 보입니다. 업로드 용량 검증은 LocalMediaService에서 별도로
            // 하고 있으므로 전송 한도 자체는 풀어줍니다.
            options.ConfigureServices = services =>
            {
                services.AddServerSideBlazor().AddHubOptions(o =>
                {
                    o.MaximumReceiveMessageSize = null;
                    o.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
                    o.HandshakeTimeout = TimeSpan.FromMinutes(2);
                    o.KeepAliveInterval = TimeSpan.FromSeconds(10);
                });

                services.AddAntiforgery(o =>
                {
                    o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                    o.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                });

                services.Configure<CircuitOptions>(o =>
                {
                    o.DisconnectedCircuitMaxRetained = 100;
                    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                    o.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
                    o.MaxBufferedUnacknowledgedRenderBatches = 10;
                });

                services.AddScoped<FamilyUserContext>();
            };

            options.AddDreamineIdentity(authOptions, usersDbPath);
        });

        // OG 플랫폼 이미지 자동 생성 (없을 때만)
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        try { OgImageGenerator.EnsureGenerated(wwwroot); } catch { /* 실패해도 앱 계속 실행 */ }

        builder.Build().RunDreamineWpfApp<App>();
    }

    private static int GetInt(IConfiguration cfg, string key, int fallback) =>
        int.TryParse(cfg[key], out int v) ? v : fallback;

    private static bool GetBool(IConfiguration cfg, string key, bool fallback) =>
        bool.TryParse(cfg[key], out bool v) ? v : fallback;

    private static string ResolvePath(string? configuredPath, string fallback)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return fallback;
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }
}
