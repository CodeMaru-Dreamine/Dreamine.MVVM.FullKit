using Dreamine.Database.Abstractions;
using Dreamine.Database.MySql;
using Dreamine.Database.Oracle;
using Dreamine.Database.SqlServer;
using Dreamine.Database.Sqlite;

namespace Dreamine.FullKit.Tests.Database;

/// <summary>
/// \if KO
/// <para>Database Provider Construction Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates database provider construction tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DatabaseProviderConstructionTests
{
    /// <summary>
    /// \if KO
    /// <para>Providers Expose Kind And Connection String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the providers expose kind and connection string operation.</para>
    /// \endif
    /// </summary>
    /// <param name="provider">
    /// \if KO
    /// <para>provider에 사용할 <c>IDatabaseProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IDatabaseProvider</c> value used for provider.</para>
    /// \endif
    /// </param>
    /// <param name="expectedKind">
    /// \if KO
    /// <para>expected Kind에 사용할 <c>DatabaseProviderKind</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DatabaseProviderKind</c> value used for expected kind.</para>
    /// \endif
    /// </param>
    [Theory]
    [MemberData(nameof(Providers))]
    public void Providers_ExposeKindAndConnectionString(IDatabaseProvider provider, DatabaseProviderKind expectedKind)
    {
        Assert.Equal(expectedKind, provider.Kind);
        Assert.Equal("Server=localhost;Database=dreamine;User Id=user;Password=password;", provider.ConnectionString);
    }

    /// <summary>
    /// \if KO
    /// <para>Providers Can Be Consumed Through Small Role Interfaces 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the providers can be consumed through small role interfaces operation.</para>
    /// \endif
    /// </summary>
    /// <param name="provider">
    /// \if KO
    /// <para>provider에 사용할 <c>IDatabaseProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IDatabaseProvider</c> value used for provider.</para>
    /// \endif
    /// </param>
    /// <param name="expectedKind">
    /// \if KO
    /// <para>expected Kind에 사용할 <c>DatabaseProviderKind</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DatabaseProviderKind</c> value used for expected kind.</para>
    /// \endif
    /// </param>
    [Theory]
    [MemberData(nameof(Providers))]
    public void Providers_CanBeConsumedThroughSmallRoleInterfaces(IDatabaseProvider provider, DatabaseProviderKind expectedKind)
    {
        IDatabaseConnection connection = provider;
        IDatabaseSchemaProvider schema = provider;
        IDatabaseCommandExecutor commands = provider;
        IDatabaseQueryProvider queries = provider;
        IDatabaseRepository repository = provider;

        Assert.Equal(expectedKind, connection.Kind);
        Assert.NotNull(schema);
        Assert.NotNull(commands);
        Assert.NotNull(queries);
        Assert.NotNull(repository);
    }

    /// <summary>
    /// \if KO
    /// <para>Providers 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the providers operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Providers 작업에서 생성한 <c>IEnumerable&lt;object[]&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;object[]&gt;</c> result produced by the providers operation.</para>
    /// \endif
    /// </returns>
    public static IEnumerable<object[]> Providers()
    {
        const string connectionString = "Server=localhost;Database=dreamine;User Id=user;Password=password;";

        yield return [new SqliteDatabaseProvider(connectionString), DatabaseProviderKind.Sqlite];
        yield return [new MySqlDatabaseProvider(connectionString), DatabaseProviderKind.MySql];
        yield return [new OracleDatabaseProvider(connectionString), DatabaseProviderKind.Oracle];
        yield return [new SqlServerDatabaseProvider(connectionString), DatabaseProviderKind.SqlServer];
    }
}
