using BabyCare.Application;
using BabyCare.Domain;
using BabyCare.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

public class FeedingAndSolidsTests
{
    private static CareCatalog Catalog() => new(Path.Combine(AppContext.BaseDirectory,"care-kinds.json"));
    [Theory]
    [InlineData("left")][InlineData("right")][InlineData("both")]
    public void NursingKeepsSideAndRealDurationWithoutInventingVolume(string side)
    {
        var now=DateTimeOffset.UtcNow;
        var command=new CareCommand("create",null,"feeding",now.AddMinutes(-15),now,null,"","",null,FeedingMode:"breast",BreastSide:side);
        var entry=CodexCareInterpreter.ConvertCommand(command,[],Catalog()).Entry!;
        Assert.Equal(side,entry.BreastSide); Assert.Null(entry.AmountMl);
        Assert.Equal(15,(entry.EndedAt!.Value-entry.StartedAt).TotalMinutes);
        Assert.Null(CarePlanning.ApplyFeedingDefault(entry,new()).ExpectedDurationMinutes);
        Assert.Throws<CareException>(()=>Catalog().Validate(entry with { AmountMl=80 }));
    }
    [Fact] public void SolidsAreSharedPersistedAndNeverCountedAsMilk()
    {
        var dir=Path.Combine(Path.GetTempPath(),"babycare-solids-"+Guid.NewGuid().ToString("N"));
        try {
            var catalog=Catalog(); var store=new CareStore(Path.Combine(dir,"test.db"),catalog);
            var family=store.Create("mother","family","baby"); store.Join("father",store.Invite("mother",family.Id));
            var now=DateTimeOffset.UtcNow;
            var entry=CodexCareInterpreter.ConvertCommand(new("create",null,"solids",now,now,null,"","cháo gạo",null,
                NoteTranslations:new() { ["ko"]="쌀미음" },AmountGrams:50),[],catalog).Entry!;
            store.Save("mother",family.Id,entry);
            var shared=Assert.Single(store.Get("father",family.Id).Entries);
            Assert.Equal(50m,shared.AmountGrams);Assert.Null(shared.AmountMl);Assert.Equal("쌀미음",CareLanguages.DisplayNote(shared,"ko"));
            Assert.Null(CareObservations.FeedingInterval(shared,[]));
            Assert.Throws<CareException>(()=>catalog.Validate(shared with { AmountMl=50 }));
            Assert.Throws<CareException>(()=>catalog.Validate(shared with { AmountGrams=-1 }));
        } finally { SqliteConnection.ClearAllPools(); if(Directory.Exists(dir))Directory.Delete(dir,true); }
    }
    [Fact] public void PumpingStartKeepsSideWithoutInventingYield()
    {
        var entry=CodexCareInterpreter.ConvertCommand(new("create",null,"pumping",DateTimeOffset.UtcNow,null,null,"","",null,BreastSide:"right"),[],Catalog()).Entry!;
        Assert.Equal("right",entry.BreastSide);Assert.Null(entry.RightAmountMl);Assert.Null(entry.EndedAt);
    }
    [Fact] public void LegacyMilkRemainsUnspecifiedAndCorrectionsPreserveNursingSide()
    {
        var legacy=System.Text.Json.JsonSerializer.Deserialize<CareEntry>("{\"Kind\":\"feeding\",\"AmountMl\":80}")!;
        Assert.Equal("",legacy.FeedingMode);Assert.Equal("",legacy.BreastSide);
        Assert.Equal("formula",QuickCareParser.TryParse("분유 80미리 먹었어",DateTimeOffset.UtcNow)!.FeedingMode);
        var previous=new CareEntry { FeedingMode="breast",BreastSide="left",StartedAt=DateTimeOffset.UtcNow.AddMinutes(-20) };
        var next=CodexCareInterpreter.ConvertCommand(new("update",previous.Id,"feeding",previous.StartedAt,DateTimeOffset.UtcNow,null,"","",null),[previous],Catalog()).Entry!;
        Assert.Equal("breast",next.FeedingMode);Assert.Equal("left",next.BreastSide);
    }
}
