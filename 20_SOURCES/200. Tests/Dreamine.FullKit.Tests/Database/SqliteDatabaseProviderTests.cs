using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.Sqlite;

namespace Dreamine.FullKit.Tests.Database;

/// <summary>
/// \if KO
/// <para>Sqlite Database Provider Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates sqlite database provider tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SqliteDatabaseProviderTests
{
    /// <summary>
    /// \if KO
    /// <para>Sqlite Provider Creates Table And Runs Crud 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sqlite provider creates table and runs crud operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Sqlite Provider Sql Cache Produces Deterministic Sql For Same Entity Type 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sqlite provider sql cache produces deterministic sql for same entity type operation.</para>
    /// \endif
    /// </summary>
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

/// <summary>
/// \if KO
/// <para>Sqlite Provider Supports Async Operations 작업을 수행합니다.</para>
/// \endif
/// \if EN
/// <para>Performs the sqlite provider supports async operations operation.</para>
/// \endif
/// </summary>
/// <returns>
/// \if KO
/// <para>Sqlite Provider Supports Async Operations 작업에서 생성한 <c>Task</c> 결과입니다.</para>
/// \endif
/// \if EN
/// <para>The <c>Task</c> result produced by the sqlite provider supports async operations operation.</para>
/// \endif
/// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Temporary Sqlite Database 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates temporary sqlite database functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class TemporarySqliteDatabase : IDisposable
    {
        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="TemporarySqliteDatabase"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="TemporarySqliteDatabase"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="path">
        /// \if KO
        /// <para>path에 사용할 <c>string</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>string</c> value used for path.</para>
        /// \endif
        /// </param>
        private TemporarySqliteDatabase(string path)
        {
            Path = path;
            ConnectionString = $"Data Source={path};Pooling=False";
        }

        /// <summary>
        /// \if KO
        /// <para>Path 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the path value.</para>
        /// \endif
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// \if KO
        /// <para>Connection String 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the connection string value.</para>
        /// \endif
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// \if KO
        /// <para>값을 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Creates the value.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Create 작업에서 생성한 <c>TemporarySqliteDatabase</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>TemporarySqliteDatabase</c> result produced by the create operation.</para>
        /// \endif
        /// </returns>
        public static TemporarySqliteDatabase Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "Dreamine.Database.Tests",
                Guid.NewGuid().ToString("N") + ".db");

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            return new TemporarySqliteDatabase(path);
        }

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Sample User 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates sample user functionality and related state.</para>
    /// \endif
    /// </summary>
    [DatabaseTable("SampleUsers")]
    private sealed class SampleUser
    {
        /// <summary>
        /// \if KO
        /// <para>Id 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the id value.</para>
        /// \endif
        /// </summary>
        [DatabaseKey]
        public int Id { get; set; }

        /// <summary>
        /// \if KO
        /// <para>Name 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the name value.</para>
        /// \endif
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// \if KO
        /// <para>Age 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the age value.</para>
        /// \endif
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// \if KO
        /// <para>Ignored 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the ignored value.</para>
        /// \endif
        /// </summary>
        [DatabaseIgnore]
        public string? Ignored { get; set; }
    }
}
