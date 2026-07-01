# Dreamine.MVVM.Behaviors

Lightweight WPF behavior package for **MVVM-oriented input and focus handling**.

This library provides small, practical behaviors that reduce code-behind and keep UI interaction logic connected to `ICommand` and declarative XAML configuration.

[➡️ 한국어 문서 보기](README_KO.md)

---

## Overview

`Dreamine.MVVM.Behaviors` is a WPF module intended to simplify common UI interaction patterns in MVVM applications.

The current package focuses on two small but frequently used behaviors:

- Execute a ViewModel command when the user presses **Enter**
- Move focus automatically when a control is **loaded**

This module targets **WPF on .NET 8** and is designed to be used with the Dreamine MVVM ecosystem. The project file declares `net8.0-windows`, enables WPF, and packages the module as `Dreamine.MVVM.Behaviors` version `1.0.2`.

---

## What is included

### 1. `EnterKeyCommandBehavior`

Attached behavior for `UIElement`.

Purpose:
- Listen for `KeyDown`
- Detect `Key.Enter`
- Execute bound `ICommand`
- Mark the event as handled after execution

Typical usage:
- Login form submit
- Search box execution
- Confirm action from a text input

Behavior summary:
- Attached property: `Command`
- Command parameter: currently `null`
- Executes only when `CanExecute(null)` returns `true`

---

### 2. `FocusOnLoadedBehavior`

Attached behavior for `FrameworkElement`.

Purpose:
- Detect control load completion
- Automatically set keyboard focus to the target element

Typical usage:
- First input focus on login screens
- Search bar auto-focus
- Initial cursor placement in form-based screens

Behavior summary:
- Attached property: `IsEnabled`
- Focus is applied only when the element is:
  - `Focusable`
  - `IsEnabled == true`
  - `Visibility == Visible`

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.MVVM.Behaviors
```

### PackageReference

```xml
<PackageReference Include="Dreamine.MVVM.Behaviors" Version="1.0.2" />
```

---

## Requirements

- .NET: `net8.0-windows`
- WPF enabled
- Dreamine behavior base dependency:
  - `Dreamine.MVVM.Behaviors.Core`

---

## Project structure

```text
Dreamine.MVVM.Behaviors/
├─ Dreamine.MVVM.Behaviors.csproj
├─ MVVM/
│  ├─ EnterKeyCommandBehavior.cs
│  └─ FocusOnLoadedBehavior.cs
└─ README.md
```

---

## Quick Start

### EnterKeyCommandBehavior

```xml
<Window x:Class="Sample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mvvm="clr-namespace:Dreamine.MVVM.Behaviors.MVVM;assembly=Dreamine.MVVM.Behaviors">
    <Grid>
        <TextBox Width="240"
                 mvvm:EnterKeyCommandBehavior.Command="{Binding LoginCommand}" />
    </Grid>
</Window>
```

Example ViewModel:

```csharp
public sealed class LoginViewModel
{
    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(OnLogin);
    }

    private void OnLogin()
    {
        // Execute login logic
    }
}
```

---

### FocusOnLoadedBehavior

```xml
<Window x:Class="Sample.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mvvm="clr-namespace:Dreamine.MVVM.Behaviors.MVVM;assembly=Dreamine.MVVM.Behaviors">
    <Grid>
        <TextBox Width="240"
                 mvvm:FocusOnLoadedBehavior.IsEnabled="True" />
    </Grid>
</Window>
```

---

## Design intent

This package follows a simple direction:

- keep behaviors small
- keep XAML usage explicit
- avoid unnecessary abstraction
- support MVVM without pushing UI logic into code-behind

The package is suitable for reusable UI modules where repetitive interaction code needs to be standardized.

---

## Precision analysis notes

Based on the source package, the following points are confirmed:

1. The package contains **two actual behaviors only**.
2. The names shown in the old README do **not** match the real class names.
   - Incorrect: `MVVMEntryCommandBehavior`
   - Actual: `EnterKeyCommandBehavior`
   - Incorrect: `MVVMFocusOnLoadedBehavior`
   - Actual: `FocusOnLoadedBehavior`
3. The project file version is **1.0.2**, not `1.0.0`.
4. `FocusOnLoadedBehavior` uses a single attached-property based load path.

This means the README should describe the package based on the actual code, not the outdated naming from the embedded README.

---

## Limitations

Current implementation limitations:

- `EnterKeyCommandBehavior` does not pass a command parameter
- `EnterKeyCommandBehavior` listens to `KeyDown` only
- `FocusOnLoadedBehavior` applies focus only once during load through the attached property path
- More advanced scenarios such as event argument forwarding, delayed focus, or selector-based focus routing are not included yet

---

## Recommended future improvements

- Add `CommandParameter` support
- Add `Key` selection support instead of fixed Enter-only behavior
- Add delayed focus option using dispatcher
- Add `SelectAllOnFocus` behavior for text entry scenarios
- Add XML documentation consistency and README/package synchronization

---

## Repository metadata

- Package ID: `Dreamine.MVVM.Behaviors`
- Version: `1.0.2`
- License: `MIT`
- Repository: `https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors`

---

## License

See `LICENSE`.
