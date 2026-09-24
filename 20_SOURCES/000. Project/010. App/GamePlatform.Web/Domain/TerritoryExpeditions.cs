namespace GamePlatform.Domain;

public static partial class TerritoryRules
{
    public const int SecondMarchLevel = 5;
    public const int ThirdMarchLevel = 10;
    public static int FreeMarchSlots(TerritoryState state) => state.Buildings[0] >= ThirdMarchLevel ? 3 : state.Buildings[0] >= SecondMarchLevel ? 2 : 1;
    // Server-owned permanent unlock, purchased atomically with CodeMaru C.
    public static int MarchSlots(TerritoryState state) => FreeMarchSlots(state) + (FreeMarchSlots(state) == 3 ? Math.Clamp(state.PurchasedMarchSlots, 0, 2) : 0);
    public static IEnumerable<TerritoryMarch> ActiveMarches(TerritoryState state) =>
        (state.March is null ? Enumerable.Empty<TerritoryMarch>() : new[] { state.March }).Concat(state.AdditionalMarches ?? []);
    public static bool CommanderAway(TerritoryState state, string id) => ActiveMarches(state).Any(m => m.Squads.Any(s => string.Equals(s.CommanderId, id, StringComparison.OrdinalIgnoreCase)));
    private static void NormalizeMarches(TerritoryState state)
    {
        state.AdditionalMarches ??= [];
        state.Battles ??= [];
        var occupied = new HashSet<int>();
        foreach (var march in ActiveMarches(state))
        {
            if (string.IsNullOrEmpty(march.Id)) march.Id = Guid.NewGuid().ToString("N");
            if (march.Slot < 1 || !occupied.Add(march.Slot))
            {
                march.Slot = Enumerable.Range(1, Math.Max(5, occupied.Count + 1)).First(s => !occupied.Contains(s));
                occupied.Add(march.Slot);
            }
        }
        state.Battles = state.Battles.Take(10).ToList();
    }
    private static void AddMarch(TerritoryState state, TerritoryMarch march)
    {
        if (march.Slot <= 0) march.Slot = Enumerable.Range(1, MarchSlots(state)).First(s => !ActiveMarches(state).Any(m => m.Slot == s));
        if (state.March is null) state.March = march;
        else state.AdditionalMarches.Add(march);
    }
    private static void RemoveMarch(TerritoryState state, TerritoryMarch march)
    {
        if (ReferenceEquals(state.March, march))
        {
            state.March = state.AdditionalMarches.FirstOrDefault();
            if (state.March is not null) state.AdditionalMarches.Remove(state.March);
        }
        else state.AdditionalMarches.Remove(march);
    }
}

public sealed class TerritoryBattleRecord
{
    public string? EncounterTitle { get; set; }
    public string? OpponentName { get; set; }
    public string? OpponentAsset { get; set; }
    public string Id { get; set; } = "";
    public int Tile { get; set; }
    public int Slot { get; set; }
    public int ArmyPower { get; set; }
    public int EnemyPower { get; set; }
    public bool Victory { get; set; }
    public int[] Troops { get; set; } = [];
    public int[] Losses { get; set; } = [];
    public int[] Wounded { get; set; } = [];
    public int[] Deaths { get; set; } = [];
    public List<TerritorySquad> Squads { get; set; } = [];
    public DateTime CompletedUtc { get; set; }
}

public static class TerritoryBattleRules
{
    // One shared outcome for presentation, casualties, conquest and stored replays.
    public static TerritoryBattleRecord Snapshot(TerritoryMarch march, int availableBeds = int.MaxValue)
    {
        var battle = new TerritoryBattleRecord()
    {
        Id = march.Id, Tile = march.Tile, Slot = march.Slot,
        ArmyPower = march.Power, EnemyPower = march.EnemyPower > 0 ? march.EnemyPower : TerritoryRules.Defense(march.Tile),
        Victory = march.Power >= (march.EnemyPower > 0 ? march.EnemyPower : TerritoryRules.Defense(march.Tile)),
        Troops = march.Troops.ToArray(), Squads = march.Squads.ToList(),
        Losses = march.Troops.Select(n => (int)Math.Ceiling(n * (march.Power >= (march.EnemyPower > 0 ? march.EnemyPower : TerritoryRules.Defense(march.Tile)) ? .1 : .3))).ToArray(),
        CompletedUtc = march.ReturnsUtc
        };
        battle.Wounded = new int[battle.Losses.Length];
        battle.Deaths = new int[battle.Losses.Length];
        for (var i = 0; i < battle.Losses.Length; i++)
        {
            battle.Wounded[i] = Math.Min(Math.Max(0, availableBeds), (int)Math.Ceiling(battle.Losses[i] * .8));
            availableBeds -= battle.Wounded[i];
            battle.Deaths[i] = battle.Losses[i] - battle.Wounded[i];
        }
        return battle;
    }
    public static double CombatProgress(TerritoryMarch march, DateTime now) => Math.Clamp((TerritoryRules.MarchProgress(march, now) - .45) / .1, 0, 1);
    public static double EnemyHealth(TerritoryBattleRecord battle, double progress) =>
        100 * (1 - Math.Clamp(progress, 0, 1) * (battle.Victory ? 1 : Math.Clamp((double)battle.ArmyPower / Math.Max(1, battle.EnemyPower) * .8, 0, .8)));
    public static double ArmyHealth(TerritoryBattleRecord battle, double progress) =>
        100 * (1 - Math.Clamp(progress, 0, 1) * battle.Losses.Sum() / Math.Max(1d, battle.Troops.Sum()));
}
