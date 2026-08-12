# Multi-Equipment Test Matrix

## Current status — 2026-08-12

| Evidence surface | Status | Boundary |
|---|---|---|
| Provider-neutral multi-equipment local regression | `PASS` | Final strict WPF regression/build is green; exact fresh totals belong in the central report. |
| Current standalone 1/2/10/50 headless command | `NOT_RUN` | No stale command output is presented as a current standalone run. |
| External SEComSimulator multi-equipment run | `NOT_RUN` | Concurrent-instance support and licensing were not established. |
| Real equipment / production network | `NOT_RUN` | No field evidence exists. |

The runner/export may contain operational result text such as `Passed` or `Failed`. Those strings report its internal assertion outcome only and are not external evidence statuses. The matrix below is explicitly historical.

## Historical Evidence — local synthetic run on 2026-08-10 only

The historical run started `2026-08-10T10:38:37.7927176+00:00` and completed `2026-08-10T10:38:51.3549447+00:00`. Every `PASS` below applies only to assertions against local synthetic TCP/HSMS peers in that dated run. It does not mean an external simulator, customer system, production network or real equipment passed.

| ID | Area | Historical assertion | Historical status | Dated evidence |
|---|---|---|---|---|
| ME-01 | Scale | 1, 2, 10 and 50 local peers connect/select, exchange the configured S1 pairs, Linktest and disconnect. | `PASS` | Four scale checks; maximum 50 peers. |
| ME-02 | GEM ownership | Communication state changes and `GemRuntime` ownership stay context-local. | `PASS` | Per-context divergence and distinct runtime instances. |
| ME-03 | Registration order | Reverse registration order is preserved while Connect All remains operational. | `PASS` | Reverse-order snapshot. |
| ME-04 | Message volume | Ten peers complete 100 S1F1/F2 broadcast rounds with correlation. | `PASS` | 1,000/1,000 historical burst replies. |
| ME-05 | Slow peer | A held response does not prevent a later request to another peer from completing first. | `PASS` | Deterministic response gate. |
| ME-06 | Collision isolation | Two connections use Session ID 0 and forced System Bytes 1 without cross-correlation. | `PASS` | Connection-scoped correlation. |
| ME-07 | Reconnect isolation | One of ten peers reconnects repeatedly with fresh connection identity while peers stay selected; three also reconnect concurrently. | `PASS` | 100 single-target plus 3 concurrent reconnects. |
| ME-08 | Pending transaction | Disconnect fails only the affected pending request; peer request completes and reconnect creates fresh state. | `PASS` | Selective-disconnect path. |
| ME-09 | HSMS state | Separation creates per-context selected/not-selected divergence without mutating the peer. | `PASS` | State isolation. |
| ME-10 | Automatic reconnect | Remote drop is observed and selection recovers with fresh connection/runtime while peer identity/state remains unchanged. | `PASS` | Automatic-reconnect recovery. |
| ME-11 | T3 | Non-responsive peer records T3 while a responsive peer remains selected and passes Linktest. | `PASS` | Injected T3 path. |
| ME-12 | Connect failure | Unavailable endpoint fails only its result while the available peer selects and passes Linktest. | `PASS` | Per-equipment aggregate result. |
| ME-13 | T6 | Withheld Select response reaches T6 while a healthy peer remains operational. | `PASS` | Injected T6 path. |
| ME-14 | T8 | Partial frame reaches T8 while a healthy peer remains operational. | `PASS` | Injected T8 path. |
| ME-15 | Malformed frame | Protocol error remains on its own context without stopping the healthy peer. | `PASS` | Raw fault peer. |
| ME-16 | Abrupt close | Closing one selected connection does not affect the healthy peer. | `PASS` | Raw fault peer. |
| ME-17 | Callback failure | Injected receive callback failure is recorded on the affected context while its peer stays usable. | `PASS` | Callback observation and last-error check. |
| ME-18 | Large inbound message | A 1 MiB Equipment→Host item is observed while a peer Linktest completes. | `PASS` | Length assertion and peer Linktest. |
| ME-19 | Cleanup | Instrumented live-session and background-operation deltas return to zero after disposal. | `PASS` | Historical counters both zero. |

### Historical totals

| Measure | Dated value | Interpretation |
|---|---:|---|
| Connections attempted | 98 | Scale, isolation, fault and reconnect setup attempts counted by that scenario version. |
| Messages requested / successful | 1,241 / 1,239 | Two non-success requests were deliberately injected: disconnected pending request and T3 request. |
| Timeout count | 3 | Deliberately injected T3, T6 and T8 paths. |
| Reconnect count | 105 | 100 one-of-ten, 3 concurrent, 1 selective and 1 automatic reconnect. |

These values are not a current regression total. Scenario implementation, provider construction and product code may have changed since the snapshot.

## Promotion rule

A fresh local run may be recorded as `PASS` only after its generated structured result, cleanup counters and source revision are captured by the final verification run. That status remains local automated evidence.

An external row may become `PASS` only after vendor/licensing prerequisites, distinct process/endpoint identities, both sides' finalized logs or screenshots, exact correlation, healthy no-drop/complete-flush evidence and manual review are recorded. Local synthetic results never promote an external or field row.
