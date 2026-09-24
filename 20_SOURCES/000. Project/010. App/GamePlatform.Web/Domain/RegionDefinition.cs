namespace GamePlatform.Domain;

/// <summary>한 월드 지역의 정적 콘텐츠 설정입니다.</summary>
public sealed record RegionDefinition
{
    /// <summary>영구적인 지역 키입니다.</summary>
    public required string RegionId { get; init; }
    /// <summary>화면에 표시할 독자 지역명입니다.</summary>
    public required string Name { get; init; }
    /// <summary>지역 시작 스테이지입니다.</summary>
    public required int StartStage { get; init; }
    /// <summary>지역 종료 스테이지입니다.</summary>
    public required int EndStage { get; init; }
    /// <summary>배경 이미지 자산 키입니다.</summary>
    public required string BackgroundAssetKey { get; init; }
    /// <summary>보스 스테이지에서 사용할 전용 배경 이미지 자산 키입니다.</summary>
    public string? BossBackgroundAssetKey { get; init; }
    /// <summary>일반 스테이지 배경 음악 자산 키입니다.</summary>
    public required string BgmAssetKey { get; init; }
    /// <summary>보스 스테이지 배경 음악 자산 키입니다.</summary>
    public required string BossBgmAssetKey { get; init; }
    /// <summary>지역 환경음 자산 키입니다.</summary>
    public required string AmbientSoundAssetKey { get; init; }
    /// <summary>환경 효과 키입니다.</summary>
    public required string AmbientEffectKey { get; init; }
    /// <summary>적 풀 키입니다.</summary>
    public required string EnemyPoolId { get; init; }
    /// <summary>지역 보스 키입니다.</summary>
    public required string BossId { get; init; }
    /// <summary>지역 보스의 전용 이미지 자산 키입니다.</summary>
    public string? BossAssetKey { get; init; }
    /// <summary>지역 기본 색상입니다.</summary>
    public required string PrimaryColor { get; init; }
    /// <summary>지역 강조 색상입니다.</summary>
    public required string AccentColor { get; init; }
}

/// <summary>현재 스테이지에서 사용할 지역과 분위기 변형을 나타냅니다.</summary>
public sealed record RegionStageView(
    RegionDefinition Region,
    string LightingKey,
    bool IsBoss,
    string EnemyName,
    string EnemyAssetUrl,
    string EnemyHitAssetUrl,
    string BackgroundUrl,
    string? PreloadBackgroundUrl,
    int BossGuardCount = 0,
    string? BossGuardFormationAssetUrl = null);

/// <summary>월드 도감에 표시할 지역별 발견 진행 정보입니다.</summary>
public sealed record CodexEntry(
    string RegionId,
    string Name,
    int StartStage,
    int EndStage,
    bool Discovered,
    bool Completed,
    int ClearedStages,
    int TotalStages,
    string RepresentativeEnemyName);
