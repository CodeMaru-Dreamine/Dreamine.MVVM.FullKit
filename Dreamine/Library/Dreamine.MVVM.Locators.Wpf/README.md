# 🌟 Dreamine.MVVM.Locators.Wpf

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Locators.Wpf`는 WPF 환경에서 ViewModelLocator 패턴을  
보다 유연하게 사용할 수 있도록 도와주는 ViewModel 자동 바인딩 유틸리티입니다.

특히 `ViewModelBinder`를 통해 XAML에서 명시적인 코딩 없이  
View와 ViewModel을 자동으로 연결할 수 있도록 지원합니다.

---

## ✨ 주요 클래스

| 클래스 | 설명 |
|--------|------|
| `ViewModelBinder` | View의 타입 또는 네이밍 규칙에 따라 ViewModel을 자동 바인딩 |
| `Attach` 메서드 | 특정 View에 ViewModel 수동 연결도 가능 |
| 디자인 타임 | 디자인 모드 여부 자동 판단 및 처리 지원 |

---

## 🧑‍💻 사용 예시

### 1. 네이밍 기반 자동 연결 (XAML)

```xml
<Window
  xmlns:locator="clr-namespace:Dreamine.MVVM.Locators.Wpf"
  locator:ViewModelBinder.Enable="True">
```

### 2. 수동 연결 (CodeBehind)

```csharp
ViewModelBinder.Attach(this, typeof(MainViewModel));
```

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Locators.Wpf
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Locators.Wpf" Version="1.0.0" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Locators.Wpf](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Locators.Wpf)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "MVVM 바인딩은 자동화되어야 한다."

Dreamine은 명시적 DI 뿐 아니라, **규칙 기반 자동화된 바인딩 방식**도 함께 제공합니다.  
이를 통해 ViewModelLocator 패턴을 더욱 실용적으로 사용할 수 있습니다.

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

`Dreamine.MVVM.Locators.Wpf` offers auto-binding support for ViewModels  
in WPF environments based on naming conventions or manual attachment.

### ✨ Key Features

| Component | Description |
|-----------|-------------|
| `ViewModelBinder` | Automatically binds View to ViewModel via XAML |
| `Attach()` | Manually connects View to ViewModel at runtime |
| Design-mode support | Works with runtime or design-mode detection |

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Locators.Wpf
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
