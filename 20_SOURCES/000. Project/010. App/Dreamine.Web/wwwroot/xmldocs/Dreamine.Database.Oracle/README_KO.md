# Dreamine.Database.Oracle

`Dreamine.Database.Oracle`은 Dreamine Database 패키지군의 Oracle Provider입니다.

[English documentation](./README.md)

## 패키지 역할

이 패키지는 `Oracle.ManagedDataAccess.Core`를 사용해서 Oracle용 `IDatabaseProvider`를 구현합니다.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.Oracle
```

## 주요 기능

- Oracle Connection 생성
- `:` 기반 Oracle Parameter Prefix 지원
- 대문자 Quoted Identifier
- Oracle Type Mapping
- Identity Column Table 생성
- 공통 `DatabaseProviderBase` 기반 CRUD

## 빠른 시작

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

## Schema 참고

Oracle은 보통 애플리케이션 Connection String으로 Database를 생성하기보다, 미리 만든 사용자/Schema에 접속하는 방식으로 사용합니다. 먼저 사용자/Schema를 준비하고 해당 계정에 Table 생성 권한을 부여하세요.

## 의존성

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Oracle.ManagedDataAccess.Core`

## 대상 프레임워크

```text
net8.0
```

## 샘플 및 테스트

- 단위 테스트: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF 샘플: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## 라이선스

MIT License
