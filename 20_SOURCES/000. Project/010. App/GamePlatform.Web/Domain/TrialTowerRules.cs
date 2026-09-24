namespace GamePlatform.Domain;

/// <summary>시련의 탑 한 층에 표시할 전투력과 최초 정복 보상입니다.</summary>
public sealed record TrialTowerFloorDefinition(
    int Floor,
    string Title,
    long EnemyPower,
    int SummonTickets,
    long Gold,
    long UpgradeMaterials,
    bool IsBoss,
    string Element);

/// <summary>무한히 상승하는 시련의 탑 난도와 최초 클리어 보상을 계산합니다.</summary>
public static class TrialTowerRules
{
    public const int RewardMultiplier = 10;
    public const int MaximumNaturalChallengeTickets = 5;
    public const int MaximumStoredChallengeTickets = 99;
    public const int ChallengeTicketPurchaseCost = 20;
    public static readonly TimeSpan ChallengeTicketRecoveryInterval = TimeSpan.FromHours(2);
    private static readonly string[] Titles = ["청동 관문", "유리 회랑", "월영 제단", "뇌화 전각", "천명 옥좌"];
    private static readonly string[] Elements = ["풍", "수", "화", "뇌", "천"];

    /// <summary>다음 도전 가능 층을 반환합니다.</summary>
    public static int NextFloor(int highestClearedFloor) => Math.Max(1, highestClearedFloor + 1);

    /// <summary>지정한 층의 서버 전투 규칙과 최초 정복 보상을 반환합니다.</summary>
    public static TrialTowerFloorDefinition Describe(int floor)
    {
        floor = Math.Max(1, floor);
        var tier = Math.Min(Titles.Length - 1, (floor - 1) / 20);
        var isBoss = floor % 10 == 0;
        var scaled = 18d * Math.Pow(1.085d, floor - 1);
        var enemyPower = scaled >= long.MaxValue ? long.MaxValue : Math.Max(18L, (long)Math.Round(scaled));
        if (isBoss) enemyPower = GameRules.SaturatingAdd(enemyPower, enemyPower / 3);

        var baseTickets = floor % 50 == 0 ? 100
            : floor % 25 == 0 ? 40
            : floor % 10 == 0 ? 20
            : floor % 5 == 0 ? 5
            : 1;
        var baseGold = GameRules.SaturatingAdd(400L, GameRules.SaturatingAdd(floor * 180L, isBoss ? floor * 420L : 0L));
        var baseMaterials = Math.Max(3L, floor / 2L + (isBoss ? floor : 0L));
        var tickets = baseTickets * RewardMultiplier;
        var gold = MultiplyReward(baseGold);
        var materials = MultiplyReward(baseMaterials);
        var title = isBoss ? $"{Titles[tier]} · 수문장" : Titles[tier];
        return new TrialTowerFloorDefinition(floor, title, enemyPower, tickets, gold, materials, isBoss, Elements[tier]);
    }

    private static long MultiplyReward(long value) => value > long.MaxValue / RewardMultiplier
        ? long.MaxValue
        : value * RewardMultiplier;

    /// <summary>계정 전투력과 층 전투력을 비교합니다. 권장 전투력의 25% 미만이면 요행으로 돌파할 수 없습니다.</summary>
    public static int SuccessChance(long playerPower, TrialTowerFloorDefinition floor)
    {
        var ratio = Math.Max(1d, playerPower) / Math.Max(1d, floor.EnemyPower);
        var chance = ratio switch
        {
            < 0.25d => 0d,
            < 0.50d => 2d + ((ratio - 0.25d) / 0.25d * 6d),
            < 0.80d => 8d + ((ratio - 0.50d) / 0.30d * 22d),
            < 1.00d => 30d + ((ratio - 0.80d) / 0.20d * 22d),
            < 1.30d => 52d + ((ratio - 1.00d) / 0.30d * 20d),
            < 2.00d => 72d + ((ratio - 1.30d) / 0.70d * 16d),
            _ => 92d
        };
        if (floor.IsBoss) chance -= 6d;
        return Math.Clamp((int)Math.Round(chance), 0, 92);
    }

    /// <summary>일반 스테이지 진행도의 10%까지는 무과금 성장선으로 보정하고, 그 위는 순수 전투력으로 판정합니다.</summary>
    public static int SuccessChance(long playerPower, TrialTowerFloorDefinition floor, int highestClearedStage)
    {
        var powerChance = SuccessChance(playerPower, floor);
        var comfortableFloor = Math.Max(0, highestClearedStage / 10);
        if (floor.Floor > comfortableFloor) return powerChance;

        var progressionRatio = comfortableFloor / (double)Math.Max(1, floor.Floor);
        var progressionChance = progressionRatio >= 1.25d
            ? 100
            : floor.IsBoss ? 94 : 97;
        return Math.Max(powerChance, progressionChance);
    }

    /// <summary>서버 시각을 기준으로 누적된 자연 회복 도전권을 반영합니다.</summary>
    public static void RefreshChallengeTickets(GameProgress progress, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(progress);
        progress.TrialTowerChallengeTickets = Math.Clamp(
            progress.TrialTowerChallengeTickets,
            0,
            MaximumStoredChallengeTickets);
        if (progress.TrialTowerTicketUpdatedUtc is null || progress.TrialTowerTicketUpdatedUtc > nowUtc)
        {
            progress.TrialTowerTicketUpdatedUtc = nowUtc;
            return;
        }
        if (progress.TrialTowerChallengeTickets >= MaximumNaturalChallengeTickets)
        {
            progress.TrialTowerTicketUpdatedUtc = nowUtc;
            return;
        }

        var elapsed = nowUtc - progress.TrialTowerTicketUpdatedUtc.Value;
        var recovered = Math.Max(0, (int)(elapsed.Ticks / ChallengeTicketRecoveryInterval.Ticks));
        if (recovered == 0) return;
        progress.TrialTowerChallengeTickets = Math.Min(
            MaximumNaturalChallengeTickets,
            progress.TrialTowerChallengeTickets + recovered);
        progress.TrialTowerTicketUpdatedUtc = progress.TrialTowerChallengeTickets >= MaximumNaturalChallengeTickets
            ? nowUtc
            : progress.TrialTowerTicketUpdatedUtc.Value.AddTicks(ChallengeTicketRecoveryInterval.Ticks * recovered);
    }

    /// <summary>다음 자연 회복 완료 시각을 반환합니다. 자연 회복 한도 이상이면 null입니다.</summary>
    public static DateTime? NextChallengeTicketUtc(GameProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (progress.TrialTowerChallengeTickets >= MaximumNaturalChallengeTickets) return null;
        return (progress.TrialTowerTicketUpdatedUtc ?? DateTime.UtcNow) + ChallengeTicketRecoveryInterval;
    }
}
