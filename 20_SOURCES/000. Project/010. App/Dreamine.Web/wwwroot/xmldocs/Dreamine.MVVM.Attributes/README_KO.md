<!--!
\file README_KO.md
\brief Dreamine.MVVM.Attributes - Dreamine MVVM 코드 생성을 위한 Attribute 정의 모음
\details 이 문서는 패키지 목적, 설치 방법, 아키텍처 역할, 사용 예제, Attribute 참조를 설명합니다.
\author Dreamine
\date 2026-04-20
\version 1.0.6
-->

# Dreamine.MVVM.Attributes

**Dreamine.MVVM.Attributes**는 Dreamine MVVM 생태계에서 사용하는 경량 Attribute 라이브러리입니다.

이 패키지는 자체적으로 MVVM 동작을 구현하지 않습니다.  
대신 소스 생성기와 런타임 모듈 등 Dreamine 도구가 해석하는 선언용 마커를 제공합니다.

목표는 반복적인 ViewModel 코드를 줄이면서도 코드베이스를 명시적이고, 읽기 쉽고, 유지보수 가능하게 유지하는 것입니다.

[➡️ English Documentation](README.md)

---

## 이 라이브러리가 해결하는 문제

MVVM 프로젝트에서는 다음과 같은 반복 패턴이 자주 발생합니다.

- private field → public property 생성
- method → command property 생성
- ViewModel ↔ Model 프록시 매핑
- 엔트리 타입 또는 구조적 역할 표시
- event/service 대상을 호출하는 커맨드 메서드 선언

이 패키지는 이러한 패턴을 Attribute만으로 표준화하며, 상위 Dreamine 도구가 일관된 방식으로 필요한 코드를 생성할 수 있도록 합니다.

---

## 주요 기능

- 의존성이 적은 Attribute 전용 패키지
- Dreamine MVVM 소스 생성 워크플로우를 위해 설계됨
- 프로퍼티 생성용 마커 지원
- 커맨드 생성용 마커 지원
- 엔트리/모델/이벤트 구조 마커 지원
- ViewModel → Model 프록시 프로퍼티 매핑 지원
- 폭넓은 호환성을 위한 `netstandard2.0` 대상

---

## 요구 사항

- **대상 프레임워크**: `netstandard2.0`
- 일반적으로 함께 사용되는 패키지:
  - Dreamine MVVM Generator 패키지
  - Dreamine MVVM Runtime/Core 패키지
  - WPF 또는 기타 .NET 데스크톱 MVVM 프로젝트

---

## 설치

### 방법 A) NuGet

```bash
dotnet add package Dreamine.MVVM.Attributes
```

### 방법 B) PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Attributes" Version="1.0.4" />
</ItemGroup>
```

---

## 프로젝트 구조

```text
Dreamine.MVVM.Attributes
├── DreamineCommandAttribute.cs
├── DreamineEntryAttribute.cs
├── DreamineEventAttribute.cs
├── DreamineModelAttribute.cs
├── DreamineModelPropertyAttribute.cs
└── DreaminePropertyAttribute.cs
```

---

## 아키텍처 역할

이 패키지는 Dreamine MVVM 스택의 선언 계층에 속합니다.

```text
ViewModel Source Code
        │
        ├─ Dreamine.MVVM.Attributes
        │     (markers / metadata)
        │
        ├─ Dreamine Generator
        │     (code generation)
        │
        └─ Dreamine Runtime/Core
              (execution / MVVM infrastructure)
```

Attribute는 의도를 선언하고, 실제 동작은 다른 Dreamine 패키지가 구현합니다.

이 분리는 책임을 명확히 유지하는 데 도움이 됩니다.

- Attribute는 메타데이터만 설명함
- Generator는 해당 메타데이터를 기반으로 코드를 생성함
- Runtime 패키지는 생성된 동작을 실행함

---

## 빠른 시작

### 1) 프로퍼티 생성 마커

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineProperty]
    private string _title;
}
```

의도:

- 필드 기반 선언
- public 프로퍼티 자동 생성
- property change notification은 generator/runtime가 처리

---

### 2) 커맨드 생성 마커

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineCommand]
    private void Save()
    {
    }
}
```

의도:

- 메서드를 커맨드 소스로 표시
- 커맨드 프로퍼티 자동 생성
- 기본 이름은 `{MethodName}Command`

---

### 3) 엔트리 마커

```csharp
using Dreamine.MVVM.Attributes;

[DreamineEntry]
public partial class App
{
}
```

의도:

- 애플리케이션 엔트리 또는 부트스트랩 타입 표시
- discovery/bootstrap 시나리오에서 유용
- Dreamine 도구에 명시적인 진입 역할 전달

---

### 4) Model 프록시 매핑 마커

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineModelProperty]
    private string _readme;
}
```

의도:

- ViewModel 필드를 Model 프로퍼티 프록시에 연결
- generator가 `Model.Readme` 또는 지정된 Model 프로퍼티로 매핑

---

### 5) 전달형 커맨드 마커

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineCommand("Event.ReadmeClick", BindTo = "Readme")]
    partial void LoadReadme();
}
```

의도:

- 대상 메서드 경로를 호출하는 커맨드 생성
- `Event.*`, `Service.*` 같은 대상 경로 지원
- 필요 시 `BindTo`를 통해 반환값을 프로퍼티에 대입

---

## Attribute 참조

### `DreaminePropertyAttribute`

필드를 프로퍼티 생성 대상으로 표시합니다.

```csharp
[DreamineProperty]
private string _name;
```

선택 파라미터:

- `propertyName`: 생성될 프로퍼티 이름을 명시적으로 지정

---

### `DreamineCommandAttribute`

메서드를 커맨드 생성 대상으로 표시합니다.

생성자 인자 없이 사용하면 생성된 커맨드가 주석이 붙은 메서드를 직접 실행합니다.

```csharp
[DreamineCommand]
private void Save()
{
}
```

`TargetMethod`를 지정하면 생성된 커맨드가 다른 대상 경로로 실행을 전달합니다.

```csharp
[DreamineCommand("Service.Load", BindTo = "Result")]
partial void Load();
```

멤버:

- `TargetMethod`: 대상 메서드 경로
- `BindTo`: 반환값을 받을 선택적 프로퍼티
- `CommandName`: 명시적으로 지정할 커맨드 프로퍼티 이름

`DreamineCommandAttribute`는 기존 relay-command 마커 역할까지 통합하므로 커맨드 생성 마커는 하나만 사용하면 됩니다.

---

### `DreamineEntryAttribute`

클래스를 엔트리 또는 부트스트랩 타입으로 표시합니다.

```csharp
[DreamineEntry]
public partial class App
{
}
```

사용 예:

- startup discovery
- generator scanning rules
- 명시적인 아키텍처 의도 표현

---

### `DreamineModelAttribute`

클래스 또는 필드를 모델 관련 구조로 표시합니다.

```csharp
[DreamineModel]
private MainModel _model;
```

일반적인 해석:

- 클래스에 적용: 해당 타입이 모델 관련 메타데이터임을 표시
- 필드에 적용: 모델 관련 멤버임을 표시하여 생성 또는 해석 대상이 되게 함

선택 파라미터:

- `propertyName`: 생성될 프로퍼티 이름을 명시적으로 지정

---

### `DreamineEventAttribute`

클래스 또는 필드를 이벤트 관련 구조로 표시합니다.

```csharp
[DreamineEvent]
private MainEvent _event;
```

일반적인 해석:

- 클래스에 적용: 해당 타입이 이벤트 관련 메타데이터임을 표시
- 필드에 적용: 이벤트 관련 멤버임을 표시하여 생성 또는 해석 대상이 되게 함

선택 파라미터:

- `propertyName`: 생성될 프로퍼티 이름을 명시적으로 지정

---

### `DreamineModelPropertyAttribute`

ViewModel 필드를 Model 프로퍼티 프록시에 매핑합니다.

```csharp
[DreamineModelProperty("Readme")]
private string _readme;
```

선택 파라미터:

- `modelPropertyName`: 명시적으로 지정할 Model 프로퍼티 이름

---

## 설계 노트

이 패키지는 의도적으로 Attribute 계층을 작고, 의존성 없이 유지합니다.

즉 다음을 포함하지 않습니다.

- MVVM 런타임 로직
- 커맨드 구현
- 프로퍼티 변경 알림 구현
- 선언 외의 실행 로직

이 분리는 전체 아키텍처에서 책임을 분리하는 방향과 잘 맞습니다.

- Attribute 패키지는 선언만 담당
- Generator는 독립적으로 진화 가능
- Runtime 동작은 선언 계층 밖에 유지

---

## 비교

| 패키지 | 역할 | 런타임 로직 | 코드 생성 마커 |
|---|---|---:|---:|
| CommunityToolkit.Mvvm | MVVM 툴킷 | Yes | Yes |
| Prism | MVVM 프레임워크 | Yes | No |
| Dreamine.MVVM.Attributes | Attribute 선언 | No | Yes |

Dreamine.MVVM.Attributes는 전체 런타임 프레임워크가 아니라 선언 계층에 집중합니다.

---

## 권장 조합 패키지

이 패키지는 아래와 함께 사용할 때 가장 유용합니다.

```text
Dreamine.MVVM.Core
Dreamine.MVVM.Generators
Dreamine runtime / UI packages
```

이 패키지 단독으로는 상위 Dreamine 도구가 사용하는 메타데이터를 주로 정의합니다.

---

## 라이선스

MIT License
