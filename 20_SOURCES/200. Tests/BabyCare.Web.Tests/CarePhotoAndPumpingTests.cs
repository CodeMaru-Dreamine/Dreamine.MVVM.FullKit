using BabyCare.Application;
using BabyCare.Domain;
using BabyCare.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BabyCare.Tests;
public class CarePhotoAndPumpingTests
{
    private static CareCatalog Catalog() => new(Path.Combine(AppContext.BaseDirectory, "care-kinds.json"));
    [Fact] public async Task PhotosRequireFamilyMembershipAndIgnoreClientFileNames()
    {
        var dir = Path.Combine(Path.GetTempPath(), "babycare-photos-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new CareStore(Path.Combine(dir,"test.db"), Catalog());
            var family = store.Create("mother", "family", "baby");
            store.Join("father", store.Invite("mother", family.Id));
            var photos = new CarePhotos(Path.Combine(dir,"photos"), store);
            byte[] png = [137,80,78,71,13,10,26,10,0,0,0,0];
            var id = await photos.SaveAsync("mother",family.Id,new MemoryStream(png));
            Assert.True(CarePhotos.ValidId(id));
            Assert.NotNull(photos.ReadPath("father", family.Id, id));
            Assert.Throws<CareException>(() => photos.ReadPath("stranger",family.Id,id));
            await Assert.ThrowsAsync<CareException>(() => photos.SaveAsync("stranger",family.Id,new MemoryStream(png)));
            Assert.Null(photos.ReadPath("mother",family.Id,"../../test.db"));
            await Assert.ThrowsAsync<CareException>(() => photos.SaveAsync("mother",family.Id,new MemoryStream("<svg onload='alert(1)'/>"u8.ToArray())));
            await Assert.ThrowsAsync<CareException>(() => photos.SaveAsync("mother",family.Id,new MemoryStream(new byte[CarePhotos.MaxBytes+1])));
            store.SavePreferences("mother",family.Id,new() { AvatarPhotoId = id });
            var entry = new CareEntry { PhotoIds = [id] }; store.Save("mother",family.Id,entry);
            var shared = store.Get("father",family.Id);
            Assert.Equal(id,shared.Preferences.AvatarPhotoId); Assert.Equal(id,Assert.Single(shared.Entries).PhotoIds.Single());
            Catalog().Validate(entry with { PhotoIds=[] });
            Assert.Throws<CareException>(() => Catalog().Validate(entry with { PhotoIds=[id,id] }));
        }
        finally { SqliteConnection.ClearAllPools(); if (Directory.Exists(dir)) Directory.Delete(dir,true); }
    }
    [Fact] public void PumpingIsSeparateFromFeedingAndUnknownSidesStayUnknown()
    {
        var now = DateTimeOffset.UtcNow;
        var command = new CareCommand("create",null,"pumping",now.AddMinutes(-20),now,null,"","",null,
            LeftAmountMl:60,RightAmountMl:null,PumpMethod:"electric");
        var pumping = CodexCareInterpreter.ConvertCommand(command,[],Catalog()).Entry!;
        Assert.Equal(60,pumping.LeftAmountMl); Assert.Null(pumping.RightAmountMl); Assert.Null(pumping.AmountMl);
        Assert.Equal("electric",pumping.PumpMethod); Assert.True(Catalog().Get("pumping").HasDuration);
        Assert.Throws<CareException>(() => Catalog().Validate(pumping with { LeftAmountMl=-1 }));
        Assert.Throws<CareException>(() => Catalog().Validate(pumping with { PumpMethod="invalid" }));
        Assert.Throws<CareException>(() => Catalog().Validate(pumping with { Kind="feeding" }));
    }
    [Fact] public void MothersPumpingDoesNotInterruptInfantSleepEstimates()
    {
        var now=DateTimeOffset.UtcNow;
        var family=new FamilyState { Entries=[new() { Kind="diaper", Detail="wet", StartedAt=now.AddHours(-2) }] };
        var expected=CarePlanning.Suggest(family,now).Select(s=>(s.Entry.StartedAt,s.Entry.EndedAt)).ToArray();
        family.Entries.Add(new() { Kind="pumping",StartedAt=now.AddHours(-1),EndedAt=now.AddMinutes(-40),LeftAmountMl=40 });
        Assert.Equal(expected,CarePlanning.Suggest(family,now).Select(s=>(s.Entry.StartedAt,s.Entry.EndedAt)).ToArray());
    }
}
