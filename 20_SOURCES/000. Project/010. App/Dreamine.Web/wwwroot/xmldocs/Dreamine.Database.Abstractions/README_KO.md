# Dreamine.Database.Abstractions

`Dreamine.Database.Abstractions`는 Dreamine Database 패키지군에서 공통으로 사용하는 Provider 중립 계약 레이어입니다.

[English documentation](./README.md)

## 패키지 역할

이 패키지는 Database 계층의 최하위 계약 레이어입니다. 공통 옵션, Provider 식별자, Mapping Attribute, `IDatabaseProvider` CRUD 계약만 포함합니다.

```text
Application / Samples / Tests
        ↓
Dreamine.Database.Abstractions
        ↑
Database.Core / SQLite / MySQL / Oracle / SQL Server
```

구체 Database Provider는 애플리케이션 코드에 벤더 전용 API를 노출하지 않고 이 패키지의 계약에 의존해야 합니다.

## 주요 기능

- `IDatabaseProvider` 공통 CRUD 계약
- `DatabaseProviderKind` Provider 식별자
- `DatabaseConnectionOptions` 연결 메타데이터
- Table, Key, Generated, Ignore, Column Name 매핑 Attribute
- Database 초기화, Table 확인, Command, Scalar Query, Read, Insert, Update, Delete 동기/비동기 API

## Mapping Attribute

| Attribute | 역할 |
|---|---|
| `DatabaseTableAttribute` | Entity의 Table 이름을 재정의합니다. |
| `DatabaseColumnAttribute` | Property의 Column 이름을 재정의합니다. |
| `DatabaseKeyAttribute` | Primary Key Property를 표시합니다. |
| `DatabaseGeneratedAttribute` | Database에서 생성되는 Key Column을 표시합니다. |
| `DatabaseIgnoreAttribute` | Property를 Database Mapping에서 제외합니다. |

## 최소 Entity 예시

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

## 설계 원칙

- 애플리케이션 코드를 구체 DB Driver에서 분리합니다.
- Mapping Metadata를 작고 Attribute 기반으로 유지합니다.
- SQLite, MySQL, Oracle, SQL Server에서 일관된 Provider API를 제공합니다.
- 계약 패키지에는 Dapper 또는 벤더 Driver 의존성을 넣지 않습니다.

## 의존성

없음.

## 대상 프레임워크

```text
net8.0
```

## 관련 패키지

- `Dreamine.Database.Core`
- `Dreamine.Database.Sqlite`
- `Dreamine.Database.MySql`
- `Dreamine.Database.Oracle`
- `Dreamine.Database.SqlServer`

## 샘플 및 테스트

- 단위 테스트: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF 샘플: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## 라이선스

MIT License
