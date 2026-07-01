
# Dreamine.MVVM.Behaviors.Wpf

Dreamine MVVM Behavior 시스템의 WPF 구현 레이어입니다.

이 패키지는 다음 라이브러리를 기반으로 동작합니다.

Dreamine.MVVM.Behaviors.Core

WPF UI 요소에 Behavior를 부착하여 MVVM 구조를 유지하면서
사용자 인터랙션 로직을 확장할 수 있도록 합니다.

[➡️ English Version](README.md)

---

# 목적

MVVM 구조에서 UI 이벤트 처리 로직을 코드비하인드에 작성하는 것은
구조적인 결합도를 높입니다.

Behavior 패턴을 사용하면 XAML에서 선언적으로 UI 동작을 확장할 수 있습니다.

Dreamine.MVVM.Behaviors.Wpf 는 다음 기능을 제공합니다.

- XAML 기반 Behavior 연결
- 여러 Behavior 조합 지원
- MVVM 친화적인 UI 확장 구조

---

# 아키텍처

Dreamine Behavior 구조

Dreamine.MVVM.Behaviors.Core
    ↓
Dreamine.MVVM.Behaviors.Wpf
    ↓
Application Behaviors

Core는 Behavior 기반 구조를 제공하고

Wpf 패키지는 실제 WPF 런타임 연결을 담당합니다.

---

# 구성 요소

## BehaviorCollection

하나의 UI 요소에 여러 Behavior를 연결할 수 있도록 하는 컬렉션 컨테이너입니다.

예시

<Button>
    <i:Interaction.Behaviors>
        <behaviors:WindowDragBehavior/>
    </i:Interaction.Behaviors>
</Button>

역할

- Behavior 저장
- Attach / Detach 라이프사이클 관리
- AssociatedObject 전달

---

## Interaction

XAML에서 Behavior를 연결하기 위한 static 헬퍼 클래스입니다.

다음 Attached Property를 제공합니다.

Interaction.Behaviors

이 속성을 통해 BehaviorCollection을 DependencyObject에 연결합니다.
`Interaction.GetBehaviors(...)` 호출은 빈 컬렉션만 생성하며, `WindowDragBehavior` 같은 동작은 XAML에서 명시적으로 선언해야 합니다.

---

## WindowDragBehavior

이 패키지에 포함된 예제 Behavior입니다.

마우스로 WPF Window를 드래그하여 이동할 수 있도록 합니다.

대표 사용 예

Borderless Window

예시

<Grid>
    <i:Interaction.Behaviors>
        <behaviors:WindowDragBehavior/>
    </i:Interaction.Behaviors>
</Grid>

---

# 설치

NuGet 패키지 설치

dotnet add package Dreamine.MVVM.Behaviors.Wpf

또는 프로젝트 참조 방식 사용

---

# 패키지 관계

Dreamine.MVVM.Behaviors.Core

Behavior 기본 인프라 제공

Dreamine.MVVM.Behaviors

공통 Behavior 구현

Dreamine.MVVM.Interactions

Event Trigger / Interaction 확장

---

# License

MIT License
