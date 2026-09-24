using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

internal sealed class InMemoryProgressStore : IGameProgressStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, GameProgress> _rows = [];

    public async Task<GameLoadResult> LoadOrCreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var isNew = !_rows.TryGetValue(userId, out var value);
            value ??= New(userId);
            Normalize(value);
            _rows[userId] = Clone(value);
            return new GameLoadResult(Clone(value), isNew);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<GameProgress>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return _rows.Values.Select(Clone).ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<TResult> MutateAsync<TResult>(string userId, Func<GameProgress, TResult> mutation, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var value = _rows.TryGetValue(userId, out var current) ? Clone(current) : New(userId);
            Normalize(value);
            var result = mutation(value);
            Normalize(value);
            _rows[userId] = Clone(value);
            return result;
        }
        finally { _gate.Release(); }
    }

    public Task SeedAsync(string userId, Action<GameProgress> seed) =>
        MutateAsync(userId, value => { seed(value); return true; });

    public async Task<GameProgress> ReadAsync(string userId) =>
        (await LoadOrCreateAsync(userId)).Progress;

    private static GameProgress New(string userId) => new() { UserId = userId, EnemyHp = GameRules.EnemyMaxHp(1) };
    private static void Normalize(GameProgress value)
    {
        value.GameNickname = GameNicknameRules.Normalize(value.GameNickname);
        value.NicknameChangeCount = Math.Max(0, value.NicknameChangeCount);
        value.Stage = Math.Max(1, value.Stage);
        value.AttackLevel = Math.Max(1, value.AttackLevel);
        value.SwordArtLevel = Math.Max(0, value.SwordArtLevel);
        if (value.EquipmentLevel > 0
            && value.WeaponEquipmentLevel == 0
            && value.ArmorEquipmentLevel == 0
            && value.JadeEquipmentLevel == 0)
        {
            value.WeaponEquipmentLevel = value.EquipmentLevel;
            value.ArmorEquipmentLevel = value.EquipmentLevel;
            value.JadeEquipmentLevel = 0;
        }
        value.WeaponEquipmentLevel = Math.Max(0, value.WeaponEquipmentLevel);
        value.ArmorEquipmentLevel = Math.Max(0, value.ArmorEquipmentLevel);
        value.JadeEquipmentLevel = Math.Max(0, value.JadeEquipmentLevel);
        value.EquipmentLevel = value.WeaponEquipmentLevel;
        value.HighestClearedStage = Math.Max(value.HighestClearedStage, value.Stage - 1);
        value.HeroSummonTickets = Math.Max(0, value.HeroSummonTickets);
        value.HeroSummonPity = Math.Clamp(value.HeroSummonPity, 0, CompanionSummonPolicy.PityThreshold);
        value.LegendarySummonPity = Math.Clamp(value.LegendarySummonPity, 0, CompanionSummonPolicy.LegendaryPityThreshold);
        value.HighestTrialTowerFloor = Math.Max(0, value.HighestTrialTowerFloor);
        value.TrialTowerChallengeTickets = Math.Clamp(
            value.TrialTowerChallengeTickets,
            0,
            TrialTowerRules.MaximumStoredChallengeTickets);
        value.Arena ??= new ArenaState();
        ArenaRules.Normalize(value.Arena);
        value.Territory ??= new TerritoryState();
        TerritoryRules.Normalize(value.Territory);
        value.ClaimedAdminMailRewardIds ??= [];
        value.CompanionUpgradeMaterials = Math.Max(0, value.CompanionUpgradeMaterials);
        SpiritFarmRules.Normalize(value);
        value.CompanionShards ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.CompanionLevels ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.CompanionRanks ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.CompanionEquippedItems ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        value.CompanionEquipmentInventory ??= [];
        var equipmentCatalog = CompanionProgressionRules.GetEquipmentCatalog();
        value.CompanionEquipmentInventory = value.CompanionEquipmentInventory
            .Where(item => equipmentCatalog.Any(definition => string.Equals(definition.ItemId, item.ItemId, StringComparison.OrdinalIgnoreCase)))
            .DistinctBy(item => item.InstanceId, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => CompanionProgressionRules.NormalizeEquipmentInstance(item, index))
            .Take(CompanionProgressionRules.EquipmentInventoryCapacity)
            .ToList();
        foreach (var entry in value.CompanionEquippedItems.ToArray())
        {
            var legacy = equipmentCatalog.FirstOrDefault(item => string.Equals(item.ItemId, entry.Value, StringComparison.OrdinalIgnoreCase));
            if (legacy is null) continue;
            var instanceId = $"legacy-{entry.Key.Replace(':', '-').ToLowerInvariant()}";
            if (!value.CompanionEquipmentInventory.Any(item => string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase)))
                value.CompanionEquipmentInventory.Add(CompanionProgressionRules.CreateEquipmentInstance(
                    instanceId,
                    legacy.ItemId,
                    0,
                    value.CompanionEquipmentInventory.Count(item => string.Equals(item.ItemId, legacy.ItemId, StringComparison.OrdinalIgnoreCase))));
            value.CompanionEquippedItems[entry.Key] = instanceId;
        }
        if (!value.CompanionEquipmentInventoryInitialized)
        {
            foreach (var definition in CompanionProgressionRules.GetStarterEquipmentCatalog())
                if (!value.CompanionEquipmentInventory.Any(item => string.Equals(item.ItemId, definition.ItemId, StringComparison.OrdinalIgnoreCase)))
                    value.CompanionEquipmentInventory.Add(CompanionProgressionRules.CreateEquipmentInstance(
                        $"starter-{definition.ItemId}", definition.ItemId, 0, 0));
            value.CompanionEquipmentInventoryInitialized = true;
        }
        var equipmentInstances = value.CompanionEquipmentInventory.ToDictionary(item => item.InstanceId, StringComparer.OrdinalIgnoreCase);
        value.CompanionEquippedItems = value.CompanionEquippedItems
            .Where(item => equipmentInstances.TryGetValue(item.Value, out var instance)
                           && IsCompatibleEquipment(item.Key, equipmentCatalog.First(definition =>
                               string.Equals(definition.ItemId, instance.ItemId, StringComparison.OrdinalIgnoreCase))))
            .GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        value.OwnedCompanionSkins ??= [];
        value.ActiveCompanionSkins ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        value.PremiumCurrency = Math.Max(0, value.PremiumCurrency);
        value.DailyDefeatedBaseline = Math.Clamp(value.DailyDefeatedBaseline, 0, value.TotalDefeated);
        value.DailyStageBaseline = Math.Clamp(value.DailyStageBaseline, 0, value.HighestClearedStage);
        value.DailyEquipmentUpgradeCount = Math.Max(0, value.DailyEquipmentUpgradeCount);
        value.ClaimedDailyMissionKeys ??= [];
        value.OwnedCompanionIds ??= [];
        if (value.HighestSummonRewardedStage == 0 && value.HighestClearedStage > 0)
        {
            value.OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList();
            value.HighestSummonRewardedStage = value.HighestClearedStage;
        }
        value.ActiveHeroId = PartyRules.NormalizeActiveHeroId(value.ActiveHeroId);
        value.SelectedCompanionIds = PartyRules.NormalizeCompanionIds(value.HighestClearedStage, value.SelectedCompanionIds, value.OwnedCompanionIds);
        value.AutoAttackUnlocked = GameRules.CanUnlockAutoAttack(value.HighestClearedStage);
        if (!value.AutoAttackUnlocked) value.AutoAttackEnabled = false;
        value.AutoBoostInventory ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        value.AutoBoostInventory = value.AutoBoostInventory
            .Where(item => item.Value > 0)
            .Select(item => (Key: GameRules.NormalizeAutoSpeedBoostItemKey(item.Key), item.Value))
            .Where(item => item.Key is not null)
            .GroupBy(item => item.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Value), StringComparer.OrdinalIgnoreCase);
        value.ActiveAutoSpeedMultiplier = GameRules.NormalizeAutoSpeedMultiplier(value.ActiveAutoSpeedMultiplier);
        value.AutoSpeedBoostRemainingMilliseconds = Math.Max(0L, value.AutoSpeedBoostRemainingMilliseconds);
        if (value.ActiveAutoSpeedMultiplier <= 1)
        {
            value.ActiveAutoSpeedMultiplier = 1;
            value.AutoSpeedBoostEndsUtc = null;
            value.AutoSpeedBoostRemainingMilliseconds = 0;
        }
        if (value.EnemyHp <= 0) value.EnemyHp = GameRules.EnemyMaxHp(value.Stage);
    }

    private static bool IsCompatibleEquipment(string equipmentKey, CompanionEquipmentDefinition definition)
    {
        if (definition.Slot != CompanionEquipmentSlot.Weapon) return true;
        var separator = equipmentKey.IndexOf(':');
        if (separator <= 0) return false;
        var hero = PartyRules.AllCompanions.FirstOrDefault(item =>
            string.Equals(item.HeroId, equipmentKey[..separator], StringComparison.OrdinalIgnoreCase));
        return hero is not null && CompanionProgressionRules.CanEquip(hero, definition);
    }

    private static GameProgress Clone(GameProgress value) => new()
    {
        UserId = value.UserId,
        GameNickname = value.GameNickname,
        NicknameChangeCount = value.NicknameChangeCount,
        Stage = value.Stage, Gold = value.Gold, AttackLevel = value.AttackLevel,
        SwordArtLevel = value.SwordArtLevel, EquipmentLevel = value.WeaponEquipmentLevel,
        WeaponEquipmentLevel = value.WeaponEquipmentLevel,
        ArmorEquipmentLevel = value.ArmorEquipmentLevel,
        JadeEquipmentLevel = value.JadeEquipmentLevel,
        ActiveHeroId = value.ActiveHeroId, SelectedCompanionIds = value.SelectedCompanionIds.ToList(),
        OwnedCompanionIds = value.OwnedCompanionIds.ToList(),
        CompanionShards = new Dictionary<string, int>(value.CompanionShards, StringComparer.OrdinalIgnoreCase),
        CompanionLevels = new Dictionary<string, int>(value.CompanionLevels, StringComparer.OrdinalIgnoreCase),
        CompanionRanks = new Dictionary<string, int>(value.CompanionRanks, StringComparer.OrdinalIgnoreCase),
        CompanionEquippedItems = new Dictionary<string, string>(value.CompanionEquippedItems, StringComparer.OrdinalIgnoreCase),
        CompanionEquipmentInventory = value.CompanionEquipmentInventory.ToList(),
        CompanionEquipmentInventoryInitialized = value.CompanionEquipmentInventoryInitialized,
        OwnedCompanionSkins = value.OwnedCompanionSkins.ToList(),
        ActiveCompanionSkins = new Dictionary<string, string>(value.ActiveCompanionSkins, StringComparer.OrdinalIgnoreCase),
        PremiumCurrency = value.PremiumCurrency,
        CompanionUpgradeMaterials = value.CompanionUpgradeMaterials,
        HeroSummonTickets = value.HeroSummonTickets,
        HighestSummonRewardedStage = value.HighestSummonRewardedStage,
        HeroSummonPity = value.HeroSummonPity,
        LegendarySummonPity = value.LegendarySummonPity,
        HighestTrialTowerFloor = value.HighestTrialTowerFloor,
        TrialTowerChallengeTickets = value.TrialTowerChallengeTickets,
        TrialTowerTicketUpdatedUtc = value.TrialTowerTicketUpdatedUtc,
        Territory = TerritoryRules.Clone(value.Territory),
        Arena = new ArenaState
        {
            Rating = value.Arena.Rating,
            AttackWins = value.Arena.AttackWins,
            AttackLosses = value.Arena.AttackLosses,
            DefenseWins = value.Arena.DefenseWins,
            DefenseLosses = value.Arena.DefenseLosses,
            DailyDateKey = value.Arena.DailyDateKey,
            DailyChallenges = value.Arena.DailyChallenges,
            PurchasedTickets = value.Arena.PurchasedTickets,
            ReplaySpeed = value.Arena.ReplaySpeed,
            MatchmakingRevision = value.Arena.MatchmakingRevision
        },
        TotalDefeated = value.TotalDefeated, EnemyHp = value.EnemyHp,
        DailyMissionDateKey = value.DailyMissionDateKey,
        DailyDefeatedBaseline = value.DailyDefeatedBaseline,
        DailyStageBaseline = value.DailyStageBaseline,
        DailyEquipmentUpgradeCount = value.DailyEquipmentUpgradeCount,
        ClaimedDailyMissionKeys = value.ClaimedDailyMissionKeys.ToList(),
        ClaimedAdminMailRewardIds = value.ClaimedAdminMailRewardIds.ToList(),
        HighestClearedStage = value.HighestClearedStage, AutoAttackUnlocked = value.AutoAttackUnlocked,
        AutoAttackEnabled = value.AutoAttackEnabled, AutoUnlockNoticeSeen = value.AutoUnlockNoticeSeen,
        AutoBoostInventory = new Dictionary<string, int>(value.AutoBoostInventory, StringComparer.OrdinalIgnoreCase),
        ActiveAutoSpeedMultiplier = value.ActiveAutoSpeedMultiplier,
        AutoSpeedBoostEndsUtc = value.AutoSpeedBoostEndsUtc,
        AutoSpeedBoostRemainingMilliseconds = value.AutoSpeedBoostRemainingMilliseconds,
        SpiritFarmLevel = value.SpiritFarmLevel,
        SpiritFarmPlots = value.SpiritFarmPlots.Select(plot => new SpiritFarmPlotState
        {
            PlotIndex = plot.PlotIndex,
            CropKey = plot.CropKey,
            PlantedUtc = plot.PlantedUtc,
            ReadyUtc = plot.ReadyUtc,
            OwnerFertilized = plot.OwnerFertilized,
            FertilizedByUserIds = plot.FertilizedByUserIds.ToList()
        }).ToList(),
        SpiritFarmMeta = new SpiritFarmMetaState
        {
            DailySeedDateKey = value.SpiritFarmMeta.DailySeedDateKey,
            SeedPacks = new Dictionary<string, int>(value.SpiritFarmMeta.SeedPacks, StringComparer.OrdinalIgnoreCase),
            CropSeeds = new Dictionary<string, int>(value.SpiritFarmMeta.CropSeeds, StringComparer.OrdinalIgnoreCase),
            Soils = value.SpiritFarmMeta.Soils.Select(soil => new SpiritFarmSoilState
            {
                PlotIndex = soil.PlotIndex,
                Vitality = soil.Vitality,
                LastCropKey = soil.LastCropKey,
                ConsecutiveCrops = soil.ConsecutiveCrops
            }).ToList(),
            DailyFertilizerDateKey = value.SpiritFarmMeta.DailyFertilizerDateKey,
            FriendHelpDateKey = value.SpiritFarmMeta.FriendHelpDateKey,
            HelpedFriendIds = value.SpiritFarmMeta.HelpedFriendIds.ToList(),
            FertilizerCount = value.SpiritFarmMeta.FertilizerCount,
            VerticalRackLevel = value.SpiritFarmMeta.VerticalRackLevel,
            HouseTier = value.SpiritFarmMeta.HouseTier
        },
        UpdatedUtc = value.UpdatedUtc
    };
}

internal sealed class FixedRollSource(int roll) : IAttackRollSource
{
    public int NextRoll() => roll;
}

internal sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan elapsed) => _utcNow = _utcNow.Add(elapsed);
}

internal static class SessionFactory
{
    public static RegionCatalog Regions { get; } = RegionCatalog.Load(Path.Combine(AppContext.BaseDirectory, "regions.json"));
    public static GameSession Create(InMemoryProgressStore store, int roll = 50, TimeProvider? timeProvider = null) =>
        new(store, new FixedRollSource(roll), Regions, timeProvider);
}

internal static class CombatTestDriver
{
    public static async Task<GameSnapshot> DefeatEncounterGuardsAsync(GameSession session, GameSnapshot state)
    {
        if (state.Phase is CombatPhase.Loading or CombatPhase.Intro)
            state = await session.AdvanceCombatPhaseAsync();

        while (state.BossGuardUnits.Any(unit => unit.IsAlive))
        {
            var result = await session.AttackAsync(AttackSource.Manual);
            if (result.Accepted)
            {
                state = result.State;
                await Task.Delay(GameRules.MinimumManualAttackInterval + TimeSpan.FromMilliseconds(10));
            }
        }

        return state;
    }
}
