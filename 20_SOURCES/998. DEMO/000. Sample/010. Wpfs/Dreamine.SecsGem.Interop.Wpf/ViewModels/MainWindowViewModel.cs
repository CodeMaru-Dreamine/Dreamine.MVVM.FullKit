using System.Collections;
using System.Collections.Specialized;
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

public sealed class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly ConnectionManager _connection;
    private readonly MessageManager _messages;
    private readonly ScenarioManager _scenarios;
    private readonly InteropLogManager _log;
    private readonly ResultExportManager _export;
    private readonly SimulatorProcessManager _simulator;
    private readonly FileDialogManager _fileDialog;
    private readonly MultiEquipmentHost _multiEquipmentHost;
    private readonly MultiEquipmentScenarioManager _multiEquipmentScenarios;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _operationCancellationGate = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HashSet<EquipmentConnectionContext> _observedEquipment = [];
    private CancellationTokenSource? _operationCancellation;
    private int _disposed;
    private int _operationRunning;
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
    private string _hostMode = "Single Equipment";
    private int _selectedMainTabIndex;
    private object? _selectedEquipment;
    private string _newEquipmentId = "EQ-001";
    private string _newEquipmentHost = "127.0.0.1";
    private int _newEquipmentPort = 7101;
    private SecsConnectionMode _newEquipmentMode = SecsConnectionMode.Active;
    private ushort _newEquipmentSessionId;
    private string _equipmentLogFilter = "All Equipment";
    private MultiEquipmentSelfTestSummary? _lastMultiEquipmentSummary;

    public MainWindowViewModel(ConnectionManager connection, MessageManager messages, ScenarioManager scenarios,
        InteropLogManager log, ResultExportManager export, SimulatorProcessManager simulator, FileDialogManager fileDialog)
    {
        _connection = connection; _messages = messages; _scenarios = scenarios; _log = log; _export = export; _simulator = simulator; _fileDialog = fileDialog;
        _multiEquipmentHost = new MultiEquipmentHost(log);
        _multiEquipmentScenarios = new MultiEquipmentScenarioManager(log);
        FilteredLogs = CreateLogView(_log.Entries);
        FilteredSecs1Logs = CreateLogView(_log.Secs1Entries);
        FilteredSecs2Logs = CreateLogView(_log.Secs2Entries);
        FilteredDiagnosticsLogs = CreateLogView(_log.DiagnosticsEntries);
        ((INotifyCollectionChanged)_multiEquipmentHost.Connections).CollectionChanged += (_, _) => Dispatch(RefreshEquipmentCollection);
        _connection.StateChanged += (_, _) => Dispatch(RefreshStates);
        _log.EntryAdded += (_, entry) => Dispatch(() =>
        {
            LastActivity = $"{entry.Timestamp:HH:mm:ss.fff} {entry.Summary}";
            if (!string.IsNullOrWhiteSpace(entry.EquipmentId) && entry.EquipmentId != "--" &&
                !EquipmentFilterOptions.Contains(entry.EquipmentId))
                EquipmentFilterOptions.Add(entry.EquipmentId);
            if (entry.Level != "Error") return;
            CurrentError = entry.Summary;
            LastError = entry.Summary;
        });
        ConnectCommand = Command(ConnectAsync, () => IsSingleEquipmentMode && TcpState is ("Disconnected" or "Faulted"));
        DisconnectCommand = Command(DisconnectAsync, () => IsSingleEquipmentMode && TcpState is not "Disconnected");
        SelectCommand = Command(() => RunAsync("Select", token => _connection.SelectAsync(token)), () => IsSingleEquipmentMode && HsmsState == "ConnectedNotSelected");
        LinktestCommand = Command(() => RunAsync("Linktest", token => _connection.LinktestAsync(token)), () => IsSingleEquipmentMode && HsmsState == "Selected");
        SeparateCommand = Command(() => RunAsync("Separate", token => _connection.SeparateAsync(token)), () => IsSingleEquipmentMode && HsmsState == "Selected");
        ReconnectCommand = Command(() => RunAsync("Reconnect", token => _connection.ReconnectAsync(token)), () => IsSingleEquipmentMode && TcpState is ("Connected" or "Faulted"));
        SendCommand = Command(SendAsync, () => IsSingleEquipmentMode && HsmsState == "Selected"); RunSelfTestCommand = Command(RunSelfTestAsync);
        ExportCommand = Command(ExportAsync);
        EnableResponderCommand = new RelayCommand(EnableResponder);
        LaunchSimulatorCommand = new RelayCommand(LaunchSimulator);
        BrowseSimulatorCommand = new RelayCommand(BrowseSimulator);
        BrowseExportCommand = new RelayCommand(BrowseExport);
        CancelCommand = new RelayCommand(CancelCurrentOperation);
        MarkWaitingCommand = new RelayCommand(() => { _scenarios.MarkExternalWaiting(); StatusText = "Waiting for User: follow the documented simulator steps."; });
        RunSelectedCommand = Command(RunSelectedAsync);
        RunAllCommand = Command(RunSelfTestAsync);
        ResetScenariosCommand = new RelayCommand(() => { _scenarios.Reset(); StatusText = "Scenario results reset to Not Run."; });
        AddRootItemCommand = new RelayCommand(() => RootItems.Add(new SecsItemNodeViewModel()));
        AddChildItemCommand = new RelayCommand(() => SelectedItem?.Children.Add(new SecsItemNodeViewModel(SecsItemEditorFormat.Ascii)));
        RemoveItemCommand = new RelayCommand(RemoveSelectedItem);
        ClearLogCommand = new RelayCommand(_log.Clear);
        AddEquipmentCommand = new RelayCommand(AddEquipment, () => IsOperationIdle && IsMultiEquipmentMode);
        RemoveEquipmentCommand = Command(RemoveSelectedEquipmentAsync, () => IsMultiEquipmentMode && SelectedEquipmentContext is not null);
        ConnectSelectedEquipmentCommand = Command(() => RunSelectedEquipmentAsync("Connect selected", (id, token) => _multiEquipmentHost.ConnectAsync(id, token)),
            () => IsMultiEquipmentMode && SelectedEquipmentContext is { IsConnected: false, TcpState: not "Connecting" and not "Listening" });
        DisconnectSelectedEquipmentCommand = Command(() => RunSelectedEquipmentAsync("Disconnect selected", (id, token) => _multiEquipmentHost.DisconnectAsync(id, token)),
            () => IsMultiEquipmentMode && SelectedEquipmentContext?.Session is not null);
        ReconnectSelectedEquipmentCommand = Command(() => RunSelectedEquipmentAsync("Reconnect selected", (id, token) => _multiEquipmentHost.ReconnectAsync(id, token)),
            () => IsMultiEquipmentMode && SelectedEquipmentContext is not null);
        ConnectAllEquipmentCommand = Command(() => RunMultiResultAsync("Connect all", token => _multiEquipmentHost.ConnectAllAsync(token)),
            () => IsMultiEquipmentMode && _multiEquipmentHost.Connections.Any(value => !value.IsConnected));
        DisconnectAllEquipmentCommand = Command(() => RunMultiResultAsync("Disconnect all", token => _multiEquipmentHost.DisconnectAllAsync(token)),
            () => IsMultiEquipmentMode && _multiEquipmentHost.Connections.Any(value => value.Session is not null));
        LinktestSelectedEquipmentCommand = Command(() => RunSelectedEquipmentAsync("Linktest selected", (id, token) => _multiEquipmentHost.LinktestAsync(id, token)),
            () => IsMultiEquipmentMode && SelectedEquipmentContext?.IsSelected == true);
        LinktestAllEquipmentCommand = Command(() => RunMultiResultAsync("Linktest all", token => _multiEquipmentHost.LinktestAllAsync(token)),
            () => IsMultiEquipmentMode && _multiEquipmentHost.Connections.Any(value => value.IsSelected));
        SendSelectedEquipmentCommand = Command(SendSelectedEquipmentAsync, () => IsMultiEquipmentMode && SelectedEquipmentContext?.IsSelected == true);
        BroadcastEquipmentCommand = Command(BroadcastEquipmentAsync,
            () => IsMultiEquipmentMode && _multiEquipmentHost.Connections.Any(value => value.IsSelected));
        RunSelectedEquipmentScenarioCommand = Command(RunSelectedEquipmentScenarioAsync,
            () => IsMultiEquipmentMode && SelectedEquipmentContext?.IsSelected == true);
        RunAllEquipmentScenariosCommand = Command(RunAllEquipmentScenariosAsync,
            () => IsMultiEquipmentMode && _multiEquipmentHost.Connections.Any(value => value.IsSelected));
        RunMultiEquipmentSelfTestCommand = Command(RunMultiEquipmentSelfTestAsync, () => IsMultiEquipmentMode);
        ImportEquipmentConfigurationCommand = Command(ImportEquipmentConfigurationAsync, () => IsMultiEquipmentMode);
        ExportEquipmentConfigurationCommand = Command(ExportEquipmentConfigurationAsync, () => IsMultiEquipmentMode);
    }

    public ConnectionSettings Settings { get; } = new();
    public IReadOnlyList<SecsConnectionMode> ConnectionModes { get; } = new[] { SecsConnectionMode.Active, SecsConnectionMode.Passive };
    public IReadOnlyList<SecsRole> Roles { get; } = new[] { SecsRole.Host, SecsRole.Equipment };
    public ObservableCollection<SecsItemNodeViewModel> RootItems { get; } = new();
    public ObservableCollection<InteropScenarioResult> Scenarios => _scenarios.Scenarios;
    public ObservableCollection<InteropLogEntry> Logs => _log.Entries;
    public ObservableCollection<InteropLogEntry> Secs1Logs => _log.Secs1Entries;
    public ObservableCollection<InteropLogEntry> Secs2Logs => _log.Secs2Entries;
    public ObservableCollection<InteropLogEntry> DiagnosticsLogs => _log.DiagnosticsEntries;
    public ICollectionView FilteredLogs { get; }
    public ICollectionView FilteredSecs1Logs { get; }
    public ICollectionView FilteredSecs2Logs { get; }
    public ICollectionView FilteredDiagnosticsLogs { get; }
    public IEnumerable EquipmentConnections => _multiEquipmentHost.Connections;
    public IReadOnlyList<string> HostModes { get; } = new[] { "Single Equipment", "Multi Equipment Host" };
    public ObservableCollection<string> EquipmentFilterOptions { get; } = new() { "All Equipment" };
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
            RefreshLogViews();
        }
    }
    public string HostMode
    {
        get => _hostMode;
        set
        {
            if (!SetProperty(ref _hostMode, value)) return;
            OnPropertyChanged(nameof(IsMultiEquipmentMode));
            OnPropertyChanged(nameof(IsSingleEquipmentMode));
            SelectedMainTabIndex = IsMultiEquipmentMode ? 1 : 0;
            RaiseAllCommandStates();
        }
    }
    public bool IsMultiEquipmentMode => HostMode == "Multi Equipment Host";
    public bool IsSingleEquipmentMode => !IsMultiEquipmentMode;
    public int SelectedMainTabIndex { get => _selectedMainTabIndex; set => SetProperty(ref _selectedMainTabIndex, value); }
    public object? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (!SetProperty(ref _selectedEquipment, value)) return;
            RaiseEquipmentCommandStates();
        }
    }
    public string NewEquipmentId { get => _newEquipmentId; set => SetProperty(ref _newEquipmentId, value); }
    public string NewEquipmentHost { get => _newEquipmentHost; set => SetProperty(ref _newEquipmentHost, value); }
    public int NewEquipmentPort { get => _newEquipmentPort; set => SetProperty(ref _newEquipmentPort, value); }
    public SecsConnectionMode NewEquipmentMode { get => _newEquipmentMode; set => SetProperty(ref _newEquipmentMode, value); }
    public ushort NewEquipmentSessionId { get => _newEquipmentSessionId; set => SetProperty(ref _newEquipmentSessionId, value); }
    public string EquipmentLogFilter
    {
        get => _equipmentLogFilter;
        set { if (SetProperty(ref _equipmentLogFilter, value)) RefreshLogViews(); }
    }
    public string MultiEquipmentSummary => $"{_multiEquipmentHost.Connections.Count} configured · {_multiEquipmentHost.ActiveSessionCount} active";
    public string MultiEquipmentTestResult => _lastMultiEquipmentSummary is null
        ? "Not Run"
        : $"{_lastMultiEquipmentSummary.Result}: {_lastMultiEquipmentSummary.MessagesPassed:N0}/{_lastMultiEquipmentSummary.MessagesRequested:N0} replies";
    private EquipmentConnectionContext? SelectedEquipmentContext => SelectedEquipment as EquipmentConnectionContext;
    private bool IsOperationIdle => Volatile.Read(ref _operationRunning) == 0;

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
    public RelayCommand AddEquipmentCommand { get; }
    public AsyncRelayCommand RemoveEquipmentCommand { get; }
    public AsyncRelayCommand ConnectSelectedEquipmentCommand { get; }
    public AsyncRelayCommand DisconnectSelectedEquipmentCommand { get; }
    public AsyncRelayCommand ReconnectSelectedEquipmentCommand { get; }
    public AsyncRelayCommand ConnectAllEquipmentCommand { get; }
    public AsyncRelayCommand DisconnectAllEquipmentCommand { get; }
    public AsyncRelayCommand LinktestSelectedEquipmentCommand { get; }
    public AsyncRelayCommand LinktestAllEquipmentCommand { get; }
    public AsyncRelayCommand SendSelectedEquipmentCommand { get; }
    public AsyncRelayCommand BroadcastEquipmentCommand { get; }
    public AsyncRelayCommand RunSelectedEquipmentScenarioCommand { get; }
    public AsyncRelayCommand RunAllEquipmentScenariosCommand { get; }
    public AsyncRelayCommand RunMultiEquipmentSelfTestCommand { get; }
    public AsyncRelayCommand ImportEquipmentConfigurationCommand { get; }
    public AsyncRelayCommand ExportEquipmentConfigurationCommand { get; }

    private AsyncRelayCommand Command(Func<Task> execute, Func<bool>? canExecute = null)
    {
        var command = new AsyncRelayCommand(execute, () => IsOperationIdle && (canExecute?.Invoke() ?? true));
        command.ExecutionFailed += (_, exception) => { _log.Error("Command", exception); StatusText = exception.Message; RefreshStates(); };
        return command;
    }

    private Task ConnectAsync() => RunAsync("Connect", token => _connection.ConnectAsync(Settings, token));
    private Task DisconnectAsync() => RunAsync("Disconnect", token => _connection.DisconnectAsync(token));
    private async Task SendAsync()
    {
        var item = BuildMessageItem();
        await RunAsync($"S{Stream}F{Function}", token => _messages.SendAsync(Stream, Function, ReplyExpected, item, token));
    }
    private void AddEquipment()
    {
        try
        {
            var context = _multiEquipmentHost.Add(new EquipmentConnectionDefinition
            {
                EquipmentId = NewEquipmentId.Trim(), Host = NewEquipmentHost.Trim(), Port = NewEquipmentPort,
                Mode = NewEquipmentMode, SessionId = NewEquipmentSessionId
            });
            SelectedEquipment = context;
            StatusText = $"Added {context.EquipmentId} at {context.Endpoint}.";
            RefreshEquipmentCollection();
        }
        catch (Exception exception)
        {
            _log.Error("MultiEquipment", exception);
            CurrentError = exception.Message;
            LastError = exception.Message;
            StatusText = exception.Message;
        }
    }
    private async Task RemoveSelectedEquipmentAsync()
    {
        var context = RequireSelectedEquipment();
        await RunAsync("Remove selected", async token =>
        {
            await _multiEquipmentHost.RemoveAsync(context.EquipmentId, token);
            SelectedEquipment = null;
            RefreshEquipmentCollection();
        });
    }
    private Task RunSelectedEquipmentAsync(string operation,
        Func<string, CancellationToken, Task> action)
    {
        var context = RequireSelectedEquipment();
        return RunAsync(operation, token => action(context.EquipmentId, token));
    }
    private Task RunMultiResultAsync(string operation,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, EquipmentOperationResult>>> action) =>
        RunAsync(operation, async token =>
        {
            var results = await action(token);
            var passed = results.Count(value => value.Value.Succeeded);
            StatusText = $"{operation}: {passed}/{results.Count} equipment passed.";
            OnPropertyChanged(nameof(MultiEquipmentSummary));
        });
    private Task SendSelectedEquipmentAsync()
    {
        var context = RequireSelectedEquipment();
        var item = BuildMessageItem();
        return RunAsync($"Send to {context.EquipmentId}", async token =>
        {
            if (ReplyExpected) await _multiEquipmentHost.SendPrimaryAsync(context.EquipmentId, Stream, Function, item, token);
            else await _multiEquipmentHost.SendAsync(context.EquipmentId, Stream, Function, item, token);
        });
    }
    private Task BroadcastEquipmentAsync()
    {
        var item = BuildMessageItem();
        if (!ReplyExpected)
            return RunMultiResultAsync("Broadcast", token => _multiEquipmentHost.BroadcastSendAsync(Stream, Function, item, token));
        return RunMultiResultAsync("Broadcast", token => _multiEquipmentHost.BroadcastAsync(Stream, Function, item, token));
    }
    private Task RunSelectedEquipmentScenarioAsync()
    {
        var context = RequireSelectedEquipment();
        return RunAsync($"Scenario for {context.EquipmentId}", async token =>
        {
            await _multiEquipmentHost.LinktestAsync(context.EquipmentId, token);
            await _multiEquipmentHost.SendPrimaryAsync(context.EquipmentId, 1, 1, null, token);
        });
    }
    private Task RunAllEquipmentScenariosAsync() => RunAsync("Configured equipment scenarios", async token =>
    {
        var linktests = await _multiEquipmentHost.LinktestAllAsync(token);
        var messages = await _multiEquipmentHost.BroadcastAsync(1, 1, null, token);
        var linktestPassed = linktests.Count(value => value.Value.Succeeded);
        var messagePassed = messages.Count(value => value.Value.Succeeded);
        StatusText = $"Configured scenarios: Linktest {linktestPassed}/{linktests.Count}, S1F1/S1F2 {messagePassed}/{messages.Count}.";
    });
    private Task RunMultiEquipmentSelfTestAsync() => RunAsync("Synthetic 1/2/10/50 self-test", async token =>
    {
        _lastMultiEquipmentSummary = await _multiEquipmentScenarios.RunAsync(100, token);
        OnPropertyChanged(nameof(MultiEquipmentTestResult));
        StatusText = $"Multi-equipment self-test {_lastMultiEquipmentSummary.Result}: " +
            $"{_lastMultiEquipmentSummary.MessagesPassed:N0}/{_lastMultiEquipmentSummary.MessagesRequested:N0}.";
    });
    private Task ImportEquipmentConfigurationAsync()
    {
        var path = _fileDialog.OpenMultiEquipmentConfiguration();
        if (path is null) return Task.CompletedTask;
        return RunAsync("Import configuration", async token =>
        {
            await _multiEquipmentHost.ImportConfigurationAsync(path, token);
            SelectedEquipment = null;
            RefreshEquipmentCollection();
        });
    }
    private Task ExportEquipmentConfigurationAsync()
    {
        var suggested = Path.Combine(ExportDirectory, "multi-equipment.json");
        var path = _fileDialog.SaveMultiEquipmentConfiguration(suggested);
        if (path is null) return Task.CompletedTask;
        return RunAsync("Export configuration", token => _multiEquipmentHost.ExportConfigurationAsync(path, token));
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
    private Task ExportAsync() => RunAsync("Export evidence", async token =>
    {
        var paths = await _export.ExportAsync(ExportDirectory, Scenarios, Logs, token);
        StatusText = $"Exported: {Path.GetFileName(paths.JsonPath)}, {Path.GetFileName(paths.MarkdownPath)}";
    });
    private async Task RunAsync(string operation, Func<CancellationToken, Task> action)
    {
        var gateHeld = false;
        CancellationTokenSource? operationCancellation = null;
        var runningStatus = $"{operation} running...";
        try
        {
            await _operationGate.WaitAsync(_lifetimeCancellation.Token);
            gateHeld = true;
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            Interlocked.Exchange(ref _operationRunning, 1);
            RaiseAllCommandStates();
            operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCancellation.Token);
            lock (_operationCancellationGate) _operationCancellation = operationCancellation;
            StatusText = runningStatus;
            await action(operationCancellation.Token);
            CurrentError = "None";
            if (StatusText == runningStatus) StatusText = $"{operation} completed.";
        }
        catch (OperationCanceledException) { StatusText = $"{operation} cancelled."; }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0) { }
        finally
        {
            lock (_operationCancellationGate)
                if (ReferenceEquals(_operationCancellation, operationCancellation)) _operationCancellation = null;
            operationCancellation?.Dispose();
            if (gateHeld)
            {
                Interlocked.Exchange(ref _operationRunning, 0);
                _operationGate.Release();
            }
            RefreshStates();
        }
    }

    private void CancelCurrentOperation()
    {
        lock (_operationCancellationGate)
        {
            try { _operationCancellation?.Cancel(); }
            catch (ObjectDisposedException) { }
        }
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
        RaiseAllCommandStates();
        OnPropertyChanged(nameof(MultiEquipmentSummary));
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
    private SecsItem? BuildMessageItem() => RootItems.Count switch
    {
        0 => null,
        1 => RootItems[0].ToSecsItem(),
        _ => new SecsListItem(RootItems.Select(value => value.ToSecsItem()).ToArray())
    };
    private EquipmentConnectionContext RequireSelectedEquipment() => SelectedEquipment as EquipmentConnectionContext
        ?? throw new InvalidOperationException("Select an equipment row first.");
    private void RefreshEquipmentCollection()
    {
        var current = _multiEquipmentHost.Connections.ToHashSet();
        foreach (var removed in _observedEquipment.Where(value => !current.Contains(value)).ToArray())
        {
            removed.PropertyChanged -= OnEquipmentPropertyChanged;
            _observedEquipment.Remove(removed);
        }
        foreach (var added in current.Where(value => !_observedEquipment.Contains(value)))
        {
            added.PropertyChanged += OnEquipmentPropertyChanged;
            _observedEquipment.Add(added);
        }
        var selectedFilter = EquipmentLogFilter;
        var equipmentIds = _multiEquipmentHost.Connections.Select(value => value.EquipmentId)
            .Concat(_log.Entries.Select(value => value.EquipmentId))
            .Where(value => !string.IsNullOrWhiteSpace(value) && value != "--")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        EquipmentFilterOptions.Clear();
        EquipmentFilterOptions.Add("All Equipment");
        foreach (var id in equipmentIds) EquipmentFilterOptions.Add(id);
        EquipmentLogFilter = EquipmentFilterOptions.Contains(selectedFilter) ? selectedFilter : "All Equipment";
        var next = _multiEquipmentHost.Connections.Count + 1;
        NewEquipmentId = $"EQ-{next:000}";
        NewEquipmentPort = 7100 + next;
        OnPropertyChanged(nameof(EquipmentConnections));
        OnPropertyChanged(nameof(MultiEquipmentSummary));
        RaiseEquipmentCommandStates();
    }

    private void OnEquipmentPropertyChanged(object? sender, PropertyChangedEventArgs e) => Dispatch(() =>
    {
        OnPropertyChanged(nameof(MultiEquipmentSummary));
        RaiseEquipmentCommandStates();
    });

    private void RaiseAllCommandStates()
    {
        ConnectCommand.RaiseCanExecuteChanged();
        DisconnectCommand.RaiseCanExecuteChanged();
        SelectCommand.RaiseCanExecuteChanged();
        LinktestCommand.RaiseCanExecuteChanged();
        SeparateCommand.RaiseCanExecuteChanged();
        ReconnectCommand.RaiseCanExecuteChanged();
        SendCommand.RaiseCanExecuteChanged();
        RunSelfTestCommand.RaiseCanExecuteChanged();
        ExportCommand.RaiseCanExecuteChanged();
        RunSelectedCommand.RaiseCanExecuteChanged();
        RunAllCommand.RaiseCanExecuteChanged();
        RunAllEquipmentScenariosCommand.RaiseCanExecuteChanged();
        RunMultiEquipmentSelfTestCommand.RaiseCanExecuteChanged();
        ImportEquipmentConfigurationCommand.RaiseCanExecuteChanged();
        ExportEquipmentConfigurationCommand.RaiseCanExecuteChanged();
        RaiseEquipmentCommandStates();
    }

    private void RaiseEquipmentCommandStates()
    {
        AddEquipmentCommand.RaiseCanExecuteChanged();
        RemoveEquipmentCommand.RaiseCanExecuteChanged();
        ConnectSelectedEquipmentCommand.RaiseCanExecuteChanged();
        DisconnectSelectedEquipmentCommand.RaiseCanExecuteChanged();
        ReconnectSelectedEquipmentCommand.RaiseCanExecuteChanged();
        ConnectAllEquipmentCommand.RaiseCanExecuteChanged();
        DisconnectAllEquipmentCommand.RaiseCanExecuteChanged();
        LinktestSelectedEquipmentCommand.RaiseCanExecuteChanged();
        LinktestAllEquipmentCommand.RaiseCanExecuteChanged();
        SendSelectedEquipmentCommand.RaiseCanExecuteChanged();
        BroadcastEquipmentCommand.RaiseCanExecuteChanged();
        RunSelectedEquipmentScenarioCommand.RaiseCanExecuteChanged();
        RunAllEquipmentScenariosCommand.RaiseCanExecuteChanged();
    }
    private void RefreshLogViews()
    {
        FilteredLogs.Refresh();
        FilteredSecs1Logs.Refresh();
        FilteredSecs2Logs.Refresh();
        FilteredDiagnosticsLogs.Refresh();
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
        var equipmentMatches = EquipmentLogFilter == "All Equipment" ||
            entry.EquipmentId.Equals(EquipmentLogFilter, StringComparison.OrdinalIgnoreCase);
        return equipmentMatches && (filter.Length == 0 || entry.Direction.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.SxFy.Contains(filter, StringComparison.OrdinalIgnoreCase) || entry.SystemBytes.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) || entry.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.EquipmentId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.ConnectionId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            entry.Endpoint.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            (entry.Message?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task;
            return;
        }
        try
        {
            _lifetimeCancellation.Cancel();
            CancelCurrentOperation();
            await _operationGate.WaitAsync();
            try
            {
                List<Exception>? failures = null;
                try { await _multiEquipmentHost.DisposeAsync(); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
                try { await _connection.DisposeAsync(); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
                if (failures is not null) throw new AggregateException("One or more harness connections failed to dispose.", failures);
            }
            finally { _operationGate.Release(); }
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            foreach (var context in _observedEquipment) context.PropertyChanged -= OnEquipmentPropertyChanged;
            _observedEquipment.Clear();
            lock (_operationCancellationGate)
            {
                _operationCancellation?.Dispose();
                _operationCancellation = null;
            }
            _lifetimeCancellation.Dispose();
            _operationGate.Dispose();
        }
    }
}
