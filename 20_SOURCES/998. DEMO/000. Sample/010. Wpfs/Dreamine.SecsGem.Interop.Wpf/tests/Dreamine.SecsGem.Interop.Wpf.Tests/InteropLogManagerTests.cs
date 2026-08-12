using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Wpf.Managers;
using Xunit;

namespace Dreamine.SecsGem.Interop.Wpf.Tests;

public sealed class InteropLogManagerTests
{
    [Fact]
    public void TenThousandEntryBufferEvictsOldestWithoutGrowing()
    {
        var manager = new InteropLogManager();

        for (var index = 0; index < InteropLogManager.MaximumEntries + 500; index++)
            manager.Info("Load", $"Entry-{index}");

        Assert.Equal(InteropLogManager.MaximumEntries, manager.Entries.Count);
        Assert.Equal("Entry-500", manager.Entries[0].Summary);
        Assert.Equal("Entry-10499", manager.Entries[^1].Summary);
    }

    [Fact]
    public void PauseDefersViewMutationUntilResume()
    {
        var manager = new InteropLogManager();
        manager.SetPaused(true);
        for (var index = 0; index < 100; index++) manager.Info("Pause", $"Entry-{index}");

        Assert.Empty(manager.Entries);
        manager.SetPaused(false);

        Assert.Equal(100, manager.Entries.Count);
    }

    [Fact]
    public void PausedQueueRetainsOnlyMostRecentTenThousandEntries()
    {
        var manager = new InteropLogManager();
        manager.SetPaused(true);

        for (var index = 0; index < InteropLogManager.MaximumEntries + 500; index++)
            manager.Info("Stress", $"Entry-{index}");

        manager.SetPaused(false);

        Assert.Equal(InteropLogManager.MaximumEntries, manager.Entries.Count);
        Assert.Equal("Entry-500", manager.Entries[0].Summary);
        Assert.Equal("Entry-10499", manager.Entries[^1].Summary);
    }

    [Fact]
    public void SemanticSecsMessageDoesNotClaimReEncodedBytesAreTheWireFrame()
    {
        var manager = new InteropLogManager();
        var message = new SecsMessage(new SecsSessionId(1), new SecsStream(1), new SecsFunction(2), false,
            new SecsSystemBytes(0x01020304), new SecsListItem(new SecsAsciiItem("READY")));

        manager.Message("TX", message);

        var entry = Assert.Single(manager.Entries);
        Assert.Empty(manager.Secs1Entries);
        Assert.Same(entry, Assert.Single(manager.Secs2Entries));
        Assert.Equal("S1F2", entry.SxFy);
        Assert.Equal("0x01020304", entry.SystemBytes);
        Assert.Null(entry.RawHex);
        Assert.Contains("READY", entry.Message);
    }

    [Fact]
    public void SemanticLargeBodyProjectionIsBoundedBeforeHexOrTextExpansion()
    {
        var manager = new InteropLogManager();
        var message = new SecsMessage(new SecsSessionId(1), new SecsStream(1), new SecsFunction(1), false,
            new SecsSystemBytes(1), new SecsBinaryItem(new byte[1024 * 1024]));

        manager.Message("TX", message);

        var entry = Assert.Single(manager.Secs2Entries);
        Assert.NotNull(entry.Message);
        Assert.True(entry.Message!.Length <= InteropLogManager.MaximumWireDisplayBytes);
        Assert.EndsWith("…", entry.Message, StringComparison.Ordinal);
        Assert.Null(entry.RawHex);
    }

    [Fact]
    public void ExactWireRecordProducesCorrelatedRawAndDecodedViews()
    {
        var manager = new InteropLogManager();
        var record = WireRecord(
            header: Enumerable.Range(0, 14).Select(static value => (byte)value).ToArray(),
            body: [0x41, 0x42],
            decoded: "<A[2] \"AB\">");

        manager.Wire(record);

        var entry = Assert.Single(manager.Entries);
        Assert.Same(entry, Assert.Single(manager.Secs1Entries));
        Assert.Same(entry, Assert.Single(manager.Secs2Entries));
        Assert.Contains("00 01 02", entry.RawHex, StringComparison.Ordinal);
        Assert.EndsWith("41 42", entry.RawHex, StringComparison.Ordinal);
        Assert.Equal(record.DecodedItem, entry.Message);
        Assert.Equal("0x01020304", entry.SystemBytes);
    }

    [Fact]
    public void WireViewCapsHexProjectionWithoutChangingRecordedByteCount()
    {
        var manager = new InteropLogManager();
        var body = new byte[InteropLogManager.MaximumWireDisplayBytes * 2];

        manager.Wire(WireRecord(new byte[14], body, decoded: null));

        var entry = Assert.Single(manager.Secs1Entries);
        Assert.NotNull(entry.RawHex);
        Assert.True(entry.RawHex!.Length <= InteropLogManager.MaximumWireDisplayBytes * 3 + 32);
        Assert.Contains("truncated", entry.RawHex, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(body.Length.ToString(), entry.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistentOperationalRecordUsesDiagnosticsViewWithoutInventingWireDirection()
    {
        var manager = new InteropLogManager();
        var record = WireRecord([], [], decoded: null) with
        {
            Kind = WireLogRecordKind.Diagnostic,
            Direction = null,
            DiagnosticKind = SecsDiagnosticKind.Timeout,
            DiagnosticMessage = "T3 expired.",
            Error = "T3 expired.",
            CurrentHsmsState = HsmsConnectionState.Selected
        };

        manager.Wire(record);

        var entry = Assert.Single(manager.DiagnosticsEntries);
        Assert.Empty(manager.Secs1Entries);
        Assert.Empty(manager.Secs2Entries);
        Assert.Equal("--", entry.Direction);
        Assert.Equal("Error", entry.Level);
        Assert.Contains("Timeout", entry.Summary, StringComparison.Ordinal);
        Assert.Null(entry.RawHex);
    }

    [Fact]
    public void ConcurrentProducersDoNotCorruptBoundedCollections()
    {
        var manager = new InteropLogManager();

        Parallel.For(0, 2_000, index => manager.Info("Concurrent", $"Entry-{index}"));

        Assert.Equal(2_000, manager.Entries.Count);
        Assert.Equal(2_000, manager.Entries.Select(static entry => entry.Summary).Distinct(StringComparer.Ordinal).Count());
    }

    private static WireLogRecord WireRecord(byte[] header, byte[] body, string? decoded) => new(
        WireLogRecord.CurrentSchemaVersion,
        1,
        1,
        DateTimeOffset.UnixEpoch,
        HsmsWireDirection.Inbound,
        "EQ-1",
        "CONNECTION-1",
        "127.0.0.1:5000",
        7,
        header.Length + body.Length,
        header.Length + body.Length - 4,
        7,
        1,
        2,
        false,
        0,
        0,
        0x01020304,
        WireBodyCaptureMode.FullBody,
        body.Length,
        false,
        header,
        body,
        decoded,
        null);
}
