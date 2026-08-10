using Microsoft.Win32;

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
}
