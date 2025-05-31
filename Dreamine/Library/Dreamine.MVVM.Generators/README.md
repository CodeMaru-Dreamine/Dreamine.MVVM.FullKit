# 🌟 Dreamine.MVVM.Generators

## 🇰🇷 한국어 소개

`Dreamine.MVVM.Generators`는 Dreamine MVVM 프레임워크의 소스 생성기(Source Generator) 모듈입니다.  
MVVM 개발 시 반복적으로 작성되는 Property, Command 등의 코드를  
자동으로 생성해 개발 효율성과 생산성을 극대화합니다.

이 모듈은 .NET의 Roslyn 기반 `Analyzer`로 동작하며,  
일반적인 참조 방식이 아닌 **Analyzers 경로에 직접 등록**해야 합니다.

---

## ⚙️ 사용 방법

패키지를 설치한 후 `.csproj`에 다음과 같이 수동 등록해야 합니다:

```xml
<ItemGroup>
  <Analyzer Include="$(NuGetPackageRoot)dreamine.mvvm.generators\1.0.0\analyzers\dotnet\cs\Dreamine.MVVM.Generators.dll" />
</ItemGroup>
```

> `$(NuGetPackageRoot)`는 자동으로 NuGet 캐시 루트를 참조합니다.  
> Visual Studio 및 CLI 모두에서 동작합니다.

---

## ✨ 주요 기능

| 기능 | 설명 |
|------|------|
| `[VsProperty]` | 자동으로 `INotifyPropertyChanged` 구현 |
| `[RelayCommand]` | 메서드를 기반으로 `ICommand` 속성 자동 생성 |
| `[ViewModelEntry]` | ViewModel 진입점을 자동 마킹 |
| `[GenerateForwardProperty]` | 내부 속성 → 외부 노출용 속성 자동 생성 (예정) |

---

## 📦 NuGet 설치

```bash
dotnet add package Dreamine.MVVM.Generators
```

또는 `.csproj`에 추가:

```xml
<PackageReference Include="Dreamine.MVVM.Generators" Version="1.0.0" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

※ 단, 실제 분석기 적용을 위해 위의 `<Analyzer Include=...>` 블럭도 반드시 필요합니다.

---

## 🔗 관련 링크

- 📁 GitHub: [Dreamine.MVVM.Generators](https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Generators)
- 📦 NuGet: [Dreamine.MVVM.Generators](https://www.nuget.org/packages/Dreamine.MVVM.Generators)
- 💬 문의: [CodeMaru 드리마인팀](mailto:togood1983@gmail.com)

---

## 🧙 프로젝트 철학

> "타이핑하지 마라, 선언하라."

MVVM에서 반복되는 코드는 사람이 작성하는 것이 아닌,  
**정적 분석기가 생성하는 시대**를 지향합니다.

---

## 🖋️ 작성자 정보

- 작성자: Dreamine Core Team  
- 소유자: minsujang  
- 날짜: 2025년 5월 25일  
- 라이선스: MIT

---

📅 문서 작성일: 2025년 5월 25일  
⏱️ 총 소요시간: 약 20분  
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림

---

## 🇺🇸 English Summary

`Dreamine.MVVM.Generators` is a source generator module for the Dreamine framework.  
It automates repetitive MVVM patterns using Roslyn-based compile-time code generation.

### ⚙️ How to Use

Add the generator explicitly in your `.csproj`:

```xml
<ItemGroup>
  <Analyzer Include="$(NuGetPackageRoot)dreamine.mvvm.generators\1.0.0\analyzers\dotnet\cs\Dreamine.MVVM.Generators.dll" />
</ItemGroup>
```

### ✨ Features

- `[VsProperty]`: Auto-implements property change notification
- `[RelayCommand]`: Generates `ICommand` from method definitions
- `[ViewModelEntry]`: Marks entry ViewModel
- `[GenerateForwardProperty]`: Forwards internal properties (upcoming)

---

### 📦 Installation

```bash
dotnet add package Dreamine.MVVM.Generators
```

---

### 🔖 License

MIT

---

📅 Last updated: May 25, 2025  
✍️ Author: Dreamine Core Team  
🤖 Assistant: ChatGPT (GPT-4)
