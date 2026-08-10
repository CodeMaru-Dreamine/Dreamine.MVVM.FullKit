using System.Diagnostics;
using Dreamine.SecsGem.FactoryScale.Export;
using Dreamine.SecsGem.FactoryScale.Metrics;
using Dreamine.SecsGem.FactoryScale.Scenarios;

namespace Dreamine.SecsGem.FactoryScale.Tests;

[Collection(FactoryScaleTestCollection.Name)]
public sealed class ExporterLifecycleTests
{
    [Fact]
    public async Task Dispose_DrainsAcceptedWorkAndCompletesEveryCaller()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = 0;
        var exporter = new FactoryResultExporter(
            capacity: 2,
            drainTimeout: TimeSpan.FromSeconds(2),
            cancellationTimeout: TimeSpan.FromSeconds(1),
            writer: async (_, _, cancellationToken) =>
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(cancellationToken);
                Interlocked.Increment(ref writes);
            });
        var snapshot = new FactoryMetricsCollector().CaptureSnapshot();
        var calls = Enumerable.Range(0, 3)
            .Select(index => exporter.ExportSnapshotAsync(ArtifactPath(index), snapshot, CancellationToken.None))
            .ToArray();

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var dispose = exporter.DisposeAsync().AsTask();
        release.TrySetResult();

        await Task.WhenAll(calls.Append(dispose)).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, Volatile.Read(ref writes));
        Assert.Equal(0, FactoryResultExporter.LiveWorkerCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            exporter.ExportSnapshotAsync(ArtifactPath(4), snapshot, CancellationToken.None));
    }

    [Fact]
    public async Task Dispose_WhenWriterIgnoresCancellation_IsBoundedAndFailsAllQueuedCallers()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exporter = new FactoryResultExporter(
            capacity: 2,
            drainTimeout: TimeSpan.FromMilliseconds(40),
            cancellationTimeout: TimeSpan.FromMilliseconds(40),
            writer: (_, _, _) =>
            {
                entered.TrySetResult();
                return release.Task;
            });
        var snapshot = new FactoryMetricsCollector().CaptureSnapshot();
        var calls = Enumerable.Range(0, 3)
            .Select(index => exporter.ExportSnapshotAsync(ArtifactPath(index), snapshot, CancellationToken.None))
            .ToArray();

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var timer = Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(() => exporter.DisposeAsync().AsTask());
        timer.Stop();

        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2), $"Dispose took {timer.Elapsed}.");
        Assert.All(calls, call => Assert.True(call.IsCompleted));
        await Assert.ThrowsAnyAsync<Exception>(() => Task.WhenAll(calls));

        release.TrySetResult();
        await WaitUntilAsync(() => FactoryResultExporter.LiveWorkerCount == 0, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CleanupSnapshot_SeparatesCallerOwnedExporterFromScenarioOperations()
    {
        var metrics = new FactoryMetricsCollector();
        var before = FactoryCleanupSnapshot.Capture(metrics);
        await using (var exporter = new FactoryResultExporter(capacity: 1))
        {
            var during = FactoryCleanupSnapshot.Capture(metrics);
            Assert.Equal(before.TrackedOperations, during.TrackedOperations);
            Assert.Equal(before.OwnedWorkers.ResultExportWorkers + 1,
                during.OwnedWorkers.ResultExportWorkers);
            Assert.False(FactoryScenarioCoordinator.HasFactoryResourceLeak(during, during));
        }

        var after = FactoryCleanupSnapshot.Capture(metrics);
        Assert.Equal(before.OwnedWorkers.ResultExportWorkers, after.OwnedWorkers.ResultExportWorkers);
        Assert.Equal(before.TrackedOperations, after.TrackedOperations);
    }

    private static string ArtifactPath(int index) =>
        Path.Combine(Path.GetTempPath(), $"factory-export-test-{index}-{Guid.NewGuid():N}.json");

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var timer = Stopwatch.StartNew();
        while (!predicate() && timer.Elapsed < timeout) await Task.Delay(10);
        Assert.True(predicate());
    }
}
