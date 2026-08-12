# SEComSimulator Interoperability Test

## Scope and current status

| Evidence surface | Status | Boundary |
|---|---|---|
| External SEComSimulator interoperability | `NOT_RUN` | No Dreamine/counterpart evidence pair has been reviewed in the current run. |
| Visible WPF validation | `BLOCKED_ENVIRONMENT` | Computer-use could not launch the app because app approval/elicitations were unavailable. |
| Frozen E30 Demo responder surface | `IMPLEMENTED_UNVERIFIED` | `E30-0611 derived subset profile v1`; implementation scope is frozen and is not conformance evidence. |
| Frozen E30 separate-process TCP evidence | `PASS` | The public Host and Equipment processes completed the actual local frozen-dialogue run. |
| E37.1 conformance | `BLOCKED_STANDARD` | The required licensed revision is unavailable. |

The installed product name and supplied installation context identify the simulator as 4.0, but its executable did not expose a usable file/product version. No vendor executable, DLL, message/scenario file, screenshot or manual is copied into this repository. Read-only observations of Host/Equipment and Active/Passive fields guide setup only; they are not normative evidence.

The UI values `WaitingForUser` and `Passed` are operational `InteropScenarioStatus` enum members. They do not replace the evidence statuses above. The root run will request the visual and simulator actions once, together, after automated work completes.

## Before the external run

1. Use only loopback endpoints and an agreed Session/Device ID unless a separate environment owner authorizes another endpoint.
2. Select the exact responder profile before enabling it. The default is **E30-0611 derived subset profile v1 (Demo)**. The alternative **Educational basic responder (Demo-only, not GEM)** is not GEM.
3. Keep persistent logging at `HeaderOnly` unless a separately approved per-S/F full-body policy exists. Do not store customer/private-sidecar data.
4. Prepare an evidence manifest/checklist and product-owned output folder. A healthy Dreamine log plus a counterpart log or screenshot is required; one-sided evidence cannot produce external `PASS`.
5. Execute only message rows that the installed simulator demonstrably supports. A menu entry is not proof of a compatible body shape.

## Mode B — Simulator Host Active / Dreamine Equipment Passive

1. In the workbench set `Role = Equipment`, `Mode = Passive`, `Host / Bind = 127.0.0.1`, `Port = 7000`, and the agreed Session/Device ID.
2. Choose the responder profile while it is disabled, then click **Enable Equipment Responder**. The selector is locked while active and the binding is restored on a replacement session after reconnect.
3. Verify timers in **Advanced Settings**, then use **Launch Simulator — manual action** if needed. This is the only workbench action that starts the external process.
4. In the simulator open **Configure** (or **Configurate**) → **Connection**. Select HSMS, enable **HOST Mode**, choose **Active**, set `Remote IP = 127.0.0.1`, `Remote Port = 7000`, the same Device ID, and agreed T3/T5/T6/T7/T8 values.
5. Click workbench **Connect** first. Passive listening/disconnected-not-selected is not itself a failure.
6. Click simulator **Start** or **Connect** and confirm TCP connected and HSMS selected in both applications.
7. Send only the supported Primary messages for the selected profile. Confirm direction, SxFy, W-bit, Session ID, System Bytes correlation and typed body/ACK on both sides.
8. Exercise disconnect/reconnect, confirm the responder is rebound once, and repeat a correlated exchange.
9. Disconnect/stop the session before finalizing the exact-wire recorder. Confirm zero observation drops, zero recorder drops, completed flush and no writer failure.
10. Hash the finalized Dreamine and counterpart artifacts, complete the checklist, and perform manual review. Until all steps are evidenced, every Mode B row remains `NOT_RUN`.

## Mode C — Dreamine Host Active / Simulator Equipment Passive

Proceed only if the installed simulator exposes Equipment/Local mode.

1. In the simulator clear **HOST Mode**, choose **Passive**, set `Local Port = 7000`, the agreed Device ID and timers, then click **Start**.
2. In the workbench set `Role = Host`, `Mode = Active`, `Host / Bind = 127.0.0.1`, `Port = 7000`, and the same Session ID.
3. Click **Connect**, **Select**, then **Linktest**.
4. Load a bounded Message Template Catalog v1/Scenario v1 or use the structured editor for only confirmed message shapes.
5. Verify both sides' correlation and evidence-health conditions exactly as in Mode B.
6. If Equipment/Passive is absent, retain the observation and leave Mode C `NOT_RUN`; do not infer compatibility.

Mode C is `NOT_RUN`.

## Frozen E30 Demo dialogue boundary

The exact profile name is `E30-0611 derived subset profile v1`. The public Demo profile freezes these 20 dialogue definitions:

| Direction family | Included dialogues |
|---|---|
| Host-request/Equipment-response | S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18; S2F13/F14, S2F15/F16, S2F17/F18, S2F29/F30, S2F31/F32, S2F33/F34, S2F35/F36, S2F37/F38, S2F41/F42; S5F3/F4, S5F5/F6; S6F15/F16 |
| Equipment-primary/Host-response | S5F1/F2, S6F11/F12 |

Some communication/time exchanges may also be equipment-initiated where the public Host client explicitly registers that direction. Direction must be observed, never inferred. S2F35 empty-list unlink/delete variants and S6F19/F20 are `BLOCKED_STANDARD`; multi-block, trace, limits, spooling and other capability families outside the frozen profile are `INTENTIONALLY_EXCLUDED`.

The educational fallback covers only S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18 and S2F17/F18. It is Demo-only and must not be reported as GEM.

## External `PASS` criteria

A row may become `PASS` only when all of the following are true:

- expected TCP/HSMS state and reconnect behavior are visible;
- direction, SxFy, W-bit, Session ID and System Bytes correlation match;
- the body and typed ACK match the selected profile without mutation on rejection;
- the exact-wire health gate is eligible;
- Dreamine and counterpart artifacts are finalized, hashed and manually reviewed; and
- the result is recorded in the central report without expanding the profile scope.

Timeout, malformed body, unexpected ACK, disconnect, unsupported configuration, dropped observation or incomplete flush is retained as evidence and does not become `PASS`. Local loopback, the separate-process QuickStart, or a UI workflow value cannot promote an external row.

## Historical Evidence — 2026-08-10 only

The old document recorded local synthetic loopback counts and UIAutomation/PrintWindow fallback captures from 2026-08-10. Those items are dated historical comparison evidence, not current visual or external evidence. External simulator status on that date was `NOT_RUN`.
