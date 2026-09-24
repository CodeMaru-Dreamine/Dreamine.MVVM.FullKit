using GamePlatform.Domain;
namespace GamePlatform.Web.Tests;
public class TerritoryCommandTests
{
    private static readonly DateTime Now=new(2026,9,6,0,0,0,DateTimeKind.Utc);
    private static GameProgress Player()=>new(){OwnedCompanionIds=PartyRules.StarterCompanionIds.ToList(),Territory=new(){Buildings=[10,1,1,1,5,5,1],Troops=[150,0,0],Food=10000,Wood=10000,Stone=10000}};
    [Fact]
    public void EnergyRecoversByElapsedTimeAndNotByReloads()
    {
        var s=new TerritoryState{MonsterEnergy=0}; MonsterEnergyRules.Settle(s,Now);
        MonsterEnergyRules.Settle(s,Now.AddSeconds(299));Assert.Equal(0,s.MonsterEnergy);
        MonsterEnergyRules.Settle(s,Now.AddSeconds(601));Assert.Equal(2,s.MonsterEnergy);
        MonsterEnergyRules.Settle(s,Now.AddSeconds(601));Assert.Equal(2,s.MonsterEnergy);
        MonsterEnergyRules.Settle(s,Now);Assert.Equal(2,s.MonsterEnergy);
        MonsterEnergyRules.Settle(s,Now.AddDays(10));Assert.Equal(100,s.MonsterEnergy);
    }
    [Fact]
    public void RecruitmentBatchScalesWithBarracksAndRespectsCapacity()
    {
        var p=Player();p.Territory.Troops=[0,0,0];Assert.Equal(50,TerritoryRules.RecruitmentLimit(p.Territory));
        Assert.False(TerritoryRules.Execute(p.Territory,"recruit",0,51,Now,p).Success);
        Assert.True(TerritoryRules.Execute(p.Territory,"recruit",0,50,Now,p).Success);Assert.Equal(50,p.Territory.Recruitment!.Count);
        p.Territory.Recruitment=null;p.Territory.Troops=[170,0,0];Assert.Equal(10,TerritoryRules.MaximumRecruitment(p.Territory,0));
    }
    [Fact]
    public void PresetPersistsSlotWithoutReservingAndHeroCapacityIsEnforced()
    {
        var p=Player();var hero=TerritoryRules.Commanders(p)[0];
        var form=new TerritoryFormation{Slot=2,Squads=[new(){TroopType=0,Count=hero.TroopCapacity+1,CommanderId=hero.Id}]};
        Assert.False(TerritoryRules.Execute(p.Territory,"save-formation",2,1,Now,p,form).Success);
        form.Squads[0].Count=20;Assert.True(TerritoryRules.Execute(p.Territory,"save-formation",2,1,Now,p,form).Success);
        Assert.Equal(150,p.Territory.Troops[0]);Assert.Empty(TerritoryRules.ActiveMarches(p.Territory));
        var clone=TerritoryRules.Clone(p.Territory);Assert.Equal(20,clone.FormationPresets[2].Squads[0].Count);
        Assert.True(TerritoryRules.Execute(p.Territory,"conquer",7,1,Now,p,form).Success);Assert.Equal(2,p.Territory.March!.Slot);Assert.Equal(90,p.Territory.MonsterEnergy);
    }
    [Fact]
    public void ClearHistoryDoesNotClearWorkOrEnergy()
    {
        var p=Player();p.Territory.MonsterEnergy=12;p.Territory.Reports.Add("test");p.Territory.Battles.Add(new());p.Territory.Recruitment=new(){CompletesUtc=Now.AddMinutes(1),Kind="recruit",Count=1};
        Assert.True(TerritoryRules.Execute(p.Territory,"clear-history",0,0,Now,p).Success);
        Assert.Empty(p.Territory.Reports);Assert.Empty(p.Territory.Battles);Assert.NotNull(p.Territory.Recruitment);Assert.Equal(12,p.Territory.MonsterEnergy);
    }
}
