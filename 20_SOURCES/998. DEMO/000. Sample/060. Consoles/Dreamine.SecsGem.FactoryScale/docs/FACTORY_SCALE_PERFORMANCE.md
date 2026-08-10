# Factory-Scale Performance Guide

## Current result status

The following Release self-loopback runs are verified only for their exact build, machine, mode, count, duration, and workload.

| Evidence | Current status | What may be concluded |
|---|---|---|
| In-process staged 100 / 250 / 500 / 1,000 Equipment | `Passed` | Exact stages reached full Selected state, completed baseline/isolation traffic, and cleaned tracked resources and sockets to zero. No larger or different-mode capacity is inferred. |
| Multi-process 250 Equipment / 5 workers | `Passed` | 250/250 Selected, 502/502 application pairs, and five graceful zero-resource worker exits. |
| Multi-process reconnect 500 / 5 workers × 100 endpoints | `Passed` | 100/100 reconnects, peak 16/16, unaffected traffic healthy, Host and worker cleanup zero. |
| 20-Equipment / 2-worker Host restart | `Experimental` — run `Passed` | The two-Host OS-process path passed at this engineering-smoke scope only. |
| Idle-1000 and Normal-500, 1 hour | `Passed` | Exact one-hour executions passed; this is not a 6-hour, 24-hour, external, or universal leak-freedom claim. |
| 6-hour / 24-hour run | `Not Run` | No extended-duration claim. |
| External Simulator / Real Equipment | `Not Run / Waiting for User` | No interoperability or production-equipment claim. |

All rows are self-loopback evidence, not SEMI compliance, certification, or external-equipment compatibility evidence. The Experimental Host-restart result is functional evidence, not a benchmark.

## Measurement purpose

The runner is intended to answer bounded questions for a specific build and machine:

- How many requested Equipment endpoints became TCP-connected and HSMS-selected?
- Did request/response correlation remain isolated by connection?
- What request rate, byte rate, and latency distribution were observed?
- Did configured queues, pending admission, and operation concurrency remain bounded?
- Did one reconnecting or failing Equipment disturb healthy peers?
- Did every Host and worker process return tracked resources to zero before exit?
- Did process memory exhibit a sustained rising tail during a sufficiently long run?

It does not establish a universal maximum. Results depend on CPU topology, memory, OS socket limits, build configuration, payload, timers, tracing, process partitioning, and workload duration.

## Executed profiles

| Profile | Planned load | Minimum acceptance observations | Current status |
|---|---|---|---|
| Idle Factory | 1,000 selected Equipment; 1 h | 1,000/1,000 Selected, 62,000 total Linktest exchanges including baseline, unintended timeout/failure/correlation 0, cleanup resources/sockets 0 | `Passed` |
| Normal Factory | 500 Equipment; 1 h | 1,799,999 workload pairs, 1,800,999 final pairs, 499.9985256759284 pair/s last interval, latency and queue bounds recorded | `Passed` |
| Busy Factory | 100 Equipment; 1 min | 59,987 workload pairs, 60,187 final pairs, 999.2392142731878 pair/s last interval, bounded queues/pending | `Passed` |
| Trace Burst | 20 Equipment; 1 min | 119,988 workload pairs, 120,028 final pairs, 1,998.8966852619521 pair/s last interval, diagnostic drops 0 | `Passed` |
| Large Message | 16 × 1 MiB and 4 × 16,777,202-byte bodies | Both exact runs completed without transaction loss; byte, latency, allocation, and cleanup evidence retained | `Passed` |
| Reconnect Storm | 500 Equipment; groups 10/50/100/250 | Every reconnect succeeded, unaffected traffic passed, peak reconnect 10/16/16/16, cleanup zero | `Passed` |
| Multi-process reconnect | 500 Equipment / 5 workers; 100 endpoints | 100/100 reconnect, 1,001/1,001 pairs, peak 16/16, all worker cleanup zero | `Passed` |
| Host Restart | 20 Equipment / 2 retained workers | Different Host PIDs and every `ConnectionId`, no inherited state, both Host and worker cleanup zero | `Experimental` — run `Passed` |

The actual achieved Primary/pair rate and request-plus-response data-message rate are reported separately below.

### Exact application-load results

| Profile | Final pairs | Workload pairs | Avg / P50 / P95 / P99 / Max latency (ms) | Last pair/s | Last request+response messages/s |
|---|---:|---:|---|---:|---:|
| Normal-500, 1 h | 1,800,999 | 1,799,999 | 2.4885078757400754 / 0.611 / 15.02 / 15.62 / 55.3114 | 499.9985256759284 | 999.9970513518568 |
| Busy-100, 1 min | 60,187 | 59,987 | 2.481821772143486 / 0.561 / 14.94 / 15.52 / 42.9831 | 999.2392142731878 | 1,998.4784285463757 |
| Trace-20, 1 min | 120,028 | 119,988 | 2.6960764246675777 / 0.837 / 14.81 / 15.35 / 36.1535 | 1,998.8966852619521 | 3,997.7933705239043 |

Idle-1000 recorded 2,000/2,000 baseline application pairs. Its 2,000 application latency samples were average/P50/P95/P99/max `14.126450150000002 / 8.318 / 51.12 / 53.87 / 56.6959 ms`; Linktest control round trips are not part of that sample population.

| One-hour run | Final WS / heap | Allocated since start | GC 0/1/2 | Threads / handles / sockets | Sent / received bytes |
|---|---:|---:|---:|---:|---:|
| Normal-500 | 104,775,680 / 35,292,656 | 26,251,483,496 | 2856/1462/29 | 29 / 1,758 / 1,000 | 53,428,974 / 41,446,477 |
| Idle-1000 | 95,035,392 / 29,216,824 | 477,657,616 | 53/50/2 | 37 / 2,527 / 2,000 | 912,000 / 947,000 |

### Exact scale and bounded scenario results

| Evidence | Result | Exact counters and latency |
|---|---|---|
| Scale 100 / 250 / 500 / 1,000 | `Passed` | Pairs 203 / 503 / 1,003 / 2,003; all Selected; timeout/failure/correlation 0 |
| Large 16 × 1 MiB | `Passed` | Final 56/56 pairs; sent 16,778,664 bytes; avg/P50/P95/P99/max 17.81845357142857/13.39/38.42/41.7/41.6934 ms |
| Large 4 × near-max | `Passed` | Final 12/12 pairs; sent 67,109,112 bytes; avg/P50/P95/P99/max 89.8354/28.32/260/260/259.9425 ms |
| Fault isolation, 6 Equipment | `Passed` | 15 requests/12 responses, expected timeout 2 and failure 3, one injected failed Equipment, correlation 0; avg/P50/P95/P99/max 16.510583333333333/1.086/130.7/130.7/130.6902 ms; healthy peer continued |
| Reconnect 500 × 10/50/100/250 | `Passed` | 1,001/1,001 pairs in every run; reconnect 10/10, 50/50, 100/100, 250/250; peak 10/16/16/16; timeout/failure/correlation 0 |
| MP smoke 250 / 5 workers | `Passed` | 502/502 pairs; Host avg/P50/P95/P99/max 21.2197573705179/16.39/44.6/47.18/58.9735 ms; all workers graceful and zero-resource |
| MP reconnect 500 / 5 workers × 100 | `Passed` | 1,001/1,001 pairs; reconnect 100/100; avg/P50/P95/P99/max 15.8674113886114/11.63/36.63/40.41/58.0307 ms; peak 16/16 |

| In-process reconnect target | Success | Recovery duration | Avg / P50 / P95 / P99 / Max (ms) | Peak reconnect |
|---:|---:|---:|---|---:|
| 10 | 10/10 | 0.0314765 s | 13.835752347652347 / 7.174 / 39.76 / 51.78 / 56.2041 | 10 |
| 50 | 50/50 | 0.0966569 s | 29.773887512487512 / 14.93 / 90.81 / 100.6 / 131.7071 | 16 |
| 100 | 100/100 | 0.0851495 s | 11.116085514485514 / 5.737 / 29.89 / 39.25 / 45.67 | 16 |
| 250 | 250/250 | 0.1678191 s | 12.822371628371629 / 6.254 / 42.31 / 45.81 / 49.7999 | 16 |

The multi-process 500×100 reconnect recovered in 1.6209649 s at worker granularity. Its Host final snapshot was 500/500 Selected, 100/100 reconnect, zero timeout/failure/correlation, peak control 123, Host queue peak 494, diagnostic queue peak 78, and zero final queue reject/drop/depth. Its Host cleanup WS/heap was 83,124,224/18,353,192 before forced GC and 67,330,048/1,030,376 after; sessions, pending/control/reconnect operations and sockets were zero in both cleanup snapshots.

The maximum-body run used a 16,777,202-byte body. With a 4-byte SECS item header and 10-byte HSMS header the declared frame is 16,777,216 bytes; the 4-byte length prefix makes the wire frame 16,777,220 bytes. Percentiles are fixed-memory histogram bucket upper bounds; exact min/max can therefore be slightly below a reported percentile bucket boundary.

## Metric semantics

### Connection and protocol counters

Record at minimum:

- requested, connected, selected, failed, and reconnecting Equipment counts;
- requests, responses, timeouts, reconnects, general failures, and correlation failures;
- bytes sent/received and their rates;
- per-equipment request, response, timeout, state, latency, and `ConnectionId`.

Connected or Selected counts alone do not prove traffic health. A run must also exercise and correlate its planned protocol messages.

### Latency

Response latency is measured from the active request operation until its correlated Secondary successfully completes. Timeout and failure durations are not inserted into this histogram and remain separate counters. Report:

- sample count;
- arithmetic mean;
- minimum and maximum;
- P50, P95, and P99.

Queue delay and response latency answer different questions and should remain separate. A low protocol response latency can coexist with a high admission or queue delay under saturation.

Percentiles must name the sample population. Do not combine successful latency with timeout duration without an explicit, separately labeled statistic.

### Throughput

Use a common measured interval for counters:

```text
request_rate  = issued_primary_requests_delta / elapsed_seconds
response_rate = correlated_responses_delta / elapsed_seconds
pair_rate     = request_rate when each Primary receives one correlated Secondary
messages_rate = request_rate + response_rate
byte_rate     = (bytes_sent_delta + bytes_received_delta) / elapsed_seconds
```

The scenario check's achieved `msg/s` value is historically named but numerically represents Primary/pair rate. JSON `messagesPerSecond` is the bidirectional request-plus-response data-message rate, normally twice the pair rate. Select and Linktest control frames are excluded from application request/response/message and response-latency counters, but included in frame-byte and pending-control metrics. This is why Idle-1000 checkpoints correctly report zero application messages/s while carrying approximately 466–467 bytes/s of Linktest frames.

Report warm-up, steady-state, and shutdown intervals separately when possible. The current runner preserves the last non-zero interval in sustained-profile final results. The WPF reader has a compatibility fallback for earlier retained JSON where an immediate terminal no-delta snapshot reported zero rates. Retain final totals, interval rates, evidence schema, and build together. Final totals divided by total process lifetime include startup and cleanup and are not equivalent to steady-state throughput.

### Queue and concurrency metrics

Capture current and peak values for:

- Host message queue;
- Equipment receive queues;
- diagnostic and export queues;
- per-equipment and global pending transaction admission;
- active connect, reconnect, control, and message operations.

The configured limit and observed peak must be recorded together. A peak below a limit is not sufficient if accepted operations were lost or failed without accounting.

The Host message and export queues use bounded `Wait`. The Equipment receive queue is invoked from a synchronous callback and uses explicit counted `Reject` rather than blocking a ThreadPool thread; normal scenarios require zero rejects. The diagnostic queue may drop the newest diagnostic at capacity and increments a drop counter. Therefore:

- `diagnostic_dropped = 0` means no observed diagnostic enqueue was rejected by that queue during the measurement;
- `diagnostic_dropped > 0` must be disclosed and prevents treating logs as complete evidence;
- diagnostic loss is not message loss, and neither counter substitutes for protocol request/response accounting.

### Process and OS metrics

Capture per process:

- working set and managed heap;
- Gen 0/1/2 GC counts;
- thread, handle, and thread-pool observations where available;
- OS TCP connection/socket observations where available;
- tracked session, listener, operation, pending, and queue counts.

OS counters may be unavailable on a platform. Mark them unavailable; do not replace an unavailable value with zero.

The current qualified zero-socket gate uses the Windows PID TCP-table provider. If it is unavailable, multi-process worker cleanup fails acceptance instead of claiming zero sockets; any remaining run output is diagnostic evidence only.

## Multi-process aggregation

The coordinator, Host-only children, and Equipment workers are distinct processes. Their snapshots are not interchangeable.

- A Host result reports that Host process and logical per-equipment state known to it.
- Each worker result reports the worker's own fleet and cleanup.
- Aggregate logical counts may be summed when the populations are disjoint and the result labels them as aggregate.
- Working set, heap, threads, handles, and instantaneous sockets should remain per process. If a separate sum is shown, label it explicitly as a point-in-time sum and preserve every source snapshot.
- Parent cleanup does not prove worker cleanup. Every worker must independently report zero sessions, listeners, operations, queue depth, and open sockets, then exit normally without kill fallback.

For Host restart, compare both Host child results rather than overwriting the first:

1. Host PIDs must differ.
2. Every equipment `ConnectionId` must change.
3. cleanup pending/control/session/socket counts must be zero in both results.
4. second-run request/response and Selected counts must demonstrate restoration.
5. worker identities and endpoint definitions must remain stable, and final worker cleanup must be zero.

## Memory trend interpretation

A single before/after memory delta is not a leak diagnosis. The runner's sustained-rise heuristic requires at least five snapshots and examines a recent tail of up to six samples for monotonic growth exceeding 64 MiB in total. This is a warning/check, not proof of either a leak or leak freedom.

Investigate a warning by comparing:

- workload phase and queue/pending depth;
- managed heap versus working set;
- GC counts and post-cleanup values;
- outstanding sessions, operations, and socket state;
- repeat runs on the same build and machine.

Forced-GC-after-cleanup data is supplementary. The primary lifecycle gate is zero tracked resources before forced GC.

### Observed one-hour memory and cleanup

| Run | Active WS trend | Final / post-GC heap | Post-GC process residual versus start |
|---|---|---|---|
| Normal-500 | WS peaked at 121,929,728 bytes at 20 min, then declined to 104,775,680 terminal | 35,292,656 / 3,820,656 bytes | WS +45,326,336; heap +2,900,968; handles +197 |
| Idle-1000 | WS peaked at 103,579,648 bytes at 30 min, then declined to 95,035,392 terminal | 29,216,824 / 6,129,920 bytes | WS +41,271,296; heap +5,185,816; handles +240; threads +30 |

Both runs had zero cleanup sessions, listeners, pending/control/reconnect operations, business queue depth, Factory-owned scenario workers, and process-owned sockets before and after forced GC. The caller-owned exporter remained `1/1` versus its baseline and is recorded separately from scenario operations. Working-set, heap, thread, and handle residuals are process observations affected by runtime/native state; they neither prove a leak nor prove leak freedom. Repeat-run comparison is required.

## Reproducible benchmark record

Every future benchmark record should include:

- source revision and Release build identity;
- OS, architecture, .NET runtime, CPU logical/core count, and available memory;
- power plan and relevant endpoint-security exclusions, if approved and material;
- execution mode, worker count, Equipment count, port range, payload, scenario, duration, and all non-default limits;
- snapshot schedule and artifact directory identity;
- startup failures and retry counts;
- complete acceptance checks and first failure;
- per-process results for multi-process execution;
- whether a process required kill fallback;
- raw evidence retention location outside source control.

For the verified baseline, raw final JSON, periodic snapshots, worker/control artifacts, and process logs remain local evidence and were intentionally not committed. Documentation records reviewed summaries; artifact hashes and build/run identities belong in the controlled evidence record rather than exposing machine-specific paths.

Do not record customer names, proprietary specifications, real endpoint addresses, credentials, or internal document content.

## Comparison discipline

Compare two results only when their workload and environment are materially equivalent. At minimum, normalize or disclose differences in:

- Equipment and worker count;
- selected ratio and completed message count;
- message size and rate;
- duration and warm-up;
- tracing/diagnostics configuration;
- concurrency, queue, pending, and timer settings;
- process mode and build configuration.

A faster run with fewer successful responses is not an improvement. A lower memory result that required forced termination is not an improvement. Capacity is established at the largest independently executed row that meets every acceptance check, never by extrapolating from a smaller smoke.

## Verification boundary

The recorded baseline has 42/42 FactoryScale tests, 19/19 WPF integration tests, and 222 SECS/GEM Core tests passing. The whole-solution run recorded 664 passes and one already-known Ontology failure; it must not be described as completely green. FactoryScale and WPF Release builds both completed with 0 warnings and 0 errors. The FactoryScale work changed no public SECS/GEM Core or NuGet API, package version, tag, or release.

Six-hour and 24-hour profiles remain `Not Run`. External Simulator and Real Equipment remain `Not Run / Waiting for User`. No result in this guide is external-equipment compatibility, SEMI compliance, certification, or a universal capacity guarantee.

See [FACTORY_SCALE_TEST_MATRIX.md](FACTORY_SCALE_TEST_MATRIX.md) for evidence status and [FACTORY_SCALE_SOAK.md](FACTORY_SCALE_SOAK.md) for duration procedures.
