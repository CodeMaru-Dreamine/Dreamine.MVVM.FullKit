using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Buses;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class InMemoryMessageBusTests
{
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

    [Fact]
    public async Task PublishAsync_ThrowsWhenDisconnected()
    {
        var bus = new InMemoryMessageBus();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => bus.PublishAsync(new MessageEnvelope { Route = "route" }));
    }

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
