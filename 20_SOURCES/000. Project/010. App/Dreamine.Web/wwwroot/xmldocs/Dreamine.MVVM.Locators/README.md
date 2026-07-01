# Dreamine.MVVM.Locators

View ↔ ViewModel mapping infrastructure for the Dreamine MVVM framework.

This library provides a lightweight ViewModel locator that supports convention-based resolution, manual registration, reverse view resolution, and Dependency Injection integration.

It is designed as a **platform-agnostic mapping engine**. It focuses on finding the correct ViewModel type from a View type, or the correct View type from a ViewModel type, using naming and namespace conventions.

WPF-specific concerns such as `DataContext`, `FrameworkElement`, and `Loaded` event wiring should be handled outside this library.

[➡️ 한국어 문서 보기](./README_ko.md)

## Features

- Convention-based View ↔ ViewModel mapping
- Manual ViewModel registration
- Optional DI resolver integration
- Automatic assembly scanning
- Reverse View resolution from ViewModel
- Reset and cache clearing APIs for tests and application reconfiguration
- Root namespace lookup support
- Nested namespace and subfolder lookup support
- Flexible namespace candidate expansion for structures such as:
  - `Views`
  - `View`
  - `Pages`
  - `Windows`
  - `Dialogs`
  - `GUI`
  - `GUIs`
  - `Screens`
  - `Controls`

## Design Goal

`Dreamine.MVVM.Locators` is not intended to be tied directly to WPF-only types.

Its responsibility is:

- resolve ViewModel types from View types
- resolve View types from ViewModel types
- provide a consistent convention engine
- support manual overrides and DI-based creation

Its responsibility is **not**:

- setting `DataContext`
- subscribing to `Loaded` events
- checking `Window`, `Page`, or `UserControl` inheritance

Those platform-specific behaviors should be implemented in a separate WPF-focused layer.

## Mapping Behavior

The locator resolves types in the following order.

### View → ViewModel

1. Manual registration map
2. Convention-based namespace candidates
3. Parent namespace expansion
4. Root namespace lookup
5. Assembly-wide fallback by type name

Example candidates for a View named `MainWindow`:

- `MyApp.ViewModels.MainWindowViewModel`
- `MyApp.Views.Admin.MainWindow` → `MyApp.ViewModels.Admin.MainWindowViewModel`
- `MyApp.GUI.Login.MainWindow` → `MyApp.ViewModels.Login.MainWindowViewModel`
- `MyApp.MainWindowViewModel`

### ViewModel → View

1. Convention-based namespace candidates
2. Parent namespace expansion
3. Root namespace lookup
4. Assembly-wide fallback by type name

## Supported Structure Examples

### Root namespace

```csharp
namespace MyApp;

public partial class MainWindow
{
}

public sealed class MainWindowViewModel
{
}
```

### Standard Views / ViewModels structure

```csharp
namespace MyApp.Views;

public partial class MainWindow
{
}

namespace MyApp.ViewModels;

public sealed class MainWindowViewModel
{
}
```

### Nested folders

```csharp
namespace MyApp.Views.Admin;

public partial class MainWindow
{
}

namespace MyApp.ViewModels.Admin;

public sealed class MainWindowViewModel
{
}
```

### Alternative folder names

```csharp
namespace MyApp.GUI.Account;

public partial class LoginDialog
{
}

namespace MyApp.ViewModels.Account;

public sealed class LoginDialogViewModel
{
}
```

## Dependency Injection Support

ViewModel instances are created through a registered resolver.

```csharp
resolver.Resolve(vmType)
```

There is no `Activator.CreateInstance` fallback for ViewModel creation. If no resolver is registered, or if the resolver returns `null`, `Resolve(...)` throws an `InvalidOperationException` with a message that distinguishes those two cases.

## Usage

### Register Resolver

```csharp
ViewModelLocator.RegisterResolver(new MyResolver());
```

### Clear Locator State

```csharp
ViewModelLocator.Clear();         // clears mappings and lookup caches, keeps resolver
ViewModelLocator.ClearResolver(); // removes the resolver only
ViewModelLocator.Reset();         // clears mappings, lookup caches, and resolver
```

`Reset()` is useful for unit-test isolation. `Clear()` is useful when mappings should be rebuilt while keeping the same DI strategy.

### Manual Mapping

```csharp
ViewModelLocator.Register(typeof(MainWindow), typeof(MainWindowViewModel));
```

### Resolve ViewModel

```csharp
var vm = ViewModelLocator.Resolve(typeof(MainWindow));
```

### Resolve View

```csharp
var view = ViewModelLocator.ResolveView(typeof(MainWindowViewModel));
```

### Auto Register

```csharp
ViewModelLocator.RegisterAll(Assembly.GetExecutingAssembly());
```

## Recommended Convention

The locator supports broad matching, but the recommended structure is still:

- `Views`
- `ViewModels`

This keeps projects predictable and reduces ambiguity when multiple candidates share the same type name.

## Notes

- Broad matching improves flexibility, but duplicate type names can cause ambiguous fallback resolution.
- When multiple candidates may exist, prefer manual registration.
- Use this library for mapping and creation, not for platform event wiring.
- AppDomain type scans are cached and invalidated when a new assembly is loaded or when `Clear()` / `Reset()` is called.

## License

MIT License
