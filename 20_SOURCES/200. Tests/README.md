# Dreamine FullKit Tests

This folder contains automated tests for the Dreamine MVVM FullKit libraries.

## Projects

- `Dreamine.FullKit.Tests`: Cross-platform `net8.0` tests for pure library code.
- `Dreamine.FullKit.Wpf.Tests`: Windows/WPF-specific `net8.0-windows` tests for UI-facing helpers, commands, converters, and view models.

## Scope

The first priority is fast, deterministic unit coverage for public library behavior:

- Attribute metadata and simple model/options defaults
- Dependency injection and view-model locator behavior
- Communication framing, serialization, queues, routing, and protocol adapters
- PLC/IO address, protocol, memory, and result handling
- Logging stores, formatters, sinks, and service behavior
- Threading policy and model behavior
- WPF command/converter/view-model logic that can run without a visible window

Tests that require physical hardware, COM components, live sockets, RabbitMQ servers, or a visible WPF window should be isolated behind fakes or skipped until an integration-test project is added.

## Running

From `20_SOURCES`:

```powershell
dotnet test DreamineWorkSpace.sln
```

To run only the cross-platform tests:

```powershell
dotnet test "200. Tests\Dreamine.FullKit.Tests\Dreamine.FullKit.Tests.csproj"
```

To run only the WPF/Windows tests:

```powershell
dotnet test "200. Tests\Dreamine.FullKit.Wpf.Tests\Dreamine.FullKit.Wpf.Tests.csproj"
```
