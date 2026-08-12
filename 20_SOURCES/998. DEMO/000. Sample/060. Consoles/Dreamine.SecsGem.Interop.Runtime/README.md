# Dreamine SECS/GEM Interop Runtime

`Dreamine.SecsGem.Interop.Runtime` is the reusable, provider-neutral layer used by the public console samples and WPF workbench. It provides bounded/versioned connection profiles, message-template catalogs, scenario execution, configurable Primary responders, evidence manifests, and persistent exact-wire logging.

## Capability boundary

- Connection Profile v1 validates credential-free endpoint, role, mode, non-live session settings, timer values, safety limits, reconnect policy, and a registered log-policy ID. A setting that belongs to session construction is changed through validate, diff, stop, and recreate rather than mutating a live session.
- Message Template Catalog v1 stores bounded immutable SECS item trees, direction, Primary/Secondary role, W-bit, and body-log policy. A catalog entry is application data, not a normative SxFy definition.
- Scenario v1 provides bounded run and step deadlines, cancellation, repeat limits, state waits, sends, expectations, and structured exit status. Dropped inbound messages make a run ineligible for success.
- Configurable Responder v1 owns exact dispatcher registrations and supports immediate, delayed, or deliberate no-reply behavior with bounded shutdown.
- Evidence and JSON persistence use explicit schema versions, input limits, stable snapshots, and atomic file replacement boundaries.
- Persistent wire logging consumes the session's actual wire-observation stream; it does not create a competing receive loop or present canonical re-encoding as captured wire.

The package consumes `ISecsMessageSession` and its dispatcher. It does not create a second HSMS session, transaction manager, System Bytes generator, or receive loop. Session ownership remains with the application.

## Package and source-build boundary

This project is a `Dreamine.SecsGem.Interop.Runtime` package candidate. Use it with the matching `Dreamine.Communication.Abstractions`, `Dreamine.Secs.Abstractions`, `Dreamine.Secs.Com`, `Dreamine.Gem.Abstractions`, and `Dreamine.Gem` candidates; the isolated local feed must also satisfy the Communication dependencies declared transitively by `Dreamine.Secs.Com`.

Inside the canonical full workspace, the public Host and Equipment sample projects deliberately use a `ProjectReference` to this source project. A separate application uses `PackageReference` instead. Do not mix a newly built Runtime or SECS/GEM `1.0.0` candidate with older cached binaries carrying the same version. Package validation uses a local-only feed and isolated package cache. This repository does not publish a package as part of that validation.

See the [public sample fixtures](../../../../100.%20Library/Secs.Com/samples/fixtures/README.md) and [WPF workbench](../../010.%20Wpfs/Dreamine.SecsGem.Interop.Wpf/README.md) for source examples.

## Safe persistent logging

Wire capture is opt-in and may contain sensitive application data. `HeaderOnly` is the safe default; `Excluded` retains no body or raw frame. The safe sample facade rejects a global `FullBodyExplicit` selection. Full-body storage requires separately approved per-S/F rules, bounded retention, and an application-owned storage location.

Create `InteropWireLogSessionOptions`, call `CreateObservationOptions()` while constructing the underlying session, then start `InteropWireLogSession` after the session exists. The application must stop or dispose the session before stopping the recorder so terminal producers cannot race queue completion. `StopAsync` is idempotent and finalizes the bounded JSONL sink. Treat a run as evidence only when `Health.IsEvidenceEligible` is true: both drop counters are zero, flush completed, and no writer failure was recorded.

The safe facade persists the endpoint as `redacted` and withholds dynamic diagnostic text. Header fields, timestamps, SxFy, Session ID, System Bytes, local segment paths, and operational timing can still be sensitive. Protect the product-owned log root and do not copy simulator, customer, or private-sidecar artifacts into a public package or repository.

## Evidence status

This README does not promote code presence to a test result. Until fresh build, test, pack, and isolated-consumer evidence is recorded in the central verification report, a package candidate is `IMPLEMENTED_UNVERIFIED`. An E37.1 conformance claim is `BLOCKED_STANDARD`; external simulator and field verification are `NOT_RUN`; legacy SECS-I is `INTENTIONALLY_EXCLUDED`. Local loopback or a healthy JSONL file is not certification, current-revision conformance, external interoperability, or field validation.

See [API_REVIEW.md](docs/API_REVIEW.md) for compatibility, ownership, persistence, and package-pairing decisions. The final exported-member inventory is generated separately from the Release assembly as `docs/PUBLIC_API.md`.
