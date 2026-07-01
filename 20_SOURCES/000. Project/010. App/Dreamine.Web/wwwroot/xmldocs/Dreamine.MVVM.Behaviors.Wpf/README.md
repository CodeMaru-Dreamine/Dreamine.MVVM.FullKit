
# Dreamine.MVVM.Behaviors.Wpf

WPF implementation layer for the Dreamine MVVM behavior system.

This package provides WPF-specific behavior infrastructure built on top of:

Dreamine.MVVM.Behaviors.Core

It enables attaching reusable UI behaviors to WPF elements using a lightweight
and MVVM-friendly architecture.

[➡️ 한국어 문서 보기](README_ko.md)

---

# Purpose

In MVVM applications, UI interaction logic should not live in code-behind.

Behaviors allow attaching interaction logic to controls declaratively in XAML.

Dreamine.MVVM.Behaviors.Wpf provides the WPF runtime layer that enables:

- XAML-based behavior attachment
- Multiple behavior composition
- MVVM-friendly UI interaction extensions

---

# Architecture

Dreamine Behavior stack:

Dreamine.MVVM.Behaviors.Core
    ↓
Dreamine.MVVM.Behaviors.Wpf
    ↓
Application Behaviors

Core provides the infrastructure.
Wpf package provides the runtime integration.

---

# Components

## BehaviorCollection

Container that allows multiple behaviors to be attached to a single UI element.

Example

<Button>
    <i:Interaction.Behaviors>
        <behaviors:WindowDragBehavior/>
    </i:Interaction.Behaviors>
</Button>

Responsibilities

- Stores behavior instances
- Manages attach / detach lifecycle
- Propagates AssociatedObject to each behavior

---

## Interaction

Static helper class used in XAML to attach behaviors.

Provides attached DependencyProperty:

Interaction.Behaviors

This property connects the BehaviorCollection to a DependencyObject.
Calling `Interaction.GetBehaviors(...)` creates an empty collection only; behaviors such as `WindowDragBehavior` must be declared explicitly.

---

## WindowDragBehavior

Example behavior included in this package.

Allows dragging a WPF window by clicking a UI element.

Typical use case:

Borderless window with custom title bar.

Example

<Grid>
    <i:Interaction.Behaviors>
        <behaviors:WindowDragBehavior/>
    </i:Interaction.Behaviors>
</Grid>

---

# Installation

Add NuGet package

dotnet add package Dreamine.MVVM.Behaviors.Wpf

Or reference the project directly.

---

# Example

XAML

<Grid
    xmlns:i="clr-namespace:Dreamine.MVVM.Behaviors.Wpf.Interactivity"
    xmlns:behaviors="clr-namespace:Dreamine.MVVM.Behaviors.Wpf.Interactivity">

    <i:Interaction.Behaviors>
        <behaviors:WindowDragBehavior/>
    </i:Interaction.Behaviors>

</Grid>

---

# Relationship to other packages

Dreamine.MVVM.Behaviors.Core

Provides the base infrastructure for behaviors.

Dreamine.MVVM.Behaviors

High-level reusable behaviors.

Dreamine.MVVM.Interactions

Interaction triggers and event behaviors.

---

# License

MIT License
