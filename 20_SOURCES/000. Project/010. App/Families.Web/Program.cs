using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
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
        int serverPort = GetInt(builder.Configuration, "FamilyServer:Port", 5080);
        bool listenAnyIp = GetBool(builder.Configuration, "FamilyServer:ListenAnyIp", true);

        builder.Services.AddDreamineHybridWpf();

        var familyOpts = FamilyOptions.From(builder.Configuration);
        builder.Services.AddSingleton(familyOpts);
        builder.Services.AddSingleton<IFamilyTenantStore, JsonFamilyTenantStore>();
        builder.Services.AddSingleton<IPostStore, JsonPostStore>();
        builder.Services.AddSingleton<IAlbumStore, JsonAlbumStore>();
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
            options.SharedServiceTypes.Add(typeof(IMediaService));
            options.SharedServiceTypes.Add(typeof(IReactionStore));
            options.AddPhysicalStaticFiles(familyOpts.ResolvedDataPath, "/family-data");
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
}
