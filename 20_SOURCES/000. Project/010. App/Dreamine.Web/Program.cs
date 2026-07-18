using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using DreamineWeb.Blazor;
using DreamineWeb.Models;
using DreamineWeb.Services;
using DreamineWeb.ViewModels;
using DreamineWeb.Views;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DreamineWeb;

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
        int port = builder.Configuration.GetValue("DreamineServer:Port", 4090);
        bool listenAny = builder.Configuration.GetValue("DreamineServer:ListenAnyIp", true);
        string? doxygenRoot = DocumentationPathResolver.ResolveDoxygenRoot(builder.Configuration);

        var opts = builder.Configuration.GetSection("Dreamine").Get<DreamineOptions>() ?? new();
        builder.Services.AddSingleton(opts);

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddSingleton<ILibraryStore, JsonLibraryStore>();
        builder.Services.AddSingleton<IPlaygroundStore, JsonPlaygroundStore>();
        builder.Services.AddSingleton<SiteSettingsService>();
        builder.Services.AddSingleton<AdminAuthService>();
        builder.Services.AddSingleton<MainWindow>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = port;
            options.ListenAnyIp = listenAny;
            options.SharedServiceTypes.Add(typeof(ILibraryStore));
            options.SharedServiceTypes.Add(typeof(IPlaygroundStore));
            options.SharedServiceTypes.Add(typeof(DreamineOptions));
            options.SharedServiceTypes.Add(typeof(SiteSettingsService));
            options.SharedServiceTypes.Add(typeof(AdminAuthService));
            if (doxygenRoot is not null)
                options.AddPhysicalStaticFiles(doxygenRoot, "/docs/doxygen");
            options.ConfigureServices = services =>
            {
                services.AddScoped<XmlDocAutoLinker>();
                services.AddScoped<LibraryCatalogSyncService>();
                services.AddScoped<VersionSyncService>();
                services.AddScoped<UserPreferencesService>();
                services.AddSingleton<DocumentationCatalogService>();
                services.AddScoped<PlaygroundMediaService>();
                services.AddScoped<Dreamine.UI.Blazor.DreamineDialogService>();
                services.AddScoped<HomeViewModel>();
                services.AddScoped<DocViewModel>();
                services.AddScoped<AdminViewModel>();
                services.AddAntiforgery(o => o.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax);
                services.Configure<CircuitOptions>(o =>
                {
                    o.DetailedErrors = true;
                    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                });
                services.AddSignalR(o => o.MaximumReceiveMessageSize = null);
            };
        });

        builder.Build().RunDreamineWpfApp<App>();
    }
}
