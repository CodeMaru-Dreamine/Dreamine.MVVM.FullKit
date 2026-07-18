using Dreamine.MVVM.Core;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

namespace SampleCrossUi.WinForms;

/// <summary>
/// \if KO
/// <para>Program 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates program functionality and related state.</para>
/// \endif
/// </summary>
internal static class Program
{
    /// <summary>
    /// \if KO
    /// <para>Main 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the main operation.</para>
    /// \endif
    /// </summary>
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
