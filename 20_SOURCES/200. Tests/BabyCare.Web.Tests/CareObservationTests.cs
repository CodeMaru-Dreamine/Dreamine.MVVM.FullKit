using BabyCare.Application;
using BabyCare.Domain;
using Xunit;
namespace BabyCare.Tests;
public class CareObservationTests
{
    [Fact] public void FeedingIntervalCrossesDaysAndIgnoresOtherKindsAndEndTimes()
    {
        var previous = new CareEntry { StartedAt = DateTimeOffset.Parse("2026-01-01T23:40:00+09:00"), EndedAt = DateTimeOffset.Parse("2026-01-02T00:10:00+09:00") };
        var current = new CareEntry { StartedAt = DateTimeOffset.Parse("2026-01-02T02:10:00+09:00") };
        var other = new CareEntry { Kind = "diaper", StartedAt = current.StartedAt.AddMinutes(-10) };
        Assert.Equal(TimeSpan.FromMinutes(150), CareObservations.FeedingInterval(current, [current, other, previous]));
        Assert.Null(CareObservations.FeedingInterval(previous, [current, previous]));
        Assert.Null(CareObservations.FeedingInterval(other, [current, previous]));
        Assert.Null(CareObservations.FeedingInterval(current, [current]));
    }
    [Fact] public void ExistingRecordsStayUnknownAndDefaultsAreNotObservedFacts()
    {
        var old = new CareEntry { Kind = "diaper", Detail = "mixed", Version = 1 };
        Assert.Null(CareObservations.Prepare(old, false).StoolColor);
        var fresh = CareObservations.Prepare(old with { Version = 0 }, true);
        Assert.Equal("yellow", fresh.StoolColor); Assert.Equal("pale-yellow", fresh.UrineColor);
        Assert.False(fresh.StoolColorConfirmed); Assert.False(fresh.UrineColorConfirmed);
        var edited = CareObservations.Prepare(fresh with { StoolColor = "red", StoolColorConfirmed = true }, true);
        Assert.Equal("red", edited.StoolColor); Assert.True(edited.StoolColorConfirmed);
        var wet = CareObservations.Prepare(edited with { Detail = "wet" }, false);
        Assert.Null(wet.StoolColor); Assert.False(wet.StoolColorConfirmed); Assert.Equal("pale-yellow", wet.UrineColor);
    }
    [Theory]
    [InlineData(true,"red")][InlineData(true,"black")][InlineData(true,"pale")]
    [InlineData(false,"pink-red")][InlineData(false,"brown")][InlineData(false,"dark-yellow")]
    public void ConcerningColorsHaveAdvice(bool stool,string color) => Assert.NotNull(CareObservations.Advice(stool,color));
    [Fact] public void CommonColorsDoNotShowAHealthDiagnosis()
    {
        Assert.Null(CareObservations.Advice(true,"yellow")); Assert.Null(CareObservations.Advice(true,"green"));
        Assert.Null(CareObservations.Advice(false,"pale-yellow")); Assert.Null(CareObservations.Advice(true,null));
        Assert.Contains("태변", CareObservations.Advice(true,"black")); Assert.Contains("첫 주",CareObservations.Advice(false,"pink-red"));
    }
    [Fact] public void ColorFieldsRoundTripAndInvalidKindIsRejected()
    {
        var catalog = new CareCatalog(Path.Combine(AppContext.BaseDirectory,"care-kinds.json"));
        var e = new CareEntry { Kind="diaper", Detail="mixed", StartedAt=DateTimeOffset.UtcNow.AddHours(-1), StoolColor="pale", UrineColor="pink-red", StoolColorConfirmed=true };
        catalog.Validate(e);
        var folder = Path.Combine(Path.GetTempPath(), "babycare-color-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var store = new BabyCare.Infrastructure.CareStore(Path.Combine(folder,"care.db"),catalog);
            var family = store.Create("mother","test family","test baby");
            store.Join("father",store.Invite("mother",family.Id));
            store.Save("mother",family.Id,e);
            var shared = store.Get("father",family.Id).Entries.Single();
            Assert.Equal("pale",shared.StoolColor); Assert.Equal("pink-red",shared.UrineColor);
            Assert.True(shared.StoolColorConfirmed);
            store.Save("father",family.Id,shared with { StoolColor="green", StoolColorConfirmed=true });
            Assert.Equal("green",store.Get("mother",family.Id).Entries.Single().StoolColor);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(folder,true); }
        var restored = System.Text.Json.JsonSerializer.Deserialize<CareEntry>(System.Text.Json.JsonSerializer.Serialize(e))!;
        Assert.Equal(e.StoolColor,restored.StoolColor); Assert.True(restored.StoolColorConfirmed); Assert.Equal(e.UrineColor,restored.UrineColor);
        Assert.Throws<CareException>(()=>catalog.Validate(e with { Kind="feeding" }));
        Assert.Throws<CareException>(()=>catalog.Validate(e with { StoolColor="purple" }));
    }
}
