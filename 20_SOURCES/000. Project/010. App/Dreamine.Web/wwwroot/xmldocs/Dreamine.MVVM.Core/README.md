<!--!
\file README.md
\brief Dreamine.MVVM.Core - Runtime implementation for dependency injection and auto-registration.
\details Provides the default Dreamine service container, object activation, service lifetime handling, and convention-based auto-registration infrastructure.
\author Dreamine Core Team
\date 2026-04-29
\version 1.0.10
-->

# Dreamine.MVVM.Core

**Dreamine.MVVM.Core** is the lightweight runtime infrastructure module of the Dreamine MVVM framework.

It provides the default dependency injection implementation used by Dreamine modules, including explicit service registration, constructor-based resolution, singleton instance handling, object activation, and convention-based auto-registration.

This package is intentionally focused on runtime infrastructure. It must remain independent from WPF-specific UI concepts such as `Window`, `UserControl`, `FrameworkElement`, `ContentControl`, and `DataContext`.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library provides

Dreamine.MVVM.Core provides:

- `DMContainer` static facade
- `DreamineContainer` default dependency container implementation
- thread-safe registration, resolution, and singleton creation
- transient service registration
- singleton service registration
- factory-based service registration
- constructor-based dependency resolution
- singleton instance caching
- circular dependency detection
- convention-based auto-registration
- assembly type scanning
- object activation through `IObjectActivator`
- static facade reset and container replacement APIs for test isolation

---

## Package Role

`Dreamine.MVVM.Core` implements infrastructure contracts defined by `Dreamine.MVVM.Interfaces`.

Recommended dependency direction:

```text
Dreamine.MVVM.Interfaces
        ↑
Dreamine.MVVM.Core
        ↑
Dreamine.MVVM.Wpf / Dreamine.MVVM.Locators / Application modules
```

`Dreamine.MVVM.Core` should not contain WPF-specific binding or navigation logic. WPF-specific behavior belongs in WPF-focused packages.

---

## Service Lifetime Policy

Dreamine.MVVM.Core uses explicit lifetime behavior.

| Registration API | Lifetime | Notes |
|---|---:|---|
| `Register<TImplementation>()` | Transient | Creates a new instance when resolved. |
| `Register<TService, TImplementation>()` | Transient | Uses abstraction-to-implementation mapping. |
| `Register<TService>(Func<TService>)` | Transient | Executes the factory whenever resolved. |
| `RegisterSingleton<TService>(TService)` | Singleton | Stores the provided instance. |
| `RegisterSingleton<TImplementation>()` | Singleton | Creates and caches one instance on first resolve. |
| `RegisterSingleton<TService, TImplementation>()` | Singleton | Abstraction-to-implementation singleton mapping. |
| `AutoRegisterAll(Assembly)` | Singleton for matched types | Preserves Dreamine auto-registration behavior. |
| `DMContainer.Reset()` | Static facade reset | Replaces the global container with an empty `DreamineContainer`. |
| `DMContainer.SetContainer(IServiceContainer)` | Static facade replacement | Allows tests or hosts to provide an isolated container. |

> Important: auto-registered types are registered as singleton services by default. This keeps automatically discovered ViewModels, Models, Events, and Managers stable across repeated resolution unless the application explicitly registers another lifetime.

`DreamineContainer` serializes registration and resolution internally. Singleton lazy creation is guarded so concurrent first resolves produce one shared instance. Circular dependency detection is stored in a per-resolution context instead of shared container state.

---

## Auto-Registration

`DMContainer.AutoRegisterAll(rootAssembly)` scans candidate assemblies and registers supported concrete types.

Auto-registration is implemented through:

- `AutoRegistrationService`
- `AssemblyTypeScanner`
- `NamingConventionAutoRegistrationFilter`

The current naming convention targets concrete non-generic classes whose names or full names match supported Dreamine conventions, including:

- `*Model`
- `*Event`
- `*ViewModel`
- `*Manager` only when the type is under a `.Managers.` or `.xaml.` namespace
- `.xaml.Model`
- `.xaml.Event`
- `.xaml.ViewModel`
- types marked with an attribute named `DreamineRegisterAttribute` or `DreamineAutoRegisterAttribute`

Matched types are registered using singleton lifetime.

---

## Project Structure

```text
Dreamine.MVVM.Core
├── DMContainer.cs
├── AutoRegistration
│   ├── AssemblyTypeScanner.cs
│   ├── AutoRegistrationService.cs
│   └── NamingConventionAutoRegistrationFilter.cs
└── DependencyInjection
    ├── ConstructorActivator.cs
    ├── ConstructorSelector.cs
    ├── DreamineContainer.cs
    ├── ResolutionContext.cs
    ├── ServiceDescriptor.cs
    └── ServiceLifetime.cs
```

---

## Requirements

- **.NET**: `net8.0`

---

## Installation

### Project Reference

```xml
<ItemGroup>
  <ProjectReference Include="..\Dreamine.MVVM.Interfaces\Dreamine.MVVM.Interfaces.csproj" />
  <ProjectReference Include="..\Dreamine.MVVM.Core\Dreamine.MVVM.Core.csproj" />
</ItemGroup>
```

### NuGet

```bash
dotnet add package Dreamine.MVVM.Core
```

---

## Quick Start

### Register a transient service

```csharp
DMContainer.Register<IMyService, MyService>();
```

### Register a factory

```csharp
DMContainer.Register<IMyService>(() => new MyService());
```

### Register a singleton instance

```csharp
var service = new MyService();
DMContainer.RegisterSingleton<IMyService>(service);
```

### Register a singleton type

```csharp
DMContainer.RegisterSingleton<IMyService, MyService>();
```

### Resolve a service

```csharp
IMyService service = DMContainer.Resolve<IMyService>();
```

### Auto-register Dreamine application types

```csharp
DMContainer.AutoRegisterAll(typeof(App).Assembly);
```

### Reset the static facade in tests

```csharp
DMContainer.Reset();
```

### Replace the static facade container

```csharp
DMContainer.SetContainer(new DreamineContainer());
```

---

## Component Reference

### DMContainer

`DMContainer` is a static facade over the default `DreamineContainer` instance.

Its role is to preserve simple application-level usage while delegating actual behavior to focused infrastructure components.

`DMContainer` should remain thin. It should not directly contain assembly scanning, constructor selection, object activation, or lifetime-management logic.

---

### DreamineContainer

`DreamineContainer` is the default dependency container implementation.

Responsibilities:

- store service descriptors
- resolve registered services
- resolve constructor dependencies
- cache singleton instances
- create transient instances
- detect circular dependencies during resolution
- delegate object creation to `IObjectActivator`

---

### ConstructorActivator

`ConstructorActivator` creates object instances using constructor injection.

Responsibilities:

- select a constructor through `IConstructorSelector`
- resolve constructor parameters through `IServiceResolver`
- create an object instance through reflection

---

### AutoRegistrationService

`AutoRegistrationService` performs convention-based service registration.

Responsibilities:

- scan candidate assemblies
- filter supported concrete types
- register matched types as singleton services

---

## Design Goals

Dreamine.MVVM.Core prioritizes:

- explicit behavior over hidden runtime magic
- low dependency surface
- constructor-based composition
- interface-driven infrastructure
- predictable service lifetime behavior
- compatibility with Dreamine auto-registration
- separation between contracts and implementations
- independence from WPF-specific UI concerns

---

## Related Modules

Typical composition with other Dreamine packages:

- `Dreamine.MVVM.Interfaces`
- `Dreamine.MVVM.Attributes`
- `Dreamine.MVVM.Generators`
- `Dreamine.MVVM.Locators`
- `Dreamine.MVVM.Locators.Wpf`
- `Dreamine.MVVM.ViewModels`
- `Dreamine.MVVM.Wpf`

---

## License

MIT License
