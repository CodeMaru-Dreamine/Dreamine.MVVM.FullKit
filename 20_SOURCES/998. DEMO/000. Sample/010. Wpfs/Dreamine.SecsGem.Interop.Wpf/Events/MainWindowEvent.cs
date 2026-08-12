using System.Collections;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime;
using Dreamine.SecsGem.Interop.Runtime.Evidence;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Profiles;
using Dreamine.SecsGem.Interop.Runtime.Scenarios;
using Dreamine.SecsGem.Interop.Runtime.Templates;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Dreamine.SecsGem.Interop.Wpf.ViewModels;

namespace Dreamine.SecsGem.Interop.Wpf.Events;

/// <summary>
/// Owns the harness operations, manager orchestration, subscriptions, and asynchronous lifetime.
/// The paired ViewModel remains a generated binding facade.
/// </summary>
public sealed class MainWindowEvent : IAsyncDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ConnectionManager _connection;
    private readonly MessageManager _messages;
    private readonly ScenarioManager _scenarios;
    private readonly InteropLogManager _log;
    private readonly ResultExportManager _export;
    private readonly SimulatorProcessManager _simulator;
    private readonly EquipmentSidecarProcessManager _equipmentSidecar;
    private readonly FileDialogManager _fileDialog;
    private readonly WireLogManager _wireLog;
    private readonly MultiEquipmentHost _multiEquipmentHost;
    private readonly MultiEquipmentScenarioManager _multiEquipmentScenarios;
    private readonly ScenarioFileStoreV1 _scenarioFileStore = new();
    private readonly ScenarioRunnerV1 _scenarioRunner = new();
    private readonly EvidenceManifestStore _evidenceManifestStore = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _operationCancellationGate = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly TaskCompletionSource _disposeCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HashSet<EquipmentConnectionContext> _observedEquipment = [];
    private readonly INotifyCollectionChanged _equipmentConnections;
    private CancellationTokenSource? _operationCancellation;
    private MultiEquipmentSelfTestSummary? _lastMultiEquipmentSummary;
    private EquipmentSidecarConnectionProjection? _equipmentSidecarConnection;
    private ScenarioDefinitionV1? _loadedScenario;
    private MessageTemplateCatalogV1? _loadedTemplateCatalog;
    private long _equipmentSidecarConnectionEpoch;
    private int _disposed;
    private int _operationRunning;

    public MainWindowEvent(
        MainWindowViewModel viewModel,
        ConnectionManager connection,
        MessageManager messages,
        ScenarioManager scenarios,
        InteropLogManager log,
        ResultExportManager export,
        SimulatorProcessManager simulator,
        EquipmentSidecarProcessManager equipmentSidecar,
        FileDialogManager fileDialog,
        WireLogManager wireLog)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _scenarios = scenarios ?? throw new ArgumentNullException(nameof(scenarios));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _export = export ?? throw new ArgumentNullException(nameof(export));
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        _equipmentSidecar = equipmentSidecar ?? throw new ArgumentNullException(nameof(equipmentSidecar));
        _fileDialog = fileDialog ?? throw new ArgumentNullException(nameof(fileDialog));
        _wireLog = wireLog ?? throw new ArgumentNullException(nameof(wireLog));

        _multiEquipmentHost = new MultiEquipmentHost(
            log.EquipmentSink,
            collectionContext: SynchronizationContext.Current);
        _multiEquipmentScenarios = new MultiEquipmentScenarioManager(log);

        FilteredLogs = CreateLogView(_log.Entries);
        FilteredSecs1Logs = CreateLogView(_log.Secs1Entries);
        FilteredSecs2Logs = CreateLogView(_log.Secs2Entries);
        FilteredDiagnosticsLogs = CreateLogView(_log.DiagnosticsEntries);

        _equipmentConnections = (INotifyCollectionChanged)_multiEquipmentHost.Connections;
        _equipmentConnections.CollectionChanged += OnEquipmentConnectionsChanged;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _connection.StateChanged += OnConnectionStateChanged;
        _equipmentSidecar.OutputReceived += OnEquipmentSidecarOutput;
        _equipmentSidecar.StateChanged += OnEquipmentSidecarStateChanged;
        _equipmentSidecar.Exited += OnEquipmentSidecarExited;
        _log.EntryAdded += OnLogEntryAdded;
    }

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

    public bool IsOperationIdle => Volatile.Read(ref _operationRunning) == 0;
    public string MultiEquipmentSummary =>
        $"{_multiEquipmentHost.Connections.Count} configured · {_multiEquipmentHost.ActiveSessionCount} active";

    public string MultiEquipmentTestResult => _lastMultiEquipmentSummary is null
        ? "Not Run"
        : $"{_lastMultiEquipmentSummary.Result}: " +
          $"{_lastMultiEquipmentSummary.MessagesPassed:N0}/{_lastMultiEquipmentSummary.MessagesRequested:N0} replies";

    private EquipmentConnectionContext? SelectedEquipmentContext =>
        _viewModel.SelectedEquipment as EquipmentConnectionContext;

    public bool CanConnect => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.TcpState is "Disconnected" or "Faulted";

    public bool CanDisconnect => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.TcpState != "Disconnected";

    public bool CanSelect => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.HsmsState == "ConnectedNotSelected";

    public bool CanLinktest => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.HsmsState == "Selected";

    public bool CanSeparate => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.HsmsState == "Selected";

    public bool CanReconnect => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.TcpState is "Connected" or "Faulted";

    public bool CanSend => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && _viewModel.HsmsState == "Selected";

    public bool CanEnableResponder => !_viewModel.EquipmentSidecarRunning;

    public bool CanStartEquipmentSidecar => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        !_viewModel.EquipmentSidecarRunning && File.Exists(_viewModel.EquipmentSidecarExecutablePath) &&
        !string.IsNullOrWhiteSpace(_viewModel.EquipmentSidecarEvidencePath);

    public bool CanForceStopEquipmentSidecar => IsOperationIdle && _viewModel.EquipmentSidecarRunning;
    public bool CanAddEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode;
    public bool CanRemoveEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode && SelectedEquipmentContext is not null;

    public bool CanConnectSelectedEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        SelectedEquipmentContext is { IsConnected: false, TcpState: not "Connecting" and not "Listening" };

    public bool CanDisconnectSelectedEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        SelectedEquipmentContext?.Session is not null;

    public bool CanReconnectSelectedEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        SelectedEquipmentContext is not null;

    public bool CanConnectAllEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        _multiEquipmentHost.Connections.Any(value => !value.IsConnected);

    public bool CanDisconnectAllEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        _multiEquipmentHost.Connections.Any(value => value.Session is not null);

    public bool CanLinktestSelectedEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        SelectedEquipmentContext?.IsSelected == true;

    public bool CanLinktestAllEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        _multiEquipmentHost.Connections.Any(value => value.IsSelected);

    public bool CanSendSelectedEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        SelectedEquipmentContext?.IsSelected == true;

    public bool CanBroadcastEquipment => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        _multiEquipmentHost.Connections.Any(value => value.IsSelected);

    public bool CanRunSelectedEquipmentScenario => CanSendSelectedEquipment;

    public bool CanRunAllEquipmentScenarios => IsOperationIdle && _viewModel.IsMultiEquipmentMode &&
        _multiEquipmentHost.Connections.Any(value => value.IsSelected);

    public bool CanRunScenarioFile => IsOperationIdle && _viewModel.IsSingleEquipmentMode &&
        _loadedScenario is not null && _connection.MessageSession is not null;

    public bool IsSimulatorInstalled(string path) => _simulator.IsInstalled(path);

    public Task Connect() => RunAsync("Connect", async token =>
    {
        await _connection.ConnectAsync(_viewModel.Settings, token);
        if (_viewModel.Settings.Mode == SecsConnectionMode.Passive && _connection.TcpState == "Listening")
        {
            _viewModel.StatusText =
                $"Listening for Host on {_viewModel.Settings.Host}:{_viewModel.Settings.Port}.";
        }
    });
    public Task Disconnect() => RunAsync("Disconnect", token => _connection.DisconnectAsync(token));
    public Task Select() => RunAsync("Select", token => _connection.SelectAsync(token));
    public Task Linktest() => RunAsync("Linktest", token => _connection.LinktestAsync(token));
    public Task Separate() => RunAsync("Separate", token => _connection.SeparateAsync(token));
    public Task Reconnect() => RunAsync("Reconnect", token => _connection.ReconnectAsync(token));

    public Task Send() => RunAsync(
            $"S{_viewModel.Stream}F{_viewModel.Function}",
            token =>
            {
                var item = BuildMessageItem();
                return _messages.SendAsync(
                    _viewModel.Stream,
                    _viewModel.Function,
                    _viewModel.ReplyExpected,
                    item,
                    token);
            });

    public void AddEquipment()
    {
        try
        {
            var context = _multiEquipmentHost.Add(new EquipmentConnectionDefinition
            {
                EquipmentId = _viewModel.NewEquipmentId.Trim(),
                Host = _viewModel.NewEquipmentHost.Trim(),
                Port = _viewModel.NewEquipmentPort,
                Mode = _viewModel.NewEquipmentMode,
                SessionId = _viewModel.NewEquipmentSessionId
            });
            _viewModel.SelectedEquipment = context;
            _viewModel.StatusText = $"Added {context.EquipmentId} at {context.Endpoint}.";
            RefreshEquipmentCollection();
        }
        catch (Exception exception)
        {
            _log.Error("MultiEquipment", exception);
            _viewModel.CurrentError = exception.Message;
            _viewModel.LastError = exception.Message;
            _viewModel.StatusText = exception.Message;
        }
    }

    public Task RemoveEquipment() => RunAsync("Remove selected", async token =>
        {
            var context = RequireSelectedEquipment();
            await _multiEquipmentHost.RemoveAsync(context.EquipmentId, token);
            _viewModel.SelectedEquipment = null;
            RefreshEquipmentCollection();
        });

    public Task ConnectSelectedEquipment() => RunSelectedEquipmentAsync(
        "Connect selected",
        (id, token) => _multiEquipmentHost.ConnectAsync(id, token));

    public Task DisconnectSelectedEquipment() => RunSelectedEquipmentAsync(
        "Disconnect selected",
        (id, token) => _multiEquipmentHost.DisconnectAsync(id, token));

    public Task ReconnectSelectedEquipment() => RunSelectedEquipmentAsync(
        "Reconnect selected",
        (id, token) => _multiEquipmentHost.ReconnectAsync(id, token));

    public Task ConnectAllEquipment() => RunMultiResultAsync(
        "Connect all",
        token => _multiEquipmentHost.ConnectAllAsync(token));

    public Task DisconnectAllEquipment() => RunMultiResultAsync(
        "Disconnect all",
        token => _multiEquipmentHost.DisconnectAllAsync(token));

    public Task LinktestSelectedEquipment() => RunSelectedEquipmentAsync(
        "Linktest selected",
        (id, token) => _multiEquipmentHost.LinktestAsync(id, token));

    public Task LinktestAllEquipment() => RunMultiResultAsync(
        "Linktest all",
        token => _multiEquipmentHost.LinktestAllAsync(token));

    public Task SendSelectedEquipment()
    {
        var equipmentId = SelectedEquipmentContext?.EquipmentId ?? "selected equipment";
        return RunAsync($"Send to {equipmentId}", async token =>
        {
            var context = RequireSelectedEquipment();
            var item = BuildMessageItem();
            if (_viewModel.ReplyExpected)
            {
                await _multiEquipmentHost.SendPrimaryAsync(
                    context.EquipmentId,
                    _viewModel.Stream,
                    _viewModel.Function,
                    item,
                    token);
            }
            else
            {
                await _multiEquipmentHost.SendAsync(
                    context.EquipmentId,
                    _viewModel.Stream,
                    _viewModel.Function,
                    item,
                    token);
            }
        });
    }

    public Task BroadcastEquipment()
    {
        return !_viewModel.ReplyExpected
            ? RunMultiResultAsync(
                "Broadcast",
                token =>
                {
                    var item = BuildMessageItem();
                    return _multiEquipmentHost.BroadcastSendAsync(
                        _viewModel.Stream,
                        _viewModel.Function,
                        item,
                        token);
                })
            : RunMultiResultAsync(
                "Broadcast",
                token =>
                {
                    var item = BuildMessageItem();
                    return _multiEquipmentHost.BroadcastAsync(
                        _viewModel.Stream,
                        _viewModel.Function,
                        item,
                        token);
                });
    }

    public Task RunSelectedEquipmentScenario()
    {
        var equipmentId = SelectedEquipmentContext?.EquipmentId ?? "selected equipment";
        return RunAsync($"Scenario for {equipmentId}", async token =>
        {
            var context = RequireSelectedEquipment();
            await _multiEquipmentHost.LinktestAsync(context.EquipmentId, token);
            await _multiEquipmentHost.SendPrimaryAsync(context.EquipmentId, 1, 1, null, token);
        });
    }

    public Task RunAllEquipmentScenarios() => RunAsync("Configured equipment scenarios", async token =>
    {
        var linktests = await _multiEquipmentHost.LinktestAllAsync(token);
        var messages = await _multiEquipmentHost.BroadcastAsync(1, 1, null, token);
        var linktestPassed = linktests.Count(value => value.Value.Succeeded);
        var messagePassed = messages.Count(value => value.Value.Succeeded);
        _viewModel.StatusText =
            $"Configured scenarios: Linktest {linktestPassed}/{linktests.Count}, " +
            $"S1F1/S1F2 {messagePassed}/{messages.Count}.";
    });

    public Task RunMultiEquipmentSelfTest() => RunAsync("Synthetic 1/2/10/50 self-test", async token =>
    {
        _lastMultiEquipmentSummary = await _multiEquipmentScenarios.RunAsync(100, token);
        _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.MultiEquipmentTestResult));
        _viewModel.StatusText =
            $"Multi-equipment self-test {_lastMultiEquipmentSummary.Result}: " +
            $"{_lastMultiEquipmentSummary.MessagesPassed:N0}/{_lastMultiEquipmentSummary.MessagesRequested:N0}.";
    });

    public Task ImportEquipmentConfiguration() => RunAsync(
        "Import configuration",
        async token =>
        {
            var path = _fileDialog.OpenMultiEquipmentConfiguration();
            if (path is null)
            {
                _viewModel.StatusText = "Import configuration cancelled.";
                return;
            }

            await _multiEquipmentHost.ImportConfigurationAsync(path, token);
            _viewModel.SelectedEquipment = null;
            RefreshEquipmentCollection();
        });

    public Task ExportEquipmentConfiguration() => RunAsync(
        "Export configuration",
        async token =>
        {
            var suggested = Path.Combine(_viewModel.ExportDirectory, "multi-equipment.json");
            var path = _fileDialog.SaveMultiEquipmentConfiguration(suggested);
            if (path is null)
            {
                _viewModel.StatusText = "Export configuration cancelled.";
                return;
            }

            await _multiEquipmentHost.ExportConfigurationAsync(path, token);
        });

    public Task ImportFactoryResult() => RunAsync(
        "Import Factory result",
        async token =>
        {
            var path = _fileDialog.OpenFactoryResult();
            if (path is null)
            {
                _viewModel.StatusText = "Import Factory result cancelled.";
                return;
            }

            var result = await FactoryResultJsonParser.ParseAsync(path, token);
            _viewModel.FactoryResult = result;
            _viewModel.FactoryResultPath = Path.GetFullPath(path);
            _viewModel.StatusText = $"Imported Factory result: {result.Scenario} — {result.Status}.";
        });

    public Task OpenWireLog() => RunAsync("Open exact HSMS wire log", async token =>
    {
        var selected = _fileDialog.OpenWireLog(_viewModel.WireLogFilePath);
        if (selected is null)
        {
            _viewModel.WireLogOpenStatus = "Open exact HSMS wire log cancelled.";
            return;
        }
        await OpenWireLogAsync(selected, token);
    });

    internal async Task OpenWireLogAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var filter = CreateWireLogFilter(
            _viewModel.WireLogDirectionFilter,
            _viewModel.WireLogStreamFilter,
            _viewModel.WireLogFunctionFilter,
            _viewModel.WireLogSystemBytesFilter,
            _viewModel.WireLogConnectionFilter,
            _viewModel.WireLogFromUtcFilter,
            _viewModel.WireLogToUtcFilter,
            _viewModel.WireLogErrorsOnly);
        var fullPath = Path.GetFullPath(path);
        var count = await _wireLog.OpenAsync(fullPath, filter, cancellationToken);
        _viewModel.WireLogFilePath = fullPath;
        _viewModel.WireLogOpenStatus =
            $"Loaded {count:N0} exact HSMS wire record(s) incrementally. Semantic SECS-II events remain separate.";
        _viewModel.StatusText = _viewModel.WireLogOpenStatus;
    }

    public Task LoadScenarioFile() => RunAsync("Load Scenario v1", async token =>
    {
        var selected = _fileDialog.OpenScenarioFile(_viewModel.ScenarioFilePath);
        if (selected is null)
        {
            _viewModel.ScenarioFileStatus = "Load Scenario v1 cancelled.";
            return;
        }
        await LoadScenarioFileAsync(selected, token);
    });

    internal async Task LoadScenarioFileAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var scenario = await _scenarioFileStore.LoadAsync(path, cancellationToken);
        _loadedScenario = scenario;
        _viewModel.ScenarioFilePath = Path.GetFullPath(path);
        _viewModel.ScenarioFileStatus =
            $"Loaded Scenario v1 '{scenario.Name}' ({scenario.Steps.Count:N0} defined step(s)).";
        _viewModel.ScenarioRunStatus = "Not Run";
        _viewModel.ScenarioRunDetails =
            "Validated and ready for the current provider-neutral single-session connection.";
        _viewModel.StatusText = _viewModel.ScenarioFileStatus;
        _viewModel.RefreshAllCommandStates();
    }

    public Task RunScenarioFile() => RunAsync("Selected Scenario v1", async token =>
    {
        var scenario = _loadedScenario ??
            throw new InvalidOperationException("Load a validated Scenario v1 file first.");
        var session = _connection.RequireMessageSession();
        var result = await _scenarioRunner.RunAsync(scenario, session, token);
        ProjectScenarioResult(result);
    });

    public Task LoadConnectionProfile() => RunAsync("Load Connection Profile v1", async token =>
    {
        var selected = _fileDialog.OpenConnectionProfile(_viewModel.ConnectionProfilePath);
        if (selected is null)
        {
            _viewModel.ConnectionProfileStatus = "Load Connection Profile v1 cancelled.";
            return;
        }
        await LoadConnectionProfileAsync(selected, token);
    });

    internal async Task LoadConnectionProfileAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var profile = await ConnectionProfileStore.Create().LoadAsync(path, cancellationToken);
        var diff = _viewModel.Settings.ApplyProfile(profile);
        _viewModel.ConnectionProfilePath = Path.GetFullPath(path);
        var immediate = diff.ImmediateChanges.Count == 0
            ? "none"
            : string.Join(", ", diff.ImmediateChanges);
        var recreate = diff.RecreateRequiredChanges.Count == 0
            ? "none"
            : string.Join(", ", diff.RecreateRequiredChanges);
        _viewModel.ConnectionProfileStatus = diff.RequiresSessionRecreation
            ? $"Recreate Required — immutable changes: {recreate}; immediate changes: {immediate}. " +
              "The current session was not mutated; disconnect and reconnect to apply the profile."
            : $"Applied without session recreation — immediate changes: {immediate}.";
        _viewModel.StatusText = _viewModel.ConnectionProfileStatus;
        _viewModel.RefreshAllCommandStates();
    }

    public Task LoadMessageTemplateCatalog() => RunAsync("Load Message Template Catalog v1", async token =>
    {
        var selected = _fileDialog.OpenMessageTemplateCatalog(_viewModel.MessageTemplateCatalogPath);
        if (selected is null)
        {
            _viewModel.MessageTemplateCatalogStatus =
                "Load Message Template Catalog v1 cancelled.";
            return;
        }
        await LoadMessageTemplateCatalogAsync(selected, token);
    });

    internal async Task LoadMessageTemplateCatalogAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var catalog = await MessageTemplateCatalogStore.Create().LoadAsync(path, cancellationToken);
        catalog.ValidateForSend();
        _loadedTemplateCatalog = catalog;
        _viewModel.LoadedTemplateNames.Clear();
        foreach (var template in catalog.Templates)
            _viewModel.LoadedTemplateNames.Add(
                $"{template.Name} — S{template.Stream}F{template.Function}" +
                (template.WaitBit ? " W" : string.Empty));
        _viewModel.LoadedTemplateCount = catalog.Templates.Count;
        _viewModel.MessageTemplateCatalogPath = Path.GetFullPath(path);
        _viewModel.MessageTemplateCatalogStatus =
            $"Loaded and send-validated {catalog.Templates.Count:N0} template(s). " +
            "No manual reply is available without an actual pending inbound W-bit Primary context.";
        _viewModel.PendingReplyStatus =
            "No pending inbound W-bit Primary. Manual replies are never inferred.";
        _viewModel.StatusText = _viewModel.MessageTemplateCatalogStatus;
    }

    public Task LoadEvidenceManifest() => RunAsync("Load public Evidence manifest", async token =>
    {
        var selected = _fileDialog.OpenEvidenceManifest(_viewModel.EvidenceManifestPath);
        if (selected is null)
        {
            _viewModel.EvidenceManifestStatus = "Load public Evidence manifest cancelled.";
            return;
        }
        await LoadEvidenceManifestAsync(selected, token);
    });

    internal async Task LoadEvidenceManifestAsync(
        string path,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var manifest = await _evidenceManifestStore.LoadAsync(path, cancellationToken);
        var eligibility = manifest.EvaluateExternalEligibility();
        _viewModel.EvidenceManifestPath = Path.GetFullPath(path);
        _viewModel.EvidenceReviewState = manifest.ReviewState switch
        {
            EvidenceReviewState.EvidenceRecorded => "Evidence Recorded",
            EvidenceReviewState.ManualReview => "Manual Review",
            EvidenceReviewState.Verified => "Verified",
            _ => manifest.ReviewState.ToString()
        };
        _viewModel.EvidenceEligibilityStatus = eligibility.EligibleForExternalPassReview
            ? "Eligible for external PASS review — a reviewer must still assign the result."
            : "Not eligible for external PASS review.";
        _viewModel.EvidenceEligibilityReasons.Clear();
        foreach (var reason in eligibility.Reasons)
            _viewModel.EvidenceEligibilityReasons.Add(reason);
        _viewModel.EvidenceManifestStatus =
            $"Loaded public manifest '{manifest.RunId}' · {_viewModel.EvidenceReviewState}. " +
            "Manifest attachment alone never assigns Passed.";
        _viewModel.StatusText = _viewModel.EvidenceManifestStatus;
    }

    public Task RunSelfTest() => RunAsync("Self loopback", async token =>
    {
        var summary = await _scenarios.RunSelfLoopbackAsync(
            _viewModel.MessageIterations,
            _viewModel.ReconnectCycles,
            token);
        _viewModel.StatusText =
            $"Self loopback {summary.Result}: {summary.Passed:N0}/{summary.Requested:N0}, " +
            $"P95 {summary.P95LatencyMs:N2} ms.";
    });

    public Task RunAll() => RunSelfTest();

    public Task RunSelected()
    {
        if (_viewModel.SelectedScenario is null)
        {
            _viewModel.StatusText = "Select a scenario first.";
            return Task.CompletedTask;
        }

        if (_viewModel.SelectedScenario.External)
        {
            _viewModel.SelectedScenario.Status = InteropScenarioStatus.WaitingForUser;
            _viewModel.SelectedScenario.Detail =
                "Waiting for User: complete the documented simulator action.";
            _viewModel.StatusText = _viewModel.SelectedScenario.Detail;
            return Task.CompletedTask;
        }

        return RunSelfTest();
    }

    public Task StartEquipmentSidecar() => RunAsync("Start external equipment responder", async token =>
    {
        try
        {
            _viewModel.Settings.Mode = SecsConnectionMode.Passive;
            _viewModel.Settings.Role = SecsRole.Equipment;
            if (_connection.Session is not null)
            {
                await _connection.DisconnectAsync(token).ConfigureAwait(false);
            }

            _messages.DisableEquipmentResponder();
            Dispatch(() => _viewModel.EquipmentResponderEnabled = false);

            var options = new EquipmentSidecarStartOptions
            {
                ExecutablePath = _viewModel.EquipmentSidecarExecutablePath,
                BindAddress = _viewModel.Settings.Host,
                Port = _viewModel.Settings.Port,
                SessionId = _viewModel.Settings.SessionId,
                ConnectionLimit = _viewModel.EquipmentSidecarConnectionLimit,
                RequestHostTime = true,
                EvidencePath = _viewModel.EquipmentSidecarEvidencePath,
                TelemetryStdout = true
            };
            Interlocked.Exchange(ref _equipmentSidecarConnectionEpoch, 0);
            _equipmentSidecarConnection = null;
            await _equipmentSidecar.StartAsync(options, token).ConfigureAwait(false);
            Dispatch(() =>
            {
                _viewModel.EquipmentSidecarState = _equipmentSidecar.State;
                _viewModel.EquipmentSidecarProcessId = _equipmentSidecar.ProcessId;
                _scenarios.MarkExternalWaiting();
                _viewModel.StatusText =
                    "External equipment responder is running. Simulator evidence remains Waiting for User.";
            });
        }
        catch (Exception exception)
        {
            var identity = CreateEquipmentSidecarIdentity();
            _log.SidecarOutput(
                DateTimeOffset.Now,
                error: true,
                $"Sidecar start failed: {exception.GetType().Name}.",
                identity);
            Dispatch(() => _viewModel.StatusText =
                $"External equipment responder failed to start: {exception.Message}");
        }
    });

    public Task ForceStopEquipmentSidecar() => RunAsync(
        "Force stop external equipment responder",
        async token =>
        {
            var stopped = await _equipmentSidecar.ForceStopAsync(token).ConfigureAwait(false);
            Dispatch(() => _viewModel.StatusText = stopped
                ? "External responder was force-stopped. Evidence may be incomplete; this run cannot pass."
                : "External responder was not running.");
        });

    public void BrowseEquipmentSidecar()
    {
        var selected = _fileDialog.BrowseEquipmentSidecar(_viewModel.EquipmentSidecarExecutablePath);
        if (selected is not null)
        {
            _viewModel.EquipmentSidecarExecutablePath = selected;
        }
    }

    public void BrowseEquipmentSidecarEvidence()
    {
        var selected = _fileDialog.SavePrivateEquipmentEvidence(_viewModel.EquipmentSidecarEvidencePath);
        if (selected is not null)
        {
            _viewModel.EquipmentSidecarEvidencePath = selected;
        }
    }

    public Task Export() => RunAsync("Export evidence", async token =>
    {
        var paths = await _export.ExportAsync(
            _viewModel.ExportDirectory,
            Scenarios,
            Logs,
            token);
        _viewModel.StatusText =
            $"Exported: {Path.GetFileName(paths.JsonPath)}, {Path.GetFileName(paths.MarkdownPath)}";
    });

    public void Cancel()
    {
        lock (_operationCancellationGate)
        {
            try
            {
                _operationCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    public void EnableResponder()
    {
        try
        {
            _messages.EnableEquipmentResponder(_viewModel.SelectedEquipmentResponderProfile);
            _viewModel.EquipmentResponderEnabled = true;
            _viewModel.StatusText = _viewModel.SelectedEquipmentResponderProfile ==
                EquipmentResponderProfileKind.E30DerivedSubsetDemo
                ? "Public generic E30-derived subset Demo profile enabled; this is not a conformance claim."
                : "Educational Demo-only fallback enabled; this is not a GEM profile.";
        }
        catch (Exception exception)
        {
            _log.Error("Responder", exception);
            _viewModel.StatusText = exception.Message;
        }
    }

    public void LaunchSimulator()
    {
        try
        {
            _simulator.Launch(_viewModel.SimulatorExecutablePath);
            _scenarios.MarkExternalWaiting();
            _viewModel.StatusText = "Simulator launched. Waiting for User interaction.";
        }
        catch (Exception exception)
        {
            _log.Error("Simulator", exception);
            _viewModel.StatusText = exception.Message;
        }
    }

    public void BrowseSimulator()
    {
        var selected = _fileDialog.BrowseExecutable(_viewModel.SimulatorExecutablePath);
        if (selected is not null)
        {
            _viewModel.SimulatorExecutablePath = selected;
        }
    }

    public void BrowseExport()
    {
        var selected = _fileDialog.BrowseFolder(_viewModel.ExportDirectory);
        if (selected is not null)
        {
            _viewModel.ExportDirectory = selected;
        }
    }

    public void MarkWaiting()
    {
        _scenarios.MarkExternalWaiting();
        _viewModel.StatusText = "Waiting for User: follow the documented simulator steps.";
    }

    public void ResetScenarios()
    {
        _scenarios.Reset();
        _viewModel.StatusText = "Scenario results reset to Not Run.";
    }

    public void AddRootItem() => _viewModel.RootItems.Add(new SecsItemNodeViewModel());

    public void AddChildItem() => _viewModel.SelectedItem?.Children.Add(
        new SecsItemNodeViewModel(SecsItemEditorFormat.Ascii));

    public void RemoveItem()
    {
        if (_viewModel.SelectedItem is null)
        {
            return;
        }

        if (!_viewModel.RootItems.Remove(_viewModel.SelectedItem))
        {
            foreach (var root in _viewModel.RootItems)
            {
                if (RemoveFrom(root, _viewModel.SelectedItem))
                {
                    break;
                }
            }
        }

        _viewModel.SelectedItem = null;
    }

    public void ClearLog() => _log.Clear();

    private void ProjectScenarioResult(ScenarioRunResultV1 result)
    {
        var passed = result.Steps.Count(step => step.Status == ScenarioStepStatusV1.Passed);
        _viewModel.ScenarioRunStatus = result.Status.ToString();
        var error = string.IsNullOrWhiteSpace(result.ErrorMessage)
            ? string.Empty
            : $" Error={Truncate(result.ErrorMessage, 1_024)}";
        _viewModel.ScenarioRunDetails =
            $"ExitCode={result.ExitCode}; Steps={passed:N0}/{result.Steps.Count:N0}; " +
            $"DroppedInbound={result.DroppedInboundMessageCount:N0}; " +
            $"Elapsed={(result.CompletedAtUtc - result.StartedAtUtc).TotalMilliseconds:N0} ms.{error}";
        _viewModel.StatusText =
            $"Selected Scenario v1 {result.Status}: {passed:N0}/{result.Steps.Count:N0} step(s).";
    }

    private static byte? ParseOptionalByte(
        string value,
        string name,
        byte minimum,
        byte maximum)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!byte.TryParse(value.Trim(), out var parsed) || parsed < minimum || parsed > maximum)
            throw new FormatException($"{name} filter must be between {minimum} and {maximum}.");
        return parsed;
    }

    internal static WireLogFilter CreateWireLogFilter(
        string direction,
        string stream,
        string function,
        string systemBytes,
        string connectionId,
        string fromUtc,
        string toUtc,
        bool errorsOnly)
    {
        HsmsWireDirection? parsedDirection = direction.Trim() switch
        {
            "" => null,
            var value when value.Equals("Inbound", StringComparison.OrdinalIgnoreCase) =>
                HsmsWireDirection.Inbound,
            var value when value.Equals("Outbound", StringComparison.OrdinalIgnoreCase) =>
                HsmsWireDirection.Outbound,
            _ => throw new FormatException("Direction filter must be empty, Inbound, or Outbound.")
        };
        var filter = new WireLogFilter(
            Direction: parsedDirection,
            Stream: ParseOptionalByte(stream, "Stream", 1, 127),
            Function: ParseOptionalByte(function, "Function", 0, byte.MaxValue),
            SystemBytes: ParseOptionalSystemBytes(systemBytes),
            ConnectionId: string.IsNullOrWhiteSpace(connectionId) ? null : connectionId.Trim(),
            FromUtc: ParseOptionalUtc(fromUtc, "From UTC"),
            ToUtc: ParseOptionalUtc(toUtc, "To UTC"),
            ErrorsOnly: errorsOnly);
        filter.Validate();
        return filter;
    }

    private static uint? ParseOptionalSystemBytes(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        var style = System.Globalization.NumberStyles.Integer;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
            style = System.Globalization.NumberStyles.AllowHexSpecifier;
        }
        if (!uint.TryParse(text, style, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            throw new FormatException("System Bytes filter must be an unsigned decimal or 0x-prefixed hexadecimal value.");
        return parsed;
    }

    private static DateTimeOffset? ParseOptionalUtc(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!DateTimeOffset.TryParse(
                value.Trim(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var parsed) ||
            parsed.Offset != TimeSpan.Zero)
        {
            throw new FormatException(
                $"{name} filter must be an ISO-8601 timestamp with the UTC 'Z' or +00:00 offset.");
        }
        return parsed.ToUniversalTime();
    }

    private static string Truncate(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters ? value : value[..maximumCharacters] + "…";

    private Task RunSelectedEquipmentAsync(
        string operation,
        Func<string, CancellationToken, Task> action)
        => RunAsync(operation, token =>
        {
            var context = RequireSelectedEquipment();
            return action(context.EquipmentId, token);
        });

    private Task RunMultiResultAsync(
        string operation,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, EquipmentOperationResult>>> action) =>
        RunAsync(operation, async token =>
        {
            var results = await action(token);
            var passed = results.Count(value => value.Value.Succeeded);
            _viewModel.StatusText = $"{operation}: {passed}/{results.Count} equipment passed.";
            _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.MultiEquipmentSummary));
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
            _viewModel.RefreshAllCommandStates();
            operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token);
            lock (_operationCancellationGate)
            {
                _operationCancellation = operationCancellation;
            }

            _viewModel.StatusText = runningStatus;
            await action(operationCancellation.Token);
            _viewModel.CurrentError = "None";
            if (_viewModel.StatusText == runningStatus)
            {
                _viewModel.StatusText = $"{operation} completed.";
            }
        }
        catch (OperationCanceledException)
        {
            _viewModel.StatusText = $"{operation} cancelled.";
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Command", exception);
            _viewModel.StatusText = exception.Message;
            _viewModel.CurrentError = exception.Message;
            _viewModel.LastError = exception.Message;
        }
        finally
        {
            lock (_operationCancellationGate)
            {
                if (ReferenceEquals(_operationCancellation, operationCancellation))
                {
                    _operationCancellation = null;
                }
            }

            operationCancellation?.Dispose();
            if (gateHeld)
            {
                Interlocked.Exchange(ref _operationRunning, 0);
                _operationGate.Release();
            }

            RefreshStates();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.SimulatorExecutablePath):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.SimulatorInstalled));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.SimulatorAvailability));
                break;

            case nameof(MainWindowViewModel.EquipmentSidecarExecutablePath):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentSidecarAvailable));
                _viewModel.RefreshAllCommandStates();
                break;

            case nameof(MainWindowViewModel.EquipmentSidecarEvidencePath):
                _viewModel.RefreshAllCommandStates();
                break;

            case nameof(MainWindowViewModel.EquipmentSidecarState):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentSidecarRunning));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentSidecarStatus));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentResponderState));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.AnyEquipmentResponderActive));
                _viewModel.RefreshAllCommandStates();
                break;

            case nameof(MainWindowViewModel.EquipmentSidecarProcessId):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentSidecarStatus));
                break;

            case nameof(MainWindowViewModel.EquipmentResponderEnabled):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentResponderState));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentResponderButtonText));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.AnyEquipmentResponderActive));
                break;

            case nameof(MainWindowViewModel.SelectedEquipmentResponderProfile):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentResponderState));
                break;

            case nameof(MainWindowViewModel.IsLogViewPaused):
                _log.SetPaused(_viewModel.IsLogViewPaused);
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.LogViewState));
                break;

            case nameof(MainWindowViewModel.LogFilterText):
            case nameof(MainWindowViewModel.EquipmentLogFilter):
                RefreshLogViews();
                break;

            case nameof(MainWindowViewModel.HostMode):
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.IsMultiEquipmentMode));
                _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.IsSingleEquipmentMode));
                _viewModel.SelectedMainTabIndex = _viewModel.IsMultiEquipmentMode ? 1 : 0;
                _viewModel.RefreshAllCommandStates();
                break;

            case nameof(MainWindowViewModel.SelectedEquipment):
                _viewModel.RefreshEquipmentCommandStates();
                break;
        }
    }

    private void OnEquipmentConnectionsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatch(RefreshEquipmentCollection);

    private void OnConnectionStateChanged(object? sender, EventArgs e) => Dispatch(RefreshStates);

    private void OnLogEntryAdded(object? sender, InteropLogEntry entry) => Dispatch(() =>
    {
        _viewModel.LastActivity = $"{entry.Timestamp:HH:mm:ss.fff} {entry.Summary}";
        if (!string.IsNullOrWhiteSpace(entry.EquipmentId) && entry.EquipmentId != "--" &&
            !_viewModel.EquipmentFilterOptions.Contains(entry.EquipmentId))
        {
            _viewModel.EquipmentFilterOptions.Add(entry.EquipmentId);
        }

        if (entry.Level == "Error")
        {
            _viewModel.CurrentError = entry.Summary;
            _viewModel.LastError = entry.Summary;
        }
    });

    private void OnEquipmentSidecarOutput(object? sender, EquipmentSidecarOutputEventArgs e)
    {
        if (EquipmentSidecarTelemetryParser.TryParse(e.Line, out var telemetry) && telemetry is not null)
        {
            if (telemetry.ConnectionEpoch > 0)
            {
                Interlocked.Exchange(ref _equipmentSidecarConnectionEpoch, telemetry.ConnectionEpoch);
            }

            _log.SidecarTelemetry(telemetry, CreateEquipmentSidecarIdentity(telemetry));
        }
        else
        {
            var diagnostic = EquipmentSidecarDiagnosticParser.Classify(e.Stream, e.Line);
            if (diagnostic.Connection is { } connection)
            {
                _equipmentSidecarConnection = connection;
                Dispatch(RefreshStates);
            }

            _log.SidecarOutput(
                e.Timestamp,
                diagnostic.IsError,
                diagnostic.Message,
                CreateEquipmentSidecarIdentity(uncorrelatedDiagnostic: true));
        }
    }

    private EquipmentLogIdentity CreateEquipmentSidecarIdentity(
        EquipmentSidecarTelemetry? telemetry = null,
        bool uncorrelatedDiagnostic = false)
    {
        var processId = _equipmentSidecar.ProcessId;
        var epoch = telemetry?.ConnectionEpoch > 0
            ? telemetry.ConnectionEpoch
            : Interlocked.Read(ref _equipmentSidecarConnectionEpoch);
        var process = processId is { } value ? $"PID-{value}" : "PID-not-running";
        var connection = uncorrelatedDiagnostic
            ? $"{process}:diagnostic"
            : epoch > 0 ? $"{process}:C{epoch}" : $"{process}:pending";
        return new EquipmentLogIdentity(
            "External-Profile",
            connection,
            $"{_viewModel.Settings.Host}:{_viewModel.Settings.Port}",
            telemetry?.Message?.SessionId.Value ?? _viewModel.Settings.SessionId);
    }

    private void OnEquipmentSidecarStateChanged(
        object? sender,
        EquipmentSidecarStateChangedEventArgs e) => Dispatch(() =>
    {
        _viewModel.EquipmentSidecarState = e.CurrentState;
        _viewModel.EquipmentSidecarProcessId = _equipmentSidecar.ProcessId;
    });

    private void OnEquipmentSidecarExited(object? sender, EquipmentSidecarExitedEventArgs e) => Dispatch(() =>
    {
        _equipmentSidecarConnection = new EquipmentSidecarConnectionProjection(
            EquipmentSidecarTcpStatus.Disconnected,
            EquipmentSidecarHsmsStatus.NotConnected);
        _viewModel.EquipmentSidecarState = _equipmentSidecar.State;
        _viewModel.EquipmentSidecarProcessId = null;
        _viewModel.StatusText = e.ForceStopped
            ? "External equipment responder was force-stopped; evidence is incomplete and the run is not passed."
            : e.ExitCode == 0
                ? "External equipment responder completed. Review both sides before assigning Manual Passed."
                : $"External equipment responder failed with exit code {e.ExitCode}.";
        RefreshStates();
    });

    private void RefreshStates()
    {
        if (_viewModel.EquipmentSidecarRunning)
        {
            _viewModel.TcpState = (_equipmentSidecarConnection?.Tcp ??
                EquipmentSidecarTcpStatus.Listening).ToString();
            _viewModel.HsmsState = (_equipmentSidecarConnection?.Hsms ??
                EquipmentSidecarHsmsStatus.NotConnected).ToString();
        }
        else
        {
            _viewModel.TcpState = _connection.TcpState;
            _viewModel.HsmsState = _connection.HsmsState;
        }

        _viewModel.RefreshAllCommandStates();
        _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.MultiEquipmentSummary));
    }

    private static bool RemoveFrom(SecsItemNodeViewModel parent, SecsItemNodeViewModel target)
    {
        if (parent.Children.Remove(target))
        {
            return true;
        }

        return parent.Children.Any(child => RemoveFrom(child, target));
    }

    private SecsItem? BuildMessageItem() => _viewModel.RootItems.Count switch
    {
        0 => null,
        1 => _viewModel.RootItems[0].ToSecsItem(),
        _ => new SecsListItem(_viewModel.RootItems.Select(value => value.ToSecsItem()).ToArray())
    };

    private EquipmentConnectionContext RequireSelectedEquipment() =>
        SelectedEquipmentContext ?? throw new InvalidOperationException("Select an equipment row first.");

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

        var selectedFilter = _viewModel.EquipmentLogFilter;
        var equipmentIds = _multiEquipmentHost.Connections
            .Select(value => value.EquipmentId)
            .Concat(_log.Entries.Select(value => value.EquipmentId))
            .Where(value => !string.IsNullOrWhiteSpace(value) && value != "--")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _viewModel.EquipmentFilterOptions.Clear();
        _viewModel.EquipmentFilterOptions.Add("All Equipment");
        foreach (var id in equipmentIds)
        {
            _viewModel.EquipmentFilterOptions.Add(id);
        }

        _viewModel.EquipmentLogFilter = _viewModel.EquipmentFilterOptions.Contains(selectedFilter)
            ? selectedFilter
            : "All Equipment";
        var next = _multiEquipmentHost.Connections.Count + 1;
        _viewModel.NewEquipmentId = $"EQ-{next:000}";
        _viewModel.NewEquipmentPort = 7100 + next;
        _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.EquipmentConnections));
        _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.MultiEquipmentSummary));
        _viewModel.RefreshEquipmentCommandStates();
    }

    private void OnEquipmentPropertyChanged(object? sender, PropertyChangedEventArgs e) => Dispatch(() =>
    {
        _viewModel.NotifyPropertyChanged(nameof(MainWindowViewModel.MultiEquipmentSummary));
        _viewModel.RefreshEquipmentCommandStates();
    });

    private void RefreshLogViews()
    {
        FilteredLogs.Refresh();
        FilteredSecs1Logs.Refresh();
        FilteredSecs2Logs.Refresh();
        FilteredDiagnosticsLogs.Refresh();
    }

    private ICollectionView CreateLogView(ObservableCollection<InteropLogEntry> source)
    {
        var view = new ListCollectionView(source) { IsLiveFiltering = false };
        view.Filter = item => item is InteropLogEntry entry && MatchesLogFilter(entry);
        return view;
    }

    private bool MatchesLogFilter(InteropLogEntry entry)
    {
        var filter = _viewModel.LogFilterText.Trim();
        var equipmentMatches = _viewModel.EquipmentLogFilter == "All Equipment" ||
            entry.EquipmentId.Equals(
                _viewModel.EquipmentLogFilter,
                StringComparison.OrdinalIgnoreCase);
        return equipmentMatches &&
            (filter.Length == 0 ||
             entry.Direction.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.SxFy.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.SystemBytes.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.Category.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.EquipmentId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.ConnectionId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             entry.Endpoint.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
             (entry.Message?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _ = dispatcher.BeginInvoke(action);
        }
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
            Cancel();
            await _operationGate.WaitAsync();
            try
            {
                List<Exception>? failures = null;
                try
                {
                    // The event orchestrator is the production lifetime owner. Detach responder
                    // registrations before the underlying connection is disposed.
                    _messages.Dispose();
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }

                try
                {
                    await _equipmentSidecar.DisposeAsync();
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }

                try
                {
                    await _multiEquipmentHost.DisposeAsync();
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }

                try
                {
                    await _connection.DisposeAsync();
                }
                catch (Exception exception)
                {
                    (failures ??= []).Add(exception);
                }

                if (failures is not null)
                {
                    throw new AggregateException(
                        "One or more harness connections failed to dispose.",
                        failures);
                }
            }
            finally
            {
                _operationGate.Release();
            }

            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _equipmentConnections.CollectionChanged -= OnEquipmentConnectionsChanged;
            _connection.StateChanged -= OnConnectionStateChanged;
            _log.EntryAdded -= OnLogEntryAdded;
            _equipmentSidecar.OutputReceived -= OnEquipmentSidecarOutput;
            _equipmentSidecar.StateChanged -= OnEquipmentSidecarStateChanged;
            _equipmentSidecar.Exited -= OnEquipmentSidecarExited;
            foreach (var context in _observedEquipment)
            {
                context.PropertyChanged -= OnEquipmentPropertyChanged;
            }

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
