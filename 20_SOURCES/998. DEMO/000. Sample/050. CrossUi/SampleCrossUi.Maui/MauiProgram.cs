using Microsoft.Extensions.Logging;
using SampleCrossUi.Maui.Views;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register the same shared services/view models used by WPF / WinForms / Blazor
        builder.Services.AddSingleton<ICounterService, CounterService>();
        builder.Services.AddSingleton<CounterEvent>();
        builder.Services.AddSingleton<CounterViewModel>();
        builder.Services.AddSingleton<LightBulbModel>();
        builder.Services.AddSingleton<LightBulbEvent>();
        builder.Services.AddSingleton<LightBulbViewModel>();
        builder.Services.AddSingleton<ControlsEvent>();
        builder.Services.AddSingleton<ControlsViewModel>();

        builder.Services.AddSingleton<CounterPage>();
        builder.Services.AddSingleton<LightBulbPage>();
        builder.Services.AddSingleton<ControlsPage>();
        builder.Services.AddSingleton<PopupPage>();
        builder.Services.AddSingleton<MainPage>();


        return builder.Build();
    }
}
