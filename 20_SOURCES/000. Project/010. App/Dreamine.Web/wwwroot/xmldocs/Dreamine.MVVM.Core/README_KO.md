<!--!
\file README_KO.md
\brief Dreamine.MVVM.Core - 의존성 주입 및 자동 등록 런타임 구현 모듈.
\details Dreamine 기본 서비스 컨테이너, 객체 생성, 서비스 수명 관리, 규칙 기반 자동 등록 인프라를 제공합니다.
\author Dreamine Core Team
\date 2026-04-29
\version 1.0.10
-->

# Dreamine.MVVM.Core

**Dreamine.MVVM.Core**는 Dreamine MVVM 프레임워크의 경량 런타임 인프라 구현 모듈입니다.

이 패키지는 Dreamine 모듈에서 사용하는 기본 의존성 주입 구현체를 제공합니다. 주요 역할은 명시적 서비스 등록, 생성자 기반 해석, 싱글턴 인스턴스 관리, 객체 생성, 규칙 기반 자동 등록입니다.

이 패키지는 런타임 인프라에 집중합니다. `Window`, `UserControl`, `FrameworkElement`, `ContentControl`, `DataContext` 같은 WPF 전용 UI 개념에 의존하지 않아야 합니다.

[➡️ English Version](./README.md)

---

## 이 라이브러리가 제공하는 것

Dreamine.MVVM.Core는 다음 기능을 제공합니다.

- `DMContainer` 정적 Facade
- `DreamineContainer` 기본 의존성 컨테이너 구현체
- Thread-safe 등록, 해석, Singleton 생성
- Transient 서비스 등록
- Singleton 서비스 등록
- 팩토리 기반 서비스 등록
- 생성자 기반 의존성 해석
- 싱글턴 인스턴스 캐싱
- 순환 참조 감지
- 규칙 기반 자동 등록
- Assembly 타입 스캔
- `IObjectActivator` 기반 객체 생성
- 테스트 격리를 위한 정적 Facade Reset 및 Container 교체 API

---

## 패키지 역할

`Dreamine.MVVM.Core`는 `Dreamine.MVVM.Interfaces`에 정의된 인프라 계약을 구현합니다.

권장 의존성 방향은 다음과 같습니다.

```text
Dreamine.MVVM.Interfaces
        ↑
Dreamine.MVVM.Core
        ↑
Dreamine.MVVM.Wpf / Dreamine.MVVM.Locators / Application modules
```

`Dreamine.MVVM.Core`에는 WPF 전용 바인딩 또는 내비게이션 로직이 들어가면 안 됩니다. WPF 전용 동작은 WPF 전용 패키지에 위치해야 합니다.

---

## 서비스 수명 정책

Dreamine.MVVM.Core는 명시적인 서비스 수명 정책을 사용합니다.

| 등록 API | 수명 | 설명 |
|---|---:|---|
| `Register<TImplementation>()` | Transient | 해석할 때마다 새 인스턴스를 생성합니다. |
| `Register<TService, TImplementation>()` | Transient | 추상화와 구현체를 매핑합니다. |
| `Register<TService>(Func<TService>)` | Transient | 해석할 때마다 팩토리를 실행합니다. |
| `RegisterSingleton<TService>(TService)` | Singleton | 전달받은 인스턴스를 저장합니다. |
| `RegisterSingleton<TImplementation>()` | Singleton | 최초 해석 시 한 번 생성하고 캐싱합니다. |
| `RegisterSingleton<TService, TImplementation>()` | Singleton | 추상화와 구현체를 싱글턴으로 매핑합니다. |
| `AutoRegisterAll(Assembly)` | 자동 등록 대상 Singleton | Dreamine 자동 등록 동작을 유지합니다. |
| `DMContainer.Reset()` | 정적 Facade 초기화 | 전역 Container를 빈 `DreamineContainer`로 교체합니다. |
| `DMContainer.SetContainer(IServiceContainer)` | 정적 Facade 교체 | 테스트 또는 Host가 격리된 Container를 제공할 수 있습니다. |

> 중요: 자동 등록된 타입은 기본적으로 Singleton으로 등록됩니다. 따라서 자동 발견된 ViewModel, Model, Event, Manager는 애플리케이션이 별도 수명으로 명시 등록하지 않는 한 반복 해석 시 동일 인스턴스를 유지합니다.

`DreamineContainer`는 등록과 해석을 내부적으로 직렬화합니다. Singleton 지연 생성은 보호되므로 여러 스레드가 최초 해석을 동시에 시도해도 하나의 인스턴스만 공유됩니다. 순환 참조 감지는 Container 공유 상태가 아니라 Resolve 호출 단위 Context에 저장됩니다.

---

## 자동 등록

`DMContainer.AutoRegisterAll(rootAssembly)`은 후보 Assembly를 스캔하고 지원되는 구체 타입을 등록합니다.

자동 등록은 다음 구성 요소를 통해 수행됩니다.

- `AutoRegistrationService`
- `AssemblyTypeScanner`
- `NamingConventionAutoRegistrationFilter`

현재 이름 규칙은 다음 패턴에 해당하는 구체 비제네릭 클래스를 대상으로 합니다.

- `*Model`
- `*Event`
- `*ViewModel`
- `.Managers.` 또는 `.xaml.` 네임스페이스 아래의 `*Manager`
- `.xaml.Model`
- `.xaml.Event`
- `.xaml.ViewModel`
- `DreamineRegisterAttribute` 또는 `DreamineAutoRegisterAttribute` 이름의 Attribute가 붙은 타입

조건에 맞는 타입은 Singleton 수명으로 등록됩니다.

---

## 프로젝트 구조

```text
Dreamine.MVVM.Core
├── DMContainer.cs
├── AutoRegistration
│   ├── AssemblyTypeScanner.cs
│   ├── AutoRegistrationService.cs
│   └── NamingConventionAutoRegistrationFilter.cs
└── DependencyInjection
    ├── ConstructorActivator.cs
    ├── ConstructorSelector.cs
    ├── DreamineContainer.cs
    ├── ResolutionContext.cs
    ├── ServiceDescriptor.cs
    └── ServiceLifetime.cs
```

---

## 요구사항

- **.NET**: `net8.0`

---

## 설치

### 프로젝트 참조

```xml
<ItemGroup>
  <ProjectReference Include="..\Dreamine.MVVM.Interfaces\Dreamine.MVVM.Interfaces.csproj" />
  <ProjectReference Include="..\Dreamine.MVVM.Core\Dreamine.MVVM.Core.csproj" />
</ItemGroup>
```

### NuGet

```bash
dotnet add package Dreamine.MVVM.Core
```

---

## 빠른 시작

### Transient 서비스 등록

```csharp
DMContainer.Register<IMyService, MyService>();
```

### 팩토리 등록

```csharp
DMContainer.Register<IMyService>(() => new MyService());
```

### 싱글턴 인스턴스 등록

```csharp
var service = new MyService();
DMContainer.RegisterSingleton<IMyService>(service);
```

### 싱글턴 타입 등록

```csharp
DMContainer.RegisterSingleton<IMyService, MyService>();
```

### 서비스 해석

```csharp
IMyService service = DMContainer.Resolve<IMyService>();
```

### Dreamine 애플리케이션 타입 자동 등록

```csharp
DMContainer.AutoRegisterAll(typeof(App).Assembly);
```

### 테스트에서 정적 Facade 초기화

```csharp
DMContainer.Reset();
```

### 정적 Facade Container 교체

```csharp
DMContainer.SetContainer(new DreamineContainer());
```

---

## 컴포넌트 설명

### DMContainer

`DMContainer`는 기본 `DreamineContainer` 인스턴스를 감싸는 정적 Facade입니다.

역할은 애플리케이션 레벨에서 단순한 사용 방식을 유지하면서 실제 동작을 역할별 인프라 컴포넌트에 위임하는 것입니다.

`DMContainer`는 얇게 유지되어야 합니다. Assembly 스캔, 생성자 선택, 객체 생성, 수명 관리 로직을 직접 포함하면 안 됩니다.

---

### DreamineContainer

`DreamineContainer`는 기본 의존성 컨테이너 구현체입니다.

주요 역할:

- 서비스 등록 정보 저장
- 등록된 서비스 해석
- 생성자 의존성 해석
- 싱글턴 인스턴스 캐싱
- Transient 인스턴스 생성
- 해석 중 순환 참조 감지
- 객체 생성을 `IObjectActivator`에 위임

---

### ConstructorActivator

`ConstructorActivator`는 생성자 주입을 사용해 객체 인스턴스를 생성합니다.

주요 역할:

- `IConstructorSelector`를 통해 생성자 선택
- `IServiceResolver`를 통해 생성자 파라미터 해석
- Reflection 기반 객체 생성

---

### AutoRegistrationService

`AutoRegistrationService`는 규칙 기반 서비스 등록을 수행합니다.

주요 역할:

- 후보 Assembly 스캔
- 지원되는 구체 타입 필터링
- 조건에 맞는 타입을 Singleton 서비스로 등록

---

## 설계 목표

Dreamine.MVVM.Core는 다음을 우선합니다.

- 숨겨진 런타임 마법보다 명시적 동작
- 낮은 의존성 표면적
- 생성자 기반 구성
- 인터페이스 중심 인프라
- 예측 가능한 서비스 수명 동작
- Dreamine 자동 등록과의 호환성
- 계약과 구현의 분리
- WPF 전용 UI 관심사와의 분리

---

## 관련 모듈

일반적으로 다음 Dreamine 패키지와 함께 구성됩니다.

- `Dreamine.MVVM.Interfaces`
- `Dreamine.MVVM.Attributes`
- `Dreamine.MVVM.Generators`
- `Dreamine.MVVM.Locators`
- `Dreamine.MVVM.Locators.Wpf`
- `Dreamine.MVVM.ViewModels`
- `Dreamine.MVVM.Wpf`

---

## License

MIT License
