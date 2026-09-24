using GamePlatform.Application;
using GamePlatform.Domain;
using GamePlatform.Infrastructure;
using GamePlatform.Options;

namespace GamePlatform.Web.Tests;

public class TerritorySharedWorldTests
{
    private static readonly DateTime Now = new(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc);
    private static GameProgress Player(string id) => new() { UserId = id, GameNickname = id, PremiumCurrency = 1000,
        OwnedCompanionIds = PartyRules.StarterCompanionIds.ToList(),
        Territory = new() { Buildings = [10,1,1,1,5,10,5], Troops = [100,100,100], Food = 10000, Wood = 10000, Stone = 10000, Training = 5 } };
    private static TerritoryFormation Formation(GameProgress p, int count = 100) => new() { Squads = [new() { TroopType = 0, Count = count, CommanderId = TerritoryRules.Commanders(p)[0].Id }] };
    private static (TerritorySharedWorld World, Dictionary<string, GameProgress> Players) Setup()
    {
        var world = new TerritorySharedWorld(); var players = new Dictionary<string, GameProgress> { ["a"] = Player("a"), ["b"] = Player("b") };
        foreach (var p in players.Values) TerritorySharedRules.Execute(world, players, p.UserId, "load", null, 0, null, Now);
        return (world, players);
    }
    [Fact]
    public void InitialShieldIs72HoursAndPvpRequiresLevel10OnBothSides()
    {
        var (w,p) = Setup(); Assert.Equal(Now.AddHours(72), w.Members["a"].ShieldUntilUtc);
        Assert.NotNull(TerritorySharedRules.AttackBlocked(w.Members["a"],w.Members["b"],Now));
        w.Members["b"].ShieldUntilUtc = Now; w.Members["a"].Level = 9;
        Assert.NotNull(TerritorySharedRules.AttackBlocked(w.Members["a"],w.Members["b"],Now));
        w.Members["a"].Level = 10; w.Members["b"].Level = 9;
        Assert.NotNull(TerritorySharedRules.AttackBlocked(w.Members["a"],w.Members["b"],Now));
        w.Members["b"].Level = 10; Assert.Null(TerritorySharedRules.AttackBlocked(w.Members["a"],w.Members["b"],Now));
        Assert.NotEqual(w.Members["a"].Tile,w.Members["b"].Tile);
    }
    [Fact]
    public void RallyReservesRealTroopsAndSettlesExactlyOnce()
    {
        var (w,p) = Setup();
        Assert.True(TerritorySharedRules.Execute(w,p,"a","rally-create",null,0,Formation(p["a"]),Now).Success);
        var rally = w.Rallies.Single();
        Assert.True(TerritorySharedRules.Execute(w,p,"b","rally-join",rally.Id,0,Formation(p["b"]),Now).Success);
        Assert.Equal(0,p["a"].Territory.Troops[0]); Assert.Equal(90,p["a"].Territory.MonsterEnergy);
        Assert.False(TerritorySharedRules.Execute(w,p,"b","rally-join",rally.Id,0,Formation(p["b"]),Now).Success);
        TerritoryRules.Settle(p["a"].Territory,Now.AddSeconds(191)); // Solo settlement cannot settle a shared mission.
        Assert.NotNull(p["a"].Territory.March);
        TerritorySharedRules.Settle(w,p,Now.AddSeconds(191));
        Assert.True(rally.Victory); Assert.True(rally.Returned); Assert.Null(p["a"].Territory.March);
        Assert.Equal(0, rally.NpcSupportPower);
        Assert.Equal(90,p["a"].Territory.Troops[0]); Assert.Equal(8,p["a"].Territory.Wounded[0]);
        var gold = p["a"].Territory.Wood; TerritorySharedRules.Settle(w,p,Now.AddSeconds(191));
        Assert.Equal(gold,p["a"].Territory.Wood); Assert.Single(p["a"].Territory.Battles);
    }
    [Fact]
    public void HostCancellationRefundsEnergyAndTroops()
    {
        var (w,p)=Setup(); TerritorySharedRules.Execute(w,p,"a","rally-create",null,0,Formation(p["a"]),Now);
        TerritorySharedRules.Execute(w,p,"a","rally-cancel",w.Rallies.Single().Id,0,null,Now.AddSeconds(30));
        Assert.True(w.Rallies.Single().Cancelled); Assert.Null(p["a"].Territory.March);
        Assert.Equal(100,p["a"].Territory.Troops[0]); Assert.Equal(100,p["a"].Territory.MonsterEnergy);
    }
    [Fact]
    public void SoloRallyGetsExplicitNpcOnceAndCompletesWithoutCreatingFakeAccounts()
    {
        var (w,p)=Setup(); TerritorySharedRules.Execute(w,p,"a","rally-create",null,0,Formation(p["a"]),Now);
        var rally = w.Rallies.Single(); var playerPower = rally.Power;
        TerritorySharedRules.Settle(w,p,Now.AddSeconds(59)); Assert.Equal(0,rally.NpcSupportPower);
        TerritorySharedRules.Settle(w,p,Now.AddSeconds(60));
        Assert.False(rally.Cancelled); Assert.Equal(600,rally.NpcSupportPower); Assert.Equal(playerPower+600,rally.Power);
        Assert.Single(rally.Armies); Assert.Equal(2,w.Members.Count);
        Assert.Contains(rally.Squads,s=>s.CommanderName.Contains("NPC"));
        var persisted=System.Text.Json.JsonSerializer.Deserialize<TerritorySharedWorld>(System.Text.Json.JsonSerializer.Serialize(w))!;
        TerritorySharedRules.Settle(persisted,p,Now.AddSeconds(191));
        Assert.True(persisted.Rallies.Single().Returned); Assert.True(persisted.Rallies.Single().Victory);
        Assert.Equal(playerPower+600,persisted.Rallies.Single().Power); Assert.Null(p["a"].Territory.March);
        Assert.Equal(90,p["a"].Territory.MonsterEnergy); Assert.Single(p["a"].Territory.Battles);
        var food=p["a"].Territory.Food; TerritorySharedRules.Settle(persisted,p,Now.AddSeconds(191)); Assert.Equal(food,p["a"].Territory.Food);
    }
    [Fact]
    public void ShieldPurchaseExtendsAndDeduplicatesRequest()
    {
        var (w,p)=Setup(); var request = Guid.NewGuid().ToString("N");
        Assert.True(TerritorySharedRules.Execute(w,p,"a","shield-buy",request,0,null,Now).Success);
        Assert.Equal(950,p["a"].PremiumCurrency); Assert.Equal(Now.AddHours(80),w.Members["a"].ShieldUntilUtc);
        Assert.True(TerritorySharedRules.Execute(w,p,"a","shield-buy",request,0,null,Now).Success);
        Assert.Equal(950,p["a"].PremiumCurrency); Assert.Equal(Now.AddHours(80),w.Members["a"].ShieldUntilUtc);
    }
    [Fact]
    public void RaidUsesNoEnergyBreaksOwnShieldAndRechecksTargetAtImpact()
    {
        var (w,p)=Setup(); w.Members["b"].ShieldUntilUtc=Now; p["a"].Territory.MonsterEnergy=0;
        Assert.True(TerritorySharedRules.Execute(w,p,"a","raid","b",0,Formation(p["a"]),Now).Success);
        Assert.Equal(0,p["a"].Territory.MonsterEnergy); Assert.Equal(Now,w.Members["a"].ShieldUntilUtc);
        Assert.False(TerritorySharedRules.Execute(w,p,"a","shield-buy",Guid.NewGuid().ToString("N"),0,null,Now).Success);
        Assert.True(TerritorySharedRules.Execute(w,p,"b","shield-buy",Guid.NewGuid().ToString("N"),0,null,Now.AddSeconds(30)).Success);
        TerritorySharedRules.Settle(w,p,Now.AddSeconds(131));
        Assert.True(w.Raids.Single().Cancelled); Assert.Equal(100,p["a"].Territory.Troops[0]); Assert.Empty(p["b"].Territory.Battles);
    }
    [Fact]
    public void MissingSameServerTargetIsRejectedWithoutCost()
    {
        var (w,p)=Setup(); var before=p["a"].Territory.Food;
        Assert.False(TerritorySharedRules.Execute(w,p,"a","raid","other-server-user",0,Formation(p["a"]),Now).Success);
        Assert.Null(p["a"].Territory.March); Assert.Equal(before,p["a"].Territory.Food);
    }
    [Fact]
    public async Task WorldAndCoinsPersistTogetherAndRollbackOnFailure()
    {
        var path=Path.Combine(Path.GetTempPath(),$"territory-coop-{Guid.NewGuid():N}.db");
        try
        {
            using(var store=new GameProgressStore(new GameOptions { DatabasePath=path }))
            {
                await store.LoadOrCreateAsync("a"); await store.MutateAsync("a", p=>{p.PremiumCurrency=1000;return true;});
                await store.MutateWorldAsync("one","a",(w,p)=>TerritorySharedRules.Execute(w,p,"a","shield-buy",Guid.NewGuid().ToString("N"),0,null,Now));
                await Assert.ThrowsAsync<InvalidOperationException>(()=>store.MutateWorldAsync<bool>("one","a",(w,p)=> {p["a"].PremiumCurrency=0;w.Members.Clear();throw new InvalidOperationException("rollback");}));
            }
            using var restored=new GameProgressStore(new GameOptions { DatabasePath=path });
            Assert.Equal(950,(await restored.LoadOrCreateAsync("a")).Progress.PremiumCurrency);
            Assert.Equal(1,await restored.MutateWorldAsync("one","a",(w,p)=>w.Members.Count));
            Assert.Equal(0,await restored.MutateWorldAsync("two","a",(w,p)=>w.Members.Count));
        }
        finally { using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(connection); File.Delete(path); }
    }
}
