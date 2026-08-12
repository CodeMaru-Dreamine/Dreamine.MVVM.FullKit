# 1.0.0 release-hardening evidence record

This document separates the current candidate boundary from a dated 2026-08-10 snapshot. It is not a publication, certification, current-revision conformance statement, or external interoperability claim. Fresh final totals are recorded only in `SECS_GEM_PRODUCTIZATION_REPORT.md`.

## Current candidate boundary — 2026-08-12

| Surface | Status | Evidence boundary |
|---|---|---|
| WPF/Interop Runtime local regression | `PASS` | Final strict WPF regression/build is green; exact fresh totals remain in the central report. |
| Connection Profile v1, Template Catalog v1, Scenario v1 and configurable responder | `PASS` | Shared provider-neutral runtime paths passed final local regression in UI/headless integration. |
| Persistent exact-wire logging and evidence privacy | `PASS` | Local regression covers the `HeaderOnly` default, bounded JSONL, closed export allow-list and health gating. |
| WPF visible validation in the current run | `BLOCKED_ENVIRONMENT` | Computer-use could not launch the app because app approval/elicitations were unavailable. |
| External SEComSimulator | `NOT_RUN` | Requires the consolidated operator procedure and both sides' evidence. |
| Frozen E30 Demo profile selector/responder surface | `IMPLEMENTED_UNVERIFIED` | The exact UI label and mutually exclusive bindings are implemented within the frozen scope. |
| Frozen E30 separate-process TCP evidence | `PASS` | The public Host/Equipment processes completed the actual local frozen-dialogue run. |
| E37.1 / GEM300 wire conformance | `BLOCKED_STANDARD` | Required `.1` normative material is unavailable; no mapping is guessed. |
| Legacy SECS-I | `INTENTIONALLY_EXCLUDED` | Candidate scope is HSMS/SECS-II. |

Operational UI/runtime enums such as `WaitingForUser`, `Passed`, `Failed` and `NotRun` are not release evidence vocabulary. They must be translated to the master literal statuses in the central report based on actual evidence.

## Current workbench architecture

- The workbench and public console samples consume the same provider-neutral `ISecsMessageSession` contract. They do not create a second receive loop, transaction manager or System Bytes generator.
- Connection Profile v1 validates credential-free endpoint/session/timer/safety/reconnect/log-policy data. Session-construction changes follow validate, diff, stop and recreate boundaries.
- Message Template Catalog v1 stores bounded immutable application templates; it does not claim normative message ownership.
- Scenario v1 provides bounded run/step deadlines, cancellation, repeats, state waits, sends, expectations and structured output. Cleanup failure, unhealthy logging or dropped inbound messages prevents a successful run result.
- Configurable Responder v1 owns exact dispatcher registrations and bounded shutdown. The WPF responder profile selector binds either the public E30 Demo router or the mutually exclusive Demo-only educational fallback.
- Multi-equipment contexts own independent sessions and provider-neutral runtime state. Registry replacement/removal disposes the old context; reconnect receives a fresh connection identity.

## Logging, privacy and evidence

Persistent logging consumes actual session wire observations. `HeaderOnly` is the safe default, `Excluded` retains no body/raw frame, and the safe facade rejects global full-body capture. The application stops/disposes the session before the recorder is finalized, so terminal producers do not race queue completion.

A run is eligible for evidence review only when observation and recorder drop counters are zero, flush completed and no writer failure exists. Public export is a closed allow-list: endpoints, free-form text, decoded bodies, raw frames, arbitrary check text and private-profile traffic are excluded or replaced with bounded categories. A manifest attachment alone never assigns external `PASS`; verified review, both sides' artifacts, hashes and a complete checklist are required.

## Frozen E30 Demo boundary

The default Equipment responder profile uses `E30DemoEquipmentProfile.Create()` and `E30EquipmentRouter`. It covers exactly the frozen 20-dialogue manifest. The alternative fallback covers only seven educational pairs and is explicitly Demo-only, not GEM. The two paths never register concurrently.

The separate-process `Dreamine.Gem.QuickStart` exercises the same Demo Profile with Host and Equipment roles. Its local evidence remains distinct from SEComSimulator or real-equipment evidence. S2F35 empty-list unlink/delete variants and S6F19/F20 are `BLOCKED_STANDARD`; excluded capability families remain `INTENTIONALLY_EXCLUDED`.

## Historical Evidence — 2026-08-10 only

The following snapshot is retained for traceability. It is not the current 2026-08-12 result and does not override the status table above.

| Historical gate | Historical status | Dated evidence |
|---|---|---|
| Six library Release suites | `PASS` | 222 tests were recorded in the 2026-08-10 snapshot. |
| Harness log-manager suite | `PASS` | 5 tests were recorded in that snapshot. |
| Single-equipment synthetic loopback | `PASS` | 1,000/1,000 replies, 100 Linktests, 100 reconnects, 20 concurrent primaries and zero timeouts were recorded. |
| Local package smoke | `PASS` | A FullKit local-feed consumer executed representative codec/GEM/GEM300 APIs in that dated run. |
| Parent solution build | `PASS` | The snapshot recorded 94 projects with zero warnings/errors. |
| Parent solution test | `FAIL` | 663 tests succeeded and one pre-existing Ontology freshness gate failed because its manifest was missing/stale. |
| Visible WPF fallback capture | `PASS` | UIAutomation/PrintWindow fallback captured normal/compact layouts. This historical method is not current computer-use evidence. |
| External simulator | `NOT_RUN` | No external product row was executed. |

Historical defect corrections recorded at that time included transaction-secondary correlation, immutable async parameter snapshots, reentrant spool purge safety, enum-boundary rejection, bounded/virtualized UI logging, current-versus-last error separation, and FullKit's `_._` meta-package layout. Subsequent source work may change API and package inventories, so the dated “no public signature change” statement is not a current assertion.

Historical performance measurements are isolated in [PERFORMANCE_BASELINE.md](PERFORMANCE_BASELINE.md). Licensed standards, simulator binaries and external/customer material must not be copied into source, packages, tests or public documentation.
