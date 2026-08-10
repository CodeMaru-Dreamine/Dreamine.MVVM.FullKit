using System.Collections.Concurrent;
using System.Diagnostics;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Abstractions.Validation;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Metrics;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Runtime;

internal sealed record FactoryHostRuntimeOptions(
    int ConnectConcurrency = 64,
    int ReconnectConcurrency = 16,
    int MessageConcurrency = 128,
    int MessageQueueCapacity = 8_192,
    int PerEquipmentPendingLimit = 16,
    int GlobalPendingLimit = 4_096,
    int DiagnosticQueueCapacity = 2_048)
{
    internal void Validate()
    {
        if (ConnectConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(ConnectConcurrency));
        if (ReconnectConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(ReconnectConcurrency));
        if (MessageConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(MessageConcurrency));
        if (MessageQueueCapacity < MessageConcurrency) throw new ArgumentOutOfRangeException(nameof(MessageQueueCapacity));
        if (PerEquipmentPendingLimit <= 0) throw new ArgumentOutOfRangeException(nameof(PerEquipmentPendingLimit));
        if (GlobalPendingLimit < MessageConcurrency) throw new ArgumentOutOfRangeException(nameof(GlobalPendingLimit));
        if (DiagnosticQueueCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(DiagnosticQueueCapacity));
    }
}

internal sealed record FactoryMessageResult(
    string EquipmentId,
    bool Succeeded,
    TimeSpan QueueDelay,
    TimeSpan ResponseLatency,
    SecsMessage? Response,
    string? Error)
{
    internal long CompletionOrdinal { get; init; }
}

internal sealed class FactoryHostRuntime : IAsyncDisposable
{
    private static int _liveMessageWorkers;
    private readonly FactoryHostRuntimeOptions _options;
    private readonly FactoryMetricsCollector _metrics;
    private readonly FactoryEquipmentEventSink _events;
    private readonly MultiEquipmentHost _host;
    private readonly BoundedWorkQueue<MessageWork> _messageQueue;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<FactoryMessageResult>> _outstandingMessages = new();
    private readonly SemaphoreSlim _globalPending;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _equipmentPending = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, EquipmentMetricState> _equipmentMetrics = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task[] _messageWorkers;
    private readonly TimeSpan _gracefulShutdownTimeout;
    private readonly TimeSpan _forcedShutdownTimeout;
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _activeMessageOperations;
    private int _peakMessageOperations;
    private int _peakPendingTransactions;
    private int _activeControlTransactions;
    private int _peakControlTransactions;
    private int _explicitReconnectOperations;
    private int _peakReconnectOperations;
    private long _completionSequence;
    private long _nextMessageWorkId;
    private int _disposed;

    internal FactoryHostRuntime(
        FactoryHostRuntimeOptions options,
        FactoryMetricsCollector? metrics = null,
        TimeSpan? gracefulShutdownTimeout = null,
        TimeSpan? forcedShutdownTimeout = null)
    {
        options.Validate();
        _gracefulShutdownTimeout = gracefulShutdownTimeout ?? TimeSpan.FromMinutes(2);
        _forcedShutdownTimeout = forcedShutdownTimeout ?? TimeSpan.FromSeconds(30);
        if (_gracefulShutdownTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(gracefulShutdownTimeout));
        if (_forcedShutdownTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(forcedShutdownTimeout));
        _options = options;
        _metrics = metrics ?? new FactoryMetricsCollector(WindowsProcessSocketMetricsProvider.Instance);
        _events = new FactoryEquipmentEventSink(_metrics, options.DiagnosticQueueCapacity);
        _host = new MultiEquipmentHost(_events, new MultiEquipmentHostOptions
        {
            ConnectConcurrency = options.ConnectConcurrency,
            MessageConcurrency = options.MessageConcurrency,
            ReconnectConcurrency = options.ReconnectConcurrency,
            ReconnectQueueCapacity = Math.Max(options.ReconnectConcurrency, options.MessageQueueCapacity)
        });
        _messageQueue = new BoundedWorkQueue<MessageWork>("host-message", options.MessageQueueCapacity,
            BoundedBusinessQueueFullPolicy.Wait);
        _globalPending = new SemaphoreSlim(options.GlobalPendingLimit, options.GlobalPendingLimit);
        _messageWorkers = Enumerable.Range(0, options.MessageConcurrency)
            .Select(_ => RunMessageWorkerAsync(_lifetime.Token)).ToArray();
    }

    internal FactoryMetricsCollector Metrics => _metrics;
    internal MultiEquipmentHost Host => _host;
    internal IReadOnlyList<EquipmentConnectionContext> Connections => _host.Connections;
    internal FactoryEquipmentEventSink EventSink => _events;
    internal int PendingTransactionCount => _host.PendingTransactionCount;
    internal int MessageQueueDepth => _messageQueue.Depth;
    internal int PeakMessageQueueDepth => _messageQueue.PeakDepth;
    internal int ActiveMessageOperationCount => Volatile.Read(ref _activeMessageOperations);
    internal int PeakMessageOperationCount => Volatile.Read(ref _peakMessageOperations);
    internal int PeakReconnectOperationCount => Volatile.Read(ref _peakReconnectOperations);
    internal int PeakPendingTransactionCount => Volatile.Read(ref _peakPendingTransactions);
    internal int OutstandingMessageCount => _outstandingMessages.Count;
    internal static int LiveMessageWorkerCount => Volatile.Read(ref _liveMessageWorkers);

    internal void AddEquipment(IEnumerable<EquipmentConnectionDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            if (definition.AutoReconnect)
                throw new ArgumentException(
                    $"Factory orchestration requires AutoReconnect=false for '{definition.EquipmentId}'; reconnects are scheduled through the single bounded Factory coordinator.",
                    nameof(definitions));
            _host.Add(definition);
            _equipmentPending.TryAdd(definition.EquipmentId,
                new SemaphoreSlim(_options.PerEquipmentPendingLimit, _options.PerEquipmentPendingLimit));
            _equipmentMetrics.TryAdd(definition.EquipmentId, new EquipmentMetricState());
        }
        UpdateEquipmentCounts();
    }

    internal async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> ConnectAllAsync(
        CancellationToken cancellationToken)
    {
        var result = await _host.ConnectAllAsync(cancellationToken).ConfigureAwait(false);
        UpdateEquipmentCounts();
        return result;
    }

    internal async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> DisconnectAllAsync(
        CancellationToken cancellationToken)
    {
        var result = await _host.DisconnectAllAsync(cancellationToken).ConfigureAwait(false);
        UpdateEquipmentCounts();
        return result;
    }

    internal async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> LinktestAllAsync(
        CancellationToken cancellationToken)
    {
        var contexts = _host.Connections.ToArray();
        return await _host.RunBoundedAsync(contexts, _options.MessageConcurrency, async (context, token) =>
        {
            var active = Interlocked.Increment(ref _activeControlTransactions);
            UpdateMaximum(ref _peakControlTransactions, active);
            using var metric = _metrics.TrackPendingControlTransaction();
            try { await context.LinktestAsync(token).ConfigureAwait(false); }
            finally { Interlocked.Decrement(ref _activeControlTransactions); }
        }, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<FactoryMessageResult> RequestAsync(
        string equipmentId,
        byte stream,
        byte function,
        SecsItem? item,
        CancellationToken cancellationToken,
        SecsSystemBytes? systemBytes = null)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (!_equipmentPending.ContainsKey(equipmentId))
            throw new KeyNotFoundException($"Equipment '{equipmentId}' is not registered.");

        var completion = new TaskCompletionSource<FactoryMessageResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workId = Interlocked.Increment(ref _nextMessageWorkId);
        if (!_outstandingMessages.TryAdd(workId, completion))
            throw new InvalidOperationException("The Factory message work identifier was duplicated.");
        var work = new MessageWork(workId, equipmentId, stream, function, item, systemBytes,
            Stopwatch.GetTimestamp(), completion, cancellationToken);
        try
        {
            if (!await _messageQueue.EnqueueAsync(work, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The bounded host message queue rejected the request.");
        }
        catch
        {
            _outstandingMessages.TryRemove(workId, out _);
            throw;
        }
        return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyList<FactoryMessageResult>> BroadcastAsync(
        byte stream,
        byte function,
        Func<EquipmentConnectionContext, SecsItem?> itemFactory,
        CancellationToken cancellationToken)
    {
        var snapshot = _host.Connections.ToArray();
        var tasks = snapshot.Select(context => RequestAsync(context.EquipmentId, stream, function,
            itemFactory(context), cancellationToken)).ToArray();
        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    internal async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> ReconnectAsync(
        IEnumerable<string> equipmentIds,
        CancellationToken cancellationToken)
    {
        var contexts = equipmentIds.Select(_host.Get).Distinct().ToArray();
        var result = await _host.RunBoundedAsync(contexts, _options.ReconnectConcurrency,
            (context, token) => TrackReconnectAsync(context.EquipmentId, context.ReconnectAsync, token), cancellationToken)
            .ConfigureAwait(false);
        foreach (var succeeded in result.Values.Where(value => value.Succeeded))
            _metrics.IncrementReconnectSucceeded();
        UpdateEquipmentCounts();
        return result;
    }

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> DisconnectAsync(
        IEnumerable<string> equipmentIds,
        CancellationToken cancellationToken)
    {
        var contexts = equipmentIds.Select(_host.Get).Distinct().ToArray();
        return _host.RunBoundedAsync(contexts, _options.ReconnectConcurrency,
            (context, token) => context.DisconnectAsync(token), cancellationToken);
    }

    internal async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> ConnectReconnectTargetsAsync(
        IEnumerable<string> equipmentIds,
        CancellationToken cancellationToken)
    {
        var contexts = equipmentIds.Select(_host.Get).Distinct().ToArray();
        var result = await _host.RunBoundedAsync(contexts, _options.ReconnectConcurrency,
            (context, token) => TrackReconnectAsync(context.EquipmentId, context.ConnectAsync, token), cancellationToken)
            .ConfigureAwait(false);
        foreach (var succeeded in result.Values.Where(value => value.Succeeded))
            _metrics.IncrementReconnectSucceeded();
        UpdateEquipmentCounts();
        return result;
    }

    internal FactoryMetricSnapshot CaptureSnapshot(
        int peerSessions,
        int peerListeners,
        params FactoryQueueMetricSnapshot[] additionalQueues)
    {
        UpdateEquipmentCounts();
        var queues = new List<FactoryQueueMetricSnapshot>
        {
            _messageQueue.CaptureMetrics(),
            _events.Queue.CaptureMetrics()
        };
        queues.AddRange(additionalQueues);
        var connected = _host.Connections.Count(value => value.IsConnected);
        var selected = _host.Connections.Count(value => value.IsSelected);
        var failed = _host.Connections.Count(value => !value.IsConnected && value.LastError != "None");
        return _metrics.CaptureSnapshot(new FactoryRuntimeMetricInput
        {
            RequestedEquipment = _host.Connections.Count,
            ConnectedEquipment = connected,
            SelectedEquipment = selected,
            FailedEquipment = failed,
            ReconnectingEquipment = _host.ActiveReconnectOperationCount + Volatile.Read(ref _explicitReconnectOperations),
            PendingTransactions = _host.PendingTransactionCount,
            PeakPendingTransactions = Math.Max(_host.PendingTransactionCount, Volatile.Read(ref _peakPendingTransactions)),
            PendingControlTransactions = Volatile.Read(ref _activeControlTransactions),
            PeakPendingControlTransactions = Volatile.Read(ref _peakControlTransactions),
            TrackedOperations = Math.Max(_host.BackgroundOperationCount,
                Volatile.Read(ref _activeMessageOperations)),
            PeakTrackedOperations = Math.Max(_host.PeakEquipmentOperationCount,
                Volatile.Read(ref _peakMessageOperations)),
            ReconnectOperations = _host.ActiveReconnectOperationCount + Volatile.Read(ref _explicitReconnectOperations),
            PeakReconnectOperations = Math.Max(_host.ActiveReconnectOperationCount,
                Volatile.Read(ref _peakReconnectOperations)),
            TrackedSessions = EquipmentConnectionContext.LiveSessionCount + peerSessions,
            TrackedListeners = peerListeners,
            Queues = queues
        });
    }

    internal IReadOnlyList<FactoryEquipmentMetricSnapshot> CaptureEquipmentMetrics() =>
        _host.Connections.OrderBy(value => value.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .Select(context => _equipmentMetrics[context.EquipmentId].Capture(context))
            .ToArray();

    private async Task RunMessageWorkerAsync(CancellationToken lifetimeToken)
    {
        Interlocked.Increment(ref _liveMessageWorkers);
        try
        {
            // Completion, rather than reader cancellation, owns queue shutdown. After a
            // forced runtime cancellation the workers still drain accepted items and
            // complete every caller-owned TCS instead of abandoning queued requests.
            await foreach (var work in _messageQueue.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (lifetimeToken.IsCancellationRequested)
                {
                    CompleteException(work, new ObjectDisposedException(
                        nameof(FactoryHostRuntime),
                        "The Factory host runtime stopped before the accepted request could run."));
                    continue;
                }
                if (work.CallerCancellation.IsCancellationRequested)
                {
                    CompleteCanceled(work, work.CallerCancellation);
                    continue;
                }

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken, work.CallerCancellation);
                var equipmentPermit = _equipmentPending[work.EquipmentId];
                var globalPermitHeld = false;
                var equipmentPermitHeld = false;
                try
                {
                    await equipmentPermit.WaitAsync(linked.Token).ConfigureAwait(false);
                    equipmentPermitHeld = true;
                    await _globalPending.WaitAsync(linked.Token).ConfigureAwait(false);
                    globalPermitHeld = true;
                }
                catch (OperationCanceledException exception)
                {
                    if (equipmentPermitHeld) equipmentPermit.Release();
                    if (globalPermitHeld) _globalPending.Release();
                    CompleteException(work, exception);
                    continue;
                }

                var pending = _options.GlobalPendingLimit - _globalPending.CurrentCount;
                UpdateMaximum(ref _peakPendingTransactions, pending);
                var active = Interlocked.Increment(ref _activeMessageOperations);
                UpdateMaximum(ref _peakMessageOperations, active);
                using var operationMetric = _metrics.TrackOperation();
                using var pendingMetric = _metrics.TrackPendingTransaction();
                var requestStarted = Stopwatch.GetTimestamp();
                _metrics.IncrementRequest();
                var equipmentMetrics = _equipmentMetrics[work.EquipmentId];
                equipmentMetrics.Request();
                try
                {
                    var response = await _host.SendPrimaryAsync(work.EquipmentId, work.Stream, work.Function,
                        work.Item, linked.Token, work.SystemBytes).ConfigureAwait(false);
                    var latency = Stopwatch.GetElapsedTime(requestStarted);
                    _metrics.RecordResponse(latency);
                    equipmentMetrics.Response(latency,
                        Stopwatch.GetElapsedTime(work.EnqueuedTimestamp, requestStarted));
                    if (work.SystemBytes is { } expected && response.SystemBytes != expected)
                    {
                        _metrics.IncrementCorrelationError();
                        throw new InvalidDataException(
                            $"Correlation mismatch for {work.EquipmentId}: expected {expected.Value:X8}, received {response.SystemBytes.Value:X8}.");
                    }
                    CompleteResult(work, new FactoryMessageResult(work.EquipmentId, true,
                        Stopwatch.GetElapsedTime(work.EnqueuedTimestamp, requestStarted), latency, response, null)
                    {
                        CompletionOrdinal = Interlocked.Increment(ref _completionSequence)
                    });
                }
                catch (OperationCanceledException exception)
                {
                    CompleteException(work, exception);
                }
                catch (SecsTransactionTimeoutException exception)
                {
                    _metrics.IncrementFailure();
                    equipmentMetrics.Timeout();
                    CompleteResult(work, new FactoryMessageResult(work.EquipmentId, false,
                        Stopwatch.GetElapsedTime(work.EnqueuedTimestamp, requestStarted), Stopwatch.GetElapsedTime(requestStarted),
                        null, exception.Message)
                    {
                        CompletionOrdinal = Interlocked.Increment(ref _completionSequence)
                    });
                }
                catch (Exception exception)
                {
                    _metrics.IncrementFailure();
                    equipmentMetrics.Failure();
                    CompleteResult(work, new FactoryMessageResult(work.EquipmentId, false,
                        Stopwatch.GetElapsedTime(work.EnqueuedTimestamp, requestStarted), Stopwatch.GetElapsedTime(requestStarted),
                        null, exception.Message)
                    {
                        CompletionOrdinal = Interlocked.Increment(ref _completionSequence)
                    });
                }
                finally
                {
                    Interlocked.Decrement(ref _activeMessageOperations);
                    if (globalPermitHeld) _globalPending.Release();
                    if (equipmentPermitHeld) equipmentPermit.Release();
                }
            }
        }
        finally { Interlocked.Decrement(ref _liveMessageWorkers); }
    }

    private void UpdateEquipmentCounts()
    {
        var requested = _host.Connections.Count;
        var connected = _host.Connections.Count(value => value.IsConnected);
        var selected = _host.Connections.Count(value => value.IsSelected);
        var failed = _host.Connections.Count(value => !value.IsConnected && value.LastError != "None");
        _metrics.UpdateEquipmentCounts(requested, connected, selected, failed,
            _host.ActiveReconnectOperationCount + Volatile.Read(ref _explicitReconnectOperations));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }

        List<Exception>? failures = null;
        try
        {
            _messageQueue.Complete();
            var workers = Task.WhenAll(_messageWorkers);
            try { await workers.WaitAsync(_gracefulShutdownTimeout).ConfigureAwait(false); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            _lifetime.Cancel();
            if (!workers.IsCompleted)
            {
                try { await workers.WaitAsync(_forcedShutdownTimeout).ConfigureAwait(false); }
                catch (Exception exception) { (failures ??= []).Add(exception); }
            }
            try { await _host.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            try { await _events.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            foreach (var permit in _equipmentPending.Values) permit.Dispose();
            _globalPending.Dispose();
            if (failures is not null) throw new AggregateException("Factory host runtime cleanup reported failures.", failures);
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            FailOutstandingMessages(new ObjectDisposedException(
                nameof(FactoryHostRuntime),
                "The Factory host runtime was disposed before the accepted request completed."));
            _lifetime.Dispose();
        }
    }

    private void CompleteResult(MessageWork work, FactoryMessageResult result)
    {
        work.Completion.TrySetResult(result);
        _outstandingMessages.TryRemove(work.Id, out _);
    }

    private void CompleteException(MessageWork work, Exception exception)
    {
        work.Completion.TrySetException(exception);
        _outstandingMessages.TryRemove(work.Id, out _);
    }

    private void CompleteCanceled(MessageWork work, CancellationToken cancellationToken)
    {
        work.Completion.TrySetCanceled(cancellationToken);
        _outstandingMessages.TryRemove(work.Id, out _);
    }

    private void FailOutstandingMessages(Exception exception)
    {
        foreach (var entry in _outstandingMessages)
        {
            if (!_outstandingMessages.TryRemove(entry.Key, out var completion)) continue;
            completion.TrySetException(exception);
        }
    }

    private static void UpdateMaximum(ref int target, int value)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (value <= current || Interlocked.CompareExchange(ref target, value, current) == current) return;
        }
    }

    private async Task TrackReconnectAsync(
        string equipmentId,
        Func<CancellationToken, Task> reconnect,
        CancellationToken cancellationToken)
    {
        var active = Interlocked.Increment(ref _explicitReconnectOperations);
        UpdateMaximum(ref _peakReconnectOperations, active);
        _metrics.IncrementReconnect();
        var equipmentMetrics = _equipmentMetrics[equipmentId];
        equipmentMetrics.ReconnectAttempt();
        try
        {
            await reconnect(cancellationToken).ConfigureAwait(false);
            equipmentMetrics.ReconnectSucceeded();
        }
        finally { Interlocked.Decrement(ref _explicitReconnectOperations); }
    }

    private sealed class EquipmentMetricState
    {
        private long _requests;
        private long _responses;
        private long _timeouts;
        private long _failures;
        private long _reconnectAttempts;
        private long _reconnectSucceeded;
        private long _responseTicks;
        private long _maximumResponseTicks;
        private long _maximumQueueTicks;

        internal void Request() => Interlocked.Increment(ref _requests);

        internal void Response(TimeSpan response, TimeSpan queueDelay)
        {
            Interlocked.Increment(ref _responses);
            Interlocked.Add(ref _responseTicks, response.Ticks);
            UpdateMaximum(ref _maximumResponseTicks, response.Ticks);
            UpdateMaximum(ref _maximumQueueTicks, queueDelay.Ticks);
        }

        internal void Timeout() => Interlocked.Increment(ref _timeouts);
        internal void Failure() => Interlocked.Increment(ref _failures);
        internal void ReconnectAttempt() => Interlocked.Increment(ref _reconnectAttempts);
        internal void ReconnectSucceeded() => Interlocked.Increment(ref _reconnectSucceeded);

        internal FactoryEquipmentMetricSnapshot Capture(EquipmentConnectionContext context)
        {
            var responses = Interlocked.Read(ref _responses);
            var responseTicks = Interlocked.Read(ref _responseTicks);
            return new FactoryEquipmentMetricSnapshot(
                context.EquipmentId,
                context.ConnectionId,
                context.Endpoint,
                context.SessionId,
                context.IsConnected,
                context.IsSelected,
                Interlocked.Read(ref _requests),
                responses,
                Interlocked.Read(ref _timeouts),
                Interlocked.Read(ref _failures),
                Interlocked.Read(ref _reconnectAttempts),
                Interlocked.Read(ref _reconnectSucceeded),
                responses == 0 ? 0 : TimeSpan.FromTicks(responseTicks / responses).TotalMilliseconds,
                TimeSpan.FromTicks(Interlocked.Read(ref _maximumResponseTicks)).TotalMilliseconds,
                TimeSpan.FromTicks(Interlocked.Read(ref _maximumQueueTicks)).TotalMilliseconds,
                context.LastActivity,
                context.LastError);
        }

        private static void UpdateMaximum(ref long target, long value)
        {
            while (true)
            {
                var current = Volatile.Read(ref target);
                if (value <= current || Interlocked.CompareExchange(ref target, value, current) == current) return;
            }
        }
    }

    private sealed record MessageWork(
        long Id,
        string EquipmentId,
        byte Stream,
        byte Function,
        SecsItem? Item,
        SecsSystemBytes? SystemBytes,
        long EnqueuedTimestamp,
        TaskCompletionSource<FactoryMessageResult> Completion,
        CancellationToken CallerCancellation);
}
