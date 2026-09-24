namespace GamePlatform.Domain;
public static class MonsterEnergyRules
{
    public const int Maximum = 100, AttackCost = 10, RecoverySeconds = 300;
    public static void Settle(TerritoryState state, DateTime now)
    {
        state.MonsterEnergy = Math.Clamp(state.MonsterEnergy, 0, Maximum);
        state.EnergyUpdatedUtc ??= now;
        if (now <= state.EnergyUpdatedUtc) return;
        if (state.MonsterEnergy == Maximum) { state.EnergyUpdatedUtc = now; return; }
        var ticks = (long)(now - state.EnergyUpdatedUtc.Value).TotalSeconds / RecoverySeconds;
        if (ticks <= 0) return;
        state.MonsterEnergy = (int)Math.Min(Maximum, state.MonsterEnergy + ticks);
        state.EnergyUpdatedUtc = state.MonsterEnergy == Maximum ? now : state.EnergyUpdatedUtc.Value.AddSeconds(ticks * RecoverySeconds);
    }
}
