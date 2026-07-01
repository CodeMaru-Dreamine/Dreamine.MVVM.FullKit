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

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var builder = Host.CreateApplicationBuilder();
        int port = builder.Configuration.GetValue("DreamineServer:Port", 4090);
        bool listenAny = builder.Configuration.GetValue("DreamineServer:ListenAnyIp", true);

        var opts = builder.Configuration.GetSection("Dreamine").Get<DreamineOptions>() ?? new();
        builder.Services.AddSingleton(opts);

        builder.Services.AddDreamineHybridWpf();
        builder.Services.AddSingleton<ILibraryStore, JsonLibraryStore>();
        builder.Services.AddSingleton<MainWindow>();

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = port;
            options.ListenAnyIp = listenAny;
            options.SharedServiceTypes.Add(typeof(ILibraryStore));
            options.SharedServiceTypes.Add(typeof(DreamineOptions));
            options.ConfigureServices = services =>
            {
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
