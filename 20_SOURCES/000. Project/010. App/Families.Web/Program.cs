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

/// <summary>
/// \if KO
/// <para>Program 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates program functionality and related state.</para>
/// \endif
/// </summary>
public static class Program
{
    /// <summary>
    /// \if KO
    /// <para>Main 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the main operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Int 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the int value.</para>
    /// \endif
    /// </summary>
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Int 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the get int operation.</para>
    /// \endif
    /// </returns>
    private static int GetInt(IConfiguration cfg, string key, int fallback) =>
        int.TryParse(cfg[key], out int v) ? v : fallback;

    /// <summary>
    /// \if KO
    /// <para>Bool 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the bool value.</para>
    /// \endif
    /// </summary>
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>IConfiguration</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IConfiguration</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Bool 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the get bool condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool GetBool(IConfiguration cfg, string key, bool fallback) =>
        bool.TryParse(cfg[key], out bool v) ? v : fallback;

    /// <summary>
    /// \if KO
    /// <para>Resolve Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve path operation.</para>
    /// \endif
    /// </summary>
    /// <param name="configuredPath">
    /// \if KO
    /// <para>configured Path에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for configured path.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the resolve path operation.</para>
    /// \endif
    /// </returns>
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
