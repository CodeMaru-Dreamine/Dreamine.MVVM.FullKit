using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class DifficultyCurveTests
{
    [Fact]
    public void StageOneThroughFifteen_DiligentManualClicksRemainSafelyClearable()
    {
        const int starterAttackLevel = 1;
        const int manualClicksPerSecond = 4;
        var playerHp = GameRules.PlayerMaxHp(starterAttackLevel, 0);

        for (var stage = 1; stage <= 15; stage++)
        {
            var requiredHits = DivideRoundUp(
                GameRules.EnemyMaxHp(stage),
                GameRules.AttackPower(starterAttackLevel));
            var clearSeconds = requiredHits / (double)manualClicksPerSecond;
            var enemyHitsBeforeClear = (long)Math.Floor(
                clearSeconds / GameRules.DefaultEnemyAttackInterval.TotalSeconds);
            var incomingDamage = enemyHitsBeforeClear * GameRules.EnemyAttackDamage(stage, 0, 10);

            Assert.True(incomingDamage < playerHp,
                $"Stage {stage} should be safely clearable by diligent clicking: {incomingDamage}/{playerHp} damage.");
        }
    }

    [Fact]
    public void StageSixteenThroughThirty_GrowthDeficitCanCauseDefeat()
    {
        const int underleveledAttackLevel = 1;
        var playerHp = GameRules.PlayerMaxHp(underleveledAttackLevel, 0);
        var stagesThatCanDefeatUnderleveledPlayer = 0;

        for (var stage = 16; stage <= 30; stage++)
        {
            var requiredHits = DivideRoundUp(
                GameRules.EnemyMaxHp(stage),
                GameRules.AttackPower(underleveledAttackLevel));
            var enemyHitsBeforeClear = (long)Math.Floor(
                requiredHits / GameRules.DefaultEnemyAttackInterval.TotalSeconds);
            var incomingDamage = enemyHitsBeforeClear * GameRules.EnemyAttackDamage(stage, 0, 10);
            if (incomingDamage >= playerHp) stagesThatCanDefeatUnderleveledPlayer++;
        }

        Assert.InRange(stagesThatCanDefeatUnderleveledPlayer, 1, 15);
        Assert.True(GameRules.EnemyAttackDamage(30, 0, 10) > GameRules.EnemyAttackDamage(16, 0, 10));
    }

    [Fact]
    public void DifficultyBands_IncreaseAtConfiguredBoundaries()
    {
        Assert.True(GameRules.EnemyAttackDamage(16, 0, 10) > GameRules.EnemyAttackDamage(15, 0, 10));
        Assert.True(GameRules.EnemyAttackDamage(31, 0, 10) > GameRules.EnemyAttackDamage(29, 0, 10));
        Assert.True(GameRules.EnemyAttackDamage(51, 0, 10) > GameRules.EnemyAttackDamage(49, 0, 10));
    }

    [Fact]
    public void BossStage_HitsHarderThanAdjacentNormalStageWithinSameBand()
    {
        Assert.True(GameRules.EnemyAttackDamage(10, 0, 10) > GameRules.EnemyAttackDamage(9, 0, 10));
        Assert.True(GameRules.EnemyAttackDamage(20, 0, 10) > GameRules.EnemyAttackDamage(19, 0, 10));
    }

    private static long DivideRoundUp(long value, long divisor) => (value + divisor - 1L) / divisor;
}
