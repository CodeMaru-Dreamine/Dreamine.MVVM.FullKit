---
title: BehaviorCollection
module: Dreamine.MVVM.Behaviors.Wpf
generated: true
date: 2025-05-11
---

# 🧾 BehaviorCollection.cs

## 📌 개요
`BehaviorCollection`은 Dreamine WPF 프레임워크에서  
여러 개의 `Behavior`를 XAML 상에서 `<Interaction.Behaviors>`로 연결할 수 있도록 지원하는 컬렉션 클래스입니다.

이 클래스는 WPF의 `FreezableCollection`을 상속하며,  
내부적으로 각각의 `Behavior<T>` 또는 `IBehavior`에 대해 `Attach()` / `Detach()` 작업을 순차적으로 실행합니다.

---

## 📂 파일 경로
Dreamine.MVVM.Behaviors.Wpf/Interactivity/BehaviorCollection.cs

---

## 🧠 주요 기능
- `Interaction.Behaviors`에 설정된 모든 Behavior 객체를 보관
- 대상 컨트롤과 연결 시 각 Behavior에 `Attach()` 호출
- 연결 해제 시 모든 Behavior에 `Detach()` 호출
- `FreezableCollection` 기반으로 XAML 리소스, 스타일 등과 호환 가능
- `IAttachedObject` 구현으로 `Interaction`과의 통합 보장

---

## 💡 사용 예시

```xaml
<Border>
    <i:Interaction.Behaviors>
        <mvvm:WindowDragBehavior />
        <mvvm:EnterKeyCommandBehavior />
    </i:Interaction.Behaviors>
</Border>
```
>위 예시에서 `<i:Interaction.Behaviors>`가 `BehaviorCollection`으로 처리되며,  내부의 각 `Behavior` 객체가 자동으로 `Attach()` 처리됩니다.

## 🛠️ 내부 구조

|멤버 이름|접근 수준|타입|설명|
|---|---|---|---|
|`AssociatedObject`|public|DependencyObject|현재 BehaviorCollection이 연결된 대상 객체|
|`Attach()`|public|void|대상 객체에 연결하고, 내부의 모든 Behavior도 Attach 처리|
|`Detach()`|public|void|연결 해제 및 내부 Behavior 모두 Detach 처리|

---

## 🔒 제약 사항

- 컬렉션에 포함되는 항목은 반드시 `DependencyObject`를 상속해야 하며, 보통 `Behavior<T>` 또는 `IBehavior` 구현체여야 합니다.
    
- `Attach()` 호출 시 이미 연결된 객체가 있다면 예외가 발생합니다.
    
- XAML 상에서는 직접 사용하지 않고 `Interaction.Behaviors`를 통해 간접 사용됩니다.
    

---

## 🧩 관련 모듈

|모듈|설명|
|---|---|
|`Dreamine.MVVM.Behaviors.Wpf.Interactivity`|이 클래스의 실제 위치|
|`Interaction`|이 컬렉션을 연결하는 Static Helper|
|`Behavior<T>`, `IBehavior`|내부에 포함될 수 있는 타입 기준|
|`IAttachedObject`|연결 관리 기능을 보장하는 인터페이스|

---

## 🗂️ 버전 관리

|버전|변경 내용|날짜|
|---|---|---|
|v1.0|BehaviorCollection.cs 문서 자동 생성|2025-05-11|

---

## 📁 소속 모듈

Dreamine.MVVM.Behaviors.Wpf

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
- [ ] `FreezableCollection<T>` 확장에 대한 부작용 여부 검토 필요
- [ ] 향후 `TriggerCollection`과 구조 통일 고려
```yaml
TODO:
  - 이 클래스가 관리하는 Behavior 리스트를 실시간으로 수정 가능한지 검토
  - Behavior 실행 순서에 따른 영향도 분석
```
