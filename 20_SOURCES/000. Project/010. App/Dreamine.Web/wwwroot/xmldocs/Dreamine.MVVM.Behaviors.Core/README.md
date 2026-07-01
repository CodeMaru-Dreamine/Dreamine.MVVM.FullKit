# Dreamine.MVVM.Behaviors.Core

Core infrastructure for implementing **MVVM Behaviors in WPF** within the Dreamine framework.

This library provides the **base abstractions required to build reusable attached behaviors** that can be applied to WPF UI elements.

It is intended to be used as the **foundation layer** for higher-level behavior packages such as:

- Dreamine.MVVM.Behaviors
- Dreamine.MVVM.Triggers
- Dreamine.MVVM.Interactions

[➡️ 한국어 문서 보기](README_ko.md)
---

# Purpose

WPF behaviors allow UI logic to be attached to controls without breaking the MVVM pattern.

However, many implementations rely on heavy frameworks or implicit behavior systems.

`Dreamine.MVVM.Behaviors.Core` provides a **minimal and explicit infrastructure** to build behaviors with:

- strong typing
- predictable attach/detach lifecycle
- clean MVVM separation

---

# Key Concepts

### Behavior<T>

Generic base class for creating behaviors.

Features:

- Strongly typed `AssociatedObject`
- Based on `Freezable` for XAML support
- Explicit attach / detach lifecycle
- Designed for MVVM-friendly UI extensions

Example target types:

- `Window`
- `Button`
- `TextBox`
- `Grid`

---

### IAttachedObject

Interface that represents an object that can attach to a `DependencyObject`.

Responsibilities:

- Manage reference to the attached UI element
- Handle lifecycle of connection and disconnection

Methods:

```
Attach(DependencyObject)
Detach()
```

---

### IBehavior

Core contract for all Dreamine behaviors.

Defines the minimal structure required for any behavior implementation.

---

# Project Structure

```
Dreamine.MVVM.Behaviors.Core
│
├─ Base
│   └─ BehaviorGeneric.cs
│
├─ Interfaces
│   ├─ IAttachedObject.cs
│   └─ IBehavior.cs
│
└─ Dreamine.MVVM.Behaviors.Core.csproj
```

---

# Architecture

```
WPF UI Element
     │
     └─ Behavior<T>
            │
            ├─ Attach()
            └─ Detach()
```

This design allows behaviors to extend UI functionality while keeping **ViewModel logic completely separate**.

---

# Example

Example behavior skeleton:

```csharp
public class FocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        AssociatedObject.Focus();
    }

    protected override void OnDetaching()
    {
    }
}
```

Usage in XAML:

```xml
<TextBox>
    <i:Interaction.Behaviors>
        <local:FocusBehavior/>
    </i:Interaction.Behaviors>
</TextBox>
```

---

# Requirements

Runtime:

```
.NET 8.0
WPF
```

---

# Related Packages

- Dreamine.MVVM.Behaviors
- Dreamine.MVVM.Attributes
- Dreamine.MVVM.ViewModels

---

# License

MIT License
