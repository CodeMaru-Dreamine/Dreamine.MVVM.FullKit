using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BabyCare.Application;
using BabyCare.Domain;
using Dreamine.Database.Sqlite;
using Microsoft.Data.Sqlite;

namespace BabyCare.Infrastructure;

// The family aggregate is updated atomically with optimistic concurrency. Every access checks membership.
// SQLite + Dreamine provider keeps the MVP deployable on a single existing server.
public sealed class CareStore
{
    private readonly SqliteDatabaseProvider database;
    private readonly CareCatalog catalog;
    public CareStore(string path, CareCatalog catalog)
    {
        this.catalog = catalog;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        database = new SqliteDatabaseProvider(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        database.EnsureDatabaseExists();
        database.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS BabyFamilies (Id TEXT PRIMARY KEY, Payload TEXT NOT NULL, Revision INTEGER NOT NULL, InviteHash TEXT NOT NULL DEFAULT '', InviteExpires TEXT NOT NULL DEFAULT '')");
    }

    public IReadOnlyList<FamilySummary> List(string user)
    {
        RequireUser(user);
        return database.Query<FamilyRow>("SELECT * FROM BabyFamilies WHERE EXISTS (SELECT 1 FROM json_each(Payload, '$.Members') WHERE value = @User)", new { User = user })
            .Select(Read).Select(f => new FamilySummary(f.Id, f.Name, f.BabyName)).ToArray();
    }

    public FamilyState Get(string user, string familyId) => Read(Authorized(user, familyId));

    public FamilyState Create(string user, string name, string babyName)
    {
        RequireUser(user);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40 || string.IsNullOrWhiteSpace(babyName) || babyName.Length > 40)
            throw new CareException("가족 공간과 아기 이름을 각각 1~40자로 입력해주세요.");
        var family = new FamilyState { Name = name.Trim(), BabyName = babyName.Trim(), OwnerId = user, Members = [user] };
        database.ExecuteNonQuery("INSERT INTO BabyFamilies (Id, Payload, Revision) VALUES (@Id, @Payload, 0)", new { family.Id, Payload = JsonSerializer.Serialize(family) });
        return family;
    }

    public string Invite(string user, string familyId)
    {
        var row = Authorized(user, familyId);
        if (Read(row).OwnerId != user) throw new CareException("공간을 만든 보호자만 초대 코드를 발급할 수 있습니다.");
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var changed = database.ExecuteNonQuery("UPDATE BabyFamilies SET InviteHash = @Hash, InviteExpires = @Expires, Revision = Revision + 1 WHERE Id = @Id AND Revision = @Revision",
            new { Hash = Hash(code), Expires = DateTimeOffset.UtcNow.AddDays(1).ToString("O"), row.Id, row.Revision });
        CheckChanged(changed);
        return code;
    }

    public FamilyState Join(string user, string code)
    {
        RequireUser(user);
        if (code.Trim().Length != 32) throw new CareException("초대 코드를 확인해주세요.");
        var row = database.Query<FamilyRow>("SELECT * FROM BabyFamilies WHERE InviteHash = @Hash", new { Hash = Hash(code.Trim().ToUpperInvariant()) }).SingleOrDefault();
        if (row is null || !DateTimeOffset.TryParse(row.InviteExpires, out var expires) || expires < DateTimeOffset.UtcNow)
            throw new CareException("초대 코드가 만료되었거나 유효하지 않습니다.");
        var family = Read(row);
        if (!family.Members.Contains(user)) family.Members.Add(user);
        var changed = database.ExecuteNonQuery("UPDATE BabyFamilies SET Payload = @Payload, Revision = Revision + 1, InviteHash = '', InviteExpires = '' WHERE Id = @Id AND Revision = @Revision",
            new { Payload = JsonSerializer.Serialize(family), row.Id, row.Revision });
        CheckChanged(changed);
        return family;
    }

    public void SavePreferences(string user, string familyId, CarePreferences preferences)
    {
        var row = Authorized(user, familyId);
        var family = Read(row);
        if (preferences.Version != family.Preferences.Version)
            throw new CareException("다른 보호자가 설정을 변경했습니다. 새로고침해주세요.");
        CareDashboard.Validate(preferences, DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddHours(14).DateTime));
        if (preferences.FeedingMinutes is < 1 or > 180 || preferences.BurpingMinutes is < 0 or > 180 ||
            preferences.DiaperToSleepMinutes is < 0 or > 180)
            throw new CareException("수유 시간은 1~180분, 나머지 시간은 0~180분으로 입력해주세요.");
        family.Preferences = preferences with { Version = preferences.Version + 1 };
        Write(row, family);
    }

    public void Save(string user, string familyId, CareEntry entry)
    {
        var row = Authorized(user, familyId);
        var family = Read(row);
        catalog.Validate(entry);
        var old = family.Entries.SingleOrDefault(e => e.Id == entry.Id);
        if (entry.ConfirmedFromEstimate && entry.EndedAt is { } proposalEnd && family.Entries.Any(e =>
            e.Id != entry.Id && e.Kind == entry.Kind && e.StartedAt < proposalEnd &&
            (e.EndedAt ?? DateTimeOffset.MaxValue) > entry.StartedAt))
            throw new CareException("겹치는 기록이 이미 있습니다. 새로고침 후 시간을 확인해주세요.");
        if ((old is null && entry.Version != 0) || (old is not null && old.Version != entry.Version))
            throw new CareException("다른 보호자가 수정한 기록입니다. 새로고침 후 다시 수정해주세요.");
        var saved = entry with { Version = entry.Version + 1, UpdatedBy = user, UpdatedAt = DateTimeOffset.UtcNow };
        family.Audit.Add(new(saved.Id, user, old is null ? "create" : "update", saved.UpdatedAt, old));
        if (old is not null) family.Entries.Remove(old);
        family.Entries.Add(saved);
        Write(row, family);
    }

    public void Delete(string user, string familyId, string entryId, int version)
    {
        var row = Authorized(user, familyId);
        var family = Read(row);
        var old = family.Entries.SingleOrDefault(e => e.Id == entryId) ?? throw new CareException("이미 삭제된 기록입니다.");
        if (old.Version != version) throw new CareException("기록이 변경되었습니다. 새로고침해주세요.");
        family.Entries.Remove(old);
        family.Audit.Add(new(entryId, user, "delete", DateTimeOffset.UtcNow, old));
        Write(row, family);
    }

    private FamilyRow Authorized(string user, string id)
    {
        RequireUser(user);
        var row = database.Query<FamilyRow>("SELECT * FROM BabyFamilies WHERE Id = @Id", new { Id = id }).SingleOrDefault();
        if (row is null || !Read(row).Members.Contains(user)) throw new CareException("접근할 수 없는 가족 공간입니다.");
        return row;
    }
    private void Write(FamilyRow row, FamilyState family) => CheckChanged(database.ExecuteNonQuery(
        "UPDATE BabyFamilies SET Payload = @Payload, Revision = Revision + 1 WHERE Id = @Id AND Revision = @Revision",
        new { Payload = JsonSerializer.Serialize(family), row.Id, row.Revision }));
    private static void CheckChanged(int count) { if (count != 1) throw new CareException("다른 보호자의 변경이 있습니다. 새로고침 후 다시 시도해주세요."); }
    private static void RequireUser(string user) { if (string.IsNullOrWhiteSpace(user)) throw new CareException("로그인이 필요합니다."); }
    private static FamilyState Read(FamilyRow row) => JsonSerializer.Deserialize<FamilyState>(row.Payload)!;
    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    private sealed class FamilyRow
    {
        public string Id { get; set; } = "";
        public string Payload { get; set; } = "";
        public long Revision { get; set; }
        public string InviteExpires { get; set; } = "";
    }
}
