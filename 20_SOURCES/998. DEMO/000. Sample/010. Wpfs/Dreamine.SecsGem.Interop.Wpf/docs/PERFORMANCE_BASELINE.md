# Performance evidence

## Current status — 2026-08-12

| Surface | Status | Boundary |
|---|---|---|
| Fresh WPF/Interop performance run | `NOT_RUN` | No current measurement is published here while final verification is in progress. |
| Visible WPF responsiveness/layout | `BLOCKED_ENVIRONMENT` | Computer-use could not launch the application because app approval/elicitations were unavailable. |
| External simulator/network capacity | `NOT_RUN` | Local loopback measurements cannot represent an external process, network or equipment. |

There is no UI, network, throughput, memory or production-capacity SLA in this document. A future comparison must record source revision, Release configuration, OS, runtime/SDK, CPU/logical processors, competing load, exact command, output artifact and UTC timestamps. Unlike environments must not be averaged.

The current source uses bounded queues/retention and UI batching, but implementation presence is not a performance result. The visible log path batches at most 250 pending entries per background dispatcher operation, uses row/column recycling virtualization, and retains at most the newest 10,000 paused entries.

## Historical Evidence — 2026-08-10 only

The values below are a dated local comparison snapshot, not the current 2026-08-12 result and not an SLA.

Environment recorded at the time: Windows `10.0.26200` x64, 16 logical processors, Release build, .NET host `10.0.10`, SDK `10.0.400-preview`.

| Historical measurement | Historical status | Dated value and scope |
|---|---|---|
| Headless log collection | `PASS` | 10,000 additions in 15.542 ms (1.554 µs/add); retained count exactly 10,000. |
| Single-equipment synthetic loopback | `PASS` | 1,000 S1F1/F2 correlations, 100 reconnect cycles, 100 Linktests and 20 concurrent primaries in 0.839 s; primary P95 0.5884 ms. |
| Multi-equipment synthetic loopback | `PASS` | A separate dated run is preserved in [MULTI_EQUIPMENT_PERFORMANCE.md](MULTI_EQUIPMENT_PERFORMANCE.md); its values are not combined with this row. |
| Visible WPF fallback captures | `PASS` | UIAutomation/PrintWindow fallback captured 1366×768, 1920×1080, 1093×614 and 900×500 logical layouts at the then-current 100% OS scale. This is historical fallback evidence, not current computer-use evidence and not proof of actual 125%/150% DPI operation. |
| External simulator/network | `NOT_RUN` | No external measurement was made. |

Historical compact-layout observations included vertical scrolling, no horizontal-scroll dependency and a selected-equipment detail line. They remain dated observations only; current visual validation is `BLOCKED_ENVIRONMENT` until the consolidated manual check is completed.

Codec/frame micro-results, if needed, are maintained by the SECS runtime and must be interpreted using their own dated environment and evidence boundary.
