namespace GamePlatform.Domain;

/// <summary>사용자별 마루 원정 진행 데이터를 나타냅니다.</summary>
public sealed class GameProgress
{
    /// <summary>CodeMaru 사용자 식별자를 가져오거나 설정합니다.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>게임 안에서 다른 원정대에 표시하는 닉네임입니다. 비어 있으면 로그인 이름을 사용합니다.</summary>
    public string GameNickname { get; set; } = string.Empty;

    /// <summary>완료된 닉네임 변경 횟수입니다. 첫 변경은 무료이고 이후부터 유료입니다.</summary>
    public int NicknameChangeCount { get; set; }

    /// <summary>현재 전투 스테이지를 가져오거나 설정합니다.</summary>
    public int Stage { get; set; } = 1;

    /// <summary>보유 금화를 가져오거나 설정합니다.</summary>
    public long Gold { get; set; }

    /// <summary>검 강화 레벨을 가져오거나 설정합니다.</summary>
    public int AttackLevel { get; set; } = 1;

    /// <summary>검술 수련 레벨을 가져오거나 설정합니다.</summary>
    public int SwordArtLevel { get; set; }

    /// <summary>장비 단련 레벨을 가져오거나 설정합니다.</summary>
    public int EquipmentLevel { get; set; }

    /// <summary>계정 귀속 무기 단련 레벨입니다.</summary>
    public int WeaponEquipmentLevel { get; set; }

    /// <summary>계정 귀속 갑주 단련 레벨입니다.</summary>
    public int ArmorEquipmentLevel { get; set; }

    /// <summary>계정 귀속 인연 옥패 단련 레벨입니다.</summary>
    public int JadeEquipmentLevel { get; set; }

    /// <summary>현재 선택한 주인공 ID를 가져오거나 설정합니다.</summary>
    public string ActiveHeroId { get; set; } = "yeonsu";

    /// <summary>현재 편성한 보조 캐릭터 ID 목록을 가져오거나 설정합니다.</summary>
    public List<string> SelectedCompanionIds { get; set; } = [];

    /// <summary>소환 또는 기본 보상으로 영입한 동료 ID 목록을 가져오거나 설정합니다.</summary>
    public List<string> OwnedCompanionIds { get; set; } = [];

    /// <summary>동료별 성장 조각 보유 수를 가져오거나 설정합니다.</summary>
    public Dictionary<string, int> CompanionShards { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>동료 개인 장비 강화 재료 수를 가져오거나 설정합니다.</summary>
    public long CompanionUpgradeMaterials { get; set; }

    /// <summary>동료별 독립 레벨을 가져오거나 설정합니다.</summary>
    public Dictionary<string, int> CompanionLevels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>동료별 조각 승급 단계를 가져오거나 설정합니다.</summary>
    public Dictionary<string, int> CompanionRanks { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>동료·부위별 현재 착용한 개인 장비를 가져오거나 설정합니다.</summary>
    public Dictionary<string, string> CompanionEquippedItems { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>계정 장비 가방에 보관 중인 개별 개인 장비 목록입니다.</summary>
    public List<CompanionEquipmentInstance> CompanionEquipmentInventory { get; set; } = [];

    /// <summary>초기 장비 가방 지급 또는 구형 데이터 이전을 완료했는지 여부입니다.</summary>
    public bool CompanionEquipmentInventoryInitialized { get; set; }

    /// <summary>계정이 구매한 동료 스킨 키 목록을 가져오거나 설정합니다.</summary>
    public List<string> OwnedCompanionSkins { get; set; } = [];

    /// <summary>동료별 현재 적용 중인 스킨 ID를 가져오거나 설정합니다.</summary>
    public Dictionary<string, string> ActiveCompanionSkins { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>유료 상품에 사용하는 CodeMaru C 잔액을 가져오거나 설정합니다.</summary>
    public long PremiumCurrency { get; set; }

    /// <summary>무료 동료 소환패 수를 가져오거나 설정합니다.</summary>
    public long HeroSummonTickets { get; set; }

    /// <summary>소환패 지급을 완료한 가장 높은 최초 클리어 스테이지입니다.</summary>
    public int HighestSummonRewardedStage { get; set; }

    /// <summary>영웅 미획득 연속 소환 횟수를 가져오거나 설정합니다.</summary>
    public int HeroSummonPity { get; set; }

    /// <summary>전설 미획득 연속 소환 횟수를 가져오거나 설정합니다.</summary>
    public int LegendarySummonPity { get; set; }

    /// <summary>시련의 탑에서 최초 정복을 완료한 가장 높은 층입니다.</summary>
    public int HighestTrialTowerFloor { get; set; }

    /// <summary>현재 보유한 시련의 탑 도전권입니다. 자연 회복 한도보다 유료 충전분을 더 보유할 수 있습니다.</summary>
    public int TrialTowerChallengeTickets { get; set; } = TrialTowerRules.MaximumNaturalChallengeTickets;

    /// <summary>시련의 탑 도전권 자연 회복 계산 기준 UTC 시각입니다.</summary>
    public DateTime? TrialTowerTicketUpdatedUtc { get; set; }

    /// <summary>동일 서버 원정대와 겨루는 비동기 결투장 기록입니다.</summary>
    public ArenaState Arena { get; set; } = new();
    public TerritoryState Territory { get; set; } = new();

    /// <summary>누적 격파 수를 가져오거나 설정합니다.</summary>
    public long TotalDefeated { get; set; }

    /// <summary>대한민국 표준시 기준 현재 일일 임무 날짜 키입니다.</summary>
    public string DailyMissionDateKey { get; set; } = string.Empty;

    /// <summary>현재 일일 임무가 시작될 때의 누적 격파 수입니다.</summary>
    public long DailyDefeatedBaseline { get; set; }

    /// <summary>현재 일일 임무가 시작될 때의 최고 스테이지입니다.</summary>
    public int DailyStageBaseline { get; set; }

    /// <summary>오늘 성공한 개인 장비 강화 횟수입니다.</summary>
    public int DailyEquipmentUpgradeCount { get; set; }

    /// <summary>오늘 이미 수령한 일일 임무 키 목록입니다.</summary>
    public List<string> ClaimedDailyMissionKeys { get; set; } = [];

    /// <summary>한 번만 수령 가능한 운영 우편 첨부 보상의 메시지 ID 목록입니다.</summary>
    public List<string> ClaimedAdminMailRewardIds { get; set; } = [];

    /// <summary>현재 적의 남은 체력을 가져오거나 설정합니다.</summary>
    public long EnemyHp { get; set; } = GameRules.EnemyMaxHp(1);

    /// <summary>최고 클리어 스테이지를 가져오거나 설정합니다.</summary>
    public int HighestClearedStage { get; set; }

    /// <summary>자동공격 영구 해금 여부를 가져오거나 설정합니다.</summary>
    public bool AutoAttackUnlocked { get; set; }

    /// <summary>마지막 자동공격 ON/OFF 설정을 가져오거나 설정합니다.</summary>
    public bool AutoAttackEnabled { get; set; }

    /// <summary>미사용 AUTO 가속 부적의 키별 보유 수량을 가져오거나 설정합니다.</summary>
    public Dictionary<string, int> AutoBoostInventory { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>현재 활성화된 AUTO 속도 배율을 가져오거나 설정합니다.</summary>
    public int ActiveAutoSpeedMultiplier { get; set; } = 1;

    /// <summary>현재 AUTO 가속 효과가 종료되는 UTC 시각을 가져오거나 설정합니다.</summary>
    public DateTime? AutoSpeedBoostEndsUtc { get; set; }

    /// <summary>게임이 숨겨지거나 종료되었을 때 보존한 AUTO 가속 효과의 남은 밀리초입니다.</summary>
    public long AutoSpeedBoostRemainingMilliseconds { get; set; }

    /// <summary>자동공격 해금 안내를 이미 확인했는지 가져오거나 설정합니다.</summary>
    public bool AutoUnlockNoticeSeen { get; set; }

    /// <summary>마지막 방치 시간 정산 기준 UTC 시각을 가져오거나 설정합니다.</summary>
    public DateTime LastOfflineSettlementUtc { get; set; } = DateTime.UtcNow;

    /// <summary>아직 수령하지 않은 방치 금화를 가져오거나 설정합니다.</summary>
    public long PendingOfflineGold { get; set; }

    /// <summary>현재 미수령 보상에 반영된 방치 시간을 초 단위로 가져오거나 설정합니다.</summary>
    public long PendingOfflineSeconds { get; set; }

    /// <summary>계정에 영구 귀속되는 영약 농원 등급입니다.</summary>
    public int SpiritFarmLevel { get; set; } = SpiritFarmRules.MinimumLevel;

    /// <summary>게임 종료 후에도 성장 시간이 유지되는 농원 경작지 상태입니다.</summary>
    public List<SpiritFarmPlotState> SpiritFarmPlots { get; set; } = [];

    /// <summary>씨앗·토양·친구 도움·재배 시설을 함께 저장하는 농원 관리 상태입니다.</summary>
    public SpiritFarmMetaState SpiritFarmMeta { get; set; } = new();

    /// <summary>마지막 갱신 UTC 시각을 가져오거나 설정합니다.</summary>
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
