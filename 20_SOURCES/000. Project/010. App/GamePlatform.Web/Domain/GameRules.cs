namespace GamePlatform.Domain;

/// <summary>전투, 성장 및 자동공격에 관한 서버 도메인 정책을 제공합니다.</summary>
public static class GameRules
{
    private static readonly IReadOnlyDictionary<string, AutoSpeedBoostDefinition> AutoSpeedBoosts =
        new[] { 2, 3, 5 }
            .SelectMany(multiplier => new[] { 5, 10, 20 }.Select(minutes =>
                new AutoSpeedBoostDefinition(
                    $"auto-x{multiplier}-{minutes}m",
                    multiplier,
                    TimeSpan.FromMinutes(minutes),
                    $"{AutoSpeedBoostName(multiplier)} · {minutes}분",
                    AutoSpeedBoostPriceC(multiplier, minutes),
                    $"/images/maru-idle/boosts/auto-talisman-x{multiplier}-v1.png")))
            .ToDictionary(item => item.ItemKey, StringComparer.OrdinalIgnoreCase);

    /// <summary>자동공격이 영구 해금되는 클리어 스테이지입니다.</summary>
    public const int AutoAttackUnlockStage = 15;

    /// <summary>연속 수동공격과 AUTO가 공유하는 기본 공격 간격입니다.</summary>
    public static readonly TimeSpan DefaultAutoAttackInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>몬스터의 기본 공격 간격입니다.</summary>
    public static readonly TimeSpan DefaultEnemyAttackInterval = TimeSpan.FromMilliseconds(1600);

    /// <summary>서버가 허용하는 최소 수동공격 간격입니다.</summary>
    public static readonly TimeSpan MinimumManualAttackInterval = TimeSpan.FromMilliseconds(225);

    /// <summary>서버가 허용하는 기본 AUTO 최소 요청 간격입니다.</summary>
    public static readonly TimeSpan MinimumAutomaticAttackInterval = TimeSpan.FromMilliseconds(225);

    /// <summary>스테이지에 해당하는 적의 최대 체력을 계산합니다.</summary>
    public static long EnemyMaxHp(int stage)
    {
        var safeStage = Math.Max(1, stage);
        var regionTier = (safeStage - 1L) / 10L;
        return SaturatingAdd(120L + ((safeStage - 1L) * 88L), regionTier * 240L);
    }

    /// <summary>검 강화, 검술과 장비 레벨을 합산한 공격력을 계산합니다.</summary>
    public static long AttackPower(int attackLevel, int swordArtLevel = 0, int equipmentLevel = 0)
    {
        var safeLevel = Math.Max(1, attackLevel);
        var basePower = 14L + ((safeLevel - 1L) * 8L);
        return SaturatingAdd(basePower, SaturatingAdd(
            SwordArtAttackBonus(swordArtLevel),
            EquipmentAttackBonus(equipmentLevel)));
    }

    /// <summary>계승 공격력에 현재 보조 캐릭터의 지원 비율을 적용합니다.</summary>
    public static long PartyAttackPower(long inheritedAttackPower, int assistPowerPercent) =>
        SaturatingAdd(
            Math.Max(1L, inheritedAttackPower),
            SaturatingMultiply(Math.Max(1L, inheritedAttackPower), Math.Max(0L, assistPowerPercent)) / 100L);

    /// <summary>서버 난수 롤을 이용해 최종 데미지와 치명타 여부를 계산합니다.</summary>
    public static DamageRoll CalculateDamage(
        int attackLevel,
        int roll,
        int swordArtLevel = 0,
        int equipmentLevel = 0,
        int partyAssistPowerPercent = 0)
    {
        var power = PartyAttackPower(
            AttackPower(attackLevel, swordArtLevel, equipmentLevel),
            partyAssistPowerPercent);
        var clampedRoll = Math.Clamp(roll, 0, 99);
        var isCritical = clampedRoll < 12;
        var variancePercent = 94 + (clampedRoll % 13);
        var damage = Math.Max(1L, SaturatingMultiply(power, variancePercent) / 100L);
        if (isCritical)
        {
            damage = Math.Max(1L, SaturatingMultiply(damage, 185L) / 100L);
        }

        return new DamageRoll(damage, isCritical);
    }

    /// <summary>현재 검 강화 레벨에서 다음 강화 비용을 계산합니다.</summary>
    public static long UpgradeCost(int attackLevel)
    {
        var level = Math.Max(1, attackLevel);
        return SaturatingMultiply(SaturatingMultiply(90L, level), level);
    }

    /// <summary>검술 수련 레벨이 제공하는 공격력 보너스를 계산합니다.</summary>
    public static long SwordArtAttackBonus(int swordArtLevel) =>
        SaturatingMultiply(Math.Max(0L, swordArtLevel), 5L);

    /// <summary>다음 검술 수련 비용을 계산합니다.</summary>
    public static long SwordArtUpgradeCost(int swordArtLevel)
    {
        var nextLevel = Math.Max(0L, swordArtLevel) + 1L;
        return SaturatingMultiply(180L, SaturatingMultiply(nextLevel, nextLevel));
    }

    /// <summary>장비 단련 레벨이 제공하는 공격력 보너스를 계산합니다.</summary>
    public static long EquipmentAttackBonus(int equipmentLevel) =>
        SaturatingMultiply(Math.Max(0L, equipmentLevel), 7L);

    /// <summary>인연 옥패 레벨이 모든 동료에게 제공하는 지원 효과를 계산합니다.</summary>
    public static int JadeAssistBonusPercent(int jadeLevel) =>
        (int)Math.Min(int.MaxValue, Math.Max(0L, jadeLevel) * 2L);

    /// <summary>다음 장비 단련 비용을 계산합니다.</summary>
    public static long EquipmentUpgradeCost(int equipmentLevel)
    {
        var nextLevel = Math.Max(0L, equipmentLevel) + 1L;
        return SaturatingMultiply(260L, SaturatingMultiply(nextLevel, nextLevel));
    }

    /// <summary>검 성장과 장비 단련을 반영한 플레이어 최대 생명력을 계산합니다.</summary>
    public static long PlayerMaxHp(int attackLevel, int equipmentLevel)
    {
        var safeAttackLevel = Math.Max(1L, attackLevel);
        var safeEquipmentLevel = Math.Max(0L, equipmentLevel);
        return SaturatingAdd(300L + (safeAttackLevel * 75L), safeEquipmentLevel * 90L);
    }

    /// <summary>동료 한 명의 현재 스테이지 최대 체력을 성장도와 역할 보정으로 계산합니다.</summary>
    public static long CompanionMaxHp(int stage, int equipmentLevel, int assistPowerPercent)
    {
        var safeStage = Math.Max(1L, stage);
        var safeEquipment = Math.Max(0L, equipmentLevel);
        var safeAssist = Math.Max(0L, assistPowerPercent);
        return SaturatingAdd(
            140L + SaturatingMultiply(safeStage, 12L),
            SaturatingAdd(SaturatingMultiply(safeEquipment, 35L), SaturatingMultiply(safeAssist, 18L)));
    }

    /// <summary>보스 호위 한 개체의 최대 체력을 보스 본체 체력과 독립적으로 계산합니다.</summary>
    public static long BossGuardMaxHp(int stage)
    {
        var bossHp = EnemyMaxHp(stage);
        return Math.Max(1L, SaturatingMultiply(bossHp, 28L) / 100L);
    }

    /// <summary>모든 스테이지의 보조 몬스터 체력을 일반·보스 난이도에 맞게 계산합니다.</summary>
    public static long EncounterGuardMaxHp(int stage)
    {
        var enemyHp = EnemyMaxHp(stage);
        var percent = IsBossStage(stage) ? 18L : 8L;
        return Math.Max(1L, SaturatingMultiply(enemyHp, percent) / 100L);
    }

    /// <summary>스테이지와 장비 방어력을 반영한 몬스터의 서버 데미지를 계산합니다.</summary>
    public static long EnemyAttackDamage(int stage, int equipmentLevel, int roll)
    {
        var safeStage = Math.Max(1L, stage);
        var baseDamage = safeStage switch
        {
            <= 15L => 3L + ((safeStage - 1L) / 5L),
            <= 30L => 6L + (safeStage - 15L),
            <= 50L => 22L + ((safeStage - 30L) * 2L),
            _ => 60L + ((safeStage - 50L) * 2L)
        };
        if (IsBossStage((int)Math.Min(int.MaxValue, safeStage)))
            baseDamage = Math.Max(1L, SaturatingMultiply(baseDamage, 125L) / 100L);
        var variancePercent = 90L + Math.Clamp(roll, 0, 20);
        var rolledDamage = SaturatingMultiply(baseDamage, variancePercent) / 100L;
        var defense = SaturatingMultiply(Math.Max(0L, equipmentLevel), 3L);
        return Math.Max(1L, rolledDamage - defense);
    }

    /// <summary>스테이지에 머물러 공격할 때마다 획득하는 수련 금화를 계산합니다.</summary>
    public static long AttackGold(int stage) => 1L + ((Math.Max(1L, stage) - 1L) / 10L);

    /// <summary>보스 전리품 확률과 종류 난수를 적용해 가속 부적을 결정합니다. 기본 드랍률은 3%입니다.</summary>
    public static AutoSpeedBoostDefinition? ResolveAutoSpeedBoostDrop(
        int dropRoll,
        int multiplierRoll,
        int durationRoll)
    {
        if (Math.Clamp(dropRoll, 0, 99) >= 3)
            return null;

        var normalizedMultiplierRoll = Math.Clamp(multiplierRoll, 0, 99);
        var multiplier = normalizedMultiplierRoll switch
        {
            < 10 => 5,
            < 35 => 3,
            _ => 2
        };

        var normalizedDurationRoll = Math.Clamp(durationRoll, 0, 99);
        var minutes = normalizedDurationRoll switch
        {
            < 10 => 20,
            < 35 => 10,
            _ => 5
        };
        return AutoSpeedBoosts[$"auto-x{multiplier}-{minutes}m"];
    }

    /// <summary>아이템 키에 해당하는 AUTO 가속 부적 정의를 조회합니다.</summary>
    public static bool TryGetAutoSpeedBoost(string itemKey, out AutoSpeedBoostDefinition definition) =>
        AutoSpeedBoosts.TryGetValue(itemKey, out definition!);

    /// <summary>모든 AUTO 가속 부적 정의를 반환합니다.</summary>
    public static IReadOnlyCollection<AutoSpeedBoostDefinition> GetAutoSpeedBoosts() => AutoSpeedBoosts.Values.ToArray();

    private static string AutoSpeedBoostName(int multiplier) => multiplier switch
    {
        >= 5 => "천뢰 질풍부",
        >= 3 => "뇌운 질풍부",
        _ => "청풍 질풍부"
    };

    private static long AutoSpeedBoostPriceC(int multiplier, int minutes)
    {
        var grade = multiplier switch { >= 5 => 4L, >= 3 => 2L, _ => 1L };
        var duration = minutes switch { >= 20 => 4L, >= 10 => 2L, _ => 1L };
        return 15L * grade * duration;
    }

    /// <summary>이전 버전의 AUTO 가속 부적 키를 현재 지원하는 등급과 시간으로 변환합니다.</summary>
    public static string? NormalizeAutoSpeedBoostItemKey(string? itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey)) return null;
        if (AutoSpeedBoosts.ContainsKey(itemKey)) return itemKey;

        var parts = itemKey.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3
            || !parts[1].StartsWith('x')
            || !parts[2].EndsWith('m')
            || !int.TryParse(parts[1].AsSpan(1), out var legacyMultiplier)
            || !int.TryParse(parts[2].AsSpan(0, parts[2].Length - 1), out var legacyMinutes))
            return null;

        var multiplier = legacyMultiplier switch
        {
            >= 5 => 5,
            >= 3 => 3,
            >= 2 => 2,
            _ => 0
        };
        var minutes = legacyMinutes switch
        {
            >= 60 => 20,
            >= 30 => 10,
            >= 10 => 5,
            >= 5 => 5,
            _ => 0
        };
        var normalizedKey = $"auto-x{multiplier}-{minutes}m";
        return AutoSpeedBoosts.ContainsKey(normalizedKey) ? normalizedKey : null;
    }

    /// <summary>저장된 AUTO 속도 배율을 지원 값으로 정규화합니다.</summary>
    public static int NormalizeAutoSpeedMultiplier(int multiplier) => multiplier switch
    {
        >= 5 => 5,
        >= 3 => 3,
        >= 2 => 2,
        _ => 1
    };

    /// <summary>활성 가속 부적에 따른 AUTO 공격 간격을 계산합니다.</summary>
    public static TimeSpan AutoAttackInterval(int multiplier) =>
        TimeSpan.FromTicks(DefaultAutoAttackInterval.Ticks / NormalizeAutoSpeedMultiplier(multiplier));

    /// <summary>활성 가속 배율에 맞는 서버 최소 AUTO 요청 간격을 계산합니다.</summary>
    public static TimeSpan MinimumAutomaticAttackIntervalFor(int multiplier) =>
        TimeSpan.FromTicks(AutoAttackInterval(multiplier).Ticks * 9L / 10L);


    /// <summary>격파한 스테이지의 서버 보상을 계산합니다.</summary>
    public static long VictoryReward(int defeatedStage)
    {
        var stage = Math.Max(1, defeatedStage);
        var bossBonus = IsBossStage(stage) ? 250L + (stage * 12L) : 0L;
        return SaturatingAdd(45L + (stage * 32L), bossBonus);
    }

    /// <summary>스테이지가 10의 배수인 보스 구간인지 확인합니다.</summary>
    public static bool IsBossStage(int stage) => stage > 0 && stage % 10 == 0;

    /// <summary>최고 클리어 스테이지를 기준으로 자동공격 해금 여부를 검증합니다.</summary>
    public static bool CanUnlockAutoAttack(int highestClearedStage) =>
        highestClearedStage >= AutoAttackUnlockStage;

    /// <summary>두 정수를 오버플로 없이 더합니다.</summary>
    public static long SaturatingAdd(long left, long right)
    {
        if (right > 0 && left > long.MaxValue - right) return long.MaxValue;
        if (right < 0 && left < long.MinValue - right) return long.MinValue;
        return left + right;
    }

    private static long SaturatingMultiply(long left, long right)
    {
        if (left <= 0 || right <= 0) return 0;
        return left > long.MaxValue / right ? long.MaxValue : left * right;
    }
}

/// <summary>한 번의 서버 데미지 계산 결과입니다.</summary>
public readonly record struct DamageRoll(long Damage, bool IsCritical);
