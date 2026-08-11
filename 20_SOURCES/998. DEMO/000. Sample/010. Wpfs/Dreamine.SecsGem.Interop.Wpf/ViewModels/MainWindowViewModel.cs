using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.SecsGem.Interop.Runtime;
using Dreamine.SecsGem.Interop.Wpf.Events;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.ViewModels;

/// <summary>
/// Exposes bindable harness state and generated commands. Operational behavior is isolated in
/// <see cref="MainWindowEvent"/>.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    [DreamineEvent]
    private readonly MainWindowEvent _event;

    [DreamineProperty]
    private string _tcpState = "Disconnected";

    [DreamineProperty]
    private string _hsmsState = "NotConnected";

    [DreamineProperty]
    private string _statusText = "Ready. External simulator scenarios: Not Run.";

    [DreamineProperty]
    private string _lastActivity = "None";

    [DreamineProperty]
    private string _currentError = "None";

    [DreamineProperty]
    private string _lastError = "None";

    [DreamineProperty]
    private byte _stream = 1;

    [DreamineProperty]
    private byte _function = 1;

    [DreamineProperty]
    private bool _replyExpected = true;

    [DreamineProperty]
    private int _messageIterations = 1000;

    [DreamineProperty]
    private int _reconnectCycles = 100;

    [DreamineProperty]
    private string _exportDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "DreamineInteropResults");

    private SecsItemNodeViewModel? _selectedItem;

    private InteropScenarioResult? _selectedScenario;

    private InteropLogEntry? _selectedLog;

    [DreamineProperty]
    private string _simulatorExecutablePath = SimulatorProcessManager.DefaultExecutablePath;

    [DreamineProperty]
    private string _equipmentSidecarExecutablePath =
        Environment.GetEnvironmentVariable("DREAMINE_INTEROP_EQUIPMENT_SIDECAR") ?? string.Empty;

    [DreamineProperty]
    private string _equipmentSidecarEvidencePath =
        Environment.GetEnvironmentVariable("DREAMINE_INTEROP_EQUIPMENT_EVIDENCE") ?? string.Empty;

    [DreamineProperty]
    private int _equipmentSidecarConnectionLimit = 2;

    [DreamineProperty]
    private EquipmentSidecarState _equipmentSidecarState = EquipmentSidecarState.Stopped;

    [DreamineProperty]
    private int? _equipmentSidecarProcessId;

    [DreamineProperty]
    private bool _equipmentResponderEnabled;

    [DreamineProperty]
    private bool _isLogViewPaused;

    [DreamineProperty]
    private bool _autoScrollLogs = true;

    [DreamineProperty]
    private string _logFilterText = string.Empty;

    [DreamineProperty]
    private string _hostMode = "Single Equipment";

    [DreamineProperty]
    private int _selectedMainTabIndex;

    private object? _selectedEquipment;

    [DreamineProperty]
    private string _newEquipmentId = "EQ-001";

    [DreamineProperty]
    private string _newEquipmentHost = "127.0.0.1";

    [DreamineProperty]
    private int _newEquipmentPort = 7101;

    [DreamineProperty]
    private SecsConnectionMode _newEquipmentMode = SecsConnectionMode.Active;

    [DreamineProperty]
    private ushort _newEquipmentSessionId;

    [DreamineProperty]
    private string _equipmentLogFilter = "All Equipment";

    [DreamineProperty]
    private FactoryResultSummary _factoryResult = FactoryResultSummary.NotRun;

    [DreamineProperty]
    private string _factoryResultPath = "No Factory result imported.";

    public MainWindowViewModel(
        ConnectionManager connection,
        MessageManager messages,
        ScenarioManager scenarios,
        InteropLogManager log,
        ResultExportManager export,
        SimulatorProcessManager simulator,
        EquipmentSidecarProcessManager equipmentSidecar,
        FileDialogManager fileDialog)
    {
        _event = new MainWindowEvent(
            this,
            connection,
            messages,
            scenarios,
            log,
            export,
            simulator,
            equipmentSidecar,
            fileDialog);
    }

    public ConnectionSettings Settings { get; } = new();

    // Nullable reference properties remain explicit until the property generator preserves
    // nullable annotations in its emitted type display.
    public SecsItemNodeViewModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public InteropScenarioResult? SelectedScenario
    {
        get => _selectedScenario;
        set => SetProperty(ref _selectedScenario, value);
    }

    public InteropLogEntry? SelectedLog
    {
        get => _selectedLog;
        set => SetProperty(ref _selectedLog, value);
    }

    public object? SelectedEquipment
    {
        get => _selectedEquipment;
        set => SetProperty(ref _selectedEquipment, value);
    }

    public IReadOnlyList<SecsConnectionMode> ConnectionModes { get; } =
        [SecsConnectionMode.Active, SecsConnectionMode.Passive];

    public IReadOnlyList<SecsRole> Roles { get; } = [SecsRole.Host, SecsRole.Equipment];

    public ObservableCollection<SecsItemNodeViewModel> RootItems { get; } = [];

    public ObservableCollection<InteropScenarioResult> Scenarios => Event.Scenarios;
    public ObservableCollection<InteropLogEntry> Logs => Event.Logs;
    public ObservableCollection<InteropLogEntry> Secs1Logs => Event.Secs1Logs;
    public ObservableCollection<InteropLogEntry> Secs2Logs => Event.Secs2Logs;
    public ObservableCollection<InteropLogEntry> DiagnosticsLogs => Event.DiagnosticsLogs;
    public ICollectionView FilteredLogs => Event.FilteredLogs;
    public ICollectionView FilteredSecs1Logs => Event.FilteredSecs1Logs;
    public ICollectionView FilteredSecs2Logs => Event.FilteredSecs2Logs;
    public ICollectionView FilteredDiagnosticsLogs => Event.FilteredDiagnosticsLogs;
    public IEnumerable EquipmentConnections => Event.EquipmentConnections;

    public IReadOnlyList<string> HostModes { get; } = ["Single Equipment", "Multi Equipment Host"];

    public ObservableCollection<string> EquipmentFilterOptions { get; } = ["All Equipment"];

    public bool SimulatorInstalled => Event.IsSimulatorInstalled(SimulatorExecutablePath);
    public string SimulatorAvailability => SimulatorInstalled ? "Installed (launch is manual)" : "Not found";
    public bool EquipmentSidecarAvailable => File.Exists(EquipmentSidecarExecutablePath);

    public bool EquipmentSidecarRunning => EquipmentSidecarState is
        EquipmentSidecarState.Starting or EquipmentSidecarState.Running or EquipmentSidecarState.Stopping;

    public string EquipmentSidecarStatus => EquipmentSidecarProcessId is { } processId
        ? $"{EquipmentSidecarState} (PID {processId})"
        : EquipmentSidecarState.ToString();

    public bool AnyEquipmentResponderActive => EquipmentResponderEnabled || EquipmentSidecarRunning;

    public string EquipmentResponderState =>
        EquipmentSidecarRunning ? "EXTERNAL" : EquipmentResponderEnabled ? "BASIC ON" : "OFF";

    public string EquipmentResponderButtonText =>
        EquipmentResponderEnabled ? "✓ Equipment Responder: ON" : "Enable Equipment Responder";

    public string LogViewState => IsLogViewPaused ? "Paused" : "Live";
    public bool IsMultiEquipmentMode => HostMode == "Multi Equipment Host";
    public bool IsSingleEquipmentMode => !IsMultiEquipmentMode;
    public string MultiEquipmentSummary => Event.MultiEquipmentSummary;
    public string MultiEquipmentTestResult => Event.MultiEquipmentTestResult;

    public string FactoryEvidenceBoundaryText =>
        "Imported Factory results are Headless self-loopback evidence only. External Simulator and Real Equipment remain Not Run / Waiting for User.";

    internal void NotifyPropertyChanged(string propertyName) => OnPropertyChanged(propertyName);

    internal void RefreshAllCommandStates()
    {
        NotifyConnectCommandCanExecuteChanged();
        NotifyDisconnectCommandCanExecuteChanged();
        NotifySelectCommandCanExecuteChanged();
        NotifyLinktestCommandCanExecuteChanged();
        NotifySeparateCommandCanExecuteChanged();
        NotifyReconnectCommandCanExecuteChanged();
        NotifySendCommandCanExecuteChanged();
        NotifyRunSelfTestCommandCanExecuteChanged();
        NotifyExportCommandCanExecuteChanged();
        NotifyRunSelectedCommandCanExecuteChanged();
        NotifyRunAllCommandCanExecuteChanged();
        NotifyEnableResponderCommandCanExecuteChanged();
        NotifyStartEquipmentSidecarCommandCanExecuteChanged();
        NotifyForceStopEquipmentSidecarCommandCanExecuteChanged();
        NotifyBrowseEquipmentSidecarCommandCanExecuteChanged();
        NotifyBrowseEquipmentSidecarEvidenceCommandCanExecuteChanged();
        NotifyLaunchSimulatorCommandCanExecuteChanged();
        NotifyBrowseSimulatorCommandCanExecuteChanged();
        NotifyBrowseExportCommandCanExecuteChanged();
        NotifyCancelCommandCanExecuteChanged();
        NotifyMarkWaitingCommandCanExecuteChanged();
        NotifyResetScenariosCommandCanExecuteChanged();
        NotifyAddRootItemCommandCanExecuteChanged();
        NotifyAddChildItemCommandCanExecuteChanged();
        NotifyRemoveItemCommandCanExecuteChanged();
        NotifyClearLogCommandCanExecuteChanged();
        NotifyRunAllEquipmentScenariosCommandCanExecuteChanged();
        NotifyRunMultiEquipmentSelfTestCommandCanExecuteChanged();
        NotifyImportEquipmentConfigurationCommandCanExecuteChanged();
        NotifyExportEquipmentConfigurationCommandCanExecuteChanged();
        NotifyImportFactoryResultCommandCanExecuteChanged();
        RefreshEquipmentCommandStates();
    }

    internal void RefreshEquipmentCommandStates()
    {
        NotifyAddEquipmentCommandCanExecuteChanged();
        NotifyRemoveEquipmentCommandCanExecuteChanged();
        NotifyConnectSelectedEquipmentCommandCanExecuteChanged();
        NotifyDisconnectSelectedEquipmentCommandCanExecuteChanged();
        NotifyReconnectSelectedEquipmentCommandCanExecuteChanged();
        NotifyConnectAllEquipmentCommandCanExecuteChanged();
        NotifyDisconnectAllEquipmentCommandCanExecuteChanged();
        NotifyLinktestSelectedEquipmentCommandCanExecuteChanged();
        NotifyLinktestAllEquipmentCommandCanExecuteChanged();
        NotifySendSelectedEquipmentCommandCanExecuteChanged();
        NotifyBroadcastEquipmentCommandCanExecuteChanged();
        NotifyRunSelectedEquipmentScenarioCommandCanExecuteChanged();
        NotifyRunAllEquipmentScenariosCommandCanExecuteChanged();
    }

    public ValueTask DisposeAsync() => Event.DisposeAsync();
}
