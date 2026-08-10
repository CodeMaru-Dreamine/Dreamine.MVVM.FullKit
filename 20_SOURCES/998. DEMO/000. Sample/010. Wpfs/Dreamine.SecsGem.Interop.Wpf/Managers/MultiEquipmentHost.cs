using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

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

internal sealed class EquipmentSessionFactory(InteropLogManager log) : IEquipmentSessionFactory
{
    public EquipmentConnectionContext Create(EquipmentConnectionDefinition definition) => new(definition, log);
}

internal sealed class EquipmentConnectionRegistry : IEquipmentConnectionRegistry
{
    private readonly IEquipmentSessionFactory _factory;
    private readonly ObservableCollection<EquipmentConnectionContext> _connections = new();
    private readonly object _gate = new();
    private int _disposed;
    public ReadOnlyObservableCollection<EquipmentConnectionContext> Connections { get; }

    public EquipmentConnectionRegistry(IEquipmentSessionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
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
        EquipmentConnectionContext? context;
        var removed = false;
        context = null;
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
        var duplicate = materialized.GroupBy(value => value.EquipmentId, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException($"Duplicate EquipmentId '{duplicate.Key}'.");
        cancellationToken.ThrowIfCancellationRequested();

        var replacements = materialized.Select(_factory.Create).ToArray();

        EquipmentConnectionContext[] old = Array.Empty<EquipmentConnectionContext>();
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
            var cleanupFailures = new List<Exception>();
            foreach (var context in replacements)
            {
                try { await context.DisposeAsync().ConfigureAwait(false); }
                catch (Exception exception) { cleanupFailures.Add(exception); }
            }
            if (cleanupFailures.Count > 0)
                throw new AggregateException("Registry replacement failed and one or more replacement contexts also failed to dispose.", cleanupFailures);
            throw;
        }
        List<Exception>? failures = null;
        foreach (var context in old)
        {
            try { await context.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
        if (failures is not null) throw new AggregateException("One or more replaced equipment contexts failed to dispose.", failures);
    }

    public async ValueTask DisposeAsync()
    {
        EquipmentConnectionContext[] contexts = Array.Empty<EquipmentConnectionContext>();
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
        List<Exception>? failures = null;
        foreach (var context in contexts)
        {
            try { await context.DisposeAsync().ConfigureAwait(false); }
            catch (Exception exception) { (failures ??= []).Add(exception); }
        }
        if (failures is not null) throw new AggregateException("One or more equipment contexts failed to dispose.", failures);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed != 0, this);

    private static void MutateCollection(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }
}

internal sealed class MultiEquipmentHost : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IEquipmentConnectionRegistry _registry;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly SemaphoreSlim _executionGate = new(1, 1);
    private readonly object _operationGate = new();
    private MultiEquipmentHostOptions _options;
    private TaskCompletionSource _idle = CompletedIdleSource();
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _backgroundOperations;
    private int _activeEquipmentOperations;
    private int _peakEquipmentOperations;
    private long _scheduledEquipmentOperations;
    private int _disposed;

    internal MultiEquipmentHost(InteropLogManager log)
        : this(new EquipmentConnectionRegistry(new EquipmentSessionFactory(log)), new MultiEquipmentHostOptions()) { }

    internal MultiEquipmentHost(IEquipmentConnectionRegistry registry, MultiEquipmentHostOptions options)
    {
        _registry = registry;
        _options = options;
        _options.Validate();
    }

    internal ReadOnlyObservableCollection<EquipmentConnectionContext> Connections => _registry.Connections;
    internal int ActiveSessionCount => _registry.Snapshot().Count(value => value.IsConnected);
    internal int BackgroundOperationCount => Volatile.Read(ref _backgroundOperations);
    internal int PeakEquipmentOperationCount => Volatile.Read(ref _peakEquipmentOperations);
    internal long ScheduledEquipmentOperationCount => Interlocked.Read(ref _scheduledEquipmentOperations);
    internal MultiEquipmentHostOptions Options => _options;

    internal EquipmentConnectionContext Add(EquipmentConnectionDefinition definition)
    {
        ThrowIfDisposed();
        if (!_executionGate.Wait(0))
            throw new InvalidOperationException("Equipment cannot be added while another host operation is running.");
        try
        {
            ThrowIfDisposed();
            return _registry.Add(definition);
        }
        finally { _executionGate.Release(); }
    }

    internal Task<bool> RemoveAsync(string equipmentId, CancellationToken cancellationToken) =>
        RunTrackedAsync(token => _registry.RemoveAsync(equipmentId, token), cancellationToken);

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
        RunTrackedAsync(async token =>
    {
        var json = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
        var configuration = JsonSerializer.Deserialize<MultiEquipmentConfiguration>(json, JsonOptions)
            ?? throw new InvalidDataException("The multi-equipment configuration is empty.");
        if (configuration.SchemaVersion != 1) throw new InvalidDataException($"Unsupported schema version {configuration.SchemaVersion}.");
        var options = configuration.Options ?? throw new InvalidDataException("The multi-equipment options are missing.");
        var equipment = configuration.Equipment ?? throw new InvalidDataException("The multi-equipment list is missing.");
        options.Validate();
        await _registry.ReplaceAsync(equipment, token).ConfigureAwait(false);
        _options = options;
    }, cancellationToken);

    internal async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> RunBoundedAsync(
        IEnumerable<EquipmentConnectionContext> source, int concurrency,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        if (concurrency <= 0) throw new ArgumentOutOfRangeException(nameof(concurrency));
        return await RunTrackedAsync(token => RunBoundedCoreAsync(source.ToArray(), concurrency, operation, token), cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<IReadOnlyDictionary<string, EquipmentOperationResult>> RunRegistryBoundedAsync(int concurrency,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken cancellationToken) =>
        RunTrackedAsync(token => RunBoundedCoreAsync(_registry.Snapshot(), concurrency, operation, token), cancellationToken);

    private async Task<IReadOnlyDictionary<string, EquipmentOperationResult>> RunBoundedCoreAsync(
        IReadOnlyList<EquipmentConnectionContext> source, int concurrency,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken token)
    {
        var results = new ConcurrentDictionary<string, EquipmentOperationResult>(StringComparer.OrdinalIgnoreCase);
            await Parallel.ForEachAsync(source.ToArray(), new ParallelOptions
            {
                MaxDegreeOfParallelism = concurrency,
                CancellationToken = token
            }, async (context, token) =>
            {
                Interlocked.Increment(ref _scheduledEquipmentOperations);
                var active = Interlocked.Increment(ref _activeEquipmentOperations);
                UpdateMaximum(ref _peakEquipmentOperations, active);
                var timer = Stopwatch.StartNew();
                try
                {
                    await operation(context, token).ConfigureAwait(false);
                    results[context.EquipmentId] = new(context.EquipmentId, true, "Passed", timer.Elapsed);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception exception)
                {
                    results[context.EquipmentId] = new(context.EquipmentId, false, exception.Message, timer.Elapsed);
                }
                finally { Interlocked.Decrement(ref _activeEquipmentOperations); }
            }).ConfigureAwait(false);
        return results;
    }

    private async Task<EquipmentOperationResult> RunSingleAsync(string equipmentId,
        Func<EquipmentConnectionContext, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        return await RunTrackedAsync(async token =>
        {
            var context = _registry.Get(equipmentId);
            var timer = Stopwatch.StartNew();
            await operation(context, token).ConfigureAwait(false);
            return new EquipmentOperationResult(equipmentId, true, "Passed", timer.Elapsed);
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunTrackedAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
        await RunTrackedAsync(async token => { await operation(token).ConfigureAwait(false); return true; }, cancellationToken)
            .ConfigureAwait(false);

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
        var executionHeld = false;
        try
        {
            await _executionGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            executionHeld = true;
            return await operation(linkedCancellation.Token).ConfigureAwait(false);
        }
        finally
        {
            if (executionHeld) _executionGate.Release();
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
            await _registry.DisposeAsync().ConfigureAwait(false);
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
            _executionGate.Dispose();
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
