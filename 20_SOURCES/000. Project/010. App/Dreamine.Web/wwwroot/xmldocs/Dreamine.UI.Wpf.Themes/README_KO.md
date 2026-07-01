<!--!
\file README_KO.md
\brief Dreamine.UI.Wpf.Themes - 모든 Dreamine UI 컨트롤을 위한 WPF ResourceDictionary 스타일 및 템플릿 파일
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf.Themes

**Dreamine.UI.Wpf.Themes**는 모든 Dreamine UI 컨트롤의 WPF `ResourceDictionary` 스타일과 컨트롤 템플릿 정의를 포함합니다.

순수 XAML 패키지입니다 — 코드비하인드 로직이 없습니다.  
`Dreamine.UI.Wpf.Controls`와 `Dreamine.UI.Wpf.Equipment`의 컨트롤들은 생성 시점에 이 패키지의 스타일을 자동 병합합니다.

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

WPF 컨트롤에는 다음을 위한 중앙화된 위치가 필요합니다.

- 모든 Dreamine 컨트롤의 일관된 시각적 디자인
- 컨트롤 로직을 재빌드하지 않고 업데이트 가능한 스타일 정의
- 시각 레이어와 동작 레이어의 분리

모든 XAML 스타일을 전용 패키지로 격리함으로써 애플리케이션 팀은 컨트롤 구현을 건드리지 않고 테마 레이어를 재정의하거나 교체할 수 있습니다.

---

## 주요 기능

- 컨트롤 유형별 `ResourceDictionary` 파일 하나씩
- 루트 병합 딕셔너리 `DreamineThemes.xaml`이 모든 스타일 파일 포함
- 스타일 키가 WPF `TargetType` 패턴과 일치 — 애플리케이션 XAML에서 명시적 키 불필요
- `assembly=` 한정자 xmlns를 통한 컨버터 참조 지원
- 숫자 키패드 및 메시지 박스 버튼 스타일 포함

---

## 요구 사항

- **대상 프레임워크**: `net8.0-windows`
- **의존 패키지**:
  - `Dreamine.UI.Wpf`
  - `Dreamine.UI.Wpf.Controls`

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf.Themes
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf.Themes" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.Wpf.Themes
├── DreamineThemes.xaml                — 루트 병합 딕셔너리
├── DreamineButtonStyle.xaml
├── DreamineCheckBoxStyle.xaml
├── DreamineCheckLedStyle.xaml
├── DreamineComboBoxStyle.xaml
├── DreamineDataGridStyle.xaml
├── DreamineExpanderStyle.xaml
├── DreamineGridStyles.xaml
├── DreamineImageStyle.xaml
├── DreamineLabelStyle.xaml
├── DreamineListBoxStyle.xaml
├── DreamineMessageBoxButtonStyle.xaml
├── DreaminePasswordBoxStyle.xaml
├── DreamineRadioButtonStyle.xaml
├── DreamineTabControlStyle.xaml
├── DreamineTextBlockStyle.xaml
├── DreamineTextBoxStyle.xaml
├── DreamineTimeSpinnerStyle.xaml
└── DreamineKeyPadStyles.xaml
```

---

## 아키텍처 역할

```text
Dreamine.UI.Wpf
Dreamine.UI.Wpf.Controls
        │
Dreamine.UI.Wpf.Themes       ← 이 패키지 (시각 레이어만)
        │
애플리케이션 코드
```

컨트롤 라이브러리가 이 패키지를 참조하여 스타일을 자동 병합합니다.  
애플리케이션 코드도 `App.xaml`을 통해 스타일을 전체 적용할 때 이 패키지를 참조할 수 있습니다.

---

## 사용 방법

### 방법 A) 자동 병합 (기본 동작)

컨트롤 인스턴스화 시 일치하는 `ResourceDictionary`를 자동 병합합니다.  
애플리케이션 XAML에서 별도 설정이 필요 없습니다.

### 방법 B) 전역 애플리케이션 테마

`App.xaml`에 추가하여 프로젝트 전체에 Dreamine 스타일을 적용합니다.

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Dreamine.UI.Wpf.Themes;component/DreamineThemes.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 방법 C) 개별 스타일 파일

필요한 스타일만 포함합니다.

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="/Dreamine.UI.Wpf.Themes;component/DreamineButtonStyle.xaml" />
    <ResourceDictionary Source="/Dreamine.UI.Wpf.Themes;component/DreamineTextBoxStyle.xaml" />
</ResourceDictionary.MergedDictionaries>
```

---

## 스타일 파일 참조

| 파일 | 대상 컨트롤 |
|---|---|
| `DreamineButtonStyle.xaml` | `DreamineButton` |
| `DreamineCheckBoxStyle.xaml` | `DreamineCheckBox` |
| `DreamineCheckLedStyle.xaml` | `DreamineCheckLed` |
| `DreamineComboBoxStyle.xaml` | `DreamineComboBox` |
| `DreamineDataGridStyle.xaml` | `DreamineDataGrid` |
| `DreamineExpanderStyle.xaml` | `DreamineExpander` |
| `DreamineGridStyles.xaml` | `Grid` 레이아웃 헬퍼 |
| `DreamineImageStyle.xaml` | `DreamineImage` |
| `DreamineLabelStyle.xaml` | `DreamineLabel` |
| `DreamineListBoxStyle.xaml` | `DreamineListBox` |
| `DreamineMessageBoxButtonStyle.xaml` | 메시지 박스 버튼 |
| `DreaminePasswordBoxStyle.xaml` | `DreaminePasswordBox` |
| `DreamineRadioButtonStyle.xaml` | `DreamineRadioButton` |
| `DreamineTabControlStyle.xaml` | `DreamineTabControl` |
| `DreamineTextBlockStyle.xaml` | `DreamineTextBlock` |
| `DreamineTextBoxStyle.xaml` | `DreamineTextBox` |
| `DreamineTimeSpinnerStyle.xaml` | `DreamineTimeSpinner` |
| `DreamineKeyPadStyles.xaml` | 가상 키보드 키 |

---

## 설계 노트

- 크로스 어셈블리 타입에 대한 모든 `xmlns` 선언은 `assembly=` 한정자 사용 — 프로젝트 경계 간 XAML 컴파일에 필수
- `NullToVisibilityConverter` 같은 컨버터 리소스는 이를 사용하는 스타일 파일에 인라인으로 선언
- 이 패키지에 코드비하인드 없음; 스타일은 순수 선언적
- 스타일 재정의: 애플리케이션 리소스 딕셔너리에서 컨트롤 타입에 일치하는 `x:Key`가 있는 스타일 정의 — 병합 스타일보다 우선 적용

---

## 라이선스

MIT License
