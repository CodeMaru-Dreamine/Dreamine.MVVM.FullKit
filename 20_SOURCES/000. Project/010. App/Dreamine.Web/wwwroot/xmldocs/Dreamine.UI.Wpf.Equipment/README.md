<!--!
\file README.md
\brief Dreamine.UI.Wpf.Equipment - Equipment-grade WPF components: virtual keyboard, blink popup, and alarm configuration.
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf.Equipment

**Dreamine.UI.Wpf.Equipment** provides WPF components designed for industrial and equipment-grade applications.

It implements the abstractions defined in `Dreamine.UI.Abstractions` and delivers:

- A fully featured on-screen virtual keyboard
- A configurable blink popup window system
- A popup service registered with the Dreamine DI container

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

Industrial WPF applications running on touch panels or locked-down terminals require:

- An on-screen keyboard that integrates with WPF text input without hooking IME
- Popup windows that blink to attract operator attention
- A popup service abstraction so business code does not reference window types directly
- Multi-monitor aware keyboard positioning
- Enter-key action providers for validation before the keyboard dismisses

---

## Key Features

- **DreamineVirtualKeyboard** — full alphanumeric + numeric + decimal keyboard with language switching
- **DreamineVirtualKeyboardWindow** — floating window that positions itself relative to the focused input element using Win32 monitor API
- **DreamineVirtualKeyboardAssist** — attached property helper for zero-code XAML keyboard activation
- **DreamineBlinkPopupWindow** — color-alternating animated popup with `Alt+F4` / `SC_CLOSE` blocking
- **DreaminePopupService** — implements `IPopupService`; async modal and non-modal display
- **KeyboardLayoutSelectorConverter** — `IValueConverter` that selects the correct keyboard `DataTemplate` by layout

---

## Requirements

- **Target Framework**: `net8.0-windows`
- **Dependencies**:
  - `Dreamine.UI.Abstractions`
  - `Dreamine.UI.Wpf`
  - `Dreamine.UI.Wpf.Controls`
  - `Dreamine.MVVM.ViewModels`
  - `SharpHook` 5.3.8+

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf.Equipment
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf.Equipment" />
```

---

## Project Structure

```text
Dreamine.UI.Wpf.Equipment
├── Popup/
│   ├── DreamineBlinkPopupWindow.xaml(.cs)
│   ├── DreamineBlinkPopupWindowViewModel.cs
│   └── DreaminePopupService.cs
└── VirtualKeyboard/
    ├── DreamineEnterActionGroupProvider.cs
    ├── DreamineFullKeyboardLayout.xaml(.cs)
    ├── DreamineNumericKeyboardLayout.xaml(.cs)
    ├── DreamineVirtualKeyboard.cs
    ├── DreamineVirtualKeyboardAssist.cs
    ├── DreamineVirtualKeyboardUI.xaml(.cs)
    ├── DreamineVirtualKeyboardWindow.xaml(.cs)
    ├── DreamineVkbIconAdorner.cs
    ├── Key.cs
    ├── KeyboardLayoutSelectorConverter.cs
    └── ShiftWindowOntoScreenHelper.cs
```

---

## Architecture Role

```text
Dreamine.UI.Abstractions
        │
Dreamine.UI.Wpf.Controls
Dreamine.UI.Wpf
        │
Dreamine.UI.Wpf.Equipment    ← this package
        │
Application Code
```

---

## Quick Start

### Virtual keyboard — XAML attached property

Activate the virtual keyboard for any text input by setting the attached property:

```xml
xmlns:vk="clr-namespace:Dreamine.UI.Wpf.Equipment.DreamineVirtualKeyboard;assembly=Dreamine.UI.Wpf.Equipment"

<TextBox vk:DreamineVirtualKeyboardAssist.UseVirtualKeyBoard="True"
         vk:DreamineVirtualKeyboardAssist.Layout="Text" />

<TextBox vk:DreamineVirtualKeyboardAssist.UseVirtualKeyBoard="True"
         vk:DreamineVirtualKeyboardAssist.Layout="Numeric"
         vk:DreamineVirtualKeyboardAssist.Minimum="0"
         vk:DreamineVirtualKeyboardAssist.Maximum="9999" />
```

### Blink Popup — basic usage

```csharp
var svc = DMContainer.Resolve<IPopupService>();

await svc.ShowBlinkAsync(ownerWindow, new BlinkPopupOptions
{
    Title           = "ALARM",
    Message         = "Motor overload detected",
    UseBlink        = true,
    BlinkIntervalMs = 500,
    Color1          = Colors.OrangeRed,
    Color2          = Colors.DarkRed,
    OkText          = "Acknowledge",
    IsModal         = true
});
```

### Blink Popup — block Alt+F4

```csharp
var options = new BlinkPopupOptions
{
    Message     = "Operator must acknowledge",
    BlockAltF4  = true,
    OkText      = "OK"
};
```

### Enter-key validation provider

```csharp
public class RangeCheckProvider : IEnterActionProvider
{
    public DependencyObject? PlacementTarget { get; set; }

    public async Task<ActionResult> ExecuteAsync()
    {
        double val = double.Parse(myTextBox.Text);
        if (val < 0 || val > 100)
            return ActionResult.Rejected;
        return ActionResult.Accepted;
    }
}
```

```xml
<TextBox>
    <vk:DreamineVirtualKeyboardAssist.EnterActionProvider>
        <local:RangeCheckProvider />
    </vk:DreamineVirtualKeyboardAssist.EnterActionProvider>
</TextBox>
```

---

## Enum Reference

| Enum | Values | Used By |
|---|---|---|
| `VkLayout` | `Text`, `Password`, `Numeric`, `Decimal` | Virtual keyboard |
| `LanguageCode` | `en_US`, `ko_KR`, `zh_CN`, `vi_VN` | Keyboard language |
| `ActionResult` | `Accepted`, `Rejected` | Enter-key providers |
| `KeyboardInputMode` | `Text`, `Numeric`, `Password` | Input routing |

---

## Design Notes

- Multi-monitor positioning uses Win32 `MonitorFromPoint` / `GetMonitorInfo` via P/Invoke — no WinForms dependency
- The virtual keyboard window is a singleton per application; `DreamineVirtualKeyboardAssist` manages its show/hide lifecycle
- `DreamineBlinkPopupWindow` uses `WM_SYSCOMMAND / SC_CLOSE` interception via `HwndSource` to block system close when `BlockAltF4 = true`
- `DreaminePopupService` supports both synchronous `ShowBlink` and asynchronous `ShowBlinkAsync` with optional auto-close timeout and `CancellationToken`

---

## License

MIT License
