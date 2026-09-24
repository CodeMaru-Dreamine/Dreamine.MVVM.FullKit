using GamePlatform.Domain;
namespace GamePlatform.Web.Tests;

public class TerritoryMonsterTests
{
    private static readonly DateTime Now = new(2026,9,6,0,0,0,DateTimeKind.Utc);
    private static GameProgress Player(string id) => new() { UserId=id, OwnedCompanionIds=PartyRules.StarterCompanionIds.ToList(), Territory=new() { Buildings=[10,1,1,1,5,5,5], Troops=[100,0,0], Training=5, Food=10000 } };
    private static TerritoryFormation Formation(GameProgress p,int count=100)=>new(){Squads=[new(){TroopType=0,Count=count,CommanderId=TerritoryRules.Commanders(p)[0].Id}]};
    private static TerritoryWorldBoss Easy => TerritorySharedRules.Bosses.Where(m=>m.Kind=="normal").OrderBy(m=>m.Level).First();
    [Fact]
    public void CatalogCoversEveryRegionWithVariedSpeciesAndStableLegacyIds()
    {
        var monsters=TerritorySharedRules.Bosses;
        Assert.Equal(326,monsters.Length); Assert.Equal(326,monsters.Select(m=>m.Tile).Distinct().Count());
        Assert.Equal(Enumerable.Range(0,monsters.Length),monsters.Select(m=>m.Id));
        Assert.Equal(new[]{7,13,17},monsters.Take(3).Select(m=>m.Tile)); Assert.DoesNotContain(monsters,m=>m.Tile==12);
        Assert.True(monsters.Select(m=>m.Asset).Distinct().Count()>=9);
        for(var region=0;region<25;region++) {
            var area=monsters.Where(m=>TerritoryRules.TileRow(m.Tile)/5*5+TerritoryRules.TileColumn(m.Tile)/5==region).ToArray();
            Assert.True(area.Length>=8); Assert.Contains(area,m=>m.Kind=="normal"); Assert.Contains(area,m=>m.Kind=="elite"); Assert.Contains(area,m=>m.RequiresRally);
        }
        Assert.All(monsters,m=>{Assert.True(m.Power>0);Assert.True(m.Reward>0);Assert.InRange(m.Level,1,30);});
    }
    [Fact]
    public void SoloHuntConsumesEnergyHasNoNpcAndRewardsOnceWithSharedRespawn()
    {
        var p=Player("a");var players=new Dictionary<string,GameProgress>{{"a",p},{"b",Player("b")}};var world=new TerritorySharedWorld();
        Assert.True(TerritorySharedRules.Execute(world,players,"a","hunt",null,Easy.Id,Formation(p),Now).Success);
        var hunt=world.Rallies.Single();Assert.True(hunt.SoloHunt);Assert.Equal(Now,hunt.DepartsUtc);Assert.Equal(90,p.Territory.MonsterEnergy);
        Assert.False(TerritorySharedRules.Execute(world,players,"b","hunt",null,Easy.Id,Formation(players["b"]),Now).Success);
        Assert.False(TerritorySharedRules.Execute(world,players,"b","rally-join",hunt.Id,Easy.Id,Formation(players["b"]),Now).Success);
        Assert.Equal(100,players["b"].Territory.MonsterEnergy);
        TerritorySharedRules.Settle(world,players,Now.AddSeconds(131));
        Assert.True(hunt.Returned);Assert.True(hunt.Victory);Assert.Equal(0,hunt.NpcSupportPower);Assert.Single(hunt.Armies);Assert.Null(p.Territory.March);
        Assert.Equal(Easy.Name,p.Territory.Battles.Single().EncounterTitle);Assert.Equal(Easy.Asset,p.Territory.Battles.Single().OpponentAsset);
        Assert.Equal(Now.AddSeconds(70+Easy.RespawnSeconds),world.BossRespawns[Easy.Id]);
        var resources=p.Territory.Wood;TerritorySharedRules.Settle(world,players,Now.AddSeconds(131));Assert.Equal(resources,p.Territory.Wood);
        Assert.False(TerritorySharedRules.Execute(world,players,"a","hunt",null,Easy.Id,Formation(p,20),Now.AddSeconds(131)).Success);
        Assert.True(TerritorySharedRules.Execute(world,players,"a","hunt",null,Easy.Id,Formation(p,20),Now.AddSeconds(71+Easy.RespawnSeconds)).Success);
    }
    [Fact]
    public void WrongModeInvalidMonsterAndNoEnergyCannotReserveTroops()
    {
        var p=Player("a");var players=new Dictionary<string,GameProgress>{{"a",p}};var world=new TerritorySharedWorld();
        Assert.False(TerritorySharedRules.Execute(world,players,"a","hunt",null,0,Formation(p),Now).Success);
        Assert.False(TerritorySharedRules.Execute(world,players,"a","rally-create",null,Easy.Id,Formation(p),Now).Success);
        Assert.False(TerritorySharedRules.Execute(world,players,"a","hunt",null,99999,Formation(p),Now).Success);
        p.Territory.MonsterEnergy=0;
        Assert.False(TerritorySharedRules.Execute(world,players,"a","hunt",null,Easy.Id,Formation(p),Now).Success);
        Assert.Empty(world.Rallies);Assert.Equal(100,p.Territory.Troops[0]);Assert.Equal(10000,p.Territory.Food);
    }
    [Fact]
    public void ExistingCastleOnNewSpawnIsPreservedAndNotAttackableAsMonster()
    {
        var p=Player("a");var players=new Dictionary<string,GameProgress>{{"a",p}};var world=new TerritorySharedWorld();
        TerritorySharedRules.Register(world,p,Now);world.Members["a"].Tile=Easy.Tile;
        Assert.False(TerritorySharedRules.Execute(world,players,"a","hunt",null,Easy.Id,Formation(p),Now).Success);
        Assert.Equal(Easy.Tile,world.Members["a"].Tile);Assert.Empty(world.Rallies);
    }
    [Fact]
    public void MoreWildlifeDoesNotReduceCastleCapacity()
    {
        var world=new TerritorySharedWorld(); var spawnTiles=TerritorySharedRules.Bosses.Select(m=>m.Tile).ToHashSet();
        foreach(var tile in Enumerable.Range(0,TerritoryRules.WorldTileCount).Where(t=>!spawnTiles.Contains(t))) world.Members[$"resident-{tile}"]=new(){UserId=$"resident-{tile}",Tile=tile};
        var p=Player("newcomer");TerritorySharedRules.Register(world,p,Now);
        Assert.Contains(world.Members["newcomer"].Tile,spawnTiles);
        Assert.Equal(world.Members.Count,world.Members.Values.Select(m=>m.Tile).Distinct().Count());
    }
    [Fact]
    public void DefeatDoesNotDespawnMonsterOrGenerateRewards()
    {
        var p=Player("a");var players=new Dictionary<string,GameProgress>{{"a",p}};var world=new TerritorySharedWorld();
        var elite=TerritorySharedRules.Bosses.Where(m=>m.Kind=="elite").OrderByDescending(m=>m.Power).First();
        Assert.True(TerritorySharedRules.Execute(world,players,"a","hunt",null,elite.Id,Formation(p,1),Now).Success);
        TerritorySharedRules.Settle(world,players,Now.AddSeconds(131));
        Assert.False(world.Rallies.Single().Victory);Assert.Equal(0,world.Rallies.Single().NpcSupportPower);
        Assert.False(world.BossRespawns.ContainsKey(elite.Id));Assert.Single(p.Territory.Battles);
    }
}
