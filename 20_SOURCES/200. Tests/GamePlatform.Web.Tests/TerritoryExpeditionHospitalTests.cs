using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;

public sealed class TerritoryExpeditionHospitalTests
{
    private static readonly DateTime Now = new(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);
    private static GameProgress Player(int level = 10)
    {
        var p = new GameProgress { OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(), Territory = new() { Troops = [30, 20, 10], Food = 5000, Wood = 5000 } };
        p.Territory.Buildings[0] = level;
        return p;
    }
    private static (bool Success, string Message) Dispatch(GameProgress p, int tile, string commander, int count = 10) =>
        TerritoryRules.Execute(p.Territory, "conquer", tile, 1, Now, p,
            new() { Squads = [new() { TroopType = 0, Count = count, CommanderId = commander }] });

    [Theory]
    [InlineData(1, 1)] [InlineData(4, 1)] [InlineData(5, 2)] [InlineData(9, 2)] [InlineData(10, 3)] [InlineData(20, 3)]
    public void FreeSlotsUnlockByLordLevel(int level, int expected) => Assert.Equal(expected, TerritoryRules.MarchSlots(Player(level).Territory));

    [Fact]
    public void PremiumSlotsRequireServerEntitlementAndFreeProgression()
    {
        var p = Player(5);
        p.Territory.PurchasedMarchSlots = 2;
        Assert.Equal(2, TerritoryRules.MarchSlots(p.Territory));
        p.Territory.Buildings[0] = 10;
        Assert.Equal(5, TerritoryRules.MarchSlots(p.Territory));
        p.Territory.PurchasedMarchSlots = 0;
        var food = p.Territory.Food;
        Assert.False(TerritoryRules.Execute(p.Territory, "purchase-march-slot", 4, 1, Now, p).Success);
        Assert.Equal(3, TerritoryRules.MarchSlots(p.Territory));
        Assert.Equal(food, p.Territory.Food);
    }

    [Fact]
    public void ThreeArmiesReserveDifferentCommandersAndReturnExactlyOnce()
    {
        var p = Player();
        Assert.True(Dispatch(p, 7, "haejin").Success);
        var food = p.Territory.Food;
        Assert.False(Dispatch(p, 11, "haejin").Success);
        Assert.False(Dispatch(p, 7, "seolbi").Success);
        Assert.Equal(food, p.Territory.Food);
        Assert.True(Dispatch(p, 11, "seolbi").Success);
        Assert.True(Dispatch(p, 13, "harin").Success);
        Assert.Equal(new[] { 1, 2, 3 }, TerritoryRules.ActiveMarches(p.Territory).Select(m => m.Slot));
        Assert.Equal(60, TerritoryRules.ReservedTroops(p.Territory));
        Assert.False(Dispatch(p, 17, "dowon", 1).Success);
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(90));
        Assert.Empty(TerritoryRules.ActiveMarches(p.Territory));
        Assert.Equal(57, p.Territory.Troops.Sum());
        Assert.Equal(3, p.Territory.Wounded.Sum());
        Assert.Equal(3, p.Territory.Battles.Count);
        Assert.Equal(60, TerritoryRules.ReservedTroops(p.Territory));
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(90));
        Assert.Equal(57, p.Territory.Troops.Sum());
        Assert.Equal(3, p.Territory.Battles.Count);
    }

    [Fact]
    public void SlotBoostIsIsolatedAndReturningPrimaryDoesNotLoseOtherArmy()
    {
        var p = Player(5);
        Assert.True(Dispatch(p, 7, "haejin").Success);
        Assert.True(Dispatch(p, 11, "seolbi").Success);
        Assert.True(TerritoryRules.Execute(p.Territory, "boost", 1, 1, Now.AddSeconds(30)).Success);
        var armies = TerritoryRules.ActiveMarches(p.Territory).ToArray();
        Assert.Equal(Now.AddSeconds(60), armies[0].ReturnsUtc);
        Assert.Equal(Now.AddSeconds(90), armies[1].ReturnsUtc);
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(60));
        Assert.Equal(2, Assert.Single(TerritoryRules.ActiveMarches(p.Territory)).Slot);
        Assert.False(TerritoryRules.Execute(p.Territory, "train", 0, 1, Now.AddSeconds(60)).Success);
        Assert.False(TerritoryRules.Execute(p.Territory, "boost", 1, 1, Now.AddSeconds(60)).Success);
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(90));
        Assert.Empty(TerritoryRules.ActiveMarches(p.Territory));
    }

    [Fact]
    public void CasualtiesSplitIntoWoundedAndDeathsWithHospitalOverflow()
    {
        var p = Player();
        p.Territory.Wounded = [37, 0, 0];
        p.Territory.Troops = [0, 0, 0];
        p.Territory.March = new() { Tile = 7, Mission = "conquer", Power = 1, EnemyPower = 500, Troops = [50, 0, 0], ReturnsUtc = Now };
        TerritoryRules.Settle(p.Territory, Now);
        var battle = Assert.Single(p.Territory.Battles);
        Assert.False(battle.Victory);
        Assert.Equal(15, battle.Losses.Sum());
        Assert.Equal(3, battle.Wounded.Sum());
        Assert.Equal(12, battle.Deaths.Sum());
        Assert.Equal(40, p.Territory.Wounded.Sum());
        Assert.Equal(35, p.Territory.Troops.Sum());
        Assert.True(TerritoryBattleRules.EnemyHealth(battle, 1) > 0);
        Assert.Equal(70, TerritoryBattleRules.ArmyHealth(battle, 1));
    }

    [Fact]
    public void HealingReservesBedsAndTroopCapacityUntilTimedCompletion()
    {
        var state = new TerritoryState { Wounded = [10, 0, 0], Troops = [50, 0, 0], Food = 5000, Wood = 5000 };
        Assert.Equal(60, TerritoryRules.ReservedTroops(state));
        Assert.True(TerritoryRules.Execute(state, "heal", 0, 10, Now).Success);
        Assert.Equal(0, state.Wounded[0]);
        Assert.Equal(10, TerritoryRules.HospitalOccupied(state));
        Assert.Equal(60, TerritoryRules.ReservedTroops(state));
        Assert.False(TerritoryRules.Execute(state, "heal", 0, 10, Now).Success);
        Assert.False(TerritoryRules.Execute(state, "recruit", 0, 1, Now).Success);
        var end = state.Healing!.CompletesUtc;
        TerritoryRules.Settle(state, end.AddMilliseconds(-1));
        Assert.Equal(50, state.Troops[0]);
        TerritoryRules.Settle(state, end);
        Assert.Null(state.Healing);
        Assert.Equal(60, state.Troops[0]);
        TerritoryRules.Settle(state, end);
        Assert.Equal(60, state.Troops[0]);
    }

    [Fact]
    public void LegacySaveGetsHospitalAndOneStableMarchWithoutResettingProgress()
    {
        var state = TerritoryRules.Deserialize("{\"Buildings\":[5,2,2,2,3,2],\"March\":{\"Tile\":7,\"Troops\":[5,0,0]}}");
        TerritoryRules.Normalize(state);
        var id = state.March!.Id;
        Assert.Equal(new[] { 5, 2, 2, 2, 3, 2, 1 }, state.Buildings);
        Assert.Equal(1, state.March.Slot);
        Assert.Equal(2, TerritoryRules.MarchSlots(state));
        TerritoryRules.Normalize(state);
        Assert.Equal(id, state.March.Id);
    }

    [Fact]
    public async Task MultipleMarchesAndHealingSurviveSqliteReopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"territory-multi-{Guid.NewGuid():N}.db");
        try
        {
            using (var store = new GameProgressStore(new GameOptions { DatabasePath = path }))
                await store.MutateAsync("lord", p =>
                {
                    p.OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(); p.Territory = Player().Territory;
                    p.Territory.Wounded = [2, 0, 0];
                    Assert.True(TerritoryRules.Execute(p.Territory, "heal", 0, 10, Now).Success);
                    Assert.True(Dispatch(p, 7, "haejin").Success);
                    return Dispatch(p, 11, "seolbi");
                });
            using (var store = new GameProgressStore(new GameOptions { DatabasePath = path }))
            {
                var state = (await store.LoadOrCreateAsync("lord")).Progress.Territory;
                Assert.Equal(2, TerritoryRules.ActiveMarches(state).Count());
                Assert.NotNull(state.Healing);
                TerritoryRules.Settle(state, Now.AddHours(1));
                Assert.Null(state.Healing);
                Assert.Empty(TerritoryRules.ActiveMarches(state));
                Assert.Equal(2, state.Battles.Count);
                Assert.Equal(60, state.Troops.Sum());
                Assert.Equal(2, state.Wounded.Sum());
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }
}
