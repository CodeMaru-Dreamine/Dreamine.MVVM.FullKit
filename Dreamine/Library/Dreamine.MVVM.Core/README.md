# 🌟 Dreamine.MVVM.Core

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Core`는 Dreamine 프레임워크의 MVVM 아키텍처에서  
ViewModel과 Model을 구현하는 데 필요한 핵심 베이스 클래스를 제공합니다.

MVVM 구조의 핵심 구성요소인 `ViewModelBase`, `ObservableObject`,  
또한 ICommand 바인딩을 위한 `VsRelayCommand` 등을 포함하며,  
자동화와 선언적 개발을 중심에 둔 구조를 갖고 있습니다.

---

## ✨ 주요 클래스 및 기능

| 클래스 / 기능 | 설명 |
|---------------|------|
| `ViewModelBase` | INotifyPropertyChanged 기본 구현 및 확장 지원 |
| `ObservableObject` | 속성 변경 알림 + 단순 POCO용 |
| `VsRelayCommand` | 파라미터 지원 커맨드 바인딩 |
| `ICommandService` | 커맨드 그룹화 및 라우팅 지원 |
| `ViewModelAttribute` | 진입 ViewModel 마킹 지원 어트리뷰트 |

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Core
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Core" Version="1.0.0" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Core](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Core)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "몰라도 쓸 수 있게,  
> 궁금하면 원리까지 이해되게."

Dreamine은 MVVM을 단순히 구현 패턴이 아닌  
자동화 중심의 선언적 프레임워크로 구현합니다.

---

## 🖋️ 작성자 정보

- 작성자: Dreamine Core Team  
- 소유자: minsujang  
- 날짜: 2025년 5월 25일  
- 라이선스: MIT

---

📅 문서 작성일: 2025년 5월 25일  
⏱️ 총 소요시간: 약 15분  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림

---

## 🇺🇸 English Summary

`Dreamine.MVVM.Core` provides the foundational components for MVVM  
architecture within the Dreamine framework.

It contains base classes like `ViewModelBase`, `ObservableObject`, and  
command-binding helpers that support declarative MVVM development.

### ✨ Key Features

| Component | Description |
|-----------|-------------|
| `ViewModelBase` | Implements `INotifyPropertyChanged` with helpers |
| `ObservableObject` | Minimal POCO + notify support |
| `VsRelayCommand` | Parameter-ready `ICommand` implementation |
| `ICommandService` | Central command handling |
| `ViewModelAttribute` | Entry-point ViewModel marking attribute |

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Core
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
