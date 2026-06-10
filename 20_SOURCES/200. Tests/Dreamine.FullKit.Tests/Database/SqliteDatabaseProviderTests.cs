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
