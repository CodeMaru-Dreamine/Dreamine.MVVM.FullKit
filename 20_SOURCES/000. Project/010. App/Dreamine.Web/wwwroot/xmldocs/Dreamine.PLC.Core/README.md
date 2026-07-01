# Dreamine.PLC.Core

[Korean documentation](./README_KO.md)

Core runtime utilities for Dreamine PLC communication.

This package provides vendor-neutral runtime components such as in-memory PLC simulation, common address parsing support, simulator server/client infrastructure, and shared logic used by protocol adapters and WPF diagnostics.

## Features

- InMemory PLC client
- Shared PLC memory model
- Dreamine TCP simulator server/client
- Basic read/write command flow
- Cross-PC simulator test support
- Shared runtime utilities for PLC packages

## Architecture

```text
Dreamine.PLC.Wpf / SampleSmart
        ↓
Dreamine.PLC.Abstractions
        ↓
Dreamine.PLC.Core
        ↓
Protocol Adapters / Simulators
```

`Dreamine.PLC.Core` remains vendor-neutral. Mitsubishi MC and Omron FINS protocol details belong to their own packages.

## Simulator protocol

The built-in Dreamine simulator is a simple text-based protocol used for diagnostics and framework validation.

Example command flow:

```text
WRITE_WORDS D100 100,200,300,400
READ_WORDS D100 4
WRITE_BITS M10 1,0,1,0
READ_BITS M10 4
```

This simulator is not the Mitsubishi MC protocol and is not the Omron FINS protocol.

## Mode matching rule

When testing PC-to-PC, the server mode and client mode must match.

```text
SimulatorTcp ↔ SimulatorTcp
McTcp        ↔ McTcp
McUdp        ↔ McUdp
FinsTcp      ↔ FinsTcp
FinsUdp      ↔ FinsUdp
```

A `SimulatorTcp` server cannot communicate with an `McTcp`, `McUdp`, `FinsTcp`, or `FinsUdp` client.

## PC-to-PC firewall requirement

For PC-to-PC tests, the server PC must allow the inbound port used by the selected protocol.

Example for port `55000`:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
New-NetFirewallRule -DisplayName "Dreamine PLC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

Run PowerShell as Administrator. Without this firewall rule, local 1PC tests may pass while 2PC tests fail.

## Physical PLC warning

The built-in simulator is for development and stress testing only.

- 1ms polling is allowed only for simulator stress tests.
- Do not use 1ms polling against physical PLCs.
- Recommended physical PLC monitoring interval: 100ms to 500ms.
- Recommended UI display refresh interval: 250ms to 1000ms.
- PLC writes should be event-driven, not constant polling writes.

## Validation status

Validated with:

- 1PC Simulator TCP read/write and handshake
- 2PC Simulator TCP read/write and handshake
- Shared WPF monitor flow

Physical PLC testing is handled by each protocol package.

## License

MIT License.
