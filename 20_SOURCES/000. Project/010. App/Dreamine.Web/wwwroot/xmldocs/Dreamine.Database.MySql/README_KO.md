# Dreamine.Database.MySql

`Dreamine.Database.MySql`은 Dreamine Database 패키지군의 MySQL Provider입니다.

[English documentation](./README.md)

## 패키지 역할

이 패키지는 `MySqlConnector`를 사용해서 MySQL용 `IDatabaseProvider`를 구현합니다.

```text
Dreamine.Database.Abstractions
        ↑
Dreamine.Database.Core
        ↑
Dreamine.Database.MySql
```

## 주요 기능

- MySQL Connection 생성
- Backtick Identifier Quote
- MySQL Type Mapping
- `EnsureDatabaseExists()`에서 `CREATE DATABASE IF NOT EXISTS` 수행
- `CREATE TABLE IF NOT EXISTS` Table 생성
- 공통 `DatabaseProviderBase` 기반 CRUD

## 빠른 시작

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

## Database 생성 참고

`EnsureDatabaseExists()`는 선택된 Database 없이 MySQL 서버에 접속한 뒤 Database가 없으면 생성합니다. 이후 CRUD 작업은 원래 Connection String을 사용합니다.

설정한 계정에는 Database 생성 권한이 있어야 합니다. 권한이 없다면 Database를 수동으로 만든 뒤 샘플을 다시 실행하세요.

## 의존성

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `MySqlConnector`

## 대상 프레임워크

```text
net8.0
```

## 샘플 및 테스트

- 단위 테스트: `20_SOURCES/200. Tests/Dreamine.FullKit.Tests/Database`
- WPF 샘플: `20_SOURCES/998. DEMO/000. Sample/010. Wpfs/SampleSmart/Pages/PageSub/PageDatabase.xaml`

## 라이선스

MIT License
