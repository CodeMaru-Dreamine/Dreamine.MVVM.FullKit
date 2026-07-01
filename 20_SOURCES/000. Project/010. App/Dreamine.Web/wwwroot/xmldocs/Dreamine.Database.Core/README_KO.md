# Dreamine.Database.Core

`Dreamine.Database.Core`는 구체 Dreamine Database Provider들이 공통으로 사용하는 런타임 구현 패키지입니다.

[English documentation](./README.md)

## 패키지 역할

이 패키지는 Provider와 독립적인 CRUD, SQL 생성, Entity Mapping, Dapper 연동, Database Provider 기본 동작을 구현합니다.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
SQLite / MySQL / Oracle / SQL Server providers
```

구체 Provider는 Connection 생성, Identifier Quote, Provider별 SQL Type, Table 생성 Dialect를 제공합니다.

## 주요 기능

- Attribute 기반 Entity Map 생성
- 공통 `DatabaseProviderBase`
- Dapper 기반 Command, Scalar, Query, Insert, Update, Delete
- SQL Type Mapping과 Identifier Quote를 위한 Provider별 확장 지점
- 기존 Table이 있으면 건너뛰는 `CreateTable<T>()` 흐름
- 공통 Provider 계약의 동기/비동기 구현

## Provider 확장 지점

| Member | 역할 |
|---|---|
| `CreateConnection()` | 구체 ADO.NET Connection을 생성합니다. |
| `QuoteIdentifier(string)` | Provider Dialect에 맞게 Table/Column 이름을 감쌉니다. |
| `GetSqlType(DatabasePropertyMap)` | CLR Property Type을 Provider SQL Type으로 매핑합니다. |
| `BuildCreateTableSql(DatabaseEntityMap)` | Provider별 Table 생성 SQL을 만듭니다. |
| `ParameterPrefix` | `@`, `:` 같은 Parameter Prefix를 선택합니다. |

## 설계 원칙

- 공통 CRUD 동작은 한 곳에 둡니다.
- Core 패키지에는 벤더 Driver를 넣지 않습니다.
- SQL Dialect 차이는 구체 Provider 안에 가둡니다.
- 모든 Provider에서 동일한 애플리케이션 API를 유지합니다.

## 의존성

- `Dreamine.Database.Abstractions`
- `Dapper`

## 대상 프레임워크

```text
net8.0
```

## 관련 패키지

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Sqlite`
- `Dreamine.Database.MySql`
- `Dreamine.Database.Oracle`
- `Dreamine.Database.SqlServer`

## 샘플 및 테스트

- 단위 테스트: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF 샘플: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## 라이선스

MIT License
