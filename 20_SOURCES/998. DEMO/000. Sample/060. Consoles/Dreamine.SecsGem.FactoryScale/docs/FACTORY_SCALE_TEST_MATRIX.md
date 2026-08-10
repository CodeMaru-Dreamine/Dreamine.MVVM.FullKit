# Factory-Scale Test Matrix

## Evidence status rules

This matrix separates implemented capability from executed evidence. A checked-in code path is not a passed test.

| Status | Use in this matrix |
|---|---|
| `Passed` | The exact row was executed and every stated acceptance check passed. |
| `Experimental` | A small engineering smoke exercised the path; it is not capacity or release-qualification evidence. |
| `Pending` | The implementation or qualifying execution remains to be completed. |
| `Not Run` | The procedure exists or is planned, but no result was executed for this release draft. |
| `Waiting for User` | External configuration or physical equipment operation is required. |

JSON run outcome and evidence maturity are separate. A row written as “`Experimental` — run `Passed`” passed its executable assertions, but its maturity remains an engineering smoke and it must not be promoted to capacity or release-qualification evidence.

Results apply only to the recorded mode, build, machine, duration, workload, process count, and equipment count. Do not promote one row from `Experimental` or `Passed` to another row by inference.

## Capability matrix

| Area | In-process | Multi-process | Evidence status | Notes |
|---|---|---|---|---|
| Strict CLI and exit-code mapping | Implemented | Implemented | FactoryScale test suite `Passed` | Unknown, duplicate, missing, and invalid options are rejected before execution. |
| Independent Equipment contexts | Implemented | Implemented | In-process 100/250/500/1,000 and multi-process 250 `Passed` | Session, transactions, System Bytes, GEM runtime, and `ConnectionId` are per Equipment. |
| Real TCP loopback | Implemented | Implemented | Exact staged and multi-process rows below `Passed` | Multi-process uses endpoint manifests from passive worker fleets. |
| Bounded message and receive queues | Implemented | Implemented | One-hour and bounded-load rows `Passed` | Host/export use `Wait`; Equipment receive uses explicit counted `Reject`; diagnostic drops are counted separately. |
| Per-equipment/global pending admission | Implemented | Implemented | Normal/busy/trace/large/reconnect rows `Passed` | Limits are independent of worker count. |
| Reconnect storm | Implemented | Implemented at worker granularity | In-process 10/50/100/250 and MP 500×100 `Passed` | Recovery, unaffected peers, peak concurrency, and final cleanup were checked. |
| Host runtime restart | Implemented | OS Host-child restart implemented | 20 Equipment / 2 workers `Experimental` — run `Passed` | Multi-process retains workers and runs two fresh Host-only processes. |
| Fault injection | Implemented locally | Remote behavior IPC not implemented | Multi-process `Pending` | Multi-process `fault-isolation` must not report success without remote fault control. |
| Worker identity/manifest validation | N/A | Implemented | Executed multi-process rows and FactoryScale tests `Passed` | Run, worker, PID, count, path containment, duplicate IDs, and endpoints are validated. |
| Graceful stop and kill fallback | N/A | Implemented | Graceful paths `Passed`; forced fallback remains failure evidence | Only supervisor-owned child trees may be terminated. |
| Atomic bounded result export | Implemented | Implemented | FactoryScale tests and recorded snapshot exports `Passed` | Host and worker process evidence remains distinct. |

“Implemented” in this table describes the code path only. It is not a test result.

## Executed multi-process evidence

| ID | Mode | Scenario | Equipment / workers | Observed scope | Status |
|---|---|---|---:|---|---|
| MP-SMOKE-250 | Multi-process | `factory-smoke` | 250 / 5 | 250/250 Selected, 502/502 application pairs, zero timeout/failure/correlation error, five graceful zero-resource worker exits. | `Passed` |
| MP-RECONNECT-500-100 | Multi-process | `reconnect-storm` | 500 / 5 | 100/100 endpoints recovered, peak reconnect 16/16, 1,001/1,001 application pairs, Host and five-worker cleanup zero. | `Passed` |
| EXP-HR-020 | Multi-process | `host-restart` | 20 / 2 | Two distinct Host PIDs, all 20 `ConnectionId` values replaced, fresh traffic and zero Host/worker cleanup. | `Experimental` — run `Passed` |

The first two rows are passed only for their exact counts, worker partition, build, machine, and self-loopback workload. The Host-restart row remains an engineering smoke rather than capacity or soak evidence even though its run status is `Passed`. None is external interoperability evidence.

## Scale sweep

The following exact in-process scale stages were executed. Unexecuted multi-process counts remain `Not Run` and must not be inferred from another row.

| ID | Mode | Equipment count | Minimum checks | Status |
|---|---|---:|---|---|
| SCALE-100-IP | In-process | 100 | 100/100 Selected; 203/203 pairs; timeout/failure/correlation 0; cleanup operations/sockets 0 | `Passed` |
| SCALE-100-MP | Multi-process | 100 | Worker readiness, manifest integrity, Host checks, per-worker cleanup | `Not Run` |
| SCALE-250-IP | In-process | 250 | 250/250 Selected; 503/503 pairs; timeout/failure/correlation 0; cleanup operations/sockets 0 | `Passed` |
| SCALE-250-MP | Multi-process | 250 | 250/250 Selected; 502/502 pairs; 5 workers graceful with zero resources | `Passed` as `factory-smoke` |
| SCALE-500-IP | In-process | 500 | 500/500 Selected; 1,003/1,003 pairs; timeout/failure/correlation 0; cleanup operations/sockets 0 | `Passed` |
| SCALE-500-MP | Multi-process | 500 | Exact reconnect workload is recorded separately; generic scale row | `Not Run` |
| SCALE-1000-IP | In-process | 1,000 | 1,000/1,000 Selected; 2,003/2,003 pairs; timeout/failure/correlation 0; cleanup operations/sockets 0 | `Passed` |
| SCALE-1000-MP | Multi-process | 1,000 | Idle-factory acceptance across supervised workers | `Not Run` |

No maximum supported Equipment count is established by these placeholders.

## Workload profiles

| ID | Profile / scenario | Planned shape | Required observations | Status |
|---|---|---|---|---|
| LOAD-IDLE-1000 | `idle-factory` | 1,000 Equipment, 1 h | 1,000/1,000 Selected; 2,000/2,000 application baseline pairs; 61,000 idle plus 1,000 baseline Linktest exchanges; timeout/failure/correlation 0 | `Passed` |
| LOAD-NORMAL-500 | `factory-normal` | 500 Equipment, 1 h | 1,799,999 workload pairs; 1,800,999 final pairs; last interval 499.9985256759284 pair/s; P95/P99 15.02/15.62 ms | `Passed` |
| LOAD-BUSY-100 | `factory-busy` | 100 Equipment, 1 min | 59,987 workload pairs; 60,187 final pairs; last interval 999.2392142731878 pair/s; P95/P99 14.94/15.52 ms | `Passed` |
| LOAD-TRACE-20 | `trace-burst` | 20 Equipment, 1 min | 119,988 workload pairs; 120,028 final pairs; last interval 1,998.8966852619521 pair/s; P95/P99 14.81/15.35 ms | `Passed` |
| LOAD-LARGE-1M | `large-message` | 16 concurrent 1,048,576-byte bodies | 56/56 final pairs; 16,778,664 bytes sent; P95 38.42 ms; transaction loss 0 | `Passed` |
| LOAD-LARGE-MAX | `large-message` | 4 concurrent 16,777,202-byte bodies | 12/12 final pairs; 67,109,112 bytes sent; P95/P99 260/260 ms; transaction loss 0 | `Passed` |
| LOAD-RECONNECT-500 | `reconnect-storm` | In-process groups 10/50/100/250 | Every attempt succeeded; each run 1,001/1,001 pairs; timeout/failure/correlation 0; peak reconnect 10/16/16/16 | `Passed` |
| LOAD-RECONNECT-500-MP | `reconnect-storm` | 500 Equipment / 5 workers, restart 100 | 100/100 recovered; 1,001/1,001 pairs; peak reconnect 16/16; five graceful worker cleanups | `Passed` |
| LOAD-HOST-RESTART | `host-restart` | 20 Equipment / 2 retained workers | New Host PID and every `ConnectionId`; no inherited state; both Host runs and worker cleanup zero | `Experimental` — run `Passed` |
| LOAD-FAULT | `fault-isolation` | In-process, 6 Equipment | 15 requests/12 responses; expected timeout 2, failure 3, failed Equipment 1; healthy peer continued; correlation 0 | `Passed`; multi-process remote injection `Pending` |

The rate values above are measured application Primary/pair rates. JSON `messagesPerSecond` is request rate plus response rate, normally twice the pair rate for a 1:1 exchange. The current runner preserves the last non-zero interval in sustained-profile final results; the WPF reader falls back for earlier JSON whose terminal no-delta rate was zero. Final totals and interval meaning must remain together. HSMS control frames are included in byte counters but excluded from these application message and latency counters.

## Protocol and isolation matrix

| ID | Condition | Expected evidence | Status |
|---|---|---|---|
| ISO-SB | Same System Bytes value on different connections | Each response correlates only inside its `ConnectionId`; no cross-completion | `Passed` in staged scale rows |
| ISO-SESSION | Same Session ID on many connections | All Equipment remain independent and addressable | `Passed` in staged scale rows |
| ISO-ONE-DROP | One Equipment disconnects | Other connected/selected peers and their requests remain healthy | `Passed` in reconnect rows |
| ISO-TIMEOUT | One Equipment does not respond | Only its transaction times out; pending permits return to baseline | `Passed` in in-process fault row |
| ISO-CALLBACK | One callback/diagnostic sink fails | Failure is counted; transport and unrelated peers remain usable | `Passed` in in-process fault row |
| ISO-CLOSE | Peer closes before response | Request fails deterministically; reconnect/cleanup remains bounded | `Passed` in in-process fault row |
| PROTO-SELECT | Connect and HSMS Select | Requested peer count reaches expected Selected count | `Passed` through exact 1,000-equipment in-process and 500-equipment multi-process rows |
| PROTO-LINKTEST | Linktest across selected peers | Every intended response is correlated; no unintended T6 | `Passed`; Idle-1000 completed 62,000 total control exchanges |
| PROTO-S1F1 | S1F1/S1F2 baseline | Request/response and body validation per Equipment | `Passed` in all recorded baseline rows |
| PROTO-S1F13 | S1F13/S1F14 baseline | Communication establishment response and per-peer accounting | `Passed` in all recorded baseline rows |

## Process-supervision matrix

| ID | Condition | Required result | Status |
|---|---|---|---|
| PROC-READY | Worker ready within timeout | Identity-matched ready plus validated manifest | `Passed` in recorded multi-process rows |
| PROC-STALE | Stale/mismatched control files | Rejected; never treated as current readiness | FactoryScale tests `Passed` |
| PROC-STDIO | Child emits stdout and stderr | Both streams drained concurrently; retained tails stay within bounds | FactoryScale tests and recorded graceful runs `Passed` |
| PROC-GRACEFUL | Cooperative stop | Result written, exit normal, kill fallback false | `Passed` for 5-worker smoke/reconnect and 2-worker Host restart |
| PROC-TIMEOUT | Child ignores stop or completion deadline | Owned child tree terminated after bounded wait; failure recorded | FactoryScale tests `Passed`; no successful scale row used forced kill |
| PROC-CANCEL | Parent receives cancellation | Stop propagated, children reaped, exit code 130 at CLI boundary | FactoryScale tests `Passed` |
| PROC-DISPOSE-RACE | Multiple callers dispose coordinator | Every caller observes the same completed cleanup | FactoryScale tests `Passed` |
| PROC-WORKER-CLEANUP | Worker exits | Sessions/listeners/operations/queue/open sockets all 0 | `Passed` for exact 250/5 and 500/5 rows |
| PROC-HOST-CLEANUP | Host child exits | Pending/control operations/sessions and open sockets all 0 | 20/2 Host-restart `Experimental` — run `Passed` |

## Duration matrix

| Duration tier | Intended purpose | Status |
|---|---|---|
| 5–10 min | CI/local bounded smoke | Not separately qualified as a duration tier; shorter busy/trace/load rows passed only for their exact durations |
| 1 hour | Standard local soak | Idle-1000 and Normal-500 exact runs `Passed`; no generalization to other profiles or modes |
| 6 hours | Extended engineering soak | `Not Run` |
| 24 hours | Production-candidate observation | `Not Run` |

The detailed procedure is in [FACTORY_SCALE_SOAK.md](FACTORY_SCALE_SOAK.md).

## External evidence

| Peer | Automation boundary | Status |
|---|---|---|
| External Simulator | The runner does not launch or operate it. A user configures both sides and records masked evidence. | `Not Run / Waiting for User` |
| Real Equipment | Requires approved lab access, safe operating procedure, and user-controlled execution. | `Not Run / Waiting for User` |

Customer identities, internal specifications, raw external paths, and proprietary message content must not appear in source, test names, documentation, or committed evidence. External observations may inspire generalized tests but are not normative SEMI evidence.

Qualified zero-socket cleanup currently requires the Windows PID TCP-table provider. An unavailable provider is reported as unavailable and fails multi-process worker cleanup acceptance; it is never substituted with zero.

## Build, regression, and artifact boundary

| Verification | Recorded result |
|---|---|
| FactoryScale tests | 42/42 passed |
| WPF integration tests | 19/19 passed |
| SECS/GEM Core tests | 222 passed |
| Whole-solution tests | 664 passed; one already-known Ontology failure remains and is not reported as a pass |
| Release builds | FactoryScale and WPF both 0 warnings, 0 errors |
| Public/package surface | Public SECS/GEM Core and NuGet APIs, package versions, tags, and releases unchanged |

Raw final JSON, periodic snapshots, worker/control manifests, and process logs remain local evidence outside committed source regardless of file size. This matrix contains reviewed summaries only. Every Passed row is self-loopback evidence, not SEMI compliance, certification, or external-equipment compatibility evidence.

## Acceptance gate for a future result

A row may move to `Passed` only when its result bundle identifies the build and environment and shows all applicable checks, including:

1. requested versus connected and selected counts;
2. request, response, timeout, correlation, and failure counts;
3. latency percentiles and throughput for load scenarios;
4. queue depth/peak/drop and concurrency peaks against configured bounds;
5. per-equipment isolation, including `ConnectionId` where relevant;
6. Host and each worker process cleanup independently;
7. normal versus forced child termination;
8. final result and snapshots written successfully;
9. no unresolved first failure;
10. the exact status is copied into this matrix without extrapolation.

Cleanup before forced GC is the primary resource-lifecycle check. A post-GC snapshot is supplementary and cannot convert an explicit cleanup failure into a pass.
