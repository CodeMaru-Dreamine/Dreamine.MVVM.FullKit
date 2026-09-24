using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;

public sealed class TerritoryDevelopmentTests
{
    private static readonly DateTime Now = new(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);
    private static GameProgress Player() => new() { PremiumCurrency = 2000, OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(), Territory = new() { Buildings = [10,1,1,1,5,10,2], Food = 10000, Wood = 10000, Stone = 10000, Troops = [30,20,10] } };
    private static (bool Success, string Message) Act(GameProgress p, string action, int target = 0, int count = 1, DateTime? now = null, string? id = null) => TerritoryRules.Execute(p.Territory, action, target, count, now ?? Now, p, expectedJobId: id);

    [Fact]
    public void CoinSlotsAreSequentialPermanentAndNeverChargedTwice()
    {
        var p = Player();
        Assert.False(Act(p, "purchase-march-slot", 5).Success);
        p.Territory.Buildings[0] = 9;
        Assert.False(Act(p, "purchase-march-slot", 4).Success);
        p.Territory.Buildings[0] = 10;
        Assert.True(Act(p, "purchase-march-slot", 4).Success);
        Assert.Equal(1500, p.PremiumCurrency);
        Assert.False(Act(p, "purchase-march-slot", 4).Success);
        Assert.Equal(1500, p.PremiumCurrency);
        Assert.True(Act(p, "purchase-march-slot", 5).Success);
        Assert.Equal(500, p.PremiumCurrency);
        Assert.Equal(5, TerritoryRules.MarchSlots(TerritoryRules.Clone(p.Territory)));
        Assert.False(Act(p, "purchase-march-slot", 6).Success);
    }

    [Fact]
    public void InsufficientCoinsCannotUnlockOrAccelerate()
    {
        var p = Player(); p.PremiumCurrency = 0;
        Assert.False(Act(p, "purchase-march-slot", 4).Success);
        Assert.True(Act(p, "build", 0).Success);
        var job = p.Territory.Construction!;
        Assert.False(Act(p, "accelerate", job.Slot, 0, id: job.Id).Success);
        Assert.Equal(Now.AddSeconds(600), job.CompletesUtc);
        Assert.Equal(0, p.PremiumCurrency);
    }

    [Fact]
    public void TwoConstructionSlotsRejectThirdAndDuplicateFacilities()
    {
        var p = Player();
        Assert.True(Act(p, "build", 0).Success);
        Assert.True(Act(p, "build", 1).Success);
        Assert.False(Act(p, "build", 1).Success);
        Assert.False(Act(p, "build", 2).Success);
        Assert.Equal(new[] { 1, 2 }, TerritoryRules.ActiveConstructions(p.Territory).Select(j => j.Slot));
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(60));
        Assert.Equal(2, p.Territory.Buildings[1]);
        Assert.Equal(1, Assert.Single(TerritoryRules.ActiveConstructions(p.Territory)).Slot);
        Assert.True(Act(p, "build", 2, now: Now.AddSeconds(60)).Success);
        Assert.Equal(2, p.Territory.AdditionalConstruction!.Slot);
    }

    [Fact]
    public void CompletingPrimaryPreservesSecondSlotAndStaleIdsCannotHitReplacement()
    {
        var p = Player();
        Assert.True(Act(p, "build", 1).Success);
        var oldId = p.Territory.Construction!.Id;
        Assert.True(Act(p, "build", 0).Success);
        TerritoryRules.Settle(p.Territory, Now.AddSeconds(60));
        Assert.Equal(2, p.Territory.Construction!.Slot);
        Assert.True(Act(p, "build", 2, now: Now.AddSeconds(60)).Success);
        Assert.Equal(1, p.Territory.AdditionalConstruction!.Slot);
        Assert.False(Act(p, "accelerate", 1, 0, Now.AddSeconds(60), oldId).Success);
        Assert.Equal(2000, p.PremiumCurrency);
    }

    [Fact]
    public void AdditionalFarmProducesOnlyAfterCompletionIncludingOffline()
    {
        var p = Player(); var s = p.Territory;
        Assert.True(Act(p, "build", 1).Success);
        Assert.True(Act(p, "site-build", 0).Success);
        Assert.Equal(12, TerritoryRules.Production(s, 1));
        TerritoryRules.Settle(s, Now.AddMinutes(3));
        Assert.Equal(2, s.Buildings[1]);
        Assert.Equal(1, s.ProductionSites[0]);
        Assert.Equal(36, TerritoryRules.Production(s, 1));
        Assert.Equal(10072, s.Food); // 12 + 24 + 36, not retroactive income.
        Assert.Empty(TerritoryRules.ActiveConstructions(s));
        TerritoryRules.Settle(s, Now.AddMinutes(3));
        Assert.Equal(10072, s.Food);
    }

    [Theory]
    [InlineData(0, 2)] [InlineData(1, 5)] [InlineData(2, 10)] [InlineData(3, 15)]
    [InlineData(4, 2)] [InlineData(8, 2)]
    public void ProductionPlotsAreLevelGatedAndIndividuallyUpgraded(int site, int level)
    {
        var p = Player(); p.Territory.Buildings[0] = level - 1;
        Assert.False(Act(p, "site-build", site).Success);
        p.Territory.Buildings[0] = level;
        Assert.True(Act(p, "site-build", site).Success);
        Assert.False(Act(p, "site-build", site).Success);
        var end = p.Territory.Construction!.CompletesUtc;
        TerritoryRules.Settle(p.Territory, end);
        Assert.Equal(1, p.Territory.ProductionSites[site]);
        Assert.True(Act(p, "site-build", site, now: end).Success);
        TerritoryRules.Settle(p.Territory, p.Territory.Construction!.CompletesUtc);
        Assert.Equal(2, p.Territory.ProductionSites[site]);
        Assert.Equal(2, p.Territory.ProductionSites.Sum());
    }

    [Fact]
    public void PartialAndImmediateAccelerationChargeOnlyRemainingTime()
    {
        var p = Player();
        Assert.True(Act(p, "build", 0).Success);
        var job = p.Territory.Construction!;
        Assert.True(Act(p, "accelerate", 1, 5, id: job.Id).Success);
        Assert.Equal(1995, p.PremiumCurrency);
        Assert.Equal(Now.AddSeconds(300), job.CompletesUtc);
        Assert.True(Act(p, "accelerate", 1, 30, Now.AddSeconds(299), job.Id).Success);
        Assert.Equal(1994, p.PremiumCurrency); // One second remaining -> C 1, not C 30.
        Assert.Equal(11, p.Territory.Buildings[0]);
        Assert.False(Act(p, "accelerate", 1, 0, Now.AddSeconds(299), job.Id).Success);
        Assert.Equal(1994, p.PremiumCurrency);
    }

    [Theory]
    [InlineData("recruit", 0, 5, 3)] [InlineData("train", 0, 1, 4)] [InlineData("heal", 0, 10, 5)]
    public void RecruitmentTrainingAndHealingCanBeCompletedOnce(string action, int target, int count, int queue)
    {
        var p = Player(); p.Territory.Wounded = [10,0,0];
        Assert.True(Act(p, action, target, count).Success);
        var job = TerritoryRules.AccelerationJob(p.Territory, queue)!;
        var cost = TerritoryRules.AccelerationQuote(job.CompletesUtc, Now, 0).Cost;
        Assert.True(Act(p, "accelerate", queue, 0, id: job.Id).Success);
        Assert.Equal(2000 - cost, p.PremiumCurrency);
        Assert.Null(TerritoryRules.AccelerationJob(p.Territory, queue));
        Assert.Equal(action == "train" ? 2 : 1, p.Territory.Training);
        Assert.Equal(action == "recruit" ? 35 : action == "heal" ? 40 : 30, p.Territory.Troops[0]);
        Assert.False(Act(p, "accelerate", queue, 0, id: job.Id).Success);
        Assert.Equal(2000 - cost, p.PremiumCurrency);
    }

    [Fact]
    public void MarchAccelerationMaintainsPositionAndImmediateReturnIsExactlyOnce()
    {
        var p = Player();
        Assert.True(TerritoryRules.Execute(p.Territory, "conquer", 7, 1, Now, p, new() { Squads = [new() { TroopType = 0, Count = 10, CommanderId = "haejin" }] }).Success);
        var march = p.Territory.March!;
        march.ReturnsUtc = Now.AddMinutes(20);
        var position = TerritoryRules.MarchProgress(march, Now.AddMinutes(1));
        Assert.True(Act(p, "accelerate", 11, 5, Now.AddMinutes(1), march.Id).Success);
        Assert.Equal(position, TerritoryRules.MarchProgress(march, Now.AddMinutes(1)), 8);
        Assert.True(Act(p, "accelerate", 11, 0, Now.AddMinutes(1), march.Id).Success);
        Assert.Null(p.Territory.March);
        Assert.Single(p.Territory.Battles);
        var coins = p.PremiumCurrency;
        Assert.False(Act(p, "accelerate", 11, 0, Now.AddMinutes(1), march.Id).Success);
        Assert.Equal(coins, p.PremiumCurrency);
        Assert.Single(p.Territory.Battles);
    }

    [Fact]
    public void LegacySaveGetsNoFreeProductionFacilities()
    {
        var s = TerritoryRules.Deserialize("{\"Buildings\":[5,2,2,2,3,2],\"Construction\":{\"Kind\":\"build\",\"Target\":0,\"TargetLevel\":6}}");
        TerritoryRules.Normalize(s);
        Assert.Equal(12, s.ProductionSites.Length);
        Assert.Equal(0, s.ProductionSites.Sum());
        var id = s.Construction!.Id;
        var reopened = TerritoryRules.Clone(s);
        Assert.Equal(id, reopened.Construction!.Id);
        Assert.Equal(24, TerritoryRules.Production(reopened, 1));
    }

    [Theory]
    [InlineData("site-build", -1, 1)] [InlineData("site-build", 12, 1)]
    [InlineData("accelerate", 1, -1)] [InlineData("accelerate", 1, int.MaxValue)]
    [InlineData("accelerate", 99, 0)] [InlineData("purchase-march-slot", -1, 1)]
    public void InvalidOrdersNeverChargeCoins(string action, int target, int count)
    {
        var p = Player(); Assert.True(Act(p, "build", 0).Success);
        Assert.False(Act(p, action, target, count, id: p.Territory.Construction!.Id).Success);
        Assert.Equal(2000, p.PremiumCurrency);
    }

    [Fact]
    public async Task SessionUpdatesDisplayedCoinsAndRejectsDuplicateUnlock()
    {
        var store = new InMemoryProgressStore();
        var clock = new MutableTimeProvider(new DateTimeOffset(Now));
        var session = SessionFactory.Create(store, timeProvider: clock);
        await session.InitializeAsync("lord");
        await store.MutateAsync("lord", p => { p.PremiumCurrency = 2000; p.Territory = Player().Territory; return true; });
        await session.LoadTerritoryAsync();
        Assert.Equal(2000, session.Snapshot.PremiumCurrency);
        Assert.True((await session.TerritoryActionAsync("purchase-march-slot", 4)).Success);
        Assert.Equal(1500, session.Snapshot.PremiumCurrency);
        Assert.False((await session.TerritoryActionAsync("purchase-march-slot", 4)).Success);
        Assert.Equal(1500, (await store.ReadAsync("lord")).PremiumCurrency);
    }

    [Fact]
    public async Task CoinPurchasesAndConstructionAreAtomicAndPersistAfterReopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"territory-development-{Guid.NewGuid():N}.db");
        try
        {
            using (var store = new GameProgressStore(new GameOptions { DatabasePath = path }))
            {
                await store.MutateAsync("lord", p => { p.PremiumCurrency = 2000; p.Territory = Player().Territory; return true; });
                var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => store.MutateAsync("lord", p => Act(p, "purchase-march-slot", 4))));
                Assert.Single(results, r => r.Success);
                await store.MutateAsync("lord", p => { Assert.True(Act(p, "build", 0).Success); Assert.True(Act(p, "site-build", 0).Success); return true; });
            }
            using (var store = new GameProgressStore(new GameOptions { DatabasePath = path }))
            {
                var p = (await store.LoadOrCreateAsync("lord")).Progress;
                Assert.Equal(1500, p.PremiumCurrency);
                Assert.Equal(4, TerritoryRules.MarchSlots(p.Territory));
                Assert.Equal(2, TerritoryRules.ActiveConstructions(p.Territory).Count());
                TerritoryRules.Settle(p.Territory, Now.AddHours(1));
                Assert.Equal(11, p.Territory.Buildings[0]);
                Assert.Equal(1, p.Territory.ProductionSites[0]);
                Assert.Empty(TerritoryRules.ActiveConstructions(p.Territory));
            }
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }
}
