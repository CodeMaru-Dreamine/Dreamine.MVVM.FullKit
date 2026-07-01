# Dreamine.Database.Sqlite

`Dreamine.Database.Sqlite` is the SQLite provider for the Dreamine Database package family.

[Korean documentation](./README_KO.md)

## Package Role

This package implements `IDatabaseProvider` for SQLite using `Microsoft.Data.Sqlite`.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.Sqlite
```

It is the default provider used by the SampleSmart database screen because it works without an external database server.

## Features

- SQLite connection creation
- SQLite identifier quoting
- SQLite type mapping
- `CREATE TABLE IF NOT EXISTS` table creation
- CRUD support through the shared `DatabaseProviderBase`

## Quick Start

```csharp
using Dreamine.Database.Sqlite;

var provider = new SqliteDatabaseProvider("Data Source=SampleSmart.db");

provider.EnsureDatabaseExists();
provider.CreateTable<SampleCustomer>();
provider.Insert(new SampleCustomer
{
    Name = "Dreamine",
    Role = "Operator",
    CreatedAt = DateTime.Now
});

var rows = provider.Query<SampleCustomer>(
    "SELECT Id, Name, Role, CreatedAt FROM SampleCustomers ORDER BY Id DESC");
```

## Dependencies

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Microsoft.Data.Sqlite`

## Target Framework

```text
net8.0
```

## Samples and Tests

- Unit tests: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF sample: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## License

MIT License
