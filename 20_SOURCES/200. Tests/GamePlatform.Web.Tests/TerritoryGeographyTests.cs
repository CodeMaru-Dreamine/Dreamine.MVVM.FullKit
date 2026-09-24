using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public class TerritoryGeographyTests
{
    [Fact]
    public void ExpandedWorldHas625UniqueConnectedSites()
    {
        Assert.Equal(625, TerritoryRules.WorldTileCount);
        var reached = new HashSet<int> { 12 };
        var pending = new Queue<int>(); pending.Enqueue(12);
        while (pending.TryDequeue(out var tile))
        {
            Assert.Equal(tile, TerritoryRules.TileAt(TerritoryRules.TileColumn(tile), TerritoryRules.TileRow(tile)));
            foreach (var neighbor in TerritoryRules.Neighbors(tile))
            {
                Assert.Contains(tile, TerritoryRules.Neighbors(neighbor));
                if (reached.Add(neighbor)) pending.Enqueue(neighbor);
            }
        }
        Assert.Equal(625, reached.Count);
        Assert.Equal(24, reached.Count(TerritoryRules.IsRegionalCapital));
        Assert.False(TerritoryRules.ValidTile(625));
        Assert.Equal(-1, TerritoryRules.TileAt(25, 0));
    }

    [Fact]
    public void OriginalProvinceKeepsIdsDistancesClaimsAndMarchDestination()
    {
        for (var tile = 0; tile < 25; tile++)
        {
            Assert.Equal(Math.Abs(tile / 5 - 2) + Math.Abs(tile % 5 - 2), TerritoryRules.Distance(tile));
            Assert.Equal(tile % 5 + 10, TerritoryRules.TileColumn(tile));
            Assert.Equal(tile / 5 + 10, TerritoryRules.TileRow(tile));
        }
        var state = new TerritoryState { Claimed = [12, 7, 2], March = new() { Tile = 0 } };
        TerritoryRules.Normalize(state);
        Assert.Equal(new[] { 12, 7, 2 }, state.Claimed);
        Assert.Equal(0, state.March!.Tile);
        Assert.Contains(TerritoryRules.TileAt(12, 9), TerritoryRules.Neighbors(2));
        Assert.Equal(3500, TerritoryWorldVisuals.X(12));
        Assert.Equal(7000, TerritoryWorldVisuals.WorldSize);
    }

    [Fact]
    public void ScoutCrossesOldBoundaryAndOuterClaimsSurviveSerialization()
    {
        var now = DateTime.UtcNow;
        var outside = TerritoryRules.TileAt(12, 9);
        var state = new TerritoryState { Revealed = [12, 7, 2], March = new() { Tile = 2, Mission = "scout", Troops = [1, 0, 0], ReturnsUtc = now } };
        TerritoryRules.Settle(state, now);
        Assert.Contains(outside, state.Revealed);
        state.Claimed.Add(outside);
        var restored = TerritoryRules.Clone(state);
        TerritoryRules.Normalize(restored);
        Assert.Contains(outside, restored.Claimed);
        Assert.Contains(outside, restored.Revealed);
    }
}
