using Codemaru.Blazor;
using Codemaru.Blazor.Pages;
using Codemaru.Models;
using Codemaru.Services;
using Codemaru.ViewModels;
using Codemaru.Views;
using Dreamine.Hybrid.Wpf.DependencyInjection;
using Dreamine.Hybrid.Wpf.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Codemaru;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        int serverPort = GetInt(builder.Configuration, "CodemaruServer:Port", 5040);
        bool listenAnyIp = GetBool(builder.Configuration, "CodemaruServer:ListenAnyIp", true);

        builder.Services.AddDreamineHybridWpf();

        builder.Services.AddSingleton<FreeQrSvgGenerator>();
        builder.Services.AddSingleton<ImageBackgroundRemover>();
        builder.Services.AddSingleton<LandingPageExporter>();
        builder.Services.AddSingleton<LandingPagePublisher>();
        builder.Services.AddSingleton<CardProfileImporter>();
        builder.Services.AddSingleton<ICardProfileStore, JsonCardProfileStore>();
        builder.Services.AddSingleton<CardHybridSession>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<MainWindowViewModel>();
        builder.Services.Configure<Contact.MailSettings>(
            builder.Configuration.GetSection("MailSettings"));
        builder.Services.Configure<FfmpegOptions>(
            builder.Configuration.GetSection("Ffmpeg"));
        builder.Services.Configure<List<StreamConfig>>(
            builder.Configuration.GetSection("Streams"));

        builder.Services.AddDreamineBlazorServer<AppShell>(options =>
        {
            options.Port = serverPort;
            options.ListenAnyIp = listenAnyIp;
            options.SharedServiceTypes.Add(typeof(CardHybridSession));
            options.SharedServiceTypes.Add(typeof(FreeQrSvgGenerator));
            options.SharedServiceTypes.Add(typeof(ImageBackgroundRemover));
            options.SharedServiceTypes.Add(typeof(LandingPageExporter));
            options.SharedServiceTypes.Add(typeof(LandingPagePublisher));
            options.SharedServiceTypes.Add(typeof(CardProfileImporter));
        });


        builder.Build().RunDreamineWpfApp<App>();
    }

    private static int GetInt(IConfiguration configuration, string key, int fallback)
    {
        string? value = configuration[key];

        return int.TryParse(value, out int parsed)
            ? parsed
            : fallback;
    }

    private static bool GetBool(IConfiguration configuration, string key, bool fallback)
    {
        string? value = configuration[key];

        return bool.TryParse(value, out bool parsed)
            ? parsed
            : fallback;
    }
}
