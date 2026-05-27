# Dreamine.PLC Package Family

Dreamine PLC is a modular PLC communication package family for C#/.NET industrial automation applications.

## Packages

| Package | Purpose | Status |
|---|---|---|
| `Dreamine.PLC.Abstractions` | Common PLC contracts | Ready |
| `Dreamine.PLC.Core` | InMemory client and shared simulator runtime | Ready |
| `Dreamine.PLC.Wpf` | WPF diagnostic monitor | Ready |
| `Dreamine.PLC.Mitsubishi.MC` | Mitsubishi MC TCP/UDP adapter and simulator | Simulator validated |
| `Dreamine.PLC.Omron.Fins` | Omron FINS TCP/UDP adapter and simulator | Simulator validated |
| `Dreamine.PLC.Mitsubishi.MxComponent` | MX Component adapter boundary | Vendor runtime required |
| `Dreamine.PLC.Omron.CxComponent` | CX-Compolet adapter boundary | Vendor runtime required |

## Current validation status

Validated:

- 1PC Simulator TCP read/write and handshake
- 2PC Simulator TCP read/write and handshake
- 1PC Mitsubishi MC TCP read/write and handshake
- 2PC Mitsubishi MC TCP read/write and handshake
- 1PC Mitsubishi MC UDP read/write and handshake
- 2PC Mitsubishi MC UDP read/write and handshake
- 1PC Omron FINS TCP read/write and handshake
- 2PC Omron FINS TCP read/write and handshake
- 1PC Omron FINS UDP read/write and handshake
- 2PC Omron FINS UDP read/write and handshake
- WPF monitor integration

Pending:

- Physical Mitsubishi PLC test
- Physical Omron PLC test
- Field-specific memory map verification
- Field-specific polling/write policy verification

## Mode matching rule

For simulator-based tests, server and client modes must match.

```text
SimulatorTcp ↔ SimulatorTcp
McTcp        ↔ McTcp
McUdp        ↔ McUdp
FinsTcp      ↔ FinsTcp
FinsUdp      ↔ FinsUdp
```

Cross-mode communication is expected to fail because each mode uses a different protocol.

## PC-to-PC firewall requirement

For PC-to-PC tests, open the inbound port on the server PC.

Example for `55000`:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
New-NetFirewallRule -DisplayName "Dreamine PLC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

PowerShell must be run as Administrator. Without this setting, 1PC tests can pass while 2PC tests fail.

## Physical PLC test requirement

Simulator validation is not a replacement for physical PLC validation.

Before production use, test against the target PLC model and verify:

- PLC communication setting
- TCP/UDP port
- Network routing and firewall
- Device memory area
- Node/network/unit address for FINS
- MC frame settings where applicable
- Polling interval
- Write safety policy
- Error recovery behavior

## Polling safety

1ms polling is for simulator stress testing only.

Do not use 1ms polling against physical PLCs.

Recommended defaults:

- Monitoring: 100ms to 500ms
- UI display refresh: 250ms to 1000ms
- Write: event-driven only

## Vendor runtime policy

Dreamine PLC packages do not redistribute vendor runtime DLLs.

Not included:

- Mitsubishi MX Component DLLs
- Omron CX-Compolet DLLs
- Omron SYSMAC Gateway runtime files
- Any vendor-licensed installer or runtime file

Users must install and license vendor software separately when using vendor runtime adapters.

## License

Dreamine source code: MIT License.

Vendor products and trademarks belong to their respective owners.
