using Dreamine.MVVM.Core;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        DMContainer.Register<ICounterService, CounterService>();
        DMContainer.Register<CounterViewModel>();

        var counterVm = DMContainer.Resolve<CounterViewModel>();
        Application.Run(new MainForm(counterVm));
    }
}
