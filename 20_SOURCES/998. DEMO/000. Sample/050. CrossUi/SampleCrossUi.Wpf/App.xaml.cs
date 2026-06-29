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

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DMContainer.Register<ICounterService, CounterService>();
        DMContainer.Register<CounterEvent>();
        DMContainer.Register<CounterViewModel>();
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
