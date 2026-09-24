using BabyCare.Application;
using BabyCare.Domain;
using BabyCare.Infrastructure;
using Xunit;

namespace BabyCare.Tests;

public sealed class CarePlanningTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "babycare-tests", Guid.NewGuid().ToString("N"));
    private readonly CareCatalog catalog = new(Path.Combine(AppContext.BaseDirectory, "care-kinds.json"));
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 16, 0, 0, TimeSpan.FromHours(9));
    private static CareEntry Feed() => new() { Kind = "feeding", StartedAt = Now.AddHours(-2), ExpectedDurationMinutes = 20, AmountMl = 120 };

    [Fact] public void DefaultsNeverInventCompletedFeedingOrOverrideExplicitEnd()
    {
        var feed = new CareEntry { StartedAt = Now };
        var proposed = CarePlanning.ApplyFeedingDefault(feed, new());
        Assert.Equal(20, proposed.ExpectedDurationMinutes); Assert.Null(proposed.EndedAt);
        Assert.Null(feed.ExpectedDurationMinutes);
        var completed = feed with { EndedAt = Now.AddMinutes(25) };
        Assert.Equal(completed, CarePlanning.ApplyFeedingDefault(completed, new()));
        Assert.Equal(feed, CarePlanning.ApplyFeedingDefault(feed, new() { UseFeedingDefault = false }));
        Assert.Equal(30, CarePlanning.ApplyFeedingDefault(feed, new() { FeedingMinutes = 30 }).ExpectedDurationMinutes);
    }
    [Fact] public void FeedingSuggestsBurpThenSleepWithoutWritingAnything()
    {
        var family = new FamilyState { Entries = [Feed()] };
        var suggestions = CarePlanning.Suggest(family, Now);
        var burp = Assert.Single(suggestions, s => s.Entry.Kind == "burping").Entry;
        var sleep = Assert.Single(suggestions, s => s.Entry.Kind == "sleep").Entry;
        Assert.Equal(Now.AddMinutes(-100), burp.StartedAt); Assert.Equal(Now.AddMinutes(-80), burp.EndedAt);
        Assert.Equal(burp.EndedAt, sleep.StartedAt); Assert.Equal(Now, sleep.EndedAt);
        Assert.Single(family.Entries);
        family.Entries.Add(burp); family.Entries.Add(sleep);
        Assert.Empty(CarePlanning.Suggest(family, Now));
    }
    [Fact] public void DiaperDelayAndNextEventBoundSleepAcrossMidnight()
    {
        var at = Now.Date.AddDays(-1).AddHours(23).AddMinutes(55);
        var diaper = new CareEntry { Kind = "diaper", Detail = "wet", StartedAt = new DateTimeOffset(at, Now.Offset) };
        var measured = new CareEntry { Kind = "temperature", TemperatureC = 36.7m, StartedAt = diaper.StartedAt.AddMinutes(40) };
        var family = new FamilyState { Entries = [diaper, measured] };
        var sleep = Assert.Single(CarePlanning.Suggest(family, Now)).Entry;
        Assert.Equal(diaper.StartedAt.AddMinutes(10), sleep.StartedAt); Assert.Equal(measured.StartedAt, sleep.EndedAt);
    }
    [Fact] public void FutureOrOpenFeedAndOverlappingSleepNeverGenerateSleep()
    {
        Assert.Empty(CarePlanning.Suggest(new() { Entries = [Feed() with { StartedAt = Now }] }, Now));
        Assert.Empty(CarePlanning.Suggest(new() { Entries = [Feed() with { ExpectedDurationMinutes = null }] }, Now));
        var family = new FamilyState { Entries = [Feed(), new() { Kind = "sleep", StartedAt = Now.AddHours(-3) }] };
        Assert.Empty(CarePlanning.Suggest(family, Now));
        Assert.Empty(CarePlanning.Suggest(new() { Entries = [Feed()], Preferences = new() { SuggestSleep = false } }, Now));
    }
    [Theory]
    [InlineData("지금 체온 36.7도", 16, 0)]
    [InlineData("오후 3시 온도 36.7도", 15, 0)]
    [InlineData("오전 12시 15분 체온 36점7도", 0, 15)]
    public void TemperatureParsesDecimalsAndExplicitTimes(string text, int hour, int minute)
    {
        var entry = QuickCareParser.TryParse(text, Now)!;
        Assert.NotNull(entry); Assert.Equal("temperature", entry.Kind); Assert.Equal(36.7m, entry.TemperatureC);
        Assert.Equal(hour, entry.StartedAt.Hour); Assert.Equal(minute, entry.StartedAt.Minute);
        catalog.Validate(entry);
    }
    [Theory]
    [InlineData("3시 체온 36.7도")]
    [InlineData("오후 5시 체온 36.7도")]
    [InlineData("어제 체온 36.7도")]
    [InlineData("체온 36.7도 아니고 37.1도")]
    public void AmbiguousTemperatureFallsBackWithoutGuessing(string text) => Assert.Null(QuickCareParser.TryParse(text, Now));
    [Fact] public void SettingsPersistPerFamilyAndRejectStaleOrUnauthorizedWrites()
    {
        var store = new CareStore(Path.Combine(folder, "care.db"), catalog);
        var a = store.Create("a", "A", "아기"); var b = store.Create("b", "B", "아기");
        store.SavePreferences("a", a.Id, new() { FeedingMinutes = 30 });
        Assert.Equal(30, store.Get("a", a.Id).Preferences.FeedingMinutes);
        Assert.Equal(20, store.Get("b", b.Id).Preferences.FeedingMinutes);
        Assert.Throws<CareException>(() => store.SavePreferences("b", a.Id, new()));
        Assert.Throws<CareException>(() => store.SavePreferences("a", a.Id, new()));
        Assert.Throws<CareException>(() => store.SavePreferences("a", a.Id, new() { Version = 1, FeedingMinutes = 0 }));
        var entry = new CareEntry { Kind = "temperature", StartedAt = Now, TemperatureC = 36.7m };
        store.Save("a", a.Id, entry);
        var saved = store.Get("a", a.Id).Entries.Single();
        Assert.Equal(36.7m, saved.TemperatureC);
        store.Save("a", a.Id, saved with { TemperatureC = 37.1m });
        saved = store.Get("a", a.Id).Entries.Single(); Assert.Equal(37.1m, saved.TemperatureC);
        store.Delete("a", a.Id, saved.Id, saved.Version); Assert.Empty(store.Get("a", a.Id).Entries);
    }
    [Fact] public void LegacyFamilyJsonGetsDefaultsAndTemperatureValidationIsStrict()
    {
        var legacy = System.Text.Json.JsonSerializer.Deserialize<FamilyState>("{\"Name\":\"A\",\"Entries\":[]}")!;
        Assert.Equal(20, legacy.Preferences.FeedingMinutes);
        Assert.Throws<CareException>(() => catalog.Validate(new() { Kind = "temperature", StartedAt = Now }));
        Assert.Throws<CareException>(() => catalog.Validate(new() { Kind = "temperature", StartedAt = Now, TemperatureC = 367 }));
        Assert.Throws<CareException>(() => catalog.Validate(Feed() with { TemperatureC = 36.7m }));
    }
    [Fact] public void SleepTotalsClipAtMidnightAndDoNotDoubleCountOverlaps()
    {
        var midnight = new DateTimeOffset(Now.Date, Now.Offset);
        var entries = new[] {
            new CareEntry { Kind = "sleep", StartedAt = midnight.AddMinutes(-30), EndedAt = midnight.AddMinutes(30) },
            new CareEntry { Kind = "sleep", StartedAt = midnight.AddMinutes(20), EndedAt = midnight.AddMinutes(60) },
            new CareEntry { Kind = "sleep", StartedAt = midnight.AddHours(2) }
        };
        Assert.Equal(60, CarePlanning.SleepMinutesOnDay(entries, Now.Date, Now.Offset));
        Assert.Equal(30, CarePlanning.SleepMinutesOnDay(entries, Now.Date.AddDays(-1), Now.Offset));
    }
    [Fact] public void ConcurrentConfirmationCannotSaveDuplicateEstimatedSleep()
    {
        var store = new CareStore(Path.Combine(folder, "care.db"), catalog);
        var family = store.Create("a", "A", "아기");
        var sleep = new CareEntry { Kind = "sleep", StartedAt = Now.AddHours(-1), EndedAt = Now, ConfirmedFromEstimate = true };
        store.Save("a", family.Id, sleep);
        Assert.Throws<CareException>(() => store.Save("a", family.Id, sleep with { Id = Guid.NewGuid().ToString("N") }));
        Assert.Single(store.Get("a", family.Id).Entries);
    }
    [Fact] public void AiTemperatureAndFeedingCorrectionPreserveTypedFields()
    {
        var temperature = new CareCommand("create", null, "temperature", Now, null, null, "", "", null, 36.7m);
        Assert.Equal(36.7m, CodexCareInterpreter.ConvertCommand(temperature, [], catalog).Entry!.TemperatureC);
        var feed = Feed() with { Version = 1 };
        var correction = new CareCommand("update", feed.Id, "feeding", feed.StartedAt, null, 100, "", "", null);
        Assert.Equal(20, CodexCareInterpreter.ConvertCommand(correction, [feed], catalog).Entry!.ExpectedDurationMinutes);
    }
    public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
