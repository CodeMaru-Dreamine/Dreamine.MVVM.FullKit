<!--!
\file README_ko.md
\brief Dreamine.MVVM.Interfaces - Dreamine MVVM 모듈을 위한 공유 추상 계약 패키지.
\details 구체 런타임 구현 없이 의존성 주입, 객체 생성, 자동 등록, ViewModel 해석, 내비게이션, 이벤트 계약을 정의합니다.
\author Dreamine Core Team
\date 2026-04-29
\version 1.0.5
-->

# Dreamine.MVVM.Interfaces

**Dreamine.MVVM.Interfaces**는 Dreamine MVVM 프레임워크 전반에서 사용하는 공유 추상 계약을 정의합니다.

이 패키지는 계약만 포함합니다. 구체 의존성 컨테이너, WPF 내비게이션 구현체, ViewModel Locator 구현체, 런타임 객체 생성 로직은 제공하지 않습니다.

이 패키지의 목적은 Dreamine 모듈 간 결합도를 낮추고 프레임워크 레이어 사이의 올바른 의존성 방향을 유지하는 것입니다.

[➡️ English Version](./README.md)

---

## 이 라이브러리가 제공하는 것

Dreamine.MVVM.Interfaces는 다음 계약을 제공합니다.

- 의존성 등록
- 의존성 해석
- 서비스 컨테이너 구성
- 생성자 선택
- 객체 생성
- Assembly 타입 스캔
- 자동 등록
- ViewModel 해석
- 내비게이션
- 이벤트 기본 마커
- Window 상태 payload 계약

---

## 패키지 역할

`Dreamine.MVVM.Interfaces`는 가장 낮은 레벨의 공유 계약 패키지로 유지되어야 합니다.

권장 의존성 방향은 다음과 같습니다.

```text
Dreamine.MVVM.Interfaces
        ↑
Dreamine.MVVM.Core
        ↑
Dreamine.MVVM.Locators / Dreamine.MVVM.Wpf / Application modules
```

규칙:

- `Dreamine.MVVM.Interfaces`는 `Dreamine.MVVM.Core`에 의존하면 안 됩니다.
- `Dreamine.MVVM.Interfaces`는 WPF 전용 패키지에 의존하면 안 됩니다.
- 구현체는 상위 레벨 패키지에 위치해야 합니다.
- 소비자는 필요한 최소 인터페이스에만 의존해야 합니다.

---

## 프로젝트 구조

```text
Dreamine.MVVM.Interfaces
├── DependencyInjection
│   ├── IAssemblyTypeScanner.cs
│   ├── IAutoRegistrationService.cs
│   ├── IConstructorSelector.cs
│   ├── IObjectActivator.cs
│   ├── IServiceContainer.cs
│   ├── IServiceRegistry.cs
│   └── IServiceResolver.cs
├── Events
│   └── IEventBase.cs
├── Locators
│   └── IViewModelResolver.cs
├── Navigation
│   ├── INavigator.cs
│   └── IViewManager.cs
└── Windows
    ├── IWindowStateChange.cs
    ├── IWindowStateService.cs
    └── WindowStateChangedEventArgs.cs
```

---

## 의존성 주입 계약

### IServiceRegistry

서비스 등록 작업을 정의합니다.

지원하는 등록 개념:

- Transient 구체 타입 등록
- Transient 추상화-구현체 등록
- 팩토리 등록
- 싱글턴 인스턴스 등록
- 싱글턴 구체 타입 등록
- 싱글턴 추상화-구현체 등록
- 등록 여부 확인

구현체 사용 예:

```csharp
registry.Register<IMyService, MyService>();
registry.RegisterSingleton<ISharedState, SharedState>();
```

---

### IServiceResolver

서비스 해석 작업을 정의합니다.

```csharp
TService service = resolver.Resolve<TService>();
object service = resolver.Resolve(typeof(TService));
```

---

### IServiceContainer

`IServiceRegistry`와 `IServiceResolver`를 결합합니다.

등록과 해석이 모두 필요한 컴포넌트에서만 사용하는 것이 좋습니다.

인터페이스 분리 원칙을 유지하려면 다음 기준을 따릅니다.

- 등록만 필요하면 `IServiceRegistry`에 의존
- 해석만 필요하면 `IServiceResolver`에 의존

---

### IConstructorSelector

객체 생성 시 사용할 생성자를 선택합니다.

구현체는 다음과 같은 정책을 사용할 수 있습니다.

- 파라미터 수가 가장 많은 생성자 선택
- 명시적 Attribute 기반 선택
- 파라미터 해석 가능 여부 기반 선택

---

### IObjectActivator

생성자 주입을 사용해 객체 인스턴스를 생성합니다.

`IServiceResolver`를 전달받기 때문에 구체 컨테이너에 직접 의존하지 않고 생성자 의존성을 해석할 수 있습니다.

---

### IAssemblyTypeScanner

Assembly를 스캔하고 로드 가능한 타입을 반환합니다.

부분적으로만 로드 가능한 Assembly나 `ReflectionTypeLoadException` 상황을 구현체가 안전하게 처리할 수 있도록 분리된 계약입니다.

---

### IAutoRegistrationService

규칙 기반 자동 등록 계약을 정의합니다.

구현체가 결정하는 영역:

- 어떤 Assembly를 스캔할지
- 어떤 타입을 등록 대상으로 볼지
- 어떤 수명 정책을 적용할지

---

## Locator 계약

### IViewModelResolver

최소 ViewModel 해석 전략을 정의합니다.

```csharp
object? viewModel = resolver.Resolve(typeof(MainWindowViewModel));
```

대표 구현체:

- DI 기반 ViewModel Resolver
- 수동 팩토리 Resolver
- 테스트용 Resolver

---

## Navigation 계약

### INavigator

Dreamine region navigation 구현체에서 사용하는 최소 인스턴스 기반 내비게이션 계약입니다.

```csharp
navigator.Navigate(viewModel);
```

`INavigator`는 WPF UI 타입에 의존하지 않도록 object ViewModel을 받습니다. 구체 내비게이션 동작은 WPF 전용 패키지에 위치해야 합니다.

### IViewManager

더 넓은 View 표시 계약입니다. `INavigator`를 상속하고 타입 기반 ViewModel 해석을 추가합니다.

```csharp
viewManager.Show<MainViewModel>();
viewManager.Show(typeof(MainViewModel));
viewManager.Navigate(existingViewModel);
```

애플리케이션 레벨 View 표시는 `IViewManager`를 사용하고, region/content-control 기반 내비게이션만 필요할 때 `INavigator`를 사용합니다.

---

## Event 계약

### IEventBase

Dreamine Event 클래스의 마커 인터페이스입니다.

Source Generator, Scanner, Framework Convention이 구체 의존성 없이 Event 객체를 식별하는 데 사용할 수 있습니다.

이 마커만으로 자동 생성 대상을 결정하면 안 됩니다. 현재 Dreamine 생성기는 `[DreamineEvent]` 같은 명시적 Attribute를 기준으로 생성 후보를 고르며, scanner도 이 마커를 Attribute, namespace, naming rule과 함께 사용해야 합니다.

---

## Window 상태 계약

### IWindowStateChange

Window open-state 변경 payload 모양을 설명합니다.

### IWindowStateService

Window open/close 상태를 추적하고 `WindowStateChangedEventArgs`를 발생시킵니다.

`WindowStateChangedEventArgs`는 `IWindowStateService` 계약의 일부라 이 패키지에 남겨두되, `IWindowStateChange`를 구현하게 해서 필요한 소비자는 payload 추상화에 의존할 수 있게 했습니다.

---

## 설계 목표

Dreamine.MVVM.Interfaces는 다음을 우선합니다.

- 의존성 역전
- 인터페이스 분리
- 모듈 간 낮은 결합도
- 테스트 가능한 인프라 경계
- UI 프레임워크 독립성
- 교체 가능한 구현체
- Core, Locators, WPF, 애플리케이션 모듈을 위한 안정적인 계약

---

## 요구사항

- **.NET**: `net8.0`
- WPF 의존성 없음
- 구체 런타임 구현 의존성 없음

---

## 설치

```bash
dotnet add package Dreamine.MVVM.Interfaces
```

또는 프로젝트 파일에 직접 추가합니다.

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Interfaces" Version="1.0.4" />
</ItemGroup>
```

---

## 관련 모듈

대표 구현체 및 소비 모듈:

- `Dreamine.MVVM.Core`
- `Dreamine.MVVM.Locators`
- `Dreamine.MVVM.Locators.Wpf`
- `Dreamine.MVVM.Wpf`
- `Dreamine.MVVM.Behaviors.Core`
- `Dreamine.MVVM.Behaviors.Wpf`

---

## License

MIT License
