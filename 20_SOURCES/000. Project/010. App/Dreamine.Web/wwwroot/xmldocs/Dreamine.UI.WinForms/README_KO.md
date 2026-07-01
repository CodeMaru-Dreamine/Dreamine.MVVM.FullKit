<!--!
\file README_KO.md
\brief Dreamine.UI.WinForms - WPF API 호환 다크 테마 WinForms 커스텀 컨트롤 라이브러리
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.WinForms

**Dreamine.UI.WinForms**는 `Dreamine.UI.Wpf.Controls`와 동일한 API를 WinForms 환경에서 제공하는 다크 테마 커스텀 컨트롤 라이브러리입니다.

동일한 속성 이름(`Content`, `IsChecked`, `IsExpanded` 등)을 사용하므로, 하나의 ViewModel을 WPF와 WinForms 양쪽에서 코드 중복 없이 재사용할 수 있습니다.

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

Dreamine 생태계의 WinForms 애플리케이션에는 다음이 필요합니다.

- Dreamine WPF 다크 테마에 맞게 스타일된 Owner-draw 컨트롤
- `Dreamine.UI.Wpf.Controls`와 동일한 속성 이름으로 플랫폼 독립적인 ViewModel 공유
- `SetStyle(UserPaint | AllPaintingInWmPaint | DoubleBuffer, true)`를 통한 깜빡임 없는 렌더링
- Win32 플레이스홀더 텍스트(`EM_SETCUEBANNER`) 및 GDI+ 둥근 사각형 헬퍼

---

## 주요 기능

- **DreamineButton** — 그라디언트 채우기, 광택 오버레이, Hover/Press 상태, `ICommand` 지원, `IsSelected` 밑줄
- **DreamineCheckBox** — `IsChecked` / `CheckedChanged`가 있는 Owner-draw 체크박스
- **DreamineRadioButton** — `Parent.Controls`를 스캔하는 `GroupName` 기반 상호 배타 선택
- **DreamineCheckLed** — 코너 배치, 선택적 펄스(페이드) 애니메이션이 있는 상태 LED
- **DreamineTextBox** — Win32 힌트, 포커스 테두리 강조가 있는 `UserControl` 래퍼
- **DreaminePasswordBox** — 힌트와 포커스 테두리가 있는 비밀번호 `UserControl` 래퍼
- **DreamineComboBox** — 완전한 다크 테마 아이템 렌더링이 있는 Owner-draw 드롭다운
- **DreamineTabControl** — 다크 헤더 바, 선택된 탭에 `AccentBlue` 밑줄
- **DreamineExpander** — 헤더 화살표 토글과 `ExpandedChanged` 이벤트가 있는 접기/펼치기 패널
- **DreamineTheme** — WPF 테마와 정확히 일치하는 정적 다크 팔레트 상수
- **DreamineDrawHelper** — GDI+ 헬퍼: 둥근 경로, 그라디언트 채우기, 광택 오버레이, 중앙 정렬 텍스트, 색상 블렌드

---

## 요구 사항

- **대상 프레임워크**: `net8.0-windows`
- **의존 패키지**:
  - `Dreamine.MVVM.Interfaces` (`ICommand` 지원용)

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.WinForms
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.WinForms" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.WinForms
├── Controls/
│   ├── DreamineButton.cs
│   ├── DreamineCheckBox.cs
│   ├── DreamineCheckLed.cs
│   ├── DreamineComboBox.cs
│   ├── DreamineExpander.cs
│   ├── DreaminePasswordBox.cs
│   ├── DreamineRadioButton.cs
│   ├── DreamineTabControl.cs
│   └── DreamineTextBox.cs
│   └── LedCorner.cs
├── DreamineDrawHelper.cs
└── DreamineTheme.cs
```

---

## 아키텍처 역할

```text
Dreamine.MVVM.Interfaces
        │
Dreamine.UI.WinForms     ← 이 패키지
        │
SampleCrossUi.WinForms
애플리케이션 코드
```

---

## 빠른 시작

```csharp
using Dreamine.UI.WinForms;
using Dreamine.UI.WinForms.Controls;

public partial class MainForm : Form
{
    public MainForm()
    {
        BackColor = DreamineTheme.AppBackground;

        var button = new DreamineButton
        {
            Content = "확인",
            Location = new Point(20, 20),
            Size = new Size(120, 40),
        };
        button.Click += (_, _) => MessageBox.Show("클릭됨");
        Controls.Add(button);

        var textBox = new DreamineTextBox
        {
            Hint = "이름을 입력하세요",
            Location = new Point(20, 80),
            Size = new Size(200, 36),
        };
        Controls.Add(textBox);

        var checkBox = new DreamineCheckBox
        {
            Content = "동의합니다",
            Location = new Point(20, 130),
            Size = new Size(160, 28),
        };
        checkBox.CheckedChanged += (_, _) => Console.WriteLine(checkBox.IsChecked);
        Controls.Add(checkBox);

        var expander = new DreamineExpander
        {
            Header = "상세 옵션",
            IsExpanded = false,
            Location = new Point(20, 170),
            Size = new Size(300, 200),
        };
        Controls.Add(expander);
    }
}
```

---

## 컨트롤 참조

### DreamineButton

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Content` | `string` | 버튼 라벨 텍스트 |
| `ShineColor` | `Color` | 상단 광택 오버레이 색상 |
| `CornerRadius` | `int` | 모서리 반경 |
| `IsSelected` | `bool` | 선택 상태 — `AccentBlue` 하단 밑줄 표시 |
| `Command` | `ICommand` | 클릭 시 실행 |
| `CommandParameter` | `object?` | 커맨드에 전달할 파라미터 |

Hover 시 그라디언트가 밝아지고, Press 시 어두워집니다.

---

### DreamineCheckBox

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Content` | `string` | 라벨 텍스트 |
| `IsChecked` | `bool` | 체크 상태 |
| `CheckedChanged` | `EventHandler` | 상태 변경 시 발생 |

---

### DreamineRadioButton

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Content` | `string` | 라벨 텍스트 |
| `IsChecked` | `bool` | 선택 상태 |
| `GroupName` | `string` | 상호 배타 그룹 (`Parent.Controls` 내에서 스캔) |
| `CheckedChanged` | `EventHandler` | 상태 변경 시 발생 |

---

### DreamineCheckLed

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `IsOn` | `bool` | LED 켜짐/꺼짐 |
| `IsPulse` | `bool` | 연속 페이드 인/아웃 애니메이션 활성화 |
| `Corner` | `LedCorner` | 배치 위치: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` |
| `Diameter` | `float` | LED 원의 지름 |

---

### DreamineTextBox

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Text` | `string` | 입력 텍스트 |
| `Hint` | `string` | 플레이스홀더 (Win32 `EM_SETCUEBANNER`) |
| `IsReadOnly` | `bool` | 읽기 전용 모드 |
| `TextChanged` | `EventHandler` | 텍스트 변경 시 발생 |

---

### DreaminePasswordBox

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Password` | `string` | 입력된 비밀번호 |
| `Hint` | `string` | 플레이스홀더 텍스트 |
| `PasswordChanged` | `EventHandler` | 비밀번호 변경 시 발생 |

---

### DreamineComboBox

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Items` | `ObjectCollection` | 항목 컬렉션 |
| `SelectedItem` | `object?` | 현재 선택된 항목 |
| `SelectedIndex` | `int` | 선택된 항목의 인덱스 |
| `SelectedIndexChanged` | `EventHandler` | 선택 변경 시 발생 |

Owner-draw 렌더링으로 드롭다운까지 다크 테마를 적용합니다.

---

### DreamineTabControl

- Owner-draw로 다크 헤더 바 렌더링.
- 선택된 탭에 `AccentBlue` 하단 밑줄 표시.
- Hover 시 탭 헤더 하이라이트.

---

### DreamineExpander

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Header` | `string` | 헤더 텍스트 |
| `IsExpanded` | `bool` | 펼침 / 접힘 상태 |
| `Content` | `Panel` | 내부 콘텐츠 패널 |
| `ExpandedChanged` | `EventHandler` | 상태 변경 시 발생 |

---

## DreamineTheme 색상 상수

```csharp
// 배경
DreamineTheme.AppBackground    // #1A1A2E  — 폼 / 윈도우
DreamineTheme.CardBackground   // #0F1E3A  — 패널 / 카드
DreamineTheme.InputBackground  // #162040  — 텍스트 입력 필드

// 강조색
DreamineTheme.AccentBlue       // #1E90FF

// 테두리
DreamineTheme.BorderNormal     // #2D4A6E
DreamineTheme.BorderFocus      // #1E90FF

// 텍스트
DreamineTheme.TextPrimary      // White
DreamineTheme.TextSecondary    // #8899AA

// LED
DreamineTheme.LedOnOuter       // #1FD36B
```

모든 색상 값은 WPF 다크 테마 팔레트와 정확히 일치합니다.

---

## DreamineDrawHelper

Owner-draw 렌더링에 사용하는 정적 헬퍼 클래스입니다.

| 메서드 | 설명 |
|---|---|
| `RoundedRect(RectangleF, float)` | 둥근 모서리 `GraphicsPath` 반환 |
| `FillRoundedRect(Graphics, Brush, Pen, Rectangle, float)` | 테두리 선택적 포함 둥근 사각형 채우기 |
| `FillRoundedGradient(Graphics, Color, Color, Pen?, Rectangle, float)` | 둥근 사각형 내 수직 그라디언트 채우기 |
| `DrawShineOverlay(Graphics, Color, Rectangle, float)` | 상단 절반에 반투명 광택 오버레이 그리기 |
| `DrawCenteredText(Graphics, string, Font, Color, Rectangle)` | 사각형 중앙에 텍스트 렌더링 |
| `Blend(Color, Color, float)` | 두 색상 사이의 선형 알파 블렌드 |

---

## 구현 특이사항

- 모든 커스텀 컨트롤은 `SetStyle(UserPaint | AllPaintingInWmPaint | DoubleBuffer | SupportsTransparentBackColor, true)`를 설정하여 깜빡임 없는 Owner-draw를 구현합니다.
- `DreamineTextBox`, `DreaminePasswordBox`, `DreamineComboBox`는 네이티브 컨트롤을 `UserControl` 내부에 래핑하여 테두리를 직접 제어합니다.
- `DreamineRadioButton`은 체크 시점에 `Parent.Controls`를 스캔하여 동일 `GroupName`의 형제 컨트롤을 해제합니다.
- `DreamineCheckLed` 펄스 애니메이션은 `System.Windows.Forms.Timer`로 동작하며, 컨트롤 Dispose 시 함께 정지·해제됩니다.

---

## 크로스플랫폼 노트

`Dreamine.UI.WinForms`는 `Dreamine.UI.Wpf.Controls`의 속성 이름을 의도적으로 미러링하여 ViewModel이 이식 가능합니다.

```csharp
// 공유 ViewModel
public class LoginViewModel : INotifyPropertyChanged
{
    public string UserName { get; set; }
    public bool   RememberMe { get; set; }
    public ICommand LoginCommand { get; set; }
}

// WPF — 데이터 바인딩
<dreamine:DreamineTextBox  Text="{Binding UserName}" />
<dreamine:DreamineCheckBox Content="로그인 유지" IsChecked="{Binding RememberMe}" />
<dreamine:DreamineButton   Content="로그인" Command="{Binding LoginCommand}" />

// WinForms — 동일 ViewModel, 바인딩 인프라 불필요
textBox.Text          = vm.UserName;
checkBox.IsChecked    = vm.RememberMe;
button.Command        = vm.LoginCommand;
```

---

## 라이선스

MIT License
