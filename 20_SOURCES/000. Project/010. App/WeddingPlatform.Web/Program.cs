using System.IO;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeddingPlatform.Blazor;
using Microsoft.Extensions.Logging;
using WeddingPlatform.Services;

namespace WeddingPlatform;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        int serverPort = GetInt(builder.Configuration, "WeddingServer:Port", 5050);
        bool listenAnyIp = GetBool(builder.Configuration, "WeddingServer:ListenAnyIp", true);

        builder.Services.AddDreamineHybridWpf();

        // ── Wedding 서비스 ─────────────────────────────────────────
        var weddingOpts = WeddingOptions.From(builder.Configuration);
        builder.Services.AddSingleton(weddingOpts);
        builder.Services.AddSingleton<ITenantStore, JsonTenantStore>();
        builder.Services.AddSingleton<IGuestbookStorage, CsvGuestbookStorage>();
        builder.Services.AddSingleton<IPhotoService, LocalPhotoService>();

        builder.Services.AddSingleton<Views.MainWindow>();
        builder.Services.AddHostedService<GhostAccountCleanupService>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            // Wedding 서비스 Blazor 호스트에 공유 (ViewModel은 AutoRegisterViewModels로 자동 등록)
            options.SharedServiceTypes.Add(typeof(WeddingOptions));
            options.SharedServiceTypes.Add(typeof(ITenantStore));
            options.SharedServiceTypes.Add(typeof(IGuestbookStorage));
            options.SharedServiceTypes.Add(typeof(IPhotoService));
            // 업로드된 사진을 /wedding-data/ URL로 제공
            options.AddPhysicalStaticFiles(weddingOpts.ResolvedDataPath, "/wedding-data");
        });

        // OG 플랫폼 이미지 자동 생성 (없을 때만)
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        try { OgImageGenerator.EnsureGenerated(wwwroot); } catch { /* 이미지 생성 실패해도 앱 계속 실행 */ }

        builder.Build().RunDreamineWpfApp<App>();
    }

    private static int GetInt(IConfiguration cfg, string key, int fallback) =>
        int.TryParse(cfg[key], out int v) ? v : fallback;

    private static bool GetBool(IConfiguration cfg, string key, bool fallback) =>
        bool.TryParse(cfg[key], out bool v) ? v : fallback;
}
