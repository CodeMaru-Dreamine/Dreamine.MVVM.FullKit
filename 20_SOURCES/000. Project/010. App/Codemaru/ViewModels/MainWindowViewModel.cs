using Codemaru.Services;
using Codemaru.States;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace Codemaru.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly CardHybridSession _session;
    private readonly CertificateMonitorViewModel _certificateMonitor;
    private bool _disposed;

    [DreamineProperty] private string _title = "Codemaru";
    [DreamineProperty] private string _brand = string.Empty;
    [DreamineProperty] private string _ownerName = string.Empty;
    [DreamineProperty] private string _landingUrl = string.Empty;
    [DreamineProperty] private string _currentUser = "Guest";
    [DreamineProperty] private string _statusMessage = "명함 랜딩 페이지 준비 중";
    [DreamineProperty] private int _historyCount;
    [DreamineProperty] private ObservableCollection<string> _logs = new();

    public CertificateMonitorViewModel CertificateMonitor => _certificateMonitor;

    public MainWindowViewModel(CardHybridSession session, CertificateMonitorViewModel certificateMonitor)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _certificateMonitor = certificateMonitor ?? throw new ArgumentNullException(nameof(certificateMonitor));
        _session.StateChanged += OnSessionChanged;
        ApplyState(_session.State);
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] CardHybrid started");
    }

    private void OnSessionChanged(object? sender, CardHybridState state)
    {
        _ = RunOnUiAsync(() => ApplyState(state));
    }

    private void ApplyState(CardHybridState state)
    {
        Brand = state.Profile.Brand;
        OwnerName = state.Profile.Name;
        LandingUrl = state.Profile.LandingUrl;
        CurrentUser = string.IsNullOrWhiteSpace(_session.CurrentUser.Email)
            ? "Guest"
            : _session.CurrentUser.Email;
        HistoryCount = state.History.Count;
        StatusMessage = $"{state.Profile.Name} / {state.Profile.Role}";
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {state.Profile.LandingSlug} updated, history {HistoryCount}");
    }

    private static Task RunOnUiAsync(Action action)
    {
        if (Application.Current?.Dispatcher is not { } dispatcher)
        {
            action();
            return Task.CompletedTask;
        }

        if (dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.StateChanged -= OnSessionChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
