<!--!
\file README.md
\brief Dreamine.UI.Wpf - Foundation WPF utilities: converters, behaviors, localization, and base controls for Dreamine UI.
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf

**Dreamine.UI.Wpf** is the WPF foundation layer of the Dreamine UI stack.

It provides shared utilities consumed by all higher-level Dreamine UI packages:

- Value converters
- WPF behaviors
- Localization infrastructure
- Base LED control primitives

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

WPF UI packages need a shared foundation for:

- Binding converters reused across many XAML files
- XAML-attachable behaviors (drag, numeric range, etc.)
- Centralized multi-language text management
- Shared enumerations and primitives for LED controls

Without this layer, each UI package would duplicate converters and utilities, causing divergence and maintenance burden.

---

## Key Features

- **Converters**: null-to-visibility, bool-to-int, LED geometry, value/unit combination, format-to-example, language-to-XML
- **Behaviors**: `WindowDragBehavior`, `NumericRangeBehavior`
- **Localization**: `DreamineLocalizationManager` — multi-language text lookup with hot-reload support
- **LED primitives**: `LedCorner` enum shared across LED-related controls
- Targets `net8.0-windows`

---

## Requirements

- **Target Framework**: `net8.0-windows`
- **Dependencies**:
  - `Dreamine.MVVM.ViewModels`
  - `Microsoft.Xaml.Behaviors.Wpf`
  - `System.Drawing.Common`

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf" />
```

---

## Project Structure

```text
Dreamine.UI.Wpf
├── Behaviors/
│   ├── NumericRangeBehavior.cs        — clamp numeric input to min/max
│   └── WindowDragBehavior.cs          — drag window by mouse-down on element
├── Controls/
│   └── LedCorner.cs                   — corner radius enum for LED controls
├── Converters/
│   ├── BoolToIntDynamicConverter.cs   — bool → configurable int value
│   ├── FormatToExampleConverter.cs    — input format → example string
│   ├── LedInnerDiameterConverter.cs   — LED inner circle size (MultiBinding)
│   ├── LedPositionConverter.cs        — LED dot offset within bounding box
│   ├── NullToVisibilityConverter.cs   — null → Collapsed, non-null → Visible
│   └── ValueUnitCombinationConverter.cs — combine value + unit label
└── Localization/
    ├── DreamineLocalization.cs        — XAML attached property entry point
    └── DreamineLocalizationManager.cs — runtime text lookup and language switch
```

---

## Architecture Role

```text
Dreamine.MVVM.ViewModels
        │
Dreamine.UI.Wpf              ← this package
        │
Dreamine.UI.Wpf.Controls
Dreamine.UI.Wpf.Equipment
Dreamine.UI.Wpf.Themes
```

All higher-level UI packages reference this package.  
It must not reference them.

---

## Quick Start

### Converters in XAML

```xml
xmlns:conv="clr-namespace:Dreamine.UI.Wpf.Converters;assembly=Dreamine.UI.Wpf"

<conv:NullToVisibilityConverter x:Key="NullToVis" />

<TextBlock Visibility="{Binding Icon, Converter={StaticResource NullToVis}}" />
```

### WindowDragBehavior

```xml
xmlns:b="clr-namespace:Dreamine.UI.Wpf.Behaviors;assembly=Dreamine.UI.Wpf"
xmlns:i="http://schemas.microsoft.com/xaml/behaviors"

<Border Background="#FF2D2D2D">
    <i:Interaction.Behaviors>
        <b:WindowDragBehavior />
    </i:Interaction.Behaviors>
</Border>
```

### Localization

```csharp
// Set language at startup
DreamineLocalizationManager.SetLanguage(Language.ko_KR);
```

```xml
xmlns:vsl="clr-namespace:Dreamine.UI.Wpf.Localization;assembly=Dreamine.UI.Wpf"

<TextBlock vsl:DreamineLocalization.Key="MainMenu.Title" />
```

---

## Converter Reference

| Converter | Input | Output |
|---|---|---|
| `NullToVisibilityConverter` | `object?` | `Visibility` |
| `BoolToIntDynamicConverter` | `bool` | `int` (configurable) |
| `LedInnerDiameterConverter` | `double, double` (MultiBinding) | `double` |
| `LedPositionConverter` | `double, LedCorner` (MultiBinding) | `double` |
| `ValueUnitCombinationConverter` | `string` | `string` |
| `FormatToExampleConverter` | `InputFormat` | `string` |

---

## Design Notes

- This package never references higher-level UI packages
- Converters are stateless and safe to use as static resources
- `DreamineLocalizationManager` loads text from XML files at runtime — the file path is configurable
- `WindowDragBehavior` uses `Window.GetWindow(AssociatedObject).DragMove()` internally

---

## License

MIT License
