# Dreamine.PLC.Mitsubishi.MC

[Korean documentation](./README_KO.md)

Mitsubishi MC protocol adapter for Dreamine PLC communication.

This package provides Mitsubishi MC TCP/UDP client support and built-in MC protocol simulator servers for local and PC-to-PC validation.

## Features

- Mitsubishi MC TCP client
- Mitsubishi MC UDP client
- MC TCP simulator server
- MC UDP simulator server
- Binary 3E frame-based read/write flow
- Word read/write diagnostics
- Repeated handshake validation flow
- Timeout and retry support for UDP
- Integration with `IPlcClient`

## Supported simulator test modes

The SampleSmart PLC Protocol page supports:

```text
McTcp ↔ McTcp
McUdp ↔ McUdp
```

The server and client modes must match. A `SimulatorTcp` server cannot be used with an `McTcp` or `McUdp` client.

## 1PC test

Use this flow for local validation.

```text
Mode: McTcp or McUdp
Host: 127.0.0.1
Port: 55000
Start Server
Use Client
Connect
Write Words
Read Words
Run Handshake
```

## 2PC test

Server PC:

```text
Mode: McTcp or McUdp
Host: 0.0.0.0
Port: 55000
Start Server
```

Client PC:

```text
Mode: same as server
Host: server PC IP
Port: 55000
Use Client
Connect
Read/Write or Handshake
```

## Firewall requirement for PC-to-PC tests

Open the inbound port on the server PC.

For TCP:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC MC TCP 55000" -Direction Inbound -Protocol TCP -LocalPort 55000 -Action Allow
```

For UDP:

```powershell
New-NetFirewallRule -DisplayName "Dreamine PLC MC UDP 55000" -Direction Inbound -Protocol UDP -LocalPort 55000 -Action Allow
```

Run PowerShell as Administrator. Without these rules, the same application can pass 1PC tests but fail 2PC tests.

## Physical PLC test notice

The built-in MC simulator verifies the Dreamine MC client/server flow, but physical Mitsubishi PLC testing must still be performed.

Before connecting to a real Mitsubishi PLC, verify:

- PLC model and Ethernet module support
- MC protocol TCP/UDP setting
- Port number
- Device memory mapping
- Binary/ASCII frame setting if applicable
- Network firewall and routing
- Safe polling interval

## Polling and write safety

Do not use 1ms polling against a physical PLC.

Recommended physical PLC values:

- Monitoring: 100ms to 500ms
- UI display refresh: 250ms to 1000ms
- Write: event-driven only
- Handshake stress test: simulator only unless explicitly approved for a real machine

## Vendor runtime policy

This package does not include Mitsubishi MX Component or any Mitsubishi runtime DLL.

This package implements MC protocol communication directly. MX Component integration, if needed, must remain in a separate adapter package without redistributing vendor DLLs.

## Validation status

Validated:

- 1PC MC TCP read/write and handshake
- 1PC MC UDP read/write and handshake
- 2PC MC TCP read/write and handshake
- 2PC MC UDP read/write and handshake
- WPF monitor integration

Pending:

- Physical Mitsubishi PLC validation

## License

MIT License.
