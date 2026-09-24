using GamePlatform.Domain;
using GamePlatform.Application;

namespace GamePlatform.Web.Tests;

public sealed class ArenaTests
{
    [Fact]
    public async Task ReplaySpeed_PersistsAcrossBattlesAndSessions()
    {
        var store = new InMemoryProgressStore();
        var first = SessionFactory.Create(store, roll: 0);
        await first.InitializeAsync("solo");
        Assert.Equal(1, (await first.LoadArenaAsync()).ReplaySpeed);
        Assert.Equal(2, await first.SetArenaReplaySpeedAsync(2));
        await first.ChallengeArenaAsync("bot:wandering-sword");
        var next = SessionFactory.Create(store);
        await next.InitializeAsync("solo");
        Assert.Equal(2, (await next.LoadArenaAsync()).ReplaySpeed);
        Assert.Equal(2, (await next.RefreshArenaOpponentsAsync()).ReplaySpeed);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => next.SetArenaReplaySpeedAsync(3));
        Assert.Equal(2, (await store.ReadAsync("solo")).Arena.ReplaySpeed);
        await next.SetArenaReplaySpeedAsync(1);
        Assert.Equal(1, (await first.LoadArenaAsync()).ReplaySpeed);
    }

    [Fact]
    public async Task Refresh_RotatesNpcCandidatesWithoutChargingAndKeepsOrdinaryLoadsStable()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", value => { value.PremiumCurrency = 200; value.Arena.PurchasedTickets = 4; });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("solo");
        var before = await session.LoadArenaAsync();
        var refreshed = await session.RefreshArenaOpponentsAsync();
        Assert.Equal(1, refreshed.MatchmakingRevision);
        Assert.Empty(before.Opponents.Select(x => x.UserId).Intersect(refreshed.Opponents.Select(x => x.UserId)));
        Assert.Equal(refreshed.Opponents.Select(x => x.UserId), (await session.LoadArenaAsync()).Opponents.Select(x => x.UserId));
        Assert.Equal(200, refreshed.PremiumCurrency);
        Assert.Equal(4, refreshed.PurchasedTickets);
        Assert.Equal(before.DailyChallenges, refreshed.DailyChallenges);
        Assert.Equal(before.Rating, refreshed.Rating);
        // A stale NPC card must not consume an attempt after another tab changes the roster.
        var rejected = await session.ChallengeArenaAsync(before.Opponents[0].UserId);
        Assert.False(rejected.Accepted);
        Assert.Equal(0, (await store.ReadAsync("solo")).Arena.DailyChallenges);
        var battle = await session.ChallengeArenaAsync(refreshed.Opponents[0].UserId);
        Assert.True(battle.Accepted);
        Assert.Equal(refreshed.Opponents[0].EstimatedWinChance, battle.WinChance);
        var second = await session.RefreshArenaOpponentsAsync();
        Assert.Empty(refreshed.Opponents.Select(x => x.UserId).Intersect(second.Opponents.Select(x => x.UserId)));
        var third = await session.RefreshArenaOpponentsAsync();
        Assert.Equal(before.Opponents.Select(x => x.UserId), third.Opponents.Select(x => x.UserId));
    }

    [Fact]
    public async Task Refresh_RotatesRealPlayersWhenMoreCandidatesExist()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", _ => { });
        for (var i = 0; i < 7; i++) await store.SeedAsync($"player-{i}", _ => { });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("solo");
        var before = await session.LoadArenaAsync();
        var after = await session.RefreshArenaOpponentsAsync();
        Assert.All(after.Opponents, x => Assert.False(x.IsBot));
        Assert.NotEmpty(after.Opponents.Select(x => x.UserId).Except(before.Opponents.Select(x => x.UserId)));
        Assert.DoesNotContain(after.Opponents, x => x.UserId == "solo");
    }

    [Theory]
    [InlineData(85, "우세")]
    [InlineData(65, "우세")]
    [InlineData(64, "약우세")]
    [InlineData(55, "약우세")]
    [InlineData(54, "호각")]
    [InlineData(45, "호각")]
    [InlineData(44, "열세")]
    [InlineData(26, "열세")]
    [InlineData(25, "강적")]
    [InlineData(15, "강적")]
    public void Matchup_LabelsFollowServerWinChance(int chance, string label)
    {
        var opponent = new ArenaOpponentView(1, "other", "검객", 100, 1000, "청동", []) { EstimatedWinChance = chance };
        Assert.Equal(label, opponent.MatchupLabel);
    }

    [Fact]
    public async Task Tickets_PurchaseAndConsumeOnlyAfterFreeChallenges()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", v => v.PremiumCurrency = 100);
        var session = SessionFactory.Create(store, roll: 0);
        await session.InitializeAsync("solo");
        var purchase = await session.PurchaseArenaTicketsAsync(5);
        Assert.True(purchase.Purchased);
        Assert.Equal(100, purchase.SpentC);
        Assert.Equal(5, purchase.Count);
        Assert.Equal(0, (await store.ReadAsync("solo")).PremiumCurrency);
        for (var i = 0; i < 10; i++)
            Assert.True((await session.ChallengeArenaAsync("bot:wandering-sword")).Accepted);
        Assert.Equal(5, (await store.ReadAsync("solo")).Arena.PurchasedTickets);
        for (var i = 0; i < 5; i++)
            Assert.True((await session.ChallengeArenaAsync("bot:wandering-sword")).Accepted);
        Assert.False((await session.ChallengeArenaAsync("bot:wandering-sword")).Accepted);
        var lobby = await session.LoadArenaAsync();
        Assert.Equal(0, lobby.RemainingChallenges);
        Assert.Equal(10, lobby.DailyChallenges);
        Assert.Equal(15, lobby.AttackWins);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public async Task Tickets_InvalidQuantityNeverCharges(int count)
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", v => v.PremiumCurrency = 200);
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("solo");
        var result = await session.PurchaseArenaTicketsAsync(count);
        Assert.False(result.Purchased);
        Assert.Equal(0, result.SpentC);
        var saved = await store.ReadAsync("solo");
        Assert.Equal(200, saved.PremiumCurrency);
        Assert.Equal(0, saved.Arena.PurchasedTickets);
    }

    [Fact]
    public async Task Tickets_ConcurrentSessionsCannotOverspendLastCoins()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", v => v.PremiumCurrency = ArenaRules.TicketPriceC);
        var first = SessionFactory.Create(store);
        var second = SessionFactory.Create(store);
        await first.InitializeAsync("solo");
        await second.InitializeAsync("solo");
        var results = await Task.WhenAll(first.PurchaseArenaTicketsAsync(1), second.PurchaseArenaTicketsAsync(1));
        Assert.Single(results, r => r.Purchased);
        Assert.Equal(0, (await store.ReadAsync("solo")).PremiumCurrency);
        Assert.Equal(1, (await store.ReadAsync("solo")).Arena.PurchasedTickets);
        Assert.Equal(0, results.Single(r => !r.Purchased).SpentC);
    }

    [Fact]
    public async Task Tickets_CapAndMidnightRolloverPreservePurchasedBalance()
    {
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 9, 4, 14, 59, 0, TimeSpan.Zero));
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", v =>
        {
            v.PremiumCurrency = 500;
            v.Arena.DailyDateKey = ArenaRules.DailyDateKey(time.GetUtcNow().UtcDateTime);
            v.Arena.DailyChallenges = 10;
            v.Arena.PurchasedTickets = ArenaRules.MaximumPurchasedTickets;
        });
        var session = SessionFactory.Create(store, timeProvider: time);
        await session.InitializeAsync("solo");
        Assert.False((await session.PurchaseArenaTicketsAsync(1)).Purchased);
        Assert.Equal(500, (await store.ReadAsync("solo")).PremiumCurrency);
        time.Advance(TimeSpan.FromMinutes(2));
        var lobby = await session.LoadArenaAsync();
        Assert.Equal(0, lobby.DailyChallenges);
        Assert.Equal(ArenaRules.MaximumPurchasedTickets, lobby.PurchasedTickets);
        Assert.Equal(10 + ArenaRules.MaximumPurchasedTickets, lobby.RemainingChallenges);
    }

    [Fact]
    public void Replay_HitsOnlyLivingUnitsAndFinishesWithSettledWinner()
    {
        foreach (var victory in new[] { true, false })
        for (var allies = 1; allies <= 6; allies++)
        for (var enemies = 1; enemies <= 6; enemies++)
        {
            var hp = new[] { Enumerable.Repeat(100, allies).ToArray(), Enumerable.Repeat(100, enemies).ToArray() };
            var plan = ArenaReplayPlan.Create(victory, allies, enemies);
            Assert.Equal(24, plan.Count);
            foreach (var hit in plan)
            {
                var attacker = hit.AttackerEnemy ? 1 : 0;
                var target = 1 - attacker;
                Assert.True(hp[attacker][hit.Attacker] > 0);
                Assert.True(hp[target][hit.Target] > 0);
                Assert.InRange(hit.Damage, 0, hp[target][hit.Target]);
                Assert.Equal(hp[target][hit.Target] - hit.Damage, hit.RemainingHp);
                hp[target][hit.Target] = hit.RemainingHp;
            }
            Assert.All(hp[victory ? 1 : 0], value => Assert.Equal(0, value));
            Assert.All(hp[victory ? 0 : 1], value => Assert.True(value > 0));
        }
    }

    [Fact]
    public async Task Arena_UsesActualAccountsAndPersistsBothSidesOfVictory()
    {
        var now = new DateTimeOffset(2026, 9, 4, 3, 0, 0, TimeSpan.Zero);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("attacker", value =>
        {
            value.GameNickname = "청룡검주";
            value.AttackLevel = 100;
            value.Stage = 101;
            value.HighestClearedStage = 100;
        });
        await store.SeedAsync("defender", value =>
        {
            value.GameNickname = "백야군주";
            value.AttackLevel = 10;
            value.Stage = 11;
            value.HighestClearedStage = 10;
        });
        var session = SessionFactory.Create(store, roll: 0, timeProvider: new MutableTimeProvider(now));
        await session.InitializeAsync("attacker");

        var lobby = await session.LoadArenaAsync();
        var result = await session.ChallengeArenaAsync("defender");
        var attacker = await store.ReadAsync("attacker");
        var defender = await store.ReadAsync("defender");

        Assert.Equal(5, lobby.Opponents.Count);
        Assert.Equal("백야군주", lobby.Opponents[0].DisplayName);
        Assert.False(lobby.Opponents[0].IsBot);
        Assert.Equal(4, lobby.Opponents.Count(item => item.IsBot));
        Assert.DoesNotContain(lobby.Leaderboard, item => item.IsBot);
        Assert.True(result.Accepted);
        Assert.True(result.Victory);
        Assert.True(result.RatingDelta > 0);
        Assert.True(result.GoldReward > 0);
        Assert.Equal(1, attacker.Arena.AttackWins);
        Assert.Equal(1, attacker.Arena.DailyChallenges);
        Assert.True(attacker.Arena.Rating > ArenaRules.InitialRating);
        Assert.Equal(1, defender.Arena.DefenseLosses);
        Assert.True(defender.Arena.Rating < ArenaRules.InitialRating);
    }

    [Fact]
    public async Task Arena_FillsLowPopulationMatchmakingWithBotsWithoutAddingThemToRanking()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("solo", value =>
        {
            value.GameNickname = "홀로검객";
            value.AttackLevel = 30;
            value.HighestClearedStage = 40;
        });
        var session = SessionFactory.Create(store, roll: 0);
        await session.InitializeAsync("solo");

        var lobby = await session.LoadArenaAsync();
        var bot = Assert.Single(lobby.Opponents, item => item.UserId == "bot:wandering-sword");
        var result = await session.ChallengeArenaAsync(bot.UserId);

        Assert.Equal(5, lobby.Opponents.Count);
        Assert.All(lobby.Opponents, item => Assert.True(item.IsBot));
        Assert.Single(lobby.Leaderboard);
        Assert.False(lobby.Leaderboard[0].IsBot);
        Assert.True(result.Accepted);
        Assert.True(result.Victory);
        Assert.Single(await store.ListAsync());
    }

    [Fact]
    public async Task Arena_RejectsChallengeAfterDailyLimit()
    {
        var now = new DateTimeOffset(2026, 9, 4, 3, 0, 0, TimeSpan.Zero);
        var store = new InMemoryProgressStore();
        await store.SeedAsync("attacker", value =>
        {
            value.Arena.DailyDateKey = ArenaRules.DailyDateKey(now.UtcDateTime);
            value.Arena.DailyChallenges = ArenaRules.DailyChallengeLimit;
        });
        await store.SeedAsync("defender", _ => { });
        var session = SessionFactory.Create(store, roll: 0, timeProvider: new MutableTimeProvider(now));
        await session.InitializeAsync("attacker");

        var result = await session.ChallengeArenaAsync("defender");

        Assert.False(result.Accepted);
        Assert.Contains("모두 사용", result.Message);
        Assert.Equal(ArenaRules.DailyChallengeLimit, (await store.ReadAsync("attacker")).Arena.DailyChallenges);
        Assert.Equal(0, (await store.ReadAsync("defender")).Arena.DefenseLosses);
    }

    [Fact]
    public void ArenaRules_KeepEqualPowerFairAndBoundExtremeMatchups()
    {
        Assert.Equal(50, ArenaRules.WinChance(10_000, 10_000));
        Assert.Equal(85, ArenaRules.WinChance(long.MaxValue, 1));
        Assert.Equal(15, ArenaRules.WinChance(1, long.MaxValue));
    }

    [Fact]
    public async Task EveryPossibleRoll_MatchesDisplayedChanceIncludingFavorableLosses()
    {
        var victories = 0;
        var expected = 0;
        for (var roll = 0; roll < 100; roll++)
        {
            var store = new InMemoryProgressStore();
            var session = SessionFactory.Create(store, roll: roll);
            await session.InitializeAsync("chance-test");
            var lobby = await session.LoadArenaAsync();
            var opponent = lobby.Opponents.First();
            expected = opponent.EstimatedWinChance;
            var battle = await session.ChallengeArenaAsync(opponent.UserId);
            Assert.True(battle.Accepted);
            Assert.Equal(expected, battle.WinChance);
            Assert.Equal(roll < expected, battle.Victory);
            Assert.Contains($"판정 승률 {expected}%", battle.Message);
            if (battle.Victory) victories++;
        }
        Assert.Equal(expected, victories);
    }
}
