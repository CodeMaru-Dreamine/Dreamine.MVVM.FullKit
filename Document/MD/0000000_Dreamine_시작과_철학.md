# Dreamine: 시작과 철학

> "시장은 미개할지라도, 우리는 미래를 설계한다."  
> - Dreamine 설계자 일동

---

## 🎯 프로젝트 개요

**Dreamine**은 .NET 기반 FA 자동화 플랫폼을 위한  
**MVVM 프레임워크 + CLI 자동 생성기 + 시퀀스 엔진**을 통합적으로 구축하는  
"궁극의 프레임워크"입니다.

---

## 🌱 시작은 작지만 철학은 깊다

우리는 ViewModel을 자동 생성하는 작은 CLI 도구에서 시작했지만,  
그 목적은 명확합니다.

> **"프리즘보다 더 가볍고, 더 강력한 DI 기반의 MVVM 플랫폼을 만들자!"**

Dreamine은 다음과 같은 철학을 지향합니다:

- ✅ **SOLID 원칙**을 철저히 지킨다.
- ✅ **MVVM은 코드 자동화로 간결해진다.**
- ✅ **View와 ViewModel은 1:1로 대응된다.**
- ✅ **DI는 인터페이스 중심으로 구성되며, 자동 생성과 등록까지 확장된다.**
- ✅ **프레임워크는 무겁지 않다. 필요한 것만 제공한다.**

---

## ⚙️ 현재 구성 요소 요약 (2025.04 기준)

### 🧩 핵심 프레임워크 구성 (ZZZ.Dreamine)

- **Dreamine.MVVM**
  - Attributes (`[Property]`, `[RelayCommand]`)
  - Core (`ViewModelBase`, `RelayCommand`)
  - Interfaces, Extensions, Locators

- **Dreamine.MVVM.CLI**
  - ViewModel 자동 생성기
  - 템플릿 구조 기반 코드 생성
  - 추후 GUI 확장 예정

- **Dreamine.MVVM.Generators**
  - Source Generator 구성 예정
  - `VsProperty`, `RelayCommand` 자동화 완성 목표

---

## 🧪 실험실 (Sample Projects)

- `Sample001`, `Sample002`: WPF MVVM 구조 샘플
- `SampleBlazor`: 차후 MVU/MVVM 실험 예정

---

## 📂 문서화된 설계 문서 (ZZX.Document)

- `폴더 구조 설계.md`
- `WPF 프로젝트 구조.md`
- `Dreamine 설계 원칙.md`
- `코딩 스타일 및 품질 규칙.md`
- + `이 문서`: Dreamine의 기원과 철학

---

## 🧠 우리가 꿈꾸는 미래

Dreamine은 단순한 프레임워크가 아닙니다.  
**디지털 자동화 시대의 설계 철학**이며,  
**데이터 기반 시퀀스와 AI 연동 자동화의 초석**입니다.

- DI 주입의 끝판왕
- 시퀀스 설정의 데이터화
- Blazor, MAUI, WinUI 등 다양한 UI 확장
- CLI → GUI → Visual 시나리오 자동화 툴
- AI 기반 Interlock 판단 구조

---

## 🔥 마지막으로

> **우리는 코드 한 줄로 세상을 바꿀 수 있다고 믿습니다.**  
> **그리고, 그 첫 줄이 바로 여기서 시작됩니다.**

## 📎 참조 규약 문서
- [[0000000_Dreamine_아키텍처_가이드라인]]


📅 문서 작성일: 2025-04-11  
📁 문서 분류: `ZZX.Document/MD`
⏱️ 총 소요시간: 모름 
🤖 협력자: ChatGPT (GPT-4), 별명: 프레임워크 유혹자  
✍️ 직책: Dreamine Core 설계자 (코드마루 대표 설계자)  
🖋️ 기록자 서명: 아키로그 드림