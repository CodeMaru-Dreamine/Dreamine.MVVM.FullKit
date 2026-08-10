using System.Text.Json;
using Dreamine.SecsGem.FactoryScale.Export;
using Dreamine.SecsGem.FactoryScale.Models;
using Dreamine.SecsGem.FactoryScale.Scenarios;

namespace Dreamine.SecsGem.FactoryScale.Tests;

[Collection(FactoryScaleTestCollection.Name)]
public sealed class ProtocolFaultScenarioTests
{
    [Fact]
    [Trait("Category", "Isolation")]
    public async Task FaultIsolation_IdentifiesEachProtocolFailureAndExportsPerEquipmentEvidence()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"dreamine-factory-fault-{Guid.NewGuid():N}.json");
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            FactoryRunResult result;
            await using (var exporter = new FactoryResultExporter(capacity: 4))
            {
                var coordinator = new FactoryScenarioCoordinator();
                result = await coordinator.RunInProcessAsync(new FactoryScenarioRunOptions
                {
                    Scenario = "fault-isolation",
                    ExecutionMode = "in-process",
                    EquipmentCount = 6,
                    DisconnectCount = 1,
                    PortRangeStart = 20_000,
                    PortRangeEnd = 48_000,
                    ConnectConcurrency = 6,
                    ReconnectConcurrency = 2,
                    MessageConcurrency = 6,
                    ReceiveQueueCapacity = 64,
                    LogQueueCapacity = 64,
                    ExportQueueCapacity = 4,
                    PerEquipmentPendingLimit = 2,
                    GlobalPendingLimit = 12
                }, exporter, deadline.Token);

                Assert.True(result.Status == FactoryRunStatus.Passed,
                    result.FirstFailure ?? $"Unexpected result status {result.Status}.");
                Assert.Null(result.FirstFailure);
                Assert.Equal(6, result.EquipmentMetrics.Count);
                Assert.Equal(6, result.EquipmentMetrics.Select(value => value.ConnectionId).Distinct().Count());
                Assert.Contains(result.Checks, value => value.StartsWith("T6:", StringComparison.Ordinal));
                Assert.Contains(result.Checks, value => value.StartsWith("T7:", StringComparison.Ordinal));
                Assert.Contains(result.Checks, value => value.StartsWith("T8:", StringComparison.Ordinal));
                Assert.Contains(result.Checks, value => value.Contains("Unknown HSMS SType", StringComparison.Ordinal));
                Assert.Contains(result.Checks, value => value.Contains("large message send", StringComparison.Ordinal));
                Assert.Equal(0, result.CleanupBeforeForcedGc.TrackedSessions);
                Assert.Equal(0, result.CleanupBeforeForcedGc.TrackedOperations);
                Assert.Equal(result.Snapshots[0].Process.Sockets.OpenSocketCount,
                    result.CleanupBeforeForcedGc.Process.Sockets.OpenSocketCount);
                Assert.Equal(result.Snapshots[0].Process.Sockets.ListenerCount,
                    result.CleanupBeforeForcedGc.Process.Sockets.ListenerCount);

                await exporter.ExportResultAsync(outputPath, result, deadline.Token);
            }

            await using var stream = File.OpenRead(outputPath);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: deadline.Token);
            var equipmentMetrics = document.RootElement.GetProperty("equipmentMetrics");
            Assert.Equal(JsonValueKind.Array, equipmentMetrics.ValueKind);
            Assert.Equal(6, equipmentMetrics.GetArrayLength());
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
