using System.Collections.Concurrent;
using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>월드 채팅 메시지의 영구 저장 경계입니다.</summary>
public interface IGameChatStore
{
    /// <summary>최근 메시지를 시간 오름차순으로 조회합니다.</summary>
    Task<IReadOnlyList<GameChatMessage>> LoadRecentAsync(
        string channelKey,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>검증된 메시지를 영구 저장합니다.</summary>
    Task AppendAsync(GameChatMessage message, CancellationToken cancellationToken = default);
}

/// <summary>접속 사용자 사이의 채팅 조회·등록·실시간 전파를 제공합니다.</summary>
public interface IGameChatService
{
    /// <summary>새 메시지가 서버에 저장된 뒤 발생합니다.</summary>
    event Action<GameChatMessage>? MessagePublished;

    /// <summary>최근 월드 채팅 메시지를 조회합니다.</summary>
    Task<IReadOnlyList<GameChatMessage>> LoadRecentAsync(
        string channelKey,
        int count = 50,
        CancellationToken cancellationToken = default);

    /// <summary>사용자의 월드 채팅 메시지를 검증하고 등록합니다.</summary>
    Task<GameChatPostResult> PostAsync(
        string userId,
        string channelKey,
        string displayName,
        string text,
        CancellationToken cancellationToken = default);
}

/// <summary>서버 저장과 실시간 구독을 결합한 월드 채팅 서비스입니다.</summary>
public sealed class GameChatService(
    IGameChatStore store,
    IGameSettingsStore settingsStore,
    TimeProvider timeProvider) : IGameChatService
{
    private static readonly TimeSpan MinimumPostInterval = TimeSpan.FromSeconds(2);
    private static readonly HashSet<string> ChannelKeys =
    [
        "cheongun-1", "cheongun-2", "baekya-1", "baekya-2"
    ];
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastPosts = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public event Action<GameChatMessage>? MessagePublished;

    /// <inheritdoc />
    public Task<IReadOnlyList<GameChatMessage>> LoadRecentAsync(
        string channelKey,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelKey);
        if (!ChannelKeys.Contains(channelKey))
            throw new ArgumentOutOfRangeException(nameof(channelKey), "존재하지 않는 게임 서버입니다.");
        return store.LoadRecentAsync(channelKey, Math.Clamp(count, 1, 100), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GameChatPostResult> PostAsync(
        string userId,
        string channelKey,
        string displayName,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new(false, null, "로그인 사용자를 확인할 수 없습니다.");
        if (string.IsNullOrWhiteSpace(channelKey) || !ChannelKeys.Contains(channelKey))
            return new(false, null, "접속할 게임 서버를 먼저 선택해 주세요.");

        var settings = await settingsStore.FindAsync(userId, cancellationToken).ConfigureAwait(false);
        var general = settings is null ? null : GameSettingsPolicy.Normalize(settings).General;
        if (general?.ChannelConfirmed != true)
            return new(false, null, "접속할 게임 서버를 먼저 선택해 주세요.");
        if (!string.Equals(general.ChannelKey, channelKey, StringComparison.OrdinalIgnoreCase))
            return new(false, null, "현재 접속한 서버의 월드 채팅만 이용할 수 있습니다.");

        var normalizedText = text?.Trim() ?? string.Empty;
        if (normalizedText.Length is < 1 or > 120)
            return new(false, null, "메시지는 1~120자로 입력해 주세요.");

        var now = timeProvider.GetUtcNow();
        if (_lastPosts.TryGetValue(userId, out var previous) && now - previous < MinimumPostInterval)
            return new(false, null, "메시지는 2초에 한 번 보낼 수 있습니다.");
        _lastPosts[userId] = now;

        var message = new GameChatMessage(
            Guid.NewGuid().ToString("N"),
            channelKey,
            userId,
            NormalizeDisplayName(displayName),
            normalizedText,
            now.UtcDateTime);
        try
        {
            await store.AppendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _lastPosts.TryRemove(userId, out _);
            throw;
        }

        MessagePublished?.Invoke(message);
        return new(true, message, null);
    }

    private static string NormalizeDisplayName(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "이름 없는 검객" : value.Trim();
        return normalized.Length <= 24 ? normalized : normalized[..24];
    }
}
