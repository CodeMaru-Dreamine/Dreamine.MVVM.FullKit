using GamePlatform.Application;

namespace GamePlatform.Web.Tests;

public sealed class SpiritFarmAndFortressTests
{
    [Fact]
    public async Task SpiritFarm_PersistsGrowthAcrossSessionsAndHarvestsAfterOfflineTime()
    {
        var clock = new MutableFarmTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("farmer", value => value.Gold = 10_000);
        var firstSession = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        var initial = await firstSession.InitializeAsync("farmer");

        var opened = await firstSession.OpenSpiritSeedPackAsync("basic");
        Assert.True(opened.Accepted);
        Assert.Equal(1, opened.State.SpiritFarm.CropSeeds["dew-leaf"]);
        var planted = await firstSession.PlantSpiritCropAsync(0, "dew-leaf");
        Assert.True(planted.Accepted);
        Assert.NotNull(planted.State.SpiritFarm.Plots[0].Crop);
        Assert.True(planted.State.Gold < initial.Gold);

        clock.Advance(TimeSpan.FromMinutes(2));
        var restoredSession = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        var restored = await restoredSession.InitializeAsync("farmer");
        Assert.True(restored.SpiritFarm.Plots[0].IsReady);

        var harvested = await restoredSession.HarvestSpiritCropAsync(0);
        Assert.True(harvested.Accepted);
        Assert.Null(harvested.State.SpiritFarm.Plots[0].Crop);
        Assert.Equal(1_050, harvested.GoldReward);
        Assert.Equal(1, harvested.MaterialReward);
    }

    [Fact]
    public async Task SpiritFarm_RepeatingSameCropReducesSoilVitality()
    {
        var clock = new MutableFarmTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("rotation-farmer", value => value.Gold = 20_000);
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        await session.InitializeAsync("rotation-farmer");
        await session.OpenSpiritSeedPackAsync("basic");
        await session.OpenSpiritSeedPackAsync("basic");

        await session.PlantSpiritCropAsync(0, "dew-leaf");
        clock.Advance(TimeSpan.FromMinutes(2));
        var firstHarvest = await session.HarvestSpiritCropAsync(0);
        await session.PlantSpiritCropAsync(0, "dew-leaf");
        clock.Advance(TimeSpan.FromMinutes(2));
        var secondHarvest = await session.HarvestSpiritCropAsync(0);

        Assert.Equal(100, firstHarvest.State.SpiritFarm.Plots[0].SoilVitality);
        Assert.Equal(88, secondHarvest.State.SpiritFarm.Plots[0].SoilVitality);
        Assert.Equal(2, secondHarvest.State.SpiritFarm.Plots[0].ConsecutiveCrops);
    }

    [Fact]
    public async Task SpiritFarm_FriendFertilizerShortensGrowthAndRewardsHelper()
    {
        var clock = new MutableFarmTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("farm-friend", value =>
        {
            value.Gold = 10_000;
            value.SpiritFarmPlots =
            [
                new GamePlatform.Domain.SpiritFarmPlotState
                {
                    PlotIndex = 0,
                    CropKey = "moon-orchid",
                    PlantedUtc = clock.GetUtcNow().UtcDateTime,
                    ReadyUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(5)
                }
            ];
        });
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        var before = await session.InitializeAsync("farm-helper");
        var originalReady = (await store.ReadAsync("farm-friend")).SpiritFarmPlots[0].ReadyUtc;

        var friends = await session.GetSpiritFarmFriendsAsync();
        var result = await session.HelpFriendFarmAsync("farm-friend");
        var target = await store.ReadAsync("farm-friend");

        Assert.Contains(friends, friend => friend.UserId == "farm-friend" && friend.CanHelp);
        Assert.True(result.Accepted);
        Assert.Equal(before.CompanionUpgradeMaterials + 1, result.State.CompanionUpgradeMaterials);
        Assert.True(target.SpiritFarmPlots[0].ReadyUtc < originalReady);
        Assert.Contains("farm-helper", target.SpiritFarmPlots[0].FertilizedByUserIds);
    }

    [Fact]
    public async Task SpiritFarm_PremiumCurrencyBuysRequestedSeedPack()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("seed-shopper", value => value.PremiumCurrency = 30);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("seed-shopper");

        var purchased = await session.PurchaseSpiritSeedPackAsync("advanced");

        Assert.True(purchased.Accepted);
        Assert.Equal(12, purchased.State.PremiumCurrency);
        Assert.Equal(1, purchased.State.SpiritFarm.SeedPacks["advanced"]);
    }

    [Fact]
    public async Task SpiritFarm_OwnFertilizerShortensGrowthAndRestoresSoil()
    {
        var clock = new MutableFarmTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("fertilizer-farmer", value => value.Gold = 10_000);
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        var initialized = await session.InitializeAsync("fertilizer-farmer");
        Assert.Equal(2, initialized.SpiritFarm.FertilizerCount);
        await session.OpenSpiritSeedPackAsync("basic");
        await session.PlantSpiritCropAsync(0, "dew-leaf");
        var originalReady = (await store.ReadAsync("fertilizer-farmer")).SpiritFarmPlots[0].ReadyUtc;

        var fertilized = await session.FertilizeOwnSpiritCropAsync(0);
        var persisted = await store.ReadAsync("fertilizer-farmer");

        Assert.True(fertilized.Accepted);
        Assert.Equal(initialized.SpiritFarm.FertilizerCount - 1, fertilized.State.SpiritFarm.FertilizerCount);
        Assert.True(persisted.SpiritFarmPlots[0].OwnerFertilized);
        Assert.True(persisted.SpiritFarmPlots[0].ReadyUtc < originalReady);
    }

    [Fact]
    public async Task SpiritFarm_UpgradeUnlocksExactlyOneAdditionalPlot()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("farm-upgrade", value => value.Gold = 100_000);
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("farm-upgrade");

        var upgraded = await session.UpgradeSpiritFarmAsync();

        Assert.True(upgraded.Accepted);
        Assert.Equal(initial.SpiritFarm.Level + 1, upgraded.State.SpiritFarm.Level);
        Assert.Equal(initial.SpiritFarm.UnlockedPlots + 1, upgraded.State.SpiritFarm.UnlockedPlots);
    }

    [Fact]
    public async Task SpiritFortress_ClampsClaimsAndRejectsReplay()
    {
        var clock = new MutableFarmTimeProvider(DateTimeOffset.UtcNow);
        var store = new InMemoryProgressStore();
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        var before = await session.InitializeAsync("spirit-fortress");
        var run = await session.BeginSpiritFortressRunAsync();
        clock.Advance(TimeSpan.FromSeconds(90));

        var result = await session.CompleteSpiritFortressRunAsync(run.RunId, 99_999, 99_999, 99_999, 99_999, 99_999, 99_999, 99_999);
        var replay = await session.CompleteSpiritFortressRunAsync(run.RunId, 90, 100, 8, 12, 1, 1, 0);

        Assert.True(result.Accepted);
        Assert.Equal(92, result.DurationSeconds);
        Assert.Equal(result.DurationSeconds / 10 + 2, result.Score);
        Assert.Equal(result.DurationSeconds * 5 + 10, result.Defeated);
        Assert.Contains("탑 전력 70", result.Message);
        Assert.True(result.State.Gold > before.Gold);
        Assert.False(replay.Accepted);
    }

    private sealed class MutableFarmTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }
}
