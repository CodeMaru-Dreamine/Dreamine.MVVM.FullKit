using Dreamine.MVVM.Core;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.ViewModels;
using Dreamine.SecsGem.Interop.Wpf.Views;

namespace Dreamine.SecsGem.Interop.Wpf;

public partial class App
{
    static partial void ShowMainWindow()
    {
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            _ = RunHeadlessSelfTestAsync(arguments);
            return;
        }

        var view = new MainWindow
        {
            DataContext = DMContainer.Resolve<MainWindowViewModel>()
        };
        Current.MainWindow = view;
        view.Show();
    }

    private static async Task RunHeadlessSelfTestAsync(string[] arguments)
    {
        var exitCode = 1;
        try
        {
            var scenarioManager = DMContainer.Resolve<ScenarioManager>();
            var result = await scenarioManager.RunSelfLoopbackAsync(1000, 100, CancellationToken.None);
            var outputIndex = Array.FindIndex(arguments, value => value.Equals("--output", StringComparison.OrdinalIgnoreCase));
            if (outputIndex >= 0 && outputIndex + 1 < arguments.Length)
            {
                var exportManager = DMContainer.Resolve<ResultExportManager>();
                await exportManager.ExportSelfTestAsync(arguments[outputIndex + 1], result, CancellationToken.None);
            }
            exitCode = result.Failed == 0 ? 0 : 2;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
        }
        finally
        {
            Environment.ExitCode = exitCode;
            await Current.Dispatcher.InvokeAsync(Current.Shutdown);
        }
    }
}
