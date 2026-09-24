using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class CompanionSummonTests
{
    [Fact]
    public async Task FirstStageClear_GrantsOneTicketAndReplayDoesNotDuplicateIt()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("ticket", value => { value.AttackLevel = 1_000; value.EnemyHp = 1; });
        var firstSession = SessionFactory.Create(store, 50);
        var firstState = await firstSession.InitializeAsync("ticket");
        await CombatTestDriver.DefeatEncounterGuardsAsync(firstSession, firstState);

        var firstClear = await firstSession.AttackAsync(AttackSource.Manual);
        await store.SeedAsync("ticket", value =>
        {
            value.Stage = 1;
            value.AttackLevel = 1_000;
            value.EnemyHp = 1;
        });
        var replaySession = SessionFactory.Create(store, 50);
        var replayState = await replaySession.InitializeAsync("ticket");
        await CombatTestDriver.DefeatEncounterGuardsAsync(replaySession, replayState);
        var replay = await replaySession.AttackAsync(AttackSource.Manual);

        Assert.True(firstClear.EnemyDefeated);
        Assert.Equal(1, firstClear.State.HeroSummonTickets);
        Assert.True(replay.EnemyDefeated);
        Assert.Equal(1, replay.State.HeroSummonTickets);
    }

    [Fact]
    public async Task Summon_CanRecruitNewCompanionAndPersistsOwnership()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("recruit", value => value.HeroSummonTickets = 1);
        var session = SessionFactory.Create(store, 3);
        await session.InitializeAsync("recruit");

        var result = await session.SummonCompanionAsync();
        var restored = await SessionFactory.Create(store).InitializeAsync("recruit");

        Assert.True(result.Accepted);
        Assert.Equal(CompanionSummonRewardKind.Companion, result.Reward?.Kind);
        Assert.Equal("haejin", result.Reward?.HeroId);
        Assert.Equal(0, result.State.HeroSummonTickets);
        Assert.Contains(restored.Party.AvailableCompanions, hero => hero.HeroId == "haejin");
    }

    [Fact]
    public async Task DuplicateHero_ConvertsToRandomQuantityOfItsShards()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("duplicate", value =>
        {
            value.HeroSummonTickets = 1;
            value.OwnedCompanionIds = ["haejin"];
        });
        var session = SessionFactory.Create(store, 3);
        await session.InitializeAsync("duplicate");

        var result = await session.SummonCompanionAsync();

        Assert.True(result.Accepted);
        Assert.Equal(CompanionSummonRewardKind.CompanionShard, result.Reward?.Kind);
        Assert.True(result.Reward?.WasDuplicate);
        Assert.InRange(result.Reward!.Quantity, 8, 15);
        Assert.Equal((int)result.Reward.Quantity, result.State.CompanionShards["haejin"]);
    }

    [Fact]
    public async Task NoHeroRoll_GrantsMaterialAndAdvancesPity()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("material", value => value.HeroSummonTickets = 1);
        var session = SessionFactory.Create(store, 50);
        await session.InitializeAsync("material");

        var result = await session.SummonCompanionAsync();

        Assert.Equal(CompanionSummonRewardKind.UpgradeMaterial, result.Reward?.Kind);
        Assert.Equal(20, result.Reward?.Quantity);
        Assert.Equal(20, result.State.CompanionUpgradeMaterials);
        Assert.Equal(1, result.State.HeroSummonPity);
    }

    [Fact]
    public async Task FiftiethRollGuaranteesEpicHeroEvenWhenNormalRollWouldGiveGold()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("pity", value =>
        {
            value.HeroSummonTickets = 1;
            value.HeroSummonPity = CompanionSummonPolicy.PityThreshold;
        });
        var session = SessionFactory.Create(store, 99);
        await session.InitializeAsync("pity");

        var result = await session.SummonCompanionAsync();

        Assert.True(result.Reward?.RolledHero);
        Assert.Equal(CompanionSummonRewardKind.Companion, result.Reward?.Kind);
        Assert.Equal(CompanionRarity.Epic, result.Reward?.Rarity);
        Assert.Equal(0, result.State.HeroSummonPity);
    }

    [Fact]
    public async Task FiveRollBatch_ConsumesSelectedTicketCountAndReturnsEveryReward()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("five-roll", value => value.HeroSummonTickets = 7);
        var session = SessionFactory.Create(store, 50);
        await session.InitializeAsync("five-roll");

        var result = await session.SummonCompanionsAsync(5);

        Assert.True(result.Accepted);
        Assert.Equal(5, result.Rewards.Count);
        Assert.Equal(2, result.State.HeroSummonTickets);
    }

    [Fact]
    public async Task ThousandthRollGuaranteesLegendaryAndResetsBothPityCounters()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("legend-pity", value =>
        {
            value.HeroSummonTickets = 1;
            value.HeroSummonPity = CompanionSummonPolicy.PityThreshold;
            value.LegendarySummonPity = CompanionSummonPolicy.LegendaryPityThreshold;
        });
        var session = SessionFactory.Create(store, 99);
        await session.InitializeAsync("legend-pity");

        var result = await session.SummonCompanionAsync();

        Assert.Equal(CompanionRarity.Legendary, result.Reward?.Rarity);
        Assert.Equal(0, result.State.HeroSummonPity);
        Assert.Equal(0, result.State.LegendarySummonPity);
    }

    [Fact]
    public async Task CodeMaruCurrency_CanPurchaseTenSummonTickets()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("ticket-shop", value => value.PremiumCurrency = 30);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("ticket-shop");

        var result = await session.PurchaseSummonTicketPackAsync("ticket-10");

        Assert.True(result.Purchased);
        Assert.Equal(10, result.State.HeroSummonTickets);
        Assert.Equal(3, result.State.PremiumCurrency);
    }

    [Fact]
    public void CompanionRoster_HasTenHeroesPerRarity_AndRareOnlyStarters()
    {
        Assert.Equal(10, PartyRules.AllCompanions.Count(hero => hero.Rarity == CompanionRarity.Rare));
        Assert.Equal(10, PartyRules.AllCompanions.Count(hero => hero.Rarity == CompanionRarity.Epic));
        Assert.Equal(10, PartyRules.AllCompanions.Count(hero => hero.Rarity == CompanionRarity.Legendary));
        Assert.Equal(5, PartyRules.StarterCompanionIds.Count);
        Assert.All(PartyRules.AllCompanions.Where(hero => PartyRules.StarterCompanionIds.Contains(hero.HeroId)),
            hero => Assert.Equal(CompanionRarity.Rare, hero.Rarity));
    }

    [Fact]
    public void NaturalLegendaryRate_IsHalfPercentBeforePity()
    {
        var legendary = 0;
        for (var rewardRoll = 0; rewardRoll < 100; rewardRoll++)
        for (var quantityRoll = 0; quantityRoll < 100; quantityRoll++)
        {
            var reward = CompanionSummonPolicy.Resolve(
                0, 0, rewardRoll, quantityRoll, 0, Array.Empty<string>(), 1);
            if (reward.Rarity == CompanionRarity.Legendary) legendary++;
        }

        Assert.Equal(CompanionSummonPolicy.LegendaryRateBasisPoints, legendary);
    }
}
