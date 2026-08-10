using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Dreamine.SecsGem.Interop.Runtime;

internal interface IReconnectScheduler : IAsyncDisposable
{
    int ActiveCount { get; }
    int QueueDepth { get; }
    long RejectedCount { get; }
    bool TrySchedule(EquipmentConnectionContext context);
}

internal sealed class NullReconnectScheduler : IReconnectScheduler
{
    internal static NullReconnectScheduler Instance { get; } = new();
    private NullReconnectScheduler() { }
    public int ActiveCount => 0;
    public int QueueDepth => 0;
    public long RejectedCount => 0;
    public bool TrySchedule(EquipmentConnectionContext context) => false;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class BoundedReconnectCoordinator : IReconnectScheduler
{
    private readonly Channel<EquipmentConnectionContext> _queue;
    private readonly ConcurrentDictionary<EquipmentConnectionContext, byte> _scheduled = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly Task[] _workers;
    private int _active;
    private int _queueDepth;
    private long _rejected;
    private int _disposed;

    internal BoundedReconnectCoordinator(int concurrency, int capacity)
    {
        if (concurrency <= 0) throw new ArgumentOutOfRangeException(nameof(concurrency));
        if (capacity < concurrency) throw new ArgumentOutOfRangeException(nameof(capacity));
        _queue = Channel.CreateBounded<EquipmentConnectionContext>(new BoundedChannelOptions(capacity)
        {
            SingleReader = concurrency == 1,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
        _workers = Enumerable.Range(0, concurrency)
            .Select(_ => RunWorkerAsync(_lifetimeCancellation.Token))
            .ToArray();
    }

    public int ActiveCount => Volatile.Read(ref _active);
    public int QueueDepth => Volatile.Read(ref _queueDepth);
    public long RejectedCount => Interlocked.Read(ref _rejected);

    public bool TrySchedule(EquipmentConnectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Volatile.Read(ref _disposed) != 0 || !context.ShouldAutoReconnect) return false;
        if (!_scheduled.TryAdd(context, 0)) return true;
        Interlocked.Increment(ref _queueDepth);
        if (_queue.Writer.TryWrite(context)) return true;
        Interlocked.Decrement(ref _queueDepth);
        _scheduled.TryRemove(context, out _);
        Interlocked.Increment(ref _rejected);
        context.ReportReconnectQueueRejected();
        return false;
    }

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var context in _queue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                Interlocked.Decrement(ref _queueDepth);
                Interlocked.Increment(ref _active);
                try
                {
                    if (context.ShouldAutoReconnect)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(context.Definition.T5Seconds), cancellationToken)
                            .ConfigureAwait(false);
                        await context.RunAutomaticReconnectAttemptAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                catch (Exception)
                {
                    // The context records the concrete connection failure. A later attempt is
                    // re-queued below without allowing failed endpoints to occupy a worker forever.
                }
                finally
                {
                    Interlocked.Decrement(ref _active);
                    _scheduled.TryRemove(context, out _);
                }

                if (!cancellationToken.IsCancellationRequested && context.ShouldAutoReconnect)
                    TrySchedule(context);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _queue.Writer.TryComplete();
        _lifetimeCancellation.Cancel();
        try { await Task.WhenAll(_workers).ConfigureAwait(false); }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested) { }
        _scheduled.Clear();
        Interlocked.Exchange(ref _queueDepth, 0);
        _lifetimeCancellation.Dispose();
    }
}
