using System.IO;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class FactoryResultJsonParserTests
{
    [Fact]
    public async Task NumericStatusAndFactoryMetricsAreProjectedWithoutFactoryExecutableReference()
    {
        var path = await WriteResultAsync(SampleJson("1", "null"));
        try
        {
            var result = await FactoryResultJsonParser.ParseAsync(path, CancellationToken.None);

            Assert.True(result.IsImported);
            Assert.Equal("Passed", result.Status);
            Assert.Equal("scale", result.Scenario);
            Assert.Equal("50", result.RequestedEquipment);
            Assert.Equal("48", result.SelectedEquipment);
            Assert.Equal("2,500.50 msg/s", result.MessagesPerSecond);
            Assert.Equal("2.500 ms", result.LatencyP95);
            Assert.Equal("Clear", result.CleanupState);
            Assert.Contains("Open 3", result.SocketSummary, StringComparison.Ordinal);
            Assert.Contains("process-owned", result.SocketSummary, StringComparison.Ordinal);
            Assert.Contains("host-message: 0/64 (peak 12, rejected 0, dropped 0)", result.QueueSummary,
                StringComparison.Ordinal);
            Assert.Contains("Cleanup passed", result.Checks, StringComparison.Ordinal);
            Assert.Equal("None reported.", result.FirstFailure);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task StringStatusAndResidualCleanupAreVisible()
    {
        var json = SampleJson("\"Failed\"", "\"peer failed\"")
            .Replace("\"trackedSessions\": 0", "\"trackedSessions\": 1", StringComparison.Ordinal);
        var path = await WriteResultAsync(json);
        try
        {
            var result = await FactoryResultJsonParser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("Failed", result.Status);
            Assert.Equal("Residual State", result.CleanupState);
            Assert.Equal("peer failed", result.FirstFailure);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task UnknownSchemaIsRejectedInsteadOfBeingMisrepresented()
    {
        var path = await WriteResultAsync(SampleJson("1", "null").Replace(
            "\"schemaVersion\": 1", "\"schemaVersion\": 99", StringComparison.Ordinal));
        try
        {
            var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
                FactoryResultJsonParser.ParseAsync(path, CancellationToken.None));
            Assert.Contains("schema version", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task SustainedProfileUsesLatestActiveIntervalWhenTerminalCaptureHasNoDelta()
    {
        var json = SampleJson("1", "null")
            .Replace("\"scenario\": \"scale\"", "\"scenario\": \"factory-normal\"", StringComparison.Ordinal)
            .Replace("\"requestsPerSecond\": 1250.25", "\"requestsPerSecond\": 0", StringComparison.Ordinal)
            .Replace("\"responsesPerSecond\": 1250.25", "\"responsesPerSecond\": 0", StringComparison.Ordinal)
            .Replace("\"messagesPerSecond\": 2500.5", "\"messagesPerSecond\": 0", StringComparison.Ordinal)
            .Replace("\"cleanupBeforeForcedGc\": {", """
              "snapshots": [
                {
                  "requestsPerSecond": 499.75,
                  "responsesPerSecond": 499.75,
                  "messagesPerSecond": 999.5
                }
              ],
              "cleanupBeforeForcedGc": {
              """, StringComparison.Ordinal);
        var path = await WriteResultAsync(json);
        try
        {
            var result = await FactoryResultJsonParser.ParseAsync(path, CancellationToken.None);

            Assert.Equal("999.50 msg/s", result.MessagesPerSecond);
            Assert.Equal("499.75 req/s", result.RequestsPerSecond);
            Assert.Equal("499.75 rsp/s", result.ResponsesPerSecond);
        }
        finally { File.Delete(path); }
    }

    private static async Task<string> WriteResultAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dreamine-factory-result-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    private static string SampleJson(string statusLiteral, string firstFailureLiteral) => $$"""
    {
      "schemaVersion": 1,
      "scenario": "scale",
      "executionMode": "in-process",
      "startedAtUtc": "2026-08-10T00:00:00+00:00",
      "completedAtUtc": "2026-08-10T00:02:03.456+00:00",
      "status": {{statusLiteral}},
      "final": {
        "requestedEquipment": 50,
        "connectedEquipment": 49,
        "selectedEquipment": 48,
        "failedEquipment": 1,
        "reconnectingEquipment": 0,
        "requests": 1000,
        "responses": 998,
        "timeouts": 1,
        "failures": 1,
        "correlationErrors": 0,
        "bytesSent": 1048576,
        "bytesReceived": 2097152,
        "requestsPerSecond": 1250.25,
        "responsesPerSecond": 1250.25,
        "messagesPerSecond": 2500.5,
        "pendingTransactions": 0,
        "peakPendingTransactions": 24,
        "pendingControlTransactions": 0,
        "peakPendingControlTransactions": 5,
        "trackedOperations": 0,
        "reconnectOperations": 0,
        "trackedSessions": 50,
        "trackedListeners": 50,
        "droppedDiagnosticLogs": 0,
        "responseLatency": {
          "sampleCount": 998,
          "averageMilliseconds": 1.5,
          "p50Milliseconds": 1.0,
          "p95Milliseconds": 2.5,
          "p99Milliseconds": 4.0,
          "maximumMilliseconds": 8.0,
          "resolution": "1 us"
        },
        "process": {
          "workingSetBytes": 104857600,
          "privateMemoryBytes": 125829120,
          "managedHeapBytes": 52428800,
          "threadCount": 18,
          "handleCount": 120,
          "sockets": {
            "openSocketCount": 3,
            "listenerCount": 1,
            "establishedCount": 2,
            "timeWaitCount": 0,
            "source": 2,
            "isProcessOwnedMeasurement": true
          }
        },
        "queues": [
          {
            "name": "host-message",
            "capacity": 64,
            "depth": 0,
            "peakDepth": 12,
            "accepted": 1000,
            "dequeued": 1000,
            "rejected": 0,
            "dropped": 0,
            "fullPolicy": "Wait"
          }
        ]
      },
      "cleanupBeforeForcedGc": {
        "trackedSessions": 0,
        "trackedListeners": 0,
        "pendingTransactions": 0,
        "pendingControlTransactions": 0,
        "trackedOperations": 0,
        "reconnectOperations": 0,
        "process": { "sockets": { "source": 0, "isProcessOwnedMeasurement": false } }
      },
      "cleanupAfterForcedGc": {
        "trackedSessions": 0,
        "trackedListeners": 0,
        "pendingTransactions": 0,
        "pendingControlTransactions": 0,
        "trackedOperations": 0,
        "reconnectOperations": 0,
        "process": { "sockets": { "openSocketCount": 0, "listenerCount": 0, "establishedCount": 0, "timeWaitCount": 0, "source": 2, "isProcessOwnedMeasurement": true } }
      },
      "checks": ["Cleanup passed"],
      "firstFailure": {{firstFailureLiteral}},
      "evidenceScope": "Self-loopback evidence only"
    }
    """;
}
