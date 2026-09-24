namespace GamePlatform.Domain;

/// <summary>개발용 테스트 계정의 전투 진행을 Stage 1 쇼케이스 상태로 되돌립니다.</summary>
public static class GameProgressResetPolicy
{
    /// <summary>사용자 식별자를 유지한 채 성장 데이터를 실제 신규 계정과 같은 상태로 초기화합니다.</summary>
    public static void ResetToStageOne(GameProgress value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Stage = 1;
        value.Gold = 0;
        value.AttackLevel = 1;
        value.SwordArtLevel = 0;
        value.EquipmentLevel = 0;
        value.WeaponEquipmentLevel = 0;
        value.ArmorEquipmentLevel = 0;
        value.JadeEquipmentLevel = 0;
        value.ActiveHeroId = "yeonsu";
        value.OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList();
        value.SelectedCompanionIds = PartyRules.StarterCompanionIds.Take(PartyRules.MaximumCompanions).ToList();
        value.CompanionShards = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.CompanionLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.CompanionRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.CompanionEquippedItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        value.CompanionEquipmentInventory = [];
        value.CompanionEquipmentInventoryInitialized = false;
        value.CompanionUpgradeMaterials = 0;
        value.HeroSummonTickets = 0;
        value.HighestSummonRewardedStage = 0;
        value.HeroSummonPity = 0;
        value.LegendarySummonPity = 0;
        value.HighestTrialTowerFloor = 0;
        value.TrialTowerChallengeTickets = TrialTowerRules.MaximumNaturalChallengeTickets;
        value.Arena = new ArenaState();
        value.Territory = new TerritoryState();
        value.TrialTowerTicketUpdatedUtc = DateTime.UtcNow;
        value.TotalDefeated = 0;
        value.DailyMissionDateKey = string.Empty;
        value.DailyDefeatedBaseline = 0;
        value.DailyStageBaseline = 0;
        value.DailyEquipmentUpgradeCount = 0;
        value.ClaimedDailyMissionKeys = [];
        value.EnemyHp = GameRules.EnemyMaxHp(1);
        value.HighestClearedStage = 0;
        value.AutoAttackUnlocked = false;
        value.AutoAttackEnabled = false;
        value.AutoBoostInventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.ActiveAutoSpeedMultiplier = 1;
        value.AutoSpeedBoostEndsUtc = null;
        value.AutoSpeedBoostRemainingMilliseconds = 0;
        value.AutoUnlockNoticeSeen = false;
        value.LastOfflineSettlementUtc = DateTime.UtcNow;
        value.PendingOfflineGold = 0;
        value.PendingOfflineSeconds = 0;
        value.UpdatedUtc = DateTime.UtcNow;
    }
}
