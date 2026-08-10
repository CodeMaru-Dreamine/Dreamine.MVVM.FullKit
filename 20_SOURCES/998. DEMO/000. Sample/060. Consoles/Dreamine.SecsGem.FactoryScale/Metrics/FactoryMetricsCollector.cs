using System.Diagnostics;
using Dreamine.SecsGem.FactoryScale.Export;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.FactoryScale.Runtime;
using Dreamine.SecsGem.FactoryScale.Simulation;

namespace Dreamine.SecsGem.FactoryScale.Metrics;

/// <summary>
/// Collects FactoryScale-owned counters together with actual .NET process/GC
/// measurements. All hot-path record methods are lock-free; only the low-rate
/// snapshot/rate calculation is serialized.
/// </summary>
internal sealed class FactoryMetricsCollector
{
    private readonly IProcessSocketMetricsProvider _socketMetrics;
    private readonly FixedMemoryLatencyHistogram _responseLatency = new();
    private readonly object _snapshotGate = new();
    private readonly long _startedTimestamp = Stopwatch.GetTimestamp();
    private readonly long _allocatedAtStart = GC.GetTotalAllocatedBytes(false);
    private readonly int _gen0AtStart = GC.CollectionCount(0);
    private readonly int _gen1AtStart = GC.CollectionCount(1);
    private readonly int _gen2AtStart = GC.CollectionCount(2);
    private readonly long _threadPoolCompletedAtStart = ThreadPool.CompletedWorkItemCount;
    private EquipmentCounts _equipment = new(0, 0, 0, 0, 0);
    private long _requests;
    private long _responses;
    private long _timeouts;
    private long _reconnectAttempts;
    private long _reconnectSucceeded;
    private long _failures;
    private long _correlationErrors;
    private long _bytesSent;
    private long _bytesReceived;
    private int _pendingTransactions;
    private int _peakPendingTransactions;
    private int _pendingControlTransactions;
    private int _peakPendingControlTransactions;
    private int _trackedOperations;
    private int _peakTrackedOperations;
    private int _reconnectOperations;
    private int _peakReconnectOperations;
    private int _trackedSessions;
    private int _trackedListeners;
    private long _lastSnapshotTimestamp;
    private long _lastRequests;
    private long _lastResponses;
    private long _lastBytes;

    internal FactoryMetricsCollector(IProcessSocketMetricsProvider? socketMetrics = null)
    {
        _socketMetrics = socketMetrics ?? UnavailableProcessSocketMetricsProvider.Instance;
        StartedAtUtc = DateTimeOffset.UtcNow;
        _lastSnapshotTimestamp = _startedTimestamp;
    }

    internal DateTimeOffset StartedAtUtc { get; }
    internal FixedMemoryLatencyHistogram ResponseLatency => _responseLatency;
    internal long RequestCount => Interlocked.Read(ref _requests);
    internal long ResponseCount => Interlocked.Read(ref _responses);

    internal void UpdateEquipmentCounts(
        int requested,
        int connected,
        int selected,
        int failed,
        int reconnecting)
    {
        if (requested < 0) throw new ArgumentOutOfRangeException(nameof(requested));
        if (connected < 0 || connected > requested) throw new ArgumentOutOfRangeException(nameof(connected));
        if (selected < 0 || selected > connected) throw new ArgumentOutOfRangeException(nameof(selected));
        if (failed < 0 || failed > requested) throw new ArgumentOutOfRangeException(nameof(failed));
        if (reconnecting < 0 || reconnecting > requested) throw new ArgumentOutOfRangeException(nameof(reconnecting));
        Volatile.Write(ref _equipment, new(requested, connected, selected, failed, reconnecting));
    }

    internal void IncrementRequest() => Interlocked.Increment(ref _requests);
    internal void RecordRequest() => IncrementRequest();

    internal void IncrementResponse() => Interlocked.Increment(ref _responses);

    internal void RecordLatency(TimeSpan latency) => _responseLatency.Record(latency);

    internal void RecordResponse(TimeSpan latency)
    {
        RecordLatency(latency);
        IncrementResponse();
    }

    internal void IncrementTimeout() => Interlocked.Increment(ref _timeouts);
    internal void RecordTimeout() => IncrementTimeout();
    internal void IncrementReconnect() => Interlocked.Increment(ref _reconnectAttempts);
    internal void RecordReconnectAttempt() => IncrementReconnect();
    internal void IncrementReconnectSucceeded() => Interlocked.Increment(ref _reconnectSucceeded);
    internal void RecordReconnectSucceeded() => IncrementReconnectSucceeded();
    internal void IncrementFailure() => Interlocked.Increment(ref _failures);
    internal void IncrementCorrelationError() => Interlocked.Increment(ref _correlationErrors);
    internal void RecordCorrelationError() => IncrementCorrelationError();

    internal void AddBytesSent(long bytes)
    {
        if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes));
        Interlocked.Add(ref _bytesSent, bytes);
    }

    internal void RecordBytesSent(long bytes) => AddBytesSent(bytes);

    internal void AddBytesReceived(long bytes)
    {
        if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes));
        Interlocked.Add(ref _bytesReceived, bytes);
    }

    internal void RecordBytesReceived(long bytes) => AddBytesReceived(bytes);

    internal FactoryMetricLease TrackPendingTransaction() => Acquire(TrackedCounter.PendingTransaction);
    internal FactoryMetricLease TrackPendingControlTransaction() => Acquire(TrackedCounter.PendingControlTransaction);
    internal FactoryMetricLease TrackOperation() => Acquire(TrackedCounter.Operation);
    internal FactoryMetricLease TrackReconnectOperation() => Acquire(TrackedCounter.ReconnectOperation);
    internal FactoryMetricLease TrackSession() => Acquire(TrackedCounter.Session);
    internal FactoryMetricLease TrackListener() => Acquire(TrackedCounter.Listener);

    internal FactoryMetricSnapshot CaptureSnapshot(
        IEnumerable<FactoryQueueMetricSnapshot>? queueMetrics = null)
        => CaptureSnapshotCore(null, queueMetrics?.ToArray() ?? Array.Empty<FactoryQueueMetricSnapshot>());

    internal FactoryMetricSnapshot CaptureSnapshot(FactoryRuntimeMetricInput runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ValidateRuntime(runtime);
        return CaptureSnapshotCore(runtime, runtime.Queues);
    }

    private FactoryMetricSnapshot CaptureSnapshotCore(
        FactoryRuntimeMetricInput? runtime,
        IReadOnlyList<FactoryQueueMetricSnapshot> queues)
    {
        var nowTimestamp = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_startedTimestamp, nowTimestamp);
        var requests = Interlocked.Read(ref _requests);
        var responses = Interlocked.Read(ref _responses);
        var sent = Interlocked.Read(ref _bytesSent);
        var received = Interlocked.Read(ref _bytesReceived);
        double requestRate;
        double responseRate;
        double byteRate;

        lock (_snapshotGate)
        {
            var interval = Stopwatch.GetElapsedTime(_lastSnapshotTimestamp, nowTimestamp).TotalSeconds;
            if (interval <= 0)
            {
                requestRate = responseRate = byteRate = 0;
            }
            else
            {
                requestRate = Math.Max(0, requests - _lastRequests) / interval;
                responseRate = Math.Max(0, responses - _lastResponses) / interval;
                byteRate = Math.Max(0, sent + received - _lastBytes) / interval;
            }

            _lastSnapshotTimestamp = nowTimestamp;
            _lastRequests = requests;
            _lastResponses = responses;
            _lastBytes = sent + received;
        }

        var equipment = Volatile.Read(ref _equipment);
        var requestedEquipment = runtime?.RequestedEquipment ?? equipment.Requested;
        var connectedEquipment = runtime?.ConnectedEquipment ?? equipment.Connected;
        var selectedEquipment = runtime?.SelectedEquipment ?? equipment.Selected;
        var failedEquipment = runtime?.FailedEquipment ?? equipment.Failed;
        var reconnectingEquipment = runtime?.ReconnectingEquipment ?? equipment.Reconnecting;
        return new FactoryMetricSnapshot(
            DateTimeOffset.UtcNow,
            elapsed,
            requestedEquipment,
            connectedEquipment,
            selectedEquipment,
            failedEquipment,
            reconnectingEquipment,
            requests,
            responses,
            Interlocked.Read(ref _timeouts),
            Interlocked.Read(ref _reconnectAttempts),
            Interlocked.Read(ref _reconnectSucceeded),
            Interlocked.Read(ref _failures),
            Interlocked.Read(ref _correlationErrors),
            sent,
            received,
            requestRate,
            responseRate,
            requestRate + responseRate,
            byteRate,
            runtime?.PendingTransactions ?? Volatile.Read(ref _pendingTransactions),
            runtime is null ? Volatile.Read(ref _peakPendingTransactions) :
                Math.Max(runtime.PendingTransactions, runtime.PeakPendingTransactions),
            runtime?.PendingControlTransactions ?? Volatile.Read(ref _pendingControlTransactions),
            runtime is null ? Volatile.Read(ref _peakPendingControlTransactions) :
                Math.Max(runtime.PendingControlTransactions, runtime.PeakPendingControlTransactions),
            runtime?.TrackedOperations ?? Volatile.Read(ref _trackedOperations),
            runtime is null ? Volatile.Read(ref _peakTrackedOperations) :
                Math.Max(runtime.TrackedOperations, runtime.PeakTrackedOperations),
            runtime?.ReconnectOperations ?? Volatile.Read(ref _reconnectOperations),
            runtime is null ? Volatile.Read(ref _peakReconnectOperations) :
                Math.Max(runtime.ReconnectOperations, runtime.PeakReconnectOperations),
            runtime?.TrackedSessions ?? Volatile.Read(ref _trackedSessions),
            runtime?.TrackedListeners ?? Volatile.Read(ref _trackedListeners),
            queues.Sum(static value => value.Dropped),
            _responseLatency.Capture(),
            CaptureProcessMetrics(runtime?.SocketMetric),
            queues)
        {
            OwnedWorkers = CaptureOwnedWorkers()
        };
    }

    private static FactoryOwnedWorkerSnapshot CaptureOwnedWorkers() => new(
        FactoryHostRuntime.LiveMessageWorkerCount,
        FactoryEquipmentFleet.LiveResponderWorkerCount,
        FactoryEquipmentEventSink.LiveDrainWorkerCount,
        FactoryResultExporter.LiveWorkerCount);

    private FactoryProcessMetricSnapshot CaptureProcessMetrics(FactorySocketMetricSnapshot? socketOverride)
    {
        var totalAllocated = GC.GetTotalAllocatedBytes(false);
        var gc = GC.GetGCMemoryInfo();
        ThreadPool.GetMaxThreads(out var maximumWorkerThreads, out var maximumIoThreads);
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableIoThreads);

        using var process = Process.GetCurrentProcess();
        process.Refresh();
        int? threadCount = TryRead(() => process.Threads.Count);
        int? handleCount = TryRead(() => process.HandleCount);
        FactorySocketMetricSnapshot sockets;
        try
        {
            sockets = socketOverride ?? _socketMetrics.Capture(process);
        }
        catch (Exception exception)
        {
            sockets = FactorySocketMetricSnapshot.Unavailable(
                $"Socket metric provider failed with {exception.GetType().Name}.");
        }

        return new FactoryProcessMetricSnapshot(
            Environment.ProcessId,
            process.WorkingSet64,
            process.PrivateMemorySize64,
            GC.GetTotalMemory(false),
            gc.HeapSizeBytes,
            gc.FragmentedBytes,
            gc.TotalCommittedBytes,
            totalAllocated,
            Math.Max(0, totalAllocated - _allocatedAtStart),
            Math.Max(0, GC.CollectionCount(0) - _gen0AtStart),
            Math.Max(0, GC.CollectionCount(1) - _gen1AtStart),
            Math.Max(0, GC.CollectionCount(2) - _gen2AtStart),
            threadCount,
            handleCount,
            ThreadPool.PendingWorkItemCount,
            Math.Max(0, ThreadPool.CompletedWorkItemCount - _threadPoolCompletedAtStart),
            Math.Max(0, maximumWorkerThreads - availableWorkerThreads),
            Math.Max(0, maximumIoThreads - availableIoThreads),
            sockets);
    }

    private static void ValidateRuntime(FactoryRuntimeMetricInput runtime)
    {
        if (runtime.RequestedEquipment < 0) throw new ArgumentOutOfRangeException(nameof(runtime.RequestedEquipment));
        if (runtime.ConnectedEquipment < 0 || runtime.ConnectedEquipment > runtime.RequestedEquipment)
            throw new ArgumentOutOfRangeException(nameof(runtime.ConnectedEquipment));
        if (runtime.SelectedEquipment < 0 || runtime.SelectedEquipment > runtime.ConnectedEquipment)
            throw new ArgumentOutOfRangeException(nameof(runtime.SelectedEquipment));
        if (runtime.FailedEquipment < 0 || runtime.FailedEquipment > runtime.RequestedEquipment)
            throw new ArgumentOutOfRangeException(nameof(runtime.FailedEquipment));
        if (runtime.ReconnectingEquipment < 0 || runtime.ReconnectingEquipment > runtime.RequestedEquipment)
            throw new ArgumentOutOfRangeException(nameof(runtime.ReconnectingEquipment));
        if (runtime.PendingTransactions < 0 || runtime.PeakPendingTransactions < 0)
            throw new ArgumentOutOfRangeException(nameof(runtime.PendingTransactions));
        if (runtime.PendingControlTransactions < 0 || runtime.PeakPendingControlTransactions < 0)
            throw new ArgumentOutOfRangeException(nameof(runtime.PendingControlTransactions));
        if (runtime.TrackedOperations < 0 || runtime.PeakTrackedOperations < 0)
            throw new ArgumentOutOfRangeException(nameof(runtime.TrackedOperations));
        if (runtime.ReconnectOperations < 0 || runtime.PeakReconnectOperations < 0)
            throw new ArgumentOutOfRangeException(nameof(runtime.ReconnectOperations));
        if (runtime.TrackedSessions < 0) throw new ArgumentOutOfRangeException(nameof(runtime.TrackedSessions));
        if (runtime.TrackedListeners < 0) throw new ArgumentOutOfRangeException(nameof(runtime.TrackedListeners));
        ArgumentNullException.ThrowIfNull(runtime.Queues);
    }

    private FactoryMetricLease Acquire(TrackedCounter counter)
    {
        switch (counter)
        {
            case TrackedCounter.PendingTransaction:
                UpdateMaximum(ref _peakPendingTransactions, Interlocked.Increment(ref _pendingTransactions));
                break;
            case TrackedCounter.PendingControlTransaction:
                UpdateMaximum(ref _peakPendingControlTransactions, Interlocked.Increment(ref _pendingControlTransactions));
                break;
            case TrackedCounter.Operation:
                UpdateMaximum(ref _peakTrackedOperations, Interlocked.Increment(ref _trackedOperations));
                break;
            case TrackedCounter.ReconnectOperation:
                UpdateMaximum(ref _peakReconnectOperations, Interlocked.Increment(ref _reconnectOperations));
                break;
            case TrackedCounter.Session:
                Interlocked.Increment(ref _trackedSessions);
                break;
            case TrackedCounter.Listener:
                Interlocked.Increment(ref _trackedListeners);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(counter));
        }
        return new FactoryMetricLease(() => Release(counter));
    }

    private void Release(TrackedCounter counter)
    {
        var remaining = counter switch
        {
            TrackedCounter.PendingTransaction => Interlocked.Decrement(ref _pendingTransactions),
            TrackedCounter.PendingControlTransaction => Interlocked.Decrement(ref _pendingControlTransactions),
            TrackedCounter.Operation => Interlocked.Decrement(ref _trackedOperations),
            TrackedCounter.ReconnectOperation => Interlocked.Decrement(ref _reconnectOperations),
            TrackedCounter.Session => Interlocked.Decrement(ref _trackedSessions),
            TrackedCounter.Listener => Interlocked.Decrement(ref _trackedListeners),
            _ => throw new ArgumentOutOfRangeException(nameof(counter))
        };
        if (remaining < 0) throw new InvalidOperationException($"Tracked metric {counter} was released more than once.");
    }

    private static int? TryRead(Func<int> read)
    {
        try { return read(); }
        catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    private static void UpdateMaximum(ref int target, int candidate)
    {
        var observed = Volatile.Read(ref target);
        while (candidate > observed)
        {
            var prior = Interlocked.CompareExchange(ref target, candidate, observed);
            if (prior == observed) return;
            observed = prior;
        }
    }

    private sealed record EquipmentCounts(int Requested, int Connected, int Selected, int Failed, int Reconnecting);

    private enum TrackedCounter
    {
        PendingTransaction,
        PendingControlTransaction,
        Operation,
        ReconnectOperation,
        Session,
        Listener
    }
}

internal sealed class FactoryMetricLease : IDisposable, IAsyncDisposable
{
    private Action? _release;

    internal FactoryMetricLease(Action release) =>
        _release = release ?? throw new ArgumentNullException(nameof(release));

    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
