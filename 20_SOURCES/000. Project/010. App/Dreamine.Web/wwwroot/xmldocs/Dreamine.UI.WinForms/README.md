<!--!
\file README.md
\brief Dreamine.UI.WinForms - Dark-theme custom WinForms control library with WPF API parity.
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.WinForms

**Dreamine.UI.WinForms** provides a full set of dark-theme custom WinForms controls that mirror the API surface of `Dreamine.UI.Wpf.Controls`.

By using identical property names (`Content`, `IsChecked`, `IsExpanded`, etc.), a single ViewModel can be reused across WPF and WinForms without duplication.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

WinForms applications in the Dreamine ecosystem need:

- Owner-draw controls styled to match the Dreamine WPF dark theme
- The same property names as `Dreamine.UI.Wpf.Controls` so ViewModels are platform-agnostic
- Flicker-free rendering via `SetStyle(UserPaint | AllPaintingInWmPaint | DoubleBuffer, true)`
- Win32 placeholder text (`EM_SETCUEBANNER`) and rounded-rectangle GDI+ helpers

---

## Key Features

- **DreamineButton** — gradient fill, shine overlay, hover/press states, `ICommand` support, `IsSelected` underline
- **DreamineCheckBox** — owner-draw checkbox with `IsChecked` / `CheckedChanged`
- **DreamineRadioButton** — `GroupName`-based mutual exclusion scanned from `Parent.Controls`
- **DreamineCheckLed** — status LED with corner placement, optional pulse (fade) animation
- **DreamineTextBox** — `UserControl` wrapper with Win32 hint, focus-border highlight
- **DreaminePasswordBox** — password `UserControl` wrapper with hint and focus-border
- **DreamineComboBox** — owner-draw dropdown with full dark-theme item rendering
- **DreamineTabControl** — dark header bar, `AccentBlue` underline for the selected tab
- **DreamineExpander** — collapsible panel with header arrow toggle and `ExpandedChanged` event
- **DreamineTheme** — static dark-palette constants matching the WPF theme exactly
- **DreamineDrawHelper** — GDI+ helpers: rounded path, gradient fill, shine overlay, centered text, color blend

---

## Requirements

- **Target Framework**: `net8.0-windows`
- **Dependencies**:
  - `Dreamine.MVVM.Interfaces` (for `ICommand` support)

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.WinForms
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.WinForms" />
```

---

## Project Structure

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

## Architecture Role

```text
Dreamine.MVVM.Interfaces
        │
Dreamine.UI.WinForms     ← this package
        │
SampleCrossUi.WinForms
Application Code
```

---

## Quick Start

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
            Content = "Confirm",
            Location = new Point(20, 20),
            Size = new Size(120, 40),
        };
        button.Click += (_, _) => MessageBox.Show("Clicked");
        Controls.Add(button);

        var textBox = new DreamineTextBox
        {
            Hint = "Enter your name",
            Location = new Point(20, 80),
            Size = new Size(200, 36),
        };
        Controls.Add(textBox);

        var checkBox = new DreamineCheckBox
        {
            Content = "I agree",
            Location = new Point(20, 130),
            Size = new Size(160, 28),
        };
        checkBox.CheckedChanged += (_, _) => Console.WriteLine(checkBox.IsChecked);
        Controls.Add(checkBox);

        var expander = new DreamineExpander
        {
            Header = "Advanced Options",
            IsExpanded = false,
            Location = new Point(20, 170),
            Size = new Size(300, 200),
        };
        Controls.Add(expander);
    }
}
```

---

## Controls Reference

### DreamineButton

| Property / Event | Type | Description |
|---|---|---|
| `Content` | `string` | Button label text |
| `ShineColor` | `Color` | Top shine overlay color |
| `CornerRadius` | `int` | Corner radius |
| `IsSelected` | `bool` | Selected state — draws `AccentBlue` bottom underline |
| `Command` | `ICommand` | Executed on click |
| `CommandParameter` | `object?` | Parameter forwarded to the command |

Hover brightens the gradient; press darkens it.

---

### DreamineCheckBox

| Property / Event | Type | Description |
|---|---|---|
| `Content` | `string` | Label text |
| `IsChecked` | `bool` | Checked state |
| `CheckedChanged` | `EventHandler` | Fires on state change |

---

### DreamineRadioButton

| Property / Event | Type | Description |
|---|---|---|
| `Content` | `string` | Label text |
| `IsChecked` | `bool` | Selected state |
| `GroupName` | `string` | Mutual-exclusion group (scanned within `Parent.Controls`) |
| `CheckedChanged` | `EventHandler` | Fires on state change |

---

### DreamineCheckLed

| Property / Event | Type | Description |
|---|---|---|
| `IsOn` | `bool` | LED on/off |
| `IsPulse` | `bool` | Enables continuous fade-in/out animation |
| `Corner` | `LedCorner` | Placement: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` |
| `Diameter` | `float` | LED circle diameter |

---

### DreamineTextBox

| Property / Event | Type | Description |
|---|---|---|
| `Text` | `string` | Input text |
| `Hint` | `string` | Placeholder (Win32 `EM_SETCUEBANNER`) |
| `IsReadOnly` | `bool` | Read-only mode |
| `TextChanged` | `EventHandler` | Fires when text changes |

---

### DreaminePasswordBox

| Property / Event | Type | Description |
|---|---|---|
| `Password` | `string` | Entered password |
| `Hint` | `string` | Placeholder text |
| `PasswordChanged` | `EventHandler` | Fires when password changes |

---

### DreamineComboBox

| Property / Event | Type | Description |
|---|---|---|
| `Items` | `ObjectCollection` | Item collection |
| `SelectedItem` | `object?` | Currently selected item |
| `SelectedIndex` | `int` | Index of selected item |
| `SelectedIndexChanged` | `EventHandler` | Fires when selection changes |

Owner-draw rendering applies the dark theme to the dropdown list.

---

### DreamineTabControl

- Dark header bar via owner-draw.
- Selected tab shows an `AccentBlue` bottom underline.
- Tab headers highlight on hover.

---

### DreamineExpander

| Property / Event | Type | Description |
|---|---|---|
| `Header` | `string` | Header text |
| `IsExpanded` | `bool` | Expanded / collapsed state |
| `Content` | `Panel` | Inner content panel |
| `ExpandedChanged` | `EventHandler` | Fires when expanded state changes |

---

## DreamineTheme Color Constants

```csharp
// Backgrounds
DreamineTheme.AppBackground    // #1A1A2E  — form / window
DreamineTheme.CardBackground   // #0F1E3A  — panels / cards
DreamineTheme.InputBackground  // #162040  — text input fields

// Accent
DreamineTheme.AccentBlue       // #1E90FF

// Borders
DreamineTheme.BorderNormal     // #2D4A6E
DreamineTheme.BorderFocus      // #1E90FF

// Text
DreamineTheme.TextPrimary      // White
DreamineTheme.TextSecondary    // #8899AA

// LED
DreamineTheme.LedOnOuter       // #1FD36B
```

All values are matched exactly to the WPF dark-theme palette.

---

## DreamineDrawHelper

Static helper class for owner-draw rendering.

| Method | Description |
|---|---|
| `RoundedRect(RectangleF, float)` | Returns a rounded-corner `GraphicsPath` |
| `FillRoundedRect(Graphics, Brush, Pen, Rectangle, float)` | Fills a rounded rectangle with optional border |
| `FillRoundedGradient(Graphics, Color, Color, Pen?, Rectangle, float)` | Vertical gradient fill in a rounded rectangle |
| `DrawShineOverlay(Graphics, Color, Rectangle, float)` | Draws a translucent shine at the top half |
| `DrawCenteredText(Graphics, string, Font, Color, Rectangle)` | Renders text centered in a rectangle |
| `Blend(Color, Color, float)` | Linear alpha-blend between two colors |

---

## Implementation Notes

- All custom controls use `SetStyle(UserPaint | AllPaintingInWmPaint | DoubleBuffer | SupportsTransparentBackColor, true)` for flicker-free rendering.
- `DreamineTextBox`, `DreaminePasswordBox`, and `DreamineComboBox` wrap native controls inside `UserControl` to enable direct border painting.
- `DreamineRadioButton` scans `Parent.Controls` at check time to uncheck siblings in the same `GroupName`.
- `DreamineCheckLed` pulse animation runs on a `System.Windows.Forms.Timer`; the timer is stopped and disposed with the control.

---

## Cross-Platform Note

`Dreamine.UI.WinForms` intentionally mirrors `Dreamine.UI.Wpf.Controls` property names so ViewModels are portable:

```csharp
// Shared ViewModel
public class LoginViewModel : INotifyPropertyChanged
{
    public string UserName { get; set; }
    public bool   RememberMe { get; set; }
    public ICommand LoginCommand { get; set; }
}

// WPF — data binding
<dreamine:DreamineTextBox  Text="{Binding UserName}" />
<dreamine:DreamineCheckBox Content="Remember Me" IsChecked="{Binding RememberMe}" />
<dreamine:DreamineButton   Content="Login" Command="{Binding LoginCommand}" />

// WinForms — same ViewModel, no binding infrastructure needed
textBox.Text          = vm.UserName;
checkBox.IsChecked    = vm.RememberMe;
button.Command        = vm.LoginCommand;
```

---

## License

MIT License
