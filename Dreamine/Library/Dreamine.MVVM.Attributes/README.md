# 🌟 Dreamine.MVVM.Attributes

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Attributes`는 Dreamine MVVM 프레임워크에서 사용되는  
커스텀 Attribute들을 정의한 모듈입니다.

MVVM 개발 시 자주 사용되는 반복 코드를 줄이고,  
명확한 선언 기반 프로그래밍을 지향합니다.

---

## ✨ 주요 기능

| Attribute | 설명 |
|-----------|------|
| `VsProperty` | `INotifyPropertyChanged` 자동 구현 |
| `RelayCommand` | `ICommand` 속성 자동 생성 |
| `DreamineEntryAttribute` | 진입 클래스 마킹 (예: MainViewModel 등) |
| `DreamineModelAttribute` | Model 역할 클래스 식별자 |
| `DreamineEventAttribute` | 이벤트 바인딩 및 자동 해석용 |
| `DreaminePropertyAttribute` | 속성 모델링용 메타 정보 부여 |

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Attributes
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Attributes" Version="1.0.1" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Attributes](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Attributes)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "몰라도 쓸 수 있게,  
> 궁금하면 원리까지 이해되게."

드리마인은 최소단위 조립식 구조를 추구하며,  
SOLID 원칙과 MVVM 철학을 기반으로 FA 자동화에 특화된 아키텍처를 제공합니다.

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
🖋️ 기록자 서명: 장민수 드림

---

## 🇺🇸 English Summary

`Dreamine.MVVM.Attributes` is a core module of the Dreamine MVVM framework,  
providing attribute-based automation for ViewModel development.

### ✨ Key Features

- `VsProperty`: Auto-implements `INotifyPropertyChanged`
- `RelayCommand`: Generates `ICommand` properties automatically
- `DreamineEntryAttribute`: Marks entry ViewModel classes
- `DreamineModelAttribute`: Identifies model-layer classes
- `DreamineEventAttribute`: Enables attribute-based event handling
- `DreaminePropertyAttribute`: Adds meta information to properties

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Attributes
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
