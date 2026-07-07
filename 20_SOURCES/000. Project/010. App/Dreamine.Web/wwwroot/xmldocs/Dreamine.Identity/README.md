# Dreamine.Identity

Shared identity and authentication infrastructure for Dreamine and CodeMaru web applications.

`Dreamine.Identity` provides local email/password accounts, OAuth login, a SQLite-backed user store, shared authentication cookies, and built-in account pages for Dreamine family services.

[Korean documentation](./README_ko.md)

---

## Purpose

Use this package when multiple Dreamine or CodeMaru services should share the same login state.

Typical examples:

- `codemaru.co.kr`
- `wedding.codemaru.co.kr`
- `thankyou.codemaru.co.kr`
- future Dreamine family services that need the same account database

The package is intentionally small: application-specific ownership rules, tenant data, service permissions, and domain features should stay in each app.

---

## Key Features

- Local email/password signup and login
- Google OAuth login
- Naver OAuth login
- Kakao OAuth login
- Shared cookie authentication across subdomains
- Shared DataProtection key support for multi-process deployments
- SQLite user store through Dreamine Database providers
- Built-in login, signup, account, and sign-out pages
- WPF/Blazor hybrid host integration helpers

---

## Main Types

- `AuthOptions`
- `OAuthProviderOptions`
- `AuthUser`
- `IUserStore`
- `SqliteUserStore`
- `AnonymousAuthenticationStateProvider`
- `DreamineIdentityExtensions`

Important claim types exposed by `DreamineIdentityExtensions`:

- `DreamineUserId`
- `DreamineProvider`

Use `DreamineUserId` as the stable internal user key when connecting service data to a logged-in account.

---

## Package Boundary

This package owns:

- account creation
- password validation and password change
- OAuth callbacks
- user record persistence
- authentication cookie configuration
- login/account HTML endpoints

This package does not own:

- service-specific tenant records
- wedding invitation ownership
- card landing page ownership
- CCTV stream permissions
- billing, roles, or admin policies

Those rules belong to the consuming application.

---

## Quick Start

### 1) Add a project reference

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\100. Library\Identity\Dreamine.Identity.csproj" />
</ItemGroup>
```

### 2) Configure authentication settings

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

Keep OAuth secrets in user secrets, environment variables, or server-only configuration files. Do not commit real client secrets.

### 3) Register WPF host helpers

```csharp
using Dreamine.Identity;

builder.Services.AddDreamineIdentityWpfHost();
```

This registers a safe `AuthenticationStateProvider` for the WPF host side.

### 4) Register identity in the Blazor server host

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

`AddDreamineIdentity(...)` wires:

- `IUserStore`
- SQLite user database provider
- cookie authentication
- OAuth providers
- authentication/authorization middleware
- built-in auth endpoints

---

## Built-in Endpoints

Canonical endpoints:

- `GET /_identity/login`
- `POST /_identity/login`
- `POST /_identity/signup`
- `GET /_identity/account`
- `POST /_identity/account`
- `GET /_identity/signout`

Compatibility aliases:

- `/login`
- `/signup`
- `/account`
- `/signout`

OAuth challenge endpoints:

- `GET /signin/google`
- `GET /signin/naver`
- `GET /signin/kakao`

OAuth callback paths:

- `/signin-google`
- `/signin-naver`
- `/signin-kakao`

Register these callback URLs in each provider console for every deployed domain that can initiate login.

---

## Shared Login Requirements

For subdomain login sharing, every participating app must use the same values:

```json
{
  "CookieDomain": ".codemaru.co.kr",
  "CookieName": ".Dreamine.Identity",
  "DataProtectionKeysPath": "C:\\Codemaru\\App_Data\\IdentityKeys",
  "DataProtectionApplicationName": "Dreamine.Identity",
  "UsersDbPath": "C:\\Codemaru\\App_Data\\codemaru.db"
}
```

If any one of these differs, users may appear logged in on one service but anonymous on another.

For local development on `localhost`, shared subdomain cookies do not behave the same way as production domains. Use provider callbacks for each local port, and leave `CookieDomain` empty if testing isolated local apps.

---

## Account Model

`AuthUser` stores one logical account row per provider identity.

Natural key:

```text
(Provider, ProviderKey)
```

Different providers are treated as separate identities even when they expose the same email address. Service apps should store ownership by the internal `AuthUser.Id`, not by email, display name, nickname, or provider-specific account text.

---

## Dependencies

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

## Target Framework

```text
net8.0-windows7.0
```

---

## License

MIT License
