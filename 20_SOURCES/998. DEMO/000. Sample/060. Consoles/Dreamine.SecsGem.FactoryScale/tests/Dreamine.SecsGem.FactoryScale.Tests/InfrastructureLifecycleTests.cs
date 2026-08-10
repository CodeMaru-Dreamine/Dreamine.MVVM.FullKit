using System.Net;
using System.Net.Sockets;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Tests;

public sealed class InfrastructureLifecycleTests
{
    [Fact]
    public async Task PortManager_ReturnsOnlyAfterTheCallbackOwnsThePort()
    {
        var port = FindUnusedPort();
        var manager = new ValidatedPortRangeManager(port, port, seed: 0);
        TcpListener? listener = null;
        try
        {
            listener = await manager.StartBoundAsync((candidate, _) =>
            {
                var bound = new TcpListener(IPAddress.Loopback, candidate);
                bound.Start();
                return Task.FromResult(bound);
            }, CancellationToken.None);

            Assert.Equal(port, ((IPEndPoint)listener.LocalEndpoint).Port);
            Assert.Equal(0, manager.CollisionCount);
            Assert.Throws<SocketException>(() => new TcpListener(IPAddress.Loopback, port).Start());
        }
        finally
        {
            listener?.Stop();
        }
    }

    [Fact]
    public async Task PortManager_ReportsAnExhaustedBoundRangeAsCollisions()
    {
        var port = FindUnusedPort();
        using var occupied = new TcpListener(IPAddress.Loopback, port);
        occupied.Start();
        var manager = new ValidatedPortRangeManager(port, port, seed: 0);

        await Assert.ThrowsAsync<IOException>(() => manager.StartBoundAsync<object>((candidate, _) =>
        {
            var listener = new TcpListener(IPAddress.Loopback, candidate);
            try
            {
                listener.Start();
                return Task.FromResult<object>(listener);
            }
            catch
            {
                listener.Stop();
                throw;
            }
        }, CancellationToken.None));

        Assert.Equal(1, manager.CollisionCount);
    }

    [Fact]
    public async Task FactoryRuntime_DisposeIsIdempotentAndRejectsNewWork()
    {
        var runtime = new FactoryHostRuntime(new FactoryHostRuntimeOptions(
            ConnectConcurrency: 1,
            ReconnectConcurrency: 1,
            MessageConcurrency: 1,
            MessageQueueCapacity: 1,
            PerEquipmentPendingLimit: 1,
            GlobalPendingLimit: 1,
            DiagnosticQueueCapacity: 1));

        var first = runtime.DisposeAsync().AsTask();
        var second = runtime.DisposeAsync().AsTask();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            runtime.RequestAsync("absent", 1, 1, null, CancellationToken.None));
        Assert.Equal(0, runtime.ActiveMessageOperationCount);
        Assert.Equal(0, runtime.PendingTransactionCount);
    }

    private static int FindUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }
}
