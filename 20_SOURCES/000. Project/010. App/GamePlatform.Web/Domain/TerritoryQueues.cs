namespace GamePlatform.Domain;

public static partial class TerritoryRules
{
    public const int MarchBoostFood = 50;
    public static int RecruitmentLimit(TerritoryState state) => state.Buildings[4] * 10;
    public static int MaximumRecruitment(TerritoryState state, int troop) => (int)Math.Max(0, new long[] { RecruitmentLimit(state), Capacity(state) - ReservedTroops(state), state.Food / (20 + troop * 10), state.Wood / 10 }.Min());
    public static int StorageCapacity(TerritoryState state) => (int)Math.Min(ResourceCap, 3000L * state.Buildings[5] * state.Buildings[5]);
    public static int ReservedTroops(TerritoryState state) => state.Troops.Sum() + ActiveMarches(state).Sum(m => m.Troops.Sum()) + HospitalOccupied(state) + (state.Recruitment?.Count ?? 0);
    public static int ConstructionSeconds(TerritoryState state, int index) => state.Buildings[index] * 60;
    public static int RecruitmentSeconds(TerritoryState state, int troop, int count) => Math.Max(10, 20 + troop * 10 - (state.Buildings[4] - 1)) * count;
    public static int TrainingSeconds(TerritoryState state) => state.Training * 90;
    public static int MarchSeconds(int tile) => 60 + Distance(tile) * 30;
    public static DateTime MarchVisualAnchor(TerritoryMarch march) => march.JourneyUpdatedUtc != default ? march.JourneyUpdatedUtc
        : march.StartedUtc != default ? march.StartedUtc : march.ReturnsUtc.AddSeconds(-MarchSeconds(march.Tile));
    public static double MarchProgress(TerritoryMarch march, DateTime now)
    {
        var anchor = MarchVisualAnchor(march);
        var fraction = Math.Clamp((now - anchor).TotalSeconds / Math.Max(.001, (march.ReturnsUtc - anchor).TotalSeconds), 0, 1);
        var previous = Math.Clamp(march.JourneyProgress, 0, 1);
        return previous + (1 - previous) * fraction;
    }
    public static string JobTitle(TerritoryJob job) => job.Kind switch
    {
        "build" => $"{BuildingNames[job.Target]} Lv.{job.TargetLevel} 건설",
        "site-build" => $"{SiteName(job.Target)} Lv.{job.TargetLevel} 건설",
        "recruit" => $"{TroopNames[job.Target]} {job.Count}명 모집",
        "heal" => $"{TroopNames[job.Target]} {job.Count}명 치료",
        _ => $"전군 훈련 Lv.{job.TargetLevel}"
    };

    public static void Settle(TerritoryState state, DateTime now)
    {
        Normalize(state);
        MonsterEnergyRules.Settle(state, now);
        state.SettledUtc ??= now;
        if (now < state.SettledUtc.Value) return;
        var cursor = state.SettledUtc.Value;
        var incomeFrom = cursor > now.AddHours(-8) ? cursor : now.AddHours(-8);
        // Complete in chronological order, including offline. New production starts
        // at completion, never retroactively at queue creation or login.
        while (true)
        {
            var next = new[] { state.Construction?.CompletesUtc, state.AdditionalConstruction?.CompletesUtc, state.Recruitment?.CompletesUtc,
                state.Research?.CompletesUtc, state.Healing?.CompletesUtc }.Concat(ActiveMarches(state).Where(m => !TerritorySharedRules.Managed(m)).Select(m => (DateTime?)m.ReturnsUtc))
                .Where(t => t.HasValue && t.Value <= now).OrderBy(t => t).FirstOrDefault();
            if (next is null) break;
            var eventTime = next.Value < cursor ? cursor : next.Value;
            Produce(state, cursor > incomeFrom ? cursor : incomeFrom, eventTime);
            cursor = eventTime;
            foreach (var build in ActiveConstructions(state).Where(b => b.CompletesUtc <= eventTime).ToArray())
            {
                RemoveConstruction(state, build);
                if (build.Kind == "site-build") state.ProductionSites[build.Target] = build.TargetLevel;
                else state.Buildings[build.Target] = build.TargetLevel;
                Report(state, build.CompletesUtc, $"{JobTitle(build)} 완료");
            }
            if (state.Recruitment is { } recruit && recruit.CompletesUtc <= eventTime)
            {
                state.Recruitment = null;
                state.Troops[recruit.Target] += recruit.Count;
                Report(state, recruit.CompletesUtc, $"{JobTitle(recruit)} 완료 · 병영 배치");
            }
            if (state.Research is { } training && training.CompletesUtc <= eventTime)
            {
                state.Research = null;
                state.Training = training.TargetLevel;
                Report(state, training.CompletesUtc, $"{JobTitle(training)} 완료");
            }
            if (state.Healing is { } healing && healing.CompletesUtc <= eventTime)
            {
                state.Healing = null;
                state.Troops[healing.Target] += healing.Count;
                Report(state, healing.CompletesUtc, $"{TroopNames[healing.Target]} {healing.Count}명 치료 완료 · 주둔군 복귀");
            }
            foreach (var march in ActiveMarches(state).Where(m => !TerritorySharedRules.Managed(m) && m.ReturnsUtc <= eventTime).ToArray())
                ResolveMarch(state, march, eventTime);
        }
        Produce(state, cursor > incomeFrom ? cursor : incomeFrom, now);
        state.SettledUtc = now;
    }

    // Existing balances above the new warehouse cap are preserved, not confiscated.
    private static long StoreResource(long current, long amount, int cap) =>
        current + Math.Min(Math.Max(0, cap - current), Math.Max(0, amount));

    private static void Produce(TerritoryState state, DateTime from, DateTime to)
    {
        if (to <= from) return;
        var values = new[] { state.Food, state.Wood, state.Stone };
        for (var i = 0; i < 3; i++)
        {
            var ticks = (to - from).Ticks * Production(state, i + 1) + state.ProductionRemainders[i];
            values[i] = StoreResource(values[i], ticks / TimeSpan.TicksPerMinute, StorageCapacity(state));
            state.ProductionRemainders[i] = values[i] >= StorageCapacity(state) ? 0 : ticks % TimeSpan.TicksPerMinute;
        }
        state.Food = values[0]; state.Wood = values[1]; state.Stone = values[2];
    }
}
