<!--!
\file README.md
\brief Dreamine.MVVM.Generators - Roslyn source generators for Dreamine MVVM
\details Describes the package purpose, installation, architecture role, supported generators, constraints, and usage examples based on the current codebase.
\author Dreamine
\date 2026-04-20
\version 1.0.11
-->

# Dreamine.MVVM.Generators

**Dreamine.MVVM.Generators** is a **Roslyn incremental source generator package** used by the Dreamine MVVM ecosystem.

This package generates MVVM boilerplate code at **compile time** based on declarative attributes.

The main attributes currently handled are:

- `DreamineProperty`
- `DreamineEntry`
- `DreamineModel`
- `DreamineEvent`
- `DreamineCommand`

The goal of this package is to reduce repetitive code while keeping generation rules and constraints explicit.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this package does

MVVM projects repeatedly need the following patterns:

- backing field → property exposure
- method → `ICommand` property generation
- model / event reference exposure
- application entry bootstrap generation
- declarative command forwarding generation

Dreamine.MVVM.Generators moves those repetitive patterns into the **generation layer** so ViewModel and App code can stay smaller.

---

## Key Features

- Built on Roslyn **Incremental Source Generators**
- Can be packaged as an analyzer package
- Generates code from Dreamine attributes
- Supports entry bootstrap generation
- Supports field-based auto wiring
- Supports DreamineCommand-based direct and forwarding command generation
- Can be packed into `analyzers/dotnet/cs`
- Supports automatic analyzer registration through `buildTransitive`

---

## Requirements

- **Target Framework**: `netstandard2.0`
- Usually used together with:
  - `Dreamine.MVVM.Attributes`
  - `Dreamine.MVVM.Core`
  - WPF / .NET MVVM applications

---

## Installation

### Option A) NuGet

```bash
dotnet add package Dreamine.MVVM.Generators
```

### Option B) PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Generators" Version="1.0.6" />
</ItemGroup>
```

This package is intended to be used as an analyzer package, and `buildTransitive` is the recommended way to register it automatically in consuming projects.

---

## Project Structure

```text
Dreamine.MVVM.Generators
├── DreamineAutoWiringGenerator.cs
├── DreamineCommandSourceGenerator.cs
├── DreamineEntryGenerator.cs
├── AnalyzerReleases.Shipped.md
├── AnalyzerReleases.Unshipped.md
├── buildTransitive/
│   └── Dreamine.MVVM.Generators.targets
└── Dreamine.MVVM.Generators.csproj
```

---

## Architecture Role

This package belongs to the **generation layer** of the Dreamine MVVM stack.

```text
ViewModel / App Source Code
        │
        ├─ Dreamine.MVVM.Attributes
        │     (markers / metadata)
        │
        ├─ Dreamine.MVVM.Generators
        │     (compile-time code generation)
        │
        └─ Dreamine.MVVM.Core
              (runtime MVVM infrastructure)
```

The responsibility split is:

- **Attributes**: declare intent
- **Generators**: emit source code
- **Core**: executes runtime behavior

---

## Supported Generators

### 1) DreamineEntryGenerator

Generates application bootstrap code for types marked with `[DreamineEntry]`.

#### Current responsibilities

- Generates application startup initialization code
- Calls `DMContainer.AutoRegisterAll(...)`
- Calls `ViewModelLocator.RegisterAll(...)`
- Hooks `FrameworkElement.Loaded` to attach View ↔ ViewModel automatically
- Generates `RegisterBefore`, `RegisterAfter`, and `ShowMainWindow` partial hooks

#### Current constraints

- The target type must be **partial**
- The target type must inherit `System.Windows.Application`
- The design assumes only one valid entry type

#### Example

```csharp
using Dreamine.MVVM.Attributes;

[DreamineEntry]
public partial class App : Application
{
}
```

---

### 2) DreamineAutoWiringGenerator

Generates helper properties from **fields** marked with `[DreamineProperty]`, `[DreamineModel]`, and `[DreamineEvent]`.

#### Current responsibilities

- `_title` → `Title`
- `_model` → `Model`
- `_event` → `Event`

#### Current behavior

- Handles **field-based generation only**
- Does not regenerate from property declarations
- Adds helper properties to a partial class
- Skips generation when a member name conflict already exists

#### Example

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineProperty]
    private string _title;

    [DreamineModel]
    private MainModel _model;

    [DreamineEvent]
    private MainEvent _event;
}
```

#### Generated intent

- `Title` → field-backed property
- `Model` → model access property
- `Event` → event access property

#### Notes

- `[DreamineProperty]` generation assumes `SetProperty(ref field, value)` is available  
  so the target type must provide `SetProperty`
- `[DreamineModel]` and `[DreamineEvent]` currently **should not be used on readonly fields**
- `DreamineModel` uses a `new T()` initialization path
- `DreamineEvent` uses a `DMContainer.Resolve<T>()` initialization path
- Generated code no longer forces inheritance from `ViewModelBase`

---

### 3) DreamineCommandSourceGenerator

Generates `ICommand` properties from methods marked with `[DreamineCommand]`.

#### Current responsibilities

- Generates a `{MethodName}Command` property
- Supports `CommandName` override when provided
- Directly wraps the annotated method when `TargetMethod` is not specified
- Generates `TargetMethod` invocation code when forwarding is requested
- Assigns the result to the `BindTo` property when forwarding with a return value
- Generates a forwarding body when the method has no implementation body and `TargetMethod` is specified
- Avoids direct dependency on an external `RelayCommand` type by emitting an internal generated `ICommand` wrapper

#### Example

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineCommand]
    private void Save()
    {
    }

    [DreamineCommand("Event.ReadmeClick", BindTo = "Readme")]
    partial void LoadReadme();
}
```

#### Current constraints

- The containing type must be **partial**
- The target method must be **parameterless void**
- Forwarding methods without a body must be **partial**
- Generation is skipped when the command property name conflicts with an existing member

---

## Quick Start

### 1) Add the required packages

```xml
<ItemGroup>
  <PackageReference Include="Dreamine.MVVM.Attributes" Version="1.0.6" />
  <PackageReference Include="Dreamine.MVVM.Core" Version="1.0.9" />
  <PackageReference Include="Dreamine.MVVM.Generators" Version="1.0.11" PrivateAssets="all" OutputItemType="Analyzer" />
</ItemGroup>
```

### 2) Declare attributes

```csharp
using Dreamine.MVVM.Attributes;

public partial class MainViewModel
{
    [DreamineProperty]
    private string _title;

    [DreamineCommand]
    private void Save()
    {
    }

    [DreamineCommand("Event.ReadmeClick", BindTo = "Readme")]
    partial void LoadReadme();
}
```

### 3) Build

During build, partial source files are generated.

Typical outputs include:

- `Title` property
- `SaveCommand`
- `LoadReadmeCommand`
- forwarding method body

---

## Important Notes Based on the Current Code

### 1) The generator package is not a standalone runtime framework

Generated code still assumes some Dreamine runtime concepts exist.

Examples:

- `DMContainer`
- `ViewModelLocator`
- `SetProperty`

This package is intended to be used inside the Dreamine MVVM stack.

### 2) Not all attributes have the same usage scope

Under the current implementation:

- `DreamineEntry` → App / bootstrap layer
- `DreamineProperty` → ViewModel layer with `SetProperty`
- `DreamineModel`, `DreamineEvent` → field access generation
- `DreamineCommand` → method-based command generation

### 3) The generation rules are intentionally becoming stricter

The current generator implementations prioritize:

- partial type validation
- method signature validation
- name conflict prevention
- diagnostics for invalid usage

---

## Packaging Notes

The current project assumes analyzer-style packaging.

Typical configuration points are:

- `PackageType=Analyzer`
- `OutputItemType=Analyzer`
- `IncludeBuildOutput=false`
- package the generator DLL into `analyzers/dotnet/cs`
- use `buildTransitive` for automatic registration

---

## Comparison

| Package | Role | Runtime Logic | Compile-Time Generation |
|---|---|---:|---:|
| Dreamine.MVVM.Attributes | declaration layer | No | No |
| Dreamine.MVVM.Generators | generation layer | No | Yes |
| Dreamine.MVVM.Core | runtime layer | Yes | No |

This separation keeps the system layered by responsibility.

---

## Recommended Pairing

This package is usually used together with:

```text
Dreamine.MVVM.Attributes
Dreamine.MVVM.Core
Dreamine WPF / UI / App packages
```

---

## License

MIT License
