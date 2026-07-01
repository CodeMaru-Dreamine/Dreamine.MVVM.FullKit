<!--!
\file README_KO.md
\brief Dreamine.UI.Maui - WPF/WinForms API 호환 다크 테마 .NET MAUI 커스텀 컨트롤 라이브러리
\author Dreamine Core Team
\date 2026-06-29
\version 1.0.0
-->

# Dreamine.UI.Maui

**Dreamine.UI.Maui**는 `Dreamine.UI.Wpf.Controls`/`Dreamine.UI.WinForms`와 동일한 개념의 API를 .NET MAUI 환경에서 제공하는 다크 테마 커스텀 컨트롤 및 팝업 오버레이 라이브러리입니다.

동일한 개념(`IsChecked`, `IsOn`, `IsExpanded`, `DialogResult` 스타일의 팝업 결과 등)을 사용하므로, 하나의 ViewModel을 WPF·WinForms·MAUI 양쪽에서 코드 중복 없이 재사용할 수 있습니다.

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

.NET MAUI에는 Dreamine 생태계의 WPF/WinForms 앱들이 의존하는 몇 가지 컨트롤이 기본적으로 없습니다.

- 상태 LED, Expander, 앱 스타일의 메시지박스/팝업 컨트롤이 기본 제공되지 않습니다.
- Windows(WinUI)의 네이티브 `CheckBox`는 내부적으로 거대한 최소 히트박스를 차지해서 라벨과 가깝게 배치할 수 없습니다 — `DreamineCheckBox`는 이를 대체하는 완전 커스텀 드로우 컨트롤입니다.
- WPF/WinForms처럼 테두리 없는 팝업 "창"을 MAUI에서는 만들 수 없어서, 반투명 배경을 가진 모달 `ContentPage`로 팝업을 구현했습니다.
- 데스크톱 모드 MAUI(Windows)에서는 OS가 화면 키보드를 자동으로 띄워주지 않아서, 데모/샘플용으로 `DreamineVirtualKeyboard`를 만들었습니다.

---

## 주요 기능

- **DreamineCheckBox** — 커스텀 드로우 체크박스(`Border` + 체크 아이콘), 라벨을 탭해도 토글됩니다
- **DreamineCheckLed** — On/Off, 펄스(페이드) 애니메이션, 가변 지름, 배지 스타일 배치를 위한 `Corner` 앵커(`TopLeft`/`TopRight`/`BottomLeft`/`BottomRight`) 지원
- **DreamineExpander** — 헤더 화살표 토글과 `ExpandedChanged` 이벤트가 있는 접기/펼치기 패널
- **DreamineVirtualKeyboard** — 데스크톱 모드 MAUI 데모용 화면 QWERTY 키보드(숫자, 기호, Shift/Caps Lock, Enter 지원), 임의의 `Entry`에 연결 가능
- **DreamineMessageBox** / **DreamineBlinkPopup** (`Dreamine.UI.Maui.Popup`) — OK/Cancel/Yes/No 결과, 자동닫힘 카운트다운, 배경 점멸 알람 팝업을 제공하는 모달 오버레이 팝업

---

## 요구 사항

- **대상 프레임워크**: `net9.0-windows10.0.19041.0` (MAUI Windows)
- **의존 패키지**:
  - `Microsoft.Maui.Controls`

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.Maui
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Maui" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.Maui
├── DreamineCheckBox.xaml(.cs)
├── DreamineCheckLed.xaml(.cs)
├── DreamineExpander.xaml(.cs)
├── DreamineVirtualKeyboard.xaml(.cs)
└── Popup/
    ├── DreamineDialogResult.cs
    ├── DreamineMessageBox.cs
    ├── DreamineMessageBoxPage.xaml(.cs)
    ├── BlinkPopupOptions.cs
    ├── DreamineBlinkPopup.cs
    └── BlinkPopupPage.xaml(.cs)
```

---

## 아키텍처 역할

```text
Microsoft.Maui.Controls
        │
Dreamine.UI.Maui        ← 이 패키지
        │
SampleCrossUi.Maui
애플리케이션 코드
```

---

## 빠른 시작

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:dc="clr-namespace:Dreamine.UI.Maui;assembly=Dreamine.UI.Maui">

    <VerticalStackLayout Spacing="12">

        <dc:DreamineCheckBox Text="동의합니다" IsChecked="{Binding RememberMe}" />

        <dc:DreamineCheckLed IsOn="{Binding LedIsOn}" IsPulse="{Binding LedIsPulse}" />

        <dc:DreamineExpander Header="상세 옵션" IsExpanded="False">
            <dc:DreamineExpander.ExpanderContent>
                <Label Text="펼쳤을 때만 보이는 내용입니다." />
            </dc:DreamineExpander.ExpanderContent>
        </dc:DreamineExpander>

    </VerticalStackLayout>
</ContentPage>
```

```csharp
using Dreamine.UI.Maui.Popup;

var result = await DreamineMessageBox.ShowAsync(
    "작업이 성공적으로 완료되었습니다.",
    "완료",
    autoClickDelaySeconds: 5);

var alarmResult = await DreamineBlinkPopup.ShowAsync(new BlinkPopupOptions
{
    Title = "⚠ ALARM",
    Message = "설비 이상이 감지되었습니다.\n운영자 확인이 필요합니다.",
    UseBlink = true,
    Color1 = Color.FromArgb("#B41E1E"),
    Color2 = Color.FromArgb("#500A0A"),
    ForegroundColor = Colors.Yellow,
    OkText = "확인",
    CancelText = "취소"
});
```

---

## 컨트롤 참조

### DreamineCheckBox

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `IsChecked` | `bool` (`TwoWay`) | 체크 상태 |
| `Text` | `string` | 라벨 텍스트 — 라벨을 탭해도 체크박스가 토글됩니다 |
| `CheckedChanged` | `EventHandler<bool>` | 상태 변경 시 발생 |

> Windows(WinUI)의 네이티브 `CheckBox`는 `WidthRequest`로 줄일 수 없는 거대한 최소 너비를 가져서 라벨과의 간격이 벌어지는 문제가 있어, 처음부터 직접 그려서 만들었습니다.

---

### DreamineCheckLed

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `IsOn` | `bool` | LED 켜짐/꺼짐 |
| `IsPulse` | `bool` | `IsOn`이 `true`일 때 연속 페이드 인/아웃 애니메이션 활성화 |
| `Diameter` | `double` | LED 원의 지름(기본값 `24`) |
| `Corner` | `DreamineCheckLedCorner` | 부모 컨테이너의 어느 모서리에 LED를 붙일지 지정: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` — 다른 컨트롤 위에 상태 배지로 얹을 때 사용 |

> 애니메이션은 컨트롤에 `Handler`가 생긴 뒤(=실제로 화면에 붙은 뒤)로 미뤄집니다. 그 전에 `Animation`/`AbortAnimation`을 호출하면 `ArgumentException: Unable to find IAnimationManager`가 발생합니다.

---

### DreamineExpander

| 속성 / 이벤트 | 타입 | 설명 |
|---|---|---|
| `Header` | `string` | 헤더 텍스트 |
| `IsExpanded` | `bool` (`TwoWay`) | 펼침/접힘 상태, 헤더를 탭하면 토글됩니다 |
| `ExpanderContent` | `View` | 펼쳤을 때 보이는 내부 콘텐츠(컨트롤 자신의 루트 레이아웃인 `ContentView.Content`와는 별개) |
| `ExpandedChanged` | `EventHandler` | 펼침 상태 변경 시 발생 |

---

### DreamineVirtualKeyboard

| 멤버 | 설명 |
|---|---|
| `Attach(Entry entry)` | `entry`가 포커스를 받으면 키보드를 표시하고, 키 입력을 `entry.Text`에 직접 삽입/삭제합니다 |

- 미국식 키보드 기호 배열을 포함한 전체 QWERTY 배열: 숫자 행(`!@#$%^&*()_+`), 괄호(`[]{}`), 구두점(`;':" ,.<>/?`).
- **Shift**: 한 번 탭하면 다음 글자 하나만 대문자(자동으로 해제); **더블탭**하면 Caps Lock으로 고정(다시 탭하면 해제). 글자뿐 아니라 Shift 영향을 받는 모든 키가 즉시 표시 글리프를 갱신합니다.
- **Enter**: 대상 `Entry`의 포커스를 해제하고 키보드를 닫습니다(별도의 "Close" 키는 없음 — `Entry`가 한 줄짜리라 Enter가 "입력 완료" 역할도 겸합니다).
- 대상 `Entry`의 `Unfocused` 이벤트로는 숨기지 **않습니다** — 키 버튼을 탭하는 순간 `Entry`가 잠시 포커스를 잃는데, 그 타이밍에 숨기면 글자가 삽입되기 전에 `Clicked` 처리가 끊겨버립니다.

---

### DreamineMessageBox (`Dreamine.UI.Maui.Popup`)

```csharp
Task<DreamineDialogResult> ShowAsync(string message, string title = "Information", int autoClickDelaySeconds = 0);
Task<DreamineDialogResult> ShowOkCancelAsync(string message, string title = "Confirm");
Task<DreamineDialogResult> ShowYesNoAsync(string message, string title = "Question");
```

- 모달 `ContentPage`(`DreamineMessageBoxPage`)를 `Navigation.PushModalAsync`로 띄우고 반투명 배경으로 팝업처럼 보이게 구현했습니다 — MAUI에는 WPF/WinForms처럼 가볍게 독립 OS 창을 띄우는 방법이 없습니다.
- `autoClickDelaySeconds`를 지정하면 메시지에 카운트다운이 표시되고, 0이 되면 자동으로 `OK`로 완료됩니다.

### DreamineBlinkPopup (`Dreamine.UI.Maui.Popup`)

```csharp
Task<DreamineDialogResult> ShowAsync(BlinkPopupOptions options);
```

| `BlinkPopupOptions` 속성 | 타입 | 설명 |
|---|---|---|
| `Title` / `Message` | `string?` | 팝업 텍스트 |
| `OkText` / `CancelText` | `string?` | 버튼 라벨 — 비워두면 해당 버튼이 숨겨집니다 |
| `UseBlink` | `bool` | 타이머로 배경을 `Color1`/`Color2` 사이에서 번갈아 바꿉니다 |
| `Color1` / `Color2` | `Color` | 점멸 색상 |
| `ForegroundColor` | `Color` | 제목/메시지 텍스트 색상 |
| `BlinkIntervalMs` | `int` | 점멸 간격, 기본값 `600` |

### DreamineDialogResult

```csharp
public enum DreamineDialogResult { None, OK, Cancel, Yes, No }
```

두 팝업 API가 공통으로 쓰는 플랫폼 독립적 결과 enum입니다 — WPF의 `MessageBoxResult` / WinForms의 `DialogResult`와 같은 개념을 크로스플랫폼 샘플 코드를 위해 통일했습니다.

---

## 구현 특이사항

- 모든 컨트롤은 별도의 `Handler`/렌더러 없이 `BindableProperty`를 사용하는 일반 `ContentView` 파생 클래스입니다.
- 팝업은 별도의 OS 창이 아니라 모달 `ContentPage`이며, 결과는 `TaskCompletionSource<DreamineDialogResult>`로 전달됩니다.
- `DreamineCheckLed.Corner`는 LED 자신의 `HorizontalOptions`/`VerticalOptions`만 설정합니다 — 모서리 배치가 실제로 보이게 하려면 고정 크기 컨테이너(테두리가 있는 `Border`/`Grid` 등)로 감싸야 합니다.

---

## 크로스플랫폼 노트

`Dreamine.UI.Maui`는 `Dreamine.UI.Wpf.Controls` / `Dreamine.UI.WinForms`의 개념을 의도적으로 미러링하여 ViewModel이 이식 가능합니다.

```csharp
// 공유 ViewModel
public class LoginViewModel : INotifyPropertyChanged
{
    public string UserName { get; set; }
    public bool   RememberMe { get; set; }
}

// MAUI — 데이터 바인딩
<dc:DreamineCheckBox Text="로그인 유지" IsChecked="{Binding RememberMe}" />
```

---

## 라이선스

MIT License
