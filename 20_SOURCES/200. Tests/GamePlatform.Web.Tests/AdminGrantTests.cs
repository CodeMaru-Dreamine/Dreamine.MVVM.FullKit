using GamePlatform.Application;
using Microsoft.Extensions.Logging.Abstractions;

namespace GamePlatform.Web.Tests;

public sealed class AdminGrantTests
{
    [Fact]
    public async Task IndividualGrant_UpdatesOnlySelectedAccountAndWritesAudit()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", value => value.PremiumCurrency = 10);
        await progress.SeedAsync("player-b", value => value.PremiumCurrency = 20);
        var audit = new InMemoryAuditStore();
        var service = CreateService(progress, audit);

        var result = await service.GrantAsync(
            new(GameAdminGrantTarget.Individual, "player-a", GameAdminRewardKind.PremiumCurrency, 100, "개별 보상"),
            "admin@codemaru.test");

        Assert.True(result.Accepted);
        Assert.Equal(110, (await progress.ReadAsync("player-a")).PremiumCurrency);
        Assert.Equal(20, (await progress.ReadAsync("player-b")).PremiumCurrency);
        Assert.Single(await audit.LoadRecentAsync());
    }

    [Fact]
    public async Task AllPlayerGrant_UpdatesEveryExistingAccount()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", value => value.HeroSummonTickets = 1);
        await progress.SeedAsync("player-b", value => value.HeroSummonTickets = 2);
        var service = CreateService(progress, new InMemoryAuditStore());

        var result = await service.GrantAsync(
            new(GameAdminGrantTarget.AllPlayers, null, GameAdminRewardKind.HeroSummonTickets, 5, "전체 선물"),
            "admin@codemaru.test");

        Assert.True(result.Accepted);
        Assert.Equal(2, result.RecipientCount);
        Assert.Equal(6, (await progress.ReadAsync("player-a")).HeroSummonTickets);
        Assert.Equal(7, (await progress.ReadAsync("player-b")).HeroSummonTickets);
    }

    [Fact]
    public async Task Grant_RejectsUnknownAccountAndInvalidAmount()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", _ => { });
        var service = CreateService(progress, new InMemoryAuditStore());

        var missing = await service.GrantAsync(
            new(GameAdminGrantTarget.Individual, "missing", GameAdminRewardKind.Gold, 10, ""),
            "admin@codemaru.test");
        var invalid = await service.GrantAsync(
            new(GameAdminGrantTarget.AllPlayers, null, GameAdminRewardKind.Gold, 0, ""),
            "admin@codemaru.test");

        Assert.False(missing.Accepted);
        Assert.False(invalid.Accepted);
    }

    [Fact]
    public async Task ResetBalance_CanClearOneCurrencyOrAllCurrenciesAndWritesAudit()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", value => { value.Gold = 123; value.PremiumCurrency = 456; value.HeroSummonTickets = 7; });
        var audit = new InMemoryAuditStore();
        var service = CreateService(progress, audit);

        var gold = await service.ResetBalanceAsync("player-a", GameAdminRewardKind.Gold, "테스트 정리", "admin@codemaru.test");
        Assert.True(gold.Accepted);
        var afterGold = await progress.ReadAsync("player-a");
        Assert.Equal(0, afterGold.Gold);
        Assert.Equal(456, afterGold.PremiumCurrency);

        var all = await service.ResetBalanceAsync("player-a", GameAdminRewardKind.AllCurrencies, "전체 초기화", "admin@codemaru.test");
        Assert.True(all.Accepted);
        var afterAll = await progress.ReadAsync("player-a");
        Assert.Equal(0, afterAll.PremiumCurrency);
        Assert.Equal(0, afterAll.HeroSummonTickets);
        Assert.Equal(2, (await audit.LoadRecentAsync()).Count);
    }

    [Fact]
    public async Task ReclaimBalance_SubtractsRequestedAmountWithoutGoingBelowZero()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", value => value.PremiumCurrency = 1_000);
        var audit = new InMemoryAuditStore();
        var service = CreateService(progress, audit);

        var first = await service.ReclaimBalanceAsync("player-a", GameAdminRewardKind.PremiumCurrency, 700, "테스트 지급 회수", "admin@codemaru.test");
        var second = await service.ReclaimBalanceAsync("player-a", GameAdminRewardKind.PremiumCurrency, 900, "잔여 회수", "admin@codemaru.test");

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Equal(0, (await progress.ReadAsync("player-a")).PremiumCurrency);
        Assert.Equal(-300, second.Record?.Amount);
    }

    private static GameAdminGrantService CreateService(InMemoryProgressStore progress, InMemoryAuditStore audit) =>
        new(progress, audit, TimeProvider.System, NullLogger<GameAdminGrantService>.Instance);

    private sealed class InMemoryAuditStore : IGameAdminGrantAuditStore
    {
        private readonly List<GameAdminGrantRecord> _records = [];

        public Task AppendAsync(GameAdminGrantRecord record, CancellationToken cancellationToken = default)
        {
            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<GameAdminGrantRecord>> LoadRecentAsync(int count = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameAdminGrantRecord>>(_records.TakeLast(count).Reverse().ToArray());
    }
}
