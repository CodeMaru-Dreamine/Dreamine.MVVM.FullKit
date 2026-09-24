using Dreamine.Database.Abstractions;
using Dreamine.Database.Sqlite;
using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Options;

namespace GamePlatform.Infrastructure;

/// <summary>Dreamine SQLite 공급자로 사용자별 게임 진행과 설정을 저장합니다.</summary>
public sealed partial class GameProgressStore : IGameProgressStore, ITerritoryWorldStore, IDisposable
{
    private readonly IDatabaseQueryProvider _queries;
    private readonly IDatabaseCommandExecutor _commands;
    private readonly IDatabaseRepository _repository;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly string _connectionString;

    /// <summary>게임 데이터베이스를 열고 기존 테이블을 비파괴 마이그레이션합니다.</summary>
    public GameProgressStore(GameOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var directory = Path.GetDirectoryName(options.DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        _connectionString = $"Data Source={options.DatabasePath}";
        var provider = new SqliteDatabaseProvider(_connectionString);
        provider.EnsureDatabaseExists();
        provider.CreateTable<GamePlayerRow>();
        _queries = provider;
        _commands = provider;
        _repository = provider;
        EnsureSchema();
    }

    /// <inheritdoc />
    public async Task<GameLoadResult> LoadOrCreateAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var row = await FindAsync(userId, cancellationToken).ConfigureAwait(false);
            var isNew = row is null;
            var progress = row?.ToDomain() ?? NewProgress(userId);
            Normalize(progress);
            await SaveCoreAsync(progress, isNew, cancellationToken).ConfigureAwait(false);
            return new GameLoadResult(Clone(progress), isNew);
        }
        finally { _writeLock.Release(); }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameProgress>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _queries.QueryAsync<GamePlayerRow>(
            """
            SELECT UserId, GameNickname, NicknameChangeCount, Stage, Gold, AttackLevel, SwordArtLevel, EquipmentLevel,
                   WeaponEquipmentLevel, ArmorEquipmentLevel, JadeEquipmentLevel,
                   ActiveHeroId, SelectedCompanionIdsJson, OwnedCompanionIdsJson,
                   CompanionShardsJson, CompanionUpgradeMaterials, CompanionLevelsJson,
                   CompanionRanksJson, CompanionEquippedItemsJson,
                   CompanionEquipmentInventoryJson, CompanionEquipmentInventoryInitialized,
                   OwnedCompanionSkinsJson,
                   ActiveCompanionSkinsJson, PremiumCurrency, HeroSummonTickets,
                   HighestSummonRewardedStage, HeroSummonPity, LegendarySummonPity, HighestTrialTowerFloor,
                   TrialTowerChallengeTickets, TrialTowerTicketUpdatedUtc, ArenaStateJson, TerritoryStateJson,
                   TotalDefeated, DailyMissionDateKey, DailyDefeatedBaseline,
                   DailyStageBaseline, DailyEquipmentUpgradeCount, ClaimedDailyMissionKeysJson, ClaimedAdminMailRewardIdsJson, BossHp,
                   LastSettledUtc, PendingOfflineGold, PendingOfflineSeconds,
                   SpiritFarmLevel, SpiritFarmPlotsJson, SpiritFarmMetaJson,
                   HighestClearedStage, AutoAttackUnlocked,
                   AutoAttackEnabled, AutoBoostInventoryJson, ActiveAutoSpeedMultiplier,
                   AutoSpeedBoostEndsUtc, AutoSpeedBoostRemainingMilliseconds,
                   AutoUnlockNoticeSeen, UpdatedUtc
            FROM MaruIdlePlayers
            """, new { }, cancellationToken).ConfigureAwait(false);
        return rows.Select(row =>
        {
            var progress = row.ToDomain();
            Normalize(progress);
            return Clone(progress);
        }).ToArray();
    }

    /// <inheritdoc />
    public async Task<TResult> MutateAsync<TResult>(
        string userId,
        Func<GameProgress, TResult> mutation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(mutation);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var row = await FindAsync(userId, cancellationToken).ConfigureAwait(false);
            var progress = row?.ToDomain() ?? NewProgress(userId);
            Normalize(progress);
            var result = mutation(progress);
            Normalize(progress);
            await SaveCoreAsync(progress, row is null, cancellationToken).ConfigureAwait(false);
            return result;
        }
        finally { _writeLock.Release(); }
    }

    private void EnsureSchema()
    {
        var columns = _queries.Query<ColumnInfo>("PRAGMA table_info(MaruIdlePlayers)")
            .Select(column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        AddColumn(columns, "GameNickname", "TEXT NOT NULL DEFAULT ''");
        AddColumn(columns, "NicknameChangeCount", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "SwordArtLevel", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "EquipmentLevel", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "WeaponEquipmentLevel", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "ArmorEquipmentLevel", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "JadeEquipmentLevel", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "ActiveHeroId", "TEXT NOT NULL DEFAULT 'yeonsu'");
        AddColumn(columns, "SelectedCompanionIdsJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "OwnedCompanionIdsJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "CompanionShardsJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "CompanionUpgradeMaterials", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "CompanionLevelsJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "CompanionRanksJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "CompanionEquippedItemsJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "CompanionEquipmentInventoryJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "CompanionEquipmentInventoryInitialized", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "OwnedCompanionSkinsJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "ActiveCompanionSkinsJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "PremiumCurrency", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "HeroSummonTickets", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "HighestSummonRewardedStage", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "HeroSummonPity", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "LegendarySummonPity", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "HighestTrialTowerFloor", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "TrialTowerChallengeTickets", $"INTEGER NOT NULL DEFAULT {TrialTowerRules.MaximumNaturalChallengeTickets}");
        AddColumn(columns, "TrialTowerTicketUpdatedUtc", "TEXT NULL");
        AddColumn(columns, "ArenaStateJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "TerritoryStateJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "DailyMissionDateKey", "TEXT NOT NULL DEFAULT ''");
        AddColumn(columns, "DailyDefeatedBaseline", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "DailyStageBaseline", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "DailyEquipmentUpgradeCount", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "ClaimedDailyMissionKeysJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "ClaimedAdminMailRewardIdsJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "HighestClearedStage", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "AutoAttackUnlocked", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "AutoAttackEnabled", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "AutoBoostInventoryJson", "TEXT NOT NULL DEFAULT '{}'");
        AddColumn(columns, "ActiveAutoSpeedMultiplier", "INTEGER NOT NULL DEFAULT 1");
        AddColumn(columns, "AutoSpeedBoostEndsUtc", "TEXT NULL");
        AddColumn(columns, "AutoSpeedBoostRemainingMilliseconds", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "AutoUnlockNoticeSeen", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "PendingOfflineGold", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "PendingOfflineSeconds", "INTEGER NOT NULL DEFAULT 0");
        AddColumn(columns, "SpiritFarmLevel", "INTEGER NOT NULL DEFAULT 1");
        AddColumn(columns, "SpiritFarmPlotsJson", "TEXT NOT NULL DEFAULT '[]'");
        AddColumn(columns, "SpiritFarmMetaJson", "TEXT NOT NULL DEFAULT '{}'");
    }

    private void AddColumn(HashSet<string> columns, string name, string definition)
    {
        if (columns.Contains(name)) return;
        _commands.ExecuteNonQuery($"ALTER TABLE MaruIdlePlayers ADD COLUMN {name} {definition}");
        columns.Add(name);
    }

    private async Task<GamePlayerRow?> FindAsync(string userId, CancellationToken cancellationToken)
    {
        var rows = await _queries.QueryAsync<GamePlayerRow>(
            """
            SELECT UserId, GameNickname, NicknameChangeCount, Stage, Gold, AttackLevel, SwordArtLevel, EquipmentLevel,
                   WeaponEquipmentLevel, ArmorEquipmentLevel, JadeEquipmentLevel,
                   ActiveHeroId, SelectedCompanionIdsJson, OwnedCompanionIdsJson,
                   CompanionShardsJson, CompanionUpgradeMaterials, CompanionLevelsJson,
                   CompanionRanksJson, CompanionEquippedItemsJson,
                   CompanionEquipmentInventoryJson, CompanionEquipmentInventoryInitialized,
                   OwnedCompanionSkinsJson,
                   ActiveCompanionSkinsJson, PremiumCurrency, HeroSummonTickets,
                   HighestSummonRewardedStage, HeroSummonPity, LegendarySummonPity, HighestTrialTowerFloor,
                   TrialTowerChallengeTickets, TrialTowerTicketUpdatedUtc, ArenaStateJson, TerritoryStateJson,
                   TotalDefeated, DailyMissionDateKey, DailyDefeatedBaseline,
                   DailyStageBaseline, DailyEquipmentUpgradeCount, ClaimedDailyMissionKeysJson, ClaimedAdminMailRewardIdsJson, BossHp,
                   LastSettledUtc, PendingOfflineGold, PendingOfflineSeconds,
                   SpiritFarmLevel, SpiritFarmPlotsJson, SpiritFarmMetaJson,
                   HighestClearedStage, AutoAttackUnlocked,
                   AutoAttackEnabled, AutoBoostInventoryJson, ActiveAutoSpeedMultiplier,
                   AutoSpeedBoostEndsUtc, AutoSpeedBoostRemainingMilliseconds,
                   AutoUnlockNoticeSeen, UpdatedUtc
            FROM MaruIdlePlayers
            WHERE UserId = @UserId
            LIMIT 1
            """,
            new { UserId = userId }, cancellationToken).ConfigureAwait(false);
        return rows.FirstOrDefault();
    }

    private async Task SaveCoreAsync(GameProgress progress, bool insert, CancellationToken cancellationToken)
    {
        var row = GamePlayerRow.FromDomain(progress);
        if (insert)
        {
            await _repository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
            return;
        }

        var updated = await _repository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        if (!updated) await _repository.InsertAsync(row, cancellationToken).ConfigureAwait(false);
    }

    private static GameProgress NewProgress(string userId) => new()
    {
        UserId = userId,
        Stage = 1,
        AttackLevel = 1,
        OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(),
        SelectedCompanionIds = PartyRules.StarterCompanionIds.Take(PartyRules.MaximumCompanions).ToList(),
        EnemyHp = GameRules.EnemyMaxHp(1),
        LastOfflineSettlementUtc = DateTime.UtcNow,
        UpdatedUtc = DateTime.UtcNow
    };

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
        value.Gold = Math.Max(0, value.Gold);
        value.TotalDefeated = Math.Max(0, value.TotalDefeated);
        value.HighestClearedStage = Math.Max(value.HighestClearedStage, value.Stage - 1);
        value.DailyDefeatedBaseline = Math.Clamp(value.DailyDefeatedBaseline, 0, value.TotalDefeated);
        value.DailyStageBaseline = Math.Clamp(value.DailyStageBaseline, 0, value.HighestClearedStage);
        value.DailyEquipmentUpgradeCount = Math.Max(0, value.DailyEquipmentUpgradeCount);
        var validMissionKeys = DailyMissionPolicy.GetDefinitions().Select(item => item.MissionKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        value.ClaimedDailyMissionKeys = (value.ClaimedDailyMissionKeys ?? [])
            .Where(validMissionKeys.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        value.ClaimedAdminMailRewardIds = (value.ClaimedAdminMailRewardIds ?? [])
            .Where(id => id.Length == 32 && Guid.TryParseExact(id, "N", out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .TakeLast(500)
            .ToList();
        value.HeroSummonTickets = Math.Max(0, value.HeroSummonTickets);
        value.HighestSummonRewardedStage = Math.Clamp(value.HighestSummonRewardedStage, 0, value.HighestClearedStage);
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
        value.CompanionUpgradeMaterials = Math.Max(0, value.CompanionUpgradeMaterials);
        value.PremiumCurrency = Math.Max(0, value.PremiumCurrency);
        value.PendingOfflineGold = Math.Max(0, value.PendingOfflineGold);
        value.PendingOfflineSeconds = Math.Max(0, value.PendingOfflineSeconds);
        SpiritFarmRules.Normalize(value);
        if (value.LastOfflineSettlementUtc == default)
            value.LastOfflineSettlementUtc = value.UpdatedUtc == default ? DateTime.UtcNow : value.UpdatedUtc;
        var validHeroIds = PartyRules.AllCompanions.Select(hero => hero.HeroId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        value.CompanionLevels = (value.CompanionLevels ?? new Dictionary<string, int>())
            .Where(item => validHeroIds.Contains(item.Key))
            .ToDictionary(item => item.Key, item => Math.Max(1, item.Value), StringComparer.OrdinalIgnoreCase);
        value.CompanionRanks = (value.CompanionRanks ?? new Dictionary<string, int>())
            .Where(item => validHeroIds.Contains(item.Key))
            .ToDictionary(item => item.Key, item => Math.Clamp(item.Value, 1, CompanionProgressionRules.MaximumRank), StringComparer.OrdinalIgnoreCase);
        NormalizeCompanionEquipmentInventory(value, validHeroIds);
        value.OwnedCompanionSkins = (value.OwnedCompanionSkins ?? [])
            .Where(key => key.Contains(':'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        value.ActiveCompanionSkins = (value.ActiveCompanionSkins ?? new Dictionary<string, string>())
            .Where(item => validHeroIds.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        value.CompanionShards = (value.CompanionShards ?? new Dictionary<string, int>())
            .Where(item => item.Value > 0 && PartyRules.AllCompanions.Any(hero => string.Equals(hero.HeroId, item.Key, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        value.OwnedCompanionIds = (value.OwnedCompanionIds ?? [])
            .Concat(value.HighestSummonRewardedStage == 0 && value.HighestClearedStage > 0
                ? PartyRules.StarterCompanionIds
                : [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(id => PartyRules.AllCompanions.Any(hero => string.Equals(hero.HeroId, id, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (value.HighestSummonRewardedStage == 0 && value.HighestClearedStage > 0)
            value.HighestSummonRewardedStage = value.HighestClearedStage;
        value.ActiveHeroId = PartyRules.NormalizeActiveHeroId(value.ActiveHeroId);
        value.SelectedCompanionIds = PartyRules.NormalizeCompanionIds(
            value.HighestClearedStage,
            value.SelectedCompanionIds,
            value.OwnedCompanionIds);
        value.AutoAttackUnlocked = GameRules.CanUnlockAutoAttack(value.HighestClearedStage);
        if (!value.AutoAttackUnlocked) value.AutoAttackEnabled = false;
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
        var maximum = GameRules.EnemyMaxHp(value.Stage);
        value.EnemyHp = value.EnemyHp <= 0 ? maximum : Math.Min(value.EnemyHp, maximum);
        if (value.UpdatedUtc == default) value.UpdatedUtc = DateTime.UtcNow;
    }

    private static GameProgress Clone(GameProgress value) => new()
    {
        UserId = value.UserId,
        GameNickname = value.GameNickname,
        NicknameChangeCount = value.NicknameChangeCount,
        Stage = value.Stage, Gold = value.Gold,
        AttackLevel = value.AttackLevel, SwordArtLevel = value.SwordArtLevel,
        EquipmentLevel = value.WeaponEquipmentLevel,
        WeaponEquipmentLevel = value.WeaponEquipmentLevel,
        ArmorEquipmentLevel = value.ArmorEquipmentLevel,
        JadeEquipmentLevel = value.JadeEquipmentLevel,
        TotalDefeated = value.TotalDefeated,
        DailyMissionDateKey = value.DailyMissionDateKey,
        DailyDefeatedBaseline = value.DailyDefeatedBaseline,
        DailyStageBaseline = value.DailyStageBaseline,
        DailyEquipmentUpgradeCount = value.DailyEquipmentUpgradeCount,
        ClaimedDailyMissionKeys = value.ClaimedDailyMissionKeys.ToList(),
        ClaimedAdminMailRewardIds = value.ClaimedAdminMailRewardIds.ToList(),
        ActiveHeroId = value.ActiveHeroId,
        SelectedCompanionIds = value.SelectedCompanionIds.ToList(),
        OwnedCompanionIds = value.OwnedCompanionIds.ToList(),
        CompanionShards = new Dictionary<string, int>(value.CompanionShards, StringComparer.OrdinalIgnoreCase),
        CompanionUpgradeMaterials = value.CompanionUpgradeMaterials,
        CompanionLevels = new Dictionary<string, int>(value.CompanionLevels, StringComparer.OrdinalIgnoreCase),
        CompanionRanks = new Dictionary<string, int>(value.CompanionRanks, StringComparer.OrdinalIgnoreCase),
        CompanionEquippedItems = new Dictionary<string, string>(value.CompanionEquippedItems, StringComparer.OrdinalIgnoreCase),
        CompanionEquipmentInventory = value.CompanionEquipmentInventory.ToList(),
        CompanionEquipmentInventoryInitialized = value.CompanionEquipmentInventoryInitialized,
        OwnedCompanionSkins = value.OwnedCompanionSkins.ToList(),
        ActiveCompanionSkins = new Dictionary<string, string>(value.ActiveCompanionSkins, StringComparer.OrdinalIgnoreCase),
        PremiumCurrency = value.PremiumCurrency,
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
        EnemyHp = value.EnemyHp, HighestClearedStage = value.HighestClearedStage,
        AutoAttackUnlocked = value.AutoAttackUnlocked, AutoAttackEnabled = value.AutoAttackEnabled,
        AutoBoostInventory = new Dictionary<string, int>(value.AutoBoostInventory, StringComparer.OrdinalIgnoreCase),
        ActiveAutoSpeedMultiplier = value.ActiveAutoSpeedMultiplier,
        AutoSpeedBoostEndsUtc = value.AutoSpeedBoostEndsUtc,
        AutoSpeedBoostRemainingMilliseconds = value.AutoSpeedBoostRemainingMilliseconds,
        AutoUnlockNoticeSeen = value.AutoUnlockNoticeSeen, UpdatedUtc = value.UpdatedUtc
        , LastOfflineSettlementUtc = value.LastOfflineSettlementUtc,
        PendingOfflineGold = value.PendingOfflineGold,
        PendingOfflineSeconds = value.PendingOfflineSeconds
        , SpiritFarmLevel = value.SpiritFarmLevel
        , SpiritFarmPlots = value.SpiritFarmPlots.Select(plot => new SpiritFarmPlotState
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
        }
    };

    private static void NormalizeCompanionEquipmentInventory(GameProgress value, IReadOnlySet<string> validHeroIds)
    {
        var catalog = CompanionProgressionRules.GetEquipmentCatalog();
        var catalogById = catalog.ToDictionary(item => item.ItemId, StringComparer.OrdinalIgnoreCase);
        value.CompanionEquipmentInventory = (value.CompanionEquipmentInventory ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.InstanceId) && catalogById.ContainsKey(item.ItemId))
            .GroupBy(item => item.InstanceId, StringComparer.OrdinalIgnoreCase)
            .Select((group, index) => CompanionProgressionRules.NormalizeEquipmentInstance(group.First(), index))
            .Take(CompanionProgressionRules.EquipmentInventoryCapacity)
            .ToList();

        var equipped = value.CompanionEquippedItems ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in equipped.ToArray())
        {
            if (!catalogById.TryGetValue(entry.Value, out var legacyDefinition)) continue;
            var instanceId = $"legacy-{entry.Key.Replace(':', '-').ToLowerInvariant()}";
            if (!value.CompanionEquipmentInventory.Any(item => string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase)))
                value.CompanionEquipmentInventory.Add(CompanionProgressionRules.CreateEquipmentInstance(
                    instanceId,
                    legacyDefinition.ItemId,
                    0,
                    value.CompanionEquipmentInventory.Count(item => string.Equals(item.ItemId, legacyDefinition.ItemId, StringComparison.OrdinalIgnoreCase))));
            equipped[entry.Key] = instanceId;
        }

        if (!value.CompanionEquipmentInventoryInitialized)
        {
            foreach (var definition in CompanionProgressionRules.GetStarterEquipmentCatalog())
            {
                if (value.CompanionEquipmentInventory.Count >= CompanionProgressionRules.EquipmentInventoryCapacity) break;
                if (value.CompanionEquipmentInventory.Any(item => string.Equals(item.ItemId, definition.ItemId, StringComparison.OrdinalIgnoreCase))) continue;
                value.CompanionEquipmentInventory.Add(CompanionProgressionRules.CreateEquipmentInstance(
                    $"starter-{definition.ItemId}", definition.ItemId, 0, 0));
            }
            value.CompanionEquipmentInventoryInitialized = true;
        }

        var instances = value.CompanionEquipmentInventory.ToDictionary(item => item.InstanceId, StringComparer.OrdinalIgnoreCase);
        value.CompanionEquippedItems = equipped
            .Where(entry => TryResolveEquipmentKey(entry.Key, validHeroIds, out var slot)
                            && instances.TryGetValue(entry.Value, out var instance)
                            && catalogById[instance.ItemId].Slot == slot
                            && IsCompatibleEquipment(entry.Key, catalogById[instance.ItemId]))
            .GroupBy(entry => entry.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);
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

    private static bool TryResolveEquipmentKey(string key, IReadOnlySet<string> validHeroIds, out CompanionEquipmentSlot slot)
    {
        slot = default;
        var separator = key.IndexOf(':');
        return separator > 0
               && validHeroIds.Contains(key[..separator])
               && Enum.TryParse(key[(separator + 1)..], true, out slot);
    }

    /// <summary>저장소 동시성 잠금을 정리합니다.</summary>
    public void Dispose() => _writeLock.Dispose();

    private sealed class ColumnInfo { public string Name { get; init; } = string.Empty; }
}
