using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class ExpeditionServiceTests
{
    [Fact]
    public async Task GoldPackPurchase_DeductsPremiumCurrencyAndPersistsGold()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("gold-pack", value =>
        {
            value.Gold = 125;
            value.PremiumCurrency = 150;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("gold-pack");

        var purchase = await session.PurchaseGoldPackAsync("gold-treasury");
        var restored = await SessionFactory.Create(store).InitializeAsync("gold-pack");

        Assert.True(purchase.Purchased);
        Assert.Equal(30, purchase.State.PremiumCurrency);
        Assert.Equal(250_125, purchase.State.Gold);
        Assert.Equal(250_125, restored.Gold);
    }

    [Fact]
    public async Task GoldPackPurchase_WithInsufficientCurrency_DoesNotChangeGold()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("gold-pack-poor", value =>
        {
            value.Gold = 777;
            value.PremiumCurrency = 20;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("gold-pack-poor");

        var purchase = await session.PurchaseGoldPackAsync("gold-travel");

        Assert.False(purchase.Purchased);
        Assert.Equal(777, purchase.State.Gold);
        Assert.Equal(20, purchase.State.PremiumCurrency);
    }

    [Fact]
    public async Task CompletedDailyMission_CanBeClaimedOnlyOnceAndPersists()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("daily-claim", value =>
        {
            value.DailyMissionDateKey = DailyMissionPolicy.TodayKey(DateTime.UtcNow);
            value.DailyDefeatedBaseline = 10;
            value.TotalDefeated = 110;
            value.Gold = 20;
        });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("daily-claim");

        var first = await session.ClaimDailyMissionAsync("defeat-100");
        var duplicate = await session.ClaimDailyMissionAsync("defeat-100");
        var restored = await SessionFactory.Create(store).InitializeAsync("daily-claim");

        Assert.True(initial.DailyMissions.Single(item => item.Definition.MissionKey == "defeat-100").IsCompleted);
        Assert.True(first.Accepted);
        Assert.Equal(5_020, first.State.Gold);
        Assert.False(duplicate.Accepted);
        Assert.Equal(5_020, duplicate.State.Gold);
        Assert.True(restored.DailyMissions.Single(item => item.Definition.MissionKey == "defeat-100").IsClaimed);
    }
}
