
# Dreamine.MVVM.Extensions

Utility extension package for the Dreamine MVVM framework.

This package is reserved for small, platform-neutral helper utilities.

[➡️ 한국어 문서 보기](./README_ko.md)

---

## Purpose

`Dreamine.MVVM.Extensions` contains small helper classes that extend the behavior of the core Dreamine MVVM components without introducing WPF-specific dependencies.

These utilities are intentionally lightweight and independent so they can be reused across different Dreamine modules.

---

## Key Components

No WPF-specific helpers live in this package. Region and visual-tree helpers belong to `Dreamine.MVVM.Locators.Wpf`.

---

## Design Goals

This package follows several principles used across Dreamine:

- minimal dependencies
- lightweight utilities
- reusable helper functions
- separation from core framework logic

Extensions are placed in a separate package so the core MVVM infrastructure remains clean and focused.

---

## Architecture Role

Within the Dreamine MVVM ecosystem this package belongs to the **Utility Layer**.

```
Dreamine.MVVM.Extensions
        ↓
Platform-neutral Dreamine modules
```

It provides supporting helpers without depending on WPF integration packages.

---

## Installation

```bash
dotnet add package Dreamine.MVVM.Extensions
```

Or add to the project file:

```xml
<PackageReference Include="Dreamine.MVVM.Extensions" Version="1.0.0" />
```

---

## Requirements

- .NET 8.0

---

## License

MIT License
