# Multi-Equipment Performance Evidence

## Current status — 2026-08-12

| Surface | Status | Boundary |
|---|---|---|
| Fresh multi-equipment performance run | `NOT_RUN` | No current measurement has been recorded by the final verification run. |
| External simulator/network capacity | `NOT_RUN` | No external process, network or real equipment was measured. |
| Production capacity/SLA | `NOT_APPLICABLE` | This document contains diagnostic local comparison data only. |

Bounded concurrency, counters and cleanup checks passed the final local functional regression. That `PASS` is not itself a throughput or memory result; fresh performance execution remains `NOT_RUN`.

## Historical Evidence — local synthetic run on 2026-08-10 only

This snapshot records one dated local headless loopback run. It is not the current 2026-08-12 result and is not a network, UI, production-capacity or compliance SLA.

- Start: `2026-08-10T10:38:37.7927176+00:00`
- Completion: `2026-08-10T10:38:51.3549447+00:00`
- Elapsed wall time: approximately `13.562 s`
- Historical status: `PASS`
- Historical generated artifact: `multi-equipment-self-test-verified.json` (local artifact; not committed)

The JSON did not embed machine, OS, CPU, SDK, runtime, build hash or competing-load metadata. That limitation makes the values suitable only for preserving the dated observation, not for normalized comparison.

### Dated measurements

| Metric | Dated value | Exact historical scope |
|---|---:|---|
| Maximum equipment | 50 | Largest synthetic fleet in the scale sweep. |
| Connections attempted | 98 | Scenario counter across scale, isolation, failures and reconnect setup. |
| Messages requested / successful | 1,241 / 1,239 | Two expected injected non-success paths, not unexplained loss. |
| Timeout count | 3 | Injected T3, T6 and T8 paths. |
| Reconnect count | 105 | Explicit and automatic reconnect cases counted by that scenario version. |
| Timed scale connection rate | 594.1782 connections/s | `63 / sum(connect elapsed for 1, 2, 10, 50 fleets)`; excludes other attempts. |
| 1,000-message burst rate | 164.0928 messages/s | Ten synthetic peers × 100 S1F1/F2 rounds only. |
| Minimum / average / maximum latency | 0.1839 / 6.2926 / 62.5623 ms | Successful S1F1/F2 samples in scale sweeps and the burst; includes a deliberately slower peer. |
| Process private-memory delta | +60,268,544 bytes (~57.48 MiB) | `PrivateMemorySize64` after forced GC minus before full scenario; includes runtime/JIT/allocation effects. |
| Remaining instrumented sessions | 0 | Workbench context and synthetic-peer counter delta after cleanup. |
| Remaining instrumented background operations | 0 | Reconnect/peer/host-operation counter delta after cleanup. |
| Tracked equipment delegates | 1,449 | Cumulative delegates scheduled by bounded host fan-out; not total CLR tasks. |
| Peak tracked equipment concurrency | 16 | Highest simultaneous bounded equipment delegates observed. |

### Dated scale timing

| Equipment count | Aggregate connect/select elapsed |
|---:|---:|
| 1 | 74.2717 ms |
| 2 | 8.6066 ms |
| 10 | 5.0442 ms |
| 50 | 18.1063 ms |

The one-peer case included first-use warm-up, so the table is not a monotonic scaling curve. All endpoints were in-process/local synthetic peers; they do not model external scheduling, network latency/loss, TLS or equipment processing time.

## Reproduce a future comparison

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release -- --multi-self-test --output multi-equipment-self-test.json
```

A new record must include the generated JSON, UTC timestamps, source revision, Release configuration, OS/runtime/SDK, CPU/logical processors, competing load and exact command. Replace the current `NOT_RUN` only from that complete record; do not average it with this incomplete historical environment.

Zero instrumented counters are evidence only for the explicit Workbench/synthetic-peer scope. They do not prove that every CLR allocation, task, socket state or native resource was enumerated. External SEComSimulator and field performance remain `NOT_RUN`.
