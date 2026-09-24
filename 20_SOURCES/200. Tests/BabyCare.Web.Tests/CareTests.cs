using BabyCare.Application;
using BabyCare.Domain;
using BabyCare.Infrastructure;
using Xunit;

namespace BabyCare.Tests;

public sealed class CareTests : IDisposable
{
    [Theory]
    [InlineData("기저기교체 대변", "dirty")]
    [InlineData("기저귀 교체 소변", "wet")]
    [InlineData("지금 기저귀 교체 소변 대변", "mixed")]
    public async Task QuickDiaperCommandWorksWithoutAiAndRemainsAnEditableProposal(string text, string detail)
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var interpreter = new CodexCareInterpreter(config, null!, catalog);
        var now = DateTimeOffset.UtcNow;
        var result = await interpreter.ParseAsync(text, now, [], CancellationToken.None);
        var entry = Assert.IsType<CareEntry>(result.Entry);
        Assert.Equal("diaper", entry.Kind); Assert.Equal(detail, entry.Detail);
        Assert.Equal(now, entry.StartedAt); Assert.Null(entry.EndedAt); Assert.Null(entry.AmountMl);
        var store = Store(); var family = store.Create("a", "A", "아기");
        Assert.Empty(store.Get("a", family.Id).Entries);
        store.Save("a", family.Id, entry);
        var saved = store.Get("a", family.Id).Entries.Single();
        Assert.Equal(detail, saved.Detail);
        store.Save("a", family.Id, saved with { Detail = "mixed" });
        Assert.Equal("mixed", store.Get("a", family.Id).Entries.Single().Detail);
    }
    [Theory]
    [InlineData("기저귀 교체")]
    [InlineData("어제 기저귀 교체 대변")]
    [InlineData("기저귀 교체 대변 아님")]
    [InlineData("11시 기저귀 교체 대변")]
    [InlineData("기저귀 교체 대변 그리고 분유 80")]
    [InlineData("아까 기저귀 교체 대변으로 수정")]
    public void QuickDiaperParserDoesNotGuessAmbiguousOrCompoundCommands(string text)
        => Assert.Null(QuickCareParser.TryParse(text, DateTimeOffset.UtcNow));
    private readonly string folder = Path.Combine(Path.GetTempPath(), "babycare-tests", Guid.NewGuid().ToString("N"));
    private readonly CareCatalog catalog = new(Path.Combine(AppContext.BaseDirectory, "care-kinds.json"));
    private CareStore Store() => new(Path.Combine(folder, "care.db"), catalog);
    private static CareEntry Entry() => new() { StartedAt = DateTimeOffset.UtcNow.AddHours(-1), AmountMl = 80 };

    [Fact] public void TenantIsolationCoversReadsWritesDeletesAndInvites()
    {
        var store = Store(); var family = store.Create("parent-a", "A", "아기"); var entry = Entry();
        store.Save("parent-a", family.Id, entry);
        Assert.Empty(store.List("parent-b"));
        Assert.Throws<CareException>(() => store.Get("parent-b", family.Id));
        Assert.Throws<CareException>(() => store.Save("parent-b", family.Id, entry));
        Assert.Throws<CareException>(() => store.Delete("parent-b", family.Id, entry.Id, 1));
        Assert.Throws<CareException>(() => store.Invite("parent-b", family.Id));
        Assert.Throws<CareException>(() => store.List(""));
        Assert.Single(store.Get("parent-a", family.Id).Entries);
    }
    [Fact] public void InviteIsSingleUseRotatableAndOwnerOnly()
    {
        var store = Store(); var family = store.Create("a", "A", "아기");
        var old = store.Invite("a", family.Id); var code = store.Invite("a", family.Id);
        Assert.Throws<CareException>(() => store.Join("b", old));
        store.Join("b", code);
        Assert.Single(store.List("b"));
        Assert.Throws<CareException>(() => store.Join("c", code));
        Assert.Throws<CareException>(() => store.Invite("b", family.Id));
        store.Save("b", family.Id, Entry());
        Assert.Single(store.Get("a", family.Id).Entries);
    }
    [Fact] public void EditPreservesOtherRecordsAndRejectsStaleUpdatesAcrossStoreInstances()
    {
        var first = Store(); var family = first.Create("a", "A", "아기"); var e = Entry();
        first.Save("a", family.Id, e);
        var second = Store(); var snapshot = second.Get("a", family.Id).Entries.Single();
        first.Save("a", family.Id, snapshot with { AmountMl = 60 });
        Assert.Throws<CareException>(() => second.Save("a", family.Id, snapshot with { AmountMl = 100 }));
        Assert.Throws<CareException>(() => second.Delete("a", family.Id, e.Id, 1));
        var persisted = second.Get("a", family.Id);
        Assert.Equal(60, persisted.Entries.Single().AmountMl);
        Assert.Equal(80, persisted.Audit.Last().Before!.AmountMl);
        second.Delete("a", family.Id, e.Id, 2);
        Assert.Throws<CareException>(() => first.Save("a", family.Id, snapshot));
        Assert.Empty(first.Get("a", family.Id).Entries);
    }
    [Fact] public void InvalidQuantityTimesAndUnknownCommandsCannotBeSaved()
    {
        Assert.Throws<CareException>(() => catalog.Validate(Entry() with { AmountMl = -1 }));
        Assert.Throws<CareException>(() => catalog.Validate(Entry() with { EndedAt = DateTimeOffset.UtcNow.AddDays(-1) }));
        Assert.Throws<CareException>(() => catalog.Validate(Entry() with { Kind = "unknown" }));
        Assert.Throws<CareException>(() => catalog.Validate(Entry() with { Kind = "diaper", AmountMl = null, Detail = "" }));
        catalog.Validate(Entry() with { Kind = "diaper", AmountMl = null, Detail = "wet" });
    }
    [Fact] public void CodexCanOnlyProposeUpdatesToTheAuthorizedContext()
    {
        var entry = Entry() with { Version = 3 };
        var cmd = new CareCommand("update", "other-tenant-entry", "feeding", entry.StartedAt, null, 60, "", "", null);
        Assert.Throws<CareException>(() => CodexCareInterpreter.ConvertCommand(cmd, [entry], catalog));
        var result = CodexCareInterpreter.ConvertCommand(cmd with { TargetId = entry.Id }, [entry], catalog);
        Assert.Equal(entry.Id, result.Entry!.Id); Assert.Equal(3, result.Entry.Version); Assert.Equal(60, result.Entry.AmountMl);
        Assert.Equal(80, entry.AmountMl);
    }
    [Fact] public void ClarificationAndUnsupportedActionsNeverProduceARecord()
    {
        var cmd = new CareCommand("clarify", null, null, null, null, null, null, null, "오전인가요, 오후인가요?");
        var result = CodexCareInterpreter.ConvertCommand(cmd, [], catalog);
        Assert.Null(result.Entry); Assert.NotNull(result.Question);
        Assert.Throws<CareException>(() => CodexCareInterpreter.ConvertCommand(cmd with { Action = "delete" }, [], catalog));
    }
    [Fact] public void NewKindsUseTheSameCatalogWithoutChangingStorage()
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "catalog.json");
        File.WriteAllText(path, """[{"id":"bath","label":"목욕","icon":"물","hasAmount":false,"hasDuration":true,"examples":["목욕 시작"]}]""");
        var extended = new CareCatalog(path);
        var store = new CareStore(Path.Combine(folder, "extended.db"), extended);
        var family = store.Create("a", "A", "아기");
        store.Save("a", family.Id, new CareEntry { Kind = "bath", StartedAt = DateTimeOffset.UtcNow.AddMinutes(-10) });
        Assert.Equal("bath", store.Get("a", family.Id).Entries.Single().Kind);
    }
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }
}
