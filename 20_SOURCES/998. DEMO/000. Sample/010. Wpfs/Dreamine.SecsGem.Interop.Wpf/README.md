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
