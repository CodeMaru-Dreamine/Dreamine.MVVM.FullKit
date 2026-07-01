# Dreamine.Database.Sqlite

`Dreamine.Database.Sqlite`는 Dreamine Database 패키지군의 SQLite Provider입니다.

[English documentation](./README.md)

## 패키지 역할

이 패키지는 `Microsoft.Data.Sqlite`를 사용해서 SQLite용 `IDatabaseProvider`를 구현합니다.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.Sqlite
```

외부 DB 서버 없이 바로 동작하기 때문에 SampleSmart Database 화면의 기본 Provider로 사용됩니다.

## 주요 기능

- SQLite Connection 생성
- SQLite Identifier Quote
- SQLite Type Mapping
- `CREATE TABLE IF NOT EXISTS` Table 생성
- 공통 `DatabaseProviderBase` 기반 CRUD

## 빠른 시작

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

## 의존성

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Microsoft.Data.Sqlite`

## 대상 프레임워크

```text
net8.0
```

## 샘플 및 테스트

- 단위 테스트: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF 샘플: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## 라이선스

MIT License
