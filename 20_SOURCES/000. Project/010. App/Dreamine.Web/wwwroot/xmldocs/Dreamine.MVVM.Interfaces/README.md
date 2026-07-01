<!--!
\file README.md
\brief Dreamine.MVVM.Interfaces - Shared abstraction contracts for Dreamine MVVM modules.
\details Defines dependency injection, object activation, auto-registration, ViewModel resolution, navigation, and event contracts without concrete runtime implementation.
\author Dreamine Core Team
\date 2026-04-29
\version 1.0.5
-->

# Dreamine.MVVM.Interfaces

**Dreamine.MVVM.Interfaces** defines shared abstraction contracts used across the Dreamine MVVM framework.

This package contains contracts only. It does not provide concrete dependency injection containers, WPF navigation implementations, ViewModel locators, or runtime object creation logic.

The purpose of this package is to keep Dreamine modules loosely coupled and to preserve clean dependency direction between framework layers.

[➡️ 한국어 문서 보기](./README_ko.md)

---

## What this library provides

Dreamine.MVVM.Interfaces provides contracts for:

- dependency registration
- dependency resolution
- service container composition
- constructor selection
- object activation
- assembly type scanning
- auto-registration
- ViewModel resolution
- navigation
- event base markers
- window state payload contracts

---

## Package Role

`Dreamine.MVVM.Interfaces` should remain the lowest-level shared contract package.

Recommended dependency direction:

```text
Dreamine.MVVM.Interfaces
        ↑
Dreamine.MVVM.Core
        ↑
Dreamine.MVVM.Locators / Dreamine.MVVM.Wpf / Application modules
```

Rules:

- `Dreamine.MVVM.Interfaces` must not depend on `Dreamine.MVVM.Core`.
- `Dreamine.MVVM.Interfaces` must not depend on WPF-specific packages.
- Implementations should live in higher-level packages.
- Consumers should depend on the smallest interface they need.

---

## Project Structure

```text
Dreamine.MVVM.Interfaces
├── DependencyInjection
│   ├── IAssemblyTypeScanner.cs
│   ├── IAutoRegistrationService.cs
│   ├── IConstructorSelector.cs
│   ├── IObjectActivator.cs
│   ├── IServiceContainer.cs
│   ├── IServiceRegistry.cs
│   └── IServiceResolver.cs
├── Events
│   └── IEventBase.cs
├── Locators
│   └── IViewModelResolver.cs
├── Navigation
│   ├── INavigator.cs
│   └── IViewManager.cs
└── Windows
    ├── IWindowStateChange.cs
    ├── IWindowStateService.cs
    └── WindowStateChangedEventArgs.cs
```

---

## Dependency Injection Contracts

### IServiceRegistry

Defines service registration operations.

Supported registration concepts:

- transient concrete registration
- transient abstraction-to-implementation registration
- factory registration
- singleton instance registration
- singleton concrete registration
- singleton abstraction-to-implementation registration
- registration existence checks

Example implementation usage:

```csharp
registry.Register<IMyService, MyService>();
registry.RegisterSingleton<ISharedState, SharedState>();
```

---

### IServiceResolver

Defines service resolution operations.

```csharp
TService service = resolver.Resolve<TService>();
object service = resolver.Resolve(typeof(TService));
```

---

### IServiceContainer

Combines `IServiceRegistry` and `IServiceResolver`.

Use this only when a component genuinely needs both registration and resolution capabilities.

For better interface segregation:

- depend on `IServiceRegistry` when only registration is needed
- depend on `IServiceResolver` when only resolution is needed

---

### IConstructorSelector

Selects the constructor used during object activation.

Concrete implementations may choose constructors by policy, for example:

- largest parameter count
- explicit attribute
- parameter resolvability

---

### IObjectActivator

Creates object instances using constructor injection.

It receives an `IServiceResolver` so constructor dependencies can be resolved without directly depending on a concrete container.

---

### IAssemblyTypeScanner

Scans assemblies and returns loadable types.

This allows implementations to safely handle partially loadable assemblies and `ReflectionTypeLoadException` scenarios.

---

### IAutoRegistrationService

Defines the contract for convention-based auto-registration.

Concrete implementations decide:

- which assemblies are scanned
- which types are eligible
- which lifetime policy is applied

---

## Locator Contract

### IViewModelResolver

Defines a minimal ViewModel resolution strategy.

```csharp
object? viewModel = resolver.Resolve(typeof(MainWindowViewModel));
```

Typical implementers:

- DI-backed ViewModel resolver
- manual factory resolver
- test resolver

---

## Navigation Contract

### INavigator

Defines the minimal instance-based navigation contract used by Dreamine region navigation implementations.

```csharp
navigator.Navigate(viewModel);
```

`INavigator` intentionally accepts an object ViewModel to keep the contract independent from WPF UI types. Concrete navigation behavior belongs to WPF-specific packages.

### IViewManager

Defines the broader View display contract. It extends `INavigator` and adds type-based ViewModel resolution.

```csharp
viewManager.Show<MainViewModel>();
viewManager.Show(typeof(MainViewModel));
viewManager.Navigate(existingViewModel);
```

Use `IViewManager` for application-level View display and `INavigator` only for region/content-control style navigation.

---

## Event Contract

### IEventBase

Marker interface for Dreamine event classes.

It can be used by source generators, scanners, or framework conventions to identify event objects without introducing concrete dependencies.

This marker should not be used as the only automatic-generation rule. Dreamine generators currently use explicit attributes such as `[DreamineEvent]` for generation candidates, and scanners should combine this marker with attributes, namespaces, or naming rules.

---

## Window State Contracts

### IWindowStateChange

Describes the payload shape for window open-state changes.

### IWindowStateService

Tracks window open/close state and raises `WindowStateChangedEventArgs`.

`WindowStateChangedEventArgs` remains in this package because it is part of the `IWindowStateService` contract, but it also implements `IWindowStateChange` so consumers can depend on the payload abstraction where useful.

---

## Design Goals

Dreamine.MVVM.Interfaces prioritizes:

- dependency inversion
- interface segregation
- low coupling between modules
- testable infrastructure boundaries
- UI-framework independence
- replaceable implementations
- stable contracts for Core, Locators, WPF, and application modules

---

## Requirements

- **.NET**: `net8.0`
- No WPF dependency
- No concrete runtime implementation dependency

---

## Installation

```bash
dotnet add package Dreamine.MVVM.Interfaces
```

Or add to your project file:

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Interfaces" Version="1.0.4" />
</ItemGroup>
```

---

## Related Modules

Typical implementations and consumers:

- `Dreamine.MVVM.Core`
- `Dreamine.MVVM.Locators`
- `Dreamine.MVVM.Locators.Wpf`
- `Dreamine.MVVM.Wpf`
- `Dreamine.MVVM.Behaviors.Core`
- `Dreamine.MVVM.Behaviors.Wpf`

---

## License

MIT License
