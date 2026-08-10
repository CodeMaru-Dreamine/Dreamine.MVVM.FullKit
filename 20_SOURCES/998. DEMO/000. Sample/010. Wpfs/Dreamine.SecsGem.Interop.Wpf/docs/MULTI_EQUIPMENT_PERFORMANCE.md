# Multi-Equipment Performance Snapshot

This page records one local headless loopback run. It is a reproducible comparison point, not a network, UI, production-capacity, or compliance SLA.

- Run started: `2026-08-10T10:38:37.7927176+00:00`
- Run completed: `2026-08-10T10:38:51.3549447+00:00`
- Elapsed wall time: approximately `13.562 s`
- Result: `Passed`
- Source snapshot: `multi-equipment-self-test-verified.json` (local generated artifact; not committed)

The JSON does not embed machine, OS, CPU, SDK, runtime, build hash, or load metadata. Record those separately before comparing a future run.

## Measured values

| Metric | Value | Exact measurement scope |
|---|---:|---|
| Maximum equipment | 50 | Largest local loopback fleet created by the scale sweep. |
| Connections attempted | 98 | Scenario counter across scale, isolation, failure, and reconnect setup. |
| Messages requested / passed | 1,241 / 1,239 | Scenario counters. The two non-success requests are expected injected isolation paths, not unexplained loss. |
| Timeout count | 3 | Injected T3, T6, and T8 cases. |
| Reconnect count | 105 | All explicit and automatic reconnect cases counted by the scenario. |
| Timed scale connection rate | 594.1782 connections/s | `63 / sum(connection elapsed for 1, 2, 10, 50 fleets)`. It excludes other scenario connection attempts. |
| 1,000-message burst rate | 164.0928 messages/s | `1,000 / elapsed` for the ten-equipment × 100-round S1F1/F2 block only. |
| Minimum response latency | 0.1839 ms | Successful S1F1/F2 results collected during scale sweeps and the 1,000-message burst. |
| Average response latency | 6.2926 ms | Same sample set; it includes the deliberately slower loopback peer in the ten-equipment fleet. |
| Maximum response latency | 62.5623 ms | Same sample set. |
| Process private-memory delta | +60,268,544 bytes (about 57.48 MiB) | Process `PrivateMemorySize64` after forced GC minus the value before the full scenario. Includes runtime/JIT/allocation effects and is not retained-object analysis. |
| Remaining sessions | 0 | Delta of harness context and loopback peer live-session counters after cleanup. |
| Remaining background operations | 0 | Delta of selection-recovery/peer background counters plus tracked host operations after cleanup. |
| Tracked equipment operation tasks | 1,449 | Cumulative equipment delegates scheduled by bounded host fan-out. It is not the total number of CLR tasks. |
| Peak concurrent equipment operations | 16 | Highest simultaneous bounded equipment delegates observed by a host in this run. |

## Scale connection timing

| Equipment count | Aggregate connect/select elapsed |
|---:|---:|
| 1 | 74.2717 ms |
| 2 | 8.6066 ms |
| 10 | 5.0442 ms |
| 50 | 18.1063 ms |

The one-equipment case includes first-use warm-up in this run, so the table must not be treated as a monotonic scaling curve. All endpoints are local loopback peers and do not model network latency, packet loss, external process scheduling, TLS, or equipment processing time.

## Reproduce and refresh

Run a Release build from the project directory:

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release -- --multi-self-test --output multi-equipment-self-test.json
```

A comparison record should include the generated JSON, UTC run timestamps, source revision, build configuration, OS/runtime/SDK, CPU and logical processor count, and whether competing workload was present. Update this page from the new JSON rather than averaging unlike environments.

The test fails cleanup when its tracked session or background-operation delta is non-zero. A zero value is evidence for the instrumented Harness/loopback scope only; it is not proof that every CLR allocation, task, socket state, or native resource in the process was enumerated.

External SEComSimulator status remains **Not Run / Waiting for User**. These local performance values are not external interoperability evidence.
