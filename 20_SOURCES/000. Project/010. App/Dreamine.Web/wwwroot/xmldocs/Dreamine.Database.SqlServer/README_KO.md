# Dreamine.Database.SqlServer

`Dreamine.Database.SqlServer`는 Dreamine Database 패키지군의 Microsoft SQL Server Provider입니다.

[English documentation](./README.md)

## 패키지 역할

이 패키지는 `Microsoft.Data.SqlClient`를 사용해서 SQL Server용 `IDatabaseProvider`를 구현합니다.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.SqlServer
```

## 주요 기능

- SQL Server Connection 생성
- Bracket Identifier Quote
- SQL Server Type Mapping
- `EnsureDatabaseExists()`에서 `master` 접속 후 Database 생성
- `OBJECT_ID` 기반 Table 생성 Guard
- 공통 `DatabaseProviderBase` 기반 CRUD

## 빠른 시작

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

## Database 생성 참고

`EnsureDatabaseExists()`는 `master`에 접속한 뒤 설정된 Database가 없으면 생성합니다. 이후 CRUD 작업은 원래 Connection String을 사용합니다.

설정한 계정에는 Database 생성 권한이 있어야 합니다. 권한이 없다면 Database를 수동으로 만든 뒤 샘플을 다시 실행하세요.

## 의존성

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Microsoft.Data.SqlClient`

## 대상 프레임워크

```text
net8.0
```

## 샘플 및 테스트

- 단위 테스트: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF 샘플: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## 라이선스

MIT License
