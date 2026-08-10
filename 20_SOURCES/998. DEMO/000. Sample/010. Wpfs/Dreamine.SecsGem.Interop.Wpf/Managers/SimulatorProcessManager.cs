using System.Diagnostics;
using System.IO;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class SimulatorProcessManager
{
    public const string DefaultExecutablePath = @"C:\Program Files (x86)\SEComSimulator\SEComSimulator.exe";
    public bool IsInstalled(string path) => File.Exists(path);
    public void Launch(string path)
    {
        if (!IsInstalled(path)) throw new FileNotFoundException("The external simulator executable was not found at the configured local installation path.");
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
}
