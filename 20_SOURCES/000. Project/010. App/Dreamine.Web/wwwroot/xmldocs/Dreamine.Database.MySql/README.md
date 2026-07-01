# Dreamine.Database.MySql

`Dreamine.Database.MySql` is the MySQL provider for the Dreamine Database package family.

[Korean documentation](./README_KO.md)

## Package Role

This package implements `IDatabaseProvider` for MySQL using `MySqlConnector`.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.MySql
```

## Features

- MySQL connection creation
- Backtick identifier quoting
- MySQL type mapping
- `CREATE DATABASE IF NOT EXISTS` during `EnsureDatabaseExists()`
- `CREATE TABLE IF NOT EXISTS` table creation
- CRUD support through the shared `DatabaseProviderBase`

## Quick Start

```csharp
using Dreamine.Database.MySql;

var provider = new MySqlDatabaseProvider(
    "Server=localhost;Port=3306;Database=dreamine_sample;User ID=root;Password=1234;");

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

`EnsureDatabaseExists()` connects to the MySQL server without the selected database, creates the database when it is missing, and then later CRUD operations use the original connection string.

The configured account must have permission to create databases. If the account does not have that permission, create the database manually and then run the sample again.

## Dependencies

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `MySqlConnector`

## Target Framework

```text
net8.0
```

## Samples and Tests

- Unit tests: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF sample: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## License

MIT License
