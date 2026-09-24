namespace GamePlatform.Domain;

/// <summary>동료 도감과 소환 연출에서 사용하는 콘텐츠 등급입니다.</summary>
public enum CompanionRarity
{
    /// <summary>희귀 등급입니다.</summary>
    Rare = 1,
    /// <summary>영웅 등급입니다.</summary>
    Epic = 2,
    /// <summary>전설 등급입니다.</summary>
    Legendary = 3
}

/// <summary>영웅이 실제 전투에서 사용하는 개인 무기 계열입니다.</summary>
public enum CompanionWeaponType
{
    Sword,
    Spear,
    Bow,
    Staff,
    Mace,
    Dagger
}

/// <summary>보유 동료 편성을 추천할 때 적용하는 전투 목적입니다.</summary>
public enum FormationRecommendationMode
{
    /// <summary>레벨·승급·장비를 제외한 영웅 고유 잠재력입니다.</summary>
    BasePotential,
    /// <summary>현재 성장 상태가 모두 반영된 실제 지원 전투력입니다.</summary>
    CurrentPower,
    /// <summary>빠른 처치와 원거리·기동 역할을 우선하는 탐험 편성입니다.</summary>
    Exploration,
    /// <summary>생존과 호위·지원 역할을 우선하는 시련의 탑 편성입니다.</summary>
    TrialTower
}

/// <summary>교체 가능한 영웅 또는 동료 한 명의 정적 정의입니다.</summary>
public sealed record HeroDefinition(
    string HeroId,
    string Name,
    string Role,
    int UnlockStage,
    int AssistPowerPercent,
    string SpriteAssetUrl,
    int SpriteFocusX,
    int SpriteFocusY,
    CompanionWeaponType WeaponType,
    CompanionRarity Rarity = CompanionRarity.Rare);

/// <summary>현재 사용자 파티와 계정 공용 성장 상태의 읽기 모델입니다.</summary>
public sealed record PartyView(
    HeroDefinition ActiveHero,
    IReadOnlyList<HeroDefinition> Companions,
    IReadOnlyList<HeroDefinition> AvailableCompanions,
    int AssistPowerPercent,
    long InheritedAttackPower,
    long PartyAttackPower,
    string FormationAssetUrl)
{
    /// <summary>현재 편성된 보조 캐릭터 수입니다.</summary>
    public int CompanionCount => Companions.Count;
}

/// <summary>영웅 교체와 무관하게 유지되는 계정 공용 성장 및 파티 편성 정책입니다.</summary>
public static class PartyRules
{
    private static readonly HeroDefinition MainHero =
        new("yeonsu", "연수", "유랑 검객", 1, 0, "/images/maru-idle/swordswoman-idle-v2.png", 50, 50, CompanionWeaponType.Sword);

    private const string CompanionAssetRoot = "/images/maru-idle/party/companions";

    private static readonly IReadOnlyList<HeroDefinition> CompanionRoster =
    [
        // 희귀 10명 — 신규 계정은 앞의 다섯 명만 지급되며 나머지는 소환으로 영입합니다.
        new("haejin", "해진", "청류 창수", 1, 2, $"{CompanionAssetRoot}/haejin-v1.png", 50, 50, CompanionWeaponType.Spear, CompanionRarity.Rare),
        new("seolbi", "설비", "월궁 사수", 1, 2, $"{CompanionAssetRoot}/seolbi-v1.png", 50, 50, CompanionWeaponType.Bow, CompanionRarity.Rare),
        new("harin", "하린", "비연 검객", 1, 2, $"{CompanionAssetRoot}/harin-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Rare),
        new("dowon", "도원", "청풍 도사", 1, 2, $"{CompanionAssetRoot}/dowon-v1.webp", 50, 50, CompanionWeaponType.Staff, CompanionRarity.Rare),
        new("taesan", "태산", "철산 호위", 1, 2, $"{CompanionAssetRoot}/taesan-v1.webp", 50, 50, CompanionWeaponType.Mace, CompanionRarity.Rare),
        new("yeoryeong", "여령", "야행 척후", 1, 2, $"{CompanionAssetRoot}/yeoryeong-v1.webp", 50, 50, CompanionWeaponType.Dagger, CompanionRarity.Rare),
        new("seoryeom", "서렴", "빙결 검수", 1, 3, $"{CompanionAssetRoot}/seoryeom-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Rare),
        new("cheongun", "청운", "풍령 술사", 1, 3, $"{CompanionAssetRoot}/cheongun-v1.webp", 50, 50, CompanionWeaponType.Staff, CompanionRarity.Rare),
        new("bihwa", "비화", "적화 창객", 1, 3, $"{CompanionAssetRoot}/bihwa-v1.webp", 50, 50, CompanionWeaponType.Spear, CompanionRarity.Rare),
        new("sora", "소라", "성월 궁사", 1, 3, $"{CompanionAssetRoot}/sora-v1.webp", 50, 50, CompanionWeaponType.Bow, CompanionRarity.Rare),

        // 영웅 10명 — 기본 확률 2%, 50회 이내 영웅 이상 확정입니다.
        new("muwon", "무원", "부적 치유", 1, 4, $"{CompanionAssetRoot}/muwon-v1.png", 50, 50, CompanionWeaponType.Staff, CompanionRarity.Epic),
        new("gajin", "가진", "철벽 호위", 1, 4, $"{CompanionAssetRoot}/gajin-v1.png", 50, 50, CompanionWeaponType.Mace, CompanionRarity.Epic),
        new("mujin", "무진", "천광 검호", 1, 4, $"{CompanionAssetRoot}/mujin-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Epic),
        new("ryuhon", "류혼", "뇌창 장군", 1, 4, $"{CompanionAssetRoot}/ryuhon-v1.webp", 50, 50, CompanionWeaponType.Spear, CompanionRarity.Epic),
        new("yuhwa", "유화", "영맥 궁희", 1, 5, $"{CompanionAssetRoot}/yuhwa-v1.webp", 50, 50, CompanionWeaponType.Bow, CompanionRarity.Epic),
        new("baekran", "백란", "자영 암객", 1, 5, $"{CompanionAssetRoot}/baekran-v1.webp", 50, 50, CompanionWeaponType.Dagger, CompanionRarity.Epic),
        new("seoryeon", "서련", "운선 책사", 1, 5, $"{CompanionAssetRoot}/seoryeon-v1.webp", 50, 50, CompanionWeaponType.Staff, CompanionRarity.Epic),
        new("taeuk", "태욱", "금갑 검장", 1, 5, $"{CompanionAssetRoot}/taeuk-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Epic),
        new("geumseol", "금설", "청린 궁장", 1, 5, $"{CompanionAssetRoot}/geumseol-v1.webp", 50, 50, CompanionWeaponType.Bow, CompanionRarity.Epic),
        new("hwayeon", "화연", "적련 검무", 1, 5, $"{CompanionAssetRoot}/hwayeon-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Epic),

        // 전설 10명 — 기본 확률 0.5%, 1,000회 이내 전설 확정입니다.
        new("cheongha", "청하", "쌍검 척후", 1, 7, $"{CompanionAssetRoot}/cheongha-v1.png", 50, 50, CompanionWeaponType.Dagger, CompanionRarity.Legendary),
        new("guryong", "구룡", "창천 검제", 1, 7, $"{CompanionAssetRoot}/guryong-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Legendary),
        new("hongryeon", "홍련", "봉황 창후", 1, 7, $"{CompanionAssetRoot}/hongryeon-v1.webp", 50, 50, CompanionWeaponType.Spear, CompanionRarity.Legendary),
        new("wolyeong", "월영", "태음 현녀", 1, 8, $"{CompanionAssetRoot}/wolyeong-v1.webp", 50, 50, CompanionWeaponType.Staff, CompanionRarity.Legendary),
        new("damhwa", "담화", "태양 궁성", 1, 8, $"{CompanionAssetRoot}/damhwa-v1.webp", 50, 50, CompanionWeaponType.Bow, CompanionRarity.Legendary),
        new("heukryong", "흑룡", "현철 패왕", 1, 8, $"{CompanionAssetRoot}/heukryong-v1.webp", 50, 50, CompanionWeaponType.Mace, CompanionRarity.Legendary),
        new("baekho", "백호", "설백 검성", 1, 8, $"{CompanionAssetRoot}/baekho-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Legendary),
        new("seonhwa", "선화", "구미 부적사", 1, 8, $"{CompanionAssetRoot}/seonhwa-v1.webp", 50, 50, CompanionWeaponType.Staff, CompanionRarity.Legendary),
        new("cheonryeong", "천령", "폭뢰 창선", 1, 9, $"{CompanionAssetRoot}/cheonryeong-v1.webp", 50, 50, CompanionWeaponType.Spear, CompanionRarity.Legendary),
        new("yeomra", "염라", "홍련 검후", 1, 9, $"{CompanionAssetRoot}/yeomra-v1.webp", 50, 50, CompanionWeaponType.Sword, CompanionRarity.Legendary)
    ];

    private static readonly IReadOnlyList<string> StarterCompanionRoster =
        ["haejin", "seolbi", "harin", "dowon", "taesan"];

    /// <summary>전투에 편성할 수 있는 보조 캐릭터의 최대 수입니다.</summary>
    public const int MaximumCompanions = 5;

    /// <summary>현재 콘텐츠 카탈로그에 등록된 모든 보조 캐릭터입니다. 편성 최대치와 별도로 계속 확장할 수 있습니다.</summary>
    public static IReadOnlyList<HeroDefinition> AllCompanions => CompanionRoster;

    /// <summary>신규 계정에 지급되는 희귀 등급 원정대입니다.</summary>
    public static IReadOnlyList<string> StarterCompanionIds => StarterCompanionRoster;

    /// <summary>선택한 전투 목적에 가장 알맞은 보유 동료 다섯 명을 추천합니다.</summary>
    public static IReadOnlyList<CompanionGrowthView> RecommendFormation(
        IEnumerable<CompanionGrowthView> companions,
        FormationRecommendationMode mode = FormationRecommendationMode.CurrentPower)
    {
        ArgumentNullException.ThrowIfNull(companions);
        return companions
            .OrderByDescending(item => FormationRecommendationScore(item, mode))
            .ThenByDescending(item => item.Hero.AssistPowerPercent)
            .ThenByDescending(item => item.Hero.Rarity)
            .ThenBy(item => item.Hero.HeroId, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumCompanions)
            .ToArray();
    }

    /// <summary>추천 근거를 UI에서도 같은 값으로 설명할 수 있도록 목적별 점수를 계산합니다.</summary>
    public static int FormationRecommendationScore(
        CompanionGrowthView companion,
        FormationRecommendationMode mode)
    {
        ArgumentNullException.ThrowIfNull(companion);
        return mode switch
        {
            FormationRecommendationMode.BasePotential =>
                companion.Hero.AssistPowerPercent * 100 + (int)companion.Hero.Rarity * 10,
            FormationRecommendationMode.Exploration =>
                companion.Hero.AssistPowerPercent * 40 + ExplorationRoleScore(companion.Hero.WeaponType),
            FormationRecommendationMode.TrialTower =>
                companion.Hero.AssistPowerPercent * 40 + TrialTowerRoleScore(companion.Hero.WeaponType),
            _ => (int)Math.Min(int.MaxValue, (long)companion.AttackPercent * 100 + companion.HealthPercent + companion.DefensePercent)
        };
    }

    private static int ExplorationRoleScore(CompanionWeaponType weaponType) => weaponType switch
    {
        CompanionWeaponType.Dagger => 70,
        CompanionWeaponType.Bow => 60,
        CompanionWeaponType.Spear => 50,
        CompanionWeaponType.Sword => 40,
        CompanionWeaponType.Staff => 20,
        CompanionWeaponType.Mace => 10,
        _ => 0
    };

    private static int TrialTowerRoleScore(CompanionWeaponType weaponType) => weaponType switch
    {
        CompanionWeaponType.Mace => 90,
        CompanionWeaponType.Staff => 80,
        CompanionWeaponType.Sword => 45,
        CompanionWeaponType.Spear => 30,
        CompanionWeaponType.Bow => 20,
        CompanionWeaponType.Dagger => 10,
        _ => 0
    };

    /// <summary>저장된 영웅 ID를 현재 카탈로그의 주인공으로 정규화합니다.</summary>
    public static string NormalizeActiveHeroId(string? heroId) =>
        string.Equals(heroId, MainHero.HeroId, StringComparison.OrdinalIgnoreCase)
            ? MainHero.HeroId
            : MainHero.HeroId;

    /// <summary>최고 클리어 스테이지와 저장된 편성을 이용해 현재 파티를 구성합니다.</summary>
    public static PartyView Resolve(
        int highestClearedStage,
        string? activeHeroId,
        IEnumerable<string>? selectedCompanionIds,
        long inheritedAttackPower,
        IEnumerable<string>? ownedCompanionIds = null,
        GameProgress? progression = null)
    {
        var owned = ownedCompanionIds?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unlocked = CompanionRoster
            .Where(hero => owned is null
                ? highestClearedStage >= hero.UnlockStage
                : owned.Contains(hero.HeroId))
            .ToArray();
        var requested = (selectedCompanionIds ?? Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaximumCompanions)
            .ToArray();
        var requestedHeroes = requested
            .Select(id => unlocked.FirstOrDefault(hero =>
                string.Equals(hero.HeroId, id, StringComparison.OrdinalIgnoreCase)))
            .Where(hero => hero is not null)
            .Cast<HeroDefinition>()
            .DistinctBy(hero => hero.HeroId, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumCompanions)
            .ToArray();
        var selected = requested.Length == 0
            ? unlocked.Take(MaximumCompanions).ToArray()
            : requestedHeroes;
        var assistPercent = progression is null
            ? selected.Sum(hero => hero.AssistPowerPercent)
            : (int)Math.Min(int.MaxValue, selected.Sum(hero => (long)CompanionProgressionRules.Resolve(progression, hero).AttackPercent));
        var partyPower = GameRules.SaturatingAdd(
            inheritedAttackPower,
            (long)Math.Min(long.MaxValue, (decimal)Math.Max(0, inheritedAttackPower) * assistPercent / 100m));
        return new PartyView(
            MainHero,
            selected,
            unlocked,
            assistPercent,
            inheritedAttackPower,
            partyPower,
            CompanionAssetRoot);
    }

    /// <summary>현재 진행도에서 선택 가능한 동료 ID만 남기고 최대 다섯 명으로 제한합니다.</summary>
    public static List<string> NormalizeCompanionIds(
        int highestClearedStage,
        IEnumerable<string>? selectedCompanionIds,
        IEnumerable<string>? ownedCompanionIds = null)
    {
        var resolved = Resolve(highestClearedStage, MainHero.HeroId, selectedCompanionIds, 1L, ownedCompanionIds);
        return resolved.Companions.Select(hero => hero.HeroId).ToList();
    }
}
