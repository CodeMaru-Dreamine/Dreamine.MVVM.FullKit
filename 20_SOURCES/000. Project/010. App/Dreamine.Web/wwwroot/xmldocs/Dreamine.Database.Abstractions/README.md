# Dreamine.Database.Abstractions

`Dreamine.Database.Abstractions` defines the provider-neutral contracts for the Dreamine Database package family.

[Korean documentation](./README_KO.md)

## Package Role

This package is the lowest-level database contract layer. It contains only shared options, provider identifiers, mapping attributes, and the `IDatabaseProvider` CRUD contract.

```text
Application / Samples / Tests
        ↓
Dreamine.Database.Abstractions
        ↑
Database.Core / SQLite / MySQL / Oracle / SQL Server
```

Concrete database providers should depend on this package instead of exposing vendor-specific APIs to application code.

## Features

- `IDatabaseProvider` common CRUD contract
- `DatabaseProviderKind` provider identifier
- `DatabaseConnectionOptions` connection metadata
- Mapping attributes for table, key, generated, ignored, and column names
- Sync and async APIs for database initialization, table checks, commands, scalar queries, reads, inserts, updates, and deletes

## Mapping Attributes

| Attribute | Purpose |
|---|---|
| `DatabaseTableAttribute` | Overrides the table name for an entity. |
| `DatabaseColumnAttribute` | Overrides the column name for a property. |
| `DatabaseKeyAttribute` | Marks the primary key property. |
| `DatabaseGeneratedAttribute` | Marks a database-generated key column. |
| `DatabaseIgnoreAttribute` | Excludes a property from database mapping. |

## Minimal Entity

```csharp
using Dreamine.Database.Abstractions.Mapping;

[DatabaseTable("SampleCustomers")]
public sealed class SampleCustomer
{
    [DatabaseKey]
    [DatabaseGenerated]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
```

## Design Principles

- Keep application code independent from concrete database drivers.
- Keep mapping metadata small and attribute-based.
- Keep provider APIs consistent across SQLite, MySQL, Oracle, and SQL Server.
- Avoid bringing Dapper or vendor driver dependencies into the contract package.

## Dependencies

None.

## Target Framework

```text
net8.0
```

## Related Packages

- `Dreamine.Database.Core`
- `Dreamine.Database.Sqlite`
- `Dreamine.Database.MySql`
- `Dreamine.Database.Oracle`
- `Dreamine.Database.SqlServer`

## Samples and Tests

- Unit tests: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF sample: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## License

MIT License
