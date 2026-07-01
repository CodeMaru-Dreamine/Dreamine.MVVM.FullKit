<!--!
\file README.md
\brief Dreamine.UI.Wpf.Controls - Custom WPF control library for the Dreamine UI framework.
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf.Controls

**Dreamine.UI.Wpf.Controls** provides the core set of custom WPF controls used across Dreamine-based applications.

It includes interactive controls, navigation infrastructure, and view-switching utilities — all integrated with the Dreamine MVVM pattern.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

WPF applications built on Dreamine MVVM need:

- Styled controls that extend standard WPF controls with project-specific capabilities
- A navigation bar that manages views, global input hooks, and auto-logout timers
- A view-switcher that decouples view loading from ViewModel lifecycle
- Message box and check-selector dialogs that integrate with the Dreamine DI container

---

## Key Features

- **DreamineButton** — icon + shadow + grade-based access control, attached command support
- **DreamineNavigationBar** — tab-based navigation bar with SharpHook global keyboard hook and idle-logout timer
- **ViewLoader** — resolves and instantiates WPF views and ViewModels through `DMContainer`
- **ViewSwitcher** — notifies `IActivatable` / `IVisibilityAware` ViewModels on show/hide transitions
- **DreamineMessageBox** — themed async message box with auto-dismiss timer
- **DreamineCheckSelector** — multi-item check list selector
- Full suite of styled counterparts: CheckBox, CheckLed, ComboBox, DataGrid, Expander, Image, Label, ListBox, PasswordBox, RadioButton, TabControl, TextBlock, TextBox, TimeSpinner

---

## Requirements

- **Target Framework**: `net8.0-windows`
- **Dependencies**:
  - `Dreamine.UI.Wpf`
  - `Dreamine.MVVM.ViewModels`
  - `Dreamine.MVVM.Interfaces`
  - `Microsoft.Xaml.Behaviors.Wpf`
  - `SharpHook` 5.3.8+

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf.Controls
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf.Controls" />
```

---

## Project Structure

```text
Dreamine.UI.Wpf.Controls
├── CheckSelector/
│   └── DreamineCheckSelector.xaml(.cs)
├── Controls/
│   ├── DreamineButton.cs
│   ├── DreamineCheckBox.cs
│   ├── DreamineCheckLed.cs
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

## Architecture Role

```text
Dreamine.UI.Wpf
        │
Dreamine.UI.Wpf.Controls     ← this package
        │
Dreamine.UI.Wpf.Equipment
Dreamine.UI.Wpf.Themes
Application Code
```

---

## Quick Start

### DreamineButton

```xml
xmlns:ctrl="clr-namespace:Dreamine.UI.Wpf.Controls;assembly=Dreamine.UI.Wpf.Controls"

<ctrl:DreamineButton Content="Save"
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
// Register views for navigation
ViewLoader.Register("MainView", typeof(MainView), typeof(MainViewModel));
```

### ViewSwitcher — ViewModel lifecycle

```csharp
// In your ViewModel, implement IActivatable or IVisibilityAware
public class DashboardViewModel : ViewModelBase, IActivatable
{
    public void Activate()   { /* start polling */ }
    public void Deactivate() { /* stop polling  */ }
}

// Register at startup
ViewSwitcher.RegisterViewModel("DashboardView", dashboardVm);
```

### DreamineMessageBox

```csharp
var result = await DreamineMessageBox.ShowAsync(
    "Discard changes?",
    "Confirm",
    autoClick: MessageBoxResult.Cancel,
    autoClickDelaySeconds: 10);
```

---

## Enum Reference

| Enum | Values | Used By |
|---|---|---|
| `SelectedVisualMode` | `BorderOnly`, `BackgroundOnly`, `Both` | `DreamineButton` |
| `IconPosition` | `Left`, `Right`, `Top`, `Bottom`, `Full` | `DreamineButton` |
| `ExpanderArrowPlacement` | `Left`, `Right` | `DreamineExpander` |
| `NavigationBarPosition` | `Left`, `Right`, `Top`, `Bottom` | `DreamineNavigationBar` |

---

## Design Notes

- All controls auto-merge their `ResourceDictionary` from `Dreamine.UI.Wpf.Themes` at construction time
- `ViewSwitcher` uses a local dictionary — ViewModels must be registered via `RegisterViewModel` before navigation begins
- `DreamineNavigationBar` uses `SharpHook.TaskPoolGlobalHook` for process-wide keyboard monitoring; the hook is shared across all instances
- Grade / MinimumGrade access control is evaluated at click time; no login system is required

---

## License

MIT License
