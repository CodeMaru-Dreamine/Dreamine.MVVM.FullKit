<!--!
\file README.md
\brief Dreamine.UI.Maui - Dark-theme custom .NET MAUI control library with WPF/WinForms API parity.
\author Dreamine Core Team
\date 2026-06-29
\version 1.0.0
-->

# Dreamine.UI.Maui

**Dreamine.UI.Maui** provides dark-theme custom .NET MAUI controls and popup overlays that mirror the API surface of `Dreamine.UI.Wpf.Controls` and `Dreamine.UI.WinForms`.

By using identical concepts (`IsChecked`, `IsOn`, `IsExpanded`, `DialogResult`-style popup results, etc.), the same ViewModel can be reused across WPF, WinForms, and MAUI without duplication.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

.NET MAUI is missing several controls that WPF/WinForms apps in the Dreamine ecosystem rely on:

- There is no built-in status LED, expander, or app-styled message box/popup control.
- The native `CheckBox` on Windows (WinUI) reserves a large minimum hit-box, making it impossible to lay out tightly next to a label — `DreamineCheckBox` is a fully custom-drawn replacement.
- Borderless popup "windows" don't exist on MAUI the way they do on WPF/WinForms — popups are implemented as modal `ContentPage`s with a translucent backdrop instead.
- There's no on-screen keyboard for desktop-mode MAUI (Windows) where the OS doesn't show one automatically — `DreamineVirtualKeyboard` fills that gap for samples/demos.

---

## Key Features

- **DreamineCheckBox** — custom-drawn checkbox (`Border` + checkmark glyph), tapping the label also toggles it
- **DreamineCheckLed** — status LED with on/off, pulse (fade) animation, variable diameter, and `Corner` anchoring (`TopLeft`/`TopRight`/`BottomLeft`/`BottomRight`) for badge-style placement
- **DreamineExpander** — collapsible panel with header arrow toggle and `ExpandedChanged` event
- **DreamineVirtualKeyboard** — on-screen QWERTY keyboard (numbers, symbols, Shift/Caps-lock, Enter) for desktop-mode MAUI demos; attaches to any `Entry`
- **DreamineMessageBox** / **DreamineBlinkPopup** (`Dreamine.UI.Maui.Popup`) — modal overlay popups with OK/Cancel/Yes/No results, auto-close countdown, and a blinking-background alarm popup

---

## Requirements

- **Target Framework**: `net9.0-windows10.0.19041.0` (MAUI Windows)
- **Dependencies**:
  - `Microsoft.Maui.Controls`

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Maui
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Maui" />
```

---

## Project Structure

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

## Architecture Role

```text
Microsoft.Maui.Controls
        │
Dreamine.UI.Maui        ← this package
        │
SampleCrossUi.Maui
Application Code
```

---

## Quick Start

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:dc="clr-namespace:Dreamine.UI.Maui;assembly=Dreamine.UI.Maui">

    <VerticalStackLayout Spacing="12">

        <dc:DreamineCheckBox Text="I agree" IsChecked="{Binding RememberMe}" />

        <dc:DreamineCheckLed IsOn="{Binding LedIsOn}" IsPulse="{Binding LedIsPulse}" />

        <dc:DreamineExpander Header="Advanced Options" IsExpanded="False">
            <dc:DreamineExpander.ExpanderContent>
                <Label Text="Hidden content goes here." />
            </dc:DreamineExpander.ExpanderContent>
        </dc:DreamineExpander>

    </VerticalStackLayout>
</ContentPage>
```

```csharp
using Dreamine.UI.Maui.Popup;

var result = await DreamineMessageBox.ShowAsync(
    "The operation completed successfully.",
    "Done",
    autoClickDelaySeconds: 5);

var alarmResult = await DreamineBlinkPopup.ShowAsync(new BlinkPopupOptions
{
    Title = "⚠ ALARM",
    Message = "Equipment fault detected.\nOperator confirmation required.",
    UseBlink = true,
    Color1 = Color.FromArgb("#B41E1E"),
    Color2 = Color.FromArgb("#500A0A"),
    ForegroundColor = Colors.Yellow,
    OkText = "Confirm",
    CancelText = "Cancel"
});
```

---

## Controls Reference

### DreamineCheckBox

| Property / Event | Type | Description |
|---|---|---|
| `IsChecked` | `bool` (`TwoWay`) | Checked state |
| `Text` | `string` | Label text — tapping it also toggles the checkbox |
| `CheckedChanged` | `EventHandler<bool>` | Fires on state change |

> Built from scratch because the native WinUI `CheckBox` reserves a large minimum width that can't be reduced via `WidthRequest`, leaving an unwanted gap before the label.

---

### DreamineCheckLed

| Property / Event | Type | Description |
|---|---|---|
| `IsOn` | `bool` | LED on/off |
| `IsPulse` | `bool` | Enables continuous fade-in/out animation while `IsOn` is `true` |
| `Diameter` | `double` | LED circle diameter (default `24`) |
| `Corner` | `DreamineCheckLedCorner` | Anchors the LED to a corner of its parent: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` — used for badge-style placement on top of another control |

> Animations are deferred until the control has a `Handler` (i.e. is actually attached to the visual tree) — calling `Animation`/`AbortAnimation` before that throws `ArgumentException: Unable to find IAnimationManager`.

---

### DreamineExpander

| Property / Event | Type | Description |
|---|---|---|
| `Header` | `string` | Header text |
| `IsExpanded` | `bool` (`TwoWay`) | Expanded / collapsed state, toggled by tapping the header |
| `ExpanderContent` | `View` | Inner content shown when expanded (distinct from `ContentView.Content`, which is the control's own root layout) |
| `ExpandedChanged` | `EventHandler` | Fires when expanded state changes |

---

### DreamineVirtualKeyboard

| Member | Description |
|---|---|
| `Attach(Entry entry)` | Shows the keyboard when `entry` gains focus; inserts/removes characters directly into `entry.Text` |

- Full QWERTY layout with a US-keyboard symbol set: number row (`!@#$%^&*()_+`), brackets (`[]{}`), punctuation (`;':"  ,.<>/?`).
- **Shift**: single tap capitalizes the next letter only (auto-resets); **double tap** toggles Caps Lock (stays on until tapped again). All shiftable keys — not just letters — update their displayed glyph immediately.
- **Enter**: unfocuses the target `Entry` and hides the keyboard (there is no separate "Close" key — `Entry` is single-line, so Enter doubles as "done").
- Does **not** hide on the target `Entry`'s `Unfocused` event — tapping a key button shifts focus away from the `Entry` momentarily, and hiding at that exact moment would cancel the in-flight `Clicked` handler before the character gets inserted.

---

### DreamineMessageBox (`Dreamine.UI.Maui.Popup`)

```csharp
Task<DreamineDialogResult> ShowAsync(string message, string title = "Information", int autoClickDelaySeconds = 0);
Task<DreamineDialogResult> ShowOkCancelAsync(string message, string title = "Confirm");
Task<DreamineDialogResult> ShowYesNoAsync(string message, string title = "Question");
```

- Implemented as a modal `ContentPage` (`DreamineMessageBoxPage`) pushed via `Navigation.PushModalAsync`, with a translucent backdrop simulating a popup — MAUI has no lightweight way to spin up an independent OS window the way WPF/WinForms can.
- `autoClickDelaySeconds` shows a countdown in the message and auto-completes with `OK` when it reaches zero.

### DreamineBlinkPopup (`Dreamine.UI.Maui.Popup`)

```csharp
Task<DreamineDialogResult> ShowAsync(BlinkPopupOptions options);
```

| `BlinkPopupOptions` Property | Type | Description |
|---|---|---|
| `Title` / `Message` | `string?` | Popup text |
| `OkText` / `CancelText` | `string?` | Button labels — omit one to hide that button |
| `UseBlink` | `bool` | Alternates the background between `Color1`/`Color2` on a timer |
| `Color1` / `Color2` | `Color` | Blink colors |
| `ForegroundColor` | `Color` | Title/message text color |
| `BlinkIntervalMs` | `int` | Blink interval, default `600` |

### DreamineDialogResult

```csharp
public enum DreamineDialogResult { None, OK, Cancel, Yes, No }
```

A single, platform-agnostic result enum used by both popup APIs — same idea as WPF's `MessageBoxResult` / WinForms' `DialogResult`, unified for cross-platform sample code.

---

## Implementation Notes

- All controls are plain `ContentView` subclasses using `BindableProperty` — no custom `Handler`/renderer is required.
- Popups are modal `ContentPage`s, not separate OS windows; results flow back via `TaskCompletionSource<DreamineDialogResult>`.
- `DreamineCheckLed.Corner` only sets `HorizontalOptions`/`VerticalOptions` on the LED itself — wrap it in a fixed-size container (e.g. a bordered `Border`/`Grid`) for the corner anchoring to be visible.

---

## Cross-Platform Note

`Dreamine.UI.Maui` intentionally mirrors the concepts in `Dreamine.UI.Wpf.Controls` / `Dreamine.UI.WinForms` so ViewModels stay portable:

```csharp
// Shared ViewModel
public class LoginViewModel : INotifyPropertyChanged
{
    public string UserName { get; set; }
    public bool   RememberMe { get; set; }
}

// MAUI — data binding
<dc:DreamineCheckBox Text="Remember Me" IsChecked="{Binding RememberMe}" />
```

---

## License

MIT License
