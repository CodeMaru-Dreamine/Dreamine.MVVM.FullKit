# Interoperability Test Matrix

Current status date: 2026-08-12. Code presence is not promoted to `PASS`, and no in-progress aggregate test count is recorded here. Fresh final evidence belongs in `SECS_GEM_PRODUCTIZATION_REPORT.md`.

The workbench uses operational enum values including `WaitingForUser`, `Passed`, `Failed` and `NotRun`. Those names describe UI/runtime state only. They are not release evidence statuses; this matrix uses the master literal status vocabulary.

## Current matrix

| ID | Peer / surface | Scenario | Status | Evidence boundary / next action |
|---|---|---|---|---|
| A-01 | Dreamine local Active Host ↔ Passive Equipment | TCP, HSMS Select, Linktest, S1 correlation and reconnect | `PASS` | Final strict local regression is green; exact fresh totals are recorded centrally. |
| A-02 | Profile/Template/Scenario workbench | Connection Profile v1, Message Template Catalog v1, Scenario v1, cancellation and structured output | `PASS` | Final local regression exercises the shared provider-neutral Runtime. |
| A-03 | Exact-wire evidence | Native session observations, bounded JSONL, `HeaderOnly` default, cleanup and health gate | `PASS` | Verified local evidence requires zero observation/recorder drops, completed flush, and no writer failure. |
| A-04 | Privacy export | Closed allow-list and private-profile exclusion | `PASS` | Regression covers the closed DTO, endpoint redaction, and omission of free text, raw frames, decoded bodies and arbitrary checks. |
| A-05 | WPF visible layout | Launch and inspect final Release UI | `BLOCKED_ENVIRONMENT` | Computer-use could not launch because app approval/elicitations were unavailable. Root will consolidate the manual visual check with the external procedure. |
| A-06 | Dreamine multi-equipment local loopback | 1/2/10/50 endpoints, correlation/fault/reconnect/resource isolation | `PASS` | Final local regression is green; this remains synthetic/local evidence. |
| G-01 | WPF Equipment responder | `E30-0611 derived subset profile v1` Demo selector and mutually exclusive Demo-only fallback | `IMPLEMENTED_UNVERIFIED` | Selector contract and bindings are implemented; this is not a conformance or external result. |
| G-02 | Separate Dreamine Host/Equipment processes | All frozen E30 subset dialogues over TCP | `PASS` | Public `Dreamine.Gem.QuickStart` completed the actual two-process local run; full evidence is central. |
| B-01 | Simulator Host Active ↔ Dreamine Equipment Passive | Connect, Select and Linktest | `NOT_RUN` | Follow the EN/KO external procedure and retain both sides' evidence. |
| B-02 | Same as B-01 | Basic S1 communication/identity/status exchanges supported by the selected Demo profile | `NOT_RUN` | Counterpart support and message shape must be confirmed before execution. |
| B-03 | Same as B-01 | Frozen E30 subset dialogue selected for the run | `NOT_RUN` | Run only rows the installed simulator demonstrably supports. |
| B-04 | Same as B-01 | Disconnect/reconnect and repeated traffic | `NOT_RUN` | Do not infer this result from local loopback. |
| C-01 | Dreamine Host Active ↔ Simulator Equipment Passive | Basic HSMS and confirmed messages | `NOT_RUN` | Execute only if the simulator exposes Equipment/Local mode. |
| D-01 | Dreamine Passive ↔ Simulator Active | Active/passive cross-check | `NOT_RUN` | Requires operator evidence from both applications. |
| D-02 | Dreamine Active ↔ Simulator Passive | Active/passive cross-check | `NOT_RUN` | Requires operator evidence from both applications. |
| F-01 | External fault peer | T3/T6/T7/T8 and partial/malformed frame | `NOT_RUN` | Local regression evidence cannot promote external behavior. |
| G3-01 | External simulator / real equipment | GEM300 wire scenarios | `BLOCKED_STANDARD` | Required `.1` normative material is unavailable; no wire mapping is guessed. |
| X-01 | Legacy SECS-I | SECS-I transport interoperability | `INTENTIONALLY_EXCLUDED` | Product scope is HSMS/SECS-II. |

## Historical Evidence — 2026-08-10 only

The following is retained only as a dated comparison point. It is not the current 2026-08-12 result and cannot override any current status above.

| Historical surface | Historical status | Dated observation |
|---|---|---|
| Single-equipment local loopback | `PASS` | 1,000/1,000 S1F1/S1F2 replies, 100 Linktests, 100 independent reconnect cycles and 20 concurrent primaries were recorded on 2026-08-10. |
| Multi-equipment synthetic loopback | `PASS` | The dated artifact recorded the ME-01..ME-19 assertions; see `MULTI_EQUIPMENT_TEST_MATRIX.md`. |
| Seven listed regression projects | `PASS` | The dated document recorded 237 tests. This is historical and is not the fresh final aggregate. |
| Visible WPF fallback capture | `PASS` | A prior UIAutomation/PrintWindow fallback produced layout captures. That method is historical only and does not satisfy the current computer-use visual check. |
| External SEComSimulator | `NOT_RUN` | No external row was executed in that run. |

## External `PASS` rule

An external row may become `PASS` only after the expected TCP/HSMS state, direction, SxFy, W-bit, Session ID, System Bytes correlation and typed body/ACK are visible, exact-wire health is eligible, and both Dreamine and counterpart artifacts are hashed and manually reviewed. A Dreamine-only log, local loopback, UI workflow value, or prediction is insufficient.

No current external simulator scenario has `PASS`. Therefore this workbench must not be described as certified, fully conformant, field-validated, or validated on real equipment.
