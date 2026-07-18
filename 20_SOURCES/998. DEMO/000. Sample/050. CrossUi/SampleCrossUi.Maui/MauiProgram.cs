using Microsoft.Extensions.Logging;
using SampleCrossUi.Maui.Views;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Maui;

/// <summary>
/// \if KO
/// <para>Maui Program 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates maui program functionality and related state.</para>
/// \endif
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// \if KO
    /// <para>Maui App 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the maui app value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Maui App 작업에서 생성한 <c>MauiApp</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MauiApp</c> result produced by the create maui app operation.</para>
    /// \endif
    /// </returns>
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
