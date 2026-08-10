using Dreamine.SecsGem.FactoryScale.Metrics;

namespace Dreamine.SecsGem.FactoryScale.Tests;

public sealed class LatencyHistogramTests
{
    [Fact]
    public void Capture_UsesNearestRankPercentilesAndExactExtrema()
    {
        var histogram = new FixedMemoryLatencyHistogram();
        foreach (var microseconds in Enumerable.Range(1, 100))
            histogram.Record(TimeSpan.FromTicks(microseconds * 10L));

        var snapshot = histogram.Capture();

        Assert.Equal(100, snapshot.SampleCount);
        Assert.Equal(0.0505, snapshot.AverageMilliseconds, 6);
        // Exact bucket boundaries fall into the following half-open bucket.
        Assert.Equal(0.051, snapshot.P50Milliseconds, 6);
        Assert.Equal(0.096, snapshot.P95Milliseconds, 6);
        Assert.Equal(0.100, snapshot.P99Milliseconds, 6);
        Assert.Equal(0.001, snapshot.MinimumMilliseconds, 6);
        Assert.Equal(0.100, snapshot.MaximumMilliseconds, 6);
    }

    [Fact]
    public void Merge_CombinesFixedMemoryStatesWithoutReplayingSamples()
    {
        var first = new FixedMemoryLatencyHistogram();
        first.Record(TimeSpan.FromMilliseconds(1));
        first.Record(TimeSpan.FromMilliseconds(3));
        var second = new FixedMemoryLatencyHistogram();
        second.Record(TimeSpan.FromMilliseconds(2));
        second.Record(TimeSpan.FromMilliseconds(4));

        first.Merge(second.CaptureState());
        var snapshot = first.Capture();

        Assert.Equal(4, snapshot.SampleCount);
        Assert.Equal(2.5, snapshot.AverageMilliseconds, 6);
        Assert.Equal(2.001, snapshot.P50Milliseconds, 6);
        Assert.Equal(4.001, snapshot.P95Milliseconds, 6);
        Assert.Equal(4.001, snapshot.P99Milliseconds, 6);
        Assert.Equal(1.0, snapshot.MinimumMilliseconds, 6);
        Assert.Equal(4.0, snapshot.MaximumMilliseconds, 6);
        Assert.Equal(second.BucketCount, first.BucketCount);
    }

    [Fact]
    public void OverflowBucket_PreservesExactMaximum()
    {
        var histogram = new FixedMemoryLatencyHistogram();
        histogram.Record(TimeSpan.FromMinutes(3));

        var snapshot = histogram.Capture();

        Assert.Equal(TimeSpan.FromMinutes(3).TotalMilliseconds, snapshot.P99Milliseconds);
        Assert.Equal(TimeSpan.FromMinutes(3).TotalMilliseconds, snapshot.MaximumMilliseconds);
    }

    [Fact]
    public void Merge_RejectsIncompatibleOrCorruptState()
    {
        var histogram = new FixedMemoryLatencyHistogram();
        var valid = histogram.CaptureState();

        Assert.Throws<ArgumentException>(() => histogram.Merge(valid with { LayoutVersion = 999 }));
        Assert.Throws<ArgumentException>(() => histogram.Merge(valid with { BucketCounts = [-1] }));
        Assert.Throws<ArgumentException>(() => histogram.Merge(valid with { SampleCount = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => histogram.RecordTicks(-1));
    }
}
