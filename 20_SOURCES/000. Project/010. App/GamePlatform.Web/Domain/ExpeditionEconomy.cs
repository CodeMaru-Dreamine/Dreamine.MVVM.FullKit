namespace GamePlatform.Domain;

/// <summary>CodeMaru C로 구매할 수 있는 금화 묶음입니다.</summary>
public sealed record GoldPackDefinition(string PackKey, string Name, long Gold, long PriceC, string AssetUrl);

/// <summary>유료 재화와 일일 원정 임무의 서버 권위 정책입니다.</summary>
public static class ExpeditionEconomy
{
    private static readonly GoldPackDefinition[] GoldPacks =
    [
        new("gold-travel", "원정 금화 주머니", 50_000, 30, "/images/maru-idle/rewards/gold-pouch.svg"),
        new("gold-treasury", "황실 금고 상자", 250_000, 120, "/images/maru-idle/icons-premium/equipment.png"),
        new("gold-vault", "천명 금고", 1_000_000, 400, "/images/maru-idle/icons-premium/forge.png")
    ];

    public static IReadOnlyList<GoldPackDefinition> GetGoldPacks() => GoldPacks;

    public static bool TryGetGoldPack(string packKey, out GoldPackDefinition pack)
    {
        pack = GoldPacks.FirstOrDefault(item =>
            string.Equals(item.PackKey, packKey, StringComparison.OrdinalIgnoreCase))!;
        return pack is not null;
    }
}

public enum DailyMissionRewardKind { Gold, SummonTicket, UpgradeMaterial }

public sealed record DailyMissionDefinition(
    string MissionKey,
    string Name,
    string Description,
    int Target,
    DailyMissionRewardKind RewardKind,
    long RewardAmount,
    string RewardLabel);

public static class DailyMissionPolicy
{
    private static readonly DailyMissionDefinition[] Definitions =
    [
        new("defeat-100", "마물 100기 격파", "오늘 전투에서 마물 100기를 쓰러뜨립니다.", 100, DailyMissionRewardKind.Gold, 5_000, "금화 5,000"),
        new("advance-10", "스테이지 10회 전진", "오늘 새로운 스테이지를 10회 돌파합니다.", 10, DailyMissionRewardKind.SummonTicket, 2, "소환패 2장"),
        new("equipment-3", "개인 장비 3회 강화", "보유하거나 착용 중인 개인 장비를 3회 강화합니다.", 3, DailyMissionRewardKind.UpgradeMaterial, 30, "강화 광석 30개")
    ];

    public static IReadOnlyList<DailyMissionDefinition> GetDefinitions() => Definitions;

    public static string TodayKey(DateTime utcNow)
    {
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Korea Standard Time" : "Asia/Seoul");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), zone).ToString("yyyy-MM-dd");
        }
        catch (TimeZoneNotFoundException) { return utcNow.AddHours(9).ToString("yyyy-MM-dd"); }
    }

    public static void EnsureCurrentDay(GameProgress value, DateTime utcNow)
    {
        var today = TodayKey(utcNow);
        if (string.Equals(value.DailyMissionDateKey, today, StringComparison.Ordinal)) return;
        value.DailyMissionDateKey = today;
        value.DailyDefeatedBaseline = Math.Max(0, value.TotalDefeated);
        value.DailyStageBaseline = Math.Max(0, value.HighestClearedStage);
        value.DailyEquipmentUpgradeCount = 0;
        value.ClaimedDailyMissionKeys = [];
    }

    public static int Progress(GameProgress value, string missionKey) => missionKey switch
    {
        "defeat-100" => (int)Math.Min(int.MaxValue, Math.Max(0L, value.TotalDefeated - value.DailyDefeatedBaseline)),
        "advance-10" => Math.Max(0, value.HighestClearedStage - value.DailyStageBaseline),
        "equipment-3" => Math.Max(0, value.DailyEquipmentUpgradeCount),
        _ => 0
    };

    public static bool TryClaim(GameProgress value, string missionKey, DateTime utcNow, out string message)
    {
        EnsureCurrentDay(value, utcNow);
        var mission = Definitions.FirstOrDefault(item =>
            string.Equals(item.MissionKey, missionKey, StringComparison.OrdinalIgnoreCase));
        if (mission is null) { message = "존재하지 않는 일일 임무입니다."; return false; }
        if (value.ClaimedDailyMissionKeys.Contains(mission.MissionKey, StringComparer.OrdinalIgnoreCase))
        { message = "이미 수령한 임무 보상입니다."; return false; }
        if (Progress(value, mission.MissionKey) < mission.Target)
        { message = "아직 임무 목표를 달성하지 못했습니다."; return false; }

        switch (mission.RewardKind)
        {
            case DailyMissionRewardKind.Gold:
                value.Gold = GameRules.SaturatingAdd(value.Gold, mission.RewardAmount);
                break;
            case DailyMissionRewardKind.SummonTicket:
                value.HeroSummonTickets = GameRules.SaturatingAdd(value.HeroSummonTickets, mission.RewardAmount);
                break;
            case DailyMissionRewardKind.UpgradeMaterial:
                value.CompanionUpgradeMaterials = GameRules.SaturatingAdd(value.CompanionUpgradeMaterials, mission.RewardAmount);
                break;
        }
        value.ClaimedDailyMissionKeys.Add(mission.MissionKey);
        message = $"{mission.Name} 보상으로 {mission.RewardLabel}을(를) 수령했습니다.";
        return true;
    }
}
