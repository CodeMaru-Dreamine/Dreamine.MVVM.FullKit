# 🌟 Dreamine.MVVM.Locators

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Locators`는 ViewModel의 인스턴스를 View에서 바인딩하거나,  
디자인 타임(Design-Time)과 런타임(Runtime)을 구분하여 처리하기 위한  
**ViewModelLocator 패턴**을 기반으로 하는 헬퍼 모듈입니다.

이 모듈은 ViewModel 인스턴스 생성, DI 컨테이너와의 연결,  
디자인 타임 Mock 데이터 지원 등을 통해 XAML과 MVVM의 연결을 단순화합니다.

---

## ✨ 주요 클래스

| 클래스 | 설명 |
|--------|------|
| `ViewModelLocator` | View와 ViewModel을 연결하는 글로벌 Locator 클래스 |
| `ViewModelLocator.Current` | 싱글톤 접근 방식 제공 |
| 디자인타임 지원 | `IsInDesignMode` 기반 가짜 ViewModel 반환 가능 |

---

## 🧑‍💻 사용 예시

```xml
<Window.DataContext>
  <Binding Source="{x:Static locator:ViewModelLocator.Main}" />
</Window.DataContext>
```

또는

```xml
DataContext="{Binding Source={StaticResource ViewModelLocator}, Path=Main}"
```

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Locators
```

또는 `.csproj`에 직접 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Locators" Version="1.0.0" />
```

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Locators](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Locators)
- 📝 문서: 준비 중
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "XAML에 코드 비하인드 없이 ViewModel을 연결하라."

Dreamine은 명시적 바인딩과 DI 기반 ViewModel 주입을 동시에 지원하며,  
테스트 및 디자인 시각화 모두를 고려합니다.

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

`Dreamine.MVVM.Locators` provides a lightweight ViewModelLocator  
pattern implementation to connect Views and ViewModels in XAML.

It supports runtime binding, design-time mock resolution, and singleton access.

### ✨ Key Components

| Class | Description |
|-------|-------------|
| `ViewModelLocator` | Static access to ViewModels from XAML |
| `IsInDesignMode` | Design-time mock ViewModel resolution |
| `Current` | Singleton locator instance for DI/container access |

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Locators
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
