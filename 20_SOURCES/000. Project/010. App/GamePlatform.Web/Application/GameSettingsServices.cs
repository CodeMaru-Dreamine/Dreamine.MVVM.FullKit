using System.Text.Json;
using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>사용자별 게임 설정의 영속 저장 경계입니다.</summary>
public interface IGameSettingsStore
{
    /// <summary>저장된 설정을 조회하며 없으면 <see langword="null"/>을 반환합니다.</summary>
    Task<GameSettings?> FindAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>검증된 설정을 사용자 계정에 저장합니다.</summary>
    Task SaveAsync(string userId, GameSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>설정 조회, 검증, 충돌 해소 및 저장 Use Case입니다.</summary>
public interface IGameSettingsService
{
    /// <summary>서버 설정을 우선하고, 서버값이 없을 때만 로그인 전 임시 설정을 가져옵니다.</summary>
    Task<GameSettings> LoadAsync(string userId, GameSettings? guestSettings = null, CancellationToken cancellationToken = default);

    /// <summary>설정을 검증한 후 계정 단위로 저장하고 저장된 값을 반환합니다.</summary>
    Task<GameSettings> SaveAsync(string userId, GameSettings settings, CancellationToken cancellationToken = default);

    /// <summary>두 설정 스냅샷이 다른지 확인합니다.</summary>
    bool HasChanges(GameSettings saved, GameSettings draft);
}

/// <summary>게임 설정 Use Case의 기본 구현입니다.</summary>
public sealed class GameSettingsService(IGameSettingsStore store) : IGameSettingsService
{
    private static readonly JsonSerializerOptions ComparisonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <inheritdoc />
    public async Task<GameSettings> LoadAsync(string userId, GameSettings? guestSettings = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var stored = await store.FindAsync(userId, cancellationToken).ConfigureAwait(false);
        if (stored is not null) return GameSettingsPolicy.Normalize(stored);

        var initial = GameSettingsPolicy.Normalize(guestSettings);
        await store.SaveAsync(userId, initial, cancellationToken).ConfigureAwait(false);
        return initial;
    }

    /// <inheritdoc />
    public async Task<GameSettings> SaveAsync(string userId, GameSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(settings);
        var normalized = GameSettingsPolicy.Normalize(settings);
        await store.SaveAsync(userId, normalized, cancellationToken).ConfigureAwait(false);
        return normalized;
    }

    /// <inheritdoc />
    public bool HasChanges(GameSettings saved, GameSettings draft) =>
        JsonSerializer.Serialize(GameSettingsPolicy.Normalize(saved), ComparisonOptions) !=
        JsonSerializer.Serialize(GameSettingsPolicy.Normalize(draft), ComparisonOptions);
}
