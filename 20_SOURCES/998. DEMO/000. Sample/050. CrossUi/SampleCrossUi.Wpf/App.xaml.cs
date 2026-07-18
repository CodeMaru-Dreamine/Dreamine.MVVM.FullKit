using System.Collections.Generic;
using System.Windows;
using Dreamine.MVVM.Core;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Wpf.Equipment.Popup;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;
using Dreamine.UI.Wpf.Localization;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;
using SampleCrossUi.Wpf.ViewModels; // PopupViewModel, MainViewModel

namespace SampleCrossUi.Wpf;

/// <summary>
/// \if KO
/// <para>App 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates app functionality and related state.</para>
/// \endif
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// \if KO
    /// <para>Startup 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the startup event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DMContainer.Register<ICounterService, CounterService>();
        DMContainer.Register<CounterEvent>();
        DMContainer.Register<CounterViewModel>();
        DMContainer.Register<LightBulbModel>();
        DMContainer.Register<LightBulbEvent>();
        DMContainer.Register<LightBulbViewModel>();
        DMContainer.RegisterSingleton<IPopupService, DreaminePopupService>();
        DMContainer.Register<ControlsEvent>();
        DMContainer.Register<ControlsViewModel>();
        DMContainer.Register<PopupEvent>();
        DMContainer.Register<PopupViewModel>();
        DMContainer.Register<MainViewModel>();

        // DreamineLocalizationManager에 샘플 번역 데이터를 주입해서
        // App.xaml의 StaticLocalizationProxy(LangProxy)가 실제로 동작하도록 한다.
        DreamineLocalizationManager.SetLanguageData(Language.Korean, new Dictionary<Language, Dictionary<string, Dictionary<string, string>>>
        {
            [Language.Korean] = new()
            {
                ["Main"] = new() { ["Greeting"] = "Dreamine Cross-UI 샘플에 오신 것을 환영합니다." }
            },
            [Language.English] = new()
            {
                ["Main"] = new() { ["Greeting"] = "Welcome to the Dreamine Cross-UI sample." }
            }
        });

        var mainWindow = new MainWindow();
        mainWindow.Closed += (_, _) =>
        {
            DreamineVirtualKeyboardAssist.Shutdown();
            Shutdown();
        };
        mainWindow.Show();
    }
}
