using GamePlatform.Domain;

namespace GamePlatform.Application;

/// <summary>상태 조회, 공격, 성장 및 AUTO 설정 유스케이스를 한 사용자 회로에 제공합니다.</summary>
public sealed partial class GameSession
{
    private readonly IGameProgressStore _store;
    private readonly IAttackRollSource _rolls;
    private readonly RegionCatalog _regions;
    private readonly TimeProvider _timeProvider;
    private readonly IGameAttackRateLimiter _attackRateLimiter;
    private readonly IGameAdminMessageStore? _adminMessageStore;
    private readonly IGameSettingsStore? _settingsStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, long> _companionHp = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _bossGuardHp = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _claimedAdminMailRewardIds = new(StringComparer.OrdinalIgnoreCase);
    private string? _userId;
    private long _playerHp = GameRules.PlayerMaxHp(1, 0);
    private int _combatantStage;
    private int _enemyTargetCursor;
    private string? _endlessTrialRunId;
    private DateTime _endlessTrialStartedUtc;
    private string? _spiritVeinRunId;
    private DateTime _spiritVeinStartedUtc;
    private string? _swordFormationRunId;
    private DateTime _swordFormationStartedUtc;
    private string? _spiritFortressRunId;
    private DateTime _spiritFortressStartedUtc;

    /// <summary>게임 세션 유스케이스를 만듭니다.</summary>
    public GameSession(
        IGameProgressStore store,
        IAttackRollSource rolls,
        RegionCatalog regions,
        TimeProvider? timeProvider = null,
        IGameAttackRateLimiter? attackRateLimiter = null,
        IGameAdminMessageStore? adminMessageStore = null,
        IGameSettingsStore? settingsStore = null)
    {
        _store = store;
        _rolls = rolls;
        _regions = regions;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _attackRateLimiter = attackRateLimiter ?? new GameAttackRateLimiter(_timeProvider);
        _adminMessageStore = adminMessageStore;
        _settingsStore = settingsStore;
    }

    /// <summary>현재 Presentation 상태를 가져옵니다.</summary>
    public GameSnapshot Snapshot { get; private set; } = GameSnapshot.Empty;

    /// <summary>로그인 사용자의 저장 데이터를 초기화하고 복원합니다.</summary>
    public async Task<GameSnapshot> InitializeAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var loaded = await _store.LoadOrCreateAsync(userId, cancellationToken).ConfigureAwait(false);
        var initialProgress = loaded.Progress;
        if (!loaded.IsNewPlayer)
        {
            initialProgress = await _store.MutateAsync(
                userId,
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    var quote = OfflineRewardPolicy.Quote(value, now);
                    if (quote.Gold > 0)
                    {
                        value.PendingOfflineGold = GameRules.SaturatingAdd(value.PendingOfflineGold, quote.Gold);
                        value.PendingOfflineSeconds = GameRules.SaturatingAdd(
                            value.PendingOfflineSeconds,
                            Math.Max(0L, (long)quote.EligibleElapsed.TotalSeconds));
                    }
                    value.LastOfflineSettlementUtc = now;
                    ResumeActiveBoost(value, now);
                    DailyMissionPolicy.EnsureCurrentDay(value, now);
                    SpiritFarmRules.EnsureDailySupply(value, now);
                    TrialTowerRules.RefreshChallengeTickets(value, now);
                    return value;
                }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            initialProgress = await _store.MutateAsync(
                userId,
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    DailyMissionPolicy.EnsureCurrentDay(value, now);
                    SpiritFarmRules.EnsureDailySupply(value, now);
                    TrialTowerRules.RefreshChallengeTickets(value, now);
                    return value;
                }, cancellationToken).ConfigureAwait(false);
        }
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _userId = userId;
            _claimedAdminMailRewardIds.Clear();
            _claimedAdminMailRewardIds.UnionWith(initialProgress.ClaimedAdminMailRewardIds);
            NormalizeActiveBoost(initialProgress, _timeProvider.GetUtcNow().UtcDateTime);
            _playerHp = GameRules.PlayerMaxHp(initialProgress.AttackLevel, initialProgress.ArmorEquipmentLevel);
            _combatantStage = 0;
            var initialPhase = GameRules.IsBossStage(initialProgress.Stage) ? CombatPhase.Intro : CombatPhase.Fighting;
            Snapshot = CreateSnapshot(initialProgress, initialPhase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    public bool HasClaimedAdminMailReward(string messageId) =>
        _claimedAdminMailRewardIds.Contains(messageId);

    public async Task<GameAdminMailClaimResult> ClaimAdminMailRewardAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (_adminMessageStore is null)
            return new(false, false, "우편 보상 서비스를 사용할 수 없습니다.", Snapshot);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var userId = RequireUserId();
            var message = await _adminMessageStore.FindForUserAsync(messageId, userId, cancellationToken).ConfigureAwait(false);
            if (message is null || message.Kind != GameAdminMessageKind.Mail)
                return new(false, false, "수령할 운영 우편을 찾지 못했습니다.", Snapshot);
            if (message.AttachmentKind == GameAdminMailAttachmentKind.None || message.AttachmentAmount < 1)
                return new(false, false, "이 우편에는 수령할 선물이 없습니다.", Snapshot);
            if (_claimedAdminMailRewardIds.Contains(message.MessageId))
                return new(false, true, "이미 수령한 우편 선물입니다.", Snapshot);

            var claimed = false;
            var progress = await _store.MutateAsync(
                userId,
                value =>
                {
                    if (value.ClaimedAdminMailRewardIds.Contains(message.MessageId, StringComparer.OrdinalIgnoreCase))
                        return value;
                    switch (message.AttachmentKind)
                    {
                        case GameAdminMailAttachmentKind.PremiumCurrency:
                            value.PremiumCurrency = GameRules.SaturatingAdd(value.PremiumCurrency, message.AttachmentAmount);
                            break;
                        case GameAdminMailAttachmentKind.Gold:
                            value.Gold = GameRules.SaturatingAdd(value.Gold, message.AttachmentAmount);
                            break;
                        case GameAdminMailAttachmentKind.HeroSummonTickets:
                            value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, message.AttachmentAmount);
                            break;
                        default:
                            return value;
                    }
                    value.ClaimedAdminMailRewardIds.Add(message.MessageId);
                    claimed = true;
                    return value;
                }, cancellationToken).ConfigureAwait(false);

            _claimedAdminMailRewardIds.Add(message.MessageId);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return claimed
                ? new(true, false, $"{AdminMailAttachmentLabel(message.AttachmentKind)} {message.AttachmentAmount:N0}을 수령했습니다.", Snapshot)
                : new(false, true, "이미 수령한 우편 선물입니다.", Snapshot);
        }
        finally { _gate.Release(); }
    }

    private static string AdminMailAttachmentLabel(GameAdminMailAttachmentKind kind) => kind switch
    {
        GameAdminMailAttachmentKind.PremiumCurrency => "CodeMaru C",
        GameAdminMailAttachmentKind.Gold => "금화",
        GameAdminMailAttachmentKind.HeroSummonTickets => "동료 소환패",
        _ => "선물"
    };

    /// <summary>공개 게임 닉네임을 변경합니다. 첫 변경은 무료이며 이후에는 CodeMaru C가 소모됩니다.</summary>
    public async Task<NicknameChangeResult> ChangeNicknameAsync(
        string requestedNickname,
        CancellationToken cancellationToken = default)
    {
        if (!GameNicknameRules.TryValidate(requestedNickname, out var nickname, out var validationMessage))
            return new NicknameChangeResult(false, validationMessage, 0, Snapshot);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var userId = RequireUserId();
            var players = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
            if (players.Any(player =>
                    !string.Equals(player.UserId, userId, StringComparison.Ordinal)
                    && string.Equals(
                        GameNicknameRules.Normalize(player.GameNickname),
                        nickname,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return new NicknameChangeResult(false, "이미 다른 원정대가 사용 중인 닉네임입니다.", 0, Snapshot);
            }

            var accepted = false;
            var charged = 0L;
            var message = string.Empty;
            var progress = await _store.MutateAsync(
                userId,
                value =>
                {
                    if (string.Equals(value.GameNickname, nickname, StringComparison.OrdinalIgnoreCase))
                    {
                        message = "현재 사용 중인 닉네임입니다.";
                        return value;
                    }

                    charged = value.NicknameChangeCount == 0 ? 0 : GameNicknameRules.PaidChangeCost;
                    if (value.PremiumCurrency < charged)
                    {
                        message = $"닉네임 변경에 CodeMaru C {charged:N0}이 필요합니다.";
                        charged = 0;
                        return value;
                    }

                    value.PremiumCurrency -= charged;
                    value.GameNickname = nickname;
                    value.NicknameChangeCount++;
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    accepted = true;
                    message = charged == 0
                        ? "첫 닉네임을 무료로 변경했습니다."
                        : $"CodeMaru C {charged:N0}을 사용해 닉네임을 변경했습니다.";
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new NicknameChangeResult(accepted, message, charged, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>누적된 방치 금화를 한 번만 수령하고 원장을 원자적으로 비웁니다.</summary>
    public async Task<OfflineRewardClaimResult> ClaimOfflineRewardAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var claimed = 0L;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    if (!OfflineRewardPolicy.IsUnlocked(value.HighestClearedStage)) return value;
                    claimed = Math.Max(0L, value.PendingOfflineGold);
                    if (claimed <= 0) return value;
                    value.Gold = GameRules.SaturatingAdd(value.Gold, claimed);
                    value.PendingOfflineGold = 0;
                    value.PendingOfflineSeconds = 0;
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new OfflineRewardClaimResult(
                claimed > 0,
                claimed,
                claimed > 0 ? $"방치 보상 {claimed:N0} 금화를 수령했습니다." : "수령할 방치 보상이 없습니다.",
                Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>클라이언트 데미지를 받지 않고 서버 정책으로 한 번의 공격을 처리합니다.</summary>
    public async Task<AttackResult> AttackAsync(AttackSource source, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Snapshot.Phase != CombatPhase.Fighting)
                return Rejected(source, "전투 연출이 끝난 뒤 공격할 수 있습니다.");
            if (source == AttackSource.Automatic
                && (!Snapshot.AutoAttackUnlocked || !Snapshot.AutoAttackEnabled))
                return Rejected(source, "AUTO가 잠겨 있거나 꺼져 있습니다.");

            var minimum = source == AttackSource.Automatic
                ? GameRules.MinimumAutomaticAttackIntervalFor(Snapshot.ActiveAutoSpeedMultiplier)
                : GameRules.MinimumManualAttackInterval;
            if (!_attackRateLimiter.TryAcquire(RequireUserId(), source, minimum))
                return Rejected(source, "공격 간격이 너무 짧습니다.");
            var guardTargetId = Snapshot.BossGuardUnits.FirstOrDefault(unit => unit.IsAlive)?.UnitId;
            var expectedStage = Snapshot.Stage;
            var mutation = await _store.MutateAsync(
                RequireUserId(),
                progress => ApplyAttack(progress, source, guardTargetId, expectedStage),
                cancellationToken).ConfigureAwait(false);

            if (mutation.EnemyDefeated)
                _playerHp = GameRules.PlayerMaxHp(mutation.Progress.AttackLevel, mutation.Progress.ArmorEquipmentLevel);
            var nextPhase = mutation.EnemyDefeated ? CombatPhase.Victory : CombatPhase.Fighting;
            Snapshot = CreateSnapshot(mutation.Progress, nextPhase);
            if (!mutation.Accepted)
                return Rejected(source, mutation.RejectionReason ?? "공격을 처리할 수 없습니다.");
            return new AttackResult(
                true, source, mutation.Damage, mutation.IsCritical, mutation.EnemyDefeated,
                mutation.Reward, mutation.AutoUnlockedNow, mutation.AutoSpeedBoostDropped, null, Snapshot)
            {
                TargetUnitId = mutation.TargetUnitId ?? "enemy",
                EquipmentDropped = mutation.EquipmentDropped
            };
        }
        finally { _gate.Release(); }
    }

    /// <summary>현재 스테이지와 장비 방어력으로 몬스터의 공격을 서버에서 처리합니다.</summary>
    public async Task<EnemyAttackResult> EnemyAttackAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Snapshot.Phase != CombatPhase.Fighting)
                return new EnemyAttackResult(false, 0, false, "전투 중에만 몬스터가 공격합니다.", Snapshot);

            var damage = GameRules.EnemyAttackDamage(
                Snapshot.Stage,
                Snapshot.ArmorEquipmentLevel,
                _rolls.NextRoll());
            var livingCompanions = Snapshot.CompanionUnits.Where(unit => unit.IsAlive).ToArray();
            if (livingCompanions.Length > 0)
            {
                var target = livingCompanions[_enemyTargetCursor++ % livingCompanions.Length];
                var growth = Snapshot.CompanionGrowth.FirstOrDefault(item =>
                    string.Equals(item.Hero.HeroId, target.UnitId, StringComparison.OrdinalIgnoreCase));
                damage = CompanionProgressionRules.ReduceDamage(damage, growth?.DefensePercent ?? 0);
                _companionHp[target.UnitId] = Math.Max(0L, target.Hp - damage);
                Snapshot = Snapshot with
                {
                    CompanionUnits = Snapshot.CompanionUnits
                        .Select(unit => unit.UnitId == target.UnitId
                            ? unit with { Hp = _companionHp[target.UnitId] }
                            : unit)
                        .ToArray()
                };
                return new EnemyAttackResult(true, damage, false, null, Snapshot)
                {
                    TargetUnitId = target.UnitId
                };
            }

            _playerHp = Math.Max(0L, _playerHp - damage);
            var defeated = _playerHp == 0;
            Snapshot = Snapshot with
            {
                PlayerHp = _playerHp,
                Phase = defeated ? CombatPhase.Defeat : CombatPhase.Fighting
            };
            return new EnemyAttackResult(true, damage, defeated, null, Snapshot)
            {
                TargetUnitId = "yeonsu"
            };
        }
        finally { _gate.Release(); }
    }

    /// <summary>플레이어 패배 후 현재 적 체력을 초기화하고 같은 스테이지에서 재도전합니다.</summary>
    public async Task<GameSnapshot> RestartAfterDefeatAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Snapshot.Phase != CombatPhase.Defeat) return Snapshot;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    value.EnemyHp = GameRules.EnemyMaxHp(value.Stage);
                    value.UpdatedUtc = DateTime.UtcNow;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            _playerHp = GameRules.PlayerMaxHp(progress.AttackLevel, progress.ArmorEquipmentLevel);
            _combatantStage = 0;
            Snapshot = CreateSnapshot(progress, CombatPhase.Fighting);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>해금 정책을 검증한 뒤 사용자별 AUTO ON/OFF 상태를 저장합니다.</summary>
    public async Task<GameSnapshot> SetAutoAttackAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    value.AutoAttackUnlocked = GameRules.CanUnlockAutoAttack(value.HighestClearedStage);
                    value.AutoAttackEnabled = enabled && value.AutoAttackUnlocked;
                    value.UpdatedUtc = DateTime.UtcNow;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>해금된 보조 캐릭터 중 최대 다섯 명을 계정의 전투 편성으로 저장합니다.</summary>
    public async Task<GameSnapshot> SetCompanionsAsync(
        IEnumerable<string> companionIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(companionIds);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var requested = companionIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(PartyRules.MaximumCompanions)
                .ToArray();
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var available = PartyRules.Resolve(
                        value.HighestClearedStage,
                        value.ActiveHeroId,
                        requested,
                        1L,
                        value.OwnedCompanionIds).AvailableCompanions;
                    var availableIds = available.Select(hero => hero.HeroId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    value.SelectedCompanionIds = requested.Where(availableIds.Contains).ToList();
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>현재 사용자의 게임 진행만 Stage 1 신규 상태로 되돌립니다.</summary>
    public async Task<GameSnapshot> ResetProgressForDevelopmentAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    GameProgressResetPolicy.ResetToStageOne(value);
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            _playerHp = GameRules.PlayerMaxHp(1, 0);
            _combatantStage = 0;
            Snapshot = CreateSnapshot(progress, CombatPhase.Intro);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>무료 소환패 한 장을 소비하고 동료·조각·재료·금화 중 서버가 결정한 보상을 저장합니다.</summary>
    public async Task<CompanionSummonResult> SummonCompanionAsync(CancellationToken cancellationToken = default)
    {
        var batch = await SummonCompanionsAsync(1, cancellationToken).ConfigureAwait(false);
        return new CompanionSummonResult(
            batch.Accepted,
            batch.Rewards.FirstOrDefault(),
            batch.RejectionReason,
            batch.State);
    }

    /// <summary>지정한 횟수만큼 소환패를 한 트랜잭션으로 소비하고 모든 보상을 저장합니다.</summary>
    public async Task<CompanionSummonBatchResult> SummonCompanionsAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var rewards = new List<CompanionSummonReward>();
            string? rejectionReason = null;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    if (count is < 1 or > 100)
                    {
                        rejectionReason = "소환 횟수는 1회부터 100회까지 선택할 수 있습니다.";
                        return value;
                    }
                    if (value.HeroSummonTickets < count)
                    {
                        rejectionReason = $"소환패가 부족합니다. {count:N0}장이 필요합니다.";
                        return value;
                    }

                    for (var index = 0; index < count; index++)
                    {
                        var reward = CompanionSummonPolicy.Resolve(
                            value.HeroSummonPity,
                            value.LegendarySummonPity,
                            _rolls.NextRoll(),
                            _rolls.NextRoll(),
                            _rolls.NextRoll(),
                            value.OwnedCompanionIds,
                            value.Stage);
                        rewards.Add(reward);
                        value.HeroSummonTickets--;
                        value.HeroSummonPity = reward.Rarity is CompanionRarity.Epic or CompanionRarity.Legendary
                            ? 0
                            : Math.Min(CompanionSummonPolicy.PityThreshold, value.HeroSummonPity + 1);
                        value.LegendarySummonPity = reward.Rarity == CompanionRarity.Legendary
                            ? 0
                            : Math.Min(CompanionSummonPolicy.LegendaryPityThreshold, value.LegendarySummonPity + 1);
                        ApplyCompanionSummonReward(value, reward);
                    }

                    value.OwnedCompanionIds = value.OwnedCompanionIds
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new CompanionSummonBatchResult(rewards.Count == count, rewards, rejectionReason, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>CodeMaru C를 소비하여 소환패 묶음을 즉시 계정에 지급합니다.</summary>
    public async Task<SummonTicketPurchaseResult> PurchaseSummonTicketPackAsync(
        string packKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packKey);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var purchased = false;
            string? rejectionReason = null;
            SummonTicketPackDefinition? purchasedItem = null;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    if (!CompanionSummonPolicy.TryGetTicketPack(packKey, out var item))
                    {
                        rejectionReason = "존재하지 않는 소환패 상품입니다.";
                        return value;
                    }
                    if (value.PremiumCurrency < item.PriceC)
                    {
                        rejectionReason = $"구매에 CodeMaru C {item.PriceC:N0}가 필요합니다.";
                        return value;
                    }

                    value.PremiumCurrency -= item.PriceC;
                    value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, item.Tickets);
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    purchased = true;
                    purchasedItem = item;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SummonTicketPurchaseResult(purchased, rejectionReason, purchasedItem, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>현재 최고 정복 층의 바로 다음 시련만 서버에서 판정하고 최초 정복 보상을 저장합니다.</summary>
    public async Task<TrialTowerChallengeResult> ChallengeTrialTowerAsync(
        int floor,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var victory = false;
            var chance = 0;
            var roll = -1;
            var consumedTickets = 0;
            var message = string.Empty;
            var requested = TrialTowerRules.Describe(floor);
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    TrialTowerRules.RefreshChallengeTickets(value, now);
                    var nextFloor = TrialTowerRules.NextFloor(value.HighestTrialTowerFloor);
                    if (floor != nextFloor)
                    {
                        requested = TrialTowerRules.Describe(nextFloor);
                        message = $"{nextFloor:N0}층부터 순서대로 정복해야 합니다.";
                        return value;
                    }
                    if (value.TrialTowerChallengeTickets <= 0)
                    {
                        message = "시련 도전권이 부족합니다. 자연 회복을 기다리거나 CodeMaru C로 충전하세요.";
                        return value;
                    }

                    accepted = true;
                    chance = TrialTowerRules.SuccessChance(ResolveTrialTowerPartyPower(value), requested, value.HighestClearedStage);
                    roll = _rolls.NextRoll();
                    victory = roll < chance;
                    if (!victory)
                    {
                        value.TrialTowerChallengeTickets--;
                        value.TrialTowerTicketUpdatedUtc ??= now;
                        consumedTickets = 1;
                        value.UpdatedUtc = now;
                        message = "수문진이 버텨냈습니다. 도전권 1장이 소모되었습니다. 성장하거나 다시 도전해 보세요.";
                        return value;
                    }

                    ApplyTrialTowerVictory(value, requested, now);
                    message = $"{requested.Floor:N0}층 정복! 도전권을 보전하고 영웅 소환패 {requested.SummonTickets:N0}장을 획득했습니다.";
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new TrialTowerChallengeResult(accepted, victory, requested, chance, roll, message, Snapshot)
            {
                ConsumedChallengeTickets = consumedTickets
            };
        }
        finally { _gate.Release(); }
    }

    /// <summary>최대 다섯 일반층을 서버에서 순서대로 판정하고 보스 직전 또는 첫 패배에서 멈춥니다.</summary>
    public async Task<TrialTowerSweepResult> SweepTrialTowerAsync(
        int maximumFloors = 5,
        CancellationToken cancellationToken = default)
    {
        maximumFloors = Math.Clamp(maximumFloors, 1, 5);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var failed = false;
            var stoppedBeforeBoss = false;
            var startFloor = 0;
            var endFloor = 0;
            var clearedFloors = 0;
            var summonTickets = 0;
            long gold = 0;
            long upgradeMaterials = 0;
            var consumedTickets = 0;
            var message = string.Empty;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    TrialTowerRules.RefreshChallengeTickets(value, now);
                    startFloor = TrialTowerRules.NextFloor(value.HighestTrialTowerFloor);
                    endFloor = value.HighestTrialTowerFloor;
                    if (TrialTowerRules.Describe(startFloor).IsBoss)
                    {
                        stoppedBeforeBoss = true;
                        message = $"{startFloor:N0}층은 수문장 층입니다. 직접 교전으로 돌파해야 합니다.";
                        return value;
                    }
                    if (value.TrialTowerChallengeTickets <= 0)
                    {
                        message = "시련 도전권이 부족합니다. 자연 회복을 기다리거나 CodeMaru C로 충전하세요.";
                        return value;
                    }

                    accepted = true;
                    var partyPower = ResolveTrialTowerPartyPower(value);
                    for (var index = 0; index < maximumFloors; index++)
                    {
                        if (value.TrialTowerChallengeTickets <= 0) break;
                        var floor = TrialTowerRules.Describe(TrialTowerRules.NextFloor(value.HighestTrialTowerFloor));
                        if (floor.IsBoss)
                        {
                            stoppedBeforeBoss = true;
                            break;
                        }

                        var chance = TrialTowerRules.SuccessChance(partyPower, floor, value.HighestClearedStage);
                        if (_rolls.NextRoll() >= chance)
                        {
                            value.TrialTowerChallengeTickets--;
                            value.TrialTowerTicketUpdatedUtc ??= now;
                            consumedTickets++;
                            failed = true;
                            break;
                        }

                        ApplyTrialTowerVictory(value, floor, now);
                        clearedFloors++;
                        endFloor = floor.Floor;
                        summonTickets = (int)Math.Min(int.MaxValue, (long)summonTickets + floor.SummonTickets);
                        gold = GameRules.SaturatingAdd(gold, floor.Gold);
                        upgradeMaterials = GameRules.SaturatingAdd(upgradeMaterials, floor.UpgradeMaterials);
                    }

                    message = clearedFloors switch
                    {
                        0 when failed => $"{startFloor:N0}층 자동 원정이 패배했습니다. 성장 후 다시 시도하세요.",
                        > 0 when stoppedBeforeBoss => $"{startFloor:N0}~{endFloor:N0}층을 정복하고 다음 수문장 앞에서 멈췄습니다.",
                        > 0 when failed => $"{startFloor:N0}~{endFloor:N0}층을 정복한 뒤 다음 층에서 패배했습니다.",
                        > 0 => $"{startFloor:N0}~{endFloor:N0}층 연속 정복 완료! 소환패 {summonTickets:N0}장을 획득했습니다.",
                        _ => "자동 원정에서 정복한 층이 없습니다."
                    };
                    if (clearedFloors > 0)
                        message += $" · 성공 {clearedFloors:N0}층 도전권 보전";
                    if (consumedTickets > 0)
                    {
                        value.UpdatedUtc = now;
                        message += $" · 실패 도전권 {consumedTickets:N0}장 소모";
                    }
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new TrialTowerSweepResult(
                accepted,
                startFloor,
                endFloor,
                clearedFloors,
                failed,
                stoppedBeforeBoss,
                summonTickets,
                gold,
                upgradeMaterials,
                message,
                Snapshot)
            {
                ConsumedChallengeTickets = consumedTickets
            };
        }
        finally { _gate.Release(); }
    }

    /// <summary>서버 시각을 기준으로 자연 회복된 시련의 탑 도전권을 계정에 반영합니다.</summary>
    public async Task<GameSnapshot> RefreshTrialTowerTicketsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    var before = value.TrialTowerChallengeTickets;
                    TrialTowerRules.RefreshChallengeTickets(value, now);
                    if (before != value.TrialTowerChallengeTickets) value.UpdatedUtc = now;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>CodeMaru C를 소비하여 시련의 탑 도전권을 즉시 충전합니다.</summary>
    public async Task<TrialTowerTicketPurchaseResult> PurchaseTrialTowerChallengeTicketsAsync(
        int count = 1,
        CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 5);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var purchased = false;
            var spent = 0L;
            var message = string.Empty;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    TrialTowerRules.RefreshChallengeTickets(value, now);
                    if (value.TrialTowerChallengeTickets + count > TrialTowerRules.MaximumStoredChallengeTickets)
                    {
                        message = $"도전권은 최대 {TrialTowerRules.MaximumStoredChallengeTickets:N0}장까지 보유할 수 있습니다.";
                        return value;
                    }

                    spent = (long)TrialTowerRules.ChallengeTicketPurchaseCost * count;
                    if (value.PremiumCurrency < spent)
                    {
                        message = $"도전권 충전에 CodeMaru C {spent:N0}가 필요합니다.";
                        spent = 0;
                        return value;
                    }

                    value.PremiumCurrency -= spent;
                    value.TrialTowerChallengeTickets += count;
                    value.UpdatedUtc = now;
                    purchased = true;
                    message = $"시련 도전권 {count:N0}장을 충전했습니다.";
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new TrialTowerTicketPurchaseResult(purchased, purchased ? count : 0, spent, message, Snapshot);
        }
        finally { _gate.Release(); }
    }

    private static long ResolveTrialTowerPartyPower(GameProgress value)
    {
        var inheritedPower = GameRules.AttackPower(
            value.AttackLevel,
            value.SwordArtLevel,
            value.WeaponEquipmentLevel);
        var party = ApplyJadeInheritance(PartyRules.Resolve(
            value.HighestClearedStage,
            value.ActiveHeroId,
            value.SelectedCompanionIds,
            inheritedPower,
            value.OwnedCompanionIds,
            value), value.JadeEquipmentLevel);
        return party.PartyAttackPower;
    }

    private static void ApplyTrialTowerVictory(GameProgress value, TrialTowerFloorDefinition floor, DateTime updatedUtc)
    {
        value.HighestTrialTowerFloor = floor.Floor;
        value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, floor.SummonTickets);
        value.Gold = GameRules.SaturatingAdd(value.Gold, floor.Gold);
        value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(
            value.CompanionUpgradeMaterials,
            floor.UpgradeMaterials);
        value.UpdatedUtc = updatedUtc;
    }

    /// <summary>동일 서버의 실제 계정을 우선하고 부족한 자리만 NPC로 보충해 결투장 매칭을 구성합니다.</summary>
    public Task<ArenaLobbyView> LoadArenaAsync(CancellationToken cancellationToken = default) =>
        LoadArenaCoreAsync(false, cancellationToken);

    public Task<ArenaLobbyView> RefreshArenaOpponentsAsync(CancellationToken cancellationToken = default) =>
        LoadArenaCoreAsync(true, cancellationToken);

    public async Task<int> SetArenaReplaySpeedAsync(int speed, CancellationToken cancellationToken = default)
    {
        if (speed is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(speed));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _store.MutateAsync(RequireUserId(), value =>
            {
                value.Arena.ReplaySpeed = speed;
                value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                return speed;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private async Task<ArenaLobbyView> LoadArenaCoreAsync(bool refresh, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var userId = RequireUserId();
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var current = await _store.MutateAsync(userId, value =>
            {
                ArenaRules.EnsureCurrentDay(value.Arena, now);
                if (refresh)
                {
                    value.Arena.MatchmakingRevision = value.Arena.MatchmakingRevision == int.MaxValue ? 0 : value.Arena.MatchmakingRevision + 1;
                    value.UpdatedUtc = now;
                }
                return value;
            }, cancellationToken).ConfigureAwait(false);
            var roster = await LoadArenaRosterAsync(userId, cancellationToken).ConfigureAwait(false);
            return BuildArenaLobby(current, roster);
        }
        finally { _gate.Release(); }
    }

    /// <summary>저장된 상대 편성을 대상으로 서버에서 승패를 판정하고 양쪽 승점을 갱신합니다.</summary>
    public async Task<ArenaBattleResult> ChallengeArenaAsync(
        string opponentUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opponentUserId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var userId = RequireUserId();
            if (string.Equals(userId, opponentUserId, StringComparison.Ordinal))
                return new(false, false, 0, -1, 0, 0, "자기 원정대에는 도전할 수 없습니다.", Snapshot);

            var roster = await LoadArenaRosterAsync(userId, cancellationToken).ConfigureAwait(false);
            var attacker = roster.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.Ordinal));
            if (attacker is null)
                return new(false, false, 0, -1, 0, 0, "현재 원정대 정보를 찾지 못했습니다.", Snapshot);

            var defender = roster.FirstOrDefault(item => string.Equals(item.UserId, opponentUserId, StringComparison.Ordinal));
            var bot = defender is null
                ? CreateArenaBots(attacker).FirstOrDefault(item => string.Equals(item.UserId, opponentUserId, StringComparison.Ordinal))
                : null;
            if (defender is null && bot is null)
                return new(false, false, 0, -1, 0, 0, "상대 명부가 변경되었거나 같은 서버에서 상대를 찾지 못했습니다. 갱신된 명부에서 다시 선택해 주세요.", Snapshot);

            var defenderPower = defender is not null ? ResolveTrialTowerPartyPower(defender) : bot!.PartyPower;
            var defenderRating = defender?.Arena.Rating ?? bot!.Rating;
            var chance = ArenaRules.WinChance(ResolveTrialTowerPartyPower(attacker), defenderPower);
            var roll = _rolls.NextRoll();
            var victory = roll < chance;
            var ratingChange = victory
                ? ArenaRules.RatingChange(attacker.Arena.Rating, defenderRating)
                : ArenaRules.RatingChange(defenderRating, attacker.Arena.Rating);
            var attackerDelta = victory ? ratingChange.WinnerGain : -ratingChange.LoserLoss;
            var defenderDelta = -attackerDelta;
            var reward = victory ? ArenaRules.VictoryGold(attacker.HighestClearedStage) : 0L;
            var accepted = false;
            var message = string.Empty;
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var progress = await _store.MutateAsync(userId, value =>
            {
                ArenaRules.EnsureCurrentDay(value.Arena, now);
                if (value.Arena.DailyChallenges >= ArenaRules.DailyChallengeLimit && value.Arena.PurchasedTickets == 0)
                {
                    message = $"오늘의 무료 결투 {ArenaRules.DailyChallengeLimit:N0}회를 모두 사용했습니다. CodeMaru C로 결투권을 구매할 수 있습니다.";
                    return value;
                }

                accepted = true;
                if (value.Arena.DailyChallenges < ArenaRules.DailyChallengeLimit)
                    value.Arena.DailyChallenges++;
                else
                    value.Arena.PurchasedTickets--;
                value.Arena.Rating = Math.Max(ArenaRules.MinimumRating, value.Arena.Rating + attackerDelta);
                if (victory)
                {
                    value.Arena.AttackWins++;
                    value.Gold = GameRules.SaturatingAdd(value.Gold, reward);
                    message = $"승리! 승점 +{attackerDelta:N0}, 금화 {reward:N0}을 획득했습니다.";
                }
                else
                {
                    value.Arena.AttackLosses++;
                    message = $"패배했습니다. 승점 {attackerDelta:N0}. 편성과 장비를 정비해 다시 도전하세요.";
                }
                value.UpdatedUtc = now;
                return value;
            }, cancellationToken).ConfigureAwait(false);

            if (accepted && defender is not null)
            {
                await _store.MutateAsync(opponentUserId, value =>
                {
                    value.Arena.Rating = Math.Max(ArenaRules.MinimumRating, value.Arena.Rating + defenderDelta);
                    if (victory) value.Arena.DefenseLosses++;
                    else value.Arena.DefenseWins++;
                    value.UpdatedUtc = now;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            }

            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new(accepted, accepted && victory, chance, roll, accepted ? attackerDelta : 0,
                accepted && victory ? reward : 0, accepted ? $"{message} (판정 승률 {chance}% · 패배 확률 {100 - chance}%)" : message, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>코인 차감과 결투권 지급을 같은 저장소 변경에서 원자적으로 처리합니다.</summary>
    public async Task<ArenaTicketPurchaseResult> PurchaseArenaTicketsAsync(int count, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (count is not (1 or 5 or 10))
                return new(false, 0, 0, "결투권은 1·5·10장 단위로 구매할 수 있습니다.", Snapshot);

            var purchased = false;
            var cost = (long)count * ArenaRules.TicketPriceC;
            var message = string.Empty;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                ArenaRules.EnsureCurrentDay(value.Arena, now);
                if (value.Arena.PurchasedTickets > ArenaRules.MaximumPurchasedTickets - count)
                {
                    message = $"구매 결투권은 최대 {ArenaRules.MaximumPurchasedTickets:N0}장까지 보유할 수 있습니다.";
                    return value;
                }
                if (value.PremiumCurrency < cost)
                {
                    message = $"CodeMaru C가 부족합니다. {count}장 구매에 {cost:N0} C가 필요합니다.";
                    return value;
                }
                value.PremiumCurrency -= cost;
                value.Arena.PurchasedTickets += count;
                value.UpdatedUtc = now;
                purchased = true;
                message = $"결투권 {count}장을 구매했습니다. · {cost:N0} C 사용";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new(purchased, purchased ? count : 0, purchased ? cost : 0, message, Snapshot);
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<GameProgress>> LoadArenaRosterAsync(
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var players = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        if (_settingsStore is null) return players;

        var currentSettings = await _settingsStore.FindAsync(currentUserId, cancellationToken).ConfigureAwait(false);
        if (currentSettings?.General.ChannelConfirmed != true) return players.Where(item => item.UserId == currentUserId).ToArray();

        var sameServer = new List<GameProgress>();
        foreach (var player in players)
        {
            var settings = await _settingsStore.FindAsync(player.UserId, cancellationToken).ConfigureAwait(false);
            if (settings?.General.ChannelConfirmed == true
                && string.Equals(settings.General.ChannelKey, currentSettings.General.ChannelKey, StringComparison.OrdinalIgnoreCase))
                sameServer.Add(player);
        }
        return sameServer;
    }

    private static ArenaLobbyView BuildArenaLobby(GameProgress current, IReadOnlyList<GameProgress> roster)
    {
        var ranked = roster
            .OrderByDescending(item => item.Arena.Rating)
            .ThenByDescending(item => item.Arena.AttackWins)
            .ThenByDescending(ResolveTrialTowerPartyPower)
            .ThenBy(item => item.UserId, StringComparer.Ordinal)
            .ToArray();
        var views = ranked.Select((item, index) => ToArenaOpponent(item, index + 1)).ToArray();
        var ownIndex = Array.FindIndex(ranked, item => string.Equals(item.UserId, current.UserId, StringComparison.Ordinal));
        var candidates = ranked
            .Where(item => !string.Equals(item.UserId, current.UserId, StringComparison.Ordinal))
            .OrderBy(item => Math.Abs(item.Arena.Rating - current.Arena.Rating))
            .ThenBy(item => PowerDistance(ResolveTrialTowerPartyPower(item), ResolveTrialTowerPartyPower(current)))
            .ToArray();
        var start = candidates.Length == 0 ? 0 : current.Arena.MatchmakingRevision % candidates.Length;
        var opponents = candidates.Skip(start).Concat(candidates.Take(start)).Take(5)
            .Select(item => views[Array.IndexOf(ranked, item)])
            .ToList();
        if (opponents.Count < 5)
            opponents.AddRange(CreateArenaBots(current).Take(5 - opponents.Count));
        var ownPower = ResolveTrialTowerPartyPower(current);
        opponents = opponents.Select(item => item with { EstimatedWinChance = ArenaRules.WinChance(ownPower, item.PartyPower) }).ToList();

        return new(
            current.Arena.Rating,
            ArenaRules.Tier(current.Arena.Rating),
            ownIndex < 0 ? 1 : ownIndex + 1,
            current.Arena.AttackWins,
            current.Arena.AttackLosses,
            current.Arena.DefenseWins,
            current.Arena.DefenseLosses,
            current.Arena.DailyChallenges,
            ArenaRules.DailyChallengeLimit,
            opponents,
            views.Take(20).ToArray())
        {
            PurchasedTickets = current.Arena.PurchasedTickets,
            PremiumCurrency = current.PremiumCurrency,
            ReplaySpeed = current.Arena.ReplaySpeed,
            MatchmakingRevision = current.Arena.MatchmakingRevision
        };
    }

    private static ArenaOpponentView ToArenaOpponent(GameProgress progress, int rank)
    {
        var assets = progress.SelectedCompanionIds
            .Select(id => PartyRules.AllCompanions.FirstOrDefault(hero => string.Equals(hero.HeroId, id, StringComparison.OrdinalIgnoreCase))?.SpriteAssetUrl)
            .Where(asset => !string.IsNullOrWhiteSpace(asset))
            .Cast<string>()
            .Take(5)
            .ToArray();
        var nickname = GameNicknameRules.Normalize(progress.GameNickname);
        return new(rank, progress.UserId, string.IsNullOrWhiteSpace(nickname) ? "닉네임 미설정" : nickname,
            ResolveTrialTowerPartyPower(progress), progress.Arena.Rating, ArenaRules.Tier(progress.Arena.Rating), assets);
    }

    private static IReadOnlyList<ArenaOpponentView> CreateArenaBots(GameProgress current)
    {
        var currentPower = ResolveTrialTowerPartyPower(current);
        var currentRating = current.Arena.Rating;
        var companions = PartyRules.AllCompanions.ToArray();
        var definitions = new (string Id, string Name, int RatingOffset, decimal PowerScale, int AssetOffset)[]
        {
            ("bot:wandering-sword", "무명 검객단", -180, 0.72m, 0),
            ("bot:jade-gate", "옥문 수호대", -80, 0.86m, 5),
            ("bot:moon-shadow", "월영 자객단", 20, 1.00m, 10),
            ("bot:red-lotus", "홍련 무사단", 100, 1.18m, 15),
            ("bot:heaven-seal", "천명 봉인대", 220, 1.40m, 20),
            ("bot:bamboo-rangers", "청죽 유랑단", -210, 0.50m, 3),
            ("bot:silver-stream", "은하 검무대", -100, 0.80m, 8),
            ("bot:cloud-riders", "운해 유격대", 30, 1.10m, 13),
            ("bot:black-tortoise", "현무 철위대", 150, 1.80m, 18),
            ("bot:crimson-emperor", "적월 천왕대", 300, 4.00m, 23),
            ("bot:plum-blossom", "매화 수련단", -190, 0.60m, 1),
            ("bot:azure-wind", "창풍 순찰대", -60, 0.90m, 6),
            ("bot:frost-blades", "빙월 검위대", 80, 1.20m, 11),
            ("bot:dragon-guard", "용문 호법대", 200, 2.20m, 16),
            ("bot:celestial-throne", "천궁 절대자", 360, 5.00m, 21)
        };

        return definitions.Skip(current.Arena.MatchmakingRevision % 3 * 5).Take(5).Select(definition =>
        {
            var rating = Math.Max(ArenaRules.MinimumRating, currentRating + definition.RatingOffset);
            var scaled = Math.Min(long.MaxValue, Math.Max(1m, currentPower * definition.PowerScale));
            var assets = Enumerable.Range(0, 5)
                .Select(assetIndex => companions[(definition.AssetOffset + assetIndex) % companions.Length].SpriteAssetUrl)
                .ToArray();
            return new ArenaOpponentView(0, definition.Id, definition.Name, decimal.ToInt64(scaled), rating,
                ArenaRules.Tier(rating), assets, true);
        }).ToArray();
    }

    private static double PowerDistance(long left, long right) =>
        Math.Abs(Math.Log10(Math.Max(1d, left)) - Math.Log10(Math.Max(1d, right)));

    private static void ApplyCompanionSummonReward(GameProgress value, CompanionSummonReward reward)
    {
        if (reward.Kind == CompanionSummonRewardKind.Companion && reward.HeroId is not null)
        {
            value.OwnedCompanionIds.Add(reward.HeroId);
        }
        else if (reward.Kind == CompanionSummonRewardKind.CompanionShard && reward.HeroId is not null)
        {
            value.CompanionShards.TryGetValue(reward.HeroId, out var current);
            value.CompanionShards[reward.HeroId] = (int)Math.Min(int.MaxValue, current + reward.Quantity);
        }
        else if (reward.Kind == CompanionSummonRewardKind.UpgradeMaterial)
        {
            value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(value.CompanionUpgradeMaterials, reward.Quantity);
        }
        else if (reward.Kind == CompanionSummonRewardKind.Gold)
        {
            value.Gold = GameRules.SaturatingAdd(value.Gold, reward.Quantity);
        }
    }

    /// <summary>금화를 소비하여 선택한 동료의 독립 레벨을 한 단계 올립니다.</summary>
    public Task<CompanionActionResult> UpgradeCompanionLevelAsync(
        string heroId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var growth = CompanionProgressionRules.Resolve(value, hero);
            if (growth.Level >= int.MaxValue)
                return (false, "더 이상 저장할 수 없는 시스템 최대 레벨입니다.");
            if (value.Gold < growth.LevelUpCost)
                return (false, "레벨업에 필요한 금화가 부족합니다.");
            value.Gold -= growth.LevelUpCost;
            value.CompanionLevels[hero.HeroId] = growth.Level + 1;
            return (true, $"{hero.Name}이(가) Lv.{growth.Level + 1}로 성장했습니다.");
        }, cancellationToken);

    /// <summary>전용 조각을 소비하여 선택한 동료의 등급을 올립니다.</summary>
    public Task<CompanionActionResult> RankUpCompanionAsync(
        string heroId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var growth = CompanionProgressionRules.Resolve(value, hero);
            if (!CompanionProgressionRules.CanRankUp(growth.Rank))
                return (false, "현재 최고 등급입니다.");
            if (growth.Shards < growth.RankUpShardCost)
                return (false, "승급에 필요한 동료 조각이 부족합니다.");
            value.CompanionShards[hero.HeroId] = growth.Shards - growth.RankUpShardCost;
            value.CompanionRanks[hero.HeroId] = growth.Rank + 1;
            return (true, $"{hero.Name}이(가) {growth.Rank + 1}성으로 승급했습니다.");
        }, cancellationToken);

    /// <summary>장비 가방의 개별 장비를 동료에게 착용하거나 해제합니다. 구형 정의 ID도 지원합니다.</summary>
    public Task<CompanionActionResult> ToggleCompanionEquipmentAsync(
        string heroId,
        string equipmentId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var inventory = CompanionProgressionRules.ResolveInventory(value);
            var equipment = inventory.FirstOrDefault(candidate =>
                                string.Equals(candidate.Instance.InstanceId, equipmentId, StringComparison.OrdinalIgnoreCase))
                            ?? inventory.FirstOrDefault(candidate =>
                                string.Equals(candidate.Definition.ItemId, equipmentId, StringComparison.OrdinalIgnoreCase));
            if (equipment is null) return (false, "장비 가방에 해당 장비가 없습니다.");
            if (!CompanionProgressionRules.CanEquip(hero, equipment.Definition))
                return (false, $"{hero.Name}은(는) {WeaponTypeName(hero.WeaponType)} 계열만 착용할 수 있습니다.");
            var key = CompanionProgressionRules.EquipmentKey(hero.HeroId, equipment.Definition.Slot);
            if (value.CompanionEquippedItems.TryGetValue(key, out var equipped)
                && string.Equals(equipped, equipment.Instance.InstanceId, StringComparison.OrdinalIgnoreCase))
            {
                value.CompanionEquippedItems.Remove(key);
                return (true, $"{hero.Name}의 {equipment.DisplayName}을(를) 해제했습니다.");
            }
            foreach (var other in value.CompanionEquippedItems
                         .Where(item => string.Equals(item.Value, equipment.Instance.InstanceId, StringComparison.OrdinalIgnoreCase))
                         .Select(item => item.Key)
                         .ToArray())
                value.CompanionEquippedItems.Remove(other);
            value.CompanionEquippedItems[key] = equipment.Instance.InstanceId;
            return (true, $"{hero.Name}에게 {equipment.DisplayName} +{equipment.Instance.Level}을(를) 착용했습니다.");
        }, cancellationToken);

    /// <summary>강화 광석을 소비해 장비 가방의 개별 장비 한 개를 강화합니다.</summary>
    public Task<CompanionActionResult> UpgradeCompanionEquipmentAsync(
        string heroId,
        string instanceId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var index = value.CompanionEquipmentInventory.FindIndex(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return (false, "강화할 장비를 찾을 수 없습니다.");
            var view = CompanionProgressionRules.ResolveInventory(value).First(item =>
                string.Equals(item.Instance.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            if (view.Instance.Level >= CompanionProgressionRules.MaximumEquipmentLevel)
                return (false, "이미 최고 강화 단계입니다.");
            if (value.CompanionUpgradeMaterials < view.UpgradeMaterialCost)
                return (false, "개인 장비 강화 광석이 부족합니다.");
            value.CompanionUpgradeMaterials -= view.UpgradeMaterialCost;
            value.CompanionEquipmentInventory[index] = view.Instance with { Level = view.Instance.Level + 1 };
            DailyMissionPolicy.EnsureCurrentDay(value, _timeProvider.GetUtcNow().UtcDateTime);
            value.DailyEquipmentUpgradeCount = value.DailyEquipmentUpgradeCount == int.MaxValue
                ? int.MaxValue
                : value.DailyEquipmentUpgradeCount + 1;
            return (true, $"{view.DisplayName}을(를) +{view.Instance.Level + 1}로 강화했습니다.");
        }, cancellationToken);

    /// <summary>미착용 장비를 판매해 금화로 전환합니다.</summary>
    public Task<CompanionActionResult> SellCompanionEquipmentAsync(
        string heroId,
        string instanceId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var view = CompanionProgressionRules.ResolveInventory(value).FirstOrDefault(item =>
                string.Equals(item.Instance.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            if (view is null) return (false, "판매할 장비를 찾을 수 없습니다.");
            if (value.CompanionEquippedItems.Values.Contains(instanceId, StringComparer.OrdinalIgnoreCase))
                return (false, "착용 중인 장비는 해제한 뒤 판매할 수 있습니다.");
            value.CompanionEquipmentInventory.RemoveAll(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
            value.Gold = GameRules.SaturatingAdd(value.Gold, view.SellGold);
            return (true, $"{view.DisplayName}을(를) 판매해 {view.SellGold:N0} 금화를 획득했습니다.");
        }, cancellationToken);

    /// <summary>강화 광석으로 지정한 종류의 새 개인 장비를 제작해 가방에 넣습니다.</summary>
    public Task<CompanionActionResult> CraftCompanionEquipmentAsync(
        string heroId,
        string itemId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var definition = CompanionProgressionRules.GetEquipmentCatalog().FirstOrDefault(item =>
                string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase));
            if (definition is null) return (false, "제작할 장비 종류를 찾을 수 없습니다.");
            if (value.CompanionEquipmentInventory.Count >= CompanionProgressionRules.EquipmentInventoryCapacity)
                return (false, "장비 가방이 가득 찼습니다. 불필요한 장비를 판매해 주세요.");
            if (value.CompanionUpgradeMaterials < CompanionProgressionRules.EquipmentCraftMaterialCost)
                return (false, "장비 제작에 필요한 강화 광석이 부족합니다.");
            value.CompanionUpgradeMaterials -= CompanionProgressionRules.EquipmentCraftMaterialCost;
            var variantIndex = value.CompanionEquipmentInventory.Count;
            var created = CompanionProgressionRules.CreateEquipmentInstance(
                Guid.NewGuid().ToString("N"), definition.ItemId, 0, variantIndex);
            value.CompanionEquipmentInventory.Add(created);
            return (true, $"{created.DisplayName} +0 장비를 제작해 가방에 넣었습니다.");
        }, cancellationToken);

    /// <summary>CodeMaru C로 스킨을 구매하거나 보유 스킨을 선택합니다.</summary>
    public Task<CompanionActionResult> PurchaseOrSelectCompanionSkinAsync(
        string heroId,
        string skinId,
        CancellationToken cancellationToken = default) =>
        MutateCompanionAsync(heroId, (value, hero) =>
        {
            var skin = CompanionProgressionRules.GetSkins(hero).FirstOrDefault(candidate =>
                string.Equals(candidate.SkinId, skinId, StringComparison.OrdinalIgnoreCase));
            if (skin is null) return (false, "존재하지 않는 스킨입니다.");
            var ownershipKey = CompanionProgressionRules.SkinKey(hero.HeroId, skin.SkinId);
            var owned = skin.PriceC == 0 || value.OwnedCompanionSkins.Contains(ownershipKey, StringComparer.OrdinalIgnoreCase);
            if (!owned)
            {
                if (value.PremiumCurrency < skin.PriceC)
                    return (false, $"스킨 구매에 CodeMaru C {skin.PriceC:N0}가 필요합니다.");
                value.PremiumCurrency -= skin.PriceC;
                value.OwnedCompanionSkins.Add(ownershipKey);
            }
            value.ActiveCompanionSkins[hero.HeroId] = skin.SkinId;
            return (true, $"{hero.Name}의 외형을 {skin.Name}(으)로 변경했습니다.");
        }, cancellationToken);

    /// <summary>로컬 개발 환경에서 유료 재화 흐름을 검증할 수 있도록 CodeMaru C를 지급합니다.</summary>
    public async Task<GameSnapshot> GrantPremiumCurrencyForDevelopmentAsync(
        long amount,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    value.PremiumCurrency = GameRules.SaturatingAdd(value.PremiumCurrency, Math.Clamp(amount, 0, 10_000));
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>CodeMaru C로 시간제 AUTO 가속 부적 하나를 구매해 공용 보관함에 넣습니다.</summary>
    public async Task<AutoSpeedBoostPurchaseResult> PurchaseAutoSpeedBoostAsync(
        string itemKey,
        CancellationToken cancellationToken = default) =>
        await PurchaseAutoSpeedBoostAsync(itemKey, 1, cancellationToken).ConfigureAwait(false);

    /// <summary>CodeMaru C로 시간제 AUTO 가속 부적을 지정 수량만큼 한 번에 구매합니다.</summary>
    public async Task<AutoSpeedBoostPurchaseResult> PurchaseAutoSpeedBoostAsync(
        string itemKey,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var purchased = false;
            string? rejectionReason = null;
            AutoSpeedBoostDefinition? purchasedItem = null;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    if (!GameRules.TryGetAutoSpeedBoost(itemKey, out var item))
                    {
                        rejectionReason = "존재하지 않는 가속 부적입니다.";
                        return value;
                    }
                    value.AutoBoostInventory.TryGetValue(item.ItemKey, out var count);
                    if (quantity > int.MaxValue - count)
                    {
                        rejectionReason = "부적 보유 한도를 초과합니다.";
                        return value;
                    }
                    if (item.PriceC > long.MaxValue / quantity)
                    {
                        rejectionReason = "구매 금액이 허용 범위를 초과합니다.";
                        return value;
                    }
                    var totalPrice = item.PriceC * quantity;
                    if (value.PremiumCurrency < totalPrice)
                    {
                        rejectionReason = $"{quantity:N0}개 구매에 CodeMaru C {totalPrice:N0}가 필요합니다.";
                        return value;
                    }

                    value.PremiumCurrency -= totalPrice;
                    value.AutoBoostInventory[item.ItemKey] = count + quantity;
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    purchased = true;
                    purchasedItem = item;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new AutoSpeedBoostPurchaseResult(purchased, rejectionReason, purchasedItem, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>CodeMaru C를 소비해 선택한 금화 묶음을 계정 금고에 지급합니다.</summary>
    public async Task<GoldPackPurchaseResult> PurchaseGoldPackAsync(
        string packKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packKey);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var purchased = false;
            string? rejectionReason = null;
            GoldPackDefinition? purchasedItem = null;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    if (!ExpeditionEconomy.TryGetGoldPack(packKey, out var item))
                    {
                        rejectionReason = "존재하지 않는 금화 상품입니다.";
                        return value;
                    }
                    if (value.PremiumCurrency < item.PriceC)
                    {
                        rejectionReason = $"구매에 CodeMaru C {item.PriceC:N0}가 필요합니다.";
                        return value;
                    }
                    value.PremiumCurrency -= item.PriceC;
                    value.Gold = GameRules.SaturatingAdd(value.Gold, item.Gold);
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    purchased = true;
                    purchasedItem = item;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new GoldPackPurchaseResult(purchased, rejectionReason, purchasedItem, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>완료한 오늘의 원정 임무 보상을 한 번만 수령합니다.</summary>
    public async Task<DailyMissionClaimResult> ClaimDailyMissionAsync(
        string missionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(missionKey);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "임무 보상을 수령할 수 없습니다.";
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    accepted = DailyMissionPolicy.TryClaim(value, missionKey, now, out message);
                    if (accepted) value.UpdatedUtc = now;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new DailyMissionClaimResult(accepted, message, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>보유한 시간제 AUTO 가속 부적 하나를 소비해 효과를 활성화합니다.</summary>
    public async Task<AutoSpeedBoostActivationResult> ActivateAutoSpeedBoostAsync(
        string itemKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var activated = false;
            string? rejectionReason = null;
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    NormalizeActiveBoost(value, now);
                    if (!GameRules.TryGetAutoSpeedBoost(itemKey, out var item)
                        || !value.AutoBoostInventory.TryGetValue(itemKey, out var count)
                        || count <= 0)
                    {
                        rejectionReason = "보유하지 않은 가속 부적입니다.";
                        return value;
                    }
                    if (value.ActiveAutoSpeedMultiplier > 1 && value.AutoSpeedBoostEndsUtc > now)
                    {
                        rejectionReason = "현재 가속 효과가 끝난 뒤 다음 부적을 사용할 수 있습니다.";
                        return value;
                    }

                    if (count == 1) value.AutoBoostInventory.Remove(itemKey);
                    else value.AutoBoostInventory[itemKey] = count - 1;
                    value.ActiveAutoSpeedMultiplier = item.Multiplier;
                    value.AutoSpeedBoostEndsUtc = now + item.Duration;
                    value.AutoSpeedBoostRemainingMilliseconds = 0;
                    value.UpdatedUtc = now;
                    activated = true;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new AutoSpeedBoostActivationResult(activated, rejectionReason, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>현재 AUTO 가속 효과를 즉시 해제합니다. 이미 소비한 부적은 반환하지 않습니다.</summary>
    public async Task<GameSnapshot> DeactivateAutoSpeedBoostAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    value.ActiveAutoSpeedMultiplier = 1;
                    value.AutoSpeedBoostEndsUtc = null;
                    value.AutoSpeedBoostRemainingMilliseconds = 0;
                    value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>화면이 숨겨지거나 게임이 종료될 때 가속 부적의 남은 시간을 고정해 저장합니다.</summary>
    public async Task<GameSnapshot> PauseAutoSpeedBoostAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    NormalizeActiveBoost(value, now);
                    if (value.ActiveAutoSpeedMultiplier > 1 && value.AutoSpeedBoostEndsUtc is not null)
                    {
                        value.AutoSpeedBoostRemainingMilliseconds = Math.Max(
                            1L,
                            (long)Math.Ceiling((value.AutoSpeedBoostEndsUtc.Value - now).TotalMilliseconds));
                        value.AutoSpeedBoostEndsUtc = null;
                        value.UpdatedUtc = now;
                    }
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>게임 화면이 다시 보이면 저장한 남은 시간부터 가속 부적을 재개합니다.</summary>
    public async Task<GameSnapshot> ResumeAutoSpeedBoostAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    ResumeActiveBoost(value, now);
                    value.UpdatedUtc = now;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>스테이지와 분리된 무한 시련 런을 서버 시각으로 시작합니다.</summary>
    public async Task<EndlessTrialStart> BeginEndlessTrialAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _endlessTrialRunId = Guid.NewGuid().ToString("N");
            _endlessTrialStartedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return new EndlessTrialStart(_endlessTrialRunId, _endlessTrialStartedUtc);
        }
        finally { _gate.Release(); }
    }

    /// <summary>클라이언트 결과를 서버 경과 시간으로 제한하고 무한 시련 보상을 계정에 반영합니다.</summary>
    public async Task<EndlessTrialResult> CompleteEndlessTrialAsync(
        string runId,
        int reportedSurvivalSeconds,
        int reportedKills,
        int reportedGoldPickups,
        int reportedMaterialPickups,
        int reportedTicketPickups,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (!string.Equals(runId, _endlessTrialRunId, StringComparison.Ordinal)
                || _endlessTrialStartedUtc == default)
                return new EndlessTrialResult(false, 0, 0, 0, 0, 0, null, null,
                    "유효한 무한 시련 기록을 찾을 수 없습니다.", Snapshot);

            _endlessTrialRunId = null;
            var serverElapsed = Math.Clamp((int)Math.Floor((now - _endlessTrialStartedUtc).TotalSeconds), 0, 3_600);
            _endlessTrialStartedUtc = default;
            var survivalSeconds = Math.Clamp(reportedSurvivalSeconds, 0, Math.Min(3_600, serverElapsed + 2));
            var kills = Math.Clamp(reportedKills, 0, survivalSeconds * 8 + 8);
            var goldPickups = Math.Clamp(reportedGoldPickups, 0, survivalSeconds / 7 + 1);
            var materialPickups = Math.Clamp(reportedMaterialPickups, 0, survivalSeconds / 15 + 1);
            var ticketPickups = Math.Clamp(reportedTicketPickups, 0, survivalSeconds / 45 + 1);
            var gold = Math.Min(250_000L,
                survivalSeconds * 3L + kills * 4L + goldPickups * (35L + survivalSeconds));
            var materials = Math.Min(2_000L, materialPickups * 3L + survivalSeconds / 30L);
            var tickets = Math.Min(10, ticketPickups);
            string? equipmentInstanceId = null;
            AutoSpeedBoostDefinition? boostDropped = null;

            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    value.Gold = GameRules.SaturatingAdd(value.Gold, gold);
                    value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(value.CompanionUpgradeMaterials, materials);
                    value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, tickets);
                    var rewardLevel = Math.Max(1, survivalSeconds / 3 + kills / 10);
                    if (survivalSeconds >= 45
                        && value.CompanionEquipmentInventory.Count < CompanionProgressionRules.EquipmentInventoryCapacity
                        && (survivalSeconds >= 180 || _rolls.NextRoll() < 45))
                    {
                        var equipment = CompanionProgressionRules.CreateDroppedEquipment(
                            rewardLevel,
                            _rolls.NextRoll(),
                            _rolls.NextRoll(),
                            value.CompanionEquipmentInventory.Count);
                        value.CompanionEquipmentInventory.Add(equipment);
                        equipmentInstanceId = equipment.InstanceId;
                    }
                    if (survivalSeconds >= 90 && (survivalSeconds >= 240 || _rolls.NextRoll() < 30))
                    {
                        boostDropped = GameRules.ResolveAutoSpeedBoostDrop(
                            0,
                            _rolls.NextRoll(),
                            _rolls.NextRoll());
                        if (boostDropped is not null)
                        {
                            value.AutoBoostInventory.TryGetValue(boostDropped.ItemKey, out var count);
                            value.AutoBoostInventory[boostDropped.ItemKey] = count == int.MaxValue ? int.MaxValue : count + 1;
                        }
                    }
                    value.UpdatedUtc = now;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            var equipmentDropped = equipmentInstanceId is null
                ? null
                : Snapshot.CompanionEquipmentInventory.FirstOrDefault(item =>
                    string.Equals(item.Instance.InstanceId, equipmentInstanceId, StringComparison.OrdinalIgnoreCase));
            var message = $"{survivalSeconds}초 생존 · {kills}기 처치 · {gold:N0} 금화";
            if (materials > 0) message += $" · 광석 {materials:N0}";
            if (tickets > 0) message += $" · 소환패 {tickets}";
            return new EndlessTrialResult(true, survivalSeconds, kills, gold, materials, tickets,
                equipmentDropped, boostDropped, message, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>영맥 질주 런을 서버 시각으로 시작합니다.</summary>
    public async Task<ArcadeTrialStart> BeginSpiritVeinRunAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _spiritVeinRunId = Guid.NewGuid().ToString("N");
            _spiritVeinStartedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return new ArcadeTrialStart(_spiritVeinRunId, _spiritVeinStartedUtc);
        }
        finally { _gate.Release(); }
    }

    /// <summary>영맥 질주 기록을 서버 경과 시간으로 제한하고 계정 보상으로 정산합니다.</summary>
    public async Task<ArcadeTrialResult> CompleteSpiritVeinRunAsync(
        string runId,
        int reportedDurationSeconds,
        int reportedDistance,
        int reportedEssence,
        int reportedGoldPickups,
        int reportedMaterialPickups,
        int reportedTicketPickups,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (!string.Equals(runId, _spiritVeinRunId, StringComparison.Ordinal) || _spiritVeinStartedUtc == default)
                return RejectedArcadeTrial("유효한 영맥 질주 기록을 찾을 수 없습니다.");

            _spiritVeinRunId = null;
            var serverElapsed = Math.Clamp((int)Math.Floor((now - _spiritVeinStartedUtc).TotalSeconds), 0, 3_600);
            _spiritVeinStartedUtc = default;
            var duration = Math.Clamp(reportedDurationSeconds, 0, Math.Min(3_600, serverElapsed + 2));
            var distance = Math.Clamp(reportedDistance, 0, duration * 32 + 96);
            var essence = Math.Clamp(reportedEssence, 0, duration / 2 + 4);
            var goldPickups = Math.Clamp(reportedGoldPickups, 0, duration / 5 + 2);
            var materialPickups = Math.Clamp(reportedMaterialPickups, 0, duration / 10 + 2);
            var ticketPickups = Math.Clamp(reportedTicketPickups, 0, duration / 35 + 1);
            var gold = Math.Min(300_000L, duration * 2L + distance * 2L + essence * 8L + goldPickups * (45L + duration));
            var materials = Math.Min(2_500L, materialPickups * 4L + essence / 8L + duration / 35L);
            var tickets = Math.Min(10, ticketPickups);
            var rewards = await AwardArcadeRewardsAsync(duration, distance + essence * 5, gold, materials, tickets, now, cancellationToken).ConfigureAwait(false);
            return new ArcadeTrialResult(true, duration, distance, essence, gold, materials, tickets,
                rewards.Equipment, rewards.Boost,
                $"{distance:N0}m 질주 · 영기 {essence:N0} · {gold:N0} 금화", Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>검진 수호 런을 서버 시각으로 시작합니다.</summary>
    public async Task<ArcadeTrialStart> BeginSwordFormationRunAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _swordFormationRunId = Guid.NewGuid().ToString("N");
            _swordFormationStartedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return new ArcadeTrialStart(_swordFormationRunId, _swordFormationStartedUtc);
        }
        finally { _gate.Release(); }
    }

    /// <summary>검진 수호 기록을 서버 경과 시간으로 제한하고 계정 보상으로 정산합니다.</summary>
    public async Task<ArcadeTrialResult> CompleteSwordFormationRunAsync(
        string runId,
        int reportedDurationSeconds,
        int reportedKills,
        int reportedWaves,
        int reportedGoldPickups,
        int reportedMaterialPickups,
        int reportedTicketPickups,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (!string.Equals(runId, _swordFormationRunId, StringComparison.Ordinal) || _swordFormationStartedUtc == default)
                return RejectedArcadeTrial("유효한 검진 수호 기록을 찾을 수 없습니다.");

            _swordFormationRunId = null;
            var serverElapsed = Math.Clamp((int)Math.Floor((now - _swordFormationStartedUtc).TotalSeconds), 0, 3_600);
            _swordFormationStartedUtc = default;
            var duration = Math.Clamp(reportedDurationSeconds, 0, Math.Min(3_600, serverElapsed + 2));
            var kills = Math.Clamp(reportedKills, 0, duration * 7 + 12);
            var waves = Math.Clamp(reportedWaves, 0, duration / 7 + 2);
            var goldPickups = Math.Clamp(reportedGoldPickups, 0, duration / 8 + 2);
            var materialPickups = Math.Clamp(reportedMaterialPickups, 0, duration / 14 + 2);
            var ticketPickups = Math.Clamp(reportedTicketPickups, 0, duration / 40 + 1);
            var gold = Math.Min(320_000L, duration * 3L + kills * 5L + waves * 32L + goldPickups * (50L + duration));
            var materials = Math.Min(2_500L, materialPickups * 4L + waves * 2L + duration / 40L);
            var tickets = Math.Min(10, ticketPickups);
            var rewards = await AwardArcadeRewardsAsync(duration, kills + waves * 8, gold, materials, tickets, now, cancellationToken).ConfigureAwait(false);
            return new ArcadeTrialResult(true, duration, waves, kills, gold, materials, tickets,
                rewards.Equipment, rewards.Boost,
                $"{waves:N0}파 수호 · {kills:N0}기 격파 · {gold:N0} 금화", Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>영맥 성채 타워 디펜스 런을 서버 시각으로 시작합니다.</summary>
    public async Task<ArcadeTrialStart> BeginSpiritFortressRunAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _spiritFortressRunId = Guid.NewGuid().ToString("N");
            _spiritFortressStartedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            return new ArcadeTrialStart(_spiritFortressRunId, _spiritFortressStartedUtc);
        }
        finally { _gate.Release(); }
    }

    /// <summary>영맥 성채 기록을 서버 경과 시간으로 제한하고 계정 보상으로 정산합니다.</summary>
    public async Task<ArcadeTrialResult> CompleteSpiritFortressRunAsync(
        string runId,
        int reportedDurationSeconds,
        int reportedKills,
        int reportedWaves,
        int reportedTowerLevels,
        int reportedGoldPickups,
        int reportedMaterialPickups,
        int reportedTicketPickups,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (!string.Equals(runId, _spiritFortressRunId, StringComparison.Ordinal) || _spiritFortressStartedUtc == default)
                return RejectedArcadeTrial("유효한 영맥 성채 기록을 찾을 수 없습니다.");

            _spiritFortressRunId = null;
            var serverElapsed = Math.Clamp((int)Math.Floor((now - _spiritFortressStartedUtc).TotalSeconds), 0, 3_600);
            _spiritFortressStartedUtc = default;
            var duration = Math.Clamp(reportedDurationSeconds, 0, Math.Min(3_600, serverElapsed + 2));
            var kills = Math.Clamp(reportedKills, 0, duration * 5 + 10);
            var waves = Math.Clamp(reportedWaves, 0, duration / 10 + 2);
            // Towers keep gaining levels after their fourth visual evolution.  The old hard cap of
            // 32 (eight level-four towers) made every legitimate late-wave build look identical.
            var towerLevels = Math.Clamp(reportedTowerLevels, 0, Math.Min(512, duration / 2 + 24));
            var goldPickups = Math.Clamp(reportedGoldPickups, 0, duration / 8 + 2);
            var materialPickups = Math.Clamp(reportedMaterialPickups, 0, duration / 14 + 2);
            var ticketPickups = Math.Clamp(reportedTicketPickups, 0, duration / 45 + 1);
            var gold = Math.Min(360_000L, duration * 3L + kills * 6L + waves * 45L + towerLevels * 20L + goldPickups * (55L + duration));
            var materials = Math.Min(2_800L, materialPickups * 5L + waves * 2L + towerLevels / 2L + duration / 40L);
            var tickets = Math.Min(10, ticketPickups);
            var rewards = await AwardArcadeRewardsAsync(duration, kills + waves * 10 + towerLevels * 3, gold, materials, tickets, now, cancellationToken).ConfigureAwait(false);
            return new ArcadeTrialResult(true, duration, waves, kills, gold, materials, tickets,
                rewards.Equipment, rewards.Boost,
                $"{waves:N0}파 방어 · {kills:N0}기 격파 · 탑 전력 {towerLevels:N0} · {gold:N0} 금화", Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>선택한 영초를 빈 경작지에 심고 비용과 완료 시간을 서버에서 확정합니다.</summary>
    public async Task<SpiritFarmActionResult> PlantSpiritCropAsync(
        int plotIndex,
        string cropKey,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "심을 수 없는 경작지입니다.";
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.Normalize(value);
                SpiritFarmRules.EnsureDailySupply(value, now);
                var crop = SpiritFarmRules.FindCrop(cropKey);
                if (crop is null) { message = "알 수 없는 영초입니다."; return value; }
                if (plotIndex < 0 || plotIndex >= SpiritFarmRules.UnlockedPlots(value.SpiritFarmLevel))
                { message = "농원을 확장해야 사용할 수 있는 밭입니다."; return value; }
                if (value.SpiritFarmPlots.Any(plot => plot.PlotIndex == plotIndex))
                { message = "이미 영초가 자라고 있습니다."; return value; }
                if (value.SpiritFarmMeta.CropSeeds.GetValueOrDefault(crop.CropKey) <= 0)
                { message = $"{crop.Name} 씨앗이 없습니다. 씨앗 꾸러미를 먼저 열어주세요."; return value; }
                if (value.Gold < crop.PlantCost)
                { message = $"재배에 필요한 금화가 부족합니다. · {crop.PlantCost:N0} 금화"; return value; }

                var soil = SpiritFarmRules.Soil(value, plotIndex);
                var growMultiplier = SpiritFarmRules.HouseGrowMultiplier(value.SpiritFarmMeta.HouseTier)
                                     * SpiritFarmRules.SoilGrowMultiplier(soil.Vitality);
                value.Gold -= crop.PlantCost;
                value.SpiritFarmMeta.CropSeeds[crop.CropKey]--;
                if (value.SpiritFarmMeta.CropSeeds[crop.CropKey] <= 0)
                    value.SpiritFarmMeta.CropSeeds.Remove(crop.CropKey);
                value.SpiritFarmPlots.Add(new SpiritFarmPlotState
                {
                    PlotIndex = plotIndex,
                    CropKey = crop.CropKey,
                    PlantedUtc = now,
                    ReadyUtc = now + TimeSpan.FromSeconds(crop.GrowTime.TotalSeconds * growMultiplier)
                });
                value.UpdatedUtc = now;
                accepted = true;
                message = $"{crop.Name} 재배를 시작했습니다.";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>완전히 자란 영초 한 칸을 수확하고 계정 재화를 지급합니다.</summary>
    public async Task<SpiritFarmActionResult> HarvestSpiritCropAsync(
        int plotIndex,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "수확할 영초가 없습니다.";
            var gold = 0L;
            var materials = 0L;
            var tickets = 0;
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.Normalize(value);
                SpiritFarmRules.EnsureDailySupply(value, now);
                var plot = value.SpiritFarmPlots.FirstOrDefault(item => item.PlotIndex == plotIndex);
                var crop = plot is null ? null : SpiritFarmRules.FindCrop(plot.CropKey);
                if (plot is null || crop is null) return value;
                if (plot.ReadyUtc > now)
                { message = "아직 영초가 완전히 자라지 않았습니다."; return value; }

                var soil = SpiritFarmRules.Soil(value, plotIndex);
                var yieldMultiplier = Math.Max(.4, soil.Vitality / 100d) * SpiritFarmRules.RackYieldMultiplier(value.SpiritFarmMeta.VerticalRackLevel);
                gold = Math.Max(1, (long)Math.Round(crop.GoldReward * yieldMultiplier));
                materials = Math.Max(1, (long)Math.Round(crop.MaterialReward * yieldMultiplier));
                tickets = crop.SummonTicketReward;
                value.Gold = GameRules.SaturatingAdd(value.Gold, gold);
                value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(value.CompanionUpgradeMaterials, materials);
                value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, tickets);
                if (string.Equals(soil.LastCropKey, crop.CropKey, StringComparison.OrdinalIgnoreCase))
                {
                    soil.ConsecutiveCrops++;
                    soil.Vitality = Math.Max(40, soil.Vitality - 12);
                }
                else
                {
                    soil.LastCropKey = crop.CropKey;
                    soil.ConsecutiveCrops = 1;
                    soil.Vitality = Math.Min(100, soil.Vitality + 15);
                }
                value.SpiritFarmPlots.Remove(plot);
                value.UpdatedUtc = now;
                accepted = true;
                message = $"{crop.Name} 수확 · 금화 {gold:N0} · 광석 {materials:N0}" + (tickets > 0 ? $" · 소환패 {tickets}" : string.Empty)
                          + (soil.ConsecutiveCrops >= 2 ? $" · 연작 {soil.ConsecutiveCrops}회로 지력 {soil.Vitality}%" : string.Empty);
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, gold, materials, tickets, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>등급별 확률로 씨앗 꾸러미 한 개를 열어 실제 작물 씨앗을 획득합니다.</summary>
    public async Task<SpiritFarmActionResult> OpenSpiritSeedPackAsync(
        string packKey,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "열 수 없는 씨앗 꾸러미입니다.";
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.EnsureDailySupply(value, now);
                var pack = SpiritFarmRules.GetSeedPacks().FirstOrDefault(item =>
                    string.Equals(item.PackKey, packKey, StringComparison.OrdinalIgnoreCase));
                if (pack is null) return value;
                if (value.SpiritFarmMeta.SeedPacks.GetValueOrDefault(pack.PackKey) <= 0)
                { message = $"보유한 {pack.Name}이 없습니다."; return value; }

                value.SpiritFarmMeta.SeedPacks[pack.PackKey]--;
                if (value.SpiritFarmMeta.SeedPacks[pack.PackKey] <= 0)
                    value.SpiritFarmMeta.SeedPacks.Remove(pack.PackKey);
                var cropKey = SpiritFarmRules.RollCropKey(pack, _rolls.NextRoll());
                var crop = SpiritFarmRules.FindCrop(cropKey)!;
                SpiritFarmRules.Add(value.SpiritFarmMeta.CropSeeds, cropKey, 1);
                value.UpdatedUtc = now;
                accepted = true;
                message = $"{pack.Name} 개봉 · {crop.Name} 씨앗을 얻었습니다.";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>CodeMaru C를 사용해 원하는 등급의 씨앗 꾸러미를 구입합니다.</summary>
    public async Task<SpiritFarmActionResult> PurchaseSpiritSeedPackAsync(
        string packKey,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "구입할 수 없는 씨앗 꾸러미입니다.";
            var safeQuantity = Math.Clamp(quantity, 1, 10);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.EnsureDailySupply(value, now);
                var pack = SpiritFarmRules.GetSeedPacks().FirstOrDefault(item =>
                    string.Equals(item.PackKey, packKey, StringComparison.OrdinalIgnoreCase));
                if (pack is null) return value;
                var cost = (long)pack.PremiumCost * safeQuantity;
                if (value.PremiumCurrency < cost)
                { message = $"CodeMaru C가 부족합니다. · C {cost:N0}"; return value; }

                value.PremiumCurrency -= cost;
                SpiritFarmRules.Add(value.SpiritFarmMeta.SeedPacks, pack.PackKey, safeQuantity);
                value.UpdatedUtc = now;
                accepted = true;
                message = $"{pack.Name} {safeQuantity}개 구입 · C {cost:N0}";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>CodeMaru C로 영맥 비료 묶음을 구입합니다.</summary>
    public async Task<SpiritFarmActionResult> PurchaseSpiritFertilizerAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "CodeMaru C가 부족합니다.";
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.EnsureDailySupply(value, now);
                if (value.PremiumCurrency < SpiritFarmRules.FertilizerPremiumCost)
                { message = $"CodeMaru C가 부족합니다. · C {SpiritFarmRules.FertilizerPremiumCost}"; return value; }
                value.PremiumCurrency -= SpiritFarmRules.FertilizerPremiumCost;
                value.SpiritFarmMeta.FertilizerCount = Math.Min(9999,
                    value.SpiritFarmMeta.FertilizerCount + SpiritFarmRules.FertilizerBundleSize);
                value.UpdatedUtc = now;
                accepted = true;
                message = $"영맥 비료 {SpiritFarmRules.FertilizerBundleSize}개 구입 · C {SpiritFarmRules.FertilizerPremiumCost}";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>보유 비료를 자신의 재배 중인 밭에 사용해 성장 시간을 단축하고 지력을 회복합니다.</summary>
    public async Task<SpiritFarmActionResult> FertilizeOwnSpiritCropAsync(
        int plotIndex,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "비료를 사용할 수 없는 밭입니다.";
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.EnsureDailySupply(value, now);
                var plot = value.SpiritFarmPlots.FirstOrDefault(item => item.PlotIndex == plotIndex);
                if (plot is null || plot.ReadyUtc <= now)
                { message = "재배 중인 영초에만 비료를 줄 수 있습니다."; return value; }
                if (plot.OwnerFertilized)
                { message = "이 작물에는 이미 직접 비료를 주었습니다."; return value; }
                if (value.SpiritFarmMeta.FertilizerCount <= 0)
                { message = "보유한 영맥 비료가 없습니다."; return value; }

                var crop = SpiritFarmRules.FindCrop(plot.CropKey)!;
                plot.ReadyUtc = plot.ReadyUtc.AddSeconds(-Math.Max(10, crop.GrowTime.TotalSeconds * .15));
                if (plot.ReadyUtc < now) plot.ReadyUtc = now;
                plot.OwnerFertilized = true;
                value.SpiritFarmMeta.FertilizerCount--;
                var soil = SpiritFarmRules.Soil(value, plotIndex);
                soil.Vitality = Math.Min(100, soil.Vitality + 12);
                value.UpdatedUtc = now;
                accepted = true;
                message = $"{crop.Name}에 비료 사용 · 성장 15% 단축 · 지력 {soil.Vitality}%";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>수직 재배대 또는 다음 등급 하우스를 계정 농원에 영구 설치합니다.</summary>
    public async Task<SpiritFarmActionResult> UpgradeSpiritFarmFacilityAsync(
        string facilityKey,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "설치할 수 없는 농원 시설입니다.";
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.Normalize(value);
                if (string.Equals(facilityKey, "rack", StringComparison.OrdinalIgnoreCase))
                {
                    var goldCost = SpiritFarmRules.RackUpgradeGoldCost(value.SpiritFarmMeta.VerticalRackLevel);
                    var materialCost = SpiritFarmRules.RackUpgradeMaterialCost(value.SpiritFarmMeta.VerticalRackLevel);
                    if (goldCost <= 0) { message = "수직 회전 재배대가 최고 단계입니다."; return value; }
                    if (value.Gold < goldCost || value.CompanionUpgradeMaterials < materialCost)
                    { message = $"재배대 설치 재료가 부족합니다. · {goldCost:N0} 금화 · 광석 {materialCost:N0}"; return value; }
                    value.Gold -= goldCost;
                    value.CompanionUpgradeMaterials -= materialCost;
                    value.SpiritFarmMeta.VerticalRackLevel++;
                    accepted = true;
                    message = $"수직 회전 재배대 Lv.{value.SpiritFarmMeta.VerticalRackLevel} · 수확량 +{value.SpiritFarmMeta.VerticalRackLevel * 15}%";
                }
                else if (string.Equals(facilityKey, "house", StringComparison.OrdinalIgnoreCase))
                {
                    var next = SpiritFarmRules.NextHouse(value.SpiritFarmMeta.HouseTier);
                    if (next is null) { message = "온돌·한지 하우스가 완성되어 있습니다."; return value; }
                    if (value.Gold < next.GoldCost || value.CompanionUpgradeMaterials < next.MaterialCost)
                    { message = $"하우스 증축 재료가 부족합니다. · {next.GoldCost:N0} 금화 · 광석 {next.MaterialCost:N0}"; return value; }
                    value.Gold -= next.GoldCost;
                    value.CompanionUpgradeMaterials -= next.MaterialCost;
                    value.SpiritFarmMeta.HouseTier = next.Tier;
                    accepted = true;
                    message = $"{next.Name} 완공 · 재배 시간 {next.GrowTimeReductionPercent}% 단축";
                }
                value.UpdatedUtc = now;
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>친구 농원 목록과 오늘 비료 도움 가능 여부를 조회합니다.</summary>
    public async Task<IReadOnlyList<SpiritFarmFriendView>> GetSpiritFarmFriendsAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();
        var today = _timeProvider.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd");
        var own = (await _store.LoadOrCreateAsync(userId, cancellationToken).ConfigureAwait(false)).Progress;
        SpiritFarmRules.Normalize(own);
        var helped = string.Equals(own.SpiritFarmMeta.FriendHelpDateKey, today, StringComparison.Ordinal)
            ? own.SpiritFarmMeta.HelpedFriendIds.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remaining = Math.Max(0, 3 - helped.Count);
        var players = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        return players
            .Where(player => !string.Equals(player.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .Select(player =>
            {
                SpiritFarmRules.Normalize(player);
                var growing = player.SpiritFarmPlots.Count(plot => plot.ReadyUtc > _timeProvider.GetUtcNow().UtcDateTime);
                return new SpiritFarmFriendView(
                    player.UserId,
                    MaskPlayerId(player.UserId),
                    player.SpiritFarmLevel,
                    growing,
                    remaining > 0 && growing > 0 && !helped.Contains(player.UserId));
            })
            .OrderByDescending(friend => friend.CanHelp)
            .ThenByDescending(friend => friend.FarmLevel)
            .Take(12)
            .ToArray();
    }

    /// <summary>친구 농원의 자라는 밭 하나에 비료를 주어 성장 시간을 단축하고 지력을 회복합니다.</summary>
    public async Task<SpiritFarmActionResult> HelpFriendFarmAsync(
        string friendUserId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var userId = RequireUserId();
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var today = now.ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(friendUserId) || string.Equals(userId, friendUserId, StringComparison.OrdinalIgnoreCase))
                return new SpiritFarmActionResult(false, "자신의 농원에는 친구 비료를 줄 수 없습니다.", 0, 0, 0, Snapshot);

            var own = (await _store.LoadOrCreateAsync(userId, cancellationToken).ConfigureAwait(false)).Progress;
            SpiritFarmRules.EnsureDailySupply(own, now);
            if (own.SpiritFarmMeta.HelpedFriendIds.Count >= 3 || own.SpiritFarmMeta.HelpedFriendIds.Contains(friendUserId, StringComparer.OrdinalIgnoreCase))
                return new SpiritFarmActionResult(false, "오늘 이 친구에게 줄 수 있는 비료 도움을 모두 사용했습니다.", 0, 0, 0, Snapshot);

            var targetAccepted = false;
            await _store.MutateAsync(friendUserId, friend =>
            {
                SpiritFarmRules.Normalize(friend);
                var plot = friend.SpiritFarmPlots
                    .Where(item => item.ReadyUtc > now && !item.FertilizedByUserIds.Contains(userId, StringComparer.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.ReadyUtc)
                    .FirstOrDefault();
                if (plot is null) return friend;
                var crop = SpiritFarmRules.FindCrop(plot.CropKey)!;
                plot.ReadyUtc = plot.ReadyUtc.AddSeconds(-Math.Max(10, crop.GrowTime.TotalSeconds * .1));
                if (plot.ReadyUtc < now) plot.ReadyUtc = now;
                plot.FertilizedByUserIds.Add(userId);
                var soil = SpiritFarmRules.Soil(friend, plot.PlotIndex);
                soil.Vitality = Math.Min(100, soil.Vitality + 8);
                friend.UpdatedUtc = now;
                targetAccepted = true;
                return friend;
            }, cancellationToken).ConfigureAwait(false);
            if (!targetAccepted)
                return new SpiritFarmActionResult(false, "비료를 줄 수 있는 재배 중인 밭이 없습니다.", 0, 0, 0, Snapshot);

            var progress = await _store.MutateAsync(userId, value =>
            {
                SpiritFarmRules.EnsureDailySupply(value, now);
                value.SpiritFarmMeta.FriendHelpDateKey = today;
                if (!value.SpiritFarmMeta.HelpedFriendIds.Contains(friendUserId, StringComparer.OrdinalIgnoreCase))
                    value.SpiritFarmMeta.HelpedFriendIds.Add(friendUserId);
                value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(value.CompanionUpgradeMaterials, 1);
                value.UpdatedUtc = now;
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(true, $"{MaskPlayerId(friendUserId)} 농원에 비료를 주고 광석 1개를 받았습니다.", 0, 1, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    /// <summary>농원 등급을 올려 새로운 경작지를 영구 해금합니다.</summary>
    public async Task<SpiritFarmActionResult> UpgradeSpiritFarmAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "이미 최고 등급의 농원입니다.";
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var progress = await _store.MutateAsync(RequireUserId(), value =>
            {
                SpiritFarmRules.Normalize(value);
                if (value.SpiritFarmLevel >= SpiritFarmRules.MaximumLevel) return value;
                var cost = SpiritFarmRules.UpgradeCost(value.SpiritFarmLevel);
                if (value.Gold < cost)
                { message = $"농원 확장에 필요한 금화가 부족합니다. · {cost:N0} 금화"; return value; }
                value.Gold -= cost;
                value.SpiritFarmLevel++;
                value.UpdatedUtc = now;
                accepted = true;
                message = $"농원이 Lv.{value.SpiritFarmLevel}로 확장되어 새 밭이 열렸습니다.";
                return value;
            }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new SpiritFarmActionResult(accepted, message, 0, 0, 0, Snapshot);
        }
        finally { _gate.Release(); }
    }

    private async Task<(CompanionEquipmentView? Equipment, AutoSpeedBoostDefinition? Boost)> AwardArcadeRewardsAsync(
        int duration,
        int rewardScore,
        long gold,
        long materials,
        int tickets,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string? equipmentInstanceId = null;
        AutoSpeedBoostDefinition? boostDropped = null;
        var progress = await _store.MutateAsync(
            RequireUserId(),
            value =>
            {
                value.Gold = GameRules.SaturatingAdd(value.Gold, gold);
                value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(value.CompanionUpgradeMaterials, materials);
                value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, tickets);
                if (duration >= 45
                    && value.CompanionEquipmentInventory.Count < CompanionProgressionRules.EquipmentInventoryCapacity
                    && (duration >= 180 || _rolls.NextRoll() < 40))
                {
                    var equipment = CompanionProgressionRules.CreateDroppedEquipment(
                        Math.Max(1, rewardScore / 4), _rolls.NextRoll(), _rolls.NextRoll(), value.CompanionEquipmentInventory.Count);
                    value.CompanionEquipmentInventory.Add(equipment);
                    equipmentInstanceId = equipment.InstanceId;
                }
                if (duration >= 90 && (duration >= 240 || _rolls.NextRoll() < 25))
                {
                    boostDropped = GameRules.ResolveAutoSpeedBoostDrop(0, _rolls.NextRoll(), _rolls.NextRoll());
                    if (boostDropped is not null)
                    {
                        value.AutoBoostInventory.TryGetValue(boostDropped.ItemKey, out var count);
                        value.AutoBoostInventory[boostDropped.ItemKey] = count == int.MaxValue ? int.MaxValue : count + 1;
                    }
                }
                value.UpdatedUtc = now;
                return value;
            }, cancellationToken).ConfigureAwait(false);
        Snapshot = CreateSnapshot(progress, Snapshot.Phase);
        var equipmentDropped = equipmentInstanceId is null
            ? null
            : Snapshot.CompanionEquipmentInventory.FirstOrDefault(item =>
                string.Equals(item.Instance.InstanceId, equipmentInstanceId, StringComparison.OrdinalIgnoreCase));
        return (equipmentDropped, boostDropped);
    }

    private ArcadeTrialResult RejectedArcadeTrial(string message) =>
        new(false, 0, 0, 0, 0, 0, 0, null, null, message, Snapshot);

    /// <summary>검 강화 비용 정책을 서버에서 한 단계 적용합니다.</summary>
    public Task<GameSnapshot> UpgradeSwordAsync(CancellationToken cancellationToken = default) =>
        UpgradeSwordAsync(1, cancellationToken);

    /// <summary>요청한 횟수만큼 검을 연속 강화하되 보유 금화로 가능한 단계까지만 적용합니다.</summary>
    public async Task<GameSnapshot> UpgradeSwordAsync(int requestedLevels, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var safeRequestedLevels = Math.Clamp(requestedLevels, 1, 1_000);
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var upgradedLevels = 0;
                    while (upgradedLevels < safeRequestedLevels && value.AttackLevel < int.MaxValue)
                    {
                        var cost = GameRules.UpgradeCost(value.AttackLevel);
                        if (value.Gold < cost) break;
                        value.Gold -= cost;
                        value.AttackLevel++;
                        upgradedLevels++;
                    }
                    if (upgradedLevels > 0)
                        value.UpdatedUtc = DateTime.UtcNow;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>서버 검술 수련 비용 정책을 한 단계 적용합니다.</summary>
    public Task<GameSnapshot> UpgradeSwordArtAsync(CancellationToken cancellationToken = default) =>
        UpgradeSwordArtAsync(1, cancellationToken);

    /// <summary>요청한 횟수만큼 검술을 연속 수련하되 보유 금화로 가능한 단계까지만 적용합니다.</summary>
    public async Task<GameSnapshot> UpgradeSwordArtAsync(int requestedLevels, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var safeRequestedLevels = Math.Clamp(requestedLevels, 1, 1_000);
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var upgradedLevels = 0;
                    while (upgradedLevels < safeRequestedLevels && value.SwordArtLevel < int.MaxValue)
                    {
                        var cost = GameRules.SwordArtUpgradeCost(value.SwordArtLevel);
                        if (value.Gold < cost) break;
                        value.Gold -= cost;
                        value.SwordArtLevel++;
                        upgradedLevels++;
                    }
                    if (upgradedLevels > 0)
                        value.UpdatedUtc = DateTime.UtcNow;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>서버 장비 단련 비용 정책을 적용하고 사용자 진행에 저장합니다.</summary>
    public Task<GameSnapshot> UpgradeEquipmentAsync(CancellationToken cancellationToken = default) =>
        UpgradeEquipmentAsync(InheritedEquipmentSlot.Weapon, 1, cancellationToken);

    /// <summary>요청한 횟수만큼 장비를 연속 단련하되 보유 금화로 가능한 단계까지만 적용합니다.</summary>
    public async Task<GameSnapshot> UpgradeEquipmentAsync(int requestedLevels, CancellationToken cancellationToken = default)
        => await UpgradeEquipmentAsync(InheritedEquipmentSlot.Weapon, requestedLevels, cancellationToken).ConfigureAwait(false);

    /// <summary>선택한 계승 장비 한 부위만 요청 횟수만큼 독립 단련합니다.</summary>
    public async Task<GameSnapshot> UpgradeEquipmentAsync(
        InheritedEquipmentSlot slot,
        int requestedLevels,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var safeRequestedLevels = Math.Clamp(requestedLevels, 1, 1_000);
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var upgradedLevels = 0;
                    var equipmentLevel = GetEquipmentLevel(value, slot);
                    while (upgradedLevels < safeRequestedLevels && equipmentLevel < int.MaxValue)
                    {
                        var cost = GameRules.EquipmentUpgradeCost(equipmentLevel);
                        if (value.Gold < cost) break;
                        value.Gold -= cost;
                        equipmentLevel++;
                        upgradedLevels++;
                    }
                    if (upgradedLevels > 0)
                    {
                        SetEquipmentLevel(value, slot, equipmentLevel);
                        value.UpdatedUtc = DateTime.UtcNow;
                    }
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>영구 해금 안내를 사용자 데이터에서 확인 완료로 저장합니다.</summary>
    public async Task<GameSnapshot> AcknowledgeAutoUnlockAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value => { if (value.AutoAttackUnlocked) value.AutoUnlockNoticeSeen = true; return value; },
                cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    /// <summary>서버 전투 상태 머신을 다음 유효 상태로 진행합니다.</summary>
    public async Task<GameSnapshot> AdvanceCombatPhaseAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var next = Snapshot.Phase switch
            {
                CombatPhase.Intro => CombatPhase.Fighting,
                CombatPhase.Victory => CombatPhase.Transitioning,
                CombatPhase.Transitioning when Snapshot.World.IsBoss => CombatPhase.Intro,
                CombatPhase.Transitioning => CombatPhase.Fighting,
                _ => Snapshot.Phase
            };
            Snapshot = Snapshot with { Phase = next };
            return Snapshot;
        }
        finally { _gate.Release(); }
    }

    private AttackMutation ApplyAttack(GameProgress progress, AttackSource source, string? guardTargetId, int expectedStage)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        NormalizeActiveBoost(progress, now);
        DailyMissionPolicy.EnsureCurrentDay(progress, now);
        if (progress.Stage != expectedStage)
            return new AttackMutation(
                progress, false, 0, false, false, 0, false, null, null,
                "이미 다음 스테이지로 전환되었습니다.");
        progress.HighestClearedStage = Math.Max(progress.HighestClearedStage, progress.Stage - 1);
        progress.AutoAttackUnlocked = progress.AutoAttackUnlocked
            || GameRules.CanUnlockAutoAttack(progress.HighestClearedStage);
        if (source == AttackSource.Automatic && (!progress.AutoAttackUnlocked || !progress.AutoAttackEnabled))
            return new AttackMutation(progress, false, 0, false, false, 0, false, null, null,
                "AUTO가 잠겨 있거나 꺼져 있습니다.");

        var inheritedAttackPower = GameRules.AttackPower(
            progress.AttackLevel,
            progress.SwordArtLevel,
            progress.WeaponEquipmentLevel);
        var party = ApplyJadeInheritance(PartyRules.Resolve(
            progress.HighestClearedStage,
            progress.ActiveHeroId,
            progress.SelectedCompanionIds,
            inheritedAttackPower,
            progress.OwnedCompanionIds,
            progress), progress.JadeEquipmentLevel);
        var roll = GameRules.CalculateDamage(
            progress.AttackLevel,
            _rolls.NextRoll(),
            progress.SwordArtLevel,
            progress.WeaponEquipmentLevel,
            party.AssistPowerPercent);
        var defeatedStage = progress.Stage;
        progress.Gold = GameRules.SaturatingAdd(progress.Gold, GameRules.AttackGold(defeatedStage));
        if (guardTargetId is not null
            && _bossGuardHp.TryGetValue(guardTargetId, out var guardHp)
            && guardHp > 0)
        {
            _bossGuardHp[guardTargetId] = Math.Max(0L, guardHp - roll.Damage);
            progress.UpdatedUtc = now;
            return new AttackMutation(
                progress, true, roll.Damage, roll.IsCritical, false, 0, false, null, guardTargetId, null);
        }

        var remainingHp = Math.Max(0L, progress.EnemyHp - roll.Damage);
        progress.EnemyHp = remainingHp;
        var defeated = remainingHp == 0;
        var reward = 0L;
        var unlockedNow = false;
        AutoSpeedBoostDefinition? speedBoostDropped = null;
        CompanionEquipmentView? equipmentDropped = null;
        if (defeated)
        {
            reward = GameRules.VictoryReward(defeatedStage);
            progress.Gold = GameRules.SaturatingAdd(progress.Gold, reward);
            progress.TotalDefeated = GameRules.SaturatingAdd(progress.TotalDefeated, 1);
            progress.HighestClearedStage = Math.Max(progress.HighestClearedStage, defeatedStage);
            if (defeatedStage > progress.HighestSummonRewardedStage)
            {
                progress.HeroSummonTickets = GameRules.SaturatingAdd(progress.HeroSummonTickets, 1);
                progress.HighestSummonRewardedStage = defeatedStage;
            }
            if (!progress.AutoAttackUnlocked && GameRules.CanUnlockAutoAttack(progress.HighestClearedStage))
            {
                progress.AutoAttackUnlocked = true;
                progress.AutoAttackEnabled = false;
                progress.AutoUnlockNoticeSeen = false;
                unlockedNow = true;
            }
            if (GameRules.IsBossStage(defeatedStage))
            {
                speedBoostDropped = GameRules.ResolveAutoSpeedBoostDrop(
                    _rolls.NextRoll(),
                    _rolls.NextRoll(),
                    _rolls.NextRoll());
                if (speedBoostDropped is not null)
                {
                    progress.AutoBoostInventory.TryGetValue(speedBoostDropped.ItemKey, out var currentCount);
                    progress.AutoBoostInventory[speedBoostDropped.ItemKey] = currentCount == int.MaxValue
                        ? int.MaxValue
                        : currentCount + 1;
                }
            }
            if (progress.CompanionEquipmentInventory.Count < CompanionProgressionRules.EquipmentInventoryCapacity
                && CompanionProgressionRules.ShouldDropEquipment(defeatedStage, _rolls.NextRoll()))
            {
                var dropped = CompanionProgressionRules.CreateDroppedEquipment(
                    defeatedStage,
                    _rolls.NextRoll(),
                    _rolls.NextRoll(),
                    progress.CompanionEquipmentInventory.Count);
                progress.CompanionEquipmentInventory.Add(dropped);
                equipmentDropped = CompanionProgressionRules.ResolveInventory(progress).First(item =>
                    string.Equals(item.Instance.InstanceId, dropped.InstanceId, StringComparison.OrdinalIgnoreCase));
            }
            progress.Stage = defeatedStage == int.MaxValue ? int.MaxValue : defeatedStage + 1;
            progress.EnemyHp = GameRules.EnemyMaxHp(progress.Stage);
        }
        progress.UpdatedUtc = now;
        return new AttackMutation(
            progress, true, roll.Damage, roll.IsCritical, defeated, reward, unlockedNow, speedBoostDropped, "enemy", null)
        {
            EquipmentDropped = equipmentDropped
        };
    }

    private GameSnapshot CreateSnapshot(GameProgress value, CombatPhase phase)
    {
        DailyMissionPolicy.EnsureCurrentDay(value, _timeProvider.GetUtcNow().UtcDateTime);
        var maxHp = GameRules.EnemyMaxHp(value.Stage);
        var inheritedAttackPower = GameRules.AttackPower(
            value.AttackLevel,
            value.SwordArtLevel,
            value.WeaponEquipmentLevel);
        var party = ApplyJadeInheritance(PartyRules.Resolve(
            value.HighestClearedStage,
            value.ActiveHeroId,
            value.SelectedCompanionIds,
            inheritedAttackPower,
            value.OwnedCompanionIds,
            value), value.JadeEquipmentLevel);
        EnsureCombatants(value, party);
        return new GameSnapshot(
            value.Stage, value.Gold, value.HeroSummonTickets, value.HeroSummonPity,
            value.CompanionUpgradeMaterials,
            new Dictionary<string, int>(value.CompanionShards, StringComparer.OrdinalIgnoreCase),
            value.AttackLevel,
            party.PartyAttackPower,
            GameRules.UpgradeCost(value.AttackLevel),
            value.SwordArtLevel, GameRules.SwordArtAttackBonus(value.SwordArtLevel),
            GameRules.SwordArtUpgradeCost(value.SwordArtLevel),
            value.WeaponEquipmentLevel, GameRules.EquipmentAttackBonus(value.WeaponEquipmentLevel),
            GameRules.EquipmentUpgradeCost(value.WeaponEquipmentLevel),
            Math.Clamp(_playerHp, 0L, GameRules.PlayerMaxHp(value.AttackLevel, value.ArmorEquipmentLevel)),
            GameRules.PlayerMaxHp(value.AttackLevel, value.ArmorEquipmentLevel), value.TotalDefeated,
            Math.Clamp(value.EnemyHp, 0, maxHp), maxHp, value.HighestClearedStage,
            value.AutoAttackUnlocked, value.AutoAttackEnabled,
            value.ActiveAutoSpeedMultiplier, value.AutoSpeedBoostEndsUtc,
            GameRules.GetAutoSpeedBoosts()
                .Select(item => new AutoSpeedBoostInventoryEntry(
                    item,
                    value.AutoBoostInventory.TryGetValue(item.ItemKey, out var count) ? count : 0))
                .Where(entry => entry.Count > 0)
                .OrderByDescending(entry => entry.Item.Multiplier)
                .ThenByDescending(entry => entry.Item.Duration)
                .ToArray(),
            value.AutoAttackUnlocked && !value.AutoUnlockNoticeSeen,
            GameRules.AutoAttackInterval(value.ActiveAutoSpeedMultiplier), phase, _regions.Resolve(value.Stage),
            _regions.CreateCodex(value.HighestClearedStage), party, value.UpdatedUtc)
        {
            GameNickname = value.GameNickname,
            NicknameChangeCount = value.NicknameChangeCount,
            CompanionUnits = CreateCompanionSnapshots(value, party),
            BossGuardUnits = CreateBossGuardSnapshots(value.Stage),
            CompanionGrowth = party.AvailableCompanions
                .Select(hero => CompanionProgressionRules.Resolve(value, hero))
                .ToArray(),
            CompanionEquipmentInventory = CompanionProgressionRules.ResolveInventory(value),
            PremiumCurrency = value.PremiumCurrency,
            LegendarySummonPity = value.LegendarySummonPity,
            HighestTrialTowerFloor = value.HighestTrialTowerFloor,
            TrialTowerChallengeTickets = value.TrialTowerChallengeTickets,
            NextTrialTowerTicketUtc = TrialTowerRules.NextChallengeTicketUtc(value),
            TrialTowerPartyPower = party.PartyAttackPower,
            WeaponEquipmentLevel = value.WeaponEquipmentLevel,
            ArmorEquipmentLevel = value.ArmorEquipmentLevel,
            JadeEquipmentLevel = value.JadeEquipmentLevel,
            WeaponUpgradeCost = GameRules.EquipmentUpgradeCost(value.WeaponEquipmentLevel),
            ArmorUpgradeCost = GameRules.EquipmentUpgradeCost(value.ArmorEquipmentLevel),
            JadeUpgradeCost = GameRules.EquipmentUpgradeCost(value.JadeEquipmentLevel),
            JadeAssistBonusPercent = GameRules.JadeAssistBonusPercent(value.JadeEquipmentLevel),
            OfflineReward = new OfflineRewardView(
                OfflineRewardPolicy.IsUnlocked(value.HighestClearedStage),
                OfflineRewardPolicy.UnlockStage,
                value.PendingOfflineGold,
                TimeSpan.FromSeconds(Math.Max(0L, value.PendingOfflineSeconds)),
                OfflineRewardPolicy.GoldPerHour(value),
                OfflineRewardPolicy.MaximumAccrual),
            DailyMissions = DailyMissionPolicy.GetDefinitions()
                .Select(mission =>
                {
                    var progress = DailyMissionPolicy.Progress(value, mission.MissionKey);
                    return new DailyMissionView(
                        mission,
                        Math.Min(mission.Target, progress),
                        progress >= mission.Target,
                        value.ClaimedDailyMissionKeys.Contains(mission.MissionKey, StringComparer.OrdinalIgnoreCase));
                })
                .ToArray(),
            SpiritFarm = CreateSpiritFarmView(value, _timeProvider.GetUtcNow().UtcDateTime)
        };
    }

    private static SpiritFarmView CreateSpiritFarmView(GameProgress value, DateTime now)
    {
        SpiritFarmRules.Normalize(value);
        var crops = SpiritFarmRules.GetCrops();
        var planted = value.SpiritFarmPlots.ToDictionary(plot => plot.PlotIndex);
        var unlockedPlots = SpiritFarmRules.UnlockedPlots(value.SpiritFarmLevel);
        var plots = Enumerable.Range(0, SpiritFarmRules.MaximumPlots)
            .Select(index =>
            {
                if (!planted.TryGetValue(index, out var plot))
                {
                    var emptySoil = SpiritFarmRules.Soil(value, index);
                    return new SpiritFarmPlotView(index, index < unlockedPlots, null, null, null, false)
                    {
                        SoilVitality = emptySoil.Vitality,
                        ConsecutiveCrops = emptySoil.ConsecutiveCrops,
                        LastCropKey = emptySoil.LastCropKey
                    };
                }
                var crop = SpiritFarmRules.FindCrop(plot.CropKey);
                var soil = SpiritFarmRules.Soil(value, index);
                return new SpiritFarmPlotView(index, index < unlockedPlots, crop, plot.PlantedUtc, plot.ReadyUtc, plot.ReadyUtc <= now)
                {
                    SoilVitality = soil.Vitality,
                    ConsecutiveCrops = soil.ConsecutiveCrops,
                    LastCropKey = soil.LastCropKey,
                    OwnerFertilized = plot.OwnerFertilized,
                    FriendFertilizerCount = plot.FertilizedByUserIds.Count
                };
            })
            .ToArray();
        return new SpiritFarmView(
            value.SpiritFarmLevel,
            unlockedPlots,
            SpiritFarmRules.MaximumPlots,
            SpiritFarmRules.UpgradeCost(value.SpiritFarmLevel),
            crops,
            plots)
        {
            SeedPacks = new Dictionary<string, int>(value.SpiritFarmMeta.SeedPacks, StringComparer.OrdinalIgnoreCase),
            CropSeeds = new Dictionary<string, int>(value.SpiritFarmMeta.CropSeeds, StringComparer.OrdinalIgnoreCase),
            SeedPackCatalog = SpiritFarmRules.GetSeedPacks(),
            FertilizerCount = value.SpiritFarmMeta.FertilizerCount,
            FriendsHelpedToday = value.SpiritFarmMeta.HelpedFriendIds.Count,
            VerticalRackLevel = value.SpiritFarmMeta.VerticalRackLevel,
            VerticalRackUpgradeGoldCost = SpiritFarmRules.RackUpgradeGoldCost(value.SpiritFarmMeta.VerticalRackLevel),
            VerticalRackUpgradeMaterialCost = SpiritFarmRules.RackUpgradeMaterialCost(value.SpiritFarmMeta.VerticalRackLevel),
            House = SpiritFarmRules.CurrentHouse(value.SpiritFarmMeta.HouseTier),
            NextHouse = SpiritFarmRules.NextHouse(value.SpiritFarmMeta.HouseTier)
        };
    }

    private void EnsureCombatants(GameProgress value, PartyView party)
    {
        var stageChanged = _combatantStage != value.Stage;
        if (stageChanged)
        {
            _companionHp.Clear();
            _bossGuardHp.Clear();
            _enemyTargetCursor = 0;
            _combatantStage = value.Stage;
        }

        var selectedIds = party.Companions
            .Select(hero => hero.HeroId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in _companionHp.Keys.Where(id => !selectedIds.Contains(id)).ToArray())
            _companionHp.Remove(stale);
        foreach (var hero in party.Companions)
        {
            var growth = CompanionProgressionRules.Resolve(value, hero);
            var maximum = GameRules.CompanionMaxHp(value.Stage, value.ArmorEquipmentLevel, growth.HealthPercent);
            if (stageChanged || !_companionHp.TryGetValue(hero.HeroId, out var current))
                _companionHp[hero.HeroId] = maximum;
            else
                _companionHp[hero.HeroId] = Math.Clamp(current, 0L, maximum);
        }

        var encounterGuards = ResolveEncounterGuards(value.Stage);
        if (encounterGuards.Count > 0)
        {
            var maximum = GameRules.EncounterGuardMaxHp(value.Stage);
            foreach (var guard in encounterGuards)
            {
                if (stageChanged || !_bossGuardHp.TryGetValue(guard.UnitId, out var current))
                    _bossGuardHp[guard.UnitId] = maximum;
                else
                    _bossGuardHp[guard.UnitId] = Math.Clamp(current, 0L, maximum);
            }
        }
        else
        {
            _bossGuardHp.Clear();
        }
    }

    private IReadOnlyList<BattleUnitSnapshot> CreateCompanionSnapshots(GameProgress value, PartyView party) =>
        party.Companions
            .Select(hero =>
            {
                var growth = CompanionProgressionRules.Resolve(value, hero);
                var maximum = GameRules.CompanionMaxHp(value.Stage, value.ArmorEquipmentLevel, growth.HealthPercent);
                return new BattleUnitSnapshot(
                    hero.HeroId,
                    hero.Name,
                    hero.Role,
                    growth.ActiveSkin.AssetUrl,
                    _companionHp.TryGetValue(hero.HeroId, out var hp) ? Math.Clamp(hp, 0L, maximum) : maximum,
                    maximum)
                {
                    SkinCssClass = growth.ActiveSkin.CssClass
                };
            })
            .ToArray();

    private async Task<CompanionActionResult> MutateCompanionAsync(
        string heroId,
        Func<GameProgress, HeroDefinition, (bool Accepted, string Message)> action,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(heroId);
        ArgumentNullException.ThrowIfNull(action);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var accepted = false;
            var message = "동료를 찾을 수 없습니다.";
            var progress = await _store.MutateAsync(
                RequireUserId(),
                value =>
                {
                    var hero = PartyRules.AllCompanions.FirstOrDefault(candidate =>
                        string.Equals(candidate.HeroId, heroId, StringComparison.OrdinalIgnoreCase));
                    if (hero is null || !value.OwnedCompanionIds.Contains(hero.HeroId, StringComparer.OrdinalIgnoreCase))
                        return value;
                    (accepted, message) = action(value, hero);
                    if (accepted) value.UpdatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                    return value;
                }, cancellationToken).ConfigureAwait(false);
            Snapshot = CreateSnapshot(progress, Snapshot.Phase);
            return new CompanionActionResult(accepted, message, Snapshot);
        }
        finally { _gate.Release(); }
    }

    private IReadOnlyList<BattleUnitSnapshot> CreateBossGuardSnapshots(int stage)
    {
        var maximum = GameRules.EncounterGuardMaxHp(stage);
        return ResolveEncounterGuards(stage)
            .Select(guard => new BattleUnitSnapshot(
                guard.UnitId,
                guard.Name,
                guard.Role,
                guard.AssetUrl,
                _bossGuardHp.TryGetValue(guard.UnitId, out var hp) ? Math.Clamp(hp, 0L, maximum) : maximum,
                maximum))
            .ToArray();
    }

    private AttackResult Rejected(AttackSource source, string reason) =>
        new(false, source, 0, false, false, 0, false, null, reason, Snapshot);

    private static void NormalizeActiveBoost(GameProgress progress, DateTime now)
    {
        progress.ActiveAutoSpeedMultiplier = GameRules.NormalizeAutoSpeedMultiplier(progress.ActiveAutoSpeedMultiplier);
        progress.AutoSpeedBoostRemainingMilliseconds = Math.Max(0L, progress.AutoSpeedBoostRemainingMilliseconds);
        if (progress.ActiveAutoSpeedMultiplier <= 1)
        {
            progress.ActiveAutoSpeedMultiplier = 1;
            progress.AutoSpeedBoostEndsUtc = null;
            progress.AutoSpeedBoostRemainingMilliseconds = 0;
            return;
        }

        if (progress.AutoSpeedBoostEndsUtc is null)
        {
            if (progress.AutoSpeedBoostRemainingMilliseconds > 0) return;
            progress.ActiveAutoSpeedMultiplier = 1;
            return;
        }

        if (progress.AutoSpeedBoostEndsUtc <= now)
        {
            progress.ActiveAutoSpeedMultiplier = 1;
            progress.AutoSpeedBoostEndsUtc = null;
            progress.AutoSpeedBoostRemainingMilliseconds = 0;
        }
    }

    private static void ResumeActiveBoost(GameProgress progress, DateTime now)
    {
        progress.ActiveAutoSpeedMultiplier = GameRules.NormalizeAutoSpeedMultiplier(progress.ActiveAutoSpeedMultiplier);
        if (progress.ActiveAutoSpeedMultiplier <= 1)
        {
            progress.ActiveAutoSpeedMultiplier = 1;
            progress.AutoSpeedBoostEndsUtc = null;
            progress.AutoSpeedBoostRemainingMilliseconds = 0;
            return;
        }

        var remainingMilliseconds = Math.Max(0L, progress.AutoSpeedBoostRemainingMilliseconds);
        if (remainingMilliseconds == 0
            && progress.AutoSpeedBoostEndsUtc is not null
            && progress.AutoSpeedBoostEndsUtc <= now)
        {
            var lastActiveUtc = progress.UpdatedUtc == default || progress.UpdatedUtc > now
                ? now
                : progress.UpdatedUtc;
            remainingMilliseconds = Math.Max(
                0L,
                (long)Math.Ceiling((progress.AutoSpeedBoostEndsUtc.Value - lastActiveUtc).TotalMilliseconds));
        }

        if (remainingMilliseconds > 0)
        {
            progress.AutoSpeedBoostEndsUtc = now.AddMilliseconds(remainingMilliseconds);
            progress.AutoSpeedBoostRemainingMilliseconds = 0;
            return;
        }

        NormalizeActiveBoost(progress, now);
    }

    private static PartyView ApplyJadeInheritance(PartyView party, int jadeLevel)
    {
        var jadeBonus = party.CompanionCount > 0 ? GameRules.JadeAssistBonusPercent(jadeLevel) : 0;
        var assist = Math.Min(int.MaxValue, (long)party.AssistPowerPercent + jadeBonus);
        return party with
        {
            AssistPowerPercent = (int)assist,
            PartyAttackPower = GameRules.PartyAttackPower(party.InheritedAttackPower, (int)assist)
        };
    }

    private static int GetEquipmentLevel(GameProgress progress, InheritedEquipmentSlot slot) => slot switch
    {
        InheritedEquipmentSlot.Armor => progress.ArmorEquipmentLevel,
        InheritedEquipmentSlot.Jade => progress.JadeEquipmentLevel,
        _ => progress.WeaponEquipmentLevel
    };

    private static void SetEquipmentLevel(GameProgress progress, InheritedEquipmentSlot slot, int level)
    {
        switch (slot)
        {
            case InheritedEquipmentSlot.Armor:
                progress.ArmorEquipmentLevel = level;
                break;
            case InheritedEquipmentSlot.Jade:
                progress.JadeEquipmentLevel = level;
                break;
            default:
                progress.WeaponEquipmentLevel = level;
                progress.EquipmentLevel = level;
                break;
        }
    }

    private string RequireUserId() =>
        _userId ?? throw new InvalidOperationException("게임 세션이 초기화되지 않았습니다.");

    private static string MaskPlayerId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return "이름 없는 협객";
        if (userId.Length <= 8) return userId;
        return $"{userId[..4]}…{userId[^3..]}";
    }

    private sealed record AttackMutation(
        GameProgress Progress,
        bool Accepted,
        long Damage,
        bool IsCritical,
        bool EnemyDefeated,
        long Reward,
        bool AutoUnlockedNow,
        AutoSpeedBoostDefinition? AutoSpeedBoostDropped,
        string? TargetUnitId,
        string? RejectionReason)
    {
        public CompanionEquipmentView? EquipmentDropped { get; init; }
    }

    private static string WeaponTypeName(CompanionWeaponType type) => type switch
    {
        CompanionWeaponType.Sword => "검",
        CompanionWeaponType.Spear => "창",
        CompanionWeaponType.Bow => "활",
        CompanionWeaponType.Staff => "지팡이",
        CompanionWeaponType.Mace => "철퇴",
        CompanionWeaponType.Dagger => "쌍단도",
        _ => "전용 무기"
    };

    private static readonly IReadOnlyList<BattleUnitDefinition> BossGuardDefinitions =
    [
        new("guard-shadow-jackal-a", "그림자 창병", "전열 호위", "/images/maru-idle/enemies/guards/shadow-jackal-v1.png"),
        new("guard-stone-lion-a", "석갑 맹수", "돌격 호위", "/images/maru-idle/enemies/guards/stone-lion-v1.png"),
        new("guard-shadow-jackal-b", "월식 추격자", "후열 호위", "/images/maru-idle/enemies/guards/shadow-jackal-v1.png"),
        new("guard-stone-lion-b", "청동 파수수", "수문 호위", "/images/maru-idle/enemies/guards/stone-lion-v1.png"),
        new("guard-shadow-jackal-c", "안개 척후", "기습 호위", "/images/maru-idle/enemies/guards/shadow-jackal-v1.png")
    ];

    private static IReadOnlyList<BattleUnitDefinition> ResolveEncounterGuards(int stage)
    {
        var count = GameRules.IsBossStage(stage) ? 5 : 2 + (Math.Max(1, stage) % 4);
        return BossGuardDefinitions.Take(count).ToArray();
    }

    private sealed record BattleUnitDefinition(string UnitId, string Name, string Role, string AssetUrl);
}
