using System.IO;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Runtime.Profiles;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

/// <summary>
/// Owns one bounded exact-wire recorder for the current Workbench session and provides
/// incremental access to finalized product-owned JSONL segments.
/// </summary>
public sealed class WireLogManager : IAsyncDisposable
{
    private readonly InteropLogManager _viewLog;
    private readonly string _rootDirectory;
    private readonly WireLogPolicy _policy;
    private readonly object _gate = new();
    private WireLogRecorder? _recorder;
    private JsonlWireLogSink? _activeSink;
    private Task _stopTask = Task.CompletedTask;
    private WireLogHealth _lastHealth = new(0, 0, 0, false, "No wire-log run has completed.");
    private IReadOnlyList<string> _finalizedSegments = [];

    /// <summary>
    /// Creates a Workbench wire logger under Documents, or LocalApplicationData when the known
    /// folder is redirected, protected, or otherwise unavailable.
    /// </summary>
    public WireLogManager(InteropLogManager viewLog)
        : this(viewLog, ResolveDefaultRootDirectory())
    {
    }

    internal WireLogManager(InteropLogManager viewLog, string rootDirectory, WireLogPolicy? policy = null)
    {
        _viewLog = viewLog ?? throw new ArgumentNullException(nameof(viewLog));
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _policy = policy ?? new WireLogPolicy();
    }

    internal WireLogHealth LastHealth
    {
        get { lock (_gate) return _lastHealth; }
    }

    internal IReadOnlyList<string> FinalizedSegments
    {
        get { lock (_gate) return _finalizedSegments; }
    }

    internal string RootDirectory => _rootDirectory;

    private static string ResolveDefaultRootDirectory() => ResolveDefaultRootDirectory(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    internal static string ResolveDefaultRootDirectory(
        string documentsDirectory,
        string localApplicationDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentsDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataDirectory);
        var documentsRoot = Path.Combine(
            documentsDirectory,
            "DreamineInteropLogs");
        try
        {
            Directory.CreateDirectory(documentsRoot);
            return documentsRoot;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Controlled Folder Access, redirected known folders, and broken sync roots can reject
            // creation below Documents. Wire capture must not prevent the HSMS listener from starting.
            var localRoot = Path.Combine(
                localApplicationDataDirectory,
                "Dreamine",
                "SecsGemInterop",
                "Logs");
            Directory.CreateDirectory(localRoot);
            return localRoot;
        }
    }

    internal HsmsWireObservationOptions CreateObservationOptions(int queueCapacity = 512) =>
        _policy.CreateObservationOptions(queueCapacity);

    internal HsmsWireObservationOptions CreateObservationOptions(
        string logPolicyId,
        int queueCapacity = 512) =>
        ResolvePolicy(logPolicyId).CreateObservationOptions(queueCapacity);

    internal void Start(IHsmsWireObservationSource source, WireLogIdentity identity)
        => Start(source, identity, _policy);

    internal void Start(
        IHsmsWireObservationSource source,
        WireLogIdentity identity,
        string logPolicyId)
        => Start(source, identity, ResolvePolicy(logPolicyId));

    private void Start(
        IHsmsWireObservationSource source,
        WireLogIdentity identity,
        WireLogPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(policy);
        identity.Validate();
        var recorderOptions = new WireLogRecorderOptions
        {
            QueueCapacity = 512,
            Policy = policy
        };
        recorderOptions.Validate();

        lock (_gate)
        {
            if (_recorder is not null || !_stopTask.IsCompleted)
                throw new InvalidOperationException("A Workbench wire-log run is already active or stopping.");
            var fileSink = new JsonlWireLogSink(new WireLogStorageOptions
            {
                RootDirectory = _rootDirectory
            });
            try
            {
                _activeSink = fileSink;
                _recorder = new WireLogRecorder(
                    source,
                    identity,
                    recorderOptions,
                    new WorkbenchWireLogSink(_viewLog, fileSink));
                _lastHealth = new WireLogHealth(0, 0, 0, false, null);
            }
            catch
            {
                _activeSink = null;
                fileSink.DisposeAsync().AsTask().GetAwaiter().GetResult();
                throw;
            }
        }
    }

    private WireLogPolicy ResolvePolicy(string logPolicyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logPolicyId);
        return logPolicyId switch
        {
            ConnectionLogPolicyIds.HeaderOnlyV1 => new WireLogPolicy(WireBodyCaptureMode.HeaderOnly),
            ConnectionLogPolicyIds.ExcludedV1 => new WireLogPolicy(WireBodyCaptureMode.Excluded),
            ConnectionLogPolicyIds.FullBodyExplicitV1 when _policy.Rules.Any(
                static rule => rule.Mode == WireBodyCaptureMode.FullBody) => _policy,
            ConnectionLogPolicyIds.FullBodyExplicitV1 => throw new InvalidOperationException(
                "Full-body logging requires an explicitly registered S/F capture policy."),
            _ => throw new ArgumentException($"Unknown wire-log policy '{logPolicyId}'.", nameof(logPolicyId))
        };
    }

    internal Task StopAsync()
    {
        lock (_gate)
        {
            if (_recorder is null) return _stopTask;
            var recorder = _recorder;
            var sink = _activeSink;
            _recorder = null;
            _activeSink = null;
            _stopTask = StopCoreAsync(recorder, sink);
            return _stopTask;
        }
    }

    internal void RecordDiagnostic(SecsDiagnosticEvent diagnostic, long connectionEpoch)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        WireLogRecorder? recorder;
        lock (_gate) recorder = _recorder;
        recorder?.TryRecordDiagnostic(diagnostic, connectionEpoch);
    }

    internal void RecordState(SecsSessionStateChangedEventArgs transition)
    {
        ArgumentNullException.ThrowIfNull(transition);
        WireLogRecorder? recorder;
        lock (_gate) recorder = _recorder;
        recorder?.TryRecordState(transition);
    }

    internal async Task<int> OpenAsync(
        string path,
        WireLogFilter? filter,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var count = 0;
        await foreach (var record in WireLogReader.ReadAsync(path, filter, cancellationToken: cancellationToken)
                           .ConfigureAwait(false))
        {
            _viewLog.Wire(record);
            count++;
        }
        return count;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task StopCoreAsync(WireLogRecorder recorder, JsonlWireLogSink? sink)
    {
        await recorder.DisposeAsync().ConfigureAwait(false);
        lock (_gate)
        {
            _lastHealth = recorder.Health;
            _finalizedSegments = sink?.FinalizedSegments.ToArray() ?? [];
        }
    }

    private sealed class WorkbenchWireLogSink(
        InteropLogManager viewLog,
        JsonlWireLogSink fileSink) : IWireLogRecordSink
    {
        public async ValueTask AppendAsync(WireLogRecord record, CancellationToken cancellationToken)
        {
            viewLog.Wire(record);
            await fileSink.AppendAsync(record, cancellationToken).ConfigureAwait(false);
        }

        public ValueTask CompleteAsync(CancellationToken cancellationToken) =>
            fileSink.CompleteAsync(cancellationToken);

        public ValueTask DisposeAsync() => fileSink.DisposeAsync();
    }
}
