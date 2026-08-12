# Public API review

Review date: 2026-08-12. Candidate package: `Dreamine.SecsGem.Interop.Runtime` `1.0.0`. The final exported-type and member inventory is generated separately from the Release assembly as `PUBLIC_API.md`.

## Result

- The project is a provider-neutral Class Library and package candidate. It depends on Communication/SECS/GEM contracts and runtimes but has no reference to WPF. WPF consumes Runtime, never the reverse.
- Runtime consumes `ISecsMessageSession`, `ISecsPrimaryDispatcher`, and source-bound reply contexts. It does not create a competing receive loop, transaction manager, or System Bytes generator.
- Connection profiles, message-template catalogs, scenarios, responder configurations, persistence limits, evidence models, and wire-log models are explicit versioned public contracts. Collection-bearing values snapshot caller input where required by their contract.
- Existing source consumers retain the public types and signatures that predated packaging. Making the existing Class Library packable does not remove a public member. Because no published Runtime package compatibility baseline is asserted here, the conservative policy is still additive: do not remove a public type/member or reinterpret a persisted version-1 document without a migration decision and tests.
- Session lifecycle remains caller-owned. Profile changes that affect session construction require validate, diff, stop, and recreate. Responder registrations and delayed work are owned and drained by their manager. A pending manual reply is one-shot and remains bound to its source connection, Session ID, Stream, System Bytes, and connection epoch.
- JSON readers enforce document size, nesting, collection, schema, and version limits. Writers use the Runtime persistence boundary and atomic replacement rather than exposing partially written versioned documents.

## Logging, privacy, and evidence ownership

- Exact-wire logging is opt-in. Observation options must be applied before creating the session; `InteropWireLogSession` then consumes that session's bounded observation stream.
- `HeaderOnly` is the safe default and does not retain a message body. `Excluded` retains neither body nor raw frame. The safe sample facade rejects global `FullBodyExplicit`; approved full-body behavior requires a separate, bounded per-S/F policy outside that facade.
- Persistent records redact the endpoint and do not persist dynamic diagnostic text. This does not make logs public: timestamps, headers, SxFy, Session ID, System Bytes, segment paths, and operational timing can still be sensitive.
- The caller stops or disposes the session before stopping the recorder. `StopAsync` is idempotent. `InteropWireLogRunHealth.IsEvidenceEligible` requires zero source drops, zero recorder drops, a completed flush, and no writer failure.
- Evidence manifests and healthy local JSONL prove only the recorded local boundary. They are not standards certification, current-revision conformance, external-counterpart evidence, or field validation.

## Package pairing and source samples

The canonical full-workspace Host and Equipment sample projects deliberately use a source `ProjectReference` to Runtime. An isolated consumer instead uses `PackageReference` and a local-only feed containing the matching package graph:

1. `Dreamine.Communication.Abstractions`
2. `Dreamine.Communication.Core`
3. `Dreamine.Communication.Sockets`
4. `Dreamine.Secs.Abstractions`
5. `Dreamine.Secs.Com`
6. `Dreamine.Gem.Abstractions`
7. `Dreamine.Gem`
8. `Dreamine.SecsGem.Interop.Runtime`

The wider product smoke may also add the GEM300 packages, but Runtime does not depend on them. Newly built SECS/GEM/Runtime candidates currently reuse version `1.0.0`; they must not be mixed with older cached binaries carrying that version. Validation therefore uses an isolated package cache and checks that the consumer has no ProjectReference or source-tree path. This review does not authorize package publication or a versioning decision.

## Evidence status

Status values are used literally:

| Capability/evidence | Status | Boundary |
|---|---|---|
| Runtime source and package candidate before the final fresh run is attached | `IMPLEMENTED_UNVERIFIED` | Code and contracts exist; the central report owns the fresh build/test/pack result. |
| E37.1 conformance claim | `BLOCKED_STANDARD` | No local E37.1 normative source was available. |
| External simulator interoperability | `NOT_RUN` | Requires separately captured evidence from both endpoints. |
| Field/real-equipment verification | `NOT_RUN` | No field evidence is inferred from local loopback. |
| Legacy SECS-I | `INTENTIONALLY_EXCLUDED` | Runtime targets SECS-II over the project's single-selected HSMS implementation. |

A later `PASS` must cite a fresh command, exit code, configuration, timestamp, result count, and artifact in the central verification report. Code presence alone does not change these rows.

## Compatibility-sensitive next steps

| Classification | Proposal | Reason |
|---|---|---|
| Publication decision | Assign a coordinated package version to the matching SECS/GEM/Runtime set before external publication | Reusing `1.0.0` can select an older cached dependency with an incompatible public surface. |
| Persisted-format decision | Add a new schema version plus explicit migration when a version-1 shape must change | Silent reinterpretation would make saved profiles, catalogs, scenarios, or responder files unsafe. |
| Application policy | Keep credentials and global full-body logging outside built-in profiles | Both require deployment-specific authorization, secret storage, and retention policy rather than a permissive library default. |
