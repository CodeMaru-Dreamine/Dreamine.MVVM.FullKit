# 🌟 Dreamine.MVVM.Behaviors.Wpf

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Behaviors.Wpf`는 Dreamine MVVM 프레임워크에서  
WPF UI 전용 동작(Behavior)을 실제로 구현하는 구현 모듈입니다.

`Dreamine.MVVM.Behaviors.Core`의 기반 클래스를 활용하여,  
WPF 환경에 최적화된 포커스, 명령 바인딩, 로딩 이벤트 등  
다양한 UI 상호작용을 MVVM 구조로 매끄럽게 연결합니다.

---

## ✨ 주요 구성 요소

| Behavior | 설명 |
|----------|------|
| `FocusOnLoadedBehavior` | 로드 시 자동 포커스 이동 처리 |
| `EventToCommandBehavior` | 일반 이벤트를 ViewModel의 ICommand로 연결 |
| `KeyboardEnterCommandBehavior` | Enter 키 입력 시 ICommand 실행 |
| `ScrollToEndBehavior` | 스크롤 컨트롤의 하단 자동 이동 지원 |

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Behaviors.Wpf
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Behaviors.Wpf" Version="1.0.0" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Behaviors.Wpf](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors.Wpf)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "몰라도 쓸 수 있게,  
> 궁금하면 원리까지 이해되게."

WPF 환경에서의 복잡한 UI 이벤트 처리를 MVVM 구조 내에서 선언적으로 처리할 수 있도록  
최소 구성 요소로 최적의 상호작용 경험을 제공합니다.

---

## 🖋️ 작성자 정보

- 작성자: Dreamine Core Team  
- 소유자: minsujang  
- 날짜: 2025년 5월 25일  
- 라이선스: MIT

---

📅 문서 작성일: 2025년 5월 25일  
⏱️ 총 소요시간: 약 10분  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림

---

## 🇺🇸 English Summary

`Dreamine.MVVM.Behaviors.Wpf` implements WPF-specific behaviors  
based on the Dreamine.MVVM.Behaviors.Core foundation.

It provides seamless UI-to-ViewModel interaction via declarative behaviors.

### ✨ Key Components

| Behavior | Description |
|----------|-------------|
| `FocusOnLoadedBehavior` | Sets focus automatically when control is loaded |
| `EventToCommandBehavior` | Binds routed events to ViewModel ICommand |
| `KeyboardEnterCommandBehavior` | Executes command when Enter key is pressed |
| `ScrollToEndBehavior` | Auto-scrolls to bottom on content change |

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Behaviors.Wpf
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
