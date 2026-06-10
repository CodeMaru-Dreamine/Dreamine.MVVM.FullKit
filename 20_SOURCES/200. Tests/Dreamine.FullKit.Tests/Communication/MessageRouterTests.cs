using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Routing;

namespace Dreamine.FullKit.Tests.Communication;

public sealed class MessageRouterTests
{
    [Fact]
    public async Task RouteAsync_InvokesHandlersInRegistrationOrder()
    {
        var router = new MessageRouter();
        var calls = new List<string>();
        router.Register("route", (_, _) =>
        {
            calls.Add("first");
            return Task.CompletedTask;
        });
        router.Register("route", (_, _) =>
        {
            calls.Add("second");
            return Task.CompletedTask;
        });

        await router.RouteAsync(new MessageEnvelope { Route = "route" });

        Assert.Equal(new[] { "first", "second" }, calls);
    }

    [Fact]
    public async Task Clear_RemovesRegisteredHandlers()
    {
        var router = new MessageRouter();
        var called = false;
        router.Register("route", (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        router.Clear();
        await router.RouteAsync(new MessageEnvelope { Route = "route" });

        Assert.False(called);
    }
}
