namespace GamePlatform.Domain;

/// <summary>계정에 영구 저장되는 비동기 결투장 기록입니다.</summary>
public sealed class ArenaState
{
    public int Rating { get; set; } = ArenaRules.InitialRating;
    public int AttackWins { get; set; }
    public int AttackLosses { get; set; }
    public int DefenseWins { get; set; }
    public int DefenseLosses { get; set; }
    public string DailyDateKey { get; set; } = string.Empty;
    public int DailyChallenges { get; set; }
    public int PurchasedTickets { get; set; }
    public int ReplaySpeed { get; set; } = 1;
    public int MatchmakingRevision { get; set; }
}

/// <summary>비동기 결투장의 매칭·승점·보상 규칙입니다.</summary>
public static class ArenaRules
{
    public const int InitialRating = 1_000;
    public const int DailyChallengeLimit = 10;
    public const int MinimumRating = 100;
    public const int TicketPriceC = 20;
    public const int MaximumPurchasedTickets = 999;

    public static void Normalize(ArenaState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Rating = Math.Max(MinimumRating, state.Rating <= 0 ? InitialRating : state.Rating);
        state.AttackWins = Math.Max(0, state.AttackWins);
        state.AttackLosses = Math.Max(0, state.AttackLosses);
        state.DefenseWins = Math.Max(0, state.DefenseWins);
        state.DefenseLosses = Math.Max(0, state.DefenseLosses);
        state.DailyChallenges = Math.Clamp(state.DailyChallenges, 0, DailyChallengeLimit);
        state.PurchasedTickets = Math.Clamp(state.PurchasedTickets, 0, MaximumPurchasedTickets);
        state.ReplaySpeed = state.ReplaySpeed == 2 ? 2 : 1;
        state.MatchmakingRevision = Math.Max(0, state.MatchmakingRevision);
    }

    public static void EnsureCurrentDay(ArenaState state, DateTime nowUtc)
    {
        Normalize(state);
        var dateKey = DailyDateKey(nowUtc);
        if (string.Equals(state.DailyDateKey, dateKey, StringComparison.Ordinal)) return;
        state.DailyDateKey = dateKey;
        state.DailyChallenges = 0;
    }

    public static string DailyDateKey(DateTime nowUtc)
    {
        var utc = nowUtc.Kind == DateTimeKind.Utc ? nowUtc : nowUtc.ToUniversalTime();
        return utc.AddHours(9).ToString("yyyy-MM-dd");
    }

    public static int WinChance(long attackerPower, long defenderPower)
    {
        attackerPower = Math.Max(1, attackerPower);
        defenderPower = Math.Max(1, defenderPower);
        var ratio = (double)attackerPower / defenderPower;
        var chance = 50d + 42d * Math.Log10(ratio);
        return Math.Clamp((int)Math.Round(chance), 15, 85);
    }

    public static (int WinnerGain, int LoserLoss) RatingChange(int winnerRating, int loserRating)
    {
        winnerRating = Math.Max(MinimumRating, winnerRating);
        loserRating = Math.Max(MinimumRating, loserRating);
        var expected = 1d / (1d + Math.Pow(10d, (loserRating - winnerRating) / 400d));
        var gain = Math.Clamp((int)Math.Round(28d * (1d - expected)), 6, 28);
        return (gain, gain);
    }

    public static long VictoryGold(int highestClearedStage) =>
        Math.Max(5_000L, (long)Math.Max(1, highestClearedStage) * 250L);

    public static string Tier(int rating) => rating switch
    {
        >= 2_100 => "천명",
        >= 1_800 => "대사",
        >= 1_500 => "금강",
        >= 1_200 => "백은",
        _ => "청동"
    };
}
