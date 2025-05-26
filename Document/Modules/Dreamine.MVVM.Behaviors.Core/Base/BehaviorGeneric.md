---
title: BehaviorGeneric
module: Dreamine.MVVM.Behaviors.Core
generated: true
date: 2025-05-11
---

# 🧾 BehaviorGeneric.cs

## 📌 개요
`Behavior<T>`는 Dreamine의 제네릭 기반 WPF Behavior 클래스입니다.  
WPF의 `Freezable`을 상속하여 XAML과 리소스 시스템의 완전한 호환성을 제공하며,  
`IAttachedObject`를 통해 런타임 시 UI 요소에 타입 안전하게 연결됩니다.

## 📂 파일 경로
Dreamine.MVVM.Behaviors.Core/BehaviorGeneric.cs

## 🧠 주요 기능
- `T` 제네릭 타입을 통해 `AssociatedObject`의 타입 안정성 보장
- `Attach()`, `Detach()`를 통해 런타임 객체 연결 및 해제 지원
- `OnAttached()`, `OnDetaching()` 메서드를 오버라이드하여 동작 확장 가능
- WPF의 `Freezable` 기반으로 리소스 시스템에 자동 연동 가능

## 💡 사용 예시
```csharp
    protected override void OnAttached()
    {
        AssociatedObject.MouseLeftButtonDown += (s, e) =>
        {
            AssociatedObject.DragMove();
        };
    }

    protected override void OnDetaching()
    {
        // 정리 코드 생략
    }
```

> 이 클래스는 직접 XAML에서 사용되지 않으며,  
> `WindowDragBehavior`, `EnterKeyCommandBehavior` 등의 기반으로 상속되어 동작합니다.
> 실제 사용법은 각 파생 클래스 문서를 참고하세요.

## 🛠️ 내부 구조

| 멤버 이름             | 접근 수준       | 타입         | 설명 |
|----------------------|----------------|--------------|------|
| `Attach()`           | `public`       | `void`       | 지정된 WPF 객체에 Behavior를 연결하고 `OnAttached()`를 호출합니다. |
| `Detach()`           | `public`       | `void`       | 연결된 객체와의 연결을 해제하고 `OnDetaching()`를 호출합니다. |
| `OnAttached()`       | `protected`    | `void`       | 연결 시 확장 포인트로 동작을 정의할 수 있습니다. 오버라이딩 가능. |
| `OnDetaching()`      | `protected`    | `void`       | 해제 시 정리 작업 등을 수행하는 확장 포인트입니다. 오버라이딩 가능. |
| `CreateInstanceCore()`| `protected`   | `Freezable`  | WPF의 리소스 시스템을 위한 인스턴스 생성 팩토리입니다. |
| `AssociatedObject`   | `public`       | `T?`         | 현재 연결된 대상 객체입니다. 연결 시 자동으로 설정되며, `Detach()` 시 null로 초기화됩니다. |


## 🔒 제약 사항
- 제네릭 타입 `T`는 반드시 `DependencyObject`를 상속받아야 함
    
- XAML 상에서 사용하려면 기본 생성자가 반드시 필요함
    
- WPF 전용 구조이며, MAUI/WinUI 호환 불가
## 🧩 관련 모듈
| 모듈                                              | 설명                  |
| ----------------------------------------------- | ------------------- |
| `Dreamine.MVVM.Behaviors.Core.Base`             | 이 클래스의 실제 위치        |
| `IAttachedObject`, `IBehavior`                  | 이 클래스가 구현하는 인터페이스   |
| `WindowDragBehavior`, `EnterKeyCommandBehavior` | 이 클래스를 상속하는 대표 구현체들 |

## 🗂️ 버전 관리
| 버전 | 변경 내용 | 날짜 |
|------|-----------|------|
| v1.0 | BehaviorGeneric.cs 문서 자동 생성 | 2025-05-09 |

## 📁 소속 모듈
Dreamine.MVVM.Behaviors.Core

---

## 🖋️ 기록 정보

| 항목       | 내용                             |
|------------|----------------------------------|
| ✍️ 작성자  | 아키로그 드림                    |
| 🤖 협력자  | ChatGPT (프레임워크 유혹자)       |
| 📅 생성일  | 2025-05-11 (자동 생성됨) |
| 🛠️ 생성도구 | Dreamine 문서화 자동화 도구         |

---

## ⛏️ 자유 작성 영역

- [ ] 설명 추가 또는 TODO 항목 작성
- [ ] 특이점, 예외 상황, 사용자 주석 등 기술 메모 작성 가능
- [ ] 이 영역은 자동 생성 도구에 의해 변경되지 않습니다.
```yaml
TODO:
  - 여기에 설명 또는 작업 내용을 작성하세요
```
