namespace GamePlatform.Domain;

public static partial class TerritoryRules
{
    public const int CoinsPerMinute = 1;
    public static int MarchSlotCost(int slot) => slot == 4 ? 500 : slot == 5 ? 1000 : 0;
    public static int SiteType(int site) => site / 4 + 1;
    public static int SiteNumber(int site) => site % 4 + 2;
    public static string SiteName(int site) => $"{BuildingNames[SiteType(site)]} {SiteNumber(site)}호";
    public static int SiteUnlockLevel(int site) => new[] { 2, 5, 10, 15 }[site % 4];
    public static (long Wood, long Stone) SiteCost(TerritoryState s, int site) =>
        (Math.Max(1, s.ProductionSites[site]) * SiteNumber(site) * 100L, Math.Max(1, s.ProductionSites[site]) * SiteNumber(site) * 70L);
    public static int SiteSeconds(TerritoryState s, int site) => Math.Max(1, s.ProductionSites[site]) * SiteNumber(site) * 60;
    public static IEnumerable<TerritoryJob> ActiveConstructions(TerritoryState s) => new[] { s.Construction, s.AdditionalConstruction }.OfType<TerritoryJob>();
    public static string? ConstructionBlocked(TerritoryState s, string kind, int target) =>
        ActiveConstructions(s).Any(b => b.Kind == kind && b.Target == target) ? "이 시설은 건설 중입니다."
        : ActiveConstructions(s).Count() >= 2 ? "건설 2개가 진행 중입니다." : null;
    private static void NormalizeDevelopment(TerritoryState s)
    {
        s.ProductionSites = Enumerable.Range(0, 12).Select(i => Math.Clamp(s.ProductionSites?.ElementAtOrDefault(i) ?? 0, 0, 20)).ToArray();
        if (s.Construction is null && s.AdditionalConstruction is not null) { s.Construction = s.AdditionalConstruction; s.AdditionalConstruction = null; }
        if (s.Construction is { } first) first.Slot = first.Slot == 2 ? 2 : 1;
        if (s.AdditionalConstruction is { } second) second.Slot = s.Construction!.Slot == 1 ? 2 : 1;
        foreach (var job in ActiveConstructions(s).Concat(new[] { s.Recruitment, s.Research, s.Healing }.OfType<TerritoryJob>()))
            if (string.IsNullOrEmpty(job.Id)) job.Id = Guid.NewGuid().ToString("N");
        s.PurchasedMarchSlots = Math.Clamp(s.PurchasedMarchSlots, 0, 2);
    }
    private static void AddConstruction(TerritoryState s, TerritoryJob job)
    {
        job.Slot = ActiveConstructions(s).Any(b => b.Slot == 1) ? 2 : 1;
        if (s.Construction is null) s.Construction = job; else s.AdditionalConstruction = job;
    }
    private static void RemoveConstruction(TerritoryState s, TerritoryJob job)
    {
        if (ReferenceEquals(s.Construction, job)) { s.Construction = s.AdditionalConstruction; s.AdditionalConstruction = null; }
        else s.AdditionalConstruction = null;
    }
    private static (bool Success, string Message) BeginSite(TerritoryState s, int site, DateTime now)
    {
        if (site is < 0 or >= 12) return (false, "생산 부지를 선택하세요.");
        if (s.Buildings[0] < SiteUnlockLevel(site)) return (false, $"영주관 Lv.{SiteUnlockLevel(site)}에 개방됩니다.");
        if (ConstructionBlocked(s, "site-build", site) is { } blocked) return (false, blocked);
        var level = s.ProductionSites[site];
        if (level >= 20 || level >= s.Buildings[0]) return (false, "최고 레벨이거나 영주관 확장이 필요합니다.");
        var cost = SiteCost(s, site);
        if (s.Wood < cost.Wood || s.Stone < cost.Stone) return (false, "건설 자원이 부족합니다.");
        s.Wood -= cost.Wood; s.Stone -= cost.Stone;
        AddConstruction(s, new() { Kind = "site-build", Target = site, TargetLevel = level + 1, StartedUtc = now, CompletesUtc = now.AddSeconds(SiteSeconds(s, site)) });
        var message = $"{SiteName(site)} Lv.{level + 1} 건설 시작 · 완료 후 생산량 증가";
        Report(s, now, message); return (true, message);
    }
    private static (bool Success, string Message) PurchaseMarchSlot(TerritoryState s, GameProgress? p, int slot, DateTime now)
    {
        if (p is null || !ReferenceEquals(p.Territory, s)) return (false, "계정 정보를 확인하세요.");
        if (slot is not (4 or 5) || slot != 4 + s.PurchasedMarchSlots) return (false, "이미 개방했거나 이전 슬롯을 먼저 개방해야 합니다.");
        if (FreeMarchSlots(s) < 3) return (false, "영주관 Lv.10에 무료 3슬롯을 먼저 개방하세요.");
        var cost = MarchSlotCost(slot);
        if (p.PremiumCurrency < cost) return (false, $"CodeMaru C {cost}가 필요합니다.");
        p.PremiumCurrency -= cost; s.PurchasedMarchSlots++;
        var message = $"제 {slot} 출정 슬롯 영구 개방 · C {cost} 사용";
        Report(s, now, message); return (true, message);
    }
    // 1/2: stable construction slots, 3: recruitment, 4: training, 5: hospital;
    // 11..15: stable expedition slots. ID prevents a stale dialog from accelerating a replacement job.
    public static int AccelerationTarget(TerritoryJob job) => job.Kind is "build" or "site-build" ? job.Slot : job.Kind == "recruit" ? 3 : job.Kind == "train" ? 4 : 5;
    public static TerritoryJob? AccelerationJob(TerritoryState s, int target) => target switch { 1 or 2 => ActiveConstructions(s).FirstOrDefault(b => b.Slot == target), 3 => s.Recruitment, 4 => s.Research, 5 => s.Healing, _ => null };
    public static (int Cost, double Seconds) AccelerationQuote(DateTime end, DateTime now, int minutes)
    {
        if (minutes is not (0 or 5 or 30)) return (0, 0);
        var seconds = Math.Max(0, (end - now).TotalSeconds);
        if (minutes != 0) seconds = Math.Min(seconds, minutes * 60);
        return ((int)Math.Ceiling(seconds / 60) * CoinsPerMinute, seconds);
    }
    private static (bool Success, string Message) Accelerate(TerritoryState s, GameProgress? p, int target, int minutes, string? id, DateTime now)
    {
        if (p is null || !ReferenceEquals(p.Territory, s) || minutes is not (0 or 5 or 30)) return (false, "가속 요청을 확인하세요.");
        var job = AccelerationJob(s, target);
        var march = target is >= 11 and <= 15 ? ActiveMarches(s).FirstOrDefault(m => m.Slot == target - 10) : null;
        if (string.IsNullOrEmpty(id) || (job?.Id ?? march?.Id) != id) return (false, "작업이 완료되었거나 변경되었습니다. 다시 선택하세요.");
        if (march is not null && TerritorySharedRules.Managed(march)) return (false, "공유 월드 출정은 집결·전투 시간에 맞춰 함께 진행됩니다.");
        var end = job?.CompletesUtc ?? march!.ReturnsUtc;
        var quote = AccelerationQuote(end, now, minutes);
        if (quote.Cost <= 0) return (false, "이미 완료된 작업입니다.");
        if (p.PremiumCurrency < quote.Cost) return (false, $"CodeMaru C {quote.Cost}가 필요합니다.");
        p.PremiumCurrency -= quote.Cost;
        if (job is not null) job.CompletesUtc = quote.Seconds >= (end - now).TotalSeconds ? now : end.AddSeconds(-quote.Seconds);
        else
        {
            march!.JourneyProgress = MarchProgress(march, now); march.JourneyUpdatedUtc = now;
            march.ReturnsUtc = quote.Seconds >= (end - now).TotalSeconds ? now : end.AddSeconds(-quote.Seconds);
        }
        var message = $"{(job is null ? $"제 {march!.Slot} 원정군" : JobTitle(job))} {Math.Ceiling(quote.Seconds)}초 가속 · C {quote.Cost} 사용";
        Report(s, now, message);
        Settle(s, now);
        return (true, message);
    }
}
