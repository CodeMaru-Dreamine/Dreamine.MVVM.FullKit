using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>Presentation에 전달하는 읽기 전용 게임 상태입니다.</summary>
public sealed record GameSnapshot(
    int Stage,
    long Gold,
    long HeroSummonTickets,
    int HeroSummonPity,
    long CompanionUpgradeMaterials,
    IReadOnlyDictionary<string, int> CompanionShards,
    int AttackLevel,
    long AttackPower,
    long UpgradeCost,
    int SwordArtLevel,
    long SwordArtAttackBonus,
    long SwordArtUpgradeCost,
    int EquipmentLevel,
    long EquipmentAttackBonus,
    long EquipmentUpgradeCost,
    long PlayerHp,
    long PlayerMaxHp,
    long TotalDefeated,
    long EnemyHp,
    long EnemyMaxHp,
    int HighestClearedStage,
    bool AutoAttackUnlocked,
    bool AutoAttackEnabled,
    int ActiveAutoSpeedMultiplier,
    DateTime? AutoSpeedBoostEndsUtc,
    IReadOnlyList<AutoSpeedBoostInventoryEntry> AutoBoostInventory,
    bool ShowAutoUnlockNotice,
    TimeSpan AutoAttackInterval,
    CombatPhase Phase,
    RegionStageView World,
    IReadOnlyList<CodexEntry> Codex,
    PartyView Party,
    DateTime UpdatedUtc)
{
    /// <summary>계정에 저장된 게임 닉네임입니다. 최초 설정 전에는 비어 있습니다.</summary>
    public string GameNickname { get; init; } = string.Empty;

    /// <summary>완료된 닉네임 변경 횟수입니다.</summary>
    public int NicknameChangeCount { get; init; }

    /// <summary>전설 동료를 획득하지 못한 연속 소환 횟수입니다.</summary>
    public int LegendarySummonPity { get; init; }

    /// <summary>계정에 영구 저장된 시련의 탑 최고 정복 층입니다.</summary>
    public int HighestTrialTowerFloor { get; init; }

    /// <summary>현재 보유한 시련의 탑 도전권입니다.</summary>
    public int TrialTowerChallengeTickets { get; init; } = TrialTowerRules.MaximumNaturalChallengeTickets;

    /// <summary>다음 시련의 탑 도전권 자연 회복 완료 시각입니다.</summary>
    public DateTime? NextTrialTowerTicketUtc { get; init; }

    /// <summary>본체와 편성 동료의 성장·장비 효과를 모두 적용한 시련의 탑 원정대 전투력입니다.</summary>
    public long TrialTowerPartyPower { get; init; }

    /// <summary>현재 스테이지에 출전한 동료별 전투 체력입니다.</summary>
    public IReadOnlyList<BattleUnitSnapshot> CompanionUnits { get; init; } = Array.Empty<BattleUnitSnapshot>();

    /// <summary>현재 보스 스테이지의 호위별 전투 체력입니다.</summary>
    public IReadOnlyList<BattleUnitSnapshot> BossGuardUnits { get; init; } = Array.Empty<BattleUnitSnapshot>();

    /// <summary>보유한 동료별 성장·개인 장비·스킨 상태입니다.</summary>
    public IReadOnlyList<CompanionGrowthView> CompanionGrowth { get; init; } = Array.Empty<CompanionGrowthView>();

    /// <summary>계정 장비 가방에 보관 중인 개별 강화 장비 목록입니다.</summary>
    public IReadOnlyList<CompanionEquipmentView> CompanionEquipmentInventory { get; init; } = Array.Empty<CompanionEquipmentView>();

    /// <summary>유료 외형 상품에 사용하는 CodeMaru C 잔액입니다.</summary>
    public long PremiumCurrency { get; init; }

    /// <summary>독립 성장하는 계승 무기 레벨입니다.</summary>
    public int WeaponEquipmentLevel { get; init; }

    /// <summary>독립 성장하는 계승 갑주 레벨입니다.</summary>
    public int ArmorEquipmentLevel { get; init; }

    /// <summary>독립 성장하는 인연 옥패 레벨입니다.</summary>
    public int JadeEquipmentLevel { get; init; }

    /// <summary>무기 다음 단계 단련 비용입니다.</summary>
    public long WeaponUpgradeCost { get; init; }

    /// <summary>갑주 다음 단계 단련 비용입니다.</summary>
    public long ArmorUpgradeCost { get; init; }

    /// <summary>옥패 다음 단계 단련 비용입니다.</summary>
    public long JadeUpgradeCost { get; init; }

    /// <summary>옥패가 제공하는 전체 동료 지원 효과입니다.</summary>
    public int JadeAssistBonusPercent { get; init; }

    /// <summary>현재 계정의 방치 보상 해금·적립 상태입니다.</summary>
    public OfflineRewardView OfflineReward { get; init; } = OfflineRewardView.Locked;

    /// <summary>오늘의 원정 임무 진행 및 수령 상태입니다.</summary>
    public IReadOnlyList<DailyMissionView> DailyMissions { get; init; } = Array.Empty<DailyMissionView>();

    /// <summary>오프라인 동안에도 재배 시간이 흐르는 영약 농원 상태입니다.</summary>
    public SpiritFarmView SpiritFarm { get; init; } = SpiritFarmView.Empty;

    /// <summary>초기 렌더링에 사용할 빈 상태입니다.</summary>
    public static GameSnapshot Empty { get; } = new(
        1, 0, 0, 0, 0, new Dictionary<string, int>(),
        1, GameRules.AttackPower(1), GameRules.UpgradeCost(1),
        0, 0, GameRules.SwordArtUpgradeCost(0),
        0, 0, GameRules.EquipmentUpgradeCost(0),
        GameRules.PlayerMaxHp(1, 0), GameRules.PlayerMaxHp(1, 0), 0,
        GameRules.EnemyMaxHp(1), GameRules.EnemyMaxHp(1), 0, false, false,
        1, null, Array.Empty<AutoSpeedBoostInventoryEntry>(), false,
        GameRules.DefaultAutoAttackInterval, CombatPhase.Loading,
        new RegionStageView(
            new RegionDefinition
            {
                RegionId = "loading", Name = "여정 준비", StartStage = 1, EndStage = 1,
                BackgroundAssetKey = "fallback", BgmAssetKey = "bgm-reed-breath",
                BossBgmAssetKey = "bgm-marsh-crown", AmbientSoundAssetKey = "amb-loading", AmbientEffectKey = "mist",
                EnemyPoolId = "none", BossId = "none", PrimaryColor = "#173247", AccentColor = "#8fd8c8"
            },
            "night", false, "기척을 찾는 중",
            "/images/maru-idle/guardian-idle.webp", "/images/maru-idle/guardian-hit.webp",
            "/images/maru-idle/regions/fallback.webp", null),
        Array.Empty<CodexEntry>(),
        PartyRules.Resolve(0, "yeonsu", Array.Empty<string>(), GameRules.AttackPower(1)),
        DateTime.UtcNow);
}

/// <summary>Presentation에서 표시할 서버 권위 방치 보상 상태입니다.</summary>
public sealed record OfflineRewardView(
    bool IsUnlocked,
    int UnlockStage,
    long PendingGold,
    TimeSpan PendingDuration,
    long GoldPerHour,
    TimeSpan MaximumAccrual)
{
    /// <summary>해금 전 초기 상태입니다.</summary>
    public static OfflineRewardView Locked { get; } = new(
        false, OfflineRewardPolicy.UnlockStage, 0, TimeSpan.Zero, 0, OfflineRewardPolicy.MaximumAccrual);
}

/// <summary>방치 보상 수령 처리 결과입니다.</summary>
public sealed record OfflineRewardClaimResult(
    bool Accepted,
    long ClaimedGold,
    string Message,
    GameSnapshot State);

/// <summary>게임 닉네임 변경 처리 결과입니다.</summary>
public sealed record NicknameChangeResult(
    bool Accepted,
    string Message,
    long ChargedPremiumCurrency,
    GameSnapshot State);

/// <summary>현재 스테이지 안에서만 유지되는 개별 전투 유닛 상태입니다.</summary>
public sealed record BattleUnitSnapshot(
    string UnitId,
    string Name,
    string Role,
    string AssetUrl,
    long Hp,
    long MaxHp)
{
    /// <summary>유닛이 현재 전투에 남아 있는지 여부입니다.</summary>
    public bool IsAlive => Hp > 0;

    /// <summary>전투 컷아웃에 적용할 현재 스킨 스타일입니다.</summary>
    public string SkinCssClass { get; init; } = "skin-default";
}

/// <summary>동료 성장 작업의 서버 검증 결과입니다.</summary>
public sealed record CompanionActionResult(
    bool Accepted,
    string Message,
    GameSnapshot State);

/// <summary>한 공격 요청의 서버 처리 결과입니다.</summary>
public sealed record AttackResult(
    bool Accepted,
    AttackSource Source,
    long Damage,
    bool IsCritical,
    bool EnemyDefeated,
    long Reward,
    bool AutoUnlockedNow,
    AutoSpeedBoostDefinition? AutoSpeedBoostDropped,
    string? RejectionReason,
    GameSnapshot State)
{
    /// <summary>이번 공격이 적중한 보스 본체 또는 호위 유닛 ID입니다.</summary>
    public string? TargetUnitId { get; init; }

    /// <summary>이번 처치에서 몬스터가 떨어뜨린 계정 공용 개인 장비입니다.</summary>
    public CompanionEquipmentView? EquipmentDropped { get; init; }
}

/// <summary>AUTO 가속 부적 사용 처리 결과입니다.</summary>
public sealed record AutoSpeedBoostActivationResult(
    bool Activated,
    string? RejectionReason,
    GameSnapshot State);

public sealed record AutoSpeedBoostPurchaseResult(
    bool Purchased,
    string? RejectionReason,
    AutoSpeedBoostDefinition? Item,
    GameSnapshot State);

public sealed record GoldPackPurchaseResult(
    bool Purchased,
    string? RejectionReason,
    GoldPackDefinition? Item,
    GameSnapshot State);

public sealed record DailyMissionView(
    DailyMissionDefinition Definition,
    int Progress,
    bool IsCompleted,
    bool IsClaimed);

public sealed record DailyMissionClaimResult(
    bool Accepted,
    string Message,
    GameSnapshot State);

/// <summary>영약 농원 전체 상태와 다음 확장 정보를 표시합니다.</summary>
public sealed record SpiritFarmView(
    int Level,
    int UnlockedPlots,
    int MaximumPlots,
    long UpgradeCost,
    IReadOnlyList<SpiritCropDefinition> Crops,
    IReadOnlyList<SpiritFarmPlotView> Plots)
{
    public IReadOnlyDictionary<string, int> SeedPacks { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> CropSeeds { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<SpiritSeedPackDefinition> SeedPackCatalog { get; init; } = Array.Empty<SpiritSeedPackDefinition>();
    public int FertilizerCount { get; init; }
    public int FriendsHelpedToday { get; init; }
    public int MaximumFriendHelps { get; init; } = 3;
    public int VerticalRackLevel { get; init; }
    public long VerticalRackUpgradeGoldCost { get; init; }
    public long VerticalRackUpgradeMaterialCost { get; init; }
    public SpiritFarmHouseDefinition House { get; init; } = SpiritFarmRules.CurrentHouse(0);
    public SpiritFarmHouseDefinition? NextHouse { get; init; }

    public static SpiritFarmView Empty { get; } = new(
        SpiritFarmRules.MinimumLevel,
        SpiritFarmRules.UnlockedPlots(SpiritFarmRules.MinimumLevel),
        SpiritFarmRules.MaximumPlots,
        SpiritFarmRules.UpgradeCost(SpiritFarmRules.MinimumLevel),
        SpiritFarmRules.GetCrops(),
        Enumerable.Range(0, SpiritFarmRules.MaximumPlots)
            .Select(index => new SpiritFarmPlotView(index, index < 2, null, null, null, false))
            .ToArray());
}

/// <summary>한 경작지의 잠금·재배·수확 가능 상태입니다.</summary>
public sealed record SpiritFarmPlotView(
    int PlotIndex,
    bool IsUnlocked,
    SpiritCropDefinition? Crop,
    DateTime? PlantedUtc,
    DateTime? ReadyUtc,
    bool IsReady)
{
    public int SoilVitality { get; init; } = 100;
    public int ConsecutiveCrops { get; init; }
    public string LastCropKey { get; init; } = string.Empty;
    public bool OwnerFertilized { get; init; }
    public int FriendFertilizerCount { get; init; }
}

public sealed record SpiritFarmFriendView(
    string UserId,
    string DisplayName,
    int FarmLevel,
    int GrowingPlots,
    bool CanHelp);

/// <summary>심기·수확·농원 확장의 서버 검증 결과입니다.</summary>
public sealed record SpiritFarmActionResult(
    bool Accepted,
    string Message,
    long GoldReward,
    long MaterialReward,
    int SummonTicketReward,
    GameSnapshot State);

/// <summary>무한 시련 한 판을 서버에서 추적하기 위한 시작 정보입니다.</summary>
public sealed record EndlessTrialStart(string RunId, DateTime StartedUtc);

/// <summary>서버가 검증하고 계정에 반영한 무한 시련 보상입니다.</summary>
public sealed record EndlessTrialResult(
    bool Accepted,
    int SurvivalSeconds,
    int Kills,
    long Gold,
    long UpgradeMaterials,
    int SummonTickets,
    CompanionEquipmentView? EquipmentDropped,
    AutoSpeedBoostDefinition? BoostDropped,
    string Message,
    GameSnapshot State);

/// <summary>시련 회랑의 개별 미니게임 런 시작 정보입니다.</summary>
public sealed record ArcadeTrialStart(string RunId, DateTime StartedUtc);

/// <summary>서버가 검증하고 계정에 반영한 시련 회랑 미니게임 보상입니다.</summary>
public sealed record ArcadeTrialResult(
    bool Accepted,
    int DurationSeconds,
    int Score,
    int Defeated,
    long Gold,
    long UpgradeMaterials,
    int SummonTickets,
    CompanionEquipmentView? EquipmentDropped,
    AutoSpeedBoostDefinition? BoostDropped,
    string Message,
    GameSnapshot State);

/// <summary>무료 소환패 한 장을 소비한 서버 소환 결과입니다.</summary>
public sealed record CompanionSummonResult(
    bool Accepted,
    CompanionSummonReward? Reward,
    string? RejectionReason,
    GameSnapshot State);

/// <summary>여러 장의 소환패를 한 트랜잭션에서 소비한 연속 소환 결과입니다.</summary>
public sealed record CompanionSummonBatchResult(
    bool Accepted,
    IReadOnlyList<CompanionSummonReward> Rewards,
    string? RejectionReason,
    GameSnapshot State);

/// <summary>CodeMaru C로 소환패 묶음을 구매한 결과입니다.</summary>
public sealed record SummonTicketPurchaseResult(
    bool Purchased,
    string? RejectionReason,
    SummonTicketPackDefinition? Item,
    GameSnapshot State);

/// <summary>시련의 탑 한 층 도전에 대한 서버 판정과 최초 정복 보상입니다.</summary>
public sealed record TrialTowerChallengeResult(
    bool Accepted,
    bool Victory,
    TrialTowerFloorDefinition Floor,
    int SuccessChance,
    int Roll,
    string Message,
    GameSnapshot State)
{
    public int ConsumedChallengeTickets { get; init; }
}

/// <summary>일반층을 연속 정복하다가 보스 직전 또는 첫 패배에서 멈춘 서버 스킵 결과입니다.</summary>
public sealed record TrialTowerSweepResult(
    bool Accepted,
    int StartFloor,
    int EndFloor,
    int ClearedFloors,
    bool Failed,
    bool StoppedBeforeBoss,
    int SummonTickets,
    long Gold,
    long UpgradeMaterials,
    string Message,
    GameSnapshot State)
{
    public int ConsumedChallengeTickets { get; init; }
}

/// <summary>CodeMaru C로 시련의 탑 도전권을 충전한 결과입니다.</summary>
public sealed record TrialTowerTicketPurchaseResult(
    bool Purchased,
    int TicketsPurchased,
    long PremiumSpent,
    string Message,
    GameSnapshot State);

/// <summary>몬스터의 한 번의 서버 공격 처리 결과입니다.</summary>
public sealed record EnemyAttackResult(
    bool Accepted,
    long Damage,
    bool PlayerDefeated,
    string? RejectionReason,
    GameSnapshot State)
{
    /// <summary>이번 적 공격이 적중한 주인공 또는 동료 유닛 ID입니다.</summary>
    public string? TargetUnitId { get; init; }
}
