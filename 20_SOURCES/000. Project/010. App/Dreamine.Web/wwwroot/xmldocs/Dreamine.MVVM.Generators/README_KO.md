<!--!
\file README_KO.md
\brief Dreamine.MVVM.Generators - Dreamine MVVM용 Roslyn 소스 제너레이터
\details 현재 코드 기준의 패키지 목적, 설치 방법, 아키텍처 역할, 지원 Generator, 제약 사항, 사용 예제를 설명합니다.
\author Dreamine
\date 2026-04-20
\version 1.0.11
-->

# Dreamine.MVVM.Generators

**Dreamine.MVVM.Generators**는 Dreamine MVVM 생태계에서 사용하는 **Roslyn 증분 소스 제너레이터 패키지**입니다.

이 패키지는 Attribute로 선언한 의도를 기반으로 MVVM 보일러플레이트 코드를 **컴파일 타임에 생성**합니다.

현재 기준으로 다루는 주요 Attribute는 다음과 같습니다.

- `DreamineProperty`
- `DreamineEntry`
- `DreamineModel`
- `DreamineEvent`
- `DreamineCommand`

이 패키지의 목표는 반복 코드를 줄이되, 생성 규칙과 제약을 명시적으로 유지하는 것입니다.

[➡️ English Documentation](./README.md)

---

## 이 패키지가 하는 일

MVVM 프로젝트에서는 반복적으로 다음 코드가 필요합니다.

- backing field → property 노출
- method → `ICommand` 프로퍼티 생성
- model / event 참조 노출
- 앱 엔트리 부트스트랩 코드 생성
- 선언형 command forwarding 코드 생성

Dreamine.MVVM.Generators는 이 반복 코드를 **생성 계층**으로 이동시켜 ViewModel 및 App 코드 양을 줄입니다.

---

## 주요 특징

- Roslyn **Incremental Source Generator** 기반
- Analyzer 패키지 형태로 배포 가능
- Dreamine Attribute를 기준으로 코드 생성
- 엔트리 부트스트랩 코드 생성 지원
- 필드 기반 Auto Wiring 지원
- DreamineCommand 기반 직접 실행/forwarding command 생성 지원
- `analyzers/dotnet/cs` 경로로 패키징 가능
- `buildTransitive` 기반 자동 analyzer 등록 지원

---

## 요구 사항

- **대상 프레임워크**: `netstandard2.0`
- 일반적으로 함께 사용되는 패키지:
  - `Dreamine.MVVM.Attributes`
  - `Dreamine.MVVM.Core`
  - WPF / .NET MVVM 애플리케이션

---

## 설치

### 방법 A) NuGet

```bash
dotnet add package Dreamine.MVVM.Generators
```

### 방법 B) PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Generators" Version="1.0.6" />
</ItemGroup>
```

이 패키지는 Analyzer 패키지 형태로 사용되며, `buildTransitive`를 통해 소비 프로젝트에 자동 등록되는 구조를 권장합니다.

---

## 프로젝트 구조

```text
Dreamine.MVVM.Generators
├── DreamineAutoWiringGenerator.cs
├── DreamineCommandSourceGenerator.cs
├── DreamineEntryGenerator.cs
├── AnalyzerReleases.Shipped.md
├── AnalyzerReleases.Unshipped.md
├── buildTransitive/
│   └── Dreamine.MVVM.Generators.targets
└── Dreamine.MVVM.Generators.csproj
```

---

## 아키텍처 역할

이 패키지는 Dreamine MVVM 스택의 **생성 계층**에 속합니다.

```text
ViewModel / App Source Code
        │
        ├─ Dreamine.MVVM.Attributes
        │     (markers / metadata)
        │
        ├─ Dreamine.MVVM.Generators
        │     (compile-time code generation)
        │
        └─ Dreamine.MVVM.Core
              (runtime MVVM infrastructure)
```

책임 분리는 다음과 같습니다.

- **Attributes**: 의도 선언
- **Generators**: 코드 생성
- **Core**: 런타임 동작 수행

---

## 지원 Generator

### 1) DreamineEntryGenerator

`[DreamineEntry]`가 적용된 타입을 기준으로 애플리케이션 부트스트랩 코드를 생성합니다.

#### 현재 역할

- 앱 시작 시 초기화 코드 생성
- `DMContainer.AutoRegisterAll(...)`
- `ViewModelLocator.RegisterAll(...)`
- `FrameworkElement.Loaded` 이벤트 기반 View ↔ ViewModel 자동 연결
- `RegisterBefore`, `RegisterAfter`, `ShowMainWindow` partial hook 생성

#### 현재 제약

- 대상 타입은 **partial** 이어야 함
- 대상 타입은 `System.Windows.Application`을 상속해야 함
- 유효한 엔트리 타입은 하나만 허용하는 방향을 전제로 함

#### 예시

```csharp
using Dreamine.MVVM.Attributes;

[DreamineEntry]
public partial class App : Application
{
}
```

---

### 2) DreamineAutoWiringGenerator

`[DreamineProperty]`, `[DreamineModel]`, `[DreamineEvent]`가 적용된 **필드**를 기준으로 보조 프로퍼티를 생성합니다.

#### 현재 역할

- `_title` → `Title`
- `_model` → `Model`
- `_event` → `Event`

#### 현재 기준

- **필드 기반 생성만 처리**
- 속성(Property) 선언 자체를 다시 생성 대상으로 보지 않음
- partial class에 보조 프로퍼티를 추가 생성
- 기존 멤버와 이름 충돌 시 생성 생략

#### 예시

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineProperty]
    private string _title;

    [DreamineModel]
    private MainModel _model;

    [DreamineEvent]
    private MainEvent _event;
}
```

#### 생성 의도

- `Title` → field-backed property
- `Model` → model access property
- `Event` → event access property

#### 주의 사항

- `[DreamineProperty]` 생성 코드는 `SetProperty(ref field, value)` 사용을 전제로 함  
  즉 대상 타입에 `SetProperty`가 존재해야 함
- `[DreamineModel]`, `[DreamineEvent]`는 현재 생성 정책상 **readonly 필드 사용을 권장하지 않음**
- `DreamineModel`은 `new T()` 초기화 경로를 사용함
- `DreamineEvent`는 `DMContainer.Resolve<T>()` 초기화 경로를 사용함
- 생성 코드에서 더 이상 `ViewModelBase` 상속을 강제하지 않음

---

### 3) DreamineCommandSourceGenerator

`[DreamineCommand]`가 적용된 메서드를 기준으로 `ICommand` 프로퍼티를 생성합니다.

#### 현재 역할

- `{MethodName}Command` 프로퍼티 생성
- 필요 시 `CommandName` override 지원
- `TargetMethod`가 없으면 주석이 붙은 메서드를 직접 실행
- forwarding이 필요한 경우 `TargetMethod` 호출 코드 생성
- forwarding 결과값이 있으면 `BindTo` 프로퍼티에 대입
- `TargetMethod`가 지정되고 메서드 본문이 없을 때 forwarding body 생성
- 외부 `RelayCommand` 타입에 직접 의존하지 않도록 생성 파일 내부에 전용 `ICommand` 구현을 포함하는 방향 사용

#### 예시

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineCommand]
    private void Save()
    {
    }

    [DreamineCommand("Event.ReadmeClick", BindTo = "Readme")]
    partial void LoadReadme();
}
```

#### 현재 제약

- containing type은 **partial** 이어야 함
- 대상 메서드는 **parameterless void** 여야 함
- 본문 없는 forwarding 메서드는 **partial** 이어야 함
- 생성될 command property 이름이 기존 멤버와 충돌하면 생성하지 않음

---

## 빠른 시작

### 1) 필요한 패키지 추가

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Attributes" Version="1.0.6" />
  <PackageReference Include="Dreamine.MVVM.Core" Version="1.0.9" />
  <PackageReference Include="Dreamine.MVVM.Generators" Version="1.0.11" PrivateAssets="all" OutputItemType="Analyzer" />
</ItemGroup>
```

### 2) Attribute 선언

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineProperty]
    private string _title;

    [DreamineCommand]
    private void Save()
    {
    }

    [DreamineCommand("Event.ReadmeClick", BindTo = "Readme")]
    partial void LoadReadme();
}
```

### 3) 빌드

빌드 시 partial source가 생성됩니다.

생성 예:

- `Title` property
- `SaveCommand`
- `LoadReadmeCommand`
- forwarding method body

---

## 현재 코드 기준 중요 메모

### 1) 생성기는 완전 독립형 런타임 프레임워크가 아니다

생성 코드가 참조하는 런타임 개념은 여전히 존재합니다.

예:

- `DMContainer`
- `ViewModelLocator`
- `SetProperty`

즉 이 패키지는 Dreamine MVVM 스택 안에서 사용하는 것을 전제로 합니다.

### 2) Attribute별 사용 범위가 동일하지 않다

현재 기준으로:

- `DreamineEntry` → App / bootstrap 계층
- `DreamineProperty` → `SetProperty` 가능한 ViewModel 계층
- `DreamineModel`, `DreamineEvent` → field access 생성
- `DreamineCommand` → method 기반 command 생성

### 3) 생성 규칙은 점진적으로 엄격해지는 방향이다

현재 Generator 구현은 단순 자동 생성보다 다음을 더 중시합니다.

- partial 타입 검증
- 메서드 시그니처 검증
- 이름 충돌 방지
- 잘못된 사용에 대한 Diagnostic 추가

---

## Packaging Notes

현재 프로젝트는 Analyzer 패키지 방향으로 구성하는 것을 전제로 합니다.

일반적인 구성 포인트:

- `PackageType=Analyzer`
- `OutputItemType=Analyzer`
- `IncludeBuildOutput=false`
- generator DLL을 `analyzers/dotnet/cs`에 패킹
- `buildTransitive`를 통한 자동 등록

---

## 비교

| 패키지 | 역할 | 런타임 로직 | 컴파일 타임 생성 |
|---|---|---:|---:|
| Dreamine.MVVM.Attributes | 선언 계층 | No | No |
| Dreamine.MVVM.Generators | 생성 계층 | No | Yes |
| Dreamine.MVVM.Core | 런타임 계층 | Yes | No |

이 분리는 시스템을 레이어 단위로 유지하기 위한 구조입니다.

---

## 권장 조합

이 패키지는 보통 아래와 함께 사용합니다.

```text
Dreamine.MVVM.Attributes
Dreamine.MVVM.Core
Dreamine WPF / UI / App packages
```

---

## 라이선스

MIT License
