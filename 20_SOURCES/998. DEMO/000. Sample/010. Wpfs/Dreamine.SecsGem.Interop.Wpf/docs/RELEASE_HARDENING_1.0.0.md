# 1.0.0 release-hardening record

Status date: 2026-08-10. This is source/package-candidate evidence, not a publication, certification, or external interoperability claim.

## Gate summary

| Gate | Result | Evidence |
|---|---|---|
| NuGet.org-only consumption (7 IDs) | Blocked | NuGet V3 flat-container returned `NOT_FOUND` for all IDs. No local feed was substituted. |
| Six library Release tests | Passed | 222/222 |
| Harness log-manager tests | Passed | 5/5 |
| Headless self-loopback | Passed | 1,000/1,000 replies, 100 linktests, 100 reconnects, 20 concurrent primaries, timeout 0 |
| Quick Start builds/runs | Passed | SECS Active/Passive two-connection pair, GEM services, GEM300 workflow (29 events) |
| Doxygen EN/KO | Passed | 12 builds, 0 logged warnings; Doxygen emits a tool-level notice that Korean templates have not been updated since 1.8.15 |
| Local package smoke | Passed | FullKit restored from local feed only; codec/GEM/GEM300 representative APIs executed |
| Parent solution Release build | Passed | 94 projects; 0 warnings, 0 errors |
| Parent solution test | Existing failure | 663 passed; one pre-existing Ontology freshness gate failed because `manifest.json` is missing/older than seven days. After the final spool edge-case addition, the relevant suite is 227/227 and was rerun separately. |
| External simulator | Not Run | Remains Waiting for User |
| Automated visible WPF UI control | Blocked | Computer-use initialization failed with local Codex-app path `EPERM`; Release build/XAML/tests were still completed |

## Actual defects corrected

1. A transaction could be consumed by an unrelated higher even secondary function. Correlation now accepts only F0 or the exact `primary F + 1` secondary.
2. GEM and GEM300 asynchronous command/action handlers observed mutable caller dictionaries. Parameters are now copied into read-only snapshots.
3. Reentrant spool purge during a sender callback could corrupt/dequeue an already-cleared queue. Drain now removes only the same head it delivered.
4. Undefined numeric connection/domain enum values reached state machines. Options and affected GEM300 commands now reject them at the boundary.
5. Harness logging used a 5,000-entry limit and dispatcher work per event. It now retains 10,000 entries, drains in 250-entry batches, bounds the paused queue, virtualizes rows/columns, and exposes filter/pause/auto-scroll.
6. Current error and historical last error were conflated. A successful operation clears `CurrentError`; `LastError` remains available in the status tooltip.
7. FullKit described itself as a meta package but contained a dummy DLL. It now uses `lib/net8.0/_._` and carries no runtime assembly.

## API and architecture

Generated inventories cover 156 exported types across the six assemblies (52 + 16 + 29 + 15 + 35 + 9). No public signature, package ID, target framework, or binary surface was intentionally changed. Runtime changes are non-breaking validation/race corrections.

Breaking proposals are deferred in each repository's `docs/API_REVIEW.md`: an explicit HSMS session abstraction/async handler boundary, typed spool result and lifecycle ownership, explicit aborted Control Job state, and repository-level cross-manager Process Job ownership.

Reference direction remains acyclic:

`Gem300 -> Gem300.Abstractions -> Gem.Abstractions -> Secs.Abstractions -> Communication.Abstractions`

Implementations point toward their abstractions; abstraction projects do not reference implementations.

## Package and CI boundary

The seven local 1.0.0 candidates contain expected nuspec/readme/icon assets. Six component packages contain their net8.0 DLL/XML documentation; FullKit contains six exact 1.0.0 dependencies and no DLL. No new NuGet version was published.

No existing per-repository GitHub Actions convention was found. Standalone submodule repositories currently contain sibling `ProjectReference` paths, so a workflow added to only one checkout would not restore. CI should be introduced only with a coordinated multi-repository checkout or an agreed package-consumption migration; no knowingly broken workflow was added.

## Verification categories

- **Unit Tested:** codec boundaries/fuzz, transactions/timers, state machines, GEM/GEM300 services and workflows.
- **Self Loopback Tested:** Active/Passive HSMS, Select, Linktest, S1F1/S1F2 correlation, reconnect and responder rebind.
- **SEComSimulator Interoperability Tested:** Not Run in this hardening run.
- **Experimental:** GEM300 workflow coordinator.
- **Not Implemented:** complete GEM/GEM300 wire mapping, SECS-I, persistence/recovery, multi-session/general-session behavior.
- **Blocked by unavailable normative document:** scopes explicitly marked Blocked in the existing requirement traces; no wire number or service code was guessed.

Performance measurements are in [PERFORMANCE_BASELINE.md](PERFORMANCE_BASELINE.md). Licensed standards and external/customer material were not copied into source, packages, tests, or documentation.
