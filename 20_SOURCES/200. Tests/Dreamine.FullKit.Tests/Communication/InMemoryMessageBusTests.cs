using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Buses;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>In Memory Message Bus Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory message bus tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class InMemoryMessageBusTests
{
    /// <summary>
    /// \if KO
    /// <para>Publish Async Dispatches To Matching Route When Connected 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish async dispatches to matching route when connected operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Publish Async Dispatches To Matching Route When Connected 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish async dispatches to matching route when connected operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task PublishAsync_DispatchesToMatchingRouteWhenConnected()
    {
        var bus = new InMemoryMessageBus();
        var received = new List<string>();
        await bus.SubscribeAsync("route.a", (message, _) =>
        {
            received.Add(message.Name);
            return Task.CompletedTask;
        });

        await bus.ConnectAsync();
        await bus.PublishAsync(new MessageEnvelope { Route = "route.a", Name = "Ping" });
        await bus.PublishAsync(new MessageEnvelope { Route = "route.b", Name = "Ignored" });

        Assert.Equal(ConnectionState.Connected, bus.State);
        Assert.Equal(new[] { "Ping" }, received);
    }

    /// <summary>
    /// \if KO
    /// <para>Publish Async Throws When Disconnected 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the publish async throws when disconnected operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Publish Async Throws When Disconnected 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the publish async throws when disconnected operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task PublishAsync_ThrowsWhenDisconnected()
    {
        var bus = new InMemoryMessageBus();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bus.PublishAsync(new MessageEnvelope { Route = "route" }));
    }

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Dispose Async Clears Subscriptions And Disconnects 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the dispose async clears subscriptions and disconnects operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task DisposeAsync_ClearsSubscriptionsAndDisconnects()
    {
        var bus = new InMemoryMessageBus();
        var count = 0;
        await bus.SubscribeAsync("route", (_, _) =>
        {
            count++;
            return Task.CompletedTask;
        });

        await bus.ConnectAsync();
        await bus.DisposeAsync();
        await bus.ConnectAsync();
        await bus.PublishAsync(new MessageEnvelope { Route = "route" });

        Assert.Equal(0, count);
    }
}
