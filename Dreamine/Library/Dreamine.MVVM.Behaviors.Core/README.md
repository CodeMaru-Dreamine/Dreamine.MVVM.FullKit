# 🌟 Dreamine.MVVM.Behaviors.Core

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Behaviors.Core`는 Dreamine 프레임워크에서  
모든 Behavior 모듈들의 기반이 되는 공통 추상 클래스 및 바인딩 유틸리티를 제공합니다.

WPF의 `Behavior<T>` 기반 클래스를 확장하며,  
MVVM에 최적화된 **BindableBehavior**, **EventToCommandBase** 등을 포함합니다.

---

## ✨ 주요 기능

| 클래스 / 기능 | 설명 |
|---------------|------|
| `BindableBehavior<T>` | DependencyProperty 바인딩 지원을 강화한 Behavior 기반 클래스 |
| `EventToCommandBase` | 커스텀 이벤트 → ViewModel의 ICommand 실행 흐름 제공 |
| `SafeBehavior<T>` | Loaded 상태 확인 및 중복 이벤트 방지 내장 |
| `BehaviorUtility` | Command 파라미터 처리, Focus 유틸리티 등 포함 예정 |

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Behaviors.Core
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Behaviors.Core" Version="1.0.0" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Behaviors.Core](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors.Core)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "몰라도 쓸 수 있게,  
> 궁금하면 원리까지 이해되게."

모든 Dreamine Behavior는 이 Core 모듈을 기반으로 동작하며,  
구현 클래스는 최소화하고 기능은 선언적으로 구성할 수 있도록 설계되어 있습니다.

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

`Dreamine.MVVM.Behaviors.Core` provides base classes and utilities  
for building WPF behaviors that are MVVM-friendly and fully bindable.

### ✨ Features

| Component | Description |
|-----------|-------------|
| `BindableBehavior<T>` | Extended `Behavior<T>` with bindable dependency support |
| `EventToCommandBase` | Common base class for event-to-command patterns |
| `SafeBehavior<T>` | Loaded-check and duplicate trigger protection |
| `BehaviorUtility` | Command helpers and focus management utilities (WIP) |

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Behaviors.Core
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
