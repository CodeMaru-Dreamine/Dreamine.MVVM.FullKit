<!--!
\file README.md
\brief Dreamine.UI.Abstractions - Platform-agnostic UI contract interfaces and enumerations for Dreamine UI components.
\author Dreamine Core Team
\date 2026-06-12
\version 1.0.0
-->

# Dreamine.UI.Abstractions

**Dreamine.UI.Abstractions** defines the platform-agnostic contracts, interfaces, and enumerations shared across all Dreamine UI packages.

It contains no WPF-specific code.  
Concrete implementations live in platform-specific packages such as `Dreamine.UI.Wpf.Equipment`.

[➡️ 한국어 문서 보기](./README_KO.md)

---

## What this library solves

UI component contracts need to be defined independently of any rendering platform so that:

- abstractions can be referenced without pulling in WPF assemblies
- business-layer code can depend on interfaces instead of concrete UI controls
- platform-specific packages implement the contracts without circular dependencies

---

## Key Features

- Platform-agnostic popup service interface (`IPopupService`)
- Virtual keyboard layout and input mode enumerations
- Language code enumeration for keyboard localization
- Action result enumeration for virtual keyboard Enter-key providers
- No WPF / UI framework dependency

---

## Requirements

- **Target Framework**: `net8.0-windows`
- No external package dependencies

---

## Installation

### NuGet

```bash
dotnet add package Dreamine.UI.Abstractions
```

### PackageReference

```xml
<PackageReference Include="Dreamine.UI.Abstractions" />
```

---

## Project Structure

```text
Dreamine.UI.Abstractions
├── Popup/
│   ├── BlinkPopupOptions.cs       — options passed to blink popup windows
│   └── IPopupService.cs           — popup service contract
└── VirtualKeyboard/
    ├── ActionResult.cs            — Enter-key provider result
    ├── eActionResult.cs           — (internal, replaced by ActionResult)
    ├── IEnterActionProvider.cs    — Enter-key action provider contract
    ├── KeyboardInputMode.cs       — text / numeric / password input mode
    ├── LanguageCode.cs            — en_US / ko_KR / zh_CN / vi_VN
    └── VkLayout.cs                — Text / Password / Numeric / Decimal
```

---

## Architecture Role

```text
Dreamine.UI.Abstractions
        │
        ├─ Dreamine.UI.Wpf.Equipment   (popup + keyboard implementation)
        ├─ Dreamine.UI.Wpf.Controls    (navigation + view management)
        └─ Application Code            (depends on interfaces only)
```

This package is the lowest layer of the UI stack — no upward dependencies.

---

## Key Interfaces

### `IPopupService`

Provides a platform-neutral contract for showing, closing, and querying blink popup windows.

```csharp
IPopupService popupService = DMContainer.Resolve<IPopupService>();

await popupService.ShowBlinkAsync(owner, new BlinkPopupOptions
{
    Message = "Operation complete",
    OkText  = "OK"
});
```

### `BlinkPopupOptions`

Configures the appearance and behavior of blink popup windows.

```csharp
var options = new BlinkPopupOptions
{
    Title          = "Warning",
    Message        = "Check equipment status",
    UseBlink       = true,
    BlinkIntervalMs = 400,
    Color1         = Colors.Red,
    Color2         = Colors.DarkRed,
    IsModal        = true
};
```

### `VkLayout`

Selects the virtual keyboard layout presented to the user.

| Value     | Description                    |
|-----------|-------------------------------|
| `Text`    | Full alphanumeric keyboard     |
| `Password`| Masked text keyboard           |
| `Numeric` | Numeric pad                    |
| `Decimal` | Decimal number input           |

### `LanguageCode`

Selects the language of the virtual keyboard.

| Value   | Language   |
|---------|-----------|
| `en_US` | English    |
| `ko_KR` | Korean     |
| `zh_CN` | Chinese    |
| `vi_VN` | Vietnamese |

---

## Design Notes

This package deliberately stays free of any rendering framework dependency.

- No `System.Windows` types
- No XAML resource references
- Contracts only — all behavior lives in implementing packages

---

## License

MIT License
