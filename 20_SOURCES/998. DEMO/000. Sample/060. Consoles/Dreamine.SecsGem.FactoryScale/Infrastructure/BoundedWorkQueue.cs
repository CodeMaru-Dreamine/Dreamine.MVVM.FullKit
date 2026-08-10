using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Dreamine.SecsGem.FactoryScale.Models;

namespace Dreamine.SecsGem.FactoryScale.Infrastructure;

internal enum BoundedBusinessQueueFullPolicy
{
    Wait,
    Reject
}

/// <summary>
/// A bounded queue for protocol/business work. Saturation is either awaited or
/// explicitly returned to the producer; an accepted item is never discarded.
/// </summary>
internal sealed class BoundedWorkQueue<T>
{
    private readonly Channel<T> _channel;
    private readonly BoundedBusinessQueueFullPolicy _fullPolicy;
    private int _peakDepth;
    private long _accepted;
    private long _dequeued;
    private long _rejected;

    internal BoundedWorkQueue(
        string name,
        int capacity,
        BoundedBusinessQueueFullPolicy fullPolicy = BoundedBusinessQueueFullPolicy.Wait,
        bool singleReader = false,
        bool singleWriter = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (!Enum.IsDefined(fullPolicy)) throw new ArgumentOutOfRangeException(nameof(fullPolicy));

        Name = name;
        Capacity = capacity;
        _fullPolicy = fullPolicy;
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = singleReader,
            SingleWriter = singleWriter,
            AllowSynchronousContinuations = false
        });
    }

    internal string Name { get; }
    internal int Capacity { get; }
    internal int Depth => _channel.Reader.CanCount ? _channel.Reader.Count : 0;
    internal int PeakDepth => Volatile.Read(ref _peakDepth);
    internal long AcceptedCount => Interlocked.Read(ref _accepted);
    internal long DequeuedCount => Interlocked.Read(ref _dequeued);
    internal long RejectedCount => Interlocked.Read(ref _rejected);
    internal Task Completion => _channel.Reader.Completion;

    /// <summary>
    /// Returns false only for the explicit Reject policy or a completed queue.
    /// Wait policy applies backpressure until a slot is available.
    /// </summary>
    internal async ValueTask<bool> EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        if (_fullPolicy == BoundedBusinessQueueFullPolicy.Reject)
            return TryEnqueue(item);

        try
        {
            await _channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
            Accepted();
            return true;
        }
        catch (ChannelClosedException)
        {
            Interlocked.Increment(ref _rejected);
            return false;
        }
    }

    /// <summary>
    /// Performs non-blocking, explicit admission. False means the producer must
    /// handle the rejected business item; the queue did not drop an accepted item.
    /// </summary>
    internal bool TryEnqueue(T item)
    {
        if (!_channel.Writer.TryWrite(item))
        {
            Interlocked.Increment(ref _rejected);
            return false;
        }

        Accepted();
        return true;
    }

    internal void Complete(Exception? error = null) => _channel.Writer.TryComplete(error);

    internal async IAsyncEnumerable<T> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            Interlocked.Increment(ref _dequeued);
            yield return item;
        }
    }

    internal FactoryQueueMetricSnapshot CaptureMetrics() => new(
        Name,
        Capacity,
        Depth,
        PeakDepth,
        AcceptedCount,
        DequeuedCount,
        RejectedCount,
        0,
        _fullPolicy.ToString());

    private void Accepted()
    {
        Interlocked.Increment(ref _accepted);
        UpdateMaximum(ref _peakDepth, Depth);
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

/// <summary>
/// A bounded, non-blocking diagnostic queue. It intentionally drops the newest
/// diagnostic when full and counts every failed admission. It must not carry
/// protocol/business messages.
/// </summary>
internal sealed class BoundedDiagnosticQueue<T>
{
    private readonly Channel<T> _channel;
    private int _peakDepth;
    private long _accepted;
    private long _dequeued;
    private long _dropped;

    internal BoundedDiagnosticQueue(
        string name,
        int capacity,
        bool singleReader = true,
        bool singleWriter = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        Name = name;
        Capacity = capacity;
        // Wait mode is deliberate: TryWrite then returns false at capacity,
        // which lets us count an actual dropped diagnostic exactly. Channel
        // DropOldest/DropWrite modes report successful writes for dropped data.
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = singleReader,
            SingleWriter = singleWriter,
            AllowSynchronousContinuations = false
        });
    }

    internal string Name { get; }
    internal int Capacity { get; }
    internal int Depth => _channel.Reader.CanCount ? _channel.Reader.Count : 0;
    internal int PeakDepth => Volatile.Read(ref _peakDepth);
    internal long AcceptedCount => Interlocked.Read(ref _accepted);
    internal long DequeuedCount => Interlocked.Read(ref _dequeued);
    internal long DroppedCount => Interlocked.Read(ref _dropped);
    internal Task Completion => _channel.Reader.Completion;

    internal bool TryWrite(T item)
    {
        if (!_channel.Writer.TryWrite(item))
        {
            Interlocked.Increment(ref _dropped);
            return false;
        }

        Interlocked.Increment(ref _accepted);
        UpdateMaximum(ref _peakDepth, Depth);
        return true;
    }

    internal void Complete(Exception? error = null) => _channel.Writer.TryComplete(error);

    internal async IAsyncEnumerable<T> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            Interlocked.Increment(ref _dequeued);
            yield return item;
        }
    }

    internal FactoryQueueMetricSnapshot CaptureMetrics() => new(
        Name,
        Capacity,
        Depth,
        PeakDepth,
        AcceptedCount,
        DequeuedCount,
        0,
        DroppedCount,
        "DropDiagnosticNewest");

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
