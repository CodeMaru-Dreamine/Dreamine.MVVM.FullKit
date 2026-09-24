namespace GamePlatform.Application;

/// <summary>확정된 승패를 표현하는 연출용 타격 기록입니다. 전투 판정이나 보상을 변경하지 않습니다.</summary>
public sealed record ArenaReplayStrike(bool AttackerEnemy, int Attacker, int Target, int Damage, int RemainingHp, bool Skill);

public static class ArenaReplayPlan
{
    public static IReadOnlyList<ArenaReplayStrike> Create(bool victory, int allyCount, int enemyCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(allyCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(allyCount, 6);
        ArgumentOutOfRangeException.ThrowIfLessThan(enemyCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(enemyCount, 6);
        var hp = new[] { Enumerable.Repeat(100, allyCount).ToArray(), Enumerable.Repeat(100, enemyCount).ToArray() };
        var winner = victory ? 0 : 1;
        var loser = 1 - winner;
        var strikes = new List<ArenaReplayStrike>();
        for (var round = 0; round < 12; round++)
        {
            // The losing side acts first so the final knockout ends the replay naturally.
            var alive = Enumerable.Range(0, hp[loser].Length).Where(i => hp[loser][i] > 0).ToArray();
            var attacker = alive[round % alive.Length];
            var target = round % hp[winner].Length;
            var damage = Math.Min(12 + round % 3 * 4, Math.Max(0, hp[winner][target] - 12));
            hp[winner][target] -= damage;
            strikes.Add(new(loser == 1, attacker, target, damage, hp[winner][target], round % 3 == 2));
            attacker = round % hp[winner].Length;
            target = round * hp[loser].Length / 12;
            var remainingHits = (int)Math.Ceiling((target + 1) * 12d / hp[loser].Length) - round;
            damage = (int)Math.Ceiling(hp[loser][target] / (double)remainingHits);
            hp[loser][target] -= damage;
            strikes.Add(new(winner == 1, attacker, target, damage, hp[loser][target], hp[loser][target] == 0));
        }
        return strikes;
    }
}
