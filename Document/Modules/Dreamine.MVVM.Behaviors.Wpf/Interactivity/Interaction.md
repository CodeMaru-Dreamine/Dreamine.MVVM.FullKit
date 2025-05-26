---
title: Interaction
module: Dreamine.MVVM.Behaviors.Wpf
generated: true
date: 2025-05-11
---

# # 🧾 Interaction.cs

## 📌 개요
`Interaction` 클래스는 Dreamine에서 WPF의 XAML 구조와 연결되는  
**Behavior 컬렉션 시스템을 구현하는 Static 헬퍼 클래스**입니다.

XAML에서 다음과 같이 `<i:Interaction.Behaviors>` 구문을 사용할 수 있게 만들며,  
실행 중 객체에 `BehaviorCollection`을 attach하거나 detach하는 작업을 내부적으로 처리합니다.

---

## 📂 파일 경로
Dreamine.MVVM.Behaviors.Wpf/Interactivity/Interaction.cs

---

## 🧠 주요 기능

- `BehaviorsProperty`라는 이름의 Attached Property 등록
- XAML 또는 코드에서 `BehaviorCollection`을 대상 컨트롤에 연결 가능
- 내부적으로 `Attach`, `Detach` 호출을 통해 동적 연결 처리
- 기본적으로 `WindowDragBehavior`를 자동 추가하여 UX 편의성 제공

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
> `Interaction.Behaviors`는 Dreamine에서 커스텀 구현된 attached property로,  내부적으로 `GetBehaviors()` 호출 → `BehaviorCollection`을 가져오거나 새로 생성합니다.

## 🛠️ 내부 구조

|멤버 이름|접근 수준|타입|설명|
|---|---|---|---|
|`BehaviorsProperty`|public static|DependencyProperty|XAML에서 연결될 수 있는 BehaviorCollection attached property|
|`GetBehaviors()`|public static|BehaviorCollection|대상 객체에 연결된 BehaviorCollection을 가져오거나 새로 생성|
|`SetBehaviors()`|public static|void|BehaviorCollection을 대상 객체에 설정|
|`OnBehaviorsChanged()`|private static|void|프로퍼티가 변경되었을 때 자동으로 `Attach`/`Detach` 처리|

---

## 🔒 제약 사항

- 반드시 WPF 환경에서만 사용 가능 (`DependencyObject` 기반)
    
- XAML 상에서 `Interaction.Behaviors` 사용 시 네임스페이스가 정확히 선언되어야 함
    
- 동일 객체에 여러 번 `Behaviors`를 설정할 경우 예외 가능성 있음 (중복 연결 방지 필요)
    

---

## 🧩 관련 모듈

|모듈|설명|
|---|---|
|`Dreamine.MVVM.Behaviors.Wpf.Interactivity`|이 클래스의 위치|
|`BehaviorCollection`|실제 attach 대상 컬렉션 타입|
|`WindowDragBehavior`, `EnterKeyCommandBehavior`|컬렉션에 포함될 수 있는 대표 Behavior들|

---

## 🗂️ 버전 관리

|버전|변경 내용|날짜|
|---|---|---|
|v1.0|Interaction.cs 문서 자동 생성|2025-05-11|

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

-  [ ] 중복 Attach 시 예외처리 전략 도입 여부 검토    
-  [ ] UWP 또는 MAUI에서도 유사 구조 지원 가능한지 조사 예정

```yaml
TODO:   
- BehaviorCollection 커스텀 확장 방법 정리   - 다양한 Control 적용 사례 정리
```
