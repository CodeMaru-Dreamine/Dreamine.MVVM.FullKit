# Multi-Equipment Host

## Current boundary

| Surface | Status | Boundary |
|---|---|---|
| Provider-neutral orchestration local regression | `PASS` | Final strict WPF regression/build is green; exact fresh totals are recorded only in the central report. |
| Standalone `--multi-self-test` command in the current run | `NOT_RUN` | A historical 2026-08-10 command run is retained in the matrix; regression coverage remains local/synthetic. |
| External SEComSimulator multi-equipment interoperability | `NOT_RUN` | Vendor support/licensing for concurrent instances was not established. |
| Production fleet SDK | `NOT_APPLICABLE` | The orchestration remains an internal Workbench/Runtime composition, not a new public fleet API. |

The Multi-Equipment Host coordinates independent Host-role HSMS/GEM connections to several equipment endpoints. It is an interoperability and isolation surface, not a compliance certificate, capacity SLA, production fleet-management SDK, or evidence from an external simulator/real equipment.

UI/runtime strings such as `Passed`, `Failed` or `WaitingForUser` are operational values only. They do not replace the evidence statuses above.

## Boundary and ownership

The orchestration was moved from WPF-specific session construction into the shared provider-neutral Runtime layer. It consumes `ISecsMessageSession` and validates the provider's `SecsConnectionIdentity`; no orchestration type is promoted as a public fleet API.

| Layer | Responsibility |
|---|---|
| Registry | Maintains case-insensitively unique `EquipmentId` entries, exposes stable snapshots, and disposes removed/replaced contexts. |
| Provider factory | Creates one Host-role message-session provider for each equipment definition and validates role, mode and Session ID. |
| Context | Owns one message session, connection-scoped `GemRuntime`, cancellation, reconnect state, diagnostics and log identity. |
| Host | Coordinates selected/all commands, bounded fan-out, credential-free configuration, cancellation and final disposal. |

Each connected context owns an independent `ISecsMessageSession`. Its transaction manager and System Bytes allocator therefore remain connection-scoped. The connection-scoped `GemRuntime` is discarded when a connection closes and recreated for a replacement session. Disconnect, timeout, reconnect or disposal of one context is not intended to mutate a peer context.

The dependency direction is one-way: WPF consumes the shared Runtime and public SECS/GEM abstractions; those product libraries do not reference WPF.

## Identity and correlation

| Field | Scope |
|---|---|
| `EquipmentId` | Stable operator-assigned registry key; unique without regard to case. |
| `ConnectionId` | Identity of one transport connection; reconnect creates a fresh value. |
| `SessionId` | HSMS/SECS identifier from the definition; different connections may use the same value. |
| `SystemBytes` | Allocated/correlated by the owning session; the same numeric value may appear independently on another connection. |

`EquipmentId` and `ConnectionId` are Workbench metadata and are not added to the SECS wire message. Protocol correlation remains session-local. Logs include enough connection/header metadata to distinguish independent contexts, but endpoint/header/timing data may still be operationally sensitive.

## Lifecycle and concurrency

- Registry add/remove/replace owns context lifetime. Import validates all replacement definitions before switching the registry and disposes the prior contexts.
- Context connect/disconnect/reconnect transitions are serialized. Disposal cancels a pending Passive accept and any lifetime-linked operation.
- Active automatic reconnect uses a bounded coordinator, creates a new connection identity/session/GEM runtime and restores selection. Passive automatic reconnect is rejected.
- One aggregate host command runs at a time; work inside it uses bounded connect/message/reconnect concurrency.
- Observable collection notifications are marshalled through the configured synchronization context without moving network work into view code-behind.
- Host disposal rejects new work, cancels lifetime-linked operations, waits for tracked aggregate work, then disposes all contexts and the owned reconnect coordinator. Cleanup failures remain observable.

The self-test counters cover only explicitly instrumented Workbench contexts, synthetic peers, reconnect work and host delegates. They are not a count of every CLR task, socket state or allocation.

## Credential-free configuration

Schema version 1 JSON contains fan-out limits, `EquipmentId`, host/address, port, Active/Passive mode, Session ID, automatic-reconnect choice and T3/T5/T6/T7/T8 values. It has no username, password, token, certificate or secret field.

Import validates schema version, unique IDs, endpoints, modes, session values, timer ranges and reconnect/mode combinations before registry replacement. Endpoint/configuration data can still be sensitive; store it under deployment access controls and do not include customer/private-sidecar configurations in a public package or repository.

## UI workflow

1. Select **Multi Equipment Host** and open **Multi Equipment**.
2. Add definitions or import a validated configuration.
3. Use a Selected command for one context or bounded All/Broadcast commands for the set.
4. Inspect per-context TCP, HSMS, GEM, responder/activity and last-error state.
5. Filter Protocol Log by equipment/free text and correlate equipment, connection, session and System Bytes columns.
6. Export the credential-free configuration only to an approved location.

**Run 1/2/10/50 Self-test** creates local synthetic peers. It does not touch configured external endpoints and cannot promote any external row.

## Headless self-test

From a Release output directory:

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

Or from the project directory:

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release -- --multi-self-test --output multi-equipment-self-test.json
```

Exit code `0` means the runner's own assertions and instrumented cleanup conditions succeeded; `2` means an assertion/cleanup condition failed; `1` means an unhandled runner error. The exported `Passed`/`Failed` text is an operational result field, not an external interoperability status.

See [MULTI_EQUIPMENT_TEST_MATRIX.md](MULTI_EQUIPMENT_TEST_MATRIX.md) for scope and [MULTI_EQUIPMENT_PERFORMANCE.md](MULTI_EQUIPMENT_PERFORMANCE.md) for the dated historical measurement.

## External preflight

Read-only inspection did not establish vendor-supported concurrent SEComSimulator instances, licensing for them, or multiple active equipment connections in one GUI. Therefore no approved external multi-equipment result exists and the status remains `NOT_RUN`.

If vendor documentation and licensing later confirm concurrent instances, the operator must use isolated configuration/log locations and unique loopback ports, prove two distinct processes are selected, reconnect only one, and show the unaffected peer remains selected while exchanging correlated traffic. Retain process identities, endpoints, timestamps and both sides' finalized evidence. The root run will consolidate any authorized manual request; this document does not create a separate request.

## Limitations

- Local synthetic peers verify only the instrumented implementation; they do not establish standards conformance, production readiness or network capacity.
- Configuration has no credential/TLS-material model.
- The context currently composes a basic connection-scoped `GemRuntime`; a frozen E30 Equipment Profile selection is a separate single-equipment responder surface.
- Throughput and memory values are diagnostic comparison points, not SLAs.
- External simulator and real-equipment rows remain `NOT_RUN` until both sides' evidence is reviewed.
