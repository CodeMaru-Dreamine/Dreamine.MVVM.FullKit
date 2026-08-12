# Dreamine SECS/GEM Interoperability Workbench

This .NET 8 WPF application is a provider-neutral workbench for the public Dreamine SECS-II/HSMS session contract. It can apply bounded Connection Profile v1 documents, load Message Template Catalog v1 and Scenario v1 documents, run configurable Primary responders, inspect finalized exact-wire JSONL, and assemble privacy-bounded evidence. It is an engineering and interoperability tool, not a compliance certificate.

## Current evidence boundary

| Surface | Status | Boundary |
|---|---|---|
| Local automated Workbench regression | `PASS` | The final strict WPF regression/build is green; exact fresh totals belong in the central productization report. |
| Visible WPF validation in the current run | `BLOCKED_ENVIRONMENT` | The approved computer-use path could not launch the application because app approval/elicitations were unavailable. Historical fallback captures are not current visual evidence. |
| SEComSimulator interoperability | `NOT_RUN` | Both Dreamine and counterpart evidence must be reviewed before any external `PASS`. |
| Frozen E30 Demo selector/responder surface | `IMPLEMENTED_UNVERIFIED` | The exact selector and mutually exclusive bindings are implemented; this is not a conformance status. |
| Frozen E30 Demo separate-process smoke | `PASS` | The public Host and Equipment roles completed the exact frozen-dialogue TCP run; this is local evidence only. |
| E37.1 conformance | `BLOCKED_STANDARD` | The required licensed revision is unavailable; no conformance claim is inferred. |

The UI model contains operational values such as `InteropScenarioStatus.WaitingForUser` and `InteropScenarioStatus.Passed`. They are workflow/runtime enum values only and must not be copied into a release evidence table as status vocabulary. Release evidence uses only the literal statuses shown in the central report.

## Run

```powershell
dotnet run --project Dreamine.SecsGem.Interop.Wpf.csproj -c Release
```

The external simulator starts only when the operator chooses **Launch Simulator — manual action**.

## Workbench flow

1. Apply a credential-free Connection Profile v1. Session-construction changes are reported as recreate-required instead of mutating a live session.
2. Optionally load a Message Template Catalog v1. Templates are bounded application data, not normative SxFy definitions.
3. Connect/select one native `ISecsMessageSession`; the workbench does not create a competing receive loop, transaction manager, or System Bytes allocator.
4. Optionally load and run Scenario v1 in the UI, or use the same runner headlessly:

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --scenario scenario.json --profile profile.json --output result.json
```

`--scenario` and `--profile` are required; `--output` is optional. The runner enforces bounded deadlines, repeat limits and cancellation. A cleanup failure, unhealthy wire log, or dropped inbound message prevents a successful run result.

The optional output is a closed public summary: status, exit code, UTC timestamps, step-status counts, dropped-inbound count, and a bounded reason code. It deliberately omits profile/scenario paths, endpoints, step identifiers, free-form provider errors, message IDs, System Bytes, and bodies. Detailed in-process results remain available to the UI/runtime; do not use the public headless summary as a diagnostic dump.

## Equipment responder profiles

The **Equipment responder profile** selector is disabled while a responder is active, so exactly one profile owns the dispatcher registrations.

- **E30-0611 derived subset profile v1 (Demo)** is the default. It uses the public `E30DemoEquipmentProfile.Create()` and `E30EquipmentRouter` path. Its scope is the frozen 20-dialogue subset only; it is not current-revision conformance or external validation.
- **Educational basic responder (Demo-only, not GEM)** is the intentionally small fallback. It demonstrates seven pairs: S1F1/F2, S1F3/F4, S1F11/F12, S1F13/F14, S1F15/F16, S1F17/F18 and S2F17/F18. Add application semantics in a separate Equipment Profile; do not represent this fallback as GEM.

Responder ownership is rebound to a replacement session after reconnect and disposed with the old binding. The E30 and educational paths are mutually exclusive.

The separate-process Host/Equipment example for all frozen E30 dialogues is in [`Dreamine.Gem.QuickStart`](../../../../100.%20Library/Gem/samples/Dreamine.Gem.QuickStart). Its local result is distinct from external simulator evidence.

## Exact-wire logging, privacy and evidence health

Persistent wire logging consumes the session's real wire-observation stream. `HeaderOnly` is the safe default; `Excluded` retains no body/raw frame. The safe facade rejects global full-body capture. Stop or dispose the session before finalizing the recorder so terminal observations cannot race queue completion.

Evidence is eligible for review only when both drop counters are zero, flush completed, and no writer failure exists. The public export uses a closed allow-list: endpoints, free-form text, raw frames, decoded bodies, arbitrary checks and private-profile traffic are omitted or withheld. Header/timing identifiers can still be operationally sensitive, so keep the log root access-controlled.

Attaching an evidence manifest never assigns external `PASS`. Manual verification, a healthy Dreamine log, counterpart log or screenshot, artifact hashes, and a completed checklist are all required before review.

## Local self-tests and multi-equipment mode

```powershell
Dreamine.SecsGem.Interop.Wpf.exe --self-test --output self-test.json
Dreamine.SecsGem.Interop.Wpf.exe --multi-self-test --output multi-equipment-self-test.json
```

These commands use local synthetic peers. They can provide local automated evidence only and cannot promote an external row. See [Multi-Equipment Host](docs/MULTI_EQUIPMENT_HOST.md), its [test matrix](docs/MULTI_EQUIPMENT_TEST_MATRIX.md), and the [historical performance snapshot](docs/MULTI_EQUIPMENT_PERFORMANCE.md).

For the one-time external procedure and evidence checklist, see [SEComSimulator Interoperability Test](docs/SECOMSIMULATOR_INTEROP_TEST.md) and the [Interoperability Test Matrix](docs/INTEROP_TEST_MATRIX.md).
