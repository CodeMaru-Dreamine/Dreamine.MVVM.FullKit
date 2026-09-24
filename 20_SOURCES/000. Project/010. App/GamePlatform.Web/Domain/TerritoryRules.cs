using System.Text.Json;

namespace GamePlatform.Domain;

public sealed class TerritoryState
{
    public long Food { get; set; } = 600;
    public long Wood { get; set; } = 600;
    public long Stone { get; set; } = 400;
    public int[] Buildings { get; set; } = [1, 1, 1, 1, 1, 1, 1];
    public int[] Troops { get; set; } = [10, 5, 0];
    public int Training { get; set; } = 1;
    public int[] Wounded { get; set; } = [0, 0, 0];
    public TerritoryJob? Healing { get; set; }
    public List<int> Revealed { get; set; } = [12, 7, 11, 13, 17];
    public List<int> Claimed { get; set; } = [12];
    public DateTime? SettledUtc { get; set; }
    public TerritoryMarch? March { get; set; }
    public List<TerritoryMarch> AdditionalMarches { get; set; } = [];
    public int PurchasedMarchSlots { get; set; }
    public List<TerritoryBattleRecord> Battles { get; set; } = [];
    public TerritoryFormation? Formation { get; set; }
    public Dictionary<int, TerritoryFormation> FormationPresets { get; set; } = new();
    public int MonsterEnergy { get; set; } = 100;
    public DateTime? EnergyUpdatedUtc { get; set; }
    public TerritoryJob? Construction { get; set; }
    public TerritoryJob? AdditionalConstruction { get; set; }
    public int[] ProductionSites { get; set; } = new int[12];
    public TerritoryJob? Recruitment { get; set; }
    public TerritoryJob? Research { get; set; }
    public long[] ProductionRemainders { get; set; } = [0, 0, 0];
    public List<string> Reports { get; set; } = [];
    public Dictionary<string, string> ResourcePurchaseReceipts { get; set; } = new();
}

public sealed class TerritoryJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Slot { get; set; } = 1;
    public string Kind { get; set; } = "";
    public int Target { get; set; }
    public int TargetLevel { get; set; }
    public int Count { get; set; }
    public DateTime StartedUtc { get; set; }
    public DateTime CompletesUtc { get; set; }
}

public sealed class TerritoryMarch
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Slot { get; set; } = 1;
    public int Tile { get; set; }
    public string Mission { get; set; } = "scout";
    public int[] Troops { get; set; } = [0, 0, 0];
    public int Power { get; set; }
    public DateTime ReturnsUtc { get; set; }
    public DateTime StartedUtc { get; set; }
    public bool Boosted { get; set; }
    public List<TerritorySquad> Squads { get; set; } = [];
    public int BasePower { get; set; }
    public int[] EnemyComposition { get; set; } = [];
    public int EnemyPower { get; set; }
    public double JourneyProgress { get; set; }
    public DateTime JourneyUpdatedUtc { get; set; }
}

/// <summary>개인 PvE 영지. 모든 비용·시간·정복 판정은 서버에서 처리합니다.</summary>
public static partial class TerritoryRules
{
    public const long ResourceCap = 1_000_000;
    public static readonly string[] BuildingNames = ["영주관", "농장", "제재소", "채석장", "병영", "창고", "치료소"];
    public static readonly string[] TroopNames = ["창병", "궁병", "기병"];
    public static TerritoryState Clone(TerritoryState state) => JsonSerializer.Deserialize<TerritoryState>(JsonSerializer.Serialize(state))!;
    public static TerritoryState Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<TerritoryState>(json) ?? new(); }
        catch (JsonException) { return new(); }
    }
    public static void Normalize(TerritoryState state)
    {
        state.Food = Math.Clamp(state.Food, 0, ResourceCap);
        state.Wood = Math.Clamp(state.Wood, 0, ResourceCap);
        state.Stone = Math.Clamp(state.Stone, 0, ResourceCap);
        state.Buildings = Enumerable.Range(0, 7).Select(i => Math.Clamp(state.Buildings?.ElementAtOrDefault(i) ?? 1, 1, 20)).ToArray();
        state.ProductionRemainders = Enumerable.Range(0, 3).Select(i => Math.Clamp(state.ProductionRemainders?.ElementAtOrDefault(i) ?? 0, 0, TimeSpan.TicksPerMinute - 1)).ToArray();
        state.Troops = Enumerable.Range(0, 3).Select(i => Math.Clamp(state.Troops?.ElementAtOrDefault(i) ?? 0, 0, 999)).ToArray();
        state.Wounded = Enumerable.Range(0, 3).Select(i => Math.Clamp(state.Wounded?.ElementAtOrDefault(i) ?? 0, 0, 999)).ToArray();
        state.Training = Math.Clamp(state.Training, 1, 20);
        NormalizeMarches(state);
        NormalizeDevelopment(state);
        state.FormationPresets ??= new();
        state.ResourcePurchaseReceipts ??= new();
        state.Revealed = (state.Revealed ?? []).Where(ValidTile).Append(12).Distinct().ToList();
        state.Claimed = (state.Claimed ?? []).Where(ValidTile).Append(12).Distinct().ToList();
        state.Revealed = state.Revealed.Union(state.Claimed).ToList();
        state.Reports = (state.Reports ?? []).Take(12).ToList();
    }
    public static bool ValidTile(int tile) => tile is >= 0 and < WorldTileCount;
    public static int Distance(int tile) => Math.Abs(TileRow(tile) - 12) + Math.Abs(TileColumn(tile) - 12);
    public static string Terrain(int tile) => tile == 12 ? "home" : tile == 0 || IsRegionalCapital(tile) ? "citadel" : tile % 6 == 0 ? "camp" : tile % 3 == 0 ? "forest" : tile % 3 == 1 ? "quarry" : "field";
    public static string TileName(int tile) => Terrain(tile) switch { "home" => "본성", "citadel" => tile == 0 ? "흑월 왕성" : $"{RegionName(tile)} 왕성", "camp" => "적 전초기지", "forest" => "청죽 숲", "quarry" => "옥석 광산", _ => "비옥한 평야" };
    public static string TileSymbol(int tile) => Terrain(tile) switch { "home" => "城", "citadel" => "王", "camp" => "⚑", "forest" => "♣", "quarry" => "◆", _ => "✿" };
    public static int Defense(int tile) => tile == 0 ? 3200 : IsRegionalCapital(tile) ? Math.Max(3200, Distance(tile) * 700) : Distance(tile) * (Terrain(tile) == "camp" ? 180 : 70);
    public static int EffectiveTraining(TerritoryState state) => Math.Clamp(state.Training, 1, state.Buildings[4]);
    public static int ArmyPower(TerritoryState state) => (state.Troops[0] * 10 + state.Troops[1] * 14 + state.Troops[2] * 22) * (10 + EffectiveTraining(state) * 2) / 10;
    public static int Capacity(TerritoryState state) => 30 + state.Buildings[4] * 30;
    public static IEnumerable<int> Neighbors(int tile)
    {
        var x = TileColumn(tile); var y = TileRow(tile);
        return new[] { TileAt(x, y - 1), TileAt(x - 1, y), TileAt(x + 1, y), TileAt(x, y + 1) }.Where(ValidTile);
    }
    public static (long Wood, long Stone) BuildingCost(TerritoryState state, int index) => (state.Buildings[index] * 100L, state.Buildings[index] * 70L);
    public static int Production(TerritoryState state, int index) => (state.Buildings[index] + state.ProductionSites.Where((_, i) => SiteType(i) == index).Sum()) * 12 + state.Claimed.Count(i => Terrain(i) == (index == 1 ? "field" : index == 2 ? "forest" : "quarry")) * 6;

    private static void ResolveMarch(TerritoryState state, TerritoryMarch march, DateTime now)
    {
        if (now < march.ReturnsUtc) return;
        // Clear before resolving, so reconnecting or refreshing cannot grant the reward twice.
        RemoveMarch(state, march);
        var message = "";
        if (march.Mission == "scout")
        {
            state.Revealed = state.Revealed.Union(Neighbors(march.Tile)).ToList();
            message = $"{TileName(march.Tile)} 정찰 완료 · 인접 지역을 발견했습니다.";
        }
        else if (march.Mission == "gather")
        {
            var amount = march.Troops.Sum() * 8L;
            var terrain = Terrain(march.Tile);
            if (terrain == "forest") state.Wood = StoreResource(state.Wood, amount, StorageCapacity(state));
            else if (terrain == "quarry") state.Stone = StoreResource(state.Stone, amount, StorageCapacity(state));
            else state.Food = StoreResource(state.Food, amount, StorageCapacity(state));
            message = $"{TileName(march.Tile)} 채집 완료 · {(terrain == "forest" ? "목재" : terrain == "quarry" ? "석재" : "식량")} +{amount:N0}";
        }
        else
        {
            var battle = TerritoryBattleRules.Snapshot(march, Math.Max(0, HospitalCapacity(state) - HospitalOccupied(state)));
            state.Battles.Insert(0, battle);
            state.Battles = state.Battles.Take(10).ToList();
            var victory = battle.Victory;
            var lost = 0;
            for (var i = 0; i < 3; i++)
            {
                var loss = battle.Losses[i];
                march.Troops[i] -= loss;
                state.Wounded[i] += battle.Wounded[i];
                lost += loss;
            }
            if (victory)
            {
                if (!state.Claimed.Contains(march.Tile)) state.Claimed.Add(march.Tile);
                state.Revealed = state.Revealed.Union(Neighbors(march.Tile)).ToList();
                state.Wood = StoreResource(state.Wood, 100 * Distance(march.Tile), StorageCapacity(state));
                state.Stone = StoreResource(state.Stone, 60 * Distance(march.Tile), StorageCapacity(state));
            }
            message = $"{TileName(march.Tile)} {(victory ? "정복 성공" : "공략 실패")} · 피해 {lost}명 (부상 {battle.Wounded.Sum()} · 전사 {battle.Deaths.Sum()})";
            if (march.Squads.Count > 0)
                message += $" · 상성 적용 {march.Power:N0} / 수비 {march.EnemyPower:N0} · 대장 {string.Join(", ", march.Squads.Select(s => s.CommanderName))}";
        }
        for (var i = 0; i < 3; i++) state.Troops[i] = Math.Min(999, state.Troops[i] + march.Troops[i]);
        Report(state, now, message + " · 자원 보상은 창고 여유 공간까지 보관");
    }

    public static (bool Success, string Message) Execute(TerritoryState state, string action, int target, int count, DateTime now,
        GameProgress? progress = null, TerritoryFormation? formation = null, string? expectedJobId = null)
    {
        Settle(state, now);
        if (now < state.SettledUtc) return (false, "서버 시간 동기화 후 다시 시도하세요.");
        string message;
        switch (action)
        {
            case "resource-buy": return TerritoryResourceShopRules.Buy(state, progress, target, count, expectedJobId);
            case "purchase-march-slot": return PurchaseMarchSlot(state, progress, target, now);
            case "accelerate": return Accelerate(state, progress, target, count, expectedJobId, now);
            case "site-build": return BeginSite(state, target, now);
            case "heal": return BeginHealing(state, target, count, now);
            case "clear-history": state.Reports.Clear(); state.Battles.Clear(); return (true, "개인 영지 기록을 비웠습니다. 진행 중인 작업과 보상 처리 상태는 유지됩니다.");
            case "save-formation":
                if (target < 1 || target > MarchSlots(state)) return (false, "개방된 부대 슬롯을 선택하세요.");
                if (ValidateFormation(state, progress, formation) is { } presetError) return (false, presetError);
                state.FormationPresets[target] = new() { Slot = target, Squads = formation!.Squads.Select(s => new TerritorySquad { TroopType = s.TroopType, Count = s.Count, CommanderId = s.CommanderId }).ToList() };
                return (true, $"제 {target} 부대 편성을 저장했습니다. 병력은 실제 출정 때 예약됩니다.");
            case "build":
                if (target is < 0 or > 6) return (false, "건물을 선택하세요.");
                if (ConstructionBlocked(state, "build", target) is { } blocked) return (false, blocked);
                var level = state.Buildings[target];
                if (level >= 20) return (false, "최고 건물 레벨입니다.");
                if (target != 0 && level >= state.Buildings[0]) return (false, "영주관을 먼저 확장하세요.");
                var cost = BuildingCost(state, target);
                if (state.Wood < cost.Wood || state.Stone < cost.Stone) return (false, "목재 또는 석재가 부족합니다.");
                state.Wood -= cost.Wood; state.Stone -= cost.Stone;
                AddConstruction(state, new() { Kind = action, Target = target, TargetLevel = level + 1, StartedUtc = now, CompletesUtc = now.AddSeconds(ConstructionSeconds(state, target)) });
                message = $"{BuildingNames[target]} Lv.{level + 1} 건설 시작 · 완료 전에는 기존 시설을 사용합니다.";
                break;
            case "recruit":
                if (target is < 0 or > 2 || count < 1 || count > RecruitmentLimit(state)) return (false, $"1회 최대 모집은 병영 레벨 × 10명({RecruitmentLimit(state)}명)입니다.");
                if (state.Recruitment is not null) return (false, "모집 대기열이 사용 중입니다.");
                if (target == 2 && state.Buildings[4] < 2) return (false, "기병은 병영 Lv.2부터 모집합니다.");
                if (ReservedTroops(state) + count > Capacity(state)) return (false, "출정·모집 인원을 포함해 병영이 가득 찼습니다.");
                if (state.Food < count * (20 + target * 10) || state.Wood < count * 10) return (false, "식량 또는 목재가 부족합니다.");
                state.Food -= count * (20 + target * 10); state.Wood -= count * 10;
                state.Recruitment = new() { Kind = action, Target = target, Count = count, StartedUtc = now, CompletesUtc = now.AddSeconds(RecruitmentSeconds(state, target, count)) };
                message = $"{TroopNames[target]} {count}명 모집 시작 · 훈련 완료 후 병영에 배치됩니다.";
                break;
            case "train":
                if (ActiveMarches(state).Any()) return (false, "모든 원정군 귀환 후 훈련할 수 있습니다.");
                if (state.Research is not null) return (false, "전군 훈련이 진행 중입니다.");
                if (state.Training >= state.Buildings[4] || state.Training >= 20) return (false, "병영 레벨을 먼저 올리세요. 병사 훈련은 병영 레벨을 넘을 수 없습니다.");
                if (state.Food < state.Training * 100 || state.Stone < state.Training * 50) return (false, "훈련 자원이 부족합니다.");
                state.Food -= state.Training * 100; state.Stone -= state.Training * 50;
                state.Research = new() { Kind = action, TargetLevel = state.Training + 1, StartedUtc = now, CompletesUtc = now.AddSeconds(TrainingSeconds(state)) };
                message = $"전군 훈련 Lv.{state.Training + 1} 시작 · 완료 전까지 출정할 수 없습니다.";
                break;
            case "boost":
                var activeMarch = target == 0 ? state.March : ActiveMarches(state).FirstOrDefault(m => m.Slot == target);
                if (activeMarch is null) return (false, "선택한 출정 슬롯에 행군이 없습니다.");
                if (TerritorySharedRules.Managed(activeMarch)) return (false, "공유 월드 부대는 집결·교전 시간에 맞춰 함께 이동합니다.");
                if (activeMarch.Boosted) return (false, "이 출정의 가속을 이미 사용했습니다.");
                if ((activeMarch.ReturnsUtc - now).TotalSeconds <= 5) return (false, "곧 귀환합니다. 가속이 필요하지 않습니다.");
                if (state.Food < MarchBoostFood) return (false, "행군 가속에 식량 50이 필요합니다.");
                state.Food -= MarchBoostFood;
                activeMarch.JourneyProgress = MarchProgress(activeMarch, now);
                activeMarch.JourneyUpdatedUtc = now;
                activeMarch.ReturnsUtc = now.AddTicks((activeMarch.ReturnsUtc - now).Ticks / 2);
                activeMarch.Boosted = true;
                message = "행군 가속 적용 · 남은 시간 50% 단축 · 식량 50 사용";
                break;
            case "scout": case "gather": case "conquer":
                if (!ValidTile(target) || target == 12 || !state.Revealed.Contains(target)) return (false, "발견한 외부 지역을 선택하세요.");
                if (ActiveMarches(state).Count() >= MarchSlots(state)) return (false, "출정 슬롯이 모두 사용 중입니다. 귀환하거나 영주관을 성장시키세요.");
                if (ActiveMarches(state).Any(m => m.Tile == target)) return (false, "이 거점에는 이미 원정군이 출정 중입니다.");
                if (state.Research is not null) return (false, "전군 훈련 완료 후 출정할 수 있습니다.");
                if (state.Troops.Sum() == 0) return (false, "병력을 먼저 모집하세요.");
                if (action == "conquer" && (state.Claimed.Contains(target) || !Neighbors(target).Any(state.Claimed.Contains))) return (false, "내 영지와 인접한 미점령 지역만 정복할 수 있습니다.");
                if (action == "gather" && !state.Claimed.Contains(target)) return (false, "점령한 지역에서만 안전하게 채집할 수 있습니다.");
                if (action == "gather" && Terrain(target) is "camp" or "citadel") return (false, "자원 지대를 선택하세요.");
                if (action == "scout" && Neighbors(target).All(state.Revealed.Contains)) return (false, "이미 주변 정찰을 마쳤습니다.");
                var formationError = ValidateFormation(state, progress, formation);
                if (formationError is not null) return (false, formationError);
                if (state.Food < 10) return (false, "출정 식량 10이 필요합니다.");
                var squads = ResolveSquads(progress!, formation!);
                var preview = PreviewBattle(state, target, squads);
                var dispatched = Enumerable.Range(0, 3).Select(i => squads.Where(s => s.TroopType == i).Sum(s => s.Count)).ToArray();
                if (action == "conquer" && state.MonsterEnergy < MonsterEnergyRules.AttackCost) return (false, "몬스터 공격 에너지가 부족합니다.");
                if (action == "conquer") state.MonsterEnergy -= MonsterEnergyRules.AttackCost;
                state.Food -= 10;
                AddMarch(state, new() { Slot = formation!.Slot, Tile = target, Mission = action, Troops = dispatched, Power = preview.EffectivePower,
                    BasePower = preview.BasePower, EnemyPower = preview.Defense, EnemyComposition = preview.EnemyComposition,
                    Squads = squads, StartedUtc = now, ReturnsUtc = now.AddSeconds(MarchSeconds(target)) });
                state.Formation = new() { Squads = squads.Select(s => new TerritorySquad { TroopType = s.TroopType, Count = s.Count, CommanderId = s.CommanderId }).ToList() };
                for (var i = 0; i < 3; i++) state.Troops[i] -= dispatched[i];
                message = $"{TileName(target)} {dispatched.Sum()}명 출정 · 대장 {string.Join(", ", squads.Select(s => s.CommanderName))} · 왕복 {MarchSeconds(target)}초";
                break;
            default: return (false, "지원하지 않는 영지 명령입니다.");
        }
        Report(state, now, message);
        return (true, message);
    }
    private static void Report(TerritoryState state, DateTime now, string message)
    {
        state.Reports.Insert(0, $"{now.AddHours(9):MM/dd HH:mm} · {message}");
        state.Reports = state.Reports.Take(12).ToList();
    }
}
