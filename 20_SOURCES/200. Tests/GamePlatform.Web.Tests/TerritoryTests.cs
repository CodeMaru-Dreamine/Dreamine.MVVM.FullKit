using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;

public sealed class TerritoryTests
{
    private static readonly DateTime Now = new(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);

    private static (bool Success, string Message) Execute(TerritoryState state, string action, int target, int count, DateTime now)
    {
        var progress = new GameProgress { Territory = state, OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList() };
        var formation = new TerritoryFormation { Squads = Enumerable.Range(0, 3).Where(i => state.Troops[i] > 0)
            .Select(i => new TerritorySquad { TroopType = i, Count = state.Troops[i], CommanderId = PartyRules.StarterCompanionIds[i] }).ToList() };
        return TerritoryRules.Execute(state, action, target, count, now, progress, formation);
    }

    [Fact]
    public void Construction_ChargesOnceAndAppliesOnlyAtCompletion()
    {
        var state = new TerritoryState();
        Assert.False(Execute(state, "build", 4, 1, Now).Success);
        Assert.True(Execute(state, "build", 0, 1, Now).Success);
        Assert.Equal(1, state.Buildings[0]);
        Assert.Equal(500, state.Wood);
        Assert.Equal(330, state.Stone);
        Assert.Equal(Now.AddSeconds(60), state.Construction!.CompletesUtc);
        Assert.False(Execute(state, "build", 0, 1, Now).Success);
        Assert.Equal(500, state.Wood);
        TerritoryRules.Settle(state, Now.AddSeconds(59));
        Assert.Equal(1, state.Buildings[0]);
        TerritoryRules.Settle(state, Now.AddSeconds(60));
        Assert.Equal(2, state.Buildings[0]);
        Assert.Null(state.Construction);
        TerritoryRules.Settle(state, Now.AddSeconds(60));
        Assert.Equal(2, state.Buildings[0]);
        Assert.True(Execute(state, "build", 4, 1, Now.AddSeconds(60)).Success);
        Assert.Equal(1, state.Buildings[4]);
    }

    [Fact]
    public void ProductionUpgrade_SettlesChronologicallyAcrossOfflineCompletion()
    {
        var state = new TerritoryState();
        state.Buildings[0] = 2;
        Assert.True(Execute(state, "build", 1, 1, Now).Success);
        TerritoryRules.Settle(state, Now.AddMinutes(3));
        Assert.Equal(2, state.Buildings[1]);
        Assert.Equal(600 + 12 + 48, state.Food);
        Assert.Contains(state.Reports, r => r.Contains("09:01") && r.Contains("완료"));
    }

    [Fact]
    public void FrequentRefreshAndSingleSettlement_ProduceIdenticalResources()
    {
        var frequent = new TerritoryState();
        var single = new TerritoryState();
        frequent.Buildings[0] = single.Buildings[0] = 2;
        Execute(frequent, "build", 1, 1, Now);
        Execute(single, "build", 1, 1, Now);
        for (var i = 1; i <= 187; i++) TerritoryRules.Settle(frequent, Now.AddSeconds(i));
        TerritoryRules.Settle(single, Now.AddSeconds(187));
        Assert.Equal(single.Food, frequent.Food);
        Assert.Equal(single.Wood, frequent.Wood);
        Assert.Equal(single.Stone, frequent.Stone);
        Assert.Equal(single.ProductionRemainders, frequent.ProductionRemainders);
    }

    [Fact]
    public void Recruitment_DoesNotGrantTroopsBeforeTimer_AndTrainingDoesNotGrantPowerEarly()
    {
        var state = new TerritoryState();
        state.Buildings[4] = 2;
        Assert.True(Execute(state, "recruit", 0, 5, Now).Success);
        Assert.Equal(10, state.Troops[0]);
        Assert.Equal(500, state.Food);
        Assert.False(Execute(state, "recruit", 1, 1, Now).Success);
        Assert.True(Execute(state, "train", 0, 1, Now).Success);
        Assert.Equal(1, state.Training);
        Assert.False(Execute(state, "conquer", 7, 1, Now).Success);
        TerritoryRules.Settle(state, Now.AddSeconds(89));
        Assert.Equal(1, state.Training);
        Assert.Equal(10, state.Troops[0]);
        TerritoryRules.Settle(state, Now.AddSeconds(90));
        Assert.Equal(2, state.Training);
        Assert.Equal(10, state.Troops[0]);
        TerritoryRules.Settle(state, Now.AddSeconds(100));
        Assert.Equal(15, state.Troops[0]);
        Assert.Null(state.Recruitment);
        Assert.Null(state.Research);
        TerritoryRules.Settle(state, Now.AddSeconds(100));
        Assert.Equal(15, state.Troops[0]);
    }

    [Fact]
    public void RecruitingDuringMarch_ReservesCapacityAndDoesNotJoinDepartedArmy()
    {
        var state = new TerritoryState { Troops = [50, 0, 0] };
        Assert.True(Execute(state, "conquer", 7, 1, Now).Success);
        Assert.True(Execute(state, "recruit", 0, 10, Now).Success);
        Assert.Equal(60, TerritoryRules.ReservedTroops(state));
        Assert.Equal(50, state.March!.Troops.Sum());
        Assert.Equal(0, state.Troops.Sum());
        Assert.False(Execute(state, "train", 0, 1, Now).Success);
        TerritoryRules.Settle(state, Now.AddSeconds(200));
        Assert.Equal(55, state.Troops.Sum());
        Assert.Null(state.March);
        Assert.Null(state.Recruitment);
    }

    [Fact]
    public void RecruitmentCapacity_IncludesArmyAwayFromHome()
    {
        var state = new TerritoryState { Troops = [60, 0, 0] };
        Execute(state, "conquer", 7, 1, Now);
        var food = state.Food;
        Assert.False(Execute(state, "recruit", 0, 1, Now).Success);
        Assert.Equal(food, state.Food);
        Assert.Null(state.Recruitment);
    }

    [Theory]
    [InlineData("build", -1, 1)]
    [InlineData("build", 6, 1)]
    [InlineData("recruit", 3, 1)]
    [InlineData("recruit", 0, -1)]
    [InlineData("recruit", 0, 1000)]
    [InlineData("gather", 7, 1)]
    [InlineData("conquer", 0, 1)]
    [InlineData("boost", 0, 1)]
    public void InvalidCommands_DoNotSpendOrQueue(string action, int target, int count)
    {
        var state = new TerritoryState();
        Assert.False(Execute(state, action, target, count, Now).Success);
        Assert.Equal(600, state.Food);
        Assert.Equal(600, state.Wood);
        Assert.Equal(400, state.Stone);
        Assert.Null(state.Construction);
        Assert.Null(state.Recruitment);
        Assert.Null(state.March);
    }

    [Fact]
    public void BuildingAndRecruitment_RespectUnlocksResourcesAndMaximums()
    {
        var state = new TerritoryState();
        Assert.False(Execute(state, "recruit", 2, 1, Now).Success);
        state.Wood = 0;
        Assert.False(Execute(state, "build", 0, 1, Now).Success);
        Assert.Null(state.Construction);
        state.Buildings[0] = 20;
        Assert.False(Execute(state, "build", 0, 1, Now).Success);
        state.Training = 2;
        Assert.False(Execute(state, "train", 0, 1, Now).Success);
    }

    [Fact]
    public void MarchBoost_HalvesRemainingTimeAndChargesOnlyOnce()
    {
        var state = new TerritoryState();
        Execute(state, "conquer", 7, 1, Now);
        Assert.Equal(Now.AddSeconds(90), state.March!.ReturnsUtc);
        Assert.True(Execute(state, "boost", 0, 1, Now.AddSeconds(10)).Success);
        Assert.Equal(Now.AddSeconds(50), state.March!.ReturnsUtc);
        Assert.Equal(542, state.Food); // 600 - departure 10 + production 2 - boost 50
        Assert.True(state.March.Boosted);
        Assert.False(Execute(state, "boost", 0, 1, Now.AddSeconds(10)).Success);
        Assert.Equal(542, state.Food);
        TerritoryRules.Settle(state, Now.AddSeconds(49));
        Assert.NotNull(state.March);
        TerritoryRules.Settle(state, Now.AddSeconds(50));
        Assert.Null(state.March);
        Assert.Contains(7, state.Claimed);
        var wood = state.Wood;
        TerritoryRules.Settle(state, Now.AddSeconds(50));
        Assert.Equal(wood, state.Wood);
    }

    [Fact]
    public void MarchBoost_RejectsInsufficientFoodAndNearArrivalWithoutCharging()
    {
        var state = new TerritoryState { Food = 10 };
        Execute(state, "conquer", 7, 1, Now);
        Assert.False(Execute(state, "boost", 0, 1, Now).Success);
        Assert.Equal(0, state.Food);
        state.Food = 100;
        TerritoryRules.Settle(state, Now.AddSeconds(86));
        var food = state.Food;
        Assert.False(Execute(state, "boost", 0, 1, Now.AddSeconds(86)).Success);
        Assert.Equal(food, state.Food);
    }

    [Fact]
    public void ConquestAndGathering_ReturnExactlyOnce()
    {
        var state = new TerritoryState();
        Execute(state, "conquer", 7, 1, Now);
        TerritoryRules.Settle(state, Now.AddSeconds(90));
        Assert.Contains(7, state.Claimed);
        Assert.Contains(2, state.Revealed);
        Assert.Equal(13, state.Troops.Sum());
        var stone = state.Stone;
        Assert.True(Execute(state, "gather", 7, 1, Now.AddSeconds(90)).Success);
        TerritoryRules.Settle(state, Now.AddSeconds(180));
        Assert.Equal(stone + 13 * 8 + 27, state.Stone);
        var after = state.Stone;
        TerritoryRules.Settle(state, Now.AddSeconds(180));
        Assert.Equal(after, state.Stone);
    }

    [Fact]
    public void DefeatAndScouting_DoNotGrantConquest()
    {
        var state = new TerritoryState { Troops = [1, 0, 0] };
        Execute(state, "conquer", 7, 1, Now);
        TerritoryRules.Settle(state, Now.AddSeconds(90));
        Assert.DoesNotContain(7, state.Claimed);
        Assert.Equal(0, state.Troops.Sum());
        state.Troops = [1, 0, 0];
        Execute(state, "scout", 7, 1, Now.AddSeconds(90));
        TerritoryRules.Settle(state, Now.AddSeconds(180));
        Assert.Contains(2, state.Revealed);
        Assert.DoesNotContain(7, state.Claimed);
        Assert.Equal(1, state.Troops.Sum());
    }

    [Fact]
    public void OfflineProduction_HasEightHourLimitAndRejectsReversedTime()
    {
        var state = new TerritoryState();
        state.Buildings[5] = 3;
        TerritoryRules.Settle(state, Now);
        TerritoryRules.Settle(state, Now.AddDays(5));
        Assert.Equal(600 + 480 * 12, state.Food);
        var food = state.Food;
        TerritoryRules.Settle(state, Now.AddDays(5));
        TerritoryRules.Settle(state, Now);
        Assert.Equal(food, state.Food);
        Assert.False(Execute(state, "build", 0, 1, Now).Success);
        TerritoryRules.Settle(state, Now.AddDays(5).AddMinutes(1));
        Assert.Equal(food + 12, state.Food);
    }

    [Fact]
    public void WarehouseCap_PausesIncomeWithoutConfiscatingLegacyBalances()
    {
        var state = new TerritoryState { Food = 2999, Wood = 70000, Stone = 2999 };
        TerritoryRules.Settle(state, Now);
        TerritoryRules.Settle(state, Now.AddMinutes(1));
        Assert.Equal(3000, state.Food);
        Assert.Equal(3000, state.Stone);
        Assert.Equal(70000, state.Wood);
        state.Buildings[0] = 2;
        Execute(state, "build", 5, 1, Now.AddMinutes(1));
        Assert.Equal(1, state.Buildings[5]);
        TerritoryRules.Settle(state, Now.AddMinutes(3));
        Assert.Equal(2, state.Buildings[5]);
        Assert.Equal(12000, TerritoryRules.StorageCapacity(state));
        Assert.Equal(3012, state.Food);
        Assert.Equal(69900, state.Wood);
    }

    [Fact]
    public async Task MultiSessionCommands_AreAtomicAndIsolated()
    {
        var store = new InMemoryProgressStore();
        var clock = new MutableTimeProvider(new DateTimeOffset(Now));
        var first = SessionFactory.Create(store, timeProvider: clock);
        var second = SessionFactory.Create(store, timeProvider: clock);
        await first.InitializeAsync("lord");
        await second.InitializeAsync("lord");
        var results = await Task.WhenAll(first.TerritoryActionAsync("build", 0), second.TerritoryActionAsync("build", 0));
        Assert.Single(results, r => r.Success);
        Assert.Equal(500, (await store.ReadAsync("lord")).Territory.Wood);
        clock.Advance(TimeSpan.FromSeconds(61));
        var state = await first.LoadTerritoryAsync();
        Assert.Equal(2, state.Buildings[0]);
        state.Buildings[0] = 20;
        Assert.Equal(2, (await second.LoadTerritoryAsync()).Buildings[0]);
        var other = SessionFactory.Create(store, timeProvider: clock);
        await other.InitializeAsync("neighbor");
        Assert.Equal(1, (await other.LoadTerritoryAsync()).Buildings[0]);
        Assert.Equal(0, (await store.ReadAsync("lord")).Gold);
    }

    [Fact]
    public async Task SqliteReopen_PreservesEveryQueueAndCompletesOnce()
    {
        var path = Path.Combine(Path.GetTempPath(), $"territory-queues-{Guid.NewGuid():N}.db");
        var options = new GameOptions { DatabasePath = path };
        try
        {
            using (var store = new GameProgressStore(options))
                await store.MutateAsync("lord", p =>
                {
                    p.Territory.Buildings[4] = 2;
                    Assert.True(Execute(p.Territory, "build", 0, 1, Now).Success);
                    Assert.True(Execute(p.Territory, "recruit", 0, 5, Now).Success);
                    return Execute(p.Territory, "train", 0, 1, Now);
                });
            using (var reopened = new GameProgressStore(options))
            {
                var saved = (await reopened.LoadOrCreateAsync("lord")).Progress.Territory;
                Assert.NotNull(saved.Construction);
                Assert.NotNull(saved.Recruitment);
                Assert.NotNull(saved.Research);
                await reopened.MutateAsync("lord", p => { TerritoryRules.Settle(p.Territory, Now.AddMinutes(10)); return true; });
            }
            using (var final = new GameProgressStore(options))
            {
                var saved = (await final.ListAsync()).Single().Territory;
                Assert.Null(saved.Construction);
                Assert.Null(saved.Recruitment);
                Assert.Null(saved.Research);
                Assert.Equal(2, saved.Buildings[0]);
                Assert.Equal(2, saved.Training);
                Assert.Equal(15, saved.Troops[0]);
                TerritoryRules.Settle(saved, Now.AddMinutes(10));
                Assert.Equal(15, saved.Troops[0]);
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }

    [Fact]
    public async Task SqliteReopen_PreservesBoostedMarchAndSettlesOnce()
    {
        var path = Path.Combine(Path.GetTempPath(), $"territory-march-{Guid.NewGuid():N}.db");
        var options = new GameOptions { DatabasePath = path };
        try
        {
            using (var store = new GameProgressStore(options))
                await store.MutateAsync("lord", p =>
                {
                    Execute(p.Territory, "conquer", 7, 1, Now);
                    return Execute(p.Territory, "boost", 0, 1, Now);
                });
            using (var reopened = new GameProgressStore(options))
            {
                var saved = (await reopened.LoadOrCreateAsync("lord")).Progress.Territory;
                Assert.True(saved.March!.Boosted);
                Assert.Equal(Now.AddSeconds(45), saved.March.ReturnsUtc);
                await reopened.MutateAsync("lord", p => { TerritoryRules.Settle(p.Territory, Now.AddSeconds(45)); return true; });
                var done = (await reopened.ListAsync()).Single().Territory;
                Assert.Null(done.March);
                Assert.Contains(7, done.Claimed);
                Assert.Equal(13, done.Troops.Sum());
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }

    [Fact]
    public void LegacyState_AddsWarehouseWithoutChangingProgressOrActiveMarch()
    {
        var state = TerritoryRules.Deserialize("{\"Buildings\":[4,3,2,1,3],\"Food\":70000}");
        TerritoryRules.Normalize(state);
        Assert.Equal(new[] { 4, 3, 2, 1, 3, 1, 1 }, state.Buildings);
        Assert.Equal(70000, state.Food);
        Assert.Null(state.Construction);
        var clone = TerritoryRules.Clone(state);
        clone.Buildings[0] = 20;
        Assert.Equal(4, state.Buildings[0]);
        state.March = new() { Tile = 7, Mission = "scout", Troops = [2, 0, 0], ReturnsUtc = Now.AddSeconds(30) };
        state.SettledUtc = Now;
        TerritoryRules.Settle(state, Now.AddSeconds(30));
        Assert.Null(state.March);
        Assert.Contains(2, state.Revealed);
    }
}
