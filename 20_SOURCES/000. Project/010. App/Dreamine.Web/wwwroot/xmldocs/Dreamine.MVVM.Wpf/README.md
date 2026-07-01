# Dreamine.MVVM.Wpf

WPF-specific bootstrap and runtime integration layer for the Dreamine MVVM framework.

[➡️ 한국어 문서 보기](./README_KO.md)

## Overview

`Dreamine.MVVM.Wpf` contains the WPF-only startup and runtime wiring logic used by Dreamine MVVM applications.

This package is responsible for:

- View ↔ ViewModel registration
- automatic DI registration through `DMContainer`
- automatic `DataContext` attachment when a View is loaded
- optional region navigator registration for the currently loaded Window
- keeping WPF runtime concerns out of platform-neutral libraries

## Why this package exists

`Dreamine.MVVM.Core` should remain as platform-neutral as possible.

WPF-specific responsibilities such as `FrameworkElement.Loaded`, `EventManager`, and View runtime binding should not live in the core package. This package isolates those concerns into a dedicated WPF layer.

## Main type

### `DreamineAppBuilder`

The `DreamineAppBuilder` initializes the Dreamine MVVM runtime for a WPF application.

It performs the following steps:

1. registers View ↔ ViewModel mappings
2. auto-registers types into `DMContainer`
3. hooks the WPF `Loaded` event to assign `DataContext` automatically when needed
4. optionally registers the Window region navigator for the current Window region

When automatic navigator registration is enabled, each loaded Window can refresh the global `INavigator` registration with its matching region. This prevents navigation from staying pinned to a previously loaded Window.

## Usage

Call `DreamineAppBuilder.Initialize(...)` once during application startup.

```csharp
using System.Reflection;
using System.Windows;
using Dreamine.MVVM.Wpf;

namespace SampleApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DreamineAppBuilder.Initialize(Assembly.GetExecutingAssembly());
    }
}
```

## Project structure guideline

Recommended responsibility boundaries:

- `Dreamine.MVVM.Core`
  - container
  - command infrastructure
  - platform-neutral MVVM runtime support
- `Dreamine.MVVM.ViewModels`
  - `ViewModelBase`
- `Dreamine.MVVM.Locators`
  - ViewModel resolution and mapping logic
- `Dreamine.MVVM.Wpf`
  - WPF startup/bootstrap/runtime integration

## Target framework

- `net8.0-windows`
- WPF enabled

## License

MIT License
