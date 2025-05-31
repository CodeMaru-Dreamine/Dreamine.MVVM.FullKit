# 🌟 Dreamine.MVVM.Interfaces

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Interfaces`는 Dreamine MVVM 프레임워크의  
구조적 일관성과 유연한 DI(의존성 주입)를 위한 핵심 인터페이스들을 정의한 모듈입니다.

ViewModel, Command, Service 등 다양한 컴포넌트들이  
명확한 책임과 의존성을 갖도록 하기 위한 설계 기반을 제공합니다.

---

## ✨ 주요 인터페이스

| 인터페이스 | 설명 |
|------------|------|
| `IViewModel` | 모든 ViewModel의 공통 베이스 식별자 |
| `ICommandService` | 커맨드 실행 및 상태 관리용 인터페이스 |
| `INavigationService` | 화면 전환 및 뷰모델 간 내비게이션 처리 |
| `ILifecycleAware` | ViewModel의 생명주기 대응 (`OnAppear`, `OnDisappear` 등) |
| `IInjectable` | DI 기반 자동 주입 마킹 인터페이스 |

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Interfaces
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Interfaces" Version="1.0.0" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Interfaces](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Interfaces)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "프레임워크의 강력함은 인터페이스에서 나온다."

Dreamine은 SOLID 원칙을 기반으로 구성되며,  
모든 구현체는 인터페이스와 느슨하게 연결되어야 유지보수가 용이해집니다.

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

`Dreamine.MVVM.Interfaces` defines shared interfaces across the Dreamine MVVM framework  
to enable DI, clear contracts, and architectural consistency.

### ✨ Key Interfaces

| Interface | Description |
|-----------|-------------|
| `IViewModel` | Base marker for all ViewModels |
| `ICommandService` | Manages command execution and routing |
| `INavigationService` | Handles screen transitions between ViewModels |
| `ILifecycleAware` | Provides ViewModel lifecycle events |
| `IInjectable` | Marker for auto-injection in the DI system |

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Interfaces
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
