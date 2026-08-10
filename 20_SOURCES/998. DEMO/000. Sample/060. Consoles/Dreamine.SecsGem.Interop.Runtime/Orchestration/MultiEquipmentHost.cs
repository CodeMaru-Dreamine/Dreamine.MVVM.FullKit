using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.SecsGem.Interop.Runtime;

internal interface IEquipmentSessionFactory
{
    EquipmentConnectionContext Create(EquipmentConnectionDefinition definition);
}

internal interface IEquipmentConnectionRegistry : IAsyncDisposable
{
    ReadOnlyObservableCollection<EquipmentConnectionContext> Connections { get; }
    IReadOnlyList<EquipmentConnectionContext> Snapshot();
    EquipmentConnectionContext Add(EquipmentConnectionDefinition definition);
    EquipmentConnectionContext Get(string equipmentId);
    Task<bool> RemoveAsync(string equipmentId, CancellationToken cancellationToken);
    Task ReplaceAsync(IEnumerable<EquipmentConnectionDefinition> definitions, CancellationToken cancellationToken);
}

internal sealed class EquipmentSessionFactory : IEquipmentSessionFactory
{
    private readonly IEquipmentEventSink _events;
    private readonly IReconnectScheduler _reconnectScheduler;
    private readonly SynchronizationContext? _notificationContext;

    internal EquipmentSessionFactory(IEquipmentEventSink? events = null, IReconnectScheduler? reconnectScheduler = null,
        SynchronizationContext? notificationContext = null)
    {
        _events = events ?? NullEquipmentEventSink.Instance;
        _reconnectScheduler = reconnectScheduler ?? NullReconnectScheduler.Instance;
        _notificationContext = notificationContext;
    }

    public EquipmentConnectionContext Create(EquipmentConnectionDefinition definition) =>
        new(definition, _events, _reconnectScheduler, _notificationContext);
}

internal sealed class EquipmentConnectionRegistry : IEquipmentConnectionRegistry
{
    private readonly IEquipmentSessionFactory _factory;
    private readonly SynchronizationContext? _collectionContext;
    private readonly ObservableCollection<EquipmentConnectionContext> _connections = new();
    private readonly object _gate = new();
    private int _disposed;
    public ReadOnlyObservableCollection<EquipmentConnectionContext> Connections { get; }

    internal EquipmentConnectionRegistry(IEquipmentSessionFactory factory, SynchronizationContext? collectionContext = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _collectionContext = collectionContext;
        Connections = new ReadOnlyObservableCollection<EquipmentConnectionContext>(_connections);
    }

    public EquipmentConnectionContext Add(EquipmentConnectionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();
        EquipmentConnectionContext? context = null;
        MutateCollection(() =>
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (_connections.Any(value => value.EquipmentId.Equals(definition.EquipmentId, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"EquipmentId '{definition.EquipmentId}' already exists.");
                context = _factory.Create(definition);
                _connections.Add(context);
            }
        });
        return context!;
    }

    public EquipmentConnectionContext Get(string equipmentId)
    {
        lock (_gate)
            return _connections.FirstOrDefault(value => value.EquipmentId.Equals(equipmentId, StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException($"EquipmentId '{equipmentId}' was not found.");
    }

    public IReadOnlyList<EquipmentConnectionContext> Snapshot()
    {
        lock (_gate) return _connections.ToArray();
    }

    public async Task<bool> RemoveAsync(string equipmentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EquipmentConnectionContext? context = null;
        var removed = false;
        MutateCollection(() =>
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                context = _connections.FirstOrDefault(value => value.EquipmentId.Equals(equipmentId, StringComparison.OrdinalIgnoreCase));
                if (context is null) return;
                _connections.Remove(context);
                removed = true;
            }
        });
        if (!removed) return false;
        await context!.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    public async Task ReplaceAsync(IEnumerable<EquipmentConnectionDefinition> definitions, CancellationToken cancellationToken)
    {
        var materialized = definitions.ToArray();
        foreach (var definition in materialized) definition.Validate();
        var duplicate = materialized.GroupBy(value => value.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate EquipmentId '{duplicate.Key}'.");
        cancellationToken.ThrowIfCancellationRequested();

        var replacements = materialized.Select(_factory.Create).ToArray();
        EquipmentConnectionContext[] old = [];
        try
        {
            MutateCollection(() =>
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    old = _connections.ToArray();
                    _connections.Clear();
                    foreach (var context in replacements) _connections.Add(context);
                }
            });
        }
        catch
        {
            var cleanupFailures = await DisposeContextsAsync(replacements).ConfigureAwait(false);
            if (cleanupFailures.Count > 0)
                throw new AggregateException("Registry replacement failed and replacement cleanup also failed.", cleanupFailures);
            throw;
        }

        var failures = await DisposeContextsAsync(old).ConfigureAwait(false);
        if (failures.Count > 0)
            throw new AggregateException("One or more replaced equipment contexts failed to dispose.", failures);
    }

    public async ValueTask DisposeAsync()
    {
        EquipmentConnectionContext[] contexts = [];
        MutateCollection(() =>
        {
            lock (_gate)
            {
                if (_disposed != 0) return;
                _disposed = 1;
                contexts = _connections.ToArray();
                _connections.Clear();
            }
        });
        var failures = await DisposeContextsAsync(contexts).ConfigureAwait(false);
        if (failures.Count > 0)
            throw new AggregateException("One or more equipment contexts failed to dispose.", failures);
    }

    private static async Task<List<Exception>> DisposeContextsAsync(IEnumerable<EquipmentConnectionContext> contexts)
    {
        var failures = new ConcurrentBag<Exception>();
        await Parallel.ForEachAsync(contexts, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (context, _) =>
        {
            try { await context.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { failures.Add(exception); }
        }).ConfigureAwait(false);
        return failures.ToList();
    }

    private void MutateCollection(Action action)
    {
        if (_collectionContext is null || ReferenceEquals(SynchronizationContext.Current, _collectionContext)) action();
        else _collectionContext.Send(_ => action(), null);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed != 0, this);
}

internal sealed class MultiEquipmentHost : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IEquipmentConnectionRegistry _registry;
    private readonly IReconnectScheduler _reconnectScheduler;
    private readonly bool _ownsReconnectScheduler;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly SemaphoreSlim _topologyGate = new(1, 1);
    private readonly object _operationGate = new();
    private MultiEquipmentHostOptions _options;
    private TaskCompletionSource _idle = CompletedIdleSource();
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _backgroundOperations;
    private int _activeEquipmentOperations;
    private int _peakEquipmentOperations;
    private long _scheduledEquipmentOperations;
    private int _disposed;

    internal MultiEquipmentHost(IEquipmentEventSink? events = null, MultiEquipmentHostOptions? options = null,
        SynchronizationContext? collectionContext = null)
    {
        _options = options ?? new MultiEquipmentHostOptions();
        _options.Validate();
        _reconnectScheduler = new BoundedReconnectCoordinator(_options.ReconnectConcurrency, _options.ReconnectQueueCapacity);
        _ownsReconnectScheduler = true;
        _registry = new EquipmentConnectionRegistry(
            new EquipmentSessionFactory(events, _reconnectScheduler, collectionContext), collectionContext);
    }

    internal MultiEquipmentHost(IEquipmentConnectionRegistry registry, MultiEquipmentHostOptions options)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _reconnectScheduler = NullReconnectScheduler.Instance;
    }

    internal ReadOnlyObservableCollection<EquipmentConnectionContext> Connections => _registry.Connections;
    internal int ActiveSessionCount => _registry.Snapshot().Count(value => value.IsConnected);
    internal int PendingTransactionCount => _registry.Snapshot().Sum(value => value.PendingTransactionCount);
    internal int BackgroundOperationCount => Volatile.Read(ref _backgroundOperations);
    internal int ActiveReconnectOperationCount => _reconnectScheduler.ActiveCount;
    internal int ReconnectQueueDepth => _reconnectScheduler.QueueDepth;
    internal long RejectedReconnectCount => _reconnectScheduler.RejectedCount;
    internal int PeakEquipmentOperationCount => Volatile.Read(ref _peakEquipmentOperations);
    internal long ScheduledEquipmentOperationCount => Interlocked.Read(ref _scheduledEquipmentOperations);
    internal MultiEquipmentHostOptions Options => _options;

    internal EquipmentConnectionContext Add(EquipmentConnectionDefinition definition)
    {
        _topologyGate.Wait();
        try
        {
            ThrowIfDisposed();
            return _registry.Add(definition);
        }
        finally { _topologyGate.Release(); }
    }

    internal Task<bool> RemoveAsync(string equipmentId, CancellationToken cancellationToken) =>
        RunTopologyTrackedAsync(token => _registry.RemoveAsync(equipmentId, token), cancellationToken);

    internal EquipmentConnectionContext Get(string equipmentId) => _registry.Get(equipmentId);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> ConnectAllAsync(CancellationToken cancellationToken) =>
        RunRegistryBoundedAsync(_options.ConnectConcurrency, (context, token) => context.ConnectAsync(token), cancellationToken);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> DisconnectAllAsync(CancellationToken cancellationToken) =>
        RunRegistryBoundedAsync(_options.ConnectConcurrency, (context, token) => context.DisconnectAsync(token), cancellationToken);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> LinktestAllAsync(CancellationToken cancellationToken) =>
        RunRegistryBoundedAsync(_options.MessageConcurrency, (context, token) => context.LinktestAsync(token), cancellationToken);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> ReconnectAllAsync(CancellationToken cancellationToken) =>
        RunRegistryBoundedAsync(_options.ReconnectConcurrency, (context, token) => context.ReconnectAsync(token), cancellationToken);

    internal Task<EquipmentOperationResult> ConnectAsync(string equipmentId, CancellationToken cancellationToken) =>
        RunSingleAsync(equipmentId, (context, token) => context.ConnectAsync(token), cancellationToken);

    internal Task<EquipmentOperationResult> DisconnectAsync(string equipmentId, CancellationToken cancellationToken) =>
        RunSingleAsync(equipmentId, (context, token) => context.DisconnectAsync(token), cancellationToken);

    internal Task<EquipmentOperationResult> ReconnectAsync(string equipmentId, CancellationToken cancellationToken) =>
        RunSingleAsync(equipmentId, (context, token) => context.ReconnectAsync(token), cancellationToken);

    internal Task<EquipmentOperationResult> LinktestAsync(string equipmentId, CancellationToken cancellationToken) =>
        RunSingleAsync(equipmentId, (context, token) => context.LinktestAsync(token), cancellationToken);

    internal Task<SecsMessage> SendPrimaryAsync(string equipmentId, byte stream, byte function, SecsItem? item,
        CancellationToken cancellationToken, SecsSystemBytes? systemBytes = null) =>
        RunTrackedAsync(token => _registry.Get(equipmentId).SendPrimaryAsync(stream, function, item, token, systemBytes), cancellationToken);

    internal Task SendAsync(string equipmentId, byte stream, byte function, SecsItem? item,
        CancellationToken cancellationToken) =>
        RunTrackedAsync(token => _registry.Get(equipmentId).SendAsync(stream, function, item, token), cancellationToken);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> BroadcastAsync(byte stream, byte function,
        SecsItem? item, CancellationToken cancellationToken) => RunRegistryBoundedAsync(_options.MessageConcurrency,
        async (context, token) => { await context.SendPrimaryAsync(stream, function, item, token).ConfigureAwait(false); }, cancellationToken);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> BroadcastSendAsync(byte stream, byte function,
        SecsItem? item, CancellationToken cancellationToken) => RunRegistryBoundedAsync(_options.MessageConcurrency,
        (context, token) => context.SendAsync(stream, function, item, token), cancellationToken);

    internal Task ExportConfigurationAsync(string path, CancellationToken cancellationToken) =>
        RunTrackedAsync(async token =>
        {
            var configuration = new MultiEquipmentConfiguration(1, _options,
                _registry.Snapshot().Select(value => value.Definition).ToArray());
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(configuration, JsonOptions), token).ConfigureAwait(false);
        }, cancellationToken);

    internal Task ImportConfigurationAsync(string path, CancellationToken cancellationToken) =>
        RunTopologyTrackedAsync(async token =>
        {
            var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            var configuration = JsonSerializer.Deserialize<MultiEquipmentConfiguration>(json, JsonOptions)
                ?? throw new InvalidDataException("The multi-equipment configuration is empty.");
            if (configuration.SchemaVersion != 1)
                throw new InvalidDataException($"Unsupported schema version {configuration.SchemaVersion}.");
            var options = configuration.Options ?? throw new InvalidDataException("The multi-equipment options are missing.");
            var equipment = configuration.Equipment ?? throw new InvalidDataException("The multi-equipment list is missing.");
            options.Validate();
            if (options.ReconnectConcurrency != _options.ReconnectConcurrency ||
                options.ReconnectQueueCapacity != _options.ReconnectQueueCapacity)
            {
                throw new InvalidDataException(
                    "Reconnect concurrency and queue capacity are fixed for the lifetime of a MultiEquipmentHost. " +
                    "Create the host with the imported reconnect settings before importing its equipment configuration.");
            }
            await _registry.ReplaceAsync(equipment, token).ConfigureAwait(false);
            _options = options;
        }, cancellationToken);

    internal Task<IReadOnlyDictionary<string, EquipmentOperationResult>> RunBoundedAsync(
        IEnumerable<EquipmentConnectionContext> source, int concurrency,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        if (concurrency <= 0) throw new ArgumentOutOfRangeException(nameof(concurrency));
        return RunTrackedAsync(token => RunBoundedCoreAsync(source.ToArray(), concurrency, operation, token), cancellationToken);
    }

    private Task<IReadOnlyDictionary<string, EquipmentOperationResult>> RunRegistryBoundedAsync(int concurrency,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken cancellationToken) =>
        RunTrackedAsync(token => RunBoundedCoreAsync(_registry.Snapshot(), concurrency, operation, token), cancellationToken);

    private async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> RunBoundedCoreAsync(
        IReadOnlyList<EquipmentConnectionContext> source, int concurrency,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken token)
    {
        var results = new ConcurrentDictionary<string, EquipmentOperationResult>(StringComparer.OrdinalIgnoreCase);
        await Parallel.ForEachAsync(source, new ParallelOptions
        {
            MaxDegreeOfParallelism = concurrency,
            CancellationToken = token
        }, async (context, itemToken) =>
        {
            Interlocked.Increment(ref _scheduledEquipmentOperations);
            var active = Interlocked.Increment(ref _activeEquipmentOperations);
            UpdateMaximum(ref _peakEquipmentOperations, active);
            var timer = Stopwatch.StartNew();
            try
            {
                await operation(context, itemToken).ConfigureAwait(false);
                results[context.EquipmentId] = new(context.EquipmentId, true, "Passed", timer.Elapsed);
            }
            catch (OperationCanceledException) when (itemToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                results[context.EquipmentId] = new(context.EquipmentId, false, exception.Message, timer.Elapsed);
            }
            finally { Interlocked.Decrement(ref _activeEquipmentOperations); }
        }).ConfigureAwait(false);
        return results;
    }

    private Task<EquipmentOperationResult> RunSingleAsync(string equipmentId,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken cancellationToken) =>
        RunTrackedAsync(async token =>
        {
            var context = _registry.Get(equipmentId);
            var timer = Stopwatch.StartNew();
            await operation(context, token).ConfigureAwait(false);
            return new EquipmentOperationResult(equipmentId, true, "Passed", timer.Elapsed);
        }, cancellationToken);

    private Task RunTopologyTrackedAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
        RunTrackedAsync(async token =>
        {
            await _topologyGate.WaitAsync(token).ConfigureAwait(false);
            try { await operation(token).ConfigureAwait(false); }
            finally { _topologyGate.Release(); }
        }, cancellationToken);

    private Task<T> RunTopologyTrackedAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        RunTrackedAsync(async token =>
        {
            await _topologyGate.WaitAsync(token).ConfigureAwait(false);
            try { return await operation(token).ConfigureAwait(false); }
            finally { _topologyGate.Release(); }
        }, cancellationToken);

    private Task RunTrackedAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
        RunTrackedAsync(async token => { await operation(token).ConfigureAwait(false); return true; }, cancellationToken);

    private async Task<T> RunTrackedAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        CancellationTokenSource linkedCancellation;
        lock (_operationGate)
        {
            ThrowIfDisposed();
            if (_backgroundOperations++ == 0)
                _idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellation.Token);
        }
        try { return await operation(linkedCancellation.Token).ConfigureAwait(false); }
        finally
        {
            linkedCancellation.Dispose();
            lock (_operationGate) if (--_backgroundOperations == 0) _idle.TrySetResult();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Task idle;
        var ownsDispose = false;
        lock (_operationGate)
        {
            if (_disposed == 0)
            {
                _disposed = 1;
                ownsDispose = true;
                _lifetimeCancellation.Cancel();
                idle = _idle.Task;
            }
            else idle = _disposeCompletion.Task;
        }
        if (!ownsDispose)
        {
            await idle.ConfigureAwait(false);
            return;
        }

        try
        {
            await idle.ConfigureAwait(false);
            await _topologyGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_ownsReconnectScheduler) await _reconnectScheduler.DisposeAsync().ConfigureAwait(false);
                await _registry.DisposeAsync().ConfigureAwait(false);
            }
            finally { _topologyGate.Release(); }
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally
        {
            _lifetimeCancellation.Dispose();
            _topologyGate.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private static TaskCompletionSource CompletedIdleSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
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
}
