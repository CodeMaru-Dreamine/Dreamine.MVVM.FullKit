# Dreamine.Database.SqlServer

`Dreamine.Database.SqlServer` is the Microsoft SQL Server provider for the Dreamine Database package family.

[Korean documentation](./README_KO.md)

## Package Role

This package implements `IDatabaseProvider` for SQL Server using `Microsoft.Data.SqlClient`.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.SqlServer
```

## Features

- SQL Server connection creation
- Bracket identifier quoting
- SQL Server type mapping
- Database creation from `master` during `EnsureDatabaseExists()`
- Guarded table creation with `OBJECT_ID`
- CRUD support through the shared `DatabaseProviderBase`

## Quick Start

```csharp
using Dreamine.Database.SqlServer;

var provider = new SqlServerDatabaseProvider(
    "Server=localhost;Database=dreamine_sample;User Id=sa;Password=password;TrustServerCertificate=True;");

provider.EnsureDatabaseExists();
provider.CreateTable<SampleCustomer>();
provider.Insert(new SampleCustomer
{
    Name = "Dreamine",
    Role = "Operator",
    CreatedAt = DateTime.Now
});
```

## Database Creation Note

`EnsureDatabaseExists()` connects to `master`, creates the configured database when it is missing, and then later CRUD operations use the original connection string.

The configured account must have permission to create databases. If the account does not have that permission, create the database manually and then run the sample again.

## Dependencies

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Microsoft.Data.SqlClient`

## Target Framework

```text
net8.0
```

## Samples and Tests

- Unit tests: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF sample: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## License

MIT License
