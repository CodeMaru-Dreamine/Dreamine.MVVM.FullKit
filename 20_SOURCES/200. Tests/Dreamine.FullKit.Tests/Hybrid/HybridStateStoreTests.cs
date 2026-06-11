using Dreamine.Hybrid.State;
using System.Threading;

namespace Dreamine.FullKit.Tests.Hybrid;

public sealed class HybridStateStoreTests
{
    [Fact]
    public void SetState_UpdatesStateAndFiresEvent()
    {
        var store = new HybridStateStore<int>(0);
        var received = new List<int>();
        store.StateChanged += (_, e) => received.Add(e.State);

        store.SetState(1);
        store.SetState(2);

        Assert.Equal(2, store.State);
        Assert.Equal(new[] { 1, 2 }, received);
    }

    [Fact]
    public void Update_AppliesTransformAndFiresEvent()
    {
        var store = new HybridStateStore<int>(10);
        var received = new List<int>();
        store.StateChanged += (_, e) => received.Add(e.State);

        store.Update(x => x + 5);

        Assert.Equal(15, store.State);
        Assert.Single(received);
        Assert.Equal(15, received[0]);
    }

    [Fact]
    public void Subscribe_ReturnsDisposableThatUnsubscribes()
    {
        var store = new HybridStateStore<int>(0);
        var count = 0;
        var sub = store.Subscribe((_, _) => count++);

        store.SetState(1);
        sub.Dispose();
        store.SetState(2);

        Assert.Equal(1, count);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var store = new HybridStateStore<int>(0);
        var count = 0;
        var sub = store.Subscribe((_, _) => count++);

        sub.Dispose();
        sub.Dispose(); // second dispose must not throw or re-unsubscribe

        store.SetState(1);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SetState_ConcurrentCalls_AllEventsDelivered()
    {
        const int iterations = 200;
        var store = new HybridStateStore<int>(0);
        var eventCount = 0;

        store.StateChanged += (_, _) => Interlocked.Increment(ref eventCount);

        var tasks = Enumerable.Range(1, iterations)
            .Select(i => Task.Run(() => store.SetState(i)));

        await Task.WhenAll(tasks);

        // Every SetState must fire exactly one event — no event may be lost.
        Assert.Equal(iterations, eventCount);
    }

    [Fact]
    public async Task Update_ConcurrentCalls_StateRemainsConsistent()
    {
        var store = new HybridStateStore<int>(0);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => store.Update(x => x + 1)));

        await Task.WhenAll(tasks);

        Assert.Equal(100, store.State);
    }
}
