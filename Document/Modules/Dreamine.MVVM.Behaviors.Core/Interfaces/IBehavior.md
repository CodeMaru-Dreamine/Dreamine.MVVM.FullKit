---
title: IBehavior
module: Dreamine.MVVM.Behaviors.Core
generated: true
date: 2025-05-11
---

# 🧾 IBehavior.cs

## 📌 개요
`IBehavior`는 Dreamine의 모든 Behavior 클래스가 구현해야 하는 핵심 인터페이스입니다.  
WPF의 `DependencyObject`와 연결될 수 있는 동작 확장을 정의하며,  
`Interaction.Behaviors.Add(...)` 같은 방식으로 UI 요소에 동적으로 붙여질 수 있도록 설계되었습니다.

---

## 📂 파일 경로
Dreamine.MVVM.Behaviors.Core/Interfaces/IBehavior.cs

---

## 🧠 주요 기능
- 지정된 `DependencyObject`에 Behavior를 **Attach / Detach**하는 기능 제공
- `IAttachedObject` 인터페이스에서 파생되어 연결 객체 참조 일관성 유지
- XAML 또는 코드에서 동적으로 UI 요소와 연결 가능
- 모든 Dreamine Behavior는 이 인터페이스 기반으로 동작

---

## 💡 사용 예시

```csharp
public class WindowDragBehavior : Behavior<Window>
{
    protected override void OnAttached()
    {
        AssociatedObject.MouseLeftButtonDown += (s, e) => AssociatedObject.DragMove();
    }

    protected override void OnDetaching()
    {
        // 정리 코드
    }
}
```
> `WindowDragBehavior`는 내부적으로 `Behavior<T>`를 상속하고,  이 `Behavior<T>`는 `IBehavior` 인터페이스를 구현합니다.

## 🛠️ 내부 구조

|멤버 이름|접근 수준|반환 타입|설명|
|---|---|---|---|
|`Attach()`|method|`void`|지정된 WPF UI 요소에 Behavior를 연결합니다.|
|`Detach()`|method|`void`|현재 연결된 요소에서 Behavior를 해제합니다.|

---

## 🔒 제약 사항

- 연결 대상은 반드시 `DependencyObject` 타입이어야 합니다.
    
- Behavior는 `Freezable` 또는 `IAttachedObject`와 함께 구현되어야 정상적으로 작동합니다.
    
- 이 인터페이스 자체는 XAML에서 직접 사용되지 않으며,  
    보통 `Behavior<T>` 형태로 상속되어 활용됩니다.
    

---

## 🧩 관련 모듈

|모듈|설명|
|---|---|
|`Dreamine.MVVM.Behaviors.Core.Interfaces`|인터페이스 정의 위치|
|`IAttachedObject`|이 인터페이스가 암시적으로 구현하는 상위 구조|
|`Behavior<T>`|이 인터페이스의 실질 구현체|
|`Interaction.Behaviors.Add()`|Behavior가 적용되는 WPF 메커니즘|

---

## 🗂️ 버전 관리

|버전|변경 내용|날짜|
|---|---|---|
|v1.0|IBehavior.cs 문서 자동 생성|2025-05-11|

---

## 📁 소속 모듈

Dreamine.MVVM.Behaviors.Core

---

## 🖋️ 기록 정보

|항목|내용|
|---|---|
|✍️ 작성자|아키로그 드림|
|🤖 협력자|ChatGPT (프레임워크 유혹자)|
|📅 생성일|2025-05-11 (자동 생성됨)|
|🛠️ 생성도구|Dreamine 문서화 자동화 도구|

---

## ⛏️ 자유 작성 영역

-  추후 `ITrigger` 인터페이스와 구조 통합 여부 검토 필요
    
-  `Interaction` 확장 메서드에 자동 등록 가능한지 여부 조사 예정

```yaml
TODO:
  - 이 인터페이스를 상속하는 모든 구현체 문서 링크 모음
  - WinUI 확장 가능성 기술적 검토
```
