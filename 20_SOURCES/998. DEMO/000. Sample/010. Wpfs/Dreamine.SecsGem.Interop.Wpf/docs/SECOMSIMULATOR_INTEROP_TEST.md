# SEComSimulator Interoperability Test

## Scope and evidence boundary

The harness targets .NET 8 and uses only public Dreamine APIs. The installed product name and supplied installation context identify the simulator as 4.0; the executable did not expose a usable file/product version, so the revision was not independently verified. No vendor executable, DLL, message file, scenario file, screenshot or manual is copied into this repository.

The installation was inspected read-only. Its configuration and default examples show Host/Equipment and Active/Passive fields and XML message/scenario data. No official manual was present. These observations guide setup but are not normative evidence. No SEMI revision is asserted by this document.

## Automated result

Run on 2026-08-10:

- Release build: Passed, 0 warnings, 0 errors.
- Local S1F1/S1F2: 1,000/1,000 passed; 0 timeout.
- HSMS Linktest: 100/100 passed.
- Independent connect/select/dispose cycles: 100/100 passed.
- Concurrent Primary correlation: Passed (20 simultaneous transactions).
- Existing SECS/GEM/GEM300 tests: 203/203 passed.
- UI rendering: 1366×768 and 1920×1080 passed; 125% and 150% effective-DIP layouts were rendered and inspected without horizontal-scroll dependency. Narrow-height sidebars and Advanced Settings use vertical scrolling.
- External simulator tests: **Not Run**.

The local result does not establish compatibility with an external product.

## Mode B — Simulator Host / Dreamine Equipment

1. In the harness sidebar set `Role = Equipment`, `Mode = Passive`, `Host / Bind = 127.0.0.1`, `Port = 7000`, and the agreed Session/Device ID (initial test: `0`).
2. In **Advanced Settings**, verify timers and click **Launch Simulator — manual action**. This is the only harness action that starts the external process.
3. In the simulator open **Configure** (or **Configurate**) → **Connection**. Select HSMS, enable **HOST Mode**, choose **Active**, set **Remote IP = 127.0.0.1**, **Remote Port = 7000**, and the same Device ID. Set T3/T5/T6/T7/T8 to the agreed values.
4. Click harness **Connect** first. It should enter Listening/Disconnected-not-selected semantics without being marked failed.
5. Click simulator **Start** or **Connect**. Confirm the simulator reports `HSMS Connected` and then `HSMS SELECTED`.
6. Click harness **Enable Equipment Responder**. From the simulator send the confirmed S1 scenarios below.
7. Verify direction, SxFy, System Bytes correlation, decoded item and raw frame in **Protocol Log**. Export masked JSON/Markdown from **Advanced Settings**.

Until steps 3–7 are completed by a user, Mode B remains `Waiting for User` / `Not Run`.

## Mode C — Dreamine Host / Simulator Equipment

Proceed only if the installed simulator configuration exposes Equipment/Local mode.

1. In the simulator clear **HOST Mode**, choose **Passive**, set **Local Port = 7000**, the agreed Device ID and timers, then click **Start**.
2. In the harness set `Role = Host`, `Mode = Active`, `Host / Bind = 127.0.0.1`, `Port = 7000`, and the same Session ID.
3. Click **Connect**, **Select**, then **Linktest**.
4. Use **Messages** to send the confirmed S1 primaries and compare the correlated secondaries.
5. If Equipment/Passive is absent, record `Not Supported by Tested Simulator Configuration`; do not infer success.

Mode C is currently **Not Run**.

## Confirmed basic message shapes

These shapes are limited to the installed default examples and the local E30 material; they are not a general conformance claim.

| Exchange | Primary body | Expected secondary body |
|---|---|---|
| S1F13/S1F14 | Host-initiated empty list; equipment-initiated identity list | List containing one-byte COMMACK and an identity list; accepted sample ACK is `B[0]` |
| S1F1/S1F2 | No item | List of ASCII model and software revision |
| S1F3/S1F4 | List of confirmed status-variable IDs (sample uses I2) | List of corresponding values |
| S1F11/S1F12 | Empty or requested-ID list as supported | List of ID, name and units definitions |
| S1F15/S1F16 | No item | One-byte OFLACK; accepted sample value is `B[0]` |
| S1F17/S1F18 | No item | One-byte ONLACK; accepted sample value is `B[0]` |

Do not invent bodies for other messages merely because they appear in the simulator UI.

## Pass criteria

A scenario passes only when the expected TCP/HSMS state, SxFy, W-bit, Session ID and System Bytes correlation are visible in captured evidence. A timeout, malformed body, unexpected ACK, disconnect or unsupported configuration is recorded with the wire log. Predicted behavior is never recorded as an actual result.

## Known limitations

- Simulator GUI automation is out of scope.
- External HSMS, S1F13/F14, S1F1/F2, online/offline and reconnect results are Not Run.
- Fault-peer T3/T6/T7/T8 and malformed-frame behavior is covered by library regression tests, but external-product behavior is Not Run.
- GEM300 wire handlers not implemented by the libraries are not represented as passed.
- No library or Communication public API was changed by this harness work.
