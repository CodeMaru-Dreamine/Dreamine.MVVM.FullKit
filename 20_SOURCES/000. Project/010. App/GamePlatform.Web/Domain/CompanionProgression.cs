namespace GamePlatform.Domain;

/// <summary>동료가 착용할 수 있는 개인 장비 부위입니다.</summary>
public enum CompanionEquipmentSlot
{
    Weapon,
    Armor,
    Talisman
}

/// <summary>동료 개인 장비의 정적 능력치 정의입니다.</summary>
public sealed record CompanionEquipmentDefinition(
    string ItemId,
    string Name,
    CompanionEquipmentSlot Slot,
    int AttackPercent,
    int HealthPercent,
    int DefensePercent,
    string AssetUrl,
    CompanionWeaponType? WeaponType = null,
    string AssetCssClass = "");

/// <summary>장비 가방에 실제로 존재하는 강화 가능한 개인 장비 한 개입니다.</summary>
public sealed record CompanionEquipmentInstance(
    string InstanceId,
    string ItemId,
    int Level)
{
    public string DisplayName { get; init; } = string.Empty;
    public CompanionEquipmentRarity Rarity { get; init; } = CompanionEquipmentRarity.Rare;
    public int BonusAttackPercent { get; init; }
    public int BonusHealthPercent { get; init; }
    public int BonusDefensePercent { get; init; }
}

/// <summary>개별 개인 장비의 획득 등급입니다.</summary>
public enum CompanionEquipmentRarity
{
    Rare,
    Epic,
    Legendary
}

/// <summary>개별 장비와 정적 정의를 결합한 장비 가방 읽기 모델입니다.</summary>
public sealed record CompanionEquipmentView(
    CompanionEquipmentInstance Instance,
    CompanionEquipmentDefinition Definition)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Instance.DisplayName) ? Definition.Name : Instance.DisplayName;
    public int AttackPercent => Scale(Definition.AttackPercent + Instance.BonusAttackPercent);
    public int HealthPercent => Scale(Definition.HealthPercent + Instance.BonusHealthPercent);
    public int DefensePercent => Scale(Definition.DefensePercent + Instance.BonusDefensePercent);
    public long UpgradeMaterialCost => 5L * (Math.Clamp(Instance.Level, 0, CompanionProgressionRules.MaximumEquipmentLevel) + 1L);
    public long SellGold => 180L + ((long)Instance.Rarity * 120L) + (Math.Max(0, Instance.Level) * 120L);

    private int Scale(int baseValue) => baseValue <= 0 ? 0 : baseValue + Math.Max(0, Instance.Level);
}

/// <summary>동료 외형과 계정 보너스를 함께 제공하는 스킨 정의입니다.</summary>
public sealed record CompanionSkinDefinition(
    string SkinId,
    string Name,
    long PriceC,
    int AttackPercent,
    int HealthPercent,
    int DefensePercent,
    string CssClass,
    string AssetUrl);

/// <summary>Presentation에 전달하는 동료별 성장·장비·스킨 읽기 모델입니다.</summary>
public sealed record CompanionGrowthView(
    HeroDefinition Hero,
    int Level,
    long LevelUpCost,
    int Rank,
    int Shards,
    int RankUpShardCost,
    int AttackPercent,
    int HealthPercent,
    int DefensePercent,
    IReadOnlyList<CompanionEquipmentView> Equipment,
    IReadOnlyList<CompanionSkinDefinition> Skins,
    string ActiveSkinId,
    IReadOnlySet<string> OwnedSkinIds)
{
    /// <summary>현재 선택된 스킨입니다.</summary>
    public CompanionSkinDefinition ActiveSkin =>
        Skins.First(skin => string.Equals(skin.SkinId, ActiveSkinId, StringComparison.OrdinalIgnoreCase));
}

/// <summary>동료 레벨, 등급, 개인 장비와 스킨 능력치를 계산하는 도메인 정책입니다.</summary>
public static class CompanionProgressionRules
{
    // 기존 월영 의장 5장은 배경이 합쳐진 원화라 전투 스프라이트로 사용할 수 없습니다.
    // 투명 컷아웃 자산이 준비될 때까지 구매 효과는 유지하고 캐릭터 기본 스프라이트를 사용합니다.
    private static readonly HashSet<string> MoonRegaliaAssetHeroIds = [];
    // No gameplay star cap; retain only the storage integer boundary.
    public const int MaximumRank = int.MaxValue;
    public const int MaximumEquipmentLevel = 20;
    public const int EquipmentCraftMaterialCost = 20;
    public const int EquipmentInventoryCapacity = 40;
    public const int NormalMonsterEquipmentDropPercent = 22;
    private const string EquipmentAssetRoot = "/images/maru-idle/equipment/personal";
    private static readonly IReadOnlyList<CompanionEquipmentDefinition> EquipmentCatalog =
    [
        new("spirit-weapon", "혼령 장창", CompanionEquipmentSlot.Weapon, 5, 0, 0, $"{EquipmentAssetRoot}/weapon-spear-v1.png", CompanionWeaponType.Spear),
        new("moon-armor", "월백 전포", CompanionEquipmentSlot.Armor, 0, 7, 4, "/images/maru-idle/equipment/personal/moon-armor.png"),
        new("guardian-talisman", "수호 옥패", CompanionEquipmentSlot.Talisman, 2, 3, 2, "/images/maru-idle/equipment/personal/guardian-talisman.png"),
        new("crescent-bow", "월영 각궁", CompanionEquipmentSlot.Weapon, 5, 0, 0, $"{EquipmentAssetRoot}/weapon-bow-v1.png", CompanionWeaponType.Bow),
        new("lotus-staff", "청련 지팡이", CompanionEquipmentSlot.Weapon, 4, 2, 0, $"{EquipmentAssetRoot}/weapon-staff-v1.png", CompanionWeaponType.Staff),
        new("guardian-mace", "현철 철퇴", CompanionEquipmentSlot.Weapon, 4, 0, 2, $"{EquipmentAssetRoot}/weapon-mace-v1.png", CompanionWeaponType.Mace),
        new("shadow-daggers", "암월 쌍단도", CompanionEquipmentSlot.Weapon, 6, 0, 0, $"{EquipmentAssetRoot}/weapon-daggers-v1.png", CompanionWeaponType.Dagger),
        new("celestial-sword", "천명 장검", CompanionEquipmentSlot.Weapon, 6, 0, 0, $"{EquipmentAssetRoot}/weapon-sword-v1.png", CompanionWeaponType.Sword)
    ];

    private static readonly IReadOnlySet<string> StarterEquipmentIds =
        new HashSet<string>(["spirit-weapon", "moon-armor", "guardian-talisman"], StringComparer.OrdinalIgnoreCase);

    /// <summary>동료 레벨업에 필요한 금화를 반환합니다.</summary>
    public static long LevelUpCost(int level)
    {
        var normalized = Math.Clamp(level, 1, 100_000);
        return Math.Min(long.MaxValue / 4, 180L + 42L * normalized * normalized);
    }

    /// <summary>다음 등급에 필요한 전용 조각 수를 반환합니다.</summary>
    public static int RankUpShardCost(int rank) =>
        SaturatingInt((long)Math.Max(1, rank) * 8L);

    /// <summary>동료가 더 승급할 수 있는지 반환합니다.</summary>
    public static bool CanRankUp(int rank) => rank < MaximumRank;

    /// <summary>계정에서 선택할 수 있는 개인 장비 카탈로그입니다.</summary>
    public static IReadOnlyList<CompanionEquipmentDefinition> GetEquipmentCatalog() => EquipmentCatalog;

    /// <summary>신규 계정에 지급하는 최소 장비 세트입니다. 나머지는 전투 드롭으로 획득합니다.</summary>
    public static IReadOnlyList<CompanionEquipmentDefinition> GetStarterEquipmentCatalog() =>
        EquipmentCatalog.Where(item => StarterEquipmentIds.Contains(item.ItemId)).ToArray();

    /// <summary>해당 영웅이 장비의 무기 계열까지 포함해 착용 가능한지 확인합니다.</summary>
    public static bool CanEquip(HeroDefinition hero, CompanionEquipmentDefinition equipment) =>
        equipment.Slot != CompanionEquipmentSlot.Weapon || equipment.WeaponType == hero.WeaponType;

    /// <summary>몬스터 처치 시 개인 장비가 떨어지는지 결정합니다. 보스는 장비를 확정 지급합니다.</summary>
    public static bool ShouldDropEquipment(int defeatedStage, int roll) =>
        GameRules.IsBossStage(defeatedStage)
        || Math.Clamp(roll, 0, 99) < NormalMonsterEquipmentDropPercent;

    /// <summary>서버 난수로 몬스터의 장비 드롭 한 개를 생성합니다.</summary>
    public static CompanionEquipmentInstance CreateDroppedEquipment(
        int defeatedStage,
        int itemRoll,
        int rarityRoll,
        int ownedItemCount)
    {
        var unlockedCatalog = EquipmentCatalog
            .Where(item => item.Slot != CompanionEquipmentSlot.Weapon || item.WeaponType != CompanionWeaponType.Sword)
            .ToArray();
        var itemIndex = Math.Min(unlockedCatalog.Length - 1,
            Math.Clamp(itemRoll, 0, 99) * unlockedCatalog.Length / 100);
        var definition = unlockedCatalog[itemIndex];
        var rarityIndex = rarityRoll >= 97 ? 3 : rarityRoll >= 72 ? 1 + ownedItemCount % 2 : 0;
        var generation = Math.Max(0, defeatedStage / 100) * 4;
        return CreateEquipmentInstance(Guid.NewGuid().ToString("N"), definition.ItemId, 0, rarityIndex + generation);
    }

    /// <summary>획득 순서에 따라 이름·등급·추가 능력치가 다른 개별 장비를 생성합니다.</summary>
    public static CompanionEquipmentInstance CreateEquipmentInstance(
        string instanceId,
        string itemId,
        int level,
        int variantIndex)
    {
        var definition = EquipmentCatalog.FirstOrDefault(item =>
            string.Equals(item.ItemId, itemId, StringComparison.OrdinalIgnoreCase));
        var variants = definition?.Slot switch
        {
            CompanionEquipmentSlot.Weapon => WeaponVariantsFor(definition.WeaponType),
            CompanionEquipmentSlot.Armor => ArmorVariants,
            _ => TalismanVariants
        };
        var safeIndex = Math.Max(0, variantIndex);
        var variant = variants[safeIndex % variants.Length];
        var generation = safeIndex / variants.Length + 1;
        var generationBonus = Math.Min(5, generation - 1);
        var baseName = generation == 1 ? variant.Name : $"{variant.Name} {generation}식";
        return new CompanionEquipmentInstance(instanceId, itemId, Math.Clamp(level, 0, MaximumEquipmentLevel))
        {
            DisplayName = $"{baseName} · 각인 {EquipmentSeal(instanceId):X4}",
            Rarity = variant.Rarity,
            BonusAttackPercent = variant.Attack + generationBonus,
            BonusHealthPercent = variant.Health + generationBonus,
            BonusDefensePercent = variant.Defense + generationBonus
        };
    }

    /// <summary>구형 저장 장비에 누락된 변형 정보를 비파괴 보충합니다.</summary>
    public static CompanionEquipmentInstance NormalizeEquipmentInstance(CompanionEquipmentInstance item, int variantIndex)
    {
        var normalized = item with { Level = Math.Clamp(item.Level, 0, MaximumEquipmentLevel) };
        return string.IsNullOrWhiteSpace(normalized.DisplayName)
            ? CreateEquipmentInstance(normalized.InstanceId, normalized.ItemId, normalized.Level, variantIndex)
            : normalized;
    }

    /// <summary>저장된 개별 장비를 화면과 능력치 계산에 사용할 읽기 모델로 변환합니다.</summary>
    public static IReadOnlyList<CompanionEquipmentView> ResolveInventory(GameProgress progress) =>
        progress.CompanionEquipmentInventory
            .Select(instance => new
            {
                Instance = instance,
                Definition = EquipmentCatalog.FirstOrDefault(item =>
                    string.Equals(item.ItemId, instance.ItemId, StringComparison.OrdinalIgnoreCase))
            })
            .Where(item => item.Definition is not null)
            .Select(item => new CompanionEquipmentView(item.Instance, item.Definition!))
            .OrderBy(item => item.Definition.Slot)
            .ThenByDescending(item => item.Instance.Level)
            .ThenBy(item => item.Instance.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>동료별 기본·유료 스킨 카탈로그를 반환합니다.</summary>
    public static IReadOnlyList<CompanionSkinDefinition> GetSkins(HeroDefinition hero) =>
    [
        new("default", "기본 전투복", 0, 0, 0, 0, "skin-default", hero.SpriteAssetUrl),
        new("moon-regalia", $"{hero.Name} 월영 의장", 300, 8, 10, 6, "skin-moon-regalia",
            MoonRegaliaAssetHeroIds.Contains(hero.HeroId)
                ? $"/images/maru-idle/party/skins/moon-regalia/{hero.HeroId}.png"
                : hero.SpriteAssetUrl)
    ];

    /// <summary>저장 데이터와 정적 카탈로그를 결합해 동료 성장 상태를 계산합니다.</summary>
    public static CompanionGrowthView Resolve(GameProgress progress, HeroDefinition hero)
    {
        var level = progress.CompanionLevels.TryGetValue(hero.HeroId, out var savedLevel)
            ? Math.Max(1, savedLevel)
            : 1;
        var rank = progress.CompanionRanks.TryGetValue(hero.HeroId, out var savedRank)
            ? Math.Clamp(savedRank, 1, MaximumRank)
            : 1;
        var inventory = ResolveInventory(progress);
        var equipment = inventory
            .Where(item => progress.CompanionEquippedItems.TryGetValue(EquipmentKey(hero.HeroId, item.Definition.Slot), out var instanceId)
                           && string.Equals(instanceId, item.Instance.InstanceId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var skins = GetSkins(hero);
        var ownedSkinIds = progress.OwnedCompanionSkins
            .Where(key => key.StartsWith($"{hero.HeroId}:", StringComparison.OrdinalIgnoreCase))
            .Select(key => key[(hero.HeroId.Length + 1)..])
            .Append("default")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeSkinId = progress.ActiveCompanionSkins.TryGetValue(hero.HeroId, out var savedSkin)
                           && ownedSkinIds.Contains(savedSkin)
                           && skins.Any(skin => string.Equals(skin.SkinId, savedSkin, StringComparison.OrdinalIgnoreCase))
            ? savedSkin
            : "default";
        var skin = skins.First(item => string.Equals(item.SkinId, activeSkinId, StringComparison.OrdinalIgnoreCase));
        var levelAttack = Math.Max(0, level - 1);
        var levelHealth = SaturatingInt(Math.Max(0L, (long)level - 1L) * 2L);
        var rankAttack = SaturatingInt(Math.Max(0L, (long)rank - 1) * 3L);
        var rankHealth = SaturatingInt(Math.Max(0L, (long)rank - 1) * 4L);
        var rankDefense = SaturatingInt(Math.Max(0L, (long)rank - 1) * 2L);
        return new CompanionGrowthView(
            hero,
            level,
            LevelUpCost(level),
            rank,
            progress.CompanionShards.TryGetValue(hero.HeroId, out var shards) ? Math.Max(0, shards) : 0,
            RankUpShardCost(rank),
            SaturatingInt((long)hero.AssistPowerPercent + levelAttack + rankAttack + equipment.Sum(item => (long)item.AttackPercent) + skin.AttackPercent),
            SaturatingInt(5L + levelHealth + rankHealth + equipment.Sum(item => (long)item.HealthPercent) + skin.HealthPercent),
            SaturatingInt((long)rankDefense + equipment.Sum(item => (long)item.DefensePercent) + skin.DefensePercent),
            equipment,
            skins,
            activeSkinId,
            ownedSkinIds);
    }

    /// <summary>동료와 부위를 식별하는 저장 키를 반환합니다.</summary>
    public static string EquipmentKey(string heroId, CompanionEquipmentSlot slot) => $"{heroId}:{slot}";

    /// <summary>동료와 스킨을 식별하는 소유권 키를 반환합니다.</summary>
    public static string SkinKey(string heroId, string skinId) => $"{heroId}:{skinId}";

    /// <summary>개인 방어력 보너스를 적용한 실제 피해를 반환합니다.</summary>
    public static long ReduceDamage(long damage, int defensePercent)
    {
        var reduction = Math.Clamp(defensePercent, 0, 75);
        return Math.Max(1L, damage * (100L - reduction) / 100L);
    }

    private static int SaturatingInt(long value) => (int)Math.Clamp(value, 0L, int.MaxValue);

    private sealed record EquipmentVariant(
        string Name,
        CompanionEquipmentRarity Rarity,
        int Attack,
        int Health,
        int Defense);

    private static readonly EquipmentVariant[] WeaponVariants =
    [
        new("청명 혼령검", CompanionEquipmentRarity.Rare, 1, 0, 0),
        new("적월 파혼도", CompanionEquipmentRarity.Epic, 3, 0, 1),
        new("뇌광 추혼검", CompanionEquipmentRarity.Epic, 2, 2, 0),
        new("천뢰 혼령검", CompanionEquipmentRarity.Legendary, 4, 0, 2)
    ];

    private static readonly EquipmentVariant[] SpearVariants =
    [
        new("청명 용린창", CompanionEquipmentRarity.Rare, 1, 0, 0),
        new("적월 파진창", CompanionEquipmentRarity.Epic, 3, 0, 1),
        new("벽뢰 장창", CompanionEquipmentRarity.Epic, 2, 2, 0),
        new("천명 용황창", CompanionEquipmentRarity.Legendary, 4, 0, 2)
    ];

    private static readonly EquipmentVariant[] BowVariants =
    [
        new("월영 각궁", CompanionEquipmentRarity.Rare, 1, 0, 0),
        new("적설 비익궁", CompanionEquipmentRarity.Epic, 3, 1, 0),
        new("벽해 추성궁", CompanionEquipmentRarity.Epic, 2, 2, 0),
        new("천명 일월궁", CompanionEquipmentRarity.Legendary, 5, 0, 1)
    ];

    private static readonly EquipmentVariant[] StaffVariants =
    [
        new("청련 치유장", CompanionEquipmentRarity.Rare, 0, 2, 0),
        new("월백 영목장", CompanionEquipmentRarity.Epic, 2, 3, 0),
        new("벽해 성령장", CompanionEquipmentRarity.Epic, 1, 4, 1),
        new("천명 연화장", CompanionEquipmentRarity.Legendary, 3, 5, 1)
    ];

    private static readonly EquipmentVariant[] MaceVariants =
    [
        new("현철 진압추", CompanionEquipmentRarity.Rare, 1, 0, 1),
        new("적동 파성퇴", CompanionEquipmentRarity.Epic, 3, 0, 2),
        new("뇌명 금강추", CompanionEquipmentRarity.Epic, 2, 2, 3),
        new("천명 수호퇴", CompanionEquipmentRarity.Legendary, 4, 2, 4)
    ];

    private static readonly EquipmentVariant[] DaggerVariants =
    [
        new("암월 쌍단도", CompanionEquipmentRarity.Rare, 2, 0, 0),
        new("적영 추혼인", CompanionEquipmentRarity.Epic, 4, 0, 0),
        new("청뢰 비수", CompanionEquipmentRarity.Epic, 3, 1, 1),
        new("천명 쌍월인", CompanionEquipmentRarity.Legendary, 6, 0, 1)
    ];

    private static readonly EquipmentVariant[] ArmorVariants =
    [
        new("청운 월백포", CompanionEquipmentRarity.Rare, 0, 1, 0),
        new("설월 수호의", CompanionEquipmentRarity.Epic, 0, 3, 1),
        new("현무 철린갑", CompanionEquipmentRarity.Epic, 0, 2, 3),
        new("천명 성광갑", CompanionEquipmentRarity.Legendary, 0, 4, 4)
    ];

    private static readonly EquipmentVariant[] TalismanVariants =
    [
        new("청옥 인연패", CompanionEquipmentRarity.Rare, 1, 0, 0),
        new("월화 수호패", CompanionEquipmentRarity.Epic, 1, 2, 0),
        new("벽해 영혼패", CompanionEquipmentRarity.Epic, 2, 0, 2),
        new("천명 봉황패", CompanionEquipmentRarity.Legendary, 3, 3, 2)
    ];

    private static ushort EquipmentSeal(string instanceId)
    {
        uint hash = 2166136261;
        foreach (var character in instanceId)
        {
            hash ^= character;
            hash *= 16777619;
        }
        return (ushort)(hash & 0xffff);
    }

    private static EquipmentVariant[] WeaponVariantsFor(CompanionWeaponType? weaponType) => weaponType switch
    {
        CompanionWeaponType.Spear => SpearVariants,
        CompanionWeaponType.Bow => BowVariants,
        CompanionWeaponType.Staff => StaffVariants,
        CompanionWeaponType.Mace => MaceVariants,
        CompanionWeaponType.Dagger => DaggerVariants,
        _ => WeaponVariants
    };
}
