using GamePlatform.Application;

namespace GamePlatform.Web.Tests;

public sealed class EndlessTrialTests
{
    [Fact]
    public async Task Completion_ClampsClientClaimsToServerElapsedTimeAndPersistsRewards()
    {
        var clock = new MutableTrialTimeProvider(new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var store = new InMemoryProgressStore();
        var session = new GameSession(store, new FixedRollSource(0), SessionFactory.Regions, clock);
        var before = await session.InitializeAsync("endless-rewards");
        var run = await session.BeginEndlessTrialAsync();

        clock.Advance(TimeSpan.FromSeconds(120));
        var result = await session.CompleteEndlessTrialAsync(
            run.RunId,
            reportedSurvivalSeconds: 99_999,
            reportedKills: 99_999,
            reportedGoldPickups: 99_999,
            reportedMaterialPickups: 99_999,
            reportedTicketPickups: 99_999);

        Assert.True(result.Accepted);
        Assert.Equal(122, result.SurvivalSeconds);
        Assert.Equal(result.SurvivalSeconds * 8 + 8, result.Kills);
        Assert.InRange(result.SummonTickets, 0, 10);
        Assert.True(result.State.Gold > before.Gold);
        Assert.True(result.State.CompanionUpgradeMaterials > before.CompanionUpgradeMaterials);
        Assert.True(result.State.HeroSummonTickets > before.HeroSummonTickets);
        Assert.NotNull(result.EquipmentDropped);
        Assert.NotNull(result.BoostDropped);

        var restored = await SessionFactory.Create(store).InitializeAsync("endless-rewards");
        Assert.Equal(result.State.Gold, restored.Gold);
        Assert.Equal(result.State.CompanionUpgradeMaterials, restored.CompanionUpgradeMaterials);
        Assert.Equal(result.State.HeroSummonTickets, restored.HeroSummonTickets);
    }

    [Fact]
    public async Task Completion_CannotBeReplayedForDuplicateRewards()
    {
        var clock = new MutableTrialTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        await session.InitializeAsync("endless-replay");
        var run = await session.BeginEndlessTrialAsync();
        clock.Advance(TimeSpan.FromSeconds(30));

        var first = await session.CompleteEndlessTrialAsync(run.RunId, 30, 40, 2, 1, 0);
        var replay = await session.CompleteEndlessTrialAsync(run.RunId, 30, 40, 2, 1, 0);

        Assert.True(first.Accepted);
        Assert.False(replay.Accepted);
        Assert.Equal(first.State.Gold, replay.State.Gold);
        Assert.Equal(first.State.CompanionUpgradeMaterials, replay.State.CompanionUpgradeMaterials);
    }

    private sealed class MutableTrialTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }
}
