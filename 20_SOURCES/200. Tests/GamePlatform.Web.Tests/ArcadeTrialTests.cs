using GamePlatform.Application;

namespace GamePlatform.Web.Tests;

public sealed class ArcadeTrialTests
{
    [Fact]
    public async Task SpiritVeinRun_ClampsClaimsAndPersistsRewards()
    {
        var clock = new MutableArcadeTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        var session = new GameSession(store, new FixedRollSource(0), SessionFactory.Regions, clock);
        var before = await session.InitializeAsync("spirit-vein");
        var run = await session.BeginSpiritVeinRunAsync();
        clock.Advance(TimeSpan.FromSeconds(120));

        var result = await session.CompleteSpiritVeinRunAsync(run.RunId, 99_999, 99_999, 99_999, 99_999, 99_999, 99_999);
        var replay = await session.CompleteSpiritVeinRunAsync(run.RunId, 120, 500, 20, 1, 1, 1);

        Assert.True(result.Accepted);
        Assert.Equal(122, result.DurationSeconds);
        Assert.Equal(result.DurationSeconds * 32 + 96, result.Score);
        Assert.Equal(result.DurationSeconds / 2 + 4, result.Defeated);
        Assert.True(result.State.Gold > before.Gold);
        Assert.True(result.State.CompanionUpgradeMaterials > before.CompanionUpgradeMaterials);
        Assert.NotNull(result.EquipmentDropped);
        Assert.NotNull(result.BoostDropped);
        Assert.False(replay.Accepted);
    }

    [Fact]
    public async Task SwordFormationRun_UsesWaveAndKillRewardsWithoutReplay()
    {
        var clock = new MutableArcadeTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        var before = await session.InitializeAsync("sword-formation");
        var run = await session.BeginSwordFormationRunAsync();
        clock.Advance(TimeSpan.FromSeconds(80));

        var result = await session.CompleteSwordFormationRunAsync(run.RunId, 80, 180, 7, 3, 2, 1);
        var replay = await session.CompleteSwordFormationRunAsync(run.RunId, 80, 180, 7, 3, 2, 1);

        Assert.True(result.Accepted);
        Assert.Equal(80, result.DurationSeconds);
        Assert.Equal(7, result.Score);
        Assert.Equal(180, result.Defeated);
        Assert.True(result.State.Gold > before.Gold);
        Assert.True(result.State.CompanionUpgradeMaterials > before.CompanionUpgradeMaterials);
        Assert.False(replay.Accepted);
    }

    private sealed class MutableArcadeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }
}
