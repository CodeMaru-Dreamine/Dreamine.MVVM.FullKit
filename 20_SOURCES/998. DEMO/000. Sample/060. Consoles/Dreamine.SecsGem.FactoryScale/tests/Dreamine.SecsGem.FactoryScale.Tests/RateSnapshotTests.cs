using Dreamine.SecsGem.FactoryScale.Export;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.FactoryScale.Scenarios;

namespace Dreamine.SecsGem.FactoryScale.Tests;

[Collection(FactoryScaleTestCollection.Name)]
public sealed class RateSnapshotTests
{
    [Fact]
    public async Task SustainedProfileFinalRetainsLatestMeasuredIntervalRate()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        await using var exporter = new FactoryResultExporter(capacity: 4);
        var coordinator = new FactoryScenarioCoordinator();

        var result = await coordinator.RunInProcessAsync(new FactoryScenarioRunOptions
        {
            Scenario = "factory-normal",
            ExecutionMode = "in-process",
            EquipmentCount = 2,
            DisconnectCount = 1,
            Duration = TimeSpan.FromSeconds(2),
            MessagesPerSecond = 10,
            SnapshotInterval = TimeSpan.FromSeconds(1),
            ConnectConcurrency = 2,
            ReconnectConcurrency = 1,
            MessageConcurrency = 4,
            ReceiveQueueCapacity = 16,
            LogQueueCapacity = 16,
            ExportQueueCapacity = 4,
            PerEquipmentPendingLimit = 2,
            GlobalPendingLimit = 4
        }, exporter, deadline.Token);

        Assert.Equal(FactoryRunStatus.Passed, result.Status);
        Assert.True(result.Final.RequestsPerSecond > 0, "Final request rate should retain the latest active interval.");
        Assert.True(result.Final.ResponsesPerSecond > 0, "Final response rate should retain the latest active interval.");
        Assert.True(result.Final.MessagesPerSecond > 0, "Final message rate should retain the latest active interval.");
        Assert.Equal(result.Final.RequestsPerSecond + result.Final.ResponsesPerSecond,
            result.Final.MessagesPerSecond, precision: 8);
    }
}
