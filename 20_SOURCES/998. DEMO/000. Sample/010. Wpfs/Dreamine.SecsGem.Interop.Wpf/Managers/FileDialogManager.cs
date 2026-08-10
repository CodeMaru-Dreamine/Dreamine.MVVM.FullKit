using Microsoft.Win32;
using System.IO;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class FileDialogManager
{
    public string? BrowseExecutable(string currentPath)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select simulator executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = currentPath,
            CheckFileExists = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? BrowseFolder(string currentPath)
    {
        var dialog = new OpenFolderDialog { Title = "Select evidence export folder", InitialDirectory = currentPath };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? OpenMultiEquipmentConfiguration()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import multi-equipment configuration",
            Filter = "JSON configuration (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveMultiEquipmentConfiguration(string suggestedPath)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export multi-equipment configuration",
            Filter = "JSON configuration (*.json)|*.json",
            FileName = Path.GetFileName(suggestedPath),
            InitialDirectory = Path.GetDirectoryName(suggestedPath),
            AddExtension = true,
            DefaultExt = ".json"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenFactoryResult()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import headless FactoryScale result",
            Filter = "Factory result JSON (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
