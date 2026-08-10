using System.Collections.Concurrent;
using System.Diagnostics;
using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Simulation;

internal sealed record FactoryEquipmentFleetOptions(
    int ReceiveQueueCapacity = 4_096,
    int ResponderCount = 16,
    int StartConcurrency = 64,
    int T3Seconds = 10,
    int T5Seconds = 2,
    int T6Seconds = 5,
    int T7Seconds = 10,
    int T8Seconds = 5)
{
    internal void Validate()
    {
        if (ReceiveQueueCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(ReceiveQueueCapacity));
        if (ResponderCount <= 0) throw new ArgumentOutOfRangeException(nameof(ResponderCount));
        if (StartConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(StartConcurrency));
        ValidateTimer(T3Seconds, 120, nameof(T3Seconds));
        ValidateTimer(T5Seconds, 240, nameof(T5Seconds));
        ValidateTimer(T6Seconds, 240, nameof(T6Seconds));
        ValidateTimer(T7Seconds, 240, nameof(T7Seconds));
        ValidateTimer(T8Seconds, 120, nameof(T8Seconds));
    }

    private static void ValidateTimer(int value, int maximum, string name)
    {
        if (value is < 1 || value > maximum) throw new ArgumentOutOfRangeException(name);
    }
}

internal enum FactoryEquipmentBehavior
{
    Normal,
    SlowResponse,
    NoResponse,
    CallbackException,
    DiagnosticSinkException,
    CloseBeforeResponse
}

internal sealed class FactoryEquipmentFleet : IAsyncDisposable
{
    private static int _liveResponderWorkers;
    private readonly FactoryEquipmentFleetOptions _options;
    private readonly ValidatedPortRangeManager _ports;
    private readonly Func<int, FactoryEquipmentBehavior> _behaviorSelector;
    private readonly BoundedWorkQueue<ResponseWork> _responses;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task[] _responders;
    private readonly ConcurrentDictionary<string, FactoryEquipmentPeer> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private FactoryEquipmentPeer[] _peers = [];
    private long _sentResponses;
    private long _businessRejects;
    private int _activeResponders;
    private int _peakResponders;
    private int _disposed;

    internal FactoryEquipmentFleet(
        FactoryEquipmentFleetOptions options,
        ValidatedPortRangeManager? ports = null,
        Func<int, FactoryEquipmentBehavior>? behaviorSelector = null)
    {
        options.Validate();
        _options = options;
        _ports = ports ?? new ValidatedPortRangeManager();
        _behaviorSelector = behaviorSelector ?? (_ => FactoryEquipmentBehavior.Normal);
        _responses = new BoundedWorkQueue<ResponseWork>(
            "equipment-receive",
            options.ReceiveQueueCapacity,
            BoundedBusinessQueueFullPolicy.Reject,
            singleReader: options.ResponderCount == 1);
        _responders = Enumerable.Range(0, options.ResponderCount)
            .Select(_ => RespondLoopAsync(_lifetime.Token)).ToArray();
    }

    internal IReadOnlyList<FactoryEquipmentPeer> Peers => _peers;
    internal IReadOnlyList<EquipmentConnectionDefinition> Definitions => _peers.Select(value => value.Definition).ToArray();
    internal long QueueDepth => _responses.Depth;
    internal long PeakQueueDepth => _responses.PeakDepth;
    internal long EnqueuedResponseCount => _responses.AcceptedCount;
    internal long SentResponseCount => Interlocked.Read(ref _sentResponses);
    internal long BusinessRejectCount => Interlocked.Read(ref _businessRejects);
    internal int PeakResponderCount => Volatile.Read(ref _peakResponders);
    internal static int LiveResponderWorkerCount => Volatile.Read(ref _liveResponderWorkers);
    internal int LiveSessionCount => _peers.Count(value => !value.IsSessionDisposed);
    internal int ListenerCount => _peers.Count(value => value.State == ConnectionState.Listening);
    internal int OpenSocketCount => _peers.Count(value => value.State == ConnectionState.Connected);

    internal FactoryQueueMetricSnapshot CaptureQueueMetrics() => _responses.CaptureMetrics();

    internal async Task StartAsync(int startIndex, int count, CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (startIndex < 1) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (_peers.Length != 0) throw new InvalidOperationException("The equipment fleet has already been started.");

            var started = new FactoryEquipmentPeer[count];
            try
            {
                await Parallel.ForEachAsync(Enumerable.Range(0, count), new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = Math.Min(_options.StartConcurrency, count)
                }, async (offset, token) =>
                {
                    var equipmentNumber = startIndex + offset;
                    var peer = await _ports.StartBoundAsync(
                        (port, startToken) => StartPeerAsync(equipmentNumber, port, startToken), token).ConfigureAwait(false);
                    started[offset] = peer;
                    if (!_byId.TryAdd(peer.Definition.EquipmentId, peer))
                        throw new InvalidOperationException($"Duplicate equipment ID '{peer.Definition.EquipmentId}'.");
                }).ConfigureAwait(false);
                _peers = started;
            }
            catch (Exception failure)
            {
                var cleanup = await DisposePeersAsync(started.Where(value => value is not null)!).ConfigureAwait(false);
                if (cleanup.Count > 0)
                    throw new AggregateException("Equipment fleet start failed and cleanup also reported failures.", [failure, .. cleanup]);
                throw;
            }
        }
        finally { _lifecycleGate.Release(); }
    }

    internal async Task WaitForConnectionsAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_peers.Select(value => value.ConnectionTask.WaitAsync(cancellationToken))).ConfigureAwait(false);
    }

    internal FactoryEquipmentPeer Get(string equipmentId) => _byId.TryGetValue(equipmentId, out var peer)
        ? peer
        : throw new KeyNotFoundException($"Equipment '{equipmentId}' was not found.");

    internal async Task RestartAsync(IEnumerable<string> equipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(equipmentIds);
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            await RestartCoreAsync(
                equipmentIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                cancellationToken).ConfigureAwait(false);
        }
        finally { _lifecycleGate.Release(); }
    }

    /// <summary>
    /// Replaces passive sessions whose remote Host has gone away and binds the
    /// same validated endpoints again. The lifecycle gate prevents a maintenance
    /// restart from racing explicit restart or deterministic fleet disposal.
    /// </summary>
    internal async Task<int> RestartDisconnectedAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            var targets = _peers
                .Where(value => !value.IsDisposed &&
                                value.State is ConnectionState.Disconnected or ConnectionState.Faulted)
                .Select(value => value.Definition.EquipmentId)
                .ToArray();
            if (targets.Length == 0) return 0;
            await RestartCoreAsync(targets, cancellationToken).ConfigureAwait(false);
            return targets.Length;
        }
        finally { _lifecycleGate.Release(); }
    }

    private async Task RestartCoreAsync(
        IReadOnlyCollection<string> targets,
        CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(targets, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = Math.Min(_options.StartConcurrency, Math.Max(1, targets.Count))
        }, async (equipmentId, token) =>
        {
            var previous = Get(equipmentId);
            var definition = previous.Definition;
            var behavior = previous.Behavior;
            await previous.DisposeAsync().ConfigureAwait(false);

            FactoryEquipmentPeer? replacement = null;
            Exception? lastFailure = null;
            for (var attempt = 0; attempt < 20 && replacement is null; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    replacement = await FactoryEquipmentPeer.StartAsync(definition, behavior, EnqueueResponse, token)
                        .ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is IOException or System.Net.Sockets.SocketException)
                {
                    lastFailure = exception;
                    await Task.Delay(50, token).ConfigureAwait(false);
                }
            }
            if (replacement is null)
                throw new IOException($"Equipment {equipmentId} could not rebind {definition.Endpoint}.", lastFailure);

            _byId[equipmentId] = replacement;
            var index = Array.FindIndex(_peers, value =>
                value.Definition.EquipmentId.Equals(equipmentId, StringComparison.OrdinalIgnoreCase));
            if (index < 0) throw new InvalidOperationException($"Equipment {equipmentId} was absent from the fleet array.");
            _peers[index] = replacement;
        }).ConfigureAwait(false);
    }

    private Task<FactoryEquipmentPeer> StartPeerAsync(int equipmentNumber, int port, CancellationToken cancellationToken)
    {
        var equipmentId = $"EQ-{equipmentNumber:0000}";
        var definition = new EquipmentConnectionDefinition
        {
            EquipmentId = equipmentId,
            Host = "127.0.0.1",
            Port = port,
            Mode = SecsConnectionMode.Active,
            SessionId = 0,
            AutoReconnect = false,
            T3Seconds = _options.T3Seconds,
            T5Seconds = _options.T5Seconds,
            T6Seconds = _options.T6Seconds,
            T7Seconds = _options.T7Seconds,
            T8Seconds = _options.T8Seconds
        };
        return FactoryEquipmentPeer.StartAsync(definition, _behaviorSelector(equipmentNumber), EnqueueResponse, cancellationToken);
    }

    private void EnqueueResponse(FactoryEquipmentPeer peer, SecsMessage request)
    {
        if (!request.Function.IsPrimary || !request.ReplyExpected) return;
        if (peer.Behavior == FactoryEquipmentBehavior.CallbackException)
        {
            Interlocked.Increment(ref _businessRejects);
            throw new InvalidOperationException($"Injected callback failure for {peer.Definition.EquipmentId}.");
        }

        var work = new ResponseWork(peer, request, Stopwatch.GetTimestamp());
        // HsmsSession raises MessageReceived synchronously on its receive loop.
        // Blocking that callback on an async queue can starve every responder at
        // Factory scale, so saturation is an explicit, measured business reject.
        if (!_responses.TryEnqueue(work)) Interlocked.Increment(ref _businessRejects);
    }

    private async Task RespondLoopAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _liveResponderWorkers);
        try
        {
            try
            {
                await foreach (var work in _responses.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var active = Interlocked.Increment(ref _activeResponders);
                    UpdateMaximum(ref _peakResponders, active);
                    try
                    {
                        if (work.Peer.Behavior == FactoryEquipmentBehavior.NoResponse) continue;
                        if (work.Peer.Behavior == FactoryEquipmentBehavior.SlowResponse)
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        else if ((work.Request.SystemBytes.Value & 0x07) == 0)
                            await Task.Delay(2, cancellationToken).ConfigureAwait(false);

                        if (work.Peer.Behavior == FactoryEquipmentBehavior.CloseBeforeResponse)
                        {
                            await work.Peer.DropConnectionAsync().ConfigureAwait(false);
                            continue;
                        }

                        await work.Peer.SendResponseAsync(work.Request, cancellationToken).ConfigureAwait(false);
                        Interlocked.Increment(ref _sentResponses);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                    catch (ObjectDisposedException) when (Volatile.Read(ref _disposed) != 0) { }
                    catch (IOException) { Interlocked.Increment(ref _businessRejects); }
                    catch (InvalidOperationException) { Interlocked.Increment(ref _businessRejects); }
                    finally { Interlocked.Decrement(ref _activeResponders); }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }
        finally { Interlocked.Decrement(ref _liveResponderWorkers); }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }

        await _lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        List<Exception>? failures = null;
        try
        {
            _responses.Complete();
            var responders = Task.WhenAll(_responders);
            try { await responders.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false); }
            catch (TimeoutException exception)
            {
                (failures ??= []).Add(exception);
                _lifetime.Cancel();
                try { await responders.ConfigureAwait(false); }
                catch (Exception responderFailure) { (failures ??= []).Add(responderFailure); }
            }
            catch (Exception exception) { (failures ??= []).Add(exception); }
            finally { _lifetime.Cancel(); }
            failures ??= [];
            failures.AddRange(await DisposePeersAsync(_peers).ConfigureAwait(false));
            if (failures.Count > 0) throw new AggregateException("Equipment fleet cleanup reported failures.", failures);
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            _lifetime.Dispose();
            _lifecycleGate.Release();
        }
    }

    private static async Task<List<Exception>> DisposePeersAsync(IEnumerable<FactoryEquipmentPeer> peers)
    {
        var failures = new ConcurrentBag<Exception>();
        await Parallel.ForEachAsync(peers, new ParallelOptions { MaxDegreeOfParallelism = 64 }, async (peer, _) =>
        {
            try { await peer.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }).ConfigureAwait(false);
        return failures.ToList();
    }

    private static void UpdateMaximum(ref long target, long value)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (value <= current || Interlocked.CompareExchange(ref target, value, current) == current) return;
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

    private sealed record ResponseWork(FactoryEquipmentPeer Peer, SecsMessage Request, long EnqueuedTimestamp);
}

internal sealed class FactoryEquipmentPeer : IAsyncDisposable
{
    private static int _liveSessionCount;
    private readonly HsmsSession _session;
    private readonly Action<FactoryEquipmentPeer, SecsMessage> _onPrimary;
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _disposed;
    private int _sessionDisposed;

    private FactoryEquipmentPeer(
        EquipmentConnectionDefinition definition,
        FactoryEquipmentBehavior behavior,
        HsmsSession session,
        Task connectionTask,
        Action<FactoryEquipmentPeer, SecsMessage> onPrimary)
    {
        Definition = definition;
        Behavior = behavior;
        _session = session;
        ConnectionTask = connectionTask;
        _onPrimary = onPrimary;
        _session.MessageReceived += OnMessageReceived;
        Interlocked.Increment(ref _liveSessionCount);
    }

    internal EquipmentConnectionDefinition Definition { get; }
    internal FactoryEquipmentBehavior Behavior { get; }
    internal Task ConnectionTask { get; }
    internal ConnectionState State => _session.State;
    internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;
    internal bool IsSessionDisposed => Volatile.Read(ref _sessionDisposed) != 0;
    internal static int LiveSessionCount => Volatile.Read(ref _liveSessionCount);

    internal static async Task<FactoryEquipmentPeer> StartAsync(
        EquipmentConnectionDefinition hostDefinition,
        FactoryEquipmentBehavior behavior,
        Action<FactoryEquipmentPeer, SecsMessage> onPrimary,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hostDefinition);
        ArgumentNullException.ThrowIfNull(onPrimary);
        var diagnosticSink = behavior == FactoryEquipmentBehavior.DiagnosticSinkException
            ? new ThrowingDiagnosticSink()
            : null;
        var session = new HsmsSession(new HsmsSessionOptions
        {
            Host = hostDefinition.Host,
            Port = hostDefinition.Port,
            Mode = SecsConnectionMode.Passive,
            Role = SecsRole.Equipment,
            SessionId = new SecsSessionId(hostDefinition.SessionId),
            AutoReconnect = false,
            Timers = new HsmsTimerOptions
            {
                T3 = TimeSpan.FromSeconds(hostDefinition.T3Seconds),
                T5 = TimeSpan.FromSeconds(hostDefinition.T5Seconds),
                T6 = TimeSpan.FromSeconds(hostDefinition.T6Seconds),
                T7 = TimeSpan.FromSeconds(hostDefinition.T7Seconds),
                T8 = TimeSpan.FromSeconds(hostDefinition.T8Seconds)
            }
        }, diagnostics: diagnosticSink);
        var connectTask = session.ConnectAsync(cancellationToken);
        try
        {
            await WaitUntilAsync(
                () => session.State is ConnectionState.Listening or ConnectionState.Faulted,
                TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            if (session.State == ConnectionState.Faulted) await connectTask.ConfigureAwait(false);
            return new FactoryEquipmentPeer(hostDefinition, behavior, session, connectTask, onPrimary);
        }
        catch
        {
            try { await session.DisposeAsync().ConfigureAwait(false); } catch { }
            Observe(connectTask);
            throw;
        }
    }

    internal async Task SendResponseAsync(SecsMessage request, CancellationToken cancellationToken)
    {
        SecsItem responseItem = (request.Stream.Value, request.Function.Value) switch
        {
            (1, 13) => new SecsListItem(new SecsBinaryItem(0), new SecsListItem(
                new SecsAsciiItem(Definition.EquipmentId), new SecsAsciiItem("1.0"))),
            (1, 1) => new SecsListItem(new SecsAsciiItem(Definition.EquipmentId), new SecsAsciiItem("1.0")),
            _ => new SecsAsciiItem(Definition.EquipmentId)
        };
        var response = new SecsMessage(request.SessionId, request.Stream,
            new SecsFunction((byte)(request.Function.Value + 1)), false, request.SystemBytes, responseItem);
        await _session.SendAsync(response, cancellationToken).ConfigureAwait(false);
    }

    internal Task SendOneWayAsync(byte stream, byte function, SecsItem? item, CancellationToken cancellationToken)
    {
        var message = new SecsMessage(new SecsSessionId(Definition.SessionId), new SecsStream(stream),
            new SecsFunction(function), false, _session.AllocateSystemBytes(), item);
        return _session.SendAsync(message, cancellationToken);
    }

    internal async Task DropConnectionAsync()
    {
        if (IsDisposed) return;
        await DisposeSessionAsync().ConfigureAwait(false);
    }

    private void OnMessageReceived(object? sender, SecsMessage message) => _onPrimary(this, message);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }
        try
        {
            _session.MessageReceived -= OnMessageReceived;
            await DisposeSessionAsync().ConfigureAwait(false);
            try { await ConnectionTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (IOException) { }
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
    }

    private async Task DisposeSessionAsync()
    {
        if (Interlocked.Exchange(ref _sessionDisposed, 1) != 0) return;
        try { await _session.DisposeAsync().ConfigureAwait(false); }
        finally { Interlocked.Decrement(ref _liveSessionCount); }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            while (!condition()) await Task.Delay(2, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The equipment listener did not start within {timeout}.");
        }
    }

    private static void Observe(Task task)
    {
        _ = task.ContinueWith(
            static completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed class ThrowingDiagnosticSink : ISecsDiagnosticSink
    {
        public void Emit(SecsDiagnosticEvent diagnosticEvent) =>
            throw new InvalidOperationException("Injected diagnostic sink failure.");
    }
}
