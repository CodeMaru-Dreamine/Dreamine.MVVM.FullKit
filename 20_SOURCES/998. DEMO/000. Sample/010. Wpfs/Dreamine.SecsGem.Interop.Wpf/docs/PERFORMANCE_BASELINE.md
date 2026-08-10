# Performance baseline

Measured 2026-08-10 on Windows 10.0.26200 x64, 16 logical processors, Release build, .NET 10.0.10 host (SDK 10.0.400-preview). These numbers are a local comparison point, not a UI or network SLA.

- 10,000 log additions: 15.542 ms total (1.554 µs/add in the headless collection path); retained count: exactly 10,000.
- Latest single-equipment self-loopback (2026-08-10 10:39 UTC): 1,000 S1F1/S1F2 correlations + 100 independent reconnect cycles + 100 linktests + 20 concurrent primaries: 0.839 s overall, 0.5884 ms primary P95, Passed.
- Multi-equipment loopback: see [MULTI_EQUIPMENT_PERFORMANCE.md](MULTI_EQUIPMENT_PERFORMANCE.md). Those 1/2/10/50-equipment values are a separate run and are not combined with the single-equipment baseline.
- Codec/frame micro-results are recorded in the SECS runtime `docs/PERFORMANCE_BASELINE.md`.

The visible WPF path additionally batches at most 250 pending entries per background dispatcher operation, uses recycling row/column virtualization, and supports pause/filter/auto-scroll. A paused view retains only the newest 10,000 pending records. The final Release UI was captured at 1366×768 and 1920×1080, plus 1093×614 and 900×500 logical viewports approximating the usable area under higher display scaling. The compact Multi-Equipment view exposes vertical scrolling, keeps horizontal scrolling disabled, and provides a full selected-equipment detail line when table cells are compressed. This fallback is layout evidence at the current 100% OS scale, not an assertion that Windows itself was switched to every DPI setting; responsiveness still depends on machine, GPU, theme, and actual DPI environment.
