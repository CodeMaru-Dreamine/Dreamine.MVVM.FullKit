namespace GamePlatform.Domain;

public static partial class TerritoryRules
{
    public const int HospitalIndex = 6;
    public static int HospitalCapacity(TerritoryState state) => state.Buildings[HospitalIndex] * 40;
    public static int HospitalOccupied(TerritoryState state) => state.Wounded.Sum() + (state.Healing?.Count ?? 0);
    public static (long Food, long Wood) HealingCost(int troop, int count) => ((8L + troop * 4) * count, 3L * count);
    public static int HealingSeconds(TerritoryState state, int count) => Math.Max(3, 12 - state.Buildings[HospitalIndex] / 2) * count;
    private static (bool Success, string Message) BeginHealing(TerritoryState state, int troop, int requested, DateTime now)
    {
        if (troop is < 0 or > 2 || requested is not (1 or 10 or 1000)) return (false, "치료 병종과 수량을 확인하세요.");
        if (state.Healing is not null) return (false, "치료 대기열이 사용 중입니다.");
        var count = Math.Min(requested, state.Wounded[troop]);
        if (count == 0) return (false, "치료할 부상병이 없습니다.");
        var cost = HealingCost(troop, count);
        if (state.Food < cost.Food || state.Wood < cost.Wood) return (false, "치료에 필요한 식량 또는 목재가 부족합니다.");
        state.Food -= cost.Food; state.Wood -= cost.Wood;
        state.Wounded[troop] -= count;
        state.Healing = new() { Kind = "heal", Target = troop, Count = count, StartedUtc = now, CompletesUtc = now.AddSeconds(HealingSeconds(state, count)) };
        var message = $"{TroopNames[troop]} 부상병 {count}명 치료 시작 · 완료 후 주둔군 복귀";
        Report(state, now, message);
        return (true, message);
    }
}
