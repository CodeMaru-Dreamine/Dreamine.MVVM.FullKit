---
title: WindowDragBehavior
module: Dreamine.MVVM.Behaviors.Wpf
generated: true
date: 2025-05-11
---

# 🧾 WindowDragBehavior.cs

## 📌 개요
`WindowDragBehavior`는 WPF의 `Window`를 마우스로 드래그하여 이동할 수 있도록 만드는  
Dreamine 전용 Behavior입니다.

MVVM 구조에서 코드 비하인드 없이 창 이동 기능을 제공하며,  
XAML에서 `<Interaction.Behaviors>` 형태로 간단히 연결할 수 있습니다.

---

## 📂 파일 경로
Dreamine.MVVM.Behaviors.Wpf/Interactivity/WindowDragBehavior.cs

---

## 🧠 주요 기능
- 마우스 왼쪽 버튼 클릭 시 `Window.DragMove()` 호출
- `Behavior<FrameworkElement>` 기반으로 어떤 UI 요소에서도 동작 가능
- XAML에서 `Interaction.Behaviors`로 선언형 연결 가능
- MVVM 구조에서 ViewModel 코드와 분리된 UI 동작 확장

---

## 💡 사용 예시

### 🔹 XAML

```xaml
<Window x:Class="SampleApp.MainWindow"
        xmlns:i="clr-namespace:System.Windows.Interactivity"
        xmlns:mvvm="clr-namespace:Dreamine.MVVM.Behaviors.Wpf.Interactivity;assembly=Dreamine.MVVM.Behaviors.Wpf">
    
    <Grid>
        <Border>
            <i:Interaction.Behaviors>
                <mvvm:WindowDragBehavior />
            </i:Interaction.Behaviors>
        </Border>
    </Grid>
</Window>
```

## 🛠️ 내부 구조
| 멤버 이름 | 접근 수준 | 타입 | 설명 |
| -------- | -------- | ---- | ---- |
| `OnAttached()` | `protected` | `void` | (TODO) |
| `OnDetaching()` | `protected` | `void` | (TODO) |
| `OnMouseLeftButtonDown()` | `private` | `void` | (TODO) |
> 위 예시에서는 `Border`를 마우스로 클릭하여 `Window` 전체를 드래그할 수 있습니다.  ViewModel과 무관하게 View 확장 동작만 담당하므로 MVVM 순도를 유지할 수 있습니다.


## 🛠️ 내부 구조

|멤버 이름|접근 수준|타입|설명|
|---|---|---|---|
|`OnAttached()`|protected override|void|Behavior가 Attach될 때 호출되며, 마우스 이벤트를 연결합니다.|
|`OnDetaching()`|protected override|void|Behavior가 해제될 때 호출되며, 마우스 이벤트를 제거합니다.|
|`OnMouseLeftButtonDown()`|private|void|마우스 클릭 시 `Window.DragMove()` 호출 처리|

---

## 🔒 제약 사항

- 이 Behavior는 WPF 전용이며, 반드시 `Window` 상에서 사용해야 합니다.
    
- 연결 대상은 `FrameworkElement` 또는 그 하위 타입이어야 합니다.
    
- `AssociatedObject`가 실제로 `Window`를 포함하지 않을 경우 아무 동작도 하지 않습니다.
    

---

## 🧩 관련 모듈

|모듈|설명|
|---|---|
|`Dreamine.MVVM.Behaviors.Wpf.Interactivity`|이 Behavior의 실제 정의 위치|
|`Interaction`, `BehaviorCollection`|XAML에서 이 Behavior를 붙이기 위한 시스템 구성 요소|
|`Behavior<T>`|이 클래스가 상속하는 베이스 클래스|

---

## 🗂️ 버전 관리

|버전|변경 내용|날짜|
|---|---|---|
|v1.0|WindowDragBehavior.cs 문서 자동 생성|2025-05-11|

---

## 📁 소속 모듈

Dreamine.MVVM.Behaviors.Wpf

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
