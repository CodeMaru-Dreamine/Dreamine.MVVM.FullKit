using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class BoostAndEconomyTests
{
    [Fact]
    public async Task NonLethalAttack_EarnsStageScaledGoldWhileProgressIsBlocked()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("blocked", value =>
        {
            value.Stage = 65;
            value.EnemyHp = GameRules.EnemyMaxHp(65);
        });
        var session = SessionFactory.Create(store, 50);
        await session.InitializeAsync("blocked");

        var attack = await session.AttackAsync(AttackSource.Manual);
        var restored = await SessionFactory.Create(store).InitializeAsync("blocked");

        Assert.True(attack.Accepted);
        Assert.False(attack.EnemyDefeated);
        Assert.Equal(7, attack.State.Gold);
        Assert.Equal(7, restored.Gold);
    }

    [Fact]
    public void BaseAutoAndContinuousManual_UseTheSameFourHitsPerSecondRhythm()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(250), GameRules.DefaultAutoAttackInterval);
        Assert.Equal(GameRules.DefaultAutoAttackInterval, GameRules.AutoAttackInterval(1));
        Assert.Equal(TimeSpan.FromMilliseconds(225), GameRules.MinimumManualAttackInterval);
        Assert.Equal(TimeSpan.FromMilliseconds(225), GameRules.MinimumAutomaticAttackIntervalFor(1));
    }

    [Fact]
    public async Task BossKill_CanDropTimedFiveTimesBoostAndStoreIt()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("boss-drop", value =>
        {
            value.Stage = 10;
            value.HighestClearedStage = 9;
            value.AttackLevel = 100;
            value.EnemyHp = 1;
        });
        var session = SessionFactory.Create(store, 0);
        var initial = await session.InitializeAsync("boss-drop");
        await CombatTestDriver.DefeatEncounterGuardsAsync(session, initial);
        var victory = await session.AttackAsync(AttackSource.Manual);

        Assert.True(victory.EnemyDefeated);
        Assert.NotNull(victory.AutoSpeedBoostDropped);
        Assert.NotNull(victory.EquipmentDropped);
        Assert.Equal(4, victory.State.CompanionEquipmentInventory.Count);
        Assert.Equal(5, victory.AutoSpeedBoostDropped.Multiplier);
        Assert.Equal(TimeSpan.FromMinutes(20), victory.AutoSpeedBoostDropped.Duration);
        Assert.Contains(victory.State.AutoBoostInventory,
            entry => entry.Item.ItemKey == "auto-x5-20m" && entry.Count == 1);
    }

    [Fact]
    public async Task ActivatingBoost_ConsumesItemAndChangesOnlyTimedAutoInterval()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("activate", value => value.AutoBoostInventory["auto-x5-20m"] = 1);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("activate");

        var activated = await session.ActivateAutoSpeedBoostAsync("auto-x5-20m");

        Assert.True(activated.Activated);
        Assert.Equal(5, activated.State.ActiveAutoSpeedMultiplier);
        Assert.Equal(TimeSpan.FromMilliseconds(50), activated.State.AutoAttackInterval);
        Assert.NotNull(activated.State.AutoSpeedBoostEndsUtc);
        Assert.Empty(activated.State.AutoBoostInventory);
    }

    [Fact]
    public async Task MixedBoostInventory_ConsumesOnlySelectedItemAndRejectsStacking()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("mixed-boosts", value =>
        {
            value.AutoBoostInventory["auto-x5-5m"] = 1;
            value.AutoBoostInventory["auto-x3-10m"] = 2;
            value.AutoBoostInventory["auto-x2-20m"] = 3;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("mixed-boosts");

        var activated = await session.ActivateAutoSpeedBoostAsync("auto-x3-10m");
        var stacked = await session.ActivateAutoSpeedBoostAsync("auto-x2-20m");

        Assert.True(activated.Activated);
        Assert.Equal(3, activated.State.ActiveAutoSpeedMultiplier);
        Assert.Contains(activated.State.AutoBoostInventory, entry => entry.Item.ItemKey == "auto-x3-10m" && entry.Count == 1);
        Assert.Contains(activated.State.AutoBoostInventory, entry => entry.Item.ItemKey == "auto-x5-5m" && entry.Count == 1);
        Assert.Contains(activated.State.AutoBoostInventory, entry => entry.Item.ItemKey == "auto-x2-20m" && entry.Count == 3);
        Assert.False(stacked.Activated);
        Assert.Contains(stacked.State.AutoBoostInventory, entry => entry.Item.ItemKey == "auto-x2-20m" && entry.Count == 3);
    }

    [Fact]
    public async Task PremiumBoostPurchase_DeductsCodeMaruCurrencyAndAddsUsableItem()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("boost-shop", value => value.PremiumCurrency = 100);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("boost-shop");

        var purchase = await session.PurchaseAutoSpeedBoostAsync("auto-x3-10m");
        var activation = await session.ActivateAutoSpeedBoostAsync("auto-x3-10m");

        Assert.True(purchase.Purchased);
        Assert.NotNull(purchase.Item);
        Assert.Equal(60, purchase.Item.PriceC);
        Assert.Equal(40, purchase.State.PremiumCurrency);
        Assert.Contains(purchase.State.AutoBoostInventory,
            entry => entry.Item.ItemKey == "auto-x3-10m" && entry.Count == 1);
        Assert.True(activation.Activated);
        Assert.Equal(3, activation.State.ActiveAutoSpeedMultiplier);
        Assert.Empty(activation.State.AutoBoostInventory);
    }

    [Fact]
    public async Task PremiumBoostPurchase_WithInsufficientCurrency_DoesNotChangeInventory()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("boost-shop-poor", value => value.PremiumCurrency = 10);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("boost-shop-poor");

        var purchase = await session.PurchaseAutoSpeedBoostAsync("auto-x5-20m");

        Assert.False(purchase.Purchased);
        Assert.Equal(10, purchase.State.PremiumCurrency);
        Assert.Empty(purchase.State.AutoBoostInventory);
        Assert.Contains("240", purchase.RejectionReason);
    }

    [Fact]
    public async Task PremiumBoostBatchPurchase_DeductsTotalPriceAndAddsSelectedQuantity()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("boost-shop-batch", value => value.PremiumCurrency = 1_000);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("boost-shop-batch");

        var purchase = await session.PurchaseAutoSpeedBoostAsync("auto-x3-10m", 10);

        Assert.True(purchase.Purchased);
        Assert.Equal(400, purchase.State.PremiumCurrency);
        Assert.Contains(purchase.State.AutoBoostInventory,
            entry => entry.Item.ItemKey == "auto-x3-10m" && entry.Count == 10);
    }

    [Fact]
    public async Task MultipleBrowserSessions_CannotMultiplyAutoAttackRateForSameUser()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("multi-tab", value =>
        {
            value.Stage = 16;
            value.HighestClearedStage = 15;
            value.AutoAttackUnlocked = true;
            value.AutoAttackEnabled = true;
            value.EnemyHp = GameRules.EnemyMaxHp(16);
        });
        var sharedLimiter = new GameAttackRateLimiter();
        var first = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, attackRateLimiter: sharedLimiter);
        var second = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, attackRateLimiter: sharedLimiter);
        await first.InitializeAsync("multi-tab");
        await second.InitializeAsync("multi-tab");

        var accepted = await first.AttackAsync(AttackSource.Automatic);
        var rejected = await second.AttackAsync(AttackSource.Automatic);

        Assert.True(accepted.Accepted);
        Assert.False(rejected.Accepted);
        Assert.Equal("공격 간격이 너무 짧습니다.", rejected.RejectionReason);
    }

    [Fact]
    public async Task ActiveBoost_CanBeCancelledWithoutRefundSoAnotherItemCanBeChosen()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("cancel-boost", value =>
        {
            value.AutoBoostInventory["auto-x2-10m"] = 1;
            value.AutoBoostInventory["auto-x5-5m"] = 1;
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("cancel-boost");
        var first = await session.ActivateAutoSpeedBoostAsync("auto-x2-10m");

        var cancelled = await session.DeactivateAutoSpeedBoostAsync();
        var replacement = await session.ActivateAutoSpeedBoostAsync("auto-x5-5m");

        Assert.True(first.Activated);
        Assert.Equal(1, cancelled.ActiveAutoSpeedMultiplier);
        Assert.Null(cancelled.AutoSpeedBoostEndsUtc);
        Assert.DoesNotContain(cancelled.AutoBoostInventory, entry => entry.Item.ItemKey == "auto-x2-10m");
        Assert.True(replacement.Activated);
        Assert.Equal(5, replacement.State.ActiveAutoSpeedMultiplier);
    }

    [Fact]
    public async Task PausedBoost_DoesNotConsumeDurationWhileGameIsClosed()
    {
        var start = new DateTimeOffset(2030, 4, 5, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(start);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("paused-boost", value => value.AutoBoostInventory["auto-x2-5m"] = 1);
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        await session.InitializeAsync("paused-boost");
        await session.ActivateAutoSpeedBoostAsync("auto-x2-5m");

        clock.Advance(TimeSpan.FromMinutes(2));
        var paused = await session.PauseAutoSpeedBoostAsync();
        var saved = await store.ReadAsync("paused-boost");
        clock.Advance(TimeSpan.FromHours(8));
        var restored = await new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock)
            .InitializeAsync("paused-boost");

        Assert.Equal(2, paused.ActiveAutoSpeedMultiplier);
        Assert.Null(paused.AutoSpeedBoostEndsUtc);
        Assert.Equal((long)TimeSpan.FromMinutes(3).TotalMilliseconds, saved.AutoSpeedBoostRemainingMilliseconds);
        Assert.Equal(2, restored.ActiveAutoSpeedMultiplier);
        Assert.NotNull(restored.AutoSpeedBoostEndsUtc);
        Assert.Equal(TimeSpan.FromMinutes(3), restored.AutoSpeedBoostEndsUtc!.Value - clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task LegacyExpiredBoost_RestoresDurationRemainingAtLastSavedPlayTime()
    {
        var start = new DateTimeOffset(2030, 4, 5, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(start.AddHours(2));
        var store = new InMemoryProgressStore();
        await store.SeedAsync("legacy-offline-boost", value =>
        {
            value.ActiveAutoSpeedMultiplier = 3;
            value.AutoSpeedBoostEndsUtc = start.AddMinutes(10).UtcDateTime;
            value.UpdatedUtc = start.AddMinutes(4).UtcDateTime;
        });

        var restored = await new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock)
            .InitializeAsync("legacy-offline-boost");

        Assert.Equal(3, restored.ActiveAutoSpeedMultiplier);
        Assert.NotNull(restored.AutoSpeedBoostEndsUtc);
        Assert.Equal(TimeSpan.FromMinutes(6), restored.AutoSpeedBoostEndsUtc!.Value - clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task ExpiredBoost_KeepsSavedAutoEnabledAndAllowsNextAutomaticAttack()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2030, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryProgressStore();
        await store.SeedAsync("boost-auto-continuity", value =>
        {
            value.Stage = 16;
            value.HighestClearedStage = 15;
            value.AutoAttackUnlocked = true;
            value.AutoAttackEnabled = true;
            value.EnemyHp = GameRules.EnemyMaxHp(16);
            value.AutoBoostInventory["auto-x2-5m"] = 1;
        });
        var session = new GameSession(store, new FixedRollSource(50), SessionFactory.Regions, clock);
        await session.InitializeAsync("boost-auto-continuity");
        await session.ActivateAutoSpeedBoostAsync("auto-x2-5m");

        clock.Advance(TimeSpan.FromMinutes(6));
        var manual = await session.AttackAsync(AttackSource.Manual);
        var automatic = await session.AttackAsync(AttackSource.Automatic);

        Assert.True(manual.State.AutoAttackEnabled);
        Assert.Equal(1, manual.State.ActiveAutoSpeedMultiplier);
        Assert.True(automatic.Accepted);
        Assert.True(automatic.State.AutoAttackEnabled);
    }

    [Theory]
    [InlineData(80, 80, 2, 5)]
    [InlineData(10, 20, 3, 10)]
    [InlineData(0, 0, 5, 20)]
    public void DropPolicy_UsesWeightedMultiplierAndDuration(
        int multiplierRoll,
        int durationRoll,
        int expectedMultiplier,
        int expectedMinutes)
    {
        var drop = GameRules.ResolveAutoSpeedBoostDrop(0, multiplierRoll, durationRoll);

        Assert.NotNull(drop);
        Assert.Equal(expectedMultiplier, drop.Multiplier);
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), drop.Duration);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(99, false)]
    public void BossBoostDrop_UsesThreePercentChance(int dropRoll, bool shouldDrop)
    {
        var drop = GameRules.ResolveAutoSpeedBoostDrop(dropRoll, 50, 50);

        Assert.Equal(shouldDrop, drop is not null);
    }

    [Theory]
    [InlineData("auto-x10-60m", "auto-x5-20m")]
    [InlineData("auto-x5-30m", "auto-x5-10m")]
    [InlineData("auto-x2-10m", "auto-x2-10m")]
    public void LegacyBoostKeys_AreMappedToCurrentCatalog(string legacyKey, string expectedKey)
    {
        Assert.Equal(expectedKey, GameRules.NormalizeAutoSpeedBoostItemKey(legacyKey));
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan elapsed) => utcNow += elapsed;
    }
}
