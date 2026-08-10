using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Dreamine.MVVM.ViewModels;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ConnectionManager _connection;
    private readonly MessageManager _messages;
    private readonly ScenarioManager _scenarios;
    private readonly InteropLogManager _log;
    private readonly ResultExportManager _export;
    private readonly SimulatorProcessManager _simulator;
    private readonly FileDialogManager _fileDialog;
    private CancellationTokenSource? _operationCancellation;
    private string _tcpState = "Disconnected";
    private string _hsmsState = "NotConnected";
    private string _statusText = "Ready. External simulator scenarios: Not Run.";
    private string _lastActivity = "None";
    private string _currentError = "None";
    private string _lastError = "None";
    private byte _stream = 1;
    private byte _function = 1;
    private bool _replyExpected = true;
    private int _messageIterations = 1000;
    private int _reconnectCycles = 100;
    private string _exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DreamineInteropResults");
    private SecsItemNodeViewModel? _selectedItem;
    private InteropScenarioResult? _selectedScenario;
    private InteropLogEntry? _selectedLog;
    private string _simulatorExecutablePath = SimulatorProcessManager.DefaultExecutablePath;
    private bool _equipmentResponderEnabled;
    private bool _isLogViewPaused;
    private bool _autoScrollLogs = true;
    private string _logFilterText = string.Empty;

    public MainWindowViewModel(ConnectionManager connection, MessageManager messages, ScenarioManager scenarios,
        InteropLogManager log, ResultExportManager export, SimulatorProcessManager simulator, FileDialogManager fileDialog)
    {
        _connection = connection; _messages = messages; _scenarios = scenarios; _log = log; _export = export; _simulator = simulator; _fileDialog = fileDialog;
        FilteredLogs = CreateLogView(_log.Entries);
        FilteredSecs1Logs = CreateLogView(_log.Secs1Entries);
        FilteredSecs2Logs = CreateLogView(_log.Secs2Entries);
        _connection.StateChanged += (_, _) => Dispatch(RefreshStates);
        _log.EntryAdded += (_, entry) => Dispatch(() =>
        {
            LastActivity = $"{entry.Timestamp:HH:mm:ss.fff} {entry.Summary}";
            if (entry.Level != "Error") return;
            CurrentError = entry.Summary;
            LastError = entry.Summary;
        });
        ConnectCommand = Command(ConnectAsync, () => TcpState is "Disconnected" or "Faulted");
        DisconnectCommand = Command(DisconnectAsync, () => TcpState is not "Disconnected");
        SelectCommand = Command(() => RunAsync("Select", token => _connection.SelectAsync(token)), () => HsmsState == "ConnectedNotSelected");
        LinktestCommand = Command(() => RunAsync("Linktest", token => _connection.LinktestAsync(token)), () => HsmsState == "Selected");
        SeparateCommand = Command(() => RunAsync("Separate", token => _connection.SeparateAsync(token)), () => HsmsState == "Selected");
        ReconnectCommand = Command(() => RunAsync("Reconnect", token => _connection.ReconnectAsync(token)), () => TcpState is "Connected" or "Faulted");
        SendCommand = Command(SendAsync); RunSelfTestCommand = Command(RunSelfTestAsync);
        ExportCommand = Command(ExportAsync);
        EnableResponderCommand = new RelayCommand(EnableResponder);
        LaunchSimulatorCommand = new RelayCommand(LaunchSimulator);
        BrowseSimulatorCommand = new RelayCommand(BrowseSimulator);
        BrowseExportCommand = new RelayCommand(BrowseExport);
        CancelCommand = new RelayCommand(() => _operationCancellation?.Cancel());
        MarkWaitingCommand = new RelayCommand(() => { _scenarios.MarkExternalWaiting(); StatusText = "Waiting for User: follow the documented simulator steps."; });
        RunSelectedCommand = Command(RunSelectedAsync);
        RunAllCommand = Command(RunSelfTestAsync);
        ResetScenariosCommand = new RelayCommand(() => { _scenarios.Reset(); StatusText = "Scenario results reset to Not Run."; });
        AddRootItemCommand = new RelayCommand(() => RootItems.Add(new SecsItemNodeViewModel()));
        AddChildItemCommand = new RelayCommand(() => SelectedItem?.Children.Add(new SecsItemNodeViewModel(SecsItemEditorFormat.Ascii)));
        RemoveItemCommand = new RelayCommand(RemoveSelectedItem);
        ClearLogCommand = new RelayCommand(_log.Clear);
    }

    public ConnectionSettings Settings { get; } = new();
    public IReadOnlyList<SecsConnectionMode> ConnectionModes { get; } = new[] { SecsConnectionMode.Active, SecsConnectionMode.Passive };
    public IReadOnlyList<SecsRole> Roles { get; } = new[] { SecsRole.Host, SecsRole.Equipment };
    public ObservableCollection<SecsItemNodeViewModel> RootItems { get; } = new();
    public ObservableCollection<InteropScenarioResult> Scenarios => _scenarios.Scenarios;
    public ObservableCollection<InteropLogEntry> Logs => _log.Entries;
    public ObservableCollection<InteropLogEntry> Secs1Logs => _log.Secs1Entries;
    public ObservableCollection<InteropLogEntry> Secs2Logs => _log.Secs2Entries;
    public ICollectionView FilteredLogs { get; }
    public ICollectionView FilteredSecs1Logs { get; }
    public ICollectionView FilteredSecs2Logs { get; }
    public bool SimulatorInstalled => _simulator.IsInstalled(SimulatorExecutablePath);
    public string SimulatorAvailability => SimulatorInstalled ? "Installed (launch is manual)" : "Not found";
    public string TcpState { get => _tcpState; private set => SetProperty(ref _tcpState, value); }
    public string HsmsState { get => _hsmsState; private set => SetProperty(ref _hsmsState, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string LastActivity { get => _lastActivity; private set => SetProperty(ref _lastActivity, value); }
    public string CurrentError { get => _currentError; private set => SetProperty(ref _currentError, value); }
    public string LastError { get => _lastError; private set => SetProperty(ref _lastError, value); }
    public byte Stream { get => _stream; set => SetProperty(ref _stream, value); }
    public byte Function { get => _function; set => SetProperty(ref _function, value); }
    public bool ReplyExpected { get => _replyExpected; set => SetProperty(ref _replyExpected, value); }
    public int MessageIterations { get => _messageIterations; set => SetProperty(ref _messageIterations, value); }
    public int ReconnectCycles { get => _reconnectCycles; set => SetProperty(ref _reconnectCycles, value); }
    public string ExportDirectory { get => _exportDirectory; set => SetProperty(ref _exportDirectory, value); }
    public SecsItemNodeViewModel? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
    public InteropScenarioResult? SelectedScenario { get => _selectedScenario; set => SetProperty(ref _selectedScenario, value); }
    public InteropLogEntry? SelectedLog { get => _selectedLog; set => SetProperty(ref _selectedLog, value); }
    public string SimulatorExecutablePath { get => _simulatorExecutablePath; set { if (SetProperty(ref _simulatorExecutablePath, value)) { OnPropertyChanged(nameof(SimulatorInstalled)); OnPropertyChanged(nameof(SimulatorAvailability)); } } }
    public bool EquipmentResponderEnabled
    {
        get => _equipmentResponderEnabled;
        private set
        {
            if (!SetProperty(ref _equipmentResponderEnabled, value)) return;
            OnPropertyChanged(nameof(EquipmentResponderState));
            OnPropertyChanged(nameof(EquipmentResponderButtonText));
        }
    }
    public string EquipmentResponderState => EquipmentResponderEnabled ? "ON" : "OFF";
    public string EquipmentResponderButtonText => EquipmentResponderEnabled ? "✓ Equipment Responder: ON" : "Enable Equipment Responder";
    public bool IsLogViewPaused
    {
        get => _isLogViewPaused;
        set
        {
            if (!SetProperty(ref _isLogViewPaused, value)) return;
            _log.SetPaused(value);
            OnPropertyChanged(nameof(LogViewState));
        }
    }
    public string LogViewState => IsLogViewPaused ? "Paused" : "Live";
    public bool AutoScrollLogs { get => _autoScrollLogs; set => SetProperty(ref _autoScrollLogs, value); }
    public string LogFilterText
    {
        get => _logFilterText;
        set
        {
            if (!SetProperty(ref _logFilterText, value)) return;
            FilteredLogs.Refresh(); FilteredSecs1Logs.Refresh(); FilteredSecs2Logs.Refresh();
        }
    }

    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public AsyncRelayCommand SelectCommand { get; }
    public AsyncRelayCommand LinktestCommand { get; }
    public AsyncRelayCommand SeparateCommand { get; }
    public AsyncRelayCommand ReconnectCommand { get; }
    public AsyncRelayCommand SendCommand { get; }
    public AsyncRelayCommand RunSelfTestCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }
    public AsyncRelayCommand RunSelectedCommand { get; }
    public AsyncRelayCommand RunAllCommand { get; }
    public RelayCommand EnableResponderCommand { get; }
    public RelayCommand LaunchSimulatorCommand { get; }
    public RelayCommand BrowseSimulatorCommand { get; }
    public RelayCommand BrowseExportCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand MarkWaitingCommand { get; }
    public RelayCommand ResetScenariosCommand { get; }
    public RelayCommand AddRootItemCommand { get; }
    public RelayCommand AddChildItemCommand { get; }
    public RelayCommand RemoveItemCommand { get; }
    public RelayCommand ClearLogCommand { get; }

    private AsyncRelayCommand Command(Func<Task> execute, Func<bool>? canExecute = null)
    {
        var command = new AsyncRelayCommand(execute, canExecute);
        command.ExecutionFailed += (_, exception) => { _log.Error("Command", exception); StatusText = exception.Message; RefreshStates(); };
        return command;
    }

    private Task ConnectAsync() => RunAsync("Connect", token => _connection.ConnectAsync(Settings, token));
    private Task DisconnectAsync() => RunAsync("Disconnect", token => _connection.DisconnectAsync(token));
    private async Task SendAsync()
    {
        var item = RootItems.Count switch { 0 => null, 1 => RootItems[0].ToSecsItem(), _ => new SecsListItem(RootItems.Select(value => value.ToSecsItem()).ToArray()) };
        await RunAsync($"S{Stream}F{Function}", token => _messages.SendAsync(Stream, Function, ReplyExpected, item, token));
    }
    private async Task RunSelfTestAsync()
    {
        await RunAsync("Self loopback", async token =>
        {
            var summary = await _scenarios.RunSelfLoopbackAsync(MessageIterations, ReconnectCycles, token);
            StatusText = $"Self loopback {summary.Result}: {summary.Passed:N0}/{summary.Requested:N0}, P95 {summary.P95LatencyMs:N2} ms.";
        });
    }
    private Task RunSelectedAsync()
    {
        if (SelectedScenario is null) { StatusText = "Select a scenario first."; return Task.CompletedTask; }
        if (SelectedScenario.External) { SelectedScenario.Status = InteropScenarioStatus.WaitingForUser; SelectedScenario.Detail = "Waiting for User: complete the documented simulator action."; StatusText = SelectedScenario.Detail; return Task.CompletedTask; }
        return RunSelfTestAsync();
    }
    private async Task ExportAsync()
    {
        var paths = await _export.ExportAsync(ExportDirectory, Scenarios, Logs, CancellationToken.None);
        StatusText = $"Exported: {Path.GetFileName(paths.JsonPath)}, {Path.GetFileName(paths.MarkdownPath)}";
    }
    private async Task RunAsync(string operation, Func<CancellationToken, Task> action)
    {
        _operationCancellation?.Dispose(); _operationCancellation = new CancellationTokenSource();
        StatusText = $"{operation} running...";
        try { await action(_operationCancellation.Token); CurrentError = "None"; StatusText = $"{operation} completed."; }
        catch (OperationCanceledException) { StatusText = $"{operation} cancelled."; }
        finally { RefreshStates(); }
    }
    private void EnableResponder()
    {
        try
        {
            _messages.EnableEquipmentResponder();
            EquipmentResponderEnabled = true;
            StatusText = "Basic equipment responder enabled and will persist across reconnects.";
        }
        catch (Exception exception) { _log.Error("Responder", exception); StatusText = exception.Message; }
    }
    private void LaunchSimulator()
    {
        try { _simulator.Launch(SimulatorExecutablePath); _scenarios.MarkExternalWaiting(); StatusText = "Simulator launched. Waiting for User interaction."; }
        catch (Exception exception) { _log.Error("Simulator", exception); StatusText = exception.Message; }
    }
    private void BrowseSimulator()
    {
        var selected = _fileDialog.BrowseExecutable(SimulatorExecutablePath);
        if (selected is not null) SimulatorExecutablePath = selected;
    }
    private void BrowseExport()
    {
        var selected = _fileDialog.BrowseFolder(ExportDirectory);
        if (selected is not null) ExportDirectory = selected;
    }
    private void RefreshStates()
    {
        TcpState = _connection.TcpState; HsmsState = _connection.HsmsState;
        ConnectCommand.RaiseCanExecuteChanged(); DisconnectCommand.RaiseCanExecuteChanged();
        SelectCommand.RaiseCanExecuteChanged(); LinktestCommand.RaiseCanExecuteChanged();
        SeparateCommand.RaiseCanExecuteChanged(); ReconnectCommand.RaiseCanExecuteChanged();
    }
    private void RemoveSelectedItem()
    {
        if (SelectedItem is null) return;
        if (!RootItems.Remove(SelectedItem)) foreach (var root in RootItems) if (RemoveFrom(root, SelectedItem)) break;
        SelectedItem = null;
    }
    private static bool RemoveFrom(SecsItemNodeViewModel parent, SecsItemNodeViewModel target)
    {
        if (parent.Children.Remove(target)) return true;
        return parent.Children.Any(child => RemoveFrom(child, target));
    }
    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else _ = dispatcher.BeginInvoke(action);
    }

    private ICollectionView CreateLogView(ObservableCollection<InteropLogEntry> source)
    {
        var view = new ListCollectionView(source) { IsLiveFiltering = false };
        view.Filter = item => item is InteropLogEntry entry && MatchesLogFilter(entry);
        return view;
    }

    private bool MatchesLogFilter(InteropLogEntry entry)
    {
        var filter = LogFilterText.Trim();
        return filter.Length == 0 || entry.Direction.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.SxFy.Contains(filter, StringComparison.OrdinalIgnoreCase) || entry.SystemBytes.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) || entry.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            (entry.Message?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
