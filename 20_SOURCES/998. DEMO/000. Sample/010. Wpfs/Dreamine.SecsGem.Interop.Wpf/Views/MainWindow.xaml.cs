using System.ComponentModel;
using System.Windows;

namespace Dreamine.SecsGem.Interop.Wpf.Views;

public partial class MainWindow : Window
{
    private bool _shutdownStarted;
    private bool _shutdownCompleted;

    public MainWindow() => InitializeComponent();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_shutdownCompleted)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        base.OnClosing(e);
        if (_shutdownStarted) return;
        _shutdownStarted = true;
        _ = DisposeAndCloseAsync();
    }

    private async Task DisposeAndCloseAsync()
    {
        try
        {
            if (DataContext is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"Harness shutdown cleanup failed: {exception}");
        }
        finally
        {
            _shutdownCompleted = true;
            Close();
        }
    }
}
