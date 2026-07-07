using System;
using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeddingThankYou.Blazor;
using WeddingThankYou.Services;

namespace WeddingThankYou;

/// <summary>
/// \file Program.cs
/// \brief WPF 데스크톱 호스트 안에 Blazor Server(ThankYou/Admin)를 띄우는 진입점.
/// \details WeddingPlatform.Web과 동일한 Dreamine.Hybrid.Wpf 구조를 사용합니다.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

        int serverPort = GetInt(builder.Configuration, "WeddingServer:Port", 4088);
        bool listenAnyIp = GetBool(builder.Configuration, "WeddingServer:ListenAnyIp", true);
        AuthOptions authOptions =
            builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        string usersDbPath = ResolvePath(
            builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
            Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddDreamineIdentityWpfHost();

        // ── Wedding 서비스 ─────────────────────────────────────────
        var weddingOpts = WeddingOptions.From(builder.Configuration);
        builder.Services.AddSingleton(weddingOpts);
        builder.Services.AddSingleton<ITenantStore, JsonTenantStore>();
        builder.Services.AddSingleton<IGuestbookStorage, CsvGuestbookStorage>();
        builder.Services.AddSingleton<IGlobalSettingsStore, JsonGlobalSettingsStore>();
        builder.Services.AddSingleton<IPhotoService, LocalPhotoService>();

        builder.Services.AddSingleton<Views.MainWindow>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            // Wedding 서비스 Blazor 호스트에 공유 (ViewModel은 AutoRegisterViewModels로 자동 등록)
            options.SharedServiceTypes.Add(typeof(WeddingOptions));
            options.SharedServiceTypes.Add(typeof(ITenantStore));
            options.SharedServiceTypes.Add(typeof(IGuestbookStorage));
            options.SharedServiceTypes.Add(typeof(IGlobalSettingsStore));
            options.SharedServiceTypes.Add(typeof(IPhotoService));
            // 업로드된 사진/미디어를 /wedding-data/ URL로 제공
            options.AddPhysicalStaticFiles(weddingOpts.ResolvedDataPath, "/wedding-data");

            // InputFile(동영상 등 대용량 업로드)은 SignalR 회선을 통해 청크 단위로 전송되는데,
            // 기본 SignalR 메시지 크기 제한(32KB)과 짧은 타임아웃 때문에 큰 파일은 매우 느리거나
            // 응답 없이 멈춘 것처럼 보입니다. 업로드 용량 검증은 LocalPhotoService에서 별도로
            // 하고 있으므로 전송 한도 자체는 풀어줍니다.
            options.ConfigureServices = services =>
            {
                services.AddServerSideBlazor()
                    .AddHubOptions(o =>
                    {
                        o.MaximumReceiveMessageSize = null; // 무제한 (용량 검증은 LocalPhotoService에서 수행)
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

                services.AddScoped<ThankYouUserContext>();
            };

            options.AddDreamineIdentity(authOptions, usersDbPath);
        });

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
