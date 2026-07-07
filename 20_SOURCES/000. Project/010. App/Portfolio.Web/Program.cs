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

public static class Program
{
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
