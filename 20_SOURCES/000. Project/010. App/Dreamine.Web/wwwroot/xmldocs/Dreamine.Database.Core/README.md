# Dreamine.Database.Core

`Dreamine.Database.Core` provides the shared runtime implementation used by concrete Dreamine database providers.

[Korean documentation](./README_KO.md)

## Package Role

This package implements provider-independent CRUD, SQL generation, entity mapping, Dapper integration, and database-provider base behavior.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
SQLite / MySQL / Oracle / SQL Server providers
```

Concrete providers supply connection creation, identifier quoting, provider-specific SQL types, and table creation dialects.

## Features

- Attribute-based entity map generation
- Common `DatabaseProviderBase`
- Dapper-backed command, scalar, query, insert, update, and delete operations
- Provider-specific extension points for SQL type mapping and identifier quoting
- Guarded `CreateTable<T>()` flow that skips existing tables
- Sync and async implementations for the common provider contract

## Provider Extension Points

| Member | Purpose |
|---|---|
| `CreateConnection()` | Creates the concrete ADO.NET connection. |
| `QuoteIdentifier(string)` | Quotes table and column names for the provider dialect. |
| `GetSqlType(DatabasePropertyMap)` | Maps CLR property types to provider SQL types. |
| `BuildCreateTableSql(DatabaseEntityMap)` | Builds provider-specific table creation SQL. |
| `ParameterPrefix` | Selects parameter prefix such as `@` or `:`. |

## Design Principles

- Keep shared CRUD behavior in one place.
- Keep vendor drivers out of the core package.
- Keep SQL dialect differences inside concrete providers.
- Preserve the same application-facing API for every provider.

## Dependencies

- `Dreamine.Database.Abstractions`
- `Dapper`

## Target Framework

```text
net8.0
```

## Related Packages

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Sqlite`
- `Dreamine.Database.MySql`
- `Dreamine.Database.Oracle`
- `Dreamine.Database.SqlServer`

## Samples and Tests

- Unit tests: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF sample: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## License

MIT License
