---
title: IAttachedObject
module: Dreamine.MVVM.Behaviors.Core
generated: true
date: 2025-05-11
---
# 🧾 IAttachedObject.cs

## 📌 개요
`IAttachedObject`는 Dreamine 프레임워크의 모든 `Behavior`, `Trigger`와 같은  
UI 확장 구성 요소가 **WPF 요소(DependencyObject)** 와 연결될 수 있도록 보장하는 핵심 인터페이스입니다.

`FrameworkElement`와 같은 UI 객체와의 연결 상태를 추적하며,  
WPF `Interactivity` 패턴을 Dreamine 내부 구조에 맞게 추상화한 역할을 수행합니다.

---

## 📂 파일 경로
Dreamine.MVVM.Behaviors.Core/Interfaces/IAttachedObject.cs

---

## 🧠 주요 기능
- 연결된 WPF 객체 (`DependencyObject`)를 참조할 수 있게 함
- `Attach()`, `Detach()` 메서드를 통해 객체와의 연결을 제어
- `Behavior<T>`, `Trigger<T>` 등의 기반 클래스에서 구현됨
- Dreamine의 MVVM + XAML 구조에서 동적 연결이 필요한 기능들의 **기본 인터페이스**

---

## 💡 사용 예시

```csharp
public class WindowDragBehavior : Behavior<Window>
{
    protected override void OnAttached()
    {
        AssociatedObject.MouseLeftButtonDown += (s, e) =>
        {
            AssociatedObject.DragMove();
        };
    }

    protected override void OnDetaching()
    {
        // 연결 해제 처리
    }
}
```
> `WindowDragBehavior`는 내부적으로 `IAttachedObject` 인터페이스를 구현한 `Behavior<T>`를 상속합니다.

## 🛠️ 내부 구조

|멤버 이름|접근 수준|반환 타입|설명|
|---|---|---|---|
|`AssociatedObject`|`get`|`DependencyObject`|현재 연결된 WPF 객체를 반환합니다.|
|`Attach()`|`method`|`void`|지정된 DependencyObject에 연결을 수행합니다.|
|`Detach()`|`method`|`void`|현재 연결 상태를 해제합니다.|

---

## 🔒 제약 사항

- 이 인터페이스는 WPF 환경 전용입니다.
    
- 연결 대상은 반드시 `DependencyObject`여야 하며, 보통은 `FrameworkElement` 기반입니다.
    
- XAML 상에서는 직접 사용되지 않고, 상속 클래스(`Behavior<T>`, `Trigger<T>` 등)에서만 간접 사용됩니다.
    

---

## 🧩 관련 모듈

|모듈|설명|
|---|---|
|`Dreamine.MVVM.Behaviors.Core.Interfaces`|인터페이스 위치|
|`Behavior<T>`|이 인터페이스의 대표 구현체|
|`IBehavior`, `ITrigger`|파생 확장 인터페이스들|
|`Interaction.Behaviors.Add(...)`|Attach가 실제 호출되는 WPF 내부 메커니즘|

---

## 🗂️ 버전 관리

| 버전 | 변경 내용 | 날짜 |
|------|-----------|------|
| v1.0 | IAttachedObject.cs 문서 자동 생성 | 2025-05-09 |



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

- [x] 인터페이스 확장 구조 (예: `ITrigger`, `IBehavior`) 정리 예정
- [x] 관련 유닛 테스트 작성 여부 확인 필요
```yaml
TODO:
  - 이 인터페이스를 구현하는 확장 포인트 목록 정리
  - 실제 WPF와의 차이점 비교
```
