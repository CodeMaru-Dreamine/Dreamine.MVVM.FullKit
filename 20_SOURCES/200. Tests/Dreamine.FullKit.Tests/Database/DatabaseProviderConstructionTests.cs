using Dreamine.Database.Abstractions;
using Dreamine.Database.MySql;
using Dreamine.Database.Oracle;
using Dreamine.Database.SqlServer;
using Dreamine.Database.Sqlite;

namespace Dreamine.FullKit.Tests.Database;

public sealed class DatabaseProviderConstructionTests
{
    [Theory]
    [MemberData(nameof(Providers))]
    public void Providers_ExposeKindAndConnectionString(IDatabaseProvider provider, DatabaseProviderKind expectedKind)
    {
        Assert.Equal(expectedKind, provider.Kind);
        Assert.Equal("Server=localhost;Database=dreamine;User Id=user;Password=password;", provider.ConnectionString);
    }

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

    public static IEnumerable<object[]> Providers()
    {
        const string connectionString = "Server=localhost;Database=dreamine;User Id=user;Password=password;";

        yield return [new SqliteDatabaseProvider(connectionString), DatabaseProviderKind.Sqlite];
        yield return [new MySqlDatabaseProvider(connectionString), DatabaseProviderKind.MySql];
        yield return [new OracleDatabaseProvider(connectionString), DatabaseProviderKind.Oracle];
        yield return [new SqlServerDatabaseProvider(connectionString), DatabaseProviderKind.SqlServer];
    }
}
