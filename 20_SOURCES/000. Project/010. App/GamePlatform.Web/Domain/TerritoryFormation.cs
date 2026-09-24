namespace GamePlatform.Domain;

public sealed class TerritoryFormation
{
    public int Slot { get; set; }
    public List<TerritorySquad> Squads { get; set; } = [];
}

public sealed class TerritorySquad
{
    public int TroopType { get; set; }
    public int Count { get; set; }
    public string CommanderId { get; set; } = "";
    // These values are always replaced from server-owned companion data on departure.
    public string CommanderName { get; set; } = "";
    public int CommandBonus { get; set; }
    public string MarchAssetUrl { get; set; } = "";
    public string MarchSkinId { get; set; } = "default";
    public string MarchSkinClass { get; set; } = "skin-default";
}

public sealed record TerritoryCommander(string Id, string Name, string AssetUrl, int Level, int Rank, int BonusPercent, int TroopCapacity = 130);
public sealed record TerritoryBattlePreview(int BasePower, int EffectivePower, int Defense, int[] EnemyComposition)
{
    public int MatchupPercent => BasePower == 0 ? 0 : (int)Math.Round(100d * EffectivePower / BasePower - 100);
    public string Matchup => MatchupPercent > 0 ? "상성 유리" : MatchupPercent < 0 ? "상성 불리" : "상성 균형";
    public bool Victory => EffectivePower >= Defense;
}

public static partial class TerritoryRules
{
    public static IReadOnlyList<TerritoryCommander> Commanders(GameProgress progress) => PartyRules.AllCompanions
        .Where(h => progress.OwnedCompanionIds.Contains(h.HeroId, StringComparer.OrdinalIgnoreCase))
        .Select(h =>
        {
            var level = Math.Clamp(progress.CompanionLevels.GetValueOrDefault(h.HeroId, 1), 1, 100000);
            var rank = Math.Max(1, progress.CompanionRanks.GetValueOrDefault(h.HeroId, 1));
            var bonus = (int)Math.Min(30, (long)level / 10 + (long)rank * 2 + (int)h.Rarity * 2);
            return new TerritoryCommander(h.HeroId, h.Name, h.SpriteAssetUrl, level, rank, bonus, (int)Math.Min(999, 100L + level * 10L + rank * 20L));
        }).ToArray();

    public static string? ValidateFormation(TerritoryState state, GameProgress? progress, TerritoryFormation? formation)
    {
        if (progress is null || formation?.Squads is null || formation.Squads.Count is < 1 or > 3)
            return "병종별 대장과 파견 병력을 편성하세요.";
        var squads = formation.Squads;
        if (formation.Slot < 0 || formation.Slot > MarchSlots(state)) return "출정 슬롯을 확인하세요.";
        if (formation.Slot > 0 && ActiveMarches(state).Any(m => m.Slot == formation.Slot)) return "선택한 부대가 이미 출정 중입니다.";
        if (squads.Any(s => s is null || s.TroopType is < 0 or > 2 || s.Count is < 1 or > 999))
            return "파견 병종과 수량을 확인하세요.";
        if (squads.Select(s => s.TroopType).Distinct().Count() != squads.Count)
            return "같은 병종은 한 부대에 편성하세요.";
        if (squads.Any(s => s.Count > state.Troops[s.TroopType])) return "주둔 병력보다 많이 파견할 수 없습니다.";
        var roster = Commanders(progress);
        if (squads.Any(s => !roster.Any(c => string.Equals(c.Id, s.CommanderId, StringComparison.OrdinalIgnoreCase))))
            return "보유한 동료를 부대 대장으로 선택하세요.";
        if (squads.Any(s => s.Count > roster.Single(c => string.Equals(c.Id, s.CommanderId, StringComparison.OrdinalIgnoreCase)).TroopCapacity)) return "대장 영웅의 병력 지휘 상한을 초과했습니다.";
        if (squads.Select(s => s.CommanderId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != squads.Count)
            return "한 동료를 여러 부대의 대장으로 중복 배치할 수 없습니다.";
        if (squads.Any(s => CommanderAway(state, s.CommanderId)))
            return "이미 출정 중인 대장입니다. 다른 동료를 선택하세요.";
        return null;
    }

    public static List<TerritorySquad> ResolveSquads(GameProgress progress, TerritoryFormation formation)
    {
        var roster = Commanders(progress);
        return formation.Squads.Select(s =>
        {
            var commander = roster.Single(c => string.Equals(c.Id, s.CommanderId, StringComparison.OrdinalIgnoreCase));
            var hero = PartyRules.AllCompanions.Single(h => h.HeroId == commander.Id);
            var skin = CompanionProgressionRules.Resolve(progress, hero).ActiveSkin;
            return new TerritorySquad { TroopType = s.TroopType, Count = s.Count, CommanderId = commander.Id,
                CommanderName = commander.Name, CommandBonus = commander.BonusPercent,
                MarchAssetUrl = skin.AssetUrl, MarchSkinId = skin.SkinId, MarchSkinClass = skin.CssClass };
        }).ToList();
    }

    // Composition is displayed as percentages. Citadels are balanced; other garrisons
    // have a dominant arm plus a smaller supporting arm. This is stable across reloads.
    public static int[] GarrisonComposition(int tile)
    {
        if (tile == 12) return [0, 0, 0];
        if (tile == 0 || Terrain(tile) == "camp") return [34, 33, 33];
        var dominant = tile % 3;
        var result = new int[3];
        result[dominant] = 80;
        result[(dominant + 1) % 3] = 20;
        return result;
    }

    // Spear beats cavalry; cavalry beats bow; bow beats spear.
    public static int CounterPercent(int attacker, int defender) => attacker == defender ? 100
        : (attacker == 0 && defender == 2) || (attacker == 2 && defender == 1) || (attacker == 1 && defender == 0) ? 125 : 75;

    public static TerritoryBattlePreview PreviewBattle(TerritoryState state, int tile, IEnumerable<TerritorySquad> squads)
    {
        var enemy = GarrisonComposition(tile);
        decimal basePower = 0, effectivePower = 0;
        foreach (var squad in squads)
        {
            if (squad.TroopType is < 0 or > 2 || squad.Count is < 1 or > 999) continue;
            var unitPower = squad.TroopType == 0 ? 10 : squad.TroopType == 1 ? 14 : 22;
            var power = (decimal)squad.Count * unitPower * (10 + EffectiveTraining(state) * 2) / 10
                * (100 + Math.Clamp(squad.CommandBonus, 0, 30)) / 100;
            var matchup = tile == 12 ? 100 : Enumerable.Range(0, 3).Sum(i => enemy[i] * CounterPercent(squad.TroopType, i)) / 100m;
            basePower += power;
            effectivePower += power * matchup / 100;
        }
        return new((int)basePower, (int)effectivePower, Defense(tile), enemy);
    }
}
