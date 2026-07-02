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
        DMContainer.Register<CounterEvent>();
        DMContainer.Register<CounterViewModel>();
        DMContainer.Register<LightBulbModel>();
        DMContainer.Register<LightBulbEvent>();
        DMContainer.Register<LightBulbViewModel>();

        var counterVm = DMContainer.Resolve<CounterViewModel>();
        var lightBulbVm = DMContainer.Resolve<LightBulbViewModel>();
        Application.Run(new MainForm(counterVm, lightBulbVm));
    }
}
