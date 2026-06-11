using Dreamine.MVVM.Core;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms;

/// <summary>
/// WinForms Cross-UI sample entry point.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        DMContainer.Register<ICounterService, CounterService>();
        DMContainer.Register<CounterViewModel>();

        var viewModel = DMContainer.Resolve<CounterViewModel>();
        Application.Run(new MainForm(viewModel));
    }
}
