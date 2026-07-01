<!--!
\file README_KO.md
\brief Dreamine.UI.Wpf.Controls - Dreamine UI 프레임워크의 커스텀 WPF 컨트롤 라이브러리
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf.Controls

**Dreamine.UI.Wpf.Controls**는 Dreamine 기반 애플리케이션 전반에서 사용하는 핵심 커스텀 WPF 컨트롤 세트를 제공합니다.

상호작용 컨트롤, 네비게이션 인프라, 뷰 전환 유틸리티를 포함하며 모두 Dreamine MVVM 패턴과 통합되어 있습니다.

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

Dreamine MVVM 기반 WPF 애플리케이션에는 다음이 필요합니다.

- 프로젝트별 기능이 추가된 표준 WPF 컨트롤의 스타일 확장
- 뷰 관리, 전역 입력 훅, 자동 로그아웃 타이머를 갖춘 네비게이션 바
- ViewModel 생명주기와 분리된 뷰 로딩을 위한 뷰 스위처
- Dreamine DI 컨테이너와 통합된 메시지 박스 및 체크 셀렉터 다이얼로그

---

## 주요 기능

- **DreamineButton** — 아이콘 + 그림자 + 등급 기반 접근 제어, 첨부 커맨드 지원
- **DreamineNavigationBar** — SharpHook 전역 키보드 훅과 유휴 로그아웃 타이머가 포함된 탭 기반 네비게이션 바
- **ViewLoader** — `DMContainer`를 통해 WPF 뷰와 ViewModel을 resolve하고 인스턴스화
- **ViewSwitcher** — 표시/숨김 전환 시 `IActivatable` / `IVisibilityAware` ViewModel에 알림
- **DreamineMessageBox** — 자동 닫힘 타이머가 있는 테마 적용 비동기 메시지 박스
- **DreamineCheckSelector** — 다중 항목 체크 리스트 셀렉터
- 스타일이 적용된 파생 컨트롤 전체: CheckBox, CheckLed, ComboBox, DataGrid, Expander, Image, Label, ListBox, PasswordBox, RadioButton, TabControl, TextBlock, TextBox, TimeSpinner

---

## 요구 사항

- **대상 프레임워크**: `net8.0-windows`
- **의존 패키지**:
  - `Dreamine.UI.Wpf`
  - `Dreamine.MVVM.ViewModels`
  - `Dreamine.MVVM.Interfaces`
  - `Microsoft.Xaml.Behaviors.Wpf`
  - `SharpHook` 5.3.8+

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf.Controls
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf.Controls" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.Wpf.Controls
├── CheckSelector/
│   └── DreamineCheckSelector.xaml(.cs)
├── Controls/
│   ├── DreamineButton.cs
│   ├── DreamineCheckBox.cs / DreamineCheckLed.cs
│   ├── DreamineComboBox.cs / DreamineControl.cs
│   ├── DreamineDataGrid.cs / DreamineExpander.cs
│   ├── DreamineImage.cs / DreamineLabel.cs
│   ├── DreamineListBox.cs / DreaminePasswordBox.cs
│   ├── DreamineRadioButton.cs / DreamineTabControl.cs
│   ├── DreamineTextBlock.cs / DreamineTextBox.cs
│   └── DreamineTimeSpinner.cs
├── MessageBox/
│   ├── DreamineMessageBox.cs
│   └── DreamineMessageBoxWindow.xaml(.cs)
└── Navigation/
    ├── ButtonData.cs
    ├── DreamineNavigationBar.xaml(.cs)
    ├── ViewLoader.cs
    └── ViewSwitcher.cs
```

---

## 아키텍처 역할

```text
Dreamine.UI.Wpf
        │
Dreamine.UI.Wpf.Controls     ← 이 패키지
        │
Dreamine.UI.Wpf.Equipment
Dreamine.UI.Wpf.Themes
애플리케이션 코드
```

---

## 빠른 시작

### DreamineButton

```xml
xmlns:ctrl="clr-namespace:Dreamine.UI.Wpf.Controls;assembly=Dreamine.UI.Wpf.Controls"

<ctrl:DreamineButton Content="저장"
                     Command="{Binding SaveCommand}"
                     Grade="1"
                     MinimumGrade="1"
                     UseShadow="True" />
```

### DreamineNavigationBar

```xml
<ctrl:DreamineNavigationBar ButtonDatas="{Binding MenuItems}"
                             AutoLogoutSeconds="300" />
```

```csharp
// 뷰 등록
ViewLoader.Register("MainView", typeof(MainView), typeof(MainViewModel));
```

### ViewSwitcher — ViewModel 생명주기

```csharp
// ViewModel에서 IActivatable 또는 IVisibilityAware 구현
public class DashboardViewModel : ViewModelBase, IActivatable
{
    public void Activate()   { /* 폴링 시작 */ }
    public void Deactivate() { /* 폴링 중지 */ }
}

// 시작 시 등록
ViewSwitcher.RegisterViewModel("DashboardView", dashboardVm);
```

### DreamineMessageBox

```csharp
var result = await DreamineMessageBox.ShowAsync(
    "변경 사항을 취소하시겠습니까?",
    "확인",
    autoClick: MessageBoxResult.Cancel,
    autoClickDelaySeconds: 10);
```

---

## 열거형 참조

| 열거형 | 값 | 사용처 |
|---|---|---|
| `SelectedVisualMode` | `BorderOnly`, `BackgroundOnly`, `Both` | `DreamineButton` |
| `IconPosition` | `Left`, `Right`, `Top`, `Bottom`, `Full` | `DreamineButton` |
| `ExpanderArrowPlacement` | `Left`, `Right` | `DreamineExpander` |
| `NavigationBarPosition` | `Left`, `Right`, `Top`, `Bottom` | `DreamineNavigationBar` |

---

## 설계 노트

- 모든 컨트롤은 생성 시 `Dreamine.UI.Wpf.Themes`에서 `ResourceDictionary`를 자동 병합
- `ViewSwitcher`는 로컬 딕셔너리를 사용 — 네비게이션 시작 전에 `RegisterViewModel`로 등록 필요
- `DreamineNavigationBar`는 프로세스 전체 키보드 모니터링에 `SharpHook.TaskPoolGlobalHook` 사용; 훅은 모든 인스턴스에서 공유
- Grade / MinimumGrade 접근 제어는 클릭 시점에 평가되며 로그인 시스템 불필요

---

## 라이선스

MIT License
