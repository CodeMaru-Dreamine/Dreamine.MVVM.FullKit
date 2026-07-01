<!--!
\file README.md
\brief Dreamine.UI.Blazor - Dark-theme custom Blazor component library with WPF/WinForms/MAUI API parity.
\author Dreamine Core Team
\date 2026-06-29
\version 1.0.0
-->

# Dreamine.UI.Blazor

**Dreamine.UI.Blazor** provides dark-theme custom Blazor components and a popup dialog service that mirror the API surface of `Dreamine.UI.Wpf.Controls`, `Dreamine.UI.WinForms`, and `Dreamine.UI.Maui`.

By using identical concepts (`IsOn`, `IsExpanded`, `DialogResult`-style popup results, etc.), the same ViewModel can be reused across WPF, WinForms, MAUI, and Blazor without duplication.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

Plain HTML/Blazor has no equivalent of the status LED, expander, or app-styled message box/blink-alarm popup that the other Dreamine platforms have:

- There's no native status LED or collapsible "expander" element.
- Blazor has no concept of a separate OS window for popups (unlike WPF/WinForms) and no built-in modal-page navigation (unlike MAUI) — `DreamineDialogService` + `DreamineDialogHost` fill that gap with a single shared overlay component and a request/response pattern.
- Touch devices show a native on-screen keyboard automatically, but **desktop browsers don't** — focusing a text input on desktop shows nothing, the same gap MAUI has in Windows desktop mode. `DreamineVirtualKeyboard` provides the same on-screen QWERTY demo for visual parity with WinForms/MAUI.

---

## Key Features

- **DreamineCheckLed** — status LED with on/off, CSS-animated pulse, variable diameter, and `Corner` anchoring (`TopLeft`/`TopRight`/`BottomLeft`/`BottomRight`) for badge-style placement over a `position: relative` container
- **DreamineExpander** — collapsible panel with header arrow toggle, two-way `IsExpanded` binding
- **DreamineVirtualKeyboard** — on-screen QWERTY keyboard (numbers, symbols, Shift, Enter) for desktop-browser demos; two-way binds to any `string` via `Value`/`ValueChanged`
- **DreamineDialogService** / **DreamineDialogHost** — a scoped service + a single host component (placed once in `MainLayout`) that together provide `ShowMessageBoxAsync`/`ShowOkCancelAsync`/`ShowYesNoAsync`/`ShowBlinkAsync`, all returning `Task<DreamineDialogResult>`

---

## Requirements

- **Target Framework**: `net8.0`
- **Dependencies**:
  - `Microsoft.AspNetCore.Components.Web`

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Blazor
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Blazor" />
```

### Wiring it up

```csharp
// Program.cs
builder.Services.AddScoped<DreamineDialogService>();
```

```razor
@* MainLayout.razor — place once, anywhere in the layout *@
<DreamineDialogHost />
```

```razor
@* _Imports.razor *@
@using Dreamine.UI.Blazor
```

```html
<!-- App.razor <head> — required for the library's scoped component CSS to load -->
<link rel="stylesheet" href="{YourAppAssemblyName}.styles.css" />
```

---

## Project Structure

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

## Architecture Role

```text
Microsoft.AspNetCore.Components.Web
        │
Dreamine.UI.Blazor       ← this package
        │
SampleCrossUi.Blazor
Application Code
```

---

## Quick Start

```razor
<DreamineCheckLed IsOn="@isRunning" IsPulse="@isAlarming" />

<DreamineExpander Header="Advanced Options" @bind-IsExpanded="showAdvanced">
    <p>Hidden content goes here.</p>
</DreamineExpander>

<input @bind="text" @bind:event="oninput" @onfocus="() => showKeyboard = true" />
<DreamineVirtualKeyboard IsVisible="showKeyboard" Value="@text" ValueChanged="v => text = v"
                         OnEnter="() => showKeyboard = false" />
```

```csharp
@inject DreamineDialogService DialogService

var result = await DialogService.ShowMessageBoxAsync(
    "The operation completed successfully.", "Done", autoClickDelaySeconds: 5);

var alarmResult = await DialogService.ShowBlinkAsync(new BlinkPopupOptions
{
    Title = "⚠ ALARM",
    Message = "Equipment fault detected.\nOperator confirmation required.",
    UseBlink = true,
    Color1 = "#B41E1E",
    Color2 = "#500A0A",
    ForegroundColor = "#FFD700",
    OkText = "Confirm",
    CancelText = "Cancel"
});
```

---

## Components Reference

### DreamineCheckLed

| Parameter | Type | Description |
|---|---|---|
| `IsOn` | `bool` | LED on/off |
| `IsPulse` | `bool` | Enables a CSS `@keyframes` fade animation while `IsOn` is `true` |
| `Diameter` | `double` | LED circle diameter in pixels (default `24`) |
| `Corner` | `DreamineCheckLedCorner?` | When set, absolutely positions the LED at a corner of its parent (which must be `position: relative`) — used for badge-style placement |

### DreamineExpander

| Parameter | Type | Description |
|---|---|---|
| `Header` | `string` | Header text |
| `IsExpanded` | `bool` | Expanded / collapsed state — supports `@bind-IsExpanded` |
| `ChildContent` | `RenderFragment?` | Inner content shown when expanded |

### DreamineVirtualKeyboard

| Parameter | Type | Description |
|---|---|---|
| `IsVisible` | `bool` | Show/hide the keyboard |
| `Value` / `ValueChanged` | `string` | Two-way bound text — keys append, Backspace removes the last character (no JS interop, so there's no cursor tracking) |
| `OnEnter` | `EventCallback` | Fires when Enter is tapped — typically used to hide the keyboard |

Single tap shifts only the next key, then resets automatically; **double tap** toggles Caps Lock (stays on until tapped again) — detected purely by comparing `DateTime.Now` between server round-trips, no JS interop needed.

### DreamineDialogService

```csharp
Task<DreamineDialogResult> ShowMessageBoxAsync(string message, string title = "Information", int autoClickDelaySeconds = 0);
Task<DreamineDialogResult> ShowOkCancelAsync(string message, string title = "Confirm");
Task<DreamineDialogResult> ShowYesNoAsync(string message, string title = "Question");
Task<DreamineDialogResult> ShowBlinkAsync(BlinkPopupOptions options);
```

The service holds at most one `Current` request at a time, raises `RequestChanged` when it changes, and resolves the pending `Task` when `DreamineDialogHost` calls `Complete(result)` from a button click or the auto-close timer.

### DreamineDialogResult

```csharp
public enum DreamineDialogResult { None, OK, Cancel, Yes, No }
```

The same platform-agnostic result enum used by the MAUI library — mirrors WPF's `MessageBoxResult` / WinForms' `DialogResult`.

---

## Implementation Notes

- `DreamineDialogHost` uses a `System.Threading.Timer` for both the blink animation and the once-per-second auto-close countdown; since timer callbacks run off the Blazor render sync context, every state mutation is followed by `InvokeAsync(StateHasChanged)`.
- Only one popup can be active at a time — calling another `Show*Async` while one is open replaces it; the previous caller's awaited `Task` is left uncompleted (this mirrors a single modal at a time, same as the other platforms).
- Component-scoped CSS (`*.razor.css`) is bundled by the build into `Dreamine.UI.Blazor.styles.css` and pulled into the consuming app's own bundled stylesheet automatically — but only if the consuming app's `App.razor` references its own `{AssemblyName}.styles.css`, which most minimal Blazor templates omit by default.

---

## Cross-Platform Note

`Dreamine.UI.Blazor` intentionally mirrors the concepts in `Dreamine.UI.Wpf.Controls` / `Dreamine.UI.WinForms` / `Dreamine.UI.Maui` so ViewModels stay portable:

```csharp
// Shared ViewModel
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

## License

MIT License
