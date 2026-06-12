using System.Windows;
using Dreamine.MVVM.Core;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Wpf.Equipment.Popup;
using Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;
using SampleCrossUi.Wpf.ViewModels;

namespace SampleCrossUi.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DMContainer.Register<ICounterService, CounterService>();
        DMContainer.Register<CounterViewModel>();
        DMContainer.RegisterSingleton<IPopupService, DreaminePopupService>();
        DMContainer.Register<ControlsViewModel>();
        DMContainer.Register<PopupViewModel>();
        DMContainer.Register<MainViewModel>();

        var mainWindow = new MainWindow();
        mainWindow.Closed += (_, _) =>
        {
            DreamineVirtualKeyboardAssist.Shutdown();
            Shutdown();
        };
        mainWindow.Show();
    }
}
