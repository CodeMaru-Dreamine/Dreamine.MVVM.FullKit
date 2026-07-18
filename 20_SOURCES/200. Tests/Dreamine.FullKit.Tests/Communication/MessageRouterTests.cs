using Dreamine.Communication.Abstractions.Models;
using Dreamine.Communication.Core.Routing;

namespace Dreamine.FullKit.Tests.Communication;

/// <summary>
/// \if KO
/// <para>Message Router Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates message router tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MessageRouterTests
{
    /// <summary>
    /// \if KO
    /// <para>Route Async Invokes Handlers In Registration Order 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the route async invokes handlers in registration order operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Route Async Invokes Handlers In Registration Order 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the route async invokes handlers in registration order operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Clear Removes Registered Handlers 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear removes registered handlers operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Clear Removes Registered Handlers 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the clear removes registered handlers operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Unregister Removes Only Selected Handler 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unregister removes only selected handler operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Unregister Removes Only Selected Handler 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the unregister removes only selected handler operation.</para>
    /// \endif
    /// </returns>
    [Fact]
    public async Task Unregister_RemovesOnlySelectedHandler()
    {
        var router = new MessageRouter();
        var calls = new List<string>();
        Func<MessageEnvelope, CancellationToken, Task> first = (_, _) =>
        {
            calls.Add("first");
            return Task.CompletedTask;
        };
        Func<MessageEnvelope, CancellationToken, Task> second = (_, _) =>
        {
            calls.Add("second");
            return Task.CompletedTask;
        };

        router.Register("route", first);
        router.Register("route", second);

        Assert.True(router.Unregister("route", first));

        await router.RouteAsync(new MessageEnvelope { Route = "route" });

        Assert.Equal(["second"], calls);
    }
}
