<!--!
\file README_KO.md
\brief Dreamine.UI.Blazor - WPF/WinForms/MAUI API 호환 다크 테마 Blazor 커스텀 컴포넌트 라이브러리
\author Dreamine Core Team
\date 2026-06-29
\version 1.0.0
-->

# Dreamine.UI.Blazor

**Dreamine.UI.Blazor**는 `Dreamine.UI.Wpf.Controls`/`Dreamine.UI.WinForms`/`Dreamine.UI.Maui`와 동일한 개념의 API를 Blazor 환경에서 제공하는 다크 테마 커스텀 컴포넌트 및 팝업 다이얼로그 서비스 라이브러리입니다.

동일한 개념(`IsOn`, `IsExpanded`, `DialogResult` 스타일의 팝업 결과 등)을 사용하므로, 하나의 ViewModel을 WPF·WinForms·MAUI·Blazor 전체에서 코드 중복 없이 재사용할 수 있습니다.

[➡️ English Documentation](./README.md)

---

## 이 라이브러리가 해결하는 문제

순수 HTML/Blazor에는 다른 Dreamine 플랫폼들이 가진 상태 LED, Expander, 앱 스타일의 메시지박스/점멸 알람 팝업과 대응하는 요소가 없습니다.

- 네이티브 상태 LED나 접기/펼치기("Expander") 요소가 없습니다.
- Blazor는 WPF/WinForms처럼 팝업용 별도 OS 창도 없고, MAUI처럼 모달 페이지 내비게이션도 없습니다 — `DreamineDialogService` + `DreamineDialogHost`가 하나의 공유 오버레이 컴포넌트와 요청/응답 패턴으로 이 공백을 채웁니다.
- 터치 기기는 자동으로 네이티브 화면 키보드를 띄워주지만, **데스크톱 브라우저는 그렇지 않습니다** — 텍스트 입력창을 클릭해도 아무것도 뜨지 않는데, 이는 MAUI Windows 데스크톱 모드와 똑같은 공백입니다. `DreamineVirtualKeyboard`가 WinForms/MAUI와 동일한 화면 QWERTY 데모를 제공합니다.

---

## 주요 기능

- **DreamineCheckLed** — On/Off, CSS 애니메이션 펄스, 가변 지름, `position: relative` 컨테이너 위에 배지처럼 얹을 수 있는 `Corner` 앵커(`TopLeft`/`TopRight`/`BottomLeft`/`BottomRight`)
- **DreamineExpander** — 헤더 화살표 토글, `IsExpanded` 양방향 바인딩이 있는 접기/펼치기 패널
- **DreamineVirtualKeyboard** — 데스크톱 브라우저 데모용 화면 QWERTY 키보드(숫자, 기호, Shift, Enter), `Value`/`ValueChanged`로 임의의 `string`에 양방향 바인딩
- **DreamineDialogService** / **DreamineDialogHost** — Scoped 서비스 + (레이아웃에 한 번만 배치하는) 호스트 컴포넌트 조합으로 `ShowMessageBoxAsync`/`ShowOkCancelAsync`/`ShowYesNoAsync`/`ShowBlinkAsync`를 제공하며, 모두 `Task<DreamineDialogResult>`를 반환합니다

---

## 요구 사항

- **대상 프레임워크**: `net8.0`
- **의존 패키지**:
  - `Microsoft.AspNetCore.Components.Web`

---

## 설치

### NuGet

```bash
dotnet add package Dreamine.UI.Blazor
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Blazor" />
```

### 연결 방법

```csharp
// Program.cs
builder.Services.AddScoped<DreamineDialogService>();
```

```razor
@* MainLayout.razor — 레이아웃 어디든 한 번만 배치 *@
<DreamineDialogHost />
```

```razor
@* _Imports.razor *@
@using Dreamine.UI.Blazor
```

```html
<!-- App.razor <head> — 라이브러리의 컴포넌트 스코프 CSS가 로드되려면 필수 -->
<link rel="stylesheet" href="{앱 어셈블리 이름}.styles.css" />
```

---

## 프로젝트 구조

```text
Dreamine.UI.Blazor
├── DreamineCheckLed.razor(.css)
├── DreamineCheckLedCorner.cs
├── DreamineExpander.razor(.css)
├── DreamineVirtualKeyboard.razor(.css)
├── DreamineDialogService.cs
├── DreamineDialogHost.razor(.css)
├── DreamineDialogResult.cs
└── BlinkPopupOptions.cs
```

---

## 아키텍처 역할

```text
Microsoft.AspNetCore.Components.Web
        │
Dreamine.UI.Blazor       ← 이 패키지
        │
SampleCrossUi.Blazor
애플리케이션 코드
```

---

## 빠른 시작

```razor
<DreamineCheckLed IsOn="@isRunning" IsPulse="@isAlarming" />

<DreamineExpander Header="상세 옵션" @bind-IsExpanded="showAdvanced">
    <p>펼쳤을 때만 보이는 내용입니다.</p>
</DreamineExpander>

<input @bind="text" @bind:event="oninput" @onfocus="() => showKeyboard = true" />
<DreamineVirtualKeyboard IsVisible="showKeyboard" Value="@text" ValueChanged="v => text = v"
                         OnEnter="() => showKeyboard = false" />
```

```csharp
@inject DreamineDialogService DialogService

var result = await DialogService.ShowMessageBoxAsync(
    "작업이 성공적으로 완료되었습니다.", "완료", autoClickDelaySeconds: 5);

var alarmResult = await DialogService.ShowBlinkAsync(new BlinkPopupOptions
{
    Title = "⚠ ALARM",
    Message = "설비 이상이 감지되었습니다.\n운영자 확인이 필요합니다.",
    UseBlink = true,
    Color1 = "#B41E1E",
    Color2 = "#500A0A",
    ForegroundColor = "#FFD700",
    OkText = "확인",
    CancelText = "취소"
});
```

---

## 컴포넌트 참조

### DreamineCheckLed

| 파라미터 | 타입 | 설명 |
|---|---|---|
| `IsOn` | `bool` | LED 켜짐/꺼짐 |
| `IsPulse` | `bool` | `IsOn`이 `true`일 때 CSS `@keyframes` 페이드 애니메이션 활성화 |
| `Diameter` | `double` | LED 원의 지름(px, 기본값 `24`) |
| `Corner` | `DreamineCheckLedCorner?` | 지정하면 부모(`position: relative`여야 함)의 특정 모서리에 절대 위치로 배치 — 배지 스타일 배치에 사용 |

### DreamineExpander

| 파라미터 | 타입 | 설명 |
|---|---|---|
| `Header` | `string` | 헤더 텍스트 |
| `IsExpanded` | `bool` | 펼침/접힘 상태 — `@bind-IsExpanded` 지원 |
| `ChildContent` | `RenderFragment?` | 펼쳤을 때 보이는 내부 콘텐츠 |

### DreamineVirtualKeyboard

| 파라미터 | 타입 | 설명 |
|---|---|---|
| `IsVisible` | `bool` | 키보드 표시/숨김 |
| `Value` / `ValueChanged` | `string` | 양방향 바인딩되는 텍스트 — 키는 끝에 추가, Backspace는 끝 글자 삭제(JS 인터롭이 없어서 커서 위치 추적은 안 함) |
| `OnEnter` | `EventCallback` | Enter 탭 시 발생 — 보통 키보드를 숨기는 데 사용 |

한 번 탭하면 다음 키 하나만 대문자/기호로 바뀌고 자동으로 해제되며, **더블탭**하면 Caps Lock으로 고정됩니다(다시 탭하면 해제) — JS 인터롭 없이 서버 왕복 사이의 `DateTime.Now` 차이만으로 더블탭을 판별합니다.

### DreamineDialogService

```csharp
Task<DreamineDialogResult> ShowMessageBoxAsync(string message, string title = "Information", int autoClickDelaySeconds = 0);
Task<DreamineDialogResult> ShowOkCancelAsync(string message, string title = "Confirm");
Task<DreamineDialogResult> ShowYesNoAsync(string message, string title = "Question");
Task<DreamineDialogResult> ShowBlinkAsync(BlinkPopupOptions options);
```

이 서비스는 한 번에 최대 하나의 `Current` 요청만 들고 있다가 바뀌면 `RequestChanged`를 발생시키고, `DreamineDialogHost`가 버튼 클릭이나 자동닫힘 타이머에서 `Complete(result)`를 호출하면 대기 중인 `Task`가 완료됩니다.

### DreamineDialogResult

```csharp
public enum DreamineDialogResult { None, OK, Cancel, Yes, No }
```

MAUI 라이브러리와 동일한 플랫폼 독립적 결과 enum입니다 — WPF의 `MessageBoxResult` / WinForms의 `DialogResult`와 같은 개념입니다.

---

## 구현 특이사항

- `DreamineDialogHost`는 점멸 애니메이션과 1초마다 갱신되는 자동닫힘 카운트다운 모두에 `System.Threading.Timer`를 사용합니다. 타이머 콜백은 Blazor 렌더링 동기화 컨텍스트 밖에서 실행되므로, 상태를 바꿀 때마다 `InvokeAsync(StateHasChanged)`를 호출합니다.
- 한 번에 하나의 팝업만 활성화될 수 있습니다 — 하나가 열려 있는 동안 다른 `Show*Async`를 호출하면 기존 요청이 교체되고, 이전 호출의 `Task`는 완료되지 않은 채로 남습니다(다른 플랫폼들과 마찬가지로 "한 번에 모달 하나"를 그대로 따른 것입니다).
- 컴포넌트 스코프 CSS(`*.razor.css`)는 빌드 시 `Dreamine.UI.Blazor.styles.css`로 묶여서, 사용하는 앱이 자기 자신의 `{어셈블리 이름}.styles.css`를 `App.razor`에서 참조하고 있어야 자동으로 가져와집니다 — 대부분의 최소 Blazor 템플릿은 기본적으로 이 줄이 빠져 있습니다.

---

## 크로스플랫폼 노트

`Dreamine.UI.Blazor`는 `Dreamine.UI.Wpf.Controls` / `Dreamine.UI.WinForms` / `Dreamine.UI.Maui`의 개념을 의도적으로 미러링하여 ViewModel이 이식 가능합니다.

```csharp
// 공유 ViewModel
public class EquipmentViewModel : INotifyPropertyChanged
{
    public bool IsRunning { get; set; }
}
```

```razor
<!-- Blazor -->
<DreamineCheckLed IsOn="@ViewModel.IsRunning" />
```

---

## 라이선스

MIT License
