using System.Text.Json;
using Dreamine.Database.Abstractions.Mapping;
using GamePlatform.Domain;

namespace GamePlatform.Infrastructure;

[DatabaseTable("MaruIdlePlayers")]
internal sealed class GamePlayerRow
{
    [DatabaseKey]
    public string UserId { get; set; } = string.Empty;
    public string GameNickname { get; set; } = string.Empty;
    public int NicknameChangeCount { get; set; }
    public int Stage { get; set; } = 1;
    public long Gold { get; set; }
    public int AttackLevel { get; set; } = 1;
    public int SwordArtLevel { get; set; }
    public int EquipmentLevel { get; set; }
    public int WeaponEquipmentLevel { get; set; }
    public int ArmorEquipmentLevel { get; set; }
    public int JadeEquipmentLevel { get; set; }
    public string ActiveHeroId { get; set; } = "yeonsu";
    public string SelectedCompanionIdsJson { get; set; } = "[]";
    public string OwnedCompanionIdsJson { get; set; } = "[]";
    public string CompanionShardsJson { get; set; } = "{}";
    public long CompanionUpgradeMaterials { get; set; }
    public string CompanionLevelsJson { get; set; } = "{}";
    public string CompanionRanksJson { get; set; } = "{}";
    public string CompanionEquippedItemsJson { get; set; } = "{}";
    public string CompanionEquipmentInventoryJson { get; set; } = "[]";
    public bool CompanionEquipmentInventoryInitialized { get; set; }
    public string OwnedCompanionSkinsJson { get; set; } = "[]";
    public string ActiveCompanionSkinsJson { get; set; } = "{}";
    public long PremiumCurrency { get; set; }
    public long HeroSummonTickets { get; set; }
    public int HighestSummonRewardedStage { get; set; }
    public int HeroSummonPity { get; set; }
    public int LegendarySummonPity { get; set; }
    public int HighestTrialTowerFloor { get; set; }
    public int TrialTowerChallengeTickets { get; set; } = TrialTowerRules.MaximumNaturalChallengeTickets;
    public DateTime? TrialTowerTicketUpdatedUtc { get; set; }
    public string ArenaStateJson { get; set; } = "{}";
    public string TerritoryStateJson { get; set; } = "{}";
    public long TotalDefeated { get; set; }
    public string DailyMissionDateKey { get; set; } = string.Empty;
    public long DailyDefeatedBaseline { get; set; }
    public int DailyStageBaseline { get; set; }
    public int DailyEquipmentUpgradeCount { get; set; }
    public string ClaimedDailyMissionKeysJson { get; set; } = "[]";
    public string ClaimedAdminMailRewardIdsJson { get; set; } = "[]";
    public long BossHp { get; set; } = GameRules.EnemyMaxHp(1);
    public DateTime LastSettledUtc { get; set; } = DateTime.UtcNow;
    public long PendingOfflineGold { get; set; }
    public long PendingOfflineSeconds { get; set; }
    public int SpiritFarmLevel { get; set; } = SpiritFarmRules.MinimumLevel;
    public string SpiritFarmPlotsJson { get; set; } = "[]";
    public string SpiritFarmMetaJson { get; set; } = "{}";
    public int HighestClearedStage { get; set; }
    public bool AutoAttackUnlocked { get; set; }
    public bool AutoAttackEnabled { get; set; }
    public string AutoBoostInventoryJson { get; set; } = "{}";
    public int ActiveAutoSpeedMultiplier { get; set; } = 1;
    public DateTime? AutoSpeedBoostEndsUtc { get; set; }
    public long AutoSpeedBoostRemainingMilliseconds { get; set; }
    public bool AutoUnlockNoticeSeen { get; set; }
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public GameProgress ToDomain()
    {
        var migrateLegacyEquipment = EquipmentLevel > 0
                                     && WeaponEquipmentLevel == 0
                                     && ArmorEquipmentLevel == 0
                                     && JadeEquipmentLevel == 0;
        return new GameProgress
        {
        UserId = UserId,
        GameNickname = GameNickname,
        NicknameChangeCount = NicknameChangeCount,
        Stage = Stage,
        Gold = Gold,
        AttackLevel = AttackLevel,
        SwordArtLevel = SwordArtLevel,
        EquipmentLevel = migrateLegacyEquipment ? EquipmentLevel : WeaponEquipmentLevel,
        WeaponEquipmentLevel = migrateLegacyEquipment ? EquipmentLevel : WeaponEquipmentLevel,
        ArmorEquipmentLevel = migrateLegacyEquipment ? EquipmentLevel : ArmorEquipmentLevel,
        JadeEquipmentLevel = migrateLegacyEquipment ? 0 : JadeEquipmentLevel,
        ActiveHeroId = ActiveHeroId,
        SelectedCompanionIds = DeserializeCompanions(SelectedCompanionIdsJson),
        OwnedCompanionIds = DeserializeCompanions(OwnedCompanionIdsJson),
        CompanionShards = DeserializeInventory(CompanionShardsJson),
        CompanionUpgradeMaterials = CompanionUpgradeMaterials,
        CompanionLevels = DeserializeInventory(CompanionLevelsJson),
        CompanionRanks = DeserializeInventory(CompanionRanksJson),
        CompanionEquippedItems = DeserializeStringMap(CompanionEquippedItemsJson),
        CompanionEquipmentInventory = DeserializeEquipmentInventory(CompanionEquipmentInventoryJson),
        CompanionEquipmentInventoryInitialized = CompanionEquipmentInventoryInitialized,
        OwnedCompanionSkins = DeserializeCompanions(OwnedCompanionSkinsJson),
        ActiveCompanionSkins = DeserializeStringMap(ActiveCompanionSkinsJson),
        PremiumCurrency = PremiumCurrency,
        HeroSummonTickets = HeroSummonTickets,
        HighestSummonRewardedStage = HighestSummonRewardedStage,
        HeroSummonPity = HeroSummonPity,
        LegendarySummonPity = LegendarySummonPity,
        HighestTrialTowerFloor = HighestTrialTowerFloor,
        TrialTowerChallengeTickets = TrialTowerChallengeTickets,
        TrialTowerTicketUpdatedUtc = TrialTowerTicketUpdatedUtc,
        Arena = DeserializeArenaState(ArenaStateJson),
        Territory = TerritoryRules.Deserialize(TerritoryStateJson),
        TotalDefeated = TotalDefeated,
        DailyMissionDateKey = DailyMissionDateKey,
        DailyDefeatedBaseline = DailyDefeatedBaseline,
        DailyStageBaseline = DailyStageBaseline,
        DailyEquipmentUpgradeCount = DailyEquipmentUpgradeCount,
        ClaimedDailyMissionKeys = DeserializeCompanions(ClaimedDailyMissionKeysJson),
        ClaimedAdminMailRewardIds = DeserializeCompanions(ClaimedAdminMailRewardIdsJson),
        EnemyHp = BossHp,
        HighestClearedStage = HighestClearedStage,
        AutoAttackUnlocked = AutoAttackUnlocked,
        AutoAttackEnabled = AutoAttackEnabled,
        AutoBoostInventory = DeserializeInventory(AutoBoostInventoryJson),
        ActiveAutoSpeedMultiplier = ActiveAutoSpeedMultiplier,
        AutoSpeedBoostEndsUtc = AutoSpeedBoostEndsUtc,
        AutoSpeedBoostRemainingMilliseconds = AutoSpeedBoostRemainingMilliseconds,
        AutoUnlockNoticeSeen = AutoUnlockNoticeSeen,
        LastOfflineSettlementUtc = LastSettledUtc,
        PendingOfflineGold = PendingOfflineGold,
        PendingOfflineSeconds = PendingOfflineSeconds,
        SpiritFarmLevel = SpiritFarmLevel,
        SpiritFarmPlots = DeserializeFarmPlots(SpiritFarmPlotsJson),
        SpiritFarmMeta = DeserializeFarmMeta(SpiritFarmMetaJson),
        UpdatedUtc = UpdatedUtc
        };
    }

    public static GamePlayerRow FromDomain(GameProgress value) => new()
    {
        UserId = value.UserId,
        GameNickname = value.GameNickname,
        NicknameChangeCount = value.NicknameChangeCount,
        Stage = value.Stage,
        Gold = value.Gold,
        AttackLevel = value.AttackLevel,
        SwordArtLevel = value.SwordArtLevel,
        EquipmentLevel = value.WeaponEquipmentLevel,
        WeaponEquipmentLevel = value.WeaponEquipmentLevel,
        ArmorEquipmentLevel = value.ArmorEquipmentLevel,
        JadeEquipmentLevel = value.JadeEquipmentLevel,
        ActiveHeroId = value.ActiveHeroId,
        SelectedCompanionIdsJson = JsonSerializer.Serialize(value.SelectedCompanionIds),
        OwnedCompanionIdsJson = JsonSerializer.Serialize(value.OwnedCompanionIds),
        CompanionShardsJson = JsonSerializer.Serialize(value.CompanionShards),
        CompanionUpgradeMaterials = value.CompanionUpgradeMaterials,
        CompanionLevelsJson = JsonSerializer.Serialize(value.CompanionLevels),
        CompanionRanksJson = JsonSerializer.Serialize(value.CompanionRanks),
        CompanionEquippedItemsJson = JsonSerializer.Serialize(value.CompanionEquippedItems),
        CompanionEquipmentInventoryJson = JsonSerializer.Serialize(value.CompanionEquipmentInventory),
        CompanionEquipmentInventoryInitialized = value.CompanionEquipmentInventoryInitialized,
        OwnedCompanionSkinsJson = JsonSerializer.Serialize(value.OwnedCompanionSkins),
        ActiveCompanionSkinsJson = JsonSerializer.Serialize(value.ActiveCompanionSkins),
        PremiumCurrency = value.PremiumCurrency,
        HeroSummonTickets = value.HeroSummonTickets,
        HighestSummonRewardedStage = value.HighestSummonRewardedStage,
        HeroSummonPity = value.HeroSummonPity,
        LegendarySummonPity = value.LegendarySummonPity,
        HighestTrialTowerFloor = value.HighestTrialTowerFloor,
        TrialTowerChallengeTickets = value.TrialTowerChallengeTickets,
        TrialTowerTicketUpdatedUtc = value.TrialTowerTicketUpdatedUtc,
        ArenaStateJson = JsonSerializer.Serialize(value.Arena),
        TerritoryStateJson = JsonSerializer.Serialize(value.Territory),
        TotalDefeated = value.TotalDefeated,
        DailyMissionDateKey = value.DailyMissionDateKey,
        DailyDefeatedBaseline = value.DailyDefeatedBaseline,
        DailyStageBaseline = value.DailyStageBaseline,
        DailyEquipmentUpgradeCount = value.DailyEquipmentUpgradeCount,
        ClaimedDailyMissionKeysJson = JsonSerializer.Serialize(value.ClaimedDailyMissionKeys),
        ClaimedAdminMailRewardIdsJson = JsonSerializer.Serialize(value.ClaimedAdminMailRewardIds),
        BossHp = value.EnemyHp,
        LastSettledUtc = value.LastOfflineSettlementUtc,
        PendingOfflineGold = value.PendingOfflineGold,
        PendingOfflineSeconds = value.PendingOfflineSeconds,
        SpiritFarmLevel = value.SpiritFarmLevel,
        SpiritFarmPlotsJson = JsonSerializer.Serialize(value.SpiritFarmPlots),
        SpiritFarmMetaJson = JsonSerializer.Serialize(value.SpiritFarmMeta),
        HighestClearedStage = value.HighestClearedStage,
        AutoAttackUnlocked = value.AutoAttackUnlocked,
        AutoAttackEnabled = value.AutoAttackEnabled,
        AutoBoostInventoryJson = JsonSerializer.Serialize(value.AutoBoostInventory),
        ActiveAutoSpeedMultiplier = value.ActiveAutoSpeedMultiplier,
        AutoSpeedBoostEndsUtc = value.AutoSpeedBoostEndsUtc,
        AutoSpeedBoostRemainingMilliseconds = value.AutoSpeedBoostRemainingMilliseconds,
        AutoUnlockNoticeSeen = value.AutoUnlockNoticeSeen,
        UpdatedUtc = value.UpdatedUtc
    };

    private static Dictionary<string, int> DeserializeInventory(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static ArenaState DeserializeArenaState(string json)
    {
        try { return JsonSerializer.Deserialize<ArenaState>(json) ?? new ArenaState(); }
        catch (JsonException) { return new ArenaState(); }
    }

    private static List<string> DeserializeCompanions(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static Dictionary<string, string> DeserializeStringMap(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static List<CompanionEquipmentInstance> DeserializeEquipmentInventory(string json)
    {
        try { return JsonSerializer.Deserialize<List<CompanionEquipmentInstance>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static List<SpiritFarmPlotState> DeserializeFarmPlots(string json)
    {
        try { return JsonSerializer.Deserialize<List<SpiritFarmPlotState>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static SpiritFarmMetaState DeserializeFarmMeta(string json)
    {
        try { return JsonSerializer.Deserialize<SpiritFarmMetaState>(json) ?? new SpiritFarmMetaState(); }
        catch (JsonException) { return new SpiritFarmMetaState(); }
    }
}
