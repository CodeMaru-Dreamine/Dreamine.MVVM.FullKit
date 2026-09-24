namespace GamePlatform.Domain;

/// <summary>동료 소환에서 지급할 보상 종류입니다.</summary>
public enum CompanionSummonRewardKind
{
    /// <summary>새 동료를 영입합니다.</summary>
    Companion,
    /// <summary>동료 성장에 쓰는 조각을 지급합니다.</summary>
    CompanionShard,
    /// <summary>동료 개인 장비 강화 재료를 지급합니다.</summary>
    UpgradeMaterial,
    /// <summary>일반 게임 금화를 지급합니다.</summary>
    Gold
}

/// <summary>서버가 확정한 한 번의 동료 소환 보상입니다.</summary>
public sealed record CompanionSummonReward(
    CompanionSummonRewardKind Kind,
    long Quantity,
    string DisplayName,
    string? HeroId,
    bool RolledHero,
    bool WasDuplicate,
    CompanionRarity? Rarity = null);

/// <summary>CodeMaru C로 교환하는 소환패 묶음입니다.</summary>
public sealed record SummonTicketPackDefinition(
    string PackKey,
    int Tickets,
    long PriceC,
    string Label);

/// <summary>동료·조각·재료·금화를 선택하고 영웅 미획득 천장을 적용하는 도메인 정책입니다.</summary>
public static class CompanionSummonPolicy
{
    private static readonly IReadOnlyList<SummonTicketPackDefinition> TicketPacks =
    [
        new("ticket-1", 1, 3, "단일 소환"),
        new("ticket-10", 10, 27, "10% 절약"),
        new("ticket-100", 100, 240, "20% 절약")
    ];

    /// <summary>영웅 이상 등급이 확정되는 최대 소환 간격입니다.</summary>
    public const int EpicGuaranteePulls = 50;

    /// <summary>전설 등급이 확정되는 최대 소환 간격입니다.</summary>
    public const int LegendaryGuaranteePulls = 1_000;

    /// <summary>천장과 별개로 적용되는 전설 자연 등장률(0.01% 단위)입니다.</summary>
    public const int LegendaryRateBasisPoints = 50;

    /// <summary>저장되는 영웅 천장 카운터의 최댓값입니다.</summary>
    public const int PityThreshold = EpicGuaranteePulls - 1;

    /// <summary>저장되는 전설 천장 카운터의 최댓값입니다.</summary>
    public const int LegendaryPityThreshold = LegendaryGuaranteePulls - 1;

    /// <summary>소환 화면에 노출할 소환패 상품을 반환합니다.</summary>
    public static IReadOnlyList<SummonTicketPackDefinition> GetTicketPacks() => TicketPacks;

    /// <summary>상품 키로 소환패 묶음을 찾습니다.</summary>
    public static bool TryGetTicketPack(string packKey, out SummonTicketPackDefinition item)
    {
        item = TicketPacks.FirstOrDefault(candidate =>
            string.Equals(candidate.PackKey, packKey, StringComparison.OrdinalIgnoreCase))!;
        return item is not null;
    }

    /// <summary>서버 난수와 보유 동료를 이용해 한 번의 소환 결과를 결정합니다.</summary>
    public static CompanionSummonReward Resolve(
        int epicPity,
        int legendaryPity,
        int rewardRoll,
        int quantityRoll,
        int heroRoll,
        IReadOnlyCollection<string> ownedHeroIds,
        int stage)
    {
        ArgumentNullException.ThrowIfNull(ownedHeroIds);
        var roster = PartyRules.AllCompanions;
        if (roster.Count == 0) throw new InvalidOperationException("동료 카탈로그가 비어 있습니다.");

        rewardRoll = Math.Clamp(rewardRoll, 0, 99);
        quantityRoll = Math.Clamp(quantityRoll, 0, 99);
        heroRoll = Math.Clamp(heroRoll, 0, 99);
        epicPity = Math.Clamp(epicPity, 0, PityThreshold);
        legendaryPity = Math.Clamp(legendaryPity, 0, LegendaryPityThreshold);

        // 두 개의 서버 난수를 결합해 0.01% 단위까지 판정합니다.
        // 전설 0.5%, 영웅 2%, 희귀 9.5%로 전체 동료 등장률은 12%입니다.
        // 1,000회 전설 천장은 자연 확률과 별개로 최악의 연속 실패만 차단합니다.
        var rarityRoll = rewardRoll * 100 + quantityRoll;
        CompanionRarity? rolledRarity = legendaryPity >= LegendaryPityThreshold || rarityRoll < LegendaryRateBasisPoints
            ? CompanionRarity.Legendary
            : epicPity >= PityThreshold || rarityRoll < LegendaryRateBasisPoints + 200
                ? CompanionRarity.Epic
                : rarityRoll < 1_200
                    ? CompanionRarity.Rare
                    : null;

        if (rolledRarity is not null)
        {
            var pool = roster.Where(candidate => candidate.Rarity == rolledRarity.Value).ToArray();
            var hero = pool[heroRoll * pool.Length / 100];
            var duplicate = ownedHeroIds.Contains(hero.HeroId, StringComparer.OrdinalIgnoreCase);
            var shardBase = rolledRarity.Value switch
            {
                CompanionRarity.Legendary => 50,
                CompanionRarity.Epic => 24,
                _ => 8
            };
            var shardVariance = rolledRarity.Value switch
            {
                CompanionRarity.Legendary => quantityRoll / 4,
                CompanionRarity.Epic => quantityRoll / 8,
                _ => quantityRoll / 13
            };
            return duplicate
                ? new(CompanionSummonRewardKind.CompanionShard, shardBase + shardVariance, $"{hero.Name} 조각", hero.HeroId, true, true, hero.Rarity)
                : new(CompanionSummonRewardKind.Companion, 1, hero.Name, hero.HeroId, true, false, hero.Rarity);
        }

        var rarePool = roster.Where(candidate => candidate.Rarity == CompanionRarity.Rare).ToArray();
        var rareHero = rarePool[heroRoll * rarePool.Length / 100];
        if (rarityRoll < 5_000)
            return new(CompanionSummonRewardKind.CompanionShard, 3 + quantityRoll / 17, $"{rareHero.Name} 조각", rareHero.HeroId, false, false, CompanionRarity.Rare);
        if (rarityRoll < 7_800)
            return new(CompanionSummonRewardKind.UpgradeMaterial, 10 + quantityRoll / 5, "동료 장비 광석", null, false, false);

        var baseGold = Math.Max(10L, GameRules.VictoryReward(Math.Max(1, stage)));
        return new(CompanionSummonRewardKind.Gold, GameRules.SaturatingAdd(baseGold, baseGold * (1 + quantityRoll / 20)), "금화", null, false, false);
    }
}
