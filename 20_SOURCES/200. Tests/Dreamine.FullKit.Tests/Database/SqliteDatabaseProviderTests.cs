using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.Sqlite;

namespace Dreamine.FullKit.Tests.Database;

public sealed class SqliteDatabaseProviderTests
{
    [Fact]
    public void SqliteProvider_CreatesTableAndRunsCrud()
    {
        using var database = TemporarySqliteDatabase.Create();
        var provider = new SqliteDatabaseProvider(database.ConnectionString);

        provider.EnsureDatabaseExists();
        provider.CreateTable<SampleUser>();

        Assert.True(provider.IsTableExists<SampleUser>());

        Assert.True(provider.Insert(new SampleUser
        {
            Id = 1,
            Name = "Minsu",
            Age = 30,
            Ignored = "not persisted"
        }));

        var inserted = provider.Query<SampleUser>("SELECT Id, Name, Age FROM SampleUsers WHERE Id = @Id", new { Id = 1 })
            .Single();

        Assert.Equal("Minsu", inserted.Name);
        Assert.Equal(30, inserted.Age);

        inserted.Name = "Dreamine";
        inserted.Age = 31;

        Assert.True(provider.Update(inserted));
        Assert.Equal("Dreamine", provider.ExecuteScalar<string>("SELECT Name FROM SampleUsers WHERE Id = @Id", new { Id = 1 }));

        Assert.True(provider.Delete(inserted));
        Assert.Equal(0, provider.ExecuteScalar<long>("SELECT COUNT(1) FROM SampleUsers"));
    }

    [Fact]
    public void SqliteProvider_SqlCache_ProducesDeterministicSqlForSameEntityType()
    {
        // Two separate provider instances that share the same static SQL cache.
        // The observable contract is that the same entity type always produces identical SQL
        // regardless of which provider instance triggers the build.
        using var db1 = TemporarySqliteDatabase.Create();
        using var db2 = TemporarySqliteDatabase.Create();
        var p1 = new SqliteDatabaseProvider(db1.ConnectionString);
        var p2 = new SqliteDatabaseProvider(db2.ConnectionString);

        var method = typeof(SqliteDatabaseProvider)
            .BaseType!
            .GetMethod("BuildInsertSql", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var map = Dreamine.Database.Core.Mapping.DatabaseEntityMap.Create<SampleUser>();
        var sql1 = (string)method.Invoke(p1, [map])!;
        var sql2 = (string)method.Invoke(p2, [map])!;

        Assert.NotEmpty(sql1);
        Assert.Equal(sql1, sql2);
        Assert.Contains("INSERT INTO", sql1);
        Assert.Contains("SampleUsers", sql1);
    }

[Fact]
    public async Task SqliteProvider_SupportsAsyncOperations()
    {
        using var database = TemporarySqliteDatabase.Create();
        var provider = new SqliteDatabaseProvider(database.ConnectionString);

        await provider.EnsureDatabaseExistsAsync();
        await provider.CreateTableAsync<SampleUser>();

        Assert.True(await provider.IsTableExistsAsync<SampleUser>());

        var inserted = await provider.InsertAsync(new SampleUser
        {
            Id = 2,
            Name = "Async",
            Age = 7
        });

        Assert.True(inserted);

        var rows = await provider.QueryAsync<SampleUser>(
            "SELECT Id, Name, Age FROM SampleUsers WHERE Name = @Name",
            new { Name = "Async" });

        Assert.Single(rows);
        Assert.Equal(7, rows[0].Age);

        rows[0].Age = 8;
        Assert.True(await provider.UpdateAsync(rows[0]));
        Assert.Equal(8, await provider.ExecuteScalarAsync<int>("SELECT Age FROM SampleUsers WHERE Id = @Id", new { Id = 2 }));
    }

    private sealed class TemporarySqliteDatabase : IDisposable
    {
        private TemporarySqliteDatabase(string path)
        {
            Path = path;
            ConnectionString = $"Data Source={path};Pooling=False";
        }

        public string Path { get; }

        public string ConnectionString { get; }

        public static TemporarySqliteDatabase Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "Dreamine.Database.Tests",
                Guid.NewGuid().ToString("N") + ".db");

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            return new TemporarySqliteDatabase(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    [DatabaseTable("SampleUsers")]
    private sealed class SampleUser
    {
        [DatabaseKey]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        [DatabaseIgnore]
        public string? Ignored { get; set; }
    }
}
