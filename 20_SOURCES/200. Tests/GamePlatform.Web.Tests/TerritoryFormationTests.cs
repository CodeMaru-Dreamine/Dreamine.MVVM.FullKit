using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;

public sealed class TerritoryFormationTests
{
    private static readonly DateTime Now = new(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);
    private static GameProgress Player() => new()
    {
        OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(),
        SelectedCompanionIds = PartyRules.StarterCompanionIds.ToList(),
        Territory = new() { Troops = [30, 20, 10] }
    };
    private static TerritoryFormation Army(int type = 0, int count = 10, string hero = "haejin") => new()
    {
        Squads = [new() { TroopType = type, Count = count, CommanderId = hero }]
    };
    private static (bool Success, string Message) Dispatch(GameProgress p, TerritoryFormation? army, string mission = "conquer", int tile = 7) =>
        TerritoryRules.Execute(p.Territory, mission, tile, 1, Now, p, army);

    [Theory]
    [InlineData(0, 2, 125)] [InlineData(2, 0, 75)]
    [InlineData(2, 1, 125)] [InlineData(1, 2, 75)]
    [InlineData(1, 0, 125)] [InlineData(0, 1, 75)]
    [InlineData(0, 0, 100)] [InlineData(1, 1, 100)] [InlineData(2, 2, 100)]
    public void CounterCycle_HasCorrectDirection(int attacker, int defender, int expected) =>
        Assert.Equal(expected, TerritoryRules.CounterPercent(attacker, defender));

    [Fact]
    public void MixedArmy_WeightsEachSquadAgainstEnemyMix()
    {
        var state = new TerritoryState();
        // Tile 7: 80% bow, 20% cavalry. Spears: 85%; bows: 95%; cavalry: 120%.
        var result = TerritoryRules.PreviewBattle(state, 7,
            [new() { TroopType = 0, Count = 10 }, new() { TroopType = 2, Count = 10 }]);
        Assert.Equal(new[] { 0, 80, 20 }, result.EnemyComposition);
        Assert.Equal(384, result.BasePower);
        Assert.Equal(418, result.EffectivePower); // floor(120*.85 + 264*1.2)
        Assert.True(result.Victory);
    }

    [Fact]
    public void PartialDispatch_LeavesGarrisonAndExplorationPartyUntouched()
    {
        var p = Player();
        var formation = Army();
        formation.Squads.Add(new() { TroopType = 1, Count = 5, CommanderId = "seolbi" });
        Assert.True(Dispatch(p, formation).Success);
        Assert.Equal(new[] { 20, 15, 10 }, p.Territory.Troops);
        Assert.Equal(new[] { 10, 5, 0 }, p.Territory.March!.Troops);
        Assert.Equal(60, TerritoryRules.ReservedTroops(p.Territory));
        Assert.Equal(PartyRules.StarterCompanionIds, p.SelectedCompanionIds);
        Assert.Equal(2, p.Territory.March.Squads.Count);
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(90));
        Assert.Equal(new[] { 29, 19, 10 }, p.Territory.Troops);
        var once = p.Territory.Troops.ToArray();
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(90));
        Assert.Equal(once, p.Territory.Troops);
    }

    [Theory]
    [InlineData(-1, 1, "haejin")]
    [InlineData(3, 1, "haejin")]
    [InlineData(0, -1, "haejin")]
    [InlineData(0, 0, "haejin")]
    [InlineData(0, 31, "haejin")]
    [InlineData(0, int.MaxValue, "haejin")]
    [InlineData(0, 1, "damhwa")]
    [InlineData(0, 1, "unknown")]
    [InlineData(0, 1, "")]
    public void InvalidSquads_DoNotSpendOrMove(int type, int count, string hero)
    {
        var p = Player();
        Assert.False(Dispatch(p, Army(type, count, hero)).Success);
        Assert.Null(p.Territory.March);
        Assert.Equal(new[] { 30, 20, 10 }, p.Territory.Troops);
        Assert.Equal(600, p.Territory.Food);
    }

    [Fact]
    public void MissingNullDuplicateAndExtraSquads_AreRejected()
    {
        var p = Player();
        Assert.False(Dispatch(p, null).Success);
        Assert.False(Dispatch(p, new() { Squads = null! }).Success);
        Assert.False(Dispatch(p, new() { Squads = [null!] }).Success);
        Assert.False(Dispatch(p, new()).Success);
        var sameCommander = Army();
        sameCommander.Squads.Add(new() { TroopType = 1, Count = 1, CommanderId = "HAEJIN" });
        Assert.False(Dispatch(p, sameCommander).Success);
        var sameTroops = Army();
        sameTroops.Squads.Add(new() { TroopType = 0, Count = 1, CommanderId = "seolbi" });
        Assert.False(Dispatch(p, sameTroops).Success);
        Assert.False(Dispatch(p, new() { Squads = Enumerable.Repeat(Army().Squads[0], 4).ToList() }).Success);
        Assert.Null(p.Territory.March);
        Assert.Equal(600, p.Territory.Food);
    }

    [Fact]
    public void CommanderData_IsServerOwnedCappedAndSnapshotted()
    {
        var p = Player();
        p.CompanionLevels["haejin"] = 20;
        p.CompanionRanks["haejin"] = 3;
        Assert.Equal(10, TerritoryRules.Commanders(p).Single(c => c.Id == "haejin").BonusPercent);
        var request = Army();
        request.Squads[0].CommanderName = "forged";
        request.Squads[0].CommandBonus = int.MaxValue;
        Assert.True(Dispatch(p, request).Success);
        Assert.Equal("해진", p.Territory.March!.Squads[0].CommanderName);
        Assert.Equal(10, p.Territory.March.Squads[0].CommandBonus);
        var power = p.Territory.March.Power;
        p.CompanionRanks["haejin"] = int.MaxValue;
        Assert.Equal(30, TerritoryRules.Commanders(p).Single(c => c.Id == "haejin").BonusPercent);
        request.Squads[0].Count = 999;
        Assert.Equal(10, p.Territory.March.Troops[0]);
        Assert.Equal(power, p.Territory.March.Power);
        var clone = TerritoryRules.Clone(p.Territory);
        clone.March!.Squads[0].Count = 999;
        Assert.Equal(10, p.Territory.March.Squads[0].Count);
        Assert.Equal(10, p.Territory.Formation!.Squads[0].Count);
    }

    [Theory]
    [InlineData(0, 6, false)] // Base power > 70, but loses after bow-heavy counter.
    [InlineData(2, 3, true)]  // Cavalry counters the same garrison.
    public void PreviewAndActualOutcome_Agree(int type, int count, bool victory)
    {
        var p = Player();
        var army = Army(type, count);
        var preview = TerritoryRules.PreviewBattle(p.Territory, 7, TerritoryRules.ResolveSquads(p, army));
        Assert.Equal(victory, preview.Victory);
        Assert.True(Dispatch(p, army).Success);
        Assert.Equal(preview.EffectivePower, p.Territory.March!.Power);
        Assert.Equal(preview.Defense, p.Territory.March.EnemyPower);
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(90));
        Assert.Equal(victory, p.Territory.Claimed.Contains(7));
    }

    [Fact]
    public async Task ConcurrentSessions_CannotDispatchTwiceOrBypassCommanderValidation()
    {
        var store = new InMemoryProgressStore();
        var clock = new MutableTimeProvider(new DateTimeOffset(Now));
        var first = SessionFactory.Create(store, timeProvider: clock);
        var second = SessionFactory.Create(store, timeProvider: clock);
        await first.InitializeAsync("lord"); await second.InitializeAsync("lord");
        await store.MutateAsync("lord", p => { p.OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(); return true; });
        Assert.False((await first.TerritoryActionAsync("conquer", 7)).Success);
        var roster = await first.LoadTerritoryCommandersAsync();
        Assert.Contains(roster, c => c.Id == "haejin");
        Assert.DoesNotContain(roster, c => c.Id == "damhwa");
        var results = await Task.WhenAll(first.TerritoryActionAsync("conquer", 7, formation: Army()),
            second.TerritoryActionAsync("conquer", 7, formation: Army()));
        Assert.Single(results, r => r.Success);
        var saved = (await store.ReadAsync("lord")).Territory;
        Assert.Equal(590, saved.Food);
        Assert.Equal(new[] { 0, 5, 0 }, saved.Troops);
        Assert.Equal(10, saved.March!.Troops.Sum());
    }

    [Fact]
    public async Task SqliteReopen_PreservesSquadsAndCounterSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"territory-commanders-{Guid.NewGuid():N}.db");
        try
        {
            using (var store = new GameProgressStore(new GameOptions { DatabasePath = path }))
                await store.MutateAsync("lord", p =>
                {
                    p.OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList();
                    p.Territory.Troops = [30, 20, 10];
                    return Dispatch(p, Army(2, 3));
                });
            using var reopened = new GameProgressStore(new GameOptions { DatabasePath = path });
            var saved = (await reopened.LoadOrCreateAsync("lord")).Progress.Territory;
            Assert.Equal("haejin", saved.March!.Squads.Single().CommanderId);
            Assert.Equal(new[] { 0, 80, 20 }, saved.March.EnemyComposition);
            Assert.Equal(3, saved.Formation!.Squads.Single().Count);
            TerritoryRules.Settle(saved, Now.AddSeconds(90));
            Assert.Contains(7, saved.Claimed);
            Assert.Equal(new[] { 30, 20, 9 }, saved.Troops);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }
}
