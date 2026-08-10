using System.Diagnostics;
using Dreamine.SecsGem.FactoryScale.Models;

namespace Dreamine.SecsGem.FactoryScale.Metrics;

internal sealed record FactoryLatencyHistogramState(
    int LayoutVersion,
    long[] BucketCounts,
    long SampleCount,
    long SumTicks,
    long MinimumTicks,
    long MaximumTicks);

/// <summary>
/// A mergeable, fixed-size latency histogram. The bucket layout keeps microsecond
/// resolution for the fast path and gradually widens up to the maximum HSMS T3
/// range. Memory usage is independent of message count and soak duration.
/// </summary>
internal sealed class FixedMemoryLatencyHistogram
{
    private const int CurrentLayoutVersion = 1;
    private const long TicksPerMicrosecond = 10;
    private static readonly Segment[] Segments = CreateSegments();
    private static readonly int OverflowBucket = Segments[^1].Offset + Segments[^1].BucketCount;
    private static readonly int TotalBucketCount = OverflowBucket + 1;

    private readonly long[] _buckets = new long[TotalBucketCount];
    private long _count;
    private long _sumTicks;
    private long _minimumTicks = long.MaxValue;
    private long _maximumTicks;

    internal int BucketCount => TotalBucketCount;
    internal long SampleCount => Interlocked.Read(ref _count);

    internal void Record(TimeSpan latency) => RecordTicks(latency.Ticks);

    internal void RecordElapsed(long startedTimestamp) => Record(Stopwatch.GetElapsedTime(startedTimestamp));

    internal void RecordTicks(long ticks)
    {
        if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));

        UpdateMinimum(ref _minimumTicks, ticks);
        UpdateMaximum(ref _maximumTicks, ticks);
        Interlocked.Add(ref _sumTicks, ticks);
        Interlocked.Increment(ref _buckets[FindBucket(ticks)]);
        Interlocked.Increment(ref _count);
    }

    internal FactoryLatencySnapshot Capture()
    {
        var state = CaptureState();
        return Summarize(state);
    }

    internal FactoryLatencyHistogramState CaptureState()
    {
        var buckets = new long[_buckets.Length];
        for (var index = 0; index < buckets.Length; index++)
            buckets[index] = Interlocked.Read(ref _buckets[index]);

        return new FactoryLatencyHistogramState(
            CurrentLayoutVersion,
            buckets,
            Interlocked.Read(ref _count),
            Interlocked.Read(ref _sumTicks),
            Interlocked.Read(ref _minimumTicks),
            Interlocked.Read(ref _maximumTicks));
    }

    /// <summary>
    /// Merges an aggregate received from another FactoryScale process without
    /// replaying individual latency samples.
    /// </summary>
    internal void Merge(FactoryLatencyHistogramState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.LayoutVersion != CurrentLayoutVersion)
            throw new ArgumentException($"Unsupported latency histogram layout {state.LayoutVersion}.", nameof(state));
        if (state.BucketCounts.Length != _buckets.Length)
            throw new ArgumentException("Latency histogram bucket count does not match.", nameof(state));
        if (state.SampleCount < 0 || state.SumTicks < 0)
            throw new ArgumentException("Latency histogram counters cannot be negative.", nameof(state));

        for (var index = 0; index < _buckets.Length; index++)
        {
            var value = state.BucketCounts[index];
            if (value < 0) throw new ArgumentException("Latency histogram buckets cannot be negative.", nameof(state));
            if (value != 0) Interlocked.Add(ref _buckets[index], value);
        }

        if (state.SampleCount != 0)
        {
            UpdateMinimum(ref _minimumTicks, state.MinimumTicks);
            UpdateMaximum(ref _maximumTicks, state.MaximumTicks);
            Interlocked.Add(ref _sumTicks, state.SumTicks);
            Interlocked.Add(ref _count, state.SampleCount);
        }
    }

    internal static FactoryLatencySnapshot Summarize(FactoryLatencyHistogramState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.LayoutVersion != CurrentLayoutVersion || state.BucketCounts.Length != TotalBucketCount)
            throw new ArgumentException("Latency histogram state uses an incompatible layout.", nameof(state));

        // Bucket totals are used for the percentile denominator. A live lock-free
        // capture can differ from SampleCount by an in-flight Record; a quiescent
        // final capture is exact.
        var bucketSamples = state.BucketCounts.Sum();
        if (bucketSamples == 0)
            return new FactoryLatencySnapshot(0, 0, 0, 0, 0, 0, 0, ResolutionDescription());

        var averageCount = state.SampleCount > 0 ? state.SampleCount : bucketSamples;
        return new FactoryLatencySnapshot(
            bucketSamples,
            ToMilliseconds(state.SumTicks / (double)averageCount),
            ToMilliseconds(PercentileUpperBoundTicks(state.BucketCounts, bucketSamples, 0.50, state.MaximumTicks)),
            ToMilliseconds(PercentileUpperBoundTicks(state.BucketCounts, bucketSamples, 0.95, state.MaximumTicks)),
            ToMilliseconds(PercentileUpperBoundTicks(state.BucketCounts, bucketSamples, 0.99, state.MaximumTicks)),
            ToMilliseconds(state.MaximumTicks),
            ToMilliseconds(state.MinimumTicks == long.MaxValue ? 0 : state.MinimumTicks),
            ResolutionDescription());
    }

    private static int FindBucket(long ticks)
    {
        foreach (var segment in Segments)
        {
            if (ticks < segment.EndTicks)
                return segment.Offset + (int)((ticks - segment.StartTicks) / segment.WidthTicks);
        }
        return OverflowBucket;
    }

    private static long PercentileUpperBoundTicks(
        IReadOnlyList<long> buckets,
        long sampleCount,
        double percentile,
        long maximumTicks)
    {
        var rank = Math.Max(1L, (long)Math.Ceiling(sampleCount * percentile));
        long cumulative = 0;
        for (var index = 0; index < buckets.Count; index++)
        {
            cumulative += buckets[index];
            if (cumulative < rank) continue;
            if (index == OverflowBucket) return maximumTicks;
            foreach (var segment in Segments)
            {
                if (index < segment.Offset || index >= segment.Offset + segment.BucketCount) continue;
                var localIndex = index - segment.Offset;
                return Math.Min(segment.EndTicks,
                    segment.StartTicks + ((long)localIndex + 1) * segment.WidthTicks);
            }
        }
        return maximumTicks;
    }

    private static Segment[] CreateSegments()
    {
        var definitions = new (long EndTicks, long WidthTicks)[]
        {
            (TimeSpan.FromMilliseconds(10).Ticks, TicksPerMicrosecond),
            (TimeSpan.FromMilliseconds(100).Ticks, 10 * TicksPerMicrosecond),
            (TimeSpan.FromSeconds(1).Ticks, 100 * TicksPerMicrosecond),
            (TimeSpan.FromSeconds(10).Ticks, TimeSpan.TicksPerMillisecond),
            (TimeSpan.FromSeconds(120).Ticks, 10 * TimeSpan.TicksPerMillisecond)
        };

        var result = new Segment[definitions.Length];
        long start = 0;
        var offset = 0;
        for (var index = 0; index < definitions.Length; index++)
        {
            var definition = definitions[index];
            var span = definition.EndTicks - start;
            if (span <= 0 || span % definition.WidthTicks != 0)
                throw new InvalidOperationException("Latency histogram segment layout is invalid.");
            var count = checked((int)(span / definition.WidthTicks));
            result[index] = new Segment(start, definition.EndTicks, definition.WidthTicks, offset, count);
            start = definition.EndTicks;
            offset = checked(offset + count);
        }
        return result;
    }

    private static string ResolutionDescription() =>
        "nearest-rank bucket upper bound: 1us <10ms; 10us <100ms; 100us <1s; 1ms <10s; 10ms <=120s; exact min/max";

    private static double ToMilliseconds(double ticks) => ticks / TimeSpan.TicksPerMillisecond;

    private static void UpdateMinimum(ref long target, long candidate)
    {
        var observed = Volatile.Read(ref target);
        while (candidate < observed)
        {
            var prior = Interlocked.CompareExchange(ref target, candidate, observed);
            if (prior == observed) return;
            observed = prior;
        }
    }

    private static void UpdateMaximum(ref long target, long candidate)
    {
        var observed = Volatile.Read(ref target);
        while (candidate > observed)
        {
            var prior = Interlocked.CompareExchange(ref target, candidate, observed);
            if (prior == observed) return;
            observed = prior;
        }
    }

    private sealed record Segment(
        long StartTicks,
        long EndTicks,
        long WidthTicks,
        int Offset,
        int BucketCount);
}
