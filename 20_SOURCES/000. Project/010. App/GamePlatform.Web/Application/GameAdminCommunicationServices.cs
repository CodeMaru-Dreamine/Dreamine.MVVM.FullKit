using GamePlatform.Domain;

namespace GamePlatform.Application;

public sealed record GameAdminMessageRequest(
    GameAdminMessageKind Kind,
    GameAdminMessageTarget Target,
    string? UserId,
    string Title,
    string Text,
    GameAdminMailAttachmentKind AttachmentKind = GameAdminMailAttachmentKind.None,
    long AttachmentAmount = 0);

public sealed record GameAdminMessageResult(
    bool Accepted,
    string Message,
    GameAdminMessage? Record);

public sealed record GameAdminMailClaimResult(
    bool Accepted,
    bool AlreadyClaimed,
    string Message,
    GameSnapshot State);

public interface IGameAdminMessageStore
{
    Task AppendAsync(GameAdminMessage message, CancellationToken cancellationToken = default);
    Task<GameAdminMessage?> FindForUserAsync(string messageId, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameAdminMessage>> LoadForUserAsync(
        string userId,
        GameAdminMessageKind kind,
        int count = 50,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameAdminMessage>> LoadRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default);
}

public interface IGameAdminCommunicationService
{
    event Action<GameAdminMessage>? MessagePublished;

    Task<GameAdminMessageResult> PublishAsync(
        GameAdminMessageRequest request,
        string administrator,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameAdminMessage>> LoadForUserAsync(
        string userId,
        GameAdminMessageKind kind,
        int count = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameAdminMessage>> LoadRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default);
}

public sealed class GameAdminCommunicationService(
    IGameAdminMessageStore store,
    IGameProgressStore progressStore,
    TimeProvider timeProvider) : IGameAdminCommunicationService
{
    public event Action<GameAdminMessage>? MessagePublished;

    public async Task<GameAdminMessageResult> PublishAsync(
        GameAdminMessageRequest request,
        string administrator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(administrator)) return Rejected("관리자 계정을 확인할 수 없습니다.");
        if (!Enum.IsDefined(request.Kind) || !Enum.IsDefined(request.Target)) return Rejected("지원하지 않는 메시지 유형입니다.");
        if (!Enum.IsDefined(request.AttachmentKind)) return Rejected("지원하지 않는 우편 첨부 품목입니다.");

        var target = request.Kind switch
        {
            GameAdminMessageKind.Announcement => GameAdminMessageTarget.AllPlayers,
            GameAdminMessageKind.Whisper => GameAdminMessageTarget.Individual,
            _ => request.Target
        };
        var targetUserId = target == GameAdminMessageTarget.Individual ? request.UserId?.Trim() : null;
        if (target == GameAdminMessageTarget.Individual)
        {
            if (string.IsNullOrWhiteSpace(targetUserId)) return Rejected("대상 게임 계정을 선택해 주세요.");
            var players = await progressStore.ListAsync(cancellationToken).ConfigureAwait(false);
            var player = players.FirstOrDefault(item => string.Equals(item.UserId, targetUserId, StringComparison.OrdinalIgnoreCase));
            if (player is null) return Rejected("메시지를 받을 게임 계정을 찾지 못했습니다.");
            targetUserId = player.UserId;
        }

        var text = Normalize(request.Text, 500);
        if (text.Length == 0) return Rejected("메시지 내용을 입력해 주세요.");
        var title = request.Kind == GameAdminMessageKind.Whisper
            ? "운영자 귓속말"
            : Normalize(request.Title, 80);
        if (title.Length == 0) return Rejected("공지 또는 우편 제목을 입력해 주세요.");

        var attachmentKind = request.Kind == GameAdminMessageKind.Mail
            ? request.AttachmentKind
            : GameAdminMailAttachmentKind.None;
        var attachmentAmount = attachmentKind == GameAdminMailAttachmentKind.None ? 0 : request.AttachmentAmount;
        if (attachmentAmount is < 0 or > 1_000_000_000_000)
            return Rejected("우편 첨부 수량은 1조 이하로 입력해 주세요.");
        if (attachmentKind != GameAdminMailAttachmentKind.None && attachmentAmount < 1)
            return Rejected("우편 첨부 수량은 1 이상이어야 합니다.");

        var record = new GameAdminMessage(
            Guid.NewGuid().ToString("N"),
            administrator.Trim(),
            request.Kind,
            target,
            targetUserId,
            title,
            text,
            attachmentKind,
            attachmentAmount,
            timeProvider.GetUtcNow().UtcDateTime);
        await store.AppendAsync(record, cancellationToken).ConfigureAwait(false);
        MessagePublished?.Invoke(record);

        var kindText = request.Kind switch
        {
            GameAdminMessageKind.Announcement => "전체 공지",
            GameAdminMessageKind.Mail when target == GameAdminMessageTarget.AllPlayers => "전체 우편",
            GameAdminMessageKind.Mail => "개별 우편",
            _ => "귓속말"
        };
        return new(true, $"{kindText} 발송을 완료했습니다.", record);
    }

    public Task<IReadOnlyList<GameAdminMessage>> LoadForUserAsync(
        string userId,
        GameAdminMessageKind kind,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return store.LoadForUserAsync(userId, kind, Math.Clamp(count, 1, 200), cancellationToken);
    }

    public Task<IReadOnlyList<GameAdminMessage>> LoadRecentAsync(
        int count = 100,
        CancellationToken cancellationToken = default) =>
        store.LoadRecentAsync(Math.Clamp(count, 1, 200), cancellationToken);

    private static string Normalize(string? value, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }

    private static GameAdminMessageResult Rejected(string message) => new(false, message, null);
}
