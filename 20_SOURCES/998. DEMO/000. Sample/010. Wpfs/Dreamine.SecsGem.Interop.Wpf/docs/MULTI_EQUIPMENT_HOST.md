# Multi-Equipment Host

Status: harness implementation and local loopback evidence as of 2026-08-10.

The Multi-Equipment Host lets one harness process coordinate independent HSMS/GEM connections to multiple equipment endpoints. It is an interoperability and isolation test surface. It is not a compliance certificate, a production fleet-management SDK, or evidence from an external simulator or real equipment.

## Boundary and ownership

The implementation does not change the public API of `Dreamine.Secs.Abstractions`, `Dreamine.Secs.Com`, `Dreamine.Gem.Abstractions`, or `Dreamine.Gem`. All orchestration types are `internal` to the WPF harness:

| Layer | Responsibility |
|---|---|
| Registry | Maintains the unique `EquipmentId` entries, provides immutable snapshots for fan-out work, and disposes removed or replaced contexts. |
| Factory | Creates one connection context for each equipment definition. |
| Context | Owns one `HsmsSession`, its connection state, one connection-scoped `GemRuntime`, cancellation, diagnostics, and log identity. |
| Host | Coordinates selected/all operations, bounded fan-out, configuration import/export, result aggregation, cancellation, and final disposal. |

Each connected context owns a separate `HsmsSession`. Consequently its transaction manager and `SystemBytes` generator are not shared with another equipment. A `GemRuntime` is also scoped to that context and is replaced when a new connection is established. Disconnecting, timing out, reconnecting, or disposing one context therefore does not intentionally mutate a peer context.

This dependency direction remains one-way: the harness references the SECS/GEM libraries; the libraries do not reference the harness orchestration.

## Identity and correlation

| Field | Scope and purpose |
|---|---|
| `EquipmentId` | Stable, operator-assigned logical key in the harness registry. It must be unique, case-insensitively. |
| `ConnectionId` | Harness-generated identity for a particular transport connection. A reconnect receives a fresh value. |
| `SessionId` | HSMS/SECS protocol session identifier from the equipment definition. Different connections may deliberately use the same value. |
| `SystemBytes` | Transaction identifier allocated and correlated inside the owning `HsmsSession`. Values may repeat on different connections without sharing transactions. |

Protocol correlation remains session-local. `EquipmentId` and `ConnectionId` are harness metadata used to select a context and make logs unambiguous; they are not added to a SECS message on the wire. The protocol log records `EquipmentId`, `ConnectionId`, endpoint, `SessionId`, direction, SxFy, and `SystemBytes`. The local isolation test deliberately sends two requests with `SessionId=0` and `SystemBytes=1` on different connections and verifies that each reply returns to the correct equipment context.

## Lifecycle and concurrency

- Add/Get/Remove/Replace operations are owned by the registry. Import replaces the configured set and disposes the previous contexts.
- A context serializes connect, disconnect, and reconnect transitions, links caller cancellation to its lifetime, and cancels an outstanding passive accept when it is disposed.
- Active automatic reconnect creates a fresh `ConnectionId` and `GemRuntime`, then restores HSMS selection. Automatic reconnect is rejected for Passive mode.
- The host allows one aggregate command at a time. Work within that command fans out with bounded concurrency. Defaults are 10 connects, 20 message operations, and 5 reconnects.
- Observable collection mutation is dispatched to the WPF dispatcher when necessary; network continuations are not moved into view code-behind.
- Host disposal cancels new/in-flight aggregate work, waits for tracked host work to become idle, and then asynchronously disposes every registered context and session.

The counters exposed by the self-test cover harness-scheduled equipment delegates, live harness/loopback sessions, selection-recovery work, and loopback peer background operations. They are not a count of every CLR `Task` or every internal runtime continuation.

## Credential-free JSON configuration

The UI can export and import schema version 1 JSON. It contains only:

- host fan-out limits (`ConnectConcurrency`, `MessageConcurrency`, `ReconnectConcurrency`);
- `EquipmentId`, host/address, port, Active/Passive mode, and `SessionId`;
- automatic reconnect selection and T3/T5/T6/T7/T8 values.

There are no username, password, token, certificate, or other credential fields. Import validates the schema version, unique equipment IDs, endpoint values, mode, session ID, timer ranges, and reconnect/mode combination before replacing the registry. Endpoint data can still be operationally sensitive, so configuration files should be handled according to the deployment environment's normal access policy.

## UI workflow

1. Select **Multi Equipment Host** as the host mode and open the **Multi Equipment** tab.
2. Add equipment definitions directly or use **Import Configuration**.
3. Use the Selected commands for one row, or Connect/Disconnect/Linktest/Scenario/Broadcast commands for the configured set.
4. Inspect TCP, HSMS, GEM, activity, and last-error state per row.
5. In **Protocol Log**, filter by equipment or free text and correlate entries using the equipment, connection, session, and system-byte columns.
6. Use **Export Configuration** to save connection settings without credentials.

**Run 1/2/10/50 Self-test** creates local synthetic loopback peers. It does not exercise the configured external endpoints and must not be reported as external interoperability evidence.

## Headless self-test

From a Release output directory:

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

Or from the project directory:

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release -- --multi-self-test --output multi-equipment-self-test.json
```

The command uses 100 one-of-ten reconnect cycles. Exit code `0` means the scenario result is `Passed` and no tracked session/background operation remains; `2` means the run completed with a failed assertion or cleanup result; `1` means the headless runner raised an unhandled error.

See [MULTI_EQUIPMENT_TEST_MATRIX.md](MULTI_EQUIPMENT_TEST_MATRIX.md) for asserted scenarios and [MULTI_EQUIPMENT_PERFORMANCE.md](MULTI_EQUIPMENT_PERFORMANCE.md) for the latest measured snapshot.

## External simulator preflight

Read-only local inspection did not establish vendor-supported concurrent simulator instances or more than one active equipment connection in a single simulator GUI. Consequently there is no approved external multi-equipment procedure or Passed result yet.

If vendor documentation and licensing later confirm concurrent instances, a person may prepare a **conditional** manual check by using isolated application/configuration/log locations, assigning separate loopback ports such as 7201 and 7202, and confirming two distinct processes reach Selected. The operator must then reconnect only one instance and verify that the other process remains Selected and continues S1F1/F2 and Linktest traffic. Record process identity, endpoint, timestamps, both sides' logs, and the unaffected-peer result. Until those prerequisites and observations are recorded, every external row remains **Not Run / Waiting for User**. See [SECOMSIMULATOR_INTEROP_TEST.md](SECOMSIMULATOR_INTEROP_TEST.md) for the existing single-connection evidence procedure.

## Current limitations

- Local loopback proves behavior within this implementation and process; it does not establish standards compliance, production readiness, or real-network capacity.
- The orchestration is harness-internal. There is intentionally no new multi-equipment public library API in this change.
- Configuration has no credential or TLS material model.
- Throughput and memory values are diagnostic comparison points, not SLAs; runtime warm-up, machine load, and loopback scheduling materially affect them.
- External SEComSimulator scenarios remain **Not Run / Waiting for User** until a person performs and records the external procedure. Local loopback results must not be substituted for that evidence.
