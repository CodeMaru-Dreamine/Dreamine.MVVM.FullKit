using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Dreamine.Identity;
using Dreamine.Identity.Options;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PortfolioApp.Blazor;
using PortfolioApp.Services;

namespace PortfolioApp;

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
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddUserSecrets("codemaru-oauth-2ba4e1b2");

        int serverPort = GetInt(builder.Configuration, "PortfolioServer:Port", 7080);
        bool listenAnyIp = GetBool(builder.Configuration, "PortfolioServer:ListenAnyIp", true);
        AuthOptions authOptions =
            builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        string usersDbPath = ResolvePath(
            builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"],
            Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db"));

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddDreamineIdentityWpfHost();

        var opts = PortfolioOptions.From(builder.Configuration);
        builder.Services.AddSingleton(opts);
        builder.Services.AddSingleton<IPortfolioTenantStore, JsonPortfolioTenantStore>();
        builder.Services.AddSingleton<IProjectStore, JsonProjectStore>();
        builder.Services.AddSingleton<IResumeStore, JsonResumeStore>();
        builder.Services.AddSingleton<IContactStore, JsonContactStore>();
        builder.Services.AddSingleton<IMediaService, LocalMediaService>();

        builder.Services.AddSingleton<Views.MainWindow>();
        builder.Services.AddHostedService<GhostAccountCleanupService>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            options.SharedServiceTypes.Add(typeof(PortfolioOptions));
            options.SharedServiceTypes.Add(typeof(IPortfolioTenantStore));
            options.SharedServiceTypes.Add(typeof(IProjectStore));
            options.SharedServiceTypes.Add(typeof(IResumeStore));
            options.SharedServiceTypes.Add(typeof(IContactStore));
            options.SharedServiceTypes.Add(typeof(IMediaService));
            options.AddPhysicalStaticFiles(opts.ResolvedDataPath, "/portfolio-data");

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

                services.AddScoped<PortfolioUserContext>();
            };

            options.AddDreamineIdentity(authOptions, usersDbPath);
        });

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
