# Dreamine.Identity

Dreamine 및 CodeMaru 웹 애플리케이션에서 공유하는 계정/인증 인프라 라이브러리입니다.

`Dreamine.Identity`는 이메일/비밀번호 계정, OAuth 로그인, SQLite 기반 사용자 저장소, 서브도메인 공유 인증 쿠키, 기본 로그인/계정 페이지를 제공합니다.

[English documentation](./README.md)

---

## 목적

여러 Dreamine 또는 CodeMaru 서비스가 같은 로그인 상태를 공유해야 할 때 사용합니다.

대표적인 사용 대상:

- `codemaru.co.kr`
- `wedding.codemaru.co.kr`
- `thankyou.codemaru.co.kr`
- 같은 계정 DB를 공유해야 하는 향후 Dreamine 계열 서비스

이 패키지는 의도적으로 작게 유지합니다. 서비스별 소유권 규칙, 테넌트 데이터, 권한 정책, 도메인 기능은 각 애플리케이션에 둡니다.

---

## 주요 기능

- 이메일/비밀번호 기반 회원가입 및 로그인
- Google OAuth 로그인
- Naver OAuth 로그인
- Kakao OAuth 로그인
- 서브도메인 간 인증 쿠키 공유
- 여러 서버 프로세스가 같은 쿠키를 읽기 위한 DataProtection 키 공유
- Dreamine Database 기반 SQLite 사용자 저장소
- 기본 로그인, 회원가입, 계정, 로그아웃 페이지
- WPF/Blazor 하이브리드 호스트 연동 헬퍼

---

## 주요 타입

- `AuthOptions`
- `OAuthProviderOptions`
- `AuthUser`
- `IUserStore`
- `SqliteUserStore`
- `AnonymousAuthenticationStateProvider`
- `DreamineIdentityExtensions`

`DreamineIdentityExtensions`가 제공하는 주요 Claim 타입:

- `DreamineUserId`
- `DreamineProvider`

서비스 데이터와 로그인 계정을 연결할 때는 `DreamineUserId`를 안정적인 내부 사용자 키로 사용합니다.

---

## 패키지 경계

이 패키지가 담당하는 것:

- 계정 생성
- 비밀번호 검증 및 변경
- OAuth 콜백 처리
- 사용자 레코드 저장
- 인증 쿠키 설정
- 로그인/계정 HTML 엔드포인트

이 패키지가 담당하지 않는 것:

- 서비스별 테넌트 레코드
- 청첩장 소유권
- 명함 랜딩 페이지 소유권
- CCTV 스트림 권한
- 결제, 역할, 관리자 정책

이런 규칙은 각 소비 애플리케이션에서 관리합니다.

---

## 빠른 시작

### 1. 프로젝트 참조 추가

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\100. Library\Identity\Dreamine.Identity.csproj" />
</ItemGroup>
```

### 2. 인증 설정 추가

```json
{
  "Authentication": {
    "UsersDbPath": "C:\\Codemaru\\App_Data\\codemaru.db",
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "Naver": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "Kakao": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "CookieDomain": ".codemaru.co.kr",
    "CookieName": ".Dreamine.Identity",
    "DataProtectionKeysPath": "C:\\Codemaru\\App_Data\\IdentityKeys",
    "DataProtectionApplicationName": "Dreamine.Identity"
  }
}
```

OAuth Client Secret은 `user-secrets`, 환경 변수, 서버 전용 설정 파일에 보관합니다. 실제 시크릿 값을 저장소에 커밋하지 마세요.

### 3. WPF 호스트 헬퍼 등록

```csharp
using Dreamine.Identity;

builder.Services.AddDreamineIdentityWpfHost();
```

WPF 호스트 쪽에서 안전하게 사용할 수 있는 `AuthenticationStateProvider`를 등록합니다.

### 4. Blazor 서버 호스트에 Identity 등록

```csharp
using Dreamine.Identity;
using Dreamine.Identity.Options;

var authOptions = builder.Configuration
    .GetSection(AuthOptions.SectionName)
    .Get<AuthOptions>() ?? new AuthOptions();

var usersDbPath = builder.Configuration[$"{AuthOptions.SectionName}:UsersDbPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "codemaru.db");

options.AddDreamineIdentity(authOptions, usersDbPath);
```

`AddDreamineIdentity(...)`가 연결하는 구성:

- `IUserStore`
- SQLite 사용자 DB provider
- 쿠키 인증
- OAuth provider
- 인증/인가 middleware
- 기본 인증 엔드포인트

---

## 기본 엔드포인트

표준 엔드포인트:

- `GET /_identity/login`
- `POST /_identity/login`
- `POST /_identity/signup`
- `GET /_identity/account`
- `POST /_identity/account`
- `GET /_identity/signout`

호환 alias:

- `/login`
- `/signup`
- `/account`
- `/signout`

OAuth challenge 엔드포인트:

- `GET /signin/google`
- `GET /signin/naver`
- `GET /signin/kakao`

OAuth callback 경로:

- `/signin-google`
- `/signin-naver`
- `/signin-kakao`

로그인을 시작할 수 있는 모든 배포 도메인에 대해 각 provider 콘솔에 callback URL을 등록해야 합니다.

---

## 공유 로그인 필수 조건

서브도메인 간 로그인 공유를 하려면 참여하는 모든 앱이 아래 값을 동일하게 사용해야 합니다.

```json
{
  "CookieDomain": ".codemaru.co.kr",
  "CookieName": ".Dreamine.Identity",
  "DataProtectionKeysPath": "C:\\Codemaru\\App_Data\\IdentityKeys",
  "DataProtectionApplicationName": "Dreamine.Identity",
  "UsersDbPath": "C:\\Codemaru\\App_Data\\codemaru.db"
}
```

이 중 하나라도 다르면 한 서비스에서는 로그인 상태인데 다른 서비스에서는 익명 사용자로 보일 수 있습니다.

`localhost` 개발 환경에서는 운영 도메인의 서브도메인 공유 쿠키와 동작이 다릅니다. 로컬 테스트에서는 각 포트별 callback URL을 등록하고, 앱을 서로 격리해서 테스트할 때는 `CookieDomain`을 비워두는 것이 안전합니다.

---

## 계정 모델

`AuthUser`는 provider identity 하나당 하나의 계정 레코드를 저장합니다.

논리적 자연키:

```text
(Provider, ProviderKey)
```

같은 이메일 주소가 노출되더라도 provider가 다르면 별도 identity로 취급합니다. 서비스 앱은 이메일, 표시 이름, 닉네임, provider별 계정 문자열이 아니라 내부 `AuthUser.Id`를 소유권 키로 저장해야 합니다.

---

## 의존성

- `Dreamine.Database.Abstractions`
- `Dreamine.Database.Core`
- `Dreamine.Database.Sqlite`
- `Dreamine.Hybrid.Wpf`
- `Microsoft.AspNetCore.App`
- `Microsoft.AspNetCore.Authentication.Google`
- `AspNet.Security.OAuth.Naver`
- `AspNet.Security.OAuth.KakaoTalk`
- `Microsoft.AspNetCore.Components.Authorization`

---

## 대상 프레임워크

```text
net8.0-windows7.0
```

---

## 라이선스

MIT License
