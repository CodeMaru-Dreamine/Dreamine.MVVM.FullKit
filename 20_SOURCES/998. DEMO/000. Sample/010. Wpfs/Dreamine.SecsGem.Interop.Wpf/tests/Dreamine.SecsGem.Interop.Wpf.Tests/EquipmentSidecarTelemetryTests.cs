using System.IO;
using System.Text.Json;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Dreamine.SecsGem.Interop.Wpf.Models;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class EquipmentSidecarTelemetryTests
{
    [Fact]
    public void ValidTelemetryDecodesCanonicalFrameAndCorrelation()
    {
        var message = new SecsMessage(
            new SecsSessionId(7),
            new SecsStream(1),
            new SecsFunction(2),
            replyExpected: false,
            new SecsSystemBytes(0x10203040),
            new SecsListItem(new SecsAsciiItem("READY")));
        var frame = new HsmsFrameCodec().Encode(new HsmsDataMessage(message));
        var payload = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            connectionEpoch = 2,
            timestamp = DateTimeOffset.Parse("2026-08-11T04:00:00Z"),
            direction = "Outbound",
            rawStatus = "ReencodedDataFrame",
            stream = 1,
            function = 2,
            systemBytes = 0x10203040u,
            rawHex = Convert.ToHexString(frame),
            result = "send-completed",
            timeout = ""
        });

        Assert.True(EquipmentSidecarTelemetryParser.TryParse(
            EquipmentSidecarTelemetryParser.Prefix + payload,
            out var telemetry));
        Assert.NotNull(telemetry);
        Assert.Equal(2, telemetry.ConnectionEpoch);
        Assert.Equal("Outbound", telemetry.Direction);
        Assert.Equal(0x10203040u, telemetry.Message!.SystemBytes.Value);
        Assert.Equal("READY", Assert.IsType<SecsAsciiItem>(
            Assert.IsType<SecsListItem>(telemetry.Message.Item).Items[0]).Value);
    }

    [Fact]
    public void TelemetryRejectsMetadataThatDoesNotMatchFrame()
    {
        var message = new SecsMessage(
            new SecsSessionId(0), new SecsStream(1), new SecsFunction(1), true,
            new SecsSystemBytes(1));
        var frame = new HsmsFrameCodec().Encode(new HsmsDataMessage(message));
        var payload = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            timestamp = DateTimeOffset.UtcNow,
            direction = "Inbound",
            rawStatus = "ReencodedDataFrame",
            stream = 1,
            function = 3,
            systemBytes = 1u,
            rawHex = Convert.ToHexString(frame),
            result = "received",
            timeout = ""
        });

        Assert.False(EquipmentSidecarTelemetryParser.TryParse(
            EquipmentSidecarTelemetryParser.Prefix + payload,
            out _));
    }

    [Fact]
    public async Task PublicExportExcludesPrivateProfileTraffic()
    {
        var directory = Path.Combine(Path.GetTempPath(), "dreamine-sidecar-export", Guid.NewGuid().ToString("N"));
        try
        {
            var logs = new[]
            {
                new InteropLogEntry(DateTimeOffset.UtcNow, "Info", "HSMS", "--", "PUBLIC-SUMMARY", null, null),
                new InteropLogEntry(DateTimeOffset.UtcNow, "Info", "SECS-II", "RX", "PRIVATE-SECRET", "PRIVATE-BODY", "00",
                    ContainsPrivateProfileData: true)
            };
            var paths = await new ResultExportManager().ExportAsync(
                directory,
                Array.Empty<InteropScenarioResult>(),
                logs,
                CancellationToken.None);

            var json = await File.ReadAllTextAsync(paths.JsonPath);
            Assert.DoesNotContain("PUBLIC-SUMMARY", json, StringComparison.Ordinal);
            Assert.DoesNotContain("PRIVATE-SECRET", json, StringComparison.Ordinal);
            Assert.DoesNotContain("PRIVATE-BODY", json, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Summary\"", json, StringComparison.Ordinal);
            using var document = JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty("ExcludedPrivateProfileLogCount").GetInt32());
            var exported = Assert.Single(document.RootElement.GetProperty("Logs").EnumerateArray());
            Assert.Equal("HSMS", exported.GetProperty("Category").GetString());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
