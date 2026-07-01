# Dreamine.Hybrid

Core runtime and abstraction layer for Dreamine hybrid applications.

[➡️ 한국어 문서 보기](README_ko.md)

## Purpose

`Dreamine.Hybrid` contains platform-neutral contracts and in-memory implementations used to share messages and state between a host application and embedded UI layers.

It does not host WebView2 or Blazor directly. WPF-specific hosting is provided by `Dreamine.Hybrid.Wpf`.

## Main Types

- `IHybridMessage`
- `IHybridMessageBus`
- `IHybridStateStore`
- `HybridMessageBase`
- `InMemoryHybridMessageBus`
- `HybridStateStore`

App-specific messages should live in the application or sample project, not in this library package. Derive them from `HybridMessageBase` or implement `IHybridMessage`.

## Package Boundary

Use this package when you need shared hybrid contracts or an in-process message bus.

Use `Dreamine.Hybrid.Wpf` when you need a WPF `HybridHostControl` for BlazorWebView/WebView2 hosting.

## License

MIT License
