using System.Windows;
using Dreamine.MVVM.Core;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.Wpf;

/// <summary>
/// WPF Cross-UI sample application entry point.
/// Registers services into DMContainer and launches MainWindow.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Registers DI services before the first window is shown.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DMContainer.Register<ICounterService, CounterService>();
        DMContainer.Register<CounterViewModel>();
    }
}
