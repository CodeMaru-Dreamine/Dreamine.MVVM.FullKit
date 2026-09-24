namespace GamePlatform.Domain;

/// <summary>영약 농원에서 재배할 수 있는 작물과 보상을 정의합니다.</summary>
public sealed record SpiritCropDefinition(
    string CropKey,
    string Name,
    string Description,
    TimeSpan GrowTime,
    long PlantCost,
    long GoldReward,
    long MaterialReward,
    int SummonTicketReward,
    string AssetUrl,
    string AccentColor);

public sealed record SpiritSeedPackDefinition(
    string PackKey,
    string Name,
    string Description,
    string AccentColor,
    int PremiumCost,
    int DawnDewWeight,
    int MoonOrchidWeight,
    int DragonRootWeight);

public sealed record SpiritFarmHouseDefinition(
    int Tier,
    string Name,
    string Description,
    int GrowTimeReductionPercent,
    long GoldCost,
    long MaterialCost,
    string AssetUrl);

/// <summary>계정에 영구 저장되는 한 경작지의 재배 상태입니다.</summary>
public sealed class SpiritFarmPlotState
{
    public int PlotIndex { get; set; }
    public string CropKey { get; set; } = string.Empty;
    public DateTime PlantedUtc { get; set; }
    public DateTime ReadyUtc { get; set; }
    public bool OwnerFertilized { get; set; }
    public List<string> FertilizedByUserIds { get; set; } = [];
}

public sealed class SpiritFarmSoilState
{
    public int PlotIndex { get; set; }
    public int Vitality { get; set; } = 100;
    public string LastCropKey { get; set; } = string.Empty;
    public int ConsecutiveCrops { get; set; }
}

public sealed class SpiritFarmMetaState
{
    public string DailySeedDateKey { get; set; } = string.Empty;
    public Dictionary<string, int> SeedPacks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> CropSeeds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SpiritFarmSoilState> Soils { get; set; } = [];
    public string DailyFertilizerDateKey { get; set; } = string.Empty;
    public string FriendHelpDateKey { get; set; } = string.Empty;
    public List<string> HelpedFriendIds { get; set; } = [];
    public int FertilizerCount { get; set; }
    public int VerticalRackLevel { get; set; }
    public int HouseTier { get; set; }
}

/// <summary>농원 확장·재배 시간·보상 정책입니다.</summary>
public static class SpiritFarmRules
{
    public const int MinimumLevel = 1;
    public const int MaximumLevel = 5;
    public const int MaximumPlots = 6;
    public const int FertilizerBundleSize = 5;
    public const int FertilizerPremiumCost = 8;

    private static readonly SpiritCropDefinition[] Crops =
    [
        new("dew-leaf", "새벽 이슬초", "짧게 자라 자주 돌보기 좋은 기본 영초", TimeSpan.FromMinutes(1), 300, 1_050, 1, 0, "/images/maru-idle/trials/crop-dew-leaf-v1.png", "#6ff0d5"),
        new("moon-orchid", "월영 난초", "달빛을 머금어 금화와 광석을 고르게 맺습니다.", TimeSpan.FromMinutes(5), 1_800, 6_800, 6, 0, "/images/maru-idle/trials/crop-moon-orchid-v1.png", "#8ebdff"),
        new("dragon-root", "천년 용맥근", "오래 기다린 만큼 큰 보상과 소환패를 수확합니다.", TimeSpan.FromMinutes(20), 6_000, 24_000, 20, 1, "/images/maru-idle/trials/crop-dragon-root-v1.png", "#ffd56a")
    ];

    private static readonly SpiritSeedPackDefinition[] SeedPacks =
    [
        new("basic", "초급 씨앗", "매일 가장 많이 생산되는 기본 씨앗 꾸러미", "#76e4bd", 5, 70, 25, 5),
        new("intermediate", "중급 씨앗", "월영 난초와 용맥근의 확률이 높아진 꾸러미", "#86bfff", 10, 45, 40, 15),
        new("advanced", "고급 씨앗", "희귀 영초를 노리는 최고급 종묘 꾸러미", "#ffd56a", 18, 20, 45, 35)
    ];

    private static readonly SpiritFarmHouseDefinition[] Houses =
    [
        new(0, "노지 농원", "자연의 기운으로 재배하는 기본 농원", 0, 0, 0, ""),
        new(1, "비닐 하우스", "비바람을 막아 재배 시간을 10% 줄입니다.", 10, 12_000, 25, "/images/maru-idle/trials/farm-house-vinyl-v1.png"),
        new(2, "유리 하우스", "영맥 채광을 모아 재배 시간을 20% 줄입니다.", 20, 38_000, 80, "/images/maru-idle/trials/farm-house-glass-v1.png"),
        new(3, "온돌·한지 하우스", "온돌 열기와 한지 습도 조절로 재배 시간을 35% 줄입니다.", 35, 90_000, 180, "/images/maru-idle/trials/farm-house-ondol-v1.png")
    ];

    public static IReadOnlyList<SpiritCropDefinition> GetCrops() => Crops;

    public static IReadOnlyList<SpiritSeedPackDefinition> GetSeedPacks() => SeedPacks;

    public static IReadOnlyList<SpiritFarmHouseDefinition> GetHouses() => Houses;

    public static SpiritCropDefinition? FindCrop(string cropKey) =>
        Crops.FirstOrDefault(item => string.Equals(item.CropKey, cropKey, StringComparison.OrdinalIgnoreCase));

    public static int UnlockedPlots(int farmLevel) => Math.Clamp(farmLevel, MinimumLevel, MaximumLevel) + 1;

    public static long UpgradeCost(int farmLevel) => farmLevel >= MaximumLevel
        ? 0
        : 2_500L * farmLevel * farmLevel;

    public static SpiritFarmSoilState Soil(GameProgress progress, int plotIndex)
    {
        var soil = progress.SpiritFarmMeta.Soils.FirstOrDefault(item => item.PlotIndex == plotIndex);
        if (soil is not null) return soil;
        soil = new SpiritFarmSoilState { PlotIndex = plotIndex };
        progress.SpiritFarmMeta.Soils.Add(soil);
        return soil;
    }

    public static double HouseGrowMultiplier(int houseTier) => houseTier switch
    {
        1 => .90,
        2 => .80,
        3 => .65,
        _ => 1
    };

    public static double SoilGrowMultiplier(int vitality) => 1 + (100 - Math.Clamp(vitality, 40, 100)) / 200d;

    public static double RackYieldMultiplier(int rackLevel) => 1 + Math.Clamp(rackLevel, 0, 3) * .15;

    public static long RackUpgradeGoldCost(int rackLevel) => rackLevel >= 3 ? 0 : 15_000L * (rackLevel + 1);

    public static long RackUpgradeMaterialCost(int rackLevel) => rackLevel >= 3 ? 0 : 30L * (rackLevel + 1);

    public static SpiritFarmHouseDefinition CurrentHouse(int tier) => Houses[Math.Clamp(tier, 0, Houses.Length - 1)];

    public static SpiritFarmHouseDefinition? NextHouse(int tier) => Houses.FirstOrDefault(item => item.Tier == tier + 1);

    public static bool EnsureDailySupply(GameProgress progress, DateTime now)
    {
        Normalize(progress);
        var dateKey = now.ToString("yyyy-MM-dd");
        var changed = false;
        if (!string.Equals(progress.SpiritFarmMeta.DailySeedDateKey, dateKey, StringComparison.Ordinal))
        {
            Add(progress.SpiritFarmMeta.SeedPacks, "basic", 3 + progress.SpiritFarmMeta.VerticalRackLevel);
            if (progress.SpiritFarmLevel >= 2) Add(progress.SpiritFarmMeta.SeedPacks, "intermediate", 1);
            if (progress.SpiritFarmLevel >= 4) Add(progress.SpiritFarmMeta.SeedPacks, "advanced", 1);
            progress.SpiritFarmMeta.DailySeedDateKey = dateKey;
            foreach (var soil in progress.SpiritFarmMeta.Soils) soil.Vitality = Math.Min(100, soil.Vitality + 8);
            changed = true;
        }
        if (!string.Equals(progress.SpiritFarmMeta.DailyFertilizerDateKey, dateKey, StringComparison.Ordinal))
        {
            progress.SpiritFarmMeta.FertilizerCount = Math.Min(9999, progress.SpiritFarmMeta.FertilizerCount + 2);
            progress.SpiritFarmMeta.DailyFertilizerDateKey = dateKey;
            changed = true;
        }
        if (!string.Equals(progress.SpiritFarmMeta.FriendHelpDateKey, dateKey, StringComparison.Ordinal))
        {
            progress.SpiritFarmMeta.FriendHelpDateKey = dateKey;
            progress.SpiritFarmMeta.HelpedFriendIds.Clear();
            changed = true;
        }
        return changed;
    }

    public static string RollCropKey(SpiritSeedPackDefinition pack, int roll)
    {
        var normalized = Math.Clamp(roll, 0, 99);
        if (normalized < pack.DawnDewWeight) return "dew-leaf";
        if (normalized < pack.DawnDewWeight + pack.MoonOrchidWeight) return "moon-orchid";
        return "dragon-root";
    }

    public static void Add(Dictionary<string, int> inventory, string key, int amount)
    {
        if (amount <= 0) return;
        inventory[key] = Math.Max(0, inventory.GetValueOrDefault(key)) + amount;
    }

    public static void Normalize(GameProgress progress)
    {
        progress.SpiritFarmLevel = Math.Clamp(progress.SpiritFarmLevel, MinimumLevel, MaximumLevel);
        progress.SpiritFarmMeta ??= new SpiritFarmMetaState();
        progress.SpiritFarmMeta.SeedPacks = (progress.SpiritFarmMeta.SeedPacks ?? new Dictionary<string, int>())
            .Where(item => SeedPacks.Any(pack => string.Equals(pack.PackKey, item.Key, StringComparison.OrdinalIgnoreCase)) && item.Value > 0)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        progress.SpiritFarmMeta.CropSeeds = (progress.SpiritFarmMeta.CropSeeds ?? new Dictionary<string, int>())
            .Where(item => FindCrop(item.Key) is not null && item.Value > 0)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        progress.SpiritFarmMeta.Soils = (progress.SpiritFarmMeta.Soils ?? [])
            .Where(item => item.PlotIndex >= 0 && item.PlotIndex < MaximumPlots)
            .GroupBy(item => item.PlotIndex)
            .Select(group => group.Last())
            .Select(item => { item.Vitality = Math.Clamp(item.Vitality, 40, 100); item.ConsecutiveCrops = Math.Clamp(item.ConsecutiveCrops, 0, 99); return item; })
            .OrderBy(item => item.PlotIndex)
            .ToList();
        for (var plotIndex = 0; plotIndex < MaximumPlots; plotIndex++)
            if (progress.SpiritFarmMeta.Soils.All(item => item.PlotIndex != plotIndex))
                progress.SpiritFarmMeta.Soils.Add(new SpiritFarmSoilState { PlotIndex = plotIndex });
        progress.SpiritFarmMeta.Soils = progress.SpiritFarmMeta.Soils.OrderBy(item => item.PlotIndex).ToList();
        progress.SpiritFarmMeta.HelpedFriendIds = (progress.SpiritFarmMeta.HelpedFriendIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        progress.SpiritFarmMeta.FertilizerCount = Math.Clamp(progress.SpiritFarmMeta.FertilizerCount, 0, 9999);
        progress.SpiritFarmMeta.VerticalRackLevel = Math.Clamp(progress.SpiritFarmMeta.VerticalRackLevel, 0, 3);
        progress.SpiritFarmMeta.HouseTier = Math.Clamp(progress.SpiritFarmMeta.HouseTier, 0, Houses.Length - 1);
        progress.SpiritFarmPlots = (progress.SpiritFarmPlots ?? [])
            .Where(plot => plot.PlotIndex >= 0
                           && plot.PlotIndex < MaximumPlots
                           && FindCrop(plot.CropKey) is not null
                           && plot.ReadyUtc != default)
            .GroupBy(plot => plot.PlotIndex)
            .Select(group => group.OrderByDescending(plot => plot.PlantedUtc).First())
            .Select(plot => { plot.FertilizedByUserIds = (plot.FertilizedByUserIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList(); return plot; })
            .OrderBy(plot => plot.PlotIndex)
            .ToList();
    }
}
