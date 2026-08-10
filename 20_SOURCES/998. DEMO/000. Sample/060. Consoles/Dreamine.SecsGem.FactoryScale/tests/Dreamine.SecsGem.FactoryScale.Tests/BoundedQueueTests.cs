using Dreamine.SecsGem.FactoryScale.Infrastructure;

namespace Dreamine.SecsGem.FactoryScale.Tests;

public sealed class BoundedQueueTests
{
    [Fact]
    public async Task WaitPolicy_AppliesBackpressureWithoutLosingAcceptedWork()
    {
        var queue = new BoundedWorkQueue<int>("business", 1, BoundedBusinessQueueFullPolicy.Wait,
            singleReader: true, singleWriter: true);

        Assert.True(await queue.EnqueueAsync(1));
        var blockedWrite = queue.EnqueueAsync(2).AsTask();
        await Task.Yield();
        Assert.False(blockedWrite.IsCompleted);

        await using var reader = queue.ReadAllAsync().GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(1, reader.Current);
        Assert.True(await blockedWrite.WaitAsync(TimeSpan.FromSeconds(2)));

        queue.Complete();
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(2, reader.Current);
        Assert.False(await reader.MoveNextAsync());
        await queue.Completion;

        Assert.Equal(2, queue.AcceptedCount);
        Assert.Equal(2, queue.DequeuedCount);
        Assert.Equal(0, queue.RejectedCount);
        Assert.Equal(1, queue.PeakDepth);
    }

    [Fact]
    public async Task WaitPolicy_CancelledProducerDoesNotBecomeAcceptedOrRejectedWork()
    {
        var queue = new BoundedWorkQueue<int>("business", 1, BoundedBusinessQueueFullPolicy.Wait);
        Assert.True(await queue.EnqueueAsync(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queue.EnqueueAsync(2, cancellation.Token).AsTask());

        Assert.Equal(1, queue.AcceptedCount);
        Assert.Equal(0, queue.RejectedCount);
        Assert.Equal(1, queue.Depth);
        queue.Complete();
        await DrainAsync(queue);
    }

    [Fact]
    public async Task RejectPolicy_ReportsEveryRejectedBusinessItemExplicitly()
    {
        var queue = new BoundedWorkQueue<int>("business", 1, BoundedBusinessQueueFullPolicy.Reject);

        Assert.True(queue.TryEnqueue(1));
        Assert.False(queue.TryEnqueue(2));
        Assert.False(await queue.EnqueueAsync(3));

        Assert.Equal(1, queue.AcceptedCount);
        Assert.Equal(2, queue.RejectedCount);
        Assert.Equal(1, queue.Depth);
        queue.Complete();
        await DrainAsync(queue);
    }

    [Fact]
    public async Task DiagnosticQueue_CountsEveryDropExactlyUnderContention()
    {
        const int attempts = 10_000;
        var queue = new BoundedDiagnosticQueue<int>("diagnostic", 1, singleReader: true);
        Assert.True(queue.TryWrite(-1));

        Parallel.For(0, attempts, value => Assert.False(queue.TryWrite(value)));

        Assert.Equal(1, queue.AcceptedCount);
        Assert.Equal(attempts, queue.DroppedCount);
        Assert.Equal(1, queue.Depth);
        Assert.Equal(1, queue.PeakDepth);

        queue.Complete();
        var values = new List<int>();
        await foreach (var value in queue.ReadAllAsync()) values.Add(value);
        Assert.Equal([-1], values);
        Assert.Equal(1, queue.DequeuedCount);
        Assert.Equal(attempts, queue.CaptureMetrics().Dropped);
    }

    [Fact]
    public async Task ReaderCancellation_StopsEnumerationWithoutCompletingTheQueue()
    {
        var queue = new BoundedDiagnosticQueue<int>("diagnostic", 2);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in queue.ReadAllAsync(cancellation.Token)) { }
        });

        Assert.False(queue.Completion.IsCompleted);
        Assert.True(queue.TryWrite(1));
        queue.Complete();
        await foreach (var _ in queue.ReadAllAsync()) { }
        await queue.Completion;
    }

    private static async Task DrainAsync<T>(BoundedWorkQueue<T> queue)
    {
        await foreach (var _ in queue.ReadAllAsync()) { }
        await queue.Completion;
    }
}
