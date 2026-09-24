using GamePlatform.Application;
using GamePlatform.Domain;

namespace GamePlatform.Web.Tests;

public sealed class CombatAndAutoTests
{
    [Fact]
    public async Task Stage14_KeepsAutoLocked()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("stage14", value => { value.Stage = 14; value.HighestClearedStage = 13; value.EnemyHp = 100; });
        var session = SessionFactory.Create(store);

        var state = await session.InitializeAsync("stage14");

        Assert.False(state.AutoAttackUnlocked);
        Assert.False(state.AutoAttackEnabled);
    }

    [Fact]
    public async Task FirstStage15Clear_UnlocksAutoExactlyOnce()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("stage15", value => { value.Stage = 15; value.HighestClearedStage = 14; value.AttackLevel = 1_000; value.EnemyHp = 1; });
        var firstSession = SessionFactory.Create(store);
        var initial = await firstSession.InitializeAsync("stage15");
        await CombatTestDriver.DefeatEncounterGuardsAsync(firstSession, initial);

        var first = await firstSession.AttackAsync(AttackSource.Manual);
        var secondSession = SessionFactory.Create(store);
        var restored = await secondSession.InitializeAsync("stage15");

        Assert.True(first.EnemyDefeated);
        Assert.True(first.AutoUnlockedNow);
        Assert.Equal(16, restored.Stage);
        Assert.Equal(15, restored.HighestClearedStage);
        Assert.True(restored.AutoAttackUnlocked);
        Assert.True(restored.ShowAutoUnlockNotice);
    }

    [Fact]
    public async Task AutoOff_RejectsAutomaticAttackWithoutChangingHp()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("auto-off", value => { value.HighestClearedStage = 15; value.EnemyHp = 100; value.AutoAttackEnabled = false; });
        var session = SessionFactory.Create(store);
        var before = await session.InitializeAsync("auto-off");

        var result = await session.AttackAsync(AttackSource.Automatic);

        Assert.False(result.Accepted);
        Assert.Equal(before.EnemyHp, result.State.EnemyHp);
    }

    [Fact]
    public async Task AutoOn_UsesConfiguredIntervalAndAttacks()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("auto-on", value => { value.HighestClearedStage = 15; value.EnemyHp = 500; });
        var session = SessionFactory.Create(store);
        await session.InitializeAsync("auto-on");
        await session.SetAutoAttackAsync(true);
        await using var loop = new AutoAttackLoop();
        var attacks = 0;
        loop.Start(async token =>
        {
            var result = await session.AttackAsync(AttackSource.Automatic, token);
            if (result.Accepted) Interlocked.Increment(ref attacks);
        }, GameRules.DefaultAutoAttackInterval);

        await Task.Delay(1150);
        await loop.StopAsync();

        Assert.Equal(TimeSpan.FromMilliseconds(250), GameRules.DefaultAutoAttackInterval);
        Assert.InRange(attacks, 4, 5);
    }

    [Fact]
    public async Task ManualAndAutomatic_UseSameDamagePolicy()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("manual", value => { value.AttackLevel = 4; value.EnemyHp = 1000; value.HighestClearedStage = 15; });
        await store.SeedAsync("automatic", value => { value.AttackLevel = 4; value.EnemyHp = 1000; value.HighestClearedStage = 15; });
        var manual = SessionFactory.Create(store, 37);
        var automatic = SessionFactory.Create(store, 37);
        await manual.InitializeAsync("manual");
        await automatic.InitializeAsync("automatic");
        await automatic.SetAutoAttackAsync(true);

        var manualResult = await manual.AttackAsync(AttackSource.Manual);
        var automaticResult = await automatic.AttackAsync(AttackSource.Automatic);

        Assert.Equal(manualResult.Damage, automaticResult.Damage);
        Assert.Equal(manualResult.IsCritical, automaticResult.IsCritical);
    }

    [Fact]
    public async Task SwordArtAndEquipmentBonuses_IncreaseActualAttackDamage()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("base-damage", value => value.EnemyHp = 10_000);
        await store.SeedAsync("sword-art-damage", value =>
        {
            value.EnemyHp = 10_000;
            value.SwordArtLevel = 4;
        });
        await store.SeedAsync("equipment-damage", value =>
        {
            value.EnemyHp = 10_000;
            value.EquipmentLevel = 4;
        });
        var baseSession = SessionFactory.Create(store, 50);
        var swordArtSession = SessionFactory.Create(store, 50);
        var equipmentSession = SessionFactory.Create(store, 50);
        await baseSession.InitializeAsync("base-damage");
        await swordArtSession.InitializeAsync("sword-art-damage");
        await equipmentSession.InitializeAsync("equipment-damage");

        var baseHit = await baseSession.AttackAsync(AttackSource.Manual);
        var swordArtHit = await swordArtSession.AttackAsync(AttackSource.Manual);
        var equipmentHit = await equipmentSession.AttackAsync(AttackSource.Manual);

        Assert.True(swordArtHit.Damage > baseHit.Damage);
        Assert.True(equipmentHit.Damage > baseHit.Damage);
        Assert.Equal(
            GameRules.CalculateDamage(1, 50, 4, 0).Damage,
            swordArtHit.Damage);
        Assert.Equal(
            GameRules.CalculateDamage(1, 50, 0, 4).Damage,
            equipmentHit.Damage);
    }

    [Fact]
    public async Task ConcurrentKill_ClampsHpAndPaysStageRewardOnce()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("race", value => { value.Stage = 15; value.HighestClearedStage = 14; value.AttackLevel = 1_000; value.EnemyHp = 1; });
        var first = SessionFactory.Create(store);
        var second = SessionFactory.Create(store);
        var firstState = await first.InitializeAsync("race");
        var secondState = await second.InitializeAsync("race");
        await CombatTestDriver.DefeatEncounterGuardsAsync(first, firstState);
        await CombatTestDriver.DefeatEncounterGuardsAsync(second, secondState);

        var results = await Task.WhenAll(
            first.AttackAsync(AttackSource.Manual),
            second.AttackAsync(AttackSource.Manual));
        var stored = await store.ReadAsync("race");

        Assert.Equal(1, stored.TotalDefeated);
        Assert.Equal(1, results.Count(result => result.EnemyDefeated));
        Assert.Equal(1, results.Count(result => result.AutoUnlockedNow));
        Assert.True(stored.EnemyHp >= 0);
        Assert.Equal(GameRules.VictoryReward(15) + (GameRules.AttackGold(15) * 11), stored.Gold);
    }

    [Fact]
    public async Task VictoryPhase_BlocksAttackUntilStateMachineAdvances()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("transition", value => { value.AttackLevel = 1_000; value.EnemyHp = 1; });
        var session = SessionFactory.Create(store);
        var initial = await session.InitializeAsync("transition");
        await CombatTestDriver.DefeatEncounterGuardsAsync(session, initial);
        var victory = await session.AttackAsync(AttackSource.Manual);

        var blocked = await session.AttackAsync(AttackSource.Manual);

        Assert.True(victory.EnemyDefeated);
        Assert.Equal(CombatPhase.Victory, victory.State.Phase);
        Assert.False(blocked.Accepted);
    }

    [Fact]
    public async Task EnemyAttack_UsesStageDamageAndEquipmentDefense()
    {
        var unarmoredStore = new InMemoryProgressStore();
        var armoredStore = new InMemoryProgressStore();
        await unarmoredStore.SeedAsync("unarmored", value => { value.Stage = 65; value.EnemyHp = GameRules.EnemyMaxHp(65); });
        await armoredStore.SeedAsync("armored", value => { value.Stage = 65; value.EquipmentLevel = 10; value.EnemyHp = GameRules.EnemyMaxHp(65); });
        var unarmored = SessionFactory.Create(unarmoredStore, 10);
        var armored = SessionFactory.Create(armoredStore, 10);
        await unarmored.InitializeAsync("unarmored");
        await armored.InitializeAsync("armored");

        var first = await unarmored.EnemyAttackAsync();
        var second = await armored.EnemyAttackAsync();

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Equal(GameRules.EnemyAttackDamage(65, 0, 10), first.Damage);
        Assert.Equal(GameRules.EnemyAttackDamage(65, 10, 10), second.Damage);
        Assert.True(second.Damage < first.Damage);
    }

    [Fact]
    public async Task PlayerDefeat_ResetsEnemyAndRetriesSameStage()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("defeat", value =>
        {
            value.Stage = 65;
            value.EnemyHp = 123;
        });
        var session = SessionFactory.Create(store, 20);
        await session.InitializeAsync("defeat");
        EnemyAttackResult result;
        do { result = await session.EnemyAttackAsync(); } while (!result.PlayerDefeated);

        var restarted = await session.RestartAfterDefeatAsync();

        Assert.Equal(CombatPhase.Defeat, result.State.Phase);
        Assert.Equal(65, restarted.Stage);
        Assert.Equal(CombatPhase.Fighting, restarted.Phase);
        Assert.Equal(restarted.PlayerMaxHp, restarted.PlayerHp);
        Assert.Equal(GameRules.EnemyMaxHp(65), restarted.EnemyHp);
    }

    [Fact]
    public async Task EnemyDefeat_RestoresPlayerHealthForNextStage()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("recover", value => { value.AttackLevel = 1_000; value.EnemyHp = 1; });
        var session = SessionFactory.Create(store, 10);
        var initial = await session.InitializeAsync("recover");
        await session.EnemyAttackAsync();
        await CombatTestDriver.DefeatEncounterGuardsAsync(session, initial);

        var victory = await session.AttackAsync(AttackSource.Manual);

        Assert.True(victory.EnemyDefeated);
        Assert.Equal(victory.State.PlayerMaxHp, victory.State.PlayerHp);
    }

    [Fact]
    public async Task EveryStage_HasTwoToFiveIndependentGuards()
    {
        var store = new InMemoryProgressStore();
        for (var stage = 1; stage <= 9; stage++)
        {
            await store.SeedAsync($"normal-guards-{stage}", value =>
            {
                value.Stage = stage;
                value.HighestClearedStage = Math.Max(0, stage - 1);
                value.EnemyHp = GameRules.EnemyMaxHp(stage);
            });

            var session = SessionFactory.Create(store, 50);
            var state = await session.InitializeAsync($"normal-guards-{stage}");

            Assert.InRange(state.BossGuardUnits.Count, 2, 5);
            Assert.All(state.BossGuardUnits, unit =>
            {
                Assert.True(unit.IsAlive);
                Assert.Equal(unit.MaxHp, unit.Hp);
                Assert.True(unit.MaxHp > 0);
            });
        }
    }

    [Fact]
    public async Task BossGuards_HaveIndependentHealthAndProtectBossUntilDefeated()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("boss-guards", value =>
        {
            value.Stage = 10;
            value.HighestClearedStage = 9;
            value.AttackLevel = 100;
            value.EnemyHp = GameRules.EnemyMaxHp(10);
        });
        var session = SessionFactory.Create(store, 50);
        var initial = await session.InitializeAsync("boss-guards");
        await session.AdvanceCombatPhaseAsync();

        var attacks = new List<AttackResult>();
        for (var index = 0; index < 5; index++)
        {
            attacks.Add(await session.AttackAsync(AttackSource.Manual));
            if (index < 4)
                await Task.Delay(GameRules.MinimumManualAttackInterval + TimeSpan.FromMilliseconds(10));
        }

        Assert.Equal(5, initial.BossGuardUnits.Count(unit => unit.IsAlive));
        Assert.All(attacks, attack => Assert.StartsWith("guard-", attack.TargetUnitId));
        Assert.All(attacks, attack => Assert.Equal(initial.EnemyHp, attack.State.EnemyHp));
        Assert.DoesNotContain(attacks[^1].State.BossGuardUnits, unit => unit.IsAlive);
    }

    [Fact]
    public async Task DefeatedCompanion_ReturnsAtFullHealthOnNextStage()
    {
        var store = new InMemoryProgressStore();
        await store.SeedAsync("companion-return", value =>
        {
            value.Stage = 65;
            value.HighestClearedStage = 64;
            value.AttackLevel = 1_000;
            value.EnemyHp = 1;
            value.HighestSummonRewardedStage = 64;
            value.OwnedCompanionIds = PartyRules.AllCompanions.Select(hero => hero.HeroId).ToList();
            value.SelectedCompanionIds = value.OwnedCompanionIds.Take(PartyRules.MaximumCompanions).ToList();
        });
        var session = SessionFactory.Create(store, 20);
        var initial = await session.InitializeAsync("companion-return");

        GameSnapshot damaged = initial;
        while (damaged.CompanionUnits.All(unit => unit.IsAlive))
            damaged = (await session.EnemyAttackAsync()).State;

        await CombatTestDriver.DefeatEncounterGuardsAsync(session, damaged);
        var victory = await session.AttackAsync(AttackSource.Manual);

        Assert.Equal(5, initial.CompanionUnits.Count(unit => unit.IsAlive));
        Assert.Equal(4, damaged.CompanionUnits.Count(unit => unit.IsAlive));
        Assert.True(victory.EnemyDefeated);
        Assert.Equal(66, victory.State.Stage);
        Assert.Equal(5, victory.State.CompanionUnits.Count(unit => unit.IsAlive));
        Assert.All(victory.State.CompanionUnits, unit => Assert.Equal(unit.MaxHp, unit.Hp));
    }

    [Fact]
    public async Task DisposedAutoLoop_LeavesNoRunningWork()
    {
        var loop = new AutoAttackLoop();
        var count = 0;
        loop.Start(_ => { Interlocked.Increment(ref count); return Task.CompletedTask; }, TimeSpan.FromMilliseconds(20));
        await Task.Delay(65);

        await loop.DisposeAsync();
        var stoppedAt = count;
        await Task.Delay(60);

        Assert.False(loop.IsRunning);
        Assert.Equal(stoppedAt, count);
    }

    [Fact]
    public async Task IntervalChangedInsideAttack_KeepsAutoLoopRunning()
    {
        await using var loop = new AutoAttackLoop();
        var attacks = 0;
        loop.Start(_ =>
        {
            if (Interlocked.Increment(ref attacks) == 1)
                loop.Start(_ =>
                {
                    Interlocked.Increment(ref attacks);
                    return Task.CompletedTask;
                }, TimeSpan.FromMilliseconds(15));
            return Task.CompletedTask;
        }, TimeSpan.FromMilliseconds(20));

        await Task.Delay(95);
        await loop.StopAsync();

        Assert.False(loop.IsRunning);
        Assert.True(attacks >= 4);
    }

    [Fact]
    public async Task TransientAttackFailure_DoesNotStopAutoLoop()
    {
        await using var loop = new AutoAttackLoop();
        var attempts = 0;
        loop.Start(_ =>
        {
            var current = Interlocked.Increment(ref attempts);
            if (current == 1) throw new InvalidOperationException("temporary browser failure");
            return Task.CompletedTask;
        }, TimeSpan.FromMilliseconds(15));

        await Task.Delay(85);
        await loop.StopAsync();

        Assert.False(loop.IsRunning);
        Assert.True(attempts >= 3);
    }

    [Fact]
    public async Task StartWhilePreviousLoopIsStopping_RearmsAutoLoop()
    {
        await using var loop = new AutoAttackLoop();
        var firstAttackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstAttack = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attacks = 0;
        loop.Start(async _ =>
        {
            if (Interlocked.Increment(ref attacks) != 1) return;
            firstAttackEntered.TrySetResult();
            await releaseFirstAttack.Task;
        }, TimeSpan.FromMilliseconds(10));

        await firstAttackEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));
        var stopping = loop.StopAsync();
        loop.Start(_ =>
        {
            Interlocked.Increment(ref attacks);
            return Task.CompletedTask;
        }, TimeSpan.FromMilliseconds(10));
        releaseFirstAttack.TrySetResult();

        await stopping;
        await Task.Delay(65);

        Assert.True(loop.IsRunning);
        Assert.True(attacks >= 2);
    }

    [Fact]
    public async Task CallbackCanStopItsOwnLoopWithoutDeadlock()
    {
        await using var loop = new AutoAttackLoop();
        var stoppedInsideCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loop.Start(async _ =>
        {
            await loop.StopAsync();
            stoppedInsideCallback.TrySetResult();
        }, TimeSpan.FromMilliseconds(10));

        await stoppedInsideCallback.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await Task.Delay(30);

        Assert.False(loop.IsRunning);
    }
}
