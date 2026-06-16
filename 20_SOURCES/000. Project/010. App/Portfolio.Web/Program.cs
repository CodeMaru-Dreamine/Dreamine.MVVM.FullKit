using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
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
        int serverPort = GetInt(builder.Configuration, "PortfolioServer:Port", 7080);
        bool listenAnyIp = GetBool(builder.Configuration, "PortfolioServer:ListenAnyIp", true);

        builder.Services.AddDreamineHybridWpf();

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
        });

        builder.Build().RunDreamineWpfApp<App>();
    }

    private static int GetInt(IConfiguration cfg, string key, int fallback) =>
        int.TryParse(cfg[key], out int v) ? v : fallback;

    private static bool GetBool(IConfiguration cfg, string key, bool fallback) =>
        bool.TryParse(cfg[key], out bool v) ? v : fallback;
}
