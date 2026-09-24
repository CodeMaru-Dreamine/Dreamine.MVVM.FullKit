using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class PartyAndChatTests
{
    [Fact]
    public void RecommendedFormation_UsesCurrentGrowthPowerAndLimitsPartySize()
    {
        var progress = new GameProgress
        {
            OwnedCompanionIds = PartyRules.AllCompanions.Take(7).Select(hero => hero.HeroId).ToList()
        };
        progress.CompanionLevels[PartyRules.AllCompanions[6].HeroId] = 50;
        var candidates = PartyRules.AllCompanions.Take(7)
            .Select(hero => CompanionProgressionRules.Resolve(progress, hero))
            .ToArray();

        var recommended = PartyRules.RecommendFormation(candidates);

        Assert.Equal(PartyRules.MaximumCompanions, recommended.Count);
        Assert.Equal(PartyRules.AllCompanions[6].HeroId, recommended[0].Hero.HeroId);
        Assert.True(recommended.Zip(recommended.Skip(1), (left, right) => left.AttackPercent >= right.AttackPercent).All(value => value));
    }

    [Fact]
    public void BasePotentialRecommendation_IgnoresLevelsRanksAndEquipmentGrowth()
    {
        var progress = new GameProgress
        {
            OwnedCompanionIds = PartyRules.AllCompanions.Take(7).Select(hero => hero.HeroId).ToList()
        };
        progress.CompanionLevels[PartyRules.AllCompanions[0].HeroId] = 10_000;
        var candidates = PartyRules.AllCompanions.Take(7)
            .Select(hero => CompanionProgressionRules.Resolve(progress, hero))
            .ToArray();

        var current = PartyRules.RecommendFormation(candidates, FormationRecommendationMode.CurrentPower);
        var potential = PartyRules.RecommendFormation(candidates, FormationRecommendationMode.BasePotential);

        Assert.Equal(PartyRules.AllCompanions[0].HeroId, current[0].Hero.HeroId);
        Assert.Equal(PartyRules.AllCompanions[6].HeroId, potential[0].Hero.HeroId);
    }

    [Fact]
    public void ContentRecommendations_UseDifferentRolePrioritiesWithoutGrowthValues()
    {
        var progress = new GameProgress
        {
            OwnedCompanionIds = PartyRules.AllCompanions.Select(hero => hero.HeroId).ToList()
        };
        progress.CompanionLevels["haejin"] = 50_000;
        var candidates = PartyRules.AllCompanions
            .Select(hero => CompanionProgressionRules.Resolve(progress, hero))
            .ToArray();

        var exploration = PartyRules.RecommendFormation(candidates, FormationRecommendationMode.Exploration);
        var tower = PartyRules.RecommendFormation(candidates, FormationRecommendationMode.TrialTower);

        Assert.Equal(CompanionWeaponType.Spear, exploration[0].Hero.WeaponType);
        Assert.Equal(CompanionWeaponType.Mace, tower[0].Hero.WeaponType);
        Assert.NotEqual("haejin", exploration[0].Hero.HeroId);
        Assert.NotEqual(exploration.Select(item => item.Hero.HeroId), tower.Select(item => item.Hero.HeroId));
    }

    [Fact]
    public void Party_UsesOwnedStarterCompanionsAndKeepsInheritedGrowthSeparate()
    {
        var inherited = GameRules.AttackPower(20, 10, 8);

        var party = PartyRules.Resolve(
            100,
            "yeonsu",
            Array.Empty<string>(),
            inherited,
            PartyRules.StarterCompanionIds);

        Assert.Equal(5, party.CompanionCount);
        Assert.Equal(10, party.AssistPowerPercent);
        Assert.Equal(inherited, party.InheritedAttackPower);
        Assert.Equal(GameRules.PartyAttackPower(inherited, 10), party.PartyAttackPower);
    }

    [Fact]
    public void Party_RespectsOwnedRosterAndMaximumSize()
    {
        var noOwnedCompanions = PartyRules.Resolve(14, "yeonsu", Array.Empty<string>(), 100, []);
        var twoOwnedCompanions = PartyRules.Resolve(49, "yeonsu", Array.Empty<string>(), 100, ["haejin", "seolbi"]);
        var complete = PartyRules.Resolve(
            999,
            "yeonsu",
            Enumerable.Repeat("haejin", 8),
            100,
            PartyRules.AllCompanions.Select(hero => hero.HeroId));

        Assert.Empty(noOwnedCompanions.Companions);
        Assert.Equal(2, twoOwnedCompanions.CompanionCount);
        Assert.InRange(complete.CompanionCount, 1, PartyRules.MaximumCompanions);
    }

    [Fact]
    public async Task CompanionRoster_IsSeparateFromFiveSlotSelectionAndSelectionPersists()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("party-selection", value =>
        {
            value.HighestClearedStage = 100;
            value.HighestSummonRewardedStage = 100;
            value.OwnedCompanionIds = PartyRules.AllCompanions.Select(hero => hero.HeroId).ToList();
            value.SelectedCompanionIds = ["haejin", "seolbi", "muwon", "gajin", "cheongha"];
        });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("party-selection");

        var changed = await session.SetCompanionsAsync(["haejin", "cheongha"]);
        var restored = await SessionFactory.Create(store).InitializeAsync("party-selection");

        Assert.Equal(PartyRules.AllCompanions.Count, changed.Party.AvailableCompanions.Count);
        Assert.Equal(2, changed.Party.CompanionCount);
        Assert.Equal(["haejin", "cheongha"], changed.Party.Companions.Select(hero => hero.HeroId));
        Assert.Equal(["haejin", "cheongha"], restored.Party.Companions.Select(hero => hero.HeroId));
    }

    [Fact]
    public async Task ChangingCompanionFormation_PreservesAutoAttackStateAndCombatPhase()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("party-auto", value =>
        {
            value.Stage = 20;
            value.HighestClearedStage = 20;
            value.AutoAttackUnlocked = true;
            value.AutoAttackEnabled = true;
            value.OwnedCompanionIds = ["haejin", "seolbi", "muwon"];
            value.SelectedCompanionIds = ["haejin", "seolbi"];
        });
        var session = SessionFactory.Create(store);
        var initialized = await session.InitializeAsync("party-auto");
        var fighting = initialized.Phase == CombatPhase.Fighting
            ? initialized
            : await session.AdvanceCombatPhaseAsync();

        var changed = await session.SetCompanionsAsync(["seolbi", "haejin"]);

        Assert.True(changed.AutoAttackUnlocked);
        Assert.True(changed.AutoAttackEnabled);
        Assert.Equal(fighting.Phase, changed.Phase);
        Assert.Equal(["seolbi", "haejin"], changed.Party.Companions.Select(hero => hero.HeroId));
    }

    [Fact]
    public void CompanionAssist_IncreasesActualDamageWithoutChangingInheritedLevels()
    {
        var inheritedDamage = GameRules.CalculateDamage(20, 50, 10, 8, 0);
        var partyDamage = GameRules.CalculateDamage(20, 50, 10, 8, 14);

        Assert.True(partyDamage.Damage > inheritedDamage.Damage);
        Assert.Equal(
            GameRules.AttackPower(20, 10, 8),
            PartyRules.Resolve(100, "yeonsu", Array.Empty<string>(), GameRules.AttackPower(20, 10, 8)).InheritedAttackPower);
    }

    [Fact]
    public void DevelopmentReset_ReturnsToStageOneWithRareStarterParty()
    {
        var progress = new GameProgress
        {
            UserId = "local-visual-test",
            Stage = 294,
            HighestClearedStage = 293,
            Gold = 999_999,
            SelectedCompanionIds = ["haejin"]
        };

        GameProgressResetPolicy.ResetToStageOne(progress);
        var party = PartyRules.Resolve(
            progress.HighestClearedStage,
            progress.ActiveHeroId,
            progress.SelectedCompanionIds,
            1,
            progress.OwnedCompanionIds);

        Assert.Equal("local-visual-test", progress.UserId);
        Assert.Equal(1, progress.Stage);
        Assert.Equal(0, progress.HighestClearedStage);
        Assert.Equal(PartyRules.StarterCompanionIds, progress.OwnedCompanionIds);
        Assert.Equal(PartyRules.StarterCompanionIds, progress.SelectedCompanionIds);
        Assert.Equal(PartyRules.StarterCompanionIds, party.Companions.Select(hero => hero.HeroId));
        Assert.All(party.AvailableCompanions, hero => Assert.Equal(CompanionRarity.Rare, hero.Rarity));
    }

    [Fact]
    public async Task Chat_PostsValidatedMessageAndPublishesItOnce()
    {
        var store = new InMemoryChatStore();
        var settingsStore = new InMemorySettingsStore();
        await settingsStore.SaveAsync("user-1", ConfirmedSettings("cheongun-1"));
        var service = new GameChatService(store, settingsStore, TimeProvider.System);
        var published = new List<GameChatMessage>();
        service.MessagePublished += published.Add;

        var first = await service.PostAsync("user-1", "cheongun-1", "검객", " 안녕하세요 ");
        var tooFast = await service.PostAsync("user-1", "cheongun-1", "검객", "연속 메시지");

        Assert.True(first.Accepted);
        Assert.Equal("cheongun-1", first.Message?.ChannelKey);
        Assert.Equal("안녕하세요", first.Message?.Text);
        Assert.False(tooFast.Accepted);
        Assert.Single(published);
        Assert.Single(await service.LoadRecentAsync("cheongun-1"));
        Assert.Empty(await service.LoadRecentAsync("baekya-1"));
    }

    [Fact]
    public async Task Chat_RejectsUnconfirmedOrDifferentServer()
    {
        var store = new InMemoryChatStore();
        var settingsStore = new InMemorySettingsStore();
        await settingsStore.SaveAsync("confirmed", ConfirmedSettings("baekya-2"));
        var service = new GameChatService(store, settingsStore, TimeProvider.System);

        var missingServer = await service.PostAsync("new-user", "cheongun-1", "신규", "안녕하세요");
        var differentServer = await service.PostAsync("confirmed", "cheongun-1", "백야", "다른 서버 메시지");
        var accepted = await service.PostAsync("confirmed", "baekya-2", "백야", "같은 서버 메시지");

        Assert.False(missingServer.Accepted);
        Assert.False(differentServer.Accepted);
        Assert.True(accepted.Accepted);
        Assert.Empty(await service.LoadRecentAsync("cheongun-1"));
        Assert.Single(await service.LoadRecentAsync("baekya-2"));
    }

    private static GameSettings ConfirmedSettings(string channelKey) => new()
    {
        General = new GeneralGameSettings { ChannelKey = channelKey, ChannelConfirmed = true }
    };

    private sealed class InMemoryChatStore : IGameChatStore
    {
        private readonly List<GameChatMessage> _messages = [];

        public Task<IReadOnlyList<GameChatMessage>> LoadRecentAsync(string channelKey, int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameChatMessage>>(_messages
                .Where(message => string.Equals(message.ChannelKey, channelKey, StringComparison.OrdinalIgnoreCase))
                .TakeLast(count)
                .ToArray());

        public Task AppendAsync(GameChatMessage message, CancellationToken cancellationToken = default)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySettingsStore : IGameSettingsStore
    {
        private readonly Dictionary<string, GameSettings> _settings = new(StringComparer.OrdinalIgnoreCase);

        public Task<GameSettings?> FindAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_settings.GetValueOrDefault(userId));

        public Task SaveAsync(string userId, GameSettings settings, CancellationToken cancellationToken = default)
        {
            _settings[userId] = settings;
            return Task.CompletedTask;
        }
    }
}
