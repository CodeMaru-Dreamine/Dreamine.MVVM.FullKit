# Dreamine SECS/GEM Interoperability Harness

This .NET 8 WPF tool exercises the public Dreamine SECS-II, HSMS and basic GEM APIs. It provides separate TCP/HSMS status, a structured item editor, scenario results, wire-oriented logs, masked evidence export and an automated local loopback.

It is an interoperability evidence tool, not a compliance certificate. External simulator scenarios remain `Not Run` or `Waiting for User` until a person completes the documented simulator steps.

## Run

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release
```

The simulator is launched only by the **Launch Simulator — manual action** button.

## Headless self-test

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --self-test --output self-test.json
```

See [docs/SECOMSIMULATOR_INTEROP_TEST.md](docs/SECOMSIMULATOR_INTEROP_TEST.md) and [docs/INTEROP_TEST_MATRIX.md](docs/INTEROP_TEST_MATRIX.md).

## Multi-equipment host

Use the **Multi Equipment** tab for configured endpoints, or run the isolated 1/2/10/50-equipment local loopback from a Release output directory:

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

See [Multi-Equipment Host](docs/MULTI_EQUIPMENT_HOST.md), the [test matrix](docs/MULTI_EQUIPMENT_TEST_MATRIX.md), and the [performance snapshot](docs/MULTI_EQUIPMENT_PERFORMANCE.md). External simulator results remain `Not Run / Waiting for User` until manually recorded.

## Basic responder extension examples

`Managers/MessageManager.cs` contains a deliberately small built-in Equipment responder. The examples show how to:

- read typed request children through `SecsListItem.Items` and `Values.Span`;
- build a dynamic flat `L[n]` with a temporary `List<SecsItem>`;
- create nested lists by passing child `SecsListItem` instances to a parent;
- return a one-byte Binary acknowledgement; and
- return an ASCII scalar without an unnecessary surrounding list.

`SecsListItem` is immutable, so build dynamic children in a temporary collection and pass `children.ToArray()` to its constructor. Add another `(Stream, Function)` tuple to `BuildBasicResponseItem` for a new educational pair. S1F1/S1F2 and S1F13/S1F14 are handled first by `GemProtocolEngine` and should not be duplicated in the fallback switch.

These handlers are examples, not normative message definitions or production equipment behavior. Keep state rules, data identifiers, command semantics, and other application-specific behavior in a separate Equipment Profile. The built-in responder is also separate from the external sidecar responder; sidecar traffic does not pass through `MessageManager`.
