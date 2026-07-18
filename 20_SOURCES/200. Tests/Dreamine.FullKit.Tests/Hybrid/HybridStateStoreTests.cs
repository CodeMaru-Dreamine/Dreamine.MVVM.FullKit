using Dreamine.Hybrid.State;
using System.Threading;

namespace Dreamine.FullKit.Tests.Hybrid;

/// <summary>
/// \if KO
/// <para>Hybrid State Store Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hybrid state store tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class HybridStateStoreTests
{
    /// <summary>
    /// \if KO
    /// <para>State Updates State And Fires Event 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the state updates state and fires event value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Update Applies Transform And Fires Event 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update applies transform and fires event operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Subscribe Returns Disposable That Unsubscribes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the subscribe returns disposable that unsubscribes operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>State Concurrent Calls All Events Delivered 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the state concurrent calls all events delivered value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Set State Concurrent Calls All Events Delivered 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set state concurrent calls all events delivered operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Update Concurrent Calls State Remains Consistent 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update concurrent calls state remains consistent operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Update Concurrent Calls State Remains Consistent 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the update concurrent calls state remains consistent operation.</para>
    /// \endif
    /// </returns>
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
