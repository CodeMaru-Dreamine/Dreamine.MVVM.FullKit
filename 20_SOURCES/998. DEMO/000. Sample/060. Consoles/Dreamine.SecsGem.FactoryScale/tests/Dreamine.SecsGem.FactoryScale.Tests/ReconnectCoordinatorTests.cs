using System.Net;
using System.Net.Sockets;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Tests;

[Collection(FactoryScaleTestCollection.Name)]
public sealed class ReconnectCoordinatorTests
{
    [Fact]
    public async Task Saturation_IsExplicitAndNeverExceedsConfiguredConcurrencyOrQueueCapacity()
    {
        var ports = ReserveThenReleasePorts(3);
        var coordinator = new BoundedReconnectCoordinator(concurrency: 1, capacity: 1);
        var contexts = ports.Select((port, index) => new EquipmentConnectionContext(
            new EquipmentConnectionDefinition
            {
                EquipmentId = $"RECONNECT-{index}",
                Host = "127.0.0.1",
                Port = port,
                Mode = SecsConnectionMode.Active,
                SessionId = 0,
                AutoReconnect = true,
                T3Seconds = 1,
                T5Seconds = 2,
                T6Seconds = 1,
                T7Seconds = 1,
                T8Seconds = 1
            })).ToArray();

        try
        {
            using var connectDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            foreach (var context in contexts)
            {
                await Assert.ThrowsAnyAsync<Exception>(() => context.ConnectAsync(connectDeadline.Token));
                Assert.True(context.ShouldAutoReconnect);
            }

            Assert.True(coordinator.TrySchedule(contexts[0]));
            await WaitUntilAsync(() => coordinator.ActiveCount == 1, TimeSpan.FromSeconds(2));
            Assert.True(coordinator.TrySchedule(contexts[1]));
            Assert.False(coordinator.TrySchedule(contexts[2]));

            Assert.Equal(1, coordinator.ActiveCount);
            Assert.Equal(1, coordinator.QueueDepth);
            Assert.Equal(1, coordinator.RejectedCount);
            Assert.InRange(coordinator.ActiveCount, 0, 1);
            Assert.InRange(coordinator.QueueDepth, 0, 1);
        }
        finally
        {
            await coordinator.DisposeAsync();
            await Task.WhenAll(contexts.Select(context => context.DisposeAsync().AsTask()));
        }
    }

    private static int[] ReserveThenReleasePorts(int count)
    {
        var listeners = Enumerable.Range(0, count).Select(_ =>
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return listener;
        }).ToArray();
        try
        {
            return listeners.Select(listener => ((IPEndPoint)listener.LocalEndpoint).Port).ToArray();
        }
        finally
        {
            foreach (var listener in listeners) listener.Stop();
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition()) await Task.Delay(10, cancellation.Token);
    }
}
