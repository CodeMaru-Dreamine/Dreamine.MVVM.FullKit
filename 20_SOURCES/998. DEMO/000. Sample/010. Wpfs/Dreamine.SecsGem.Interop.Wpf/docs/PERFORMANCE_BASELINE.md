# Performance baseline

Measured 2026-08-10 on Windows 10.0.26200 x64, 16 logical processors, Release build, .NET 10.0.10 host (SDK 10.0.400-preview). These numbers are a local comparison point, not a UI or network SLA.

- 10,000 log additions: 15.542 ms total (1.554 µs/add in the headless collection path); retained count: exactly 10,000.
- Self-loopback: 1,000 S1F1/S1F2 correlations + 100 independent reconnect cycles + 100 linktests + 20 concurrent primaries: 1.129 s overall, 0.451 ms primary P95, Passed.
- Codec/frame micro-results are recorded in the SECS runtime `docs/PERFORMANCE_BASELINE.md`.

The visible WPF path additionally batches at most 250 pending entries per background dispatcher operation, uses recycling row/column virtualization, and supports pause/filter/auto-scroll. A paused view retains only the newest 10,000 pending records. Visual responsiveness still depends on machine/GPU/theme and must be checked manually at 1366×768 and 1920×1080.
