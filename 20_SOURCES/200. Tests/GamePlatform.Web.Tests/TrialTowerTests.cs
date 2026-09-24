using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class TrialTowerTests
{
    [Fact]
    public async Task FirstFloorVictory_AwardsTicketsAndPersistsHighestFloor()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-first", value => value.AttackLevel = 1_000);
        var session = SessionFactory.Create(store, 0);
        await session.InitializeAsync("tower-first");

        var result = await session.ChallengeTrialTowerAsync(1);
        var restored = await SessionFactory.Create(store).InitializeAsync("tower-first");

        Assert.True(result.Accepted);
        Assert.True(result.Victory);
        Assert.Equal(1, result.State.HighestTrialTowerFloor);
        Assert.Equal(10, result.State.HeroSummonTickets);
        Assert.Equal(1, restored.HighestTrialTowerFloor);
        Assert.Equal(10, restored.HeroSummonTickets);
        Assert.Equal(5, restored.TrialTowerChallengeTickets);
        Assert.Equal(0, result.ConsumedChallengeTickets);
        Assert.Equal(5_800, result.State.Gold);
        Assert.Equal(30, result.State.CompanionUpgradeMaterials);
    }

    [Fact]
    public async Task TowerCannotSkipOrReplayClearedFloor()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-order", value => value.AttackLevel = 1_000);
        var session = SessionFactory.Create(store, 0);
        await session.InitializeAsync("tower-order");

        var skipped = await session.ChallengeTrialTowerAsync(3);
        var cleared = await session.ChallengeTrialTowerAsync(1);
        var replayed = await session.ChallengeTrialTowerAsync(1);

        Assert.False(skipped.Accepted);
        Assert.True(cleared.Victory);
        Assert.False(replayed.Accepted);
        Assert.Equal(1, replayed.State.HighestTrialTowerFloor);
        Assert.Equal(10, replayed.State.HeroSummonTickets);
    }

    [Fact]
    public async Task TenthFloorBoss_AwardsTwoHundredSummonTickets()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-boss", value =>
        {
            value.AttackLevel = 10_000;
            value.HighestTrialTowerFloor = 9;
        });
        var session = SessionFactory.Create(store, 0);
        await session.InitializeAsync("tower-boss");

        var result = await session.ChallengeTrialTowerAsync(10);

        Assert.True(result.Victory);
        Assert.True(result.Floor.IsBoss);
        Assert.Equal(200, result.Floor.SummonTickets);
        Assert.Equal(200, result.State.HeroSummonTickets);
    }

    [Fact]
    public async Task FailedFloor_DoesNotAdvanceOrAwardAnything()
    {
        var store = new InMemoryProgressStore();
        var session = SessionFactory.Create(store, 99);
        var before = await session.InitializeAsync("tower-fail");

        var result = await session.ChallengeTrialTowerAsync(1);

        Assert.True(result.Accepted);
        Assert.False(result.Victory);
        Assert.Equal(0, result.State.HighestTrialTowerFloor);
        Assert.Equal(before.HeroSummonTickets, result.State.HeroSummonTickets);
        Assert.Equal(before.Gold, result.State.Gold);
        Assert.Equal(before.TrialTowerChallengeTickets - 1, result.State.TrialTowerChallengeTickets);
        Assert.Equal(1, result.ConsumedChallengeTickets);
    }

    [Fact]
    public async Task Sweep_ClearsAtMostFiveSequentialNormalFloors()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-sweep-five", value => value.AttackLevel = 100_000);
        var session = SessionFactory.Create(store, 0);
        await session.InitializeAsync("tower-sweep-five");

        var result = await session.SweepTrialTowerAsync(5);

        Assert.True(result.Accepted);
        Assert.False(result.Failed);
        Assert.Equal(5, result.ClearedFloors);
        Assert.Equal(1, result.StartFloor);
        Assert.Equal(5, result.EndFloor);
        Assert.Equal(5, result.State.HighestTrialTowerFloor);
        Assert.Equal(90, result.SummonTickets);
        Assert.Equal(90, result.State.HeroSummonTickets);
        Assert.Equal(5, result.State.TrialTowerChallengeTickets);
        Assert.Equal(0, result.ConsumedChallengeTickets);
    }

    [Fact]
    public async Task Sweep_StopsBeforeBossFloor()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-sweep-boss", value =>
        {
            value.AttackLevel = 100_000;
            value.HighestTrialTowerFloor = 7;
        });
        var session = SessionFactory.Create(store, 0);
        await session.InitializeAsync("tower-sweep-boss");

        var result = await session.SweepTrialTowerAsync(5);

        Assert.True(result.Accepted);
        Assert.True(result.StoppedBeforeBoss);
        Assert.Equal(2, result.ClearedFloors);
        Assert.Equal(9, result.EndFloor);
        Assert.Equal(9, result.State.HighestTrialTowerFloor);

        var bossBlocked = await session.SweepTrialTowerAsync(5);
        Assert.False(bossBlocked.Accepted);
        Assert.True(bossBlocked.StoppedBeforeBoss);
        Assert.Equal(9, bossBlocked.State.HighestTrialTowerFloor);
    }

    [Fact]
    public async Task Sweep_StopsOnFirstFailureWithoutAwardingThatFloor()
    {
        var store = new InMemoryProgressStore();
        var session = SessionFactory.Create(store, 99);
        var before = await session.InitializeAsync("tower-sweep-fail");

        var result = await session.SweepTrialTowerAsync(5);

        Assert.True(result.Accepted);
        Assert.True(result.Failed);
        Assert.Equal(0, result.ClearedFloors);
        Assert.Equal(0, result.State.HighestTrialTowerFloor);
        Assert.Equal(before.HeroSummonTickets, result.State.HeroSummonTickets);
        Assert.Equal(before.Gold, result.State.Gold);
        Assert.Equal(1, result.ConsumedChallengeTickets);
    }

    [Fact]
    public async Task Challenge_WithNoTicket_IsRejectedWithoutRolling()
    {
        var now = new DateTimeOffset(2026, 8, 28, 0, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-empty", value =>
        {
            value.TrialTowerChallengeTickets = 0;
            value.TrialTowerTicketUpdatedUtc = now.UtcDateTime;
        });
        var session = SessionFactory.Create(store, 0, clock);
        await session.InitializeAsync("tower-empty");

        var result = await session.ChallengeTrialTowerAsync(1);

        Assert.False(result.Accepted);
        Assert.False(result.Victory);
        Assert.Equal(0, result.State.TrialTowerChallengeTickets);
        Assert.Equal(0, result.ConsumedChallengeTickets);
    }

    [Fact]
    public async Task ChallengeTickets_RecoverOneEveryTwoHoursUpToNaturalCap()
    {
        var now = new DateTimeOffset(2026, 8, 28, 0, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-recovery", value =>
        {
            value.TrialTowerChallengeTickets = 1;
            value.TrialTowerTicketUpdatedUtc = now.UtcDateTime;
        });
        var session = SessionFactory.Create(store, timeProvider: clock);
        var initial = await session.InitializeAsync("tower-recovery");

        clock.Advance(TimeSpan.FromHours(4));
        var recovered = await session.RefreshTrialTowerTicketsAsync();

        Assert.Equal(1, initial.TrialTowerChallengeTickets);
        Assert.Equal(3, recovered.TrialTowerChallengeTickets);
        Assert.Equal(now.UtcDateTime.AddHours(6), recovered.NextTrialTowerTicketUtc);
    }

    [Fact]
    public async Task PremiumCurrency_CanPurchaseStoredChallengeTicket()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-ticket-shop", value => value.PremiumCurrency = 100);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("tower-ticket-shop");

        var result = await session.PurchaseTrialTowerChallengeTicketsAsync();
        var restored = await SessionFactory.Create(store).InitializeAsync("tower-ticket-shop");

        Assert.True(result.Purchased);
        Assert.Equal(1, result.TicketsPurchased);
        Assert.Equal(TrialTowerRules.ChallengeTicketPurchaseCost, result.PremiumSpent);
        Assert.Equal(6, restored.TrialTowerChallengeTickets);
        Assert.Equal(80, restored.PremiumCurrency);
    }

    [Fact]
    public async Task TowerPower_UsesFullSelectedPartyContribution()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("tower-party-power", value =>
        {
            value.HighestClearedStage = 100;
            value.OwnedCompanionIds = ["haejin"];
            value.SelectedCompanionIds = ["haejin"];
            value.CompanionLevels["haejin"] = 20;
        });
        var snapshot = await SessionFactory.Create(store).InitializeAsync("tower-party-power");

        Assert.Equal(snapshot.AttackPower, snapshot.TrialTowerPartyPower);
        Assert.True(snapshot.TrialTowerPartyPower > snapshot.Party.InheritedAttackPower);
        Assert.Single(snapshot.Party.Companions);
    }

    [Fact]
    public void GrosslyUnderpoweredParty_HasNoLuckyMinimumChance()
    {
        var floor = TrialTowerRules.Describe(130);

        var chance = TrialTowerRules.SuccessChance(18_605, floor);

        Assert.Equal(0, chance);
    }

    [Theory]
    [InlineData(7000, 700, 94)]
    [InlineData(7000, 699, 97)]
    [InlineData(7968, 164, 100)]
    public void MainStageProgress_MakesTenPercentTowerRangeComfortable(int stage, int floorNumber, int minimumChance)
    {
        var floor = TrialTowerRules.Describe(floorNumber);

        var chance = TrialTowerRules.SuccessChance(1, floor, stage);

        Assert.True(chance >= minimumChance, $"Stage {stage} should comfortably clear tower floor {floorNumber}, but chance was {chance}%.");
    }
}
