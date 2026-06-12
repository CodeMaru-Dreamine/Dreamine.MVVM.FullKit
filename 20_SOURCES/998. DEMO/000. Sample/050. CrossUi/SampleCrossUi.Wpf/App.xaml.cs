using System.Windows;
using Dreamine.MVVM.Core;
using Dreamine.UI.Abstractions.Popup;
using Dreamine.UI.Wpf.Equipment.Popup;
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

        new MainWindow().Show();
    }
}
