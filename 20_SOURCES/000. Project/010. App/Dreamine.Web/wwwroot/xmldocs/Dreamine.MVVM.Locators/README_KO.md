# Dreamine.MVVM.Locators

Dreamine MVVM 프레임워크를 위한 View ↔ ViewModel 매핑 인프라입니다.

이 라이브러리는 규칙 기반 해석, 수동 등록, 역방향 View 해석, Dependency Injection 연동을 지원하는 경량 ViewModel Locator 를 제공합니다.

이 라이브러리는 **플랫폼 비종속 매핑 엔진**으로 설계되었습니다. 핵심 목적은 View 타입으로부터 적절한 ViewModel 타입을 찾고, ViewModel 타입으로부터 적절한 View 타입을 찾는 것입니다.

`DataContext`, `FrameworkElement`, `Loaded` 이벤트 연결 같은 WPF 전용 처리는 이 라이브러리의 책임이 아닙니다.

[➡️ English README](./README.md)

## 주요 기능

- 규칙 기반 View ↔ ViewModel 매핑
- 수동 ViewModel 등록
- 선택적 DI Resolver 연동
- 어셈블리 자동 스캔
- ViewModel 로부터 View 역방향 해석
- 테스트 격리 및 앱 재구성을 위한 Reset/cache clear API
- 루트 네임스페이스 탐색 지원
- 하위 폴더 및 중첩 네임스페이스 탐색 지원
- 다음과 같은 다양한 네임스페이스 구조 지원:
  - `Views`
  - `View`
  - `Pages`
  - `Windows`
  - `Dialogs`
  - `GUI`
  - `GUIs`
  - `Screens`
  - `Controls`

## 설계 목적

`Dreamine.MVVM.Locators` 는 WPF 전용 타입에 직접 종속되지 않도록 설계되었습니다.

이 라이브러리의 책임은 다음과 같습니다.

- View 타입으로부터 ViewModel 타입 해석
- ViewModel 타입으로부터 View 타입 해석
- 일관된 규칙 기반 매핑 엔진 제공
- 수동 매핑 및 DI 기반 생성 지원

이 라이브러리의 책임이 아닌 것은 다음과 같습니다.

- `DataContext` 설정
- `Loaded` 이벤트 구독
- `Window`, `Page`, `UserControl` 상속 여부 판별

이런 플랫폼 전용 동작은 별도의 WPF 전용 계층에서 처리해야 합니다.

## 매핑 동작 방식

Locator 는 다음 순서로 타입을 찾습니다.

### View → ViewModel

1. 수동 등록 맵 확인
2. 규칙 기반 네임스페이스 후보 탐색
3. 부모 네임스페이스 확장 탐색
4. 루트 네임스페이스 탐색
5. 어셈블리 전체에서 타입명 fallback 검색

예를 들어 View 이름이 `MainWindow` 라면 다음과 같은 후보를 순서대로 찾습니다.

- `MyApp.ViewModels.MainWindowViewModel`
- `MyApp.Views.Admin.MainWindow` → `MyApp.ViewModels.Admin.MainWindowViewModel`
- `MyApp.GUI.Login.MainWindow` → `MyApp.ViewModels.Login.MainWindowViewModel`
- `MyApp.MainWindowViewModel`

### ViewModel → View

1. 규칙 기반 네임스페이스 후보 탐색
2. 부모 네임스페이스 확장 탐색
3. 루트 네임스페이스 탐색
4. 어셈블리 전체에서 타입명 fallback 검색

## 지원 가능한 구조 예시

### 루트 네임스페이스

```csharp
namespace MyApp;

public partial class MainWindow
{
}

public sealed class MainWindowViewModel
{
}
```

### 기본 Views / ViewModels 구조

```csharp
namespace MyApp.Views;

public partial class MainWindow
{
}

namespace MyApp.ViewModels;

public sealed class MainWindowViewModel
{
}
```

### 하위 폴더 구조

```csharp
namespace MyApp.Views.Admin;

public partial class MainWindow
{
}

namespace MyApp.ViewModels.Admin;

public sealed class MainWindowViewModel
{
}
```

### 대체 폴더명 구조

```csharp
namespace MyApp.GUI.Account;

public partial class LoginDialog
{
}

namespace MyApp.ViewModels.Account;

public sealed class LoginDialogViewModel
{
}
```

## Dependency Injection 지원

ViewModel 인스턴스 생성은 등록된 Resolver 를 통해 수행합니다.

```csharp
resolver.Resolve(vmType)
```

ViewModel 생성에는 `Activator.CreateInstance` fallback 이 없습니다. Resolver 가 등록되어 있지 않거나 Resolver 가 `null`을 반환하면, `Resolve(...)`는 두 상황을 구분하는 `InvalidOperationException`을 발생시킵니다.

## 사용 예시

### Resolver 등록

```csharp
ViewModelLocator.RegisterResolver(new MyResolver());
```

### Locator 상태 초기화

```csharp
ViewModelLocator.Clear();         // 매핑과 lookup cache 초기화, resolver 유지
ViewModelLocator.ClearResolver(); // resolver 만 제거
ViewModelLocator.Reset();         // 매핑, lookup cache, resolver 모두 초기화
```

`Reset()`은 단위테스트 격리에 유용합니다. `Clear()`는 DI 전략은 유지하면서 매핑만 다시 구성해야 할 때 사용합니다.

### 수동 매핑 등록

```csharp
ViewModelLocator.Register(typeof(MainWindow), typeof(MainWindowViewModel));
```

### ViewModel 해석

```csharp
var vm = ViewModelLocator.Resolve(typeof(MainWindow));
```

### View 해석

```csharp
var view = ViewModelLocator.ResolveView(typeof(MainWindowViewModel));
```

### 자동 등록

```csharp
ViewModelLocator.RegisterAll(Assembly.GetExecutingAssembly());
```

## 권장 규약

이 Locator 는 폭넓은 매칭을 지원하지만, 권장 구조는 여전히 다음과 같습니다.

- `Views`
- `ViewModels`

이 규약을 유지하면 프로젝트 예측 가능성이 높아지고, 동일 타입명이 여러 개 존재할 때 모호성을 줄일 수 있습니다.

## 참고 사항

- 폭넓은 매칭은 유연성을 높이지만, 동일한 타입명이 여러 개 있을 경우 fallback 매칭이 모호해질 수 있습니다.
- 후보가 여러 개일 가능성이 있으면 수동 등록을 우선하는 것이 안전합니다.
- 이 라이브러리는 매핑과 생성까지를 담당하며, 플랫폼 이벤트 연결은 담당하지 않습니다.
- AppDomain 타입 스캔 결과는 캐시되며, 새 Assembly 가 로드되거나 `Clear()` / `Reset()`이 호출되면 무효화됩니다.

## 라이선스

MIT License
