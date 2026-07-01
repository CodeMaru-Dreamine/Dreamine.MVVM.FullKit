<!--!
\file README.md
\brief Dreamine.UI.Wpf.Themes - WPF ResourceDictionary style and template files for all Dreamine UI controls.
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Wpf.Themes

**Dreamine.UI.Wpf.Themes** contains the WPF `ResourceDictionary` style and control template definitions for all Dreamine UI controls.

It is a pure XAML package — no code-behind logic.  
Controls in `Dreamine.UI.Wpf.Controls` and `Dreamine.UI.Wpf.Equipment` auto-merge styles from this package at construction time.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

WPF controls need a centralized place for:

- Consistent visual design across all Dreamine controls
- Style definitions that can be updated without rebuilding control logic
- Separation of visual layer from behavior layer

By isolating all XAML styles into a dedicated package, application teams can override or replace the theme layer without touching control implementations.

---

## Key Features

- One `ResourceDictionary` file per control type
- Root merge dictionary `DreamineThemes.xaml` includes all style files
- Style keys match the WPF `TargetType` pattern — no explicit key required in application XAML
- Supports converter references via `assembly=` qualified xmlns
- Numeric keypad and message box button styles included

---

## Requirements

- **Target Framework**: `net8.0-windows`
- **Dependencies**:
  - `Dreamine.UI.Wpf`
  - `Dreamine.UI.Wpf.Controls`

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Wpf.Themes
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Wpf.Themes" />
```

---

## Project Structure

```text
Dreamine.UI.Wpf.Themes
├── DreamineThemes.xaml                — root merge dictionary
├── DreamineButtonStyle.xaml
├── DreamineCheckBoxStyle.xaml
├── DreamineCheckLedStyle.xaml
├── DreamineComboBoxStyle.xaml
├── DreamineDataGridStyle.xaml
├── DreamineExpanderStyle.xaml
├── DreamineGridStyles.xaml
├── DreamineImageStyle.xaml
├── DreamineLabelStyle.xaml
├── DreamineListBoxStyle.xaml
├── DreamineMessageBoxButtonStyle.xaml
├── DreaminePasswordBoxStyle.xaml
├── DreamineRadioButtonStyle.xaml
├── DreamineTabControlStyle.xaml
├── DreamineTextBlockStyle.xaml
├── DreamineTextBoxStyle.xaml
├── DreamineTimeSpinnerStyle.xaml
└── DreamineKeyPadStyles.xaml
```

---

## Architecture Role

```text
Dreamine.UI.Wpf
Dreamine.UI.Wpf.Controls
        │
Dreamine.UI.Wpf.Themes       ← this package (visual layer only)
        │
Application Code
```

Control libraries reference this package to auto-merge styles.  
Application code may also reference this package to apply styles globally via `App.xaml`.

---

## Usage

### Option A) Auto-merge (default behavior)

Controls auto-merge their matching `ResourceDictionary` when instantiated.  
No additional setup is required in application XAML.

### Option B) Global application theme

Add to `App.xaml` to apply Dreamine styles project-wide:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Dreamine.UI.Wpf.Themes;component/DreamineThemes.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### Option C) Single style file

Include only the styles you need:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="/Dreamine.UI.Wpf.Themes;component/DreamineButtonStyle.xaml" />
    <ResourceDictionary Source="/Dreamine.UI.Wpf.Themes;component/DreamineTextBoxStyle.xaml" />
</ResourceDictionary.MergedDictionaries>
```

---

## Style File Reference

| File | Target Control |
|---|---|
| `DreamineButtonStyle.xaml` | `DreamineButton` |
| `DreamineCheckBoxStyle.xaml` | `DreamineCheckBox` |
| `DreamineCheckLedStyle.xaml` | `DreamineCheckLed` |
| `DreamineComboBoxStyle.xaml` | `DreamineComboBox` |
| `DreamineDataGridStyle.xaml` | `DreamineDataGrid` |
| `DreamineExpanderStyle.xaml` | `DreamineExpander` |
| `DreamineGridStyles.xaml` | `Grid` layout helpers |
| `DreamineImageStyle.xaml` | `DreamineImage` |
| `DreamineLabelStyle.xaml` | `DreamineLabel` |
| `DreamineListBoxStyle.xaml` | `DreamineListBox` |
| `DreamineMessageBoxButtonStyle.xaml` | Message box buttons |
| `DreaminePasswordBoxStyle.xaml` | `DreaminePasswordBox` |
| `DreamineRadioButtonStyle.xaml` | `DreamineRadioButton` |
| `DreamineTabControlStyle.xaml` | `DreamineTabControl` |
| `DreamineTextBlockStyle.xaml` | `DreamineTextBlock` |
| `DreamineTextBoxStyle.xaml` | `DreamineTextBox` |
| `DreamineTimeSpinnerStyle.xaml` | `DreamineTimeSpinner` |
| `DreamineKeyPadStyles.xaml` | Virtual keyboard keys |

---

## Design Notes

- All `xmlns` declarations for cross-assembly types use the `assembly=` qualifier — required for XAML compilation across project boundaries
- Converter resources such as `NullToVisibilityConverter` are declared inline in the style files that use them
- No code-behind exists in this package; styles are purely declarative
- Overriding a style: define a style with `x:Key` matching the control type in your application resource dictionary — it takes precedence over the merged style

---

## License

MIT License
