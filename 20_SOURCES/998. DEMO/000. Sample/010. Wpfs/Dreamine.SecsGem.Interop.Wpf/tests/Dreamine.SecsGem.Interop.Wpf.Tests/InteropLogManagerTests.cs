using Dreamine.Secs.Abstractions.Model;
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
    public void SecsMessageProducesCorrelatedRawAndDecodedEntries()
    {
        var manager = new InteropLogManager();
        var message = new SecsMessage(new SecsSessionId(1), new SecsStream(1), new SecsFunction(2), false,
            new SecsSystemBytes(0x01020304), new SecsListItem(new SecsAsciiItem("READY")));

        manager.Message("TX", message);

        var entry = Assert.Single(manager.Entries);
        Assert.Same(entry, Assert.Single(manager.Secs1Entries));
        Assert.Same(entry, Assert.Single(manager.Secs2Entries));
        Assert.Equal("S1F2", entry.SxFy);
        Assert.Equal("0x01020304", entry.SystemBytes);
        Assert.Contains("52 45 41 44 59", entry.RawHex);
        Assert.Contains("READY", entry.Message);
    }

    [Fact]
    public void ConcurrentProducersDoNotCorruptBoundedCollections()
    {
        var manager = new InteropLogManager();

        Parallel.For(0, 2_000, index => manager.Info("Concurrent", $"Entry-{index}"));

        Assert.Equal(2_000, manager.Entries.Count);
        Assert.Equal(2_000, manager.Entries.Select(static entry => entry.Summary).Distinct(StringComparer.Ordinal).Count());
    }
}
