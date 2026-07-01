# Dreamine.Database.Oracle

`Dreamine.Database.Oracle` is the Oracle provider for the Dreamine Database package family.

[Korean documentation](./README_KO.md)

## Package Role

This package implements `IDatabaseProvider` for Oracle using `Oracle.ManagedDataAccess.Core`.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.Oracle
```

## Features

- Oracle connection creation
- Oracle parameter prefix support with `:`
- Uppercase quoted identifiers
- Oracle type mapping
- Identity column table creation
- CRUD support through the shared `DatabaseProviderBase`

## Quick Start

```csharp
using Dreamine.Database.Oracle;

var provider = new OracleDatabaseProvider(
    "User Id=dreamine;Password=password;Data Source=localhost:1521/XEPDB1;");

provider.EnsureDatabaseExists();
provider.CreateTable<SampleCustomer>();
provider.Insert(new SampleCustomer
{
    Name = "Dreamine",
    Role = "Operator",
    CreatedAt = DateTime.Now
});
```

## Schema Note

Oracle usually works with an existing user/schema instead of creating a database from the application connection string. Prepare the user/schema first and grant table creation permissions to that account.

## Dependencies

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Oracle.ManagedDataAccess.Core`

## Target Framework

```text
net8.0
```

## Samples and Tests

- Unit tests: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF sample: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## License

MIT License
