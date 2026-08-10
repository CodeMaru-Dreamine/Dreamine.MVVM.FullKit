using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.FactoryScale.Runtime;
using Dreamine.SecsGem.FactoryScale.Simulation;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Tests;

[Collection(FactoryScaleTestCollection.Name)]
public sealed class FactoryTcpIntegrationTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(250)]
    [Trait("Category", "FactoryScaleSmoke")]
    public async Task LoopbackFleet_ConnectsLinksAndExchangesOneMessagePerEquipment(int equipmentCount)
    {
        var baselineHostSessions = EquipmentConnectionContext.LiveSessionCount;
        var baselinePeerSessions = FactoryEquipmentPeer.LiveSessionCount;
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        await RunFleetAsync(equipmentCount, deadline.Token, async (fleet, runtime, token) =>
        {
            Assert.Equal(equipmentCount, runtime.Connections.Count);
            Assert.All(runtime.Connections, connection =>
            {
                Assert.True(connection.IsConnected, connection.EquipmentId);
                Assert.True(connection.IsSelected, connection.EquipmentId);
            });

            var linktests = await runtime.LinktestAllAsync(token);
            Assert.Equal(equipmentCount, linktests.Count);
            Assert.All(linktests.Values, result => Assert.True(result.Succeeded, result.Detail));

            var responses = await runtime.BroadcastAsync(1, 1, _ => null, token);
            Assert.Equal(equipmentCount, responses.Count);
            Assert.All(responses, result => Assert.True(result.Succeeded, result.Error));
            Assert.Equal(equipmentCount, responses.Select(result => result.EquipmentId).Distinct().Count());
            Assert.Equal(0, fleet.BusinessRejectCount);

            var snapshot = runtime.CaptureSnapshot(fleet.LiveSessionCount, fleet.ListenerCount,
                fleet.CaptureQueueMetrics());
            Assert.Equal(equipmentCount, snapshot.RequestedEquipment);
            Assert.Equal(equipmentCount, snapshot.ConnectedEquipment);
            Assert.Equal(equipmentCount, snapshot.SelectedEquipment);
            Assert.Equal(equipmentCount, snapshot.Responses);
            Assert.Equal(0, snapshot.CorrelationErrors);
            Assert.All(snapshot.Queues, queue => Assert.InRange(queue.PeakDepth, 0, queue.Capacity));
        });

        await WaitUntilAsync(
            () => EquipmentConnectionContext.LiveSessionCount == baselineHostSessions &&
                  FactoryEquipmentPeer.LiveSessionCount == baselinePeerSessions,
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    [Trait("Category", "Isolation")]
    public async Task SameSessionIdAndSystemBytes_AreCorrelatedInsideEachEquipmentContext()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await RunFleetAsync(2, deadline.Token, async (_, runtime, token) =>
        {
            var sharedSystemBytes = new SecsSystemBytes(0x0102_0304);
            var requests = runtime.Connections.Select(connection => runtime.RequestAsync(
                connection.EquipmentId, 1, 1, null, token, sharedSystemBytes));
            var responses = await Task.WhenAll(requests);

            Assert.All(responses, response =>
            {
                Assert.True(response.Succeeded, response.Error);
                Assert.Equal(sharedSystemBytes, response.Response!.SystemBytes);
                var list = Assert.IsType<SecsListItem>(response.Response.Item);
                var equipmentId = Assert.IsType<SecsAsciiItem>(list.Items[0]).Value;
                Assert.Equal(response.EquipmentId, equipmentId);
            });
            Assert.Equal(2, responses.Select(response => response.Response!.SystemBytes).Count());
            Assert.Equal(2, responses.Select(response => response.EquipmentId).Distinct().Count());
            Assert.Equal(0, runtime.Metrics.CaptureSnapshot().CorrelationErrors);
        });
    }

    [Fact]
    [Trait("Category", "Isolation")]
    public async Task OneNoResponseEquipment_TimesOutWithoutAffectingHealthyEquipment()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await RunFleetAsync(4, deadline.Token, async (_, runtime, token) =>
        {
            var responses = await runtime.BroadcastAsync(1, 1, _ => null, token);
            var failed = Assert.Single(responses, result => !result.Succeeded);
            Assert.Equal("EQ-0002", failed.EquipmentId);
            Assert.Contains("exceeded", failed.Error, StringComparison.OrdinalIgnoreCase);

            var healthy = responses.Where(result => result.EquipmentId != "EQ-0002").ToArray();
            Assert.Equal(3, healthy.Length);
            Assert.All(healthy, result => Assert.True(result.Succeeded, result.Error));

            var followUp = await runtime.RequestAsync("EQ-0003", 1, 1, null, token);
            Assert.True(followUp.Succeeded, followUp.Error);
            Assert.True(runtime.Host.Get("EQ-0001").IsSelected);
            Assert.True(runtime.Host.Get("EQ-0003").IsSelected);
            Assert.True(runtime.Host.Get("EQ-0004").IsSelected);
        }, number => number == 2 ? FactoryEquipmentBehavior.NoResponse : FactoryEquipmentBehavior.Normal,
            t3Seconds: 1);
    }

    [Fact]
    [Trait("Category", "Reconnect")]
    public async Task ReconnectStorm_RespectsConcurrencyAndMessageQueueBounds()
    {
        const int equipmentCount = 12;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        await RunFleetAsync(equipmentCount, deadline.Token, async (fleet, runtime, token) =>
        {
            var targets = runtime.Connections.Take(8).Select(value => value.EquipmentId).ToArray();
            var disconnected = await runtime.DisconnectAsync(targets, token);
            Assert.All(disconnected.Values, result => Assert.True(result.Succeeded, result.Detail));

            await fleet.RestartAsync(targets, token);
            var reconnected = await runtime.ConnectReconnectTargetsAsync(targets, token);
            Assert.All(reconnected.Values, result => Assert.True(result.Succeeded, result.Detail));
            Assert.InRange(runtime.PeakReconnectOperationCount, 1, 2);

            var responses = await runtime.BroadcastAsync(1, 1, _ => null, token);
            Assert.All(responses, result => Assert.True(result.Succeeded, result.Error));
            Assert.InRange(runtime.PeakMessageOperationCount, 1, 2);
            Assert.InRange(runtime.PeakMessageQueueDepth, 1, 4);
            Assert.Equal(0, runtime.PendingTransactionCount);
            Assert.Equal(0, runtime.MessageQueueDepth);
        }, _ => FactoryEquipmentBehavior.SlowResponse,
            runtimeOptions: new FactoryHostRuntimeOptions(
                ConnectConcurrency: 8,
                ReconnectConcurrency: 2,
                MessageConcurrency: 2,
                MessageQueueCapacity: 4,
                PerEquipmentPendingLimit: 1,
                GlobalPendingLimit: 2,
                DiagnosticQueueCapacity: 64));
    }

    [Fact]
    [Trait("Category", "Backpressure")]
    public async Task PerEquipmentAdmission_PrecedesGlobalPendingAccounting()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await RunFleetAsync(1, deadline.Token, async (_, runtime, token) =>
        {
            var requests = Enumerable.Range(0, 4)
                .Select(_ => runtime.RequestAsync("EQ-0001", 1, 1, null, token))
                .ToArray();
            var responses = await Task.WhenAll(requests);

            Assert.All(responses, response => Assert.True(response.Succeeded, response.Error));
            Assert.Equal(1, runtime.PeakPendingTransactionCount);
            Assert.Equal(0, runtime.PendingTransactionCount);
        }, _ => FactoryEquipmentBehavior.SlowResponse,
            runtimeOptions: new FactoryHostRuntimeOptions(
                ConnectConcurrency: 1,
                ReconnectConcurrency: 1,
                MessageConcurrency: 4,
                MessageQueueCapacity: 8,
                PerEquipmentPendingLimit: 1,
                GlobalPendingLimit: 4,
                DiagnosticQueueCapacity: 64));
    }

    [Fact]
    [Trait("Category", "Lifecycle")]
    public async Task ForcedDispose_CompletesEveryAcceptedMessageAndStopsWorkers()
    {
        var baselineHostWorkers = FactoryHostRuntime.LiveMessageWorkerCount;
        var baselineResponderWorkers = FactoryEquipmentFleet.LiveResponderWorkerCount;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var fleet = new FactoryEquipmentFleet(new FactoryEquipmentFleetOptions(
            ReceiveQueueCapacity: 16,
            ResponderCount: 1,
            StartConcurrency: 1,
            T3Seconds: 10,
            T5Seconds: 1,
            T6Seconds: 3,
            T7Seconds: 3,
            T8Seconds: 3), behaviorSelector: _ => FactoryEquipmentBehavior.NoResponse);
        var runtime = new FactoryHostRuntime(new FactoryHostRuntimeOptions(
                ConnectConcurrency: 1,
                ReconnectConcurrency: 1,
                MessageConcurrency: 1,
                MessageQueueCapacity: 16,
                PerEquipmentPendingLimit: 1,
                GlobalPendingLimit: 1,
                DiagnosticQueueCapacity: 32),
            gracefulShutdownTimeout: TimeSpan.FromMilliseconds(100),
            forcedShutdownTimeout: TimeSpan.FromSeconds(5));
        Task<FactoryMessageResult>[] requests = [];
        try
        {
            Assert.Equal(baselineHostWorkers + 1, FactoryHostRuntime.LiveMessageWorkerCount);
            Assert.Equal(baselineResponderWorkers + 1, FactoryEquipmentFleet.LiveResponderWorkerCount);
            await fleet.StartAsync(1, 1, deadline.Token);
            runtime.AddEquipment(fleet.Definitions);
            var connected = await runtime.ConnectAllAsync(deadline.Token);
            Assert.True(connected["EQ-0001"].Succeeded, connected["EQ-0001"].Detail);
            await fleet.WaitForConnectionsAsync(deadline.Token);

            requests = Enumerable.Range(0, 8)
                .Select(_ => runtime.RequestAsync("EQ-0001", 1, 1, null, CancellationToken.None))
                .ToArray();
            await WaitUntilAsync(() => runtime.MessageQueueDepth >= 6, TimeSpan.FromSeconds(5));

            var cleanup = await Assert.ThrowsAsync<AggregateException>(() => runtime.DisposeAsync().AsTask());
            Assert.Contains(cleanup.InnerExceptions, exception => exception is TimeoutException);
            await ObserveAllAsync(requests).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.All(requests, request => Assert.True(request.IsCompleted));
            Assert.Equal(0, runtime.OutstandingMessageCount);
            Assert.Equal(0, runtime.MessageQueueDepth);
            Assert.Equal(baselineHostWorkers, FactoryHostRuntime.LiveMessageWorkerCount);
        }
        finally
        {
            try { await runtime.DisposeAsync(); } catch { }
            await fleet.DisposeAsync();
        }

        Assert.Equal(baselineHostWorkers, FactoryHostRuntime.LiveMessageWorkerCount);
        Assert.Equal(baselineResponderWorkers, FactoryEquipmentFleet.LiveResponderWorkerCount);
    }

    private static async Task RunFleetAsync(
        int equipmentCount,
        CancellationToken cancellationToken,
        Func<FactoryEquipmentFleet, FactoryHostRuntime, CancellationToken, Task> assertion,
        Func<int, FactoryEquipmentBehavior>? behaviorSelector = null,
        int t3Seconds = 3,
        FactoryHostRuntimeOptions? runtimeOptions = null)
    {
        await using var fleet = new FactoryEquipmentFleet(new FactoryEquipmentFleetOptions(
            ReceiveQueueCapacity: Math.Max(512, equipmentCount * 2),
            ResponderCount: Math.Min(32, Math.Max(2, equipmentCount)),
            StartConcurrency: Math.Min(64, equipmentCount),
            T3Seconds: t3Seconds,
            T5Seconds: 1,
            T6Seconds: 3,
            T7Seconds: 3,
            T8Seconds: 3), behaviorSelector: behaviorSelector);
        await using var runtime = new FactoryHostRuntime(runtimeOptions ?? new FactoryHostRuntimeOptions(
            ConnectConcurrency: Math.Min(64, equipmentCount),
            ReconnectConcurrency: Math.Min(16, equipmentCount),
            MessageConcurrency: Math.Min(64, equipmentCount),
            MessageQueueCapacity: Math.Max(512, equipmentCount * 2),
            PerEquipmentPendingLimit: 4,
            GlobalPendingLimit: Math.Max(64, equipmentCount),
            DiagnosticQueueCapacity: Math.Max(512, equipmentCount * 2)));

        await fleet.StartAsync(1, equipmentCount, cancellationToken);
        runtime.AddEquipment(fleet.Definitions);
        var connected = await runtime.ConnectAllAsync(cancellationToken);
        Assert.Equal(equipmentCount, connected.Count);
        Assert.All(connected.Values, result => Assert.True(result.Succeeded, result.Detail));
        await fleet.WaitForConnectionsAsync(cancellationToken);
        await assertion(fleet, runtime, cancellationToken);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!condition()) await Task.Delay(20, cancellation.Token);
    }

    private static async Task ObserveAllAsync(IEnumerable<Task<FactoryMessageResult>> requests)
    {
        foreach (var request in requests)
        {
            try { _ = await request.ConfigureAwait(false); }
            catch (Exception) { }
        }
    }
}
