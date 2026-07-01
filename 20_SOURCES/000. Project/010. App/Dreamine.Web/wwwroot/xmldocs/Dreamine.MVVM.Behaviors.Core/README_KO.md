# Dreamine.MVVM.Behaviors.Core

Dreamine 프레임워크에서 **WPF MVVM Behavior 구현을 위한 핵심 인프라 라이브러리**입니다.

이 패키지는 WPF UI 요소에 **재사용 가능한 Behavior를 구현하기 위한 기본 구조**를 제공합니다.

상위 패키지들의 기반 레이어로 사용됩니다.

예:

- Dreamine.MVVM.Behaviors
- Dreamine.MVVM.Triggers
- Dreamine.MVVM.Interactions

[➡️ English Version](README.md)
---

# 목적

WPF Behavior는 UI 로직을 View에 직접 작성하지 않고  
**MVVM 패턴을 유지하면서 UI 확장 기능을 제공**할 수 있게 합니다.

하지만 기존 Behavior 구현 방식은 다음 문제들이 있습니다.

- 강한 프레임워크 의존성
- 암묵적인 초기화
- 타입 안정성 부족

`Dreamine.MVVM.Behaviors.Core`는 다음 목표로 설계되었습니다.

- 최소 의존성
- 명시적 Attach/Detach 구조
- Strongly Typed Behavior

---

# 핵심 개념

### Behavior<T>

제네릭 기반 Behavior 기본 클래스입니다.

특징:

- `AssociatedObject` 타입 안정성 제공
- `Freezable` 기반 XAML 지원
- Attach / Detach 라이프사이클 관리
- MVVM 친화적 구조

사용 대상 예:

- Window
- Button
- TextBox
- Grid

---

### IAttachedObject

`DependencyObject`에 연결되는 객체의 공통 인터페이스입니다.

역할:

- 연결된 UI 요소 관리
- 연결 및 해제 라이프사이클 제공

메서드:

```
Attach(DependencyObject)
Detach()
```

---

### IBehavior

Dreamine Behavior의 핵심 인터페이스입니다.

모든 Behavior는 이 인터페이스 기반으로 구현됩니다.

---

# 프로젝트 구조

```
Dreamine.MVVM.Behaviors.Core
│
├─ Base
│   └─ BehaviorGeneric.cs
│
├─ Interfaces
│   ├─ IAttachedObject.cs
│   └─ IBehavior.cs
│
└─ Dreamine.MVVM.Behaviors.Core.csproj
```

---

# 아키텍처

```
WPF UI Element
     │
     └─ Behavior<T>
            │
            ├─ Attach()
            └─ Detach()
```

이 구조를 통해 UI 확장 기능을 구현하면서도  
**ViewModel 로직과 완전히 분리된 구조를 유지**할 수 있습니다.

---

# 예제

Behavior 기본 구조:

```csharp
public class FocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        AssociatedObject.Focus();
    }

    protected override void OnDetaching()
    {
    }
}
```

XAML 사용 예:

```xml
<TextBox>
    <i:Interaction.Behaviors>
        <local:FocusBehavior/>
    </i:Interaction.Behaviors>
</TextBox>
```

---

# 요구사항

```
.NET 8.0
WPF
```

---

# 관련 패키지

- Dreamine.MVVM.Behaviors
- Dreamine.MVVM.Attributes
- Dreamine.MVVM.ViewModels

---

# License

MIT License
