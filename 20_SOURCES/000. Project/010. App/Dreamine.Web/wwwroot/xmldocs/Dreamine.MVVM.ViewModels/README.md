
# Dreamine.MVVM.ViewModels

Base ViewModel infrastructure for the Dreamine MVVM framework.

This package provides the foundational components required to build ViewModels in applications that use the Dreamine MVVM architecture.

[➡️ 한국어 문서 보기](./README_ko.md)

---

## Purpose

`Dreamine.MVVM.ViewModels` defines the base structures used when implementing ViewModels in the Dreamine ecosystem.

It serves as the core layer where ViewModel-related patterns and conventions are defined.

Typical responsibilities include:

- base ViewModel abstractions
- common MVVM patterns
- integration with Dreamine commands and bindings
- infrastructure for property notification

---

## Design Goals

The ViewModel layer follows the principles used across the Dreamine framework.

Design objectives:

- minimal dependencies
- clear MVVM separation
- lightweight base classes
- compatibility with source generators and command systems

The goal is to keep ViewModels simple while enabling powerful framework features.

---

## Architecture Role

Within the Dreamine MVVM ecosystem this package represents the **ViewModel Layer**.

```
Dreamine.MVVM.Interfaces
        ↑
Dreamine.MVVM.Commands
        ↑
Dreamine.MVVM.ViewModels
        ↑
Application ViewModels
```

Application-level ViewModels typically inherit from classes defined in this package.

---

## Installation

```bash
dotnet add package Dreamine.MVVM.ViewModels
```

Or add to the project file:

```xml
<PackageReference Include="Dreamine.MVVM.ViewModels" Version="1.0.0" />
```

---

## Requirements

- .NET 8.0

---

## License

MIT License
