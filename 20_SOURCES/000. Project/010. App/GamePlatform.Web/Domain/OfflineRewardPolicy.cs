namespace GamePlatform.Domain;

/// <summary>향후 오프라인 보상 기능의 해금 경계만 정의합니다.</summary>
public static class OfflineRewardPolicy
{
    /// <summary>오프라인 보상 기능을 열 수 있는 최고 클리어 스테이지입니다.</summary>
    public const int UnlockStage = 30;

    /// <summary>한 번에 정산할 수 있는 최대 미접속 시간입니다.</summary>
    public static readonly TimeSpan MaximumAccrual = TimeSpan.FromHours(8);

    /// <summary>보상으로 인정하는 최소 미접속 시간입니다.</summary>
    public static readonly TimeSpan MinimumAccrual = TimeSpan.FromMinutes(1);

    /// <summary>최고 클리어 스테이지가 향후 오프라인 보상 조건을 만족하는지 확인합니다.</summary>
    public static bool IsUnlocked(int highestClearedStage) => highestClearedStage >= UnlockStage;

    /// <summary>최고 기록과 계정 성장도를 사용해 시간당 금화 보상을 계산합니다.</summary>
    public static long GoldPerHour(GameProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var stage = Math.Max(1, progress.HighestClearedStage);
        var attack = GameRules.AttackPower(
            progress.AttackLevel,
            progress.SwordArtLevel,
            progress.WeaponEquipmentLevel);
        return Math.Max(300L, 180L + (stage * 42L) + (attack * 3L));
    }

    /// <summary>실제 전투를 반복하지 않고 경과 시간만으로 이번 정산액을 산출합니다.</summary>
    public static OfflineRewardQuote Quote(GameProgress progress, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var unlocked = IsUnlocked(progress.HighestClearedStage);
        var baseline = progress.LastOfflineSettlementUtc > progress.UpdatedUtc
            ? progress.LastOfflineSettlementUtc
            : progress.UpdatedUtc;
        var elapsed = nowUtc <= baseline ? TimeSpan.Zero : nowUtc - baseline;
        var eligible = elapsed > MaximumAccrual ? MaximumAccrual : elapsed;
        var rate = GoldPerHour(progress);
        var gold = unlocked && eligible >= MinimumAccrual
            ? Math.Max(1L, (long)Math.Floor(rate * eligible.TotalSeconds / 3600d))
            : 0L;
        return new OfflineRewardQuote(unlocked, elapsed, eligible, rate, gold, MaximumAccrual);
    }
}

/// <summary>서버가 산출한 한 번의 방치 보상 정산 명세입니다.</summary>
public sealed record OfflineRewardQuote(
    bool IsUnlocked,
    TimeSpan Elapsed,
    TimeSpan EligibleElapsed,
    long GoldPerHour,
    long Gold,
    TimeSpan MaximumAccrual);
