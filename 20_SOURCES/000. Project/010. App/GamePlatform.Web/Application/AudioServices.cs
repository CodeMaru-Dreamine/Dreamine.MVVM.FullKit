using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>현재 스테이지와 보스 여부에 맞는 음악을 결정합니다.</summary>
public interface IRegionAudioResolver
{
    /// <summary>현재 스테이지의 지역 음악과 지역명을 반환합니다.</summary>
    RegionAudioSelection Resolve(int stage);

    /// <summary>현재 곡 기준 이전 또는 다음 곡을 반환합니다.</summary>
    AudioTrackDefinition ResolveAdjacent(string assetKey, int direction);
}

/// <summary>지역 음악 조회 결과입니다.</summary>
public sealed record RegionAudioSelection(string RegionName, bool IsBoss, AudioTrackDefinition Track, string AmbientSoundAssetKey);

/// <summary>전투 입력 하나에 맞춰 순서대로 출력할 효과음 큐입니다.</summary>
public sealed record CombatAudioCue(string? MovementEffectKey, string ImpactEffectKey, int ImpactDelayMilliseconds);

/// <summary>논리 효과음 키를 검증된 런타임 자산으로 해석합니다.</summary>
public interface IAudioAssetCatalog
{
    /// <summary>논리 효과음 키의 파일과 재생 정책을 반환합니다.</summary>
    AudioEffectDefinition ResolveEffect(string effectKey);
    /// <summary>브라우저에서 미리 디코딩할 모든 전투 효과음을 반환합니다.</summary>
    IReadOnlyCollection<AudioEffectDefinition> GetAllEffects();
}

/// <summary>전투 결과에 맞는 검 이동음과 충격음을 결정합니다.</summary>
public interface ICombatAudioDirector
{
    /// <summary>플레이어 공격의 치명타·처치 상태에 맞는 큐를 반환합니다.</summary>
    CombatAudioCue ResolvePlayerAttack(bool critical, bool enemyDefeated);
    /// <summary>적 공격과 아군 전투 불능 상태에 맞는 큐를 반환합니다.</summary>
    CombatAudioCue ResolveEnemyAttack(bool targetDefeated);
}

/// <summary>브라우저 오디오 출력 경계입니다.</summary>
public interface IAudioService : IAsyncDisposable
{
    /// <summary>오디오 그래프를 만들고 브라우저 지원 상태를 반환합니다.</summary>
    ValueTask<bool> InitializeAsync(SoundGameSettings settings, CancellationToken cancellationToken = default);
    /// <summary>사용자 입력으로 오디오 컨텍스트를 활성화하고 성공 여부를 반환합니다.</summary>
    ValueTask<bool> UnlockAsync(CancellationToken cancellationToken = default);
    /// <summary>음량 설정을 미리듣기 그래프에 즉시 반영합니다.</summary>
    ValueTask ApplySettingsAsync(SoundGameSettings settings, CancellationToken cancellationToken = default);
    /// <summary>현재 곡에서 새 곡으로 크로스페이드합니다.</summary>
    ValueTask<bool> PlayMusicAsync(AudioTrackDefinition track, SoundGameSettings settings, CancellationToken cancellationToken = default);
    /// <summary>배경 음악을 일시정지하거나 재개합니다.</summary>
    ValueTask SetPausedAsync(bool paused, CancellationToken cancellationToken = default);
    /// <summary>효과음 채널로 짧은 전투 또는 UI 피드백을 재생합니다.</summary>
    ValueTask PlayEffectAsync(string effectKey, bool uiEffect, CancellationToken cancellationToken = default);
}

/// <summary>지역·보스 음악 키를 카탈로그 메타데이터로 해석합니다.</summary>
public sealed class RegionAudioResolver(RegionCatalog regions, AudioTrackCatalog tracks) : IRegionAudioResolver
{
    /// <inheritdoc />
    public RegionAudioSelection Resolve(int stage)
    {
        var world = regions.Resolve(stage);
        var key = world.IsBoss ? world.Region.BossBgmAssetKey : world.Region.BgmAssetKey;
        return new RegionAudioSelection(world.Region.Name, world.IsBoss, tracks.Get(key), world.Region.AmbientSoundAssetKey);
    }

    /// <inheritdoc />
    public AudioTrackDefinition ResolveAdjacent(string assetKey, int direction) => tracks.Adjacent(assetKey, direction);
}

/// <summary>효과음 도메인 카탈로그를 애플리케이션 경계로 노출합니다.</summary>
public sealed class AudioAssetCatalog(AudioEffectCatalog effects) : IAudioAssetCatalog
{
    /// <inheritdoc />
    public AudioEffectDefinition ResolveEffect(string effectKey) => effects.Get(effectKey);
    /// <inheritdoc />
    public IReadOnlyCollection<AudioEffectDefinition> GetAllEffects() => effects.All;
}

/// <summary>수동·자동 공격이 공유하는 전투 효과음 선택 정책입니다.</summary>
public sealed class CombatAudioDirector : ICombatAudioDirector
{
    /// <inheritdoc />
    public CombatAudioCue ResolvePlayerAttack(bool critical, bool enemyDefeated) => new(
        "sword-whoosh",
        enemyDefeated || critical ? "sword-impact-critical" : "sword-impact-normal",
        enemyDefeated || critical ? 58 : 46);

    /// <inheritdoc />
    public CombatAudioCue ResolveEnemyAttack(bool targetDefeated) => new(
        null,
        targetDefeated ? "enemy-defeat" : "enemy-impact",
        0);
}
