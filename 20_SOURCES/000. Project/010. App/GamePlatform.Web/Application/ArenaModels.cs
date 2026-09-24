namespace GamePlatform.Application;

/// <summary>결투장에 공개되는 실제 원정대 또는 서버 NPC 한 팀입니다.</summary>
public sealed record ArenaOpponentView(
    int Rank,
    string UserId,
    string DisplayName,
    long PartyPower,
    int Rating,
    string Tier,
    IReadOnlyList<string> CompanionAssetUrls,
    bool IsBot = false)
{
    public int EstimatedWinChance { get; init; } = 50;
    public string MatchupLabel => EstimatedWinChance switch { >= 65 => "우세", >= 55 => "약우세", >= 45 => "호각", > 25 => "열세", _ => "강적" };
    public string MatchupClass => EstimatedWinChance switch { >= 65 => "favorable", >= 55 => "even", >= 45 => "even", > 25 => "unfavorable", _ => "danger" };
}

/// <summary>동일 서버의 결투장 로비 상태입니다.</summary>
public sealed record ArenaLobbyView(
    int Rating,
    string Tier,
    int Rank,
    int AttackWins,
    int AttackLosses,
    int DefenseWins,
    int DefenseLosses,
    int DailyChallenges,
    int DailyChallengeLimit,
    IReadOnlyList<ArenaOpponentView> Opponents,
    IReadOnlyList<ArenaOpponentView> Leaderboard)
{
    public int PurchasedTickets { get; init; }
    public long PremiumCurrency { get; init; }
    public int ReplaySpeed { get; init; } = 1;
    public int MatchmakingRevision { get; init; }
    public int RemainingChallenges => Math.Max(0, DailyChallengeLimit - DailyChallenges) + PurchasedTickets;
    public static ArenaLobbyView Empty { get; } = new(
        1_000, "청동", 1, 0, 0, 0, 0, 0, 10,
        Array.Empty<ArenaOpponentView>(), Array.Empty<ArenaOpponentView>());
}

/// <summary>서버가 판정한 한 번의 비동기 결투 결과입니다.</summary>
public sealed record ArenaBattleResult(
    bool Accepted,
    bool Victory,
    int WinChance,
    int Roll,
    int RatingDelta,
    long GoldReward,
    string Message,
    GameSnapshot State);

public sealed record ArenaTicketPurchaseResult(
    bool Purchased, int Count, long SpentC, string Message, GameSnapshot State);
