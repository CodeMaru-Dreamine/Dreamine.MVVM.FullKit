using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class AdminCommunicationTests
{
    [Fact]
    public async Task Announcement_IsPublishedToEveryPlayerAndPersisted()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", _ => { });
        await progress.SeedAsync("player-b", _ => { });
        var store = new InMemoryMessageStore();
        var service = new GameAdminCommunicationService(store, progress, TimeProvider.System);
        var published = new List<GameAdminMessage>();
        service.MessagePublished += published.Add;

        var result = await service.PublishAsync(
            new(GameAdminMessageKind.Announcement, GameAdminMessageTarget.Individual, "player-a", "점검 안내", "10분 뒤 점검합니다."),
            "admin@codemaru.test");

        Assert.True(result.Accepted);
        Assert.Equal(GameAdminMessageTarget.AllPlayers, result.Record?.Target);
        Assert.Single(published);
        Assert.Single(await service.LoadForUserAsync("player-a", GameAdminMessageKind.Announcement));
        Assert.Single(await service.LoadForUserAsync("player-b", GameAdminMessageKind.Announcement));
    }

    [Fact]
    public async Task Mail_CanTargetAllPlayersOrOnePlayer()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", _ => { });
        await progress.SeedAsync("player-b", _ => { });
        var service = new GameAdminCommunicationService(new InMemoryMessageStore(), progress, TimeProvider.System);

        await service.PublishAsync(
            new(GameAdminMessageKind.Mail, GameAdminMessageTarget.AllPlayers, null, "전체 선물", "우편함을 확인하세요."),
            "admin@codemaru.test");
        await service.PublishAsync(
            new(GameAdminMessageKind.Mail, GameAdminMessageTarget.Individual, "player-a", "개별 안내", "문의 답변입니다."),
            "admin@codemaru.test");

        Assert.Equal(2, (await service.LoadForUserAsync("player-a", GameAdminMessageKind.Mail)).Count);
        Assert.Single(await service.LoadForUserAsync("player-b", GameAdminMessageKind.Mail));
    }

    [Fact]
    public async Task Mail_CanCarryRewardAttachment()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", _ => { });
        var service = new GameAdminCommunicationService(new InMemoryMessageStore(), progress, TimeProvider.System);

        var result = await service.PublishAsync(
            new(GameAdminMessageKind.Mail, GameAdminMessageTarget.AllPlayers, null, "출시 선물", "감사합니다.", GameAdminMailAttachmentKind.HeroSummonTickets, 20),
            "admin@codemaru.test");

        Assert.True(result.Accepted);
        Assert.Equal(GameAdminMailAttachmentKind.HeroSummonTickets, result.Record?.AttachmentKind);
        Assert.Equal(20, result.Record?.AttachmentAmount);
    }

    [Fact]
    public async Task Whisper_RequiresExistingIndividualPlayer()
    {
        var progress = new InMemoryProgressStore();
        await progress.SeedAsync("player-a", _ => { });
        var service = new GameAdminCommunicationService(new InMemoryMessageStore(), progress, TimeProvider.System);

        var accepted = await service.PublishAsync(
            new(GameAdminMessageKind.Whisper, GameAdminMessageTarget.AllPlayers, "player-a", "무시되는 제목", "도움이 필요하시면 답변해 주세요."),
            "admin@codemaru.test");
        var rejected = await service.PublishAsync(
            new(GameAdminMessageKind.Whisper, GameAdminMessageTarget.Individual, "missing", "", "대상이 없습니다."),
            "admin@codemaru.test");

        Assert.True(accepted.Accepted);
        Assert.Equal(GameAdminMessageTarget.Individual, accepted.Record?.Target);
        Assert.Equal("운영자 귓속말", accepted.Record?.Title);
        Assert.False(rejected.Accepted);
        Assert.Single(await service.LoadForUserAsync("player-a", GameAdminMessageKind.Whisper));
    }

    [Fact]
    public async Task MailAttachment_CanBeClaimedOnlyOnce()
    {
        var progress = new InMemoryProgressStore();
        var messages = new InMemoryMessageStore();
        var message = new GameAdminMessage(
            Guid.NewGuid().ToString("N"), "admin@codemaru.test", GameAdminMessageKind.Mail,
            GameAdminMessageTarget.AllPlayers, null, "선물", "테스트 선물",
            GameAdminMailAttachmentKind.PremiumCurrency, 500, DateTime.UtcNow);
        await messages.AppendAsync(message);
        var session = new GameSession(progress, new FixedRollSource(0), SessionFactory.Regions, adminMessageStore: messages);
        await session.InitializeAsync("player-a");

        var first = await session.ClaimAdminMailRewardAsync(message.MessageId);
        var second = await session.ClaimAdminMailRewardAsync(message.MessageId);

        Assert.True(first.Accepted);
        Assert.True(second.AlreadyClaimed);
        Assert.Equal(500, (await progress.ReadAsync("player-a")).PremiumCurrency);
    }

    private sealed class InMemoryMessageStore : IGameAdminMessageStore
    {
        private readonly List<GameAdminMessage> messages = [];

        public Task AppendAsync(GameAdminMessage message, CancellationToken cancellationToken = default)
        {
            messages.Add(message);
            return Task.CompletedTask;
        }

        public Task<GameAdminMessage?> FindForUserAsync(string messageId, string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(messages.FirstOrDefault(item => item.MessageId == messageId
                && (item.Target == GameAdminMessageTarget.AllPlayers
                    || string.Equals(item.TargetUserId, userId, StringComparison.OrdinalIgnoreCase))));

        public Task<IReadOnlyList<GameAdminMessage>> LoadForUserAsync(
            string userId,
            GameAdminMessageKind kind,
            int count = 50,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameAdminMessage>>(messages
                .Where(item => item.Kind == kind
                    && (item.Target == GameAdminMessageTarget.AllPlayers
                        || string.Equals(item.TargetUserId, userId, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(item => item.CreatedUtc)
                .Take(count)
                .ToArray());

        public Task<IReadOnlyList<GameAdminMessage>> LoadRecentAsync(
            int count = 100,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GameAdminMessage>>(messages
                .OrderByDescending(item => item.CreatedUtc)
                .Take(count)
                .ToArray());
    }
}
