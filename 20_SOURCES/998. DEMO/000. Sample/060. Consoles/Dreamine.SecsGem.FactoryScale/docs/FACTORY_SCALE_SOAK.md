# Factory-Scale Soak Guide

## Current status

Duration evidence applies only to the exact recorded profile. Two one-hour self-loopback runs passed; this does not qualify every one-hour workload or any longer tier.

| Tier | Intended duration | Intended use | Current status |
|---|---:|---|---|
| Bounded smoke | 5–10 min | CI or local lifecycle check | Shorter load/fault runs passed only at their exact durations; no generic soak claim |
| Standard | 1 h | Local sustained workload and cleanup | Idle-1000 and Normal-500 exact executions `Passed` |
| Extended | 6 h | Engineering stability observation | `Not Run` |
| Production candidate | 24 h | Long observation before a separately approved release decision | `Not Run` |

The 20-Equipment/two-worker multi-process Host-restart execution had run status `Passed` but remains `Experimental` functional evidence. It is not soak or capacity evidence.

External Simulator and Real Equipment soak remain `Not Run / Waiting for User`. This runner never starts or controls those peers automatically.

Qualified zero-socket cleanup currently requires the Windows PID TCP-table provider. A platform/provider that cannot measure process-owned sockets records `unavailable` and fails multi-process worker cleanup acceptance rather than inferring zero.

## Recorded duration evidence

| Scenario | Exact scope | Result | Key observations |
|---|---|---|---|
| `idle-factory` | In-process, 1,000 Equipment, 1 h | `Passed` | 1,000/1,000 Selected; 2,000/2,000 baseline application pairs; 61 idle fleet Linktest cycles plus one baseline cycle, 62,000 total exchanges; timeout/failure/correlation 0 |
| `factory-normal` | In-process, 500 Equipment, 1 h | `Passed` | 1,799,999 workload pairs and 1,800,999 final pairs; last interval 499.9985256759284 pair/s; avg/P95/P99/max 2.4885078757400754/15.02/15.62/55.3114 ms |

Idle working set peaked at 103,579,648 bytes at 30 minutes and finished at 95,035,392; Normal peaked at 121,929,728 at 20 minutes and finished at 104,775,680. Neither series was monotonically rising. Both deterministic cleanups returned sessions/listeners/pending/control/reconnect operations, business queue depth, Factory-owned scenario workers, and process-owned sockets to zero. Process residuals remained after forced GC and require repeated-run observation; these two executions do not prove universal leak freedom.

Other retained bounded results—Busy-100 for one minute, Trace-20 for one minute, 16 × 1 MiB and 4 × near-maximum bodies, in-process fault isolation, in-process reconnect groups 10/50/100/250, a 250-equipment/five-worker multi-process smoke, and a 500-equipment/five-worker/100-endpoint reconnect—passed their exact acceptance checks. They are not duration-soak evidence.

## What a soak can and cannot show

A soak can reveal resource accumulation, timer/reconnect drift, queue saturation, throughput decay, or cleanup failure under a recorded workload. It cannot by itself prove SEMI compliance, absence of all leaks, compatibility with untested peers, or performance on another machine.

Every conclusion is limited to the exact build, machine, mode, process partition, Equipment count, scenario, rate, payload, duration, and limits in the result bundle.

## Preconditions

Before starting a long run:

1. build the runner and referenced projects in Release;
2. run a small bounded smoke with the same mode and scenario;
3. choose a dedicated artifact directory outside source control;
4. verify free disk space for result, snapshot, and control files;
5. reserve a loopback port range large enough for every Equipment endpoint;
6. confirm the range is not in use by unrelated processes;
7. record machine and runtime identity, power policy, and relevant configuration;
8. prevent scheduled sleep, reboot, or maintenance from invalidating the interval;
9. decide the acceptance criteria before execution;
10. ensure cancellation can be issued and that child processes can be inspected after failure.

Do not place credentials, customer names, proprietary specifications, external endpoint addresses, or raw private message content in the command line or artifact names.

## Snapshot schedule

For the in-process scenario coordinator, the default checkpoint schedule is:

- 1 minute;
- 5 minutes;
- 10 minutes;
- every 10 minutes after that;
- a final in-memory result snapshot when the duration does not end on a checkpoint.

When `--snapshot-directory` is supplied, due checkpoints are written there through the bounded atomic exporter. When `--snapshot-interval` is supplied, that positive interval replaces the default checkpoint schedule.

Current implementation boundary: the multi-process Host validation path preserves its Host result and per-worker results, but it does not currently export the same periodic Host checkpoint series through the in-process snapshot scheduler. Therefore a multi-process long run must not claim the default 1/5/10-minute snapshot evidence unless that evidence was actually added and retained by the executed build. Treat automated multi-process periodic-soak evidence as `Pending` in this draft.

## Example commands

Use a Release-built runner. The placeholders below deliberately avoid machine-specific paths.

### Five-minute in-process lifecycle smoke

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  in-process factory-normal `
  --equipment-count 100 `
  --duration 00:05:00 `
  --snapshot-directory <artifact-dir>/snapshots `
  --output <artifact-dir>/result.json
```

Status before an actual recorded execution: `Not Run`.

### One-hour in-process normal profile

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  in-process factory-normal `
  --equipment-count 500 `
  --duration 01:00:00 `
  --snapshot-directory <artifact-dir>/snapshots `
  --output <artifact-dir>/result.json
```

Recorded evidence for this exact 500-equipment/one-hour profile is `Passed`. Re-running this example creates a new result that must be reviewed independently; the earlier status does not transfer automatically.

### Six-hour in-process idle profile

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  in-process idle-factory `
  --equipment-count 1000 `
  --duration 06:00:00 `
  --snapshot-directory <artifact-dir>/snapshots `
  --output <artifact-dir>/result.json
```

Status: `Not Run`. Verify OS resource headroom and port availability before attempting it.

### Twenty-four-hour candidate

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  in-process soak `
  --equipment-count <approved-count> `
  --duration 1.00:00:00 `
  --snapshot-directory <artifact-dir>/snapshots `
  --output <artifact-dir>/result.json
```

Status: `Not Run`. A 24-hour execution requires an explicit operator decision and monitoring plan; it is not inferred from a shorter run.

### Multi-process bounded observation

```powershell
dotnet run --configuration Release --project <FactoryScale.csproj> -- `
  multi-process factory-normal `
  --equipment-count <count> `
  --worker-count <workers> `
  --equipment-per-worker <capacity> `
  --duration 00:10:00 `
  --output <artifact-dir>/result.json
```

Status: `Not Run` as a recorded soak. The current output provides final Host/aggregate checks and references to worker evidence; it is not equivalent to the in-process periodic snapshot series.

## Checkpoint review

At every retained checkpoint, review:

- requested, connected, selected, failed, and reconnecting Equipment;
- cumulative requests, responses, timeouts, correlation errors, reconnects, and failures;
- achieved message and byte rates;
- response latency sample count, P50, P95, P99, maximum, and queue delay where present;
- current and peak queue depth, rejected work, and diagnostic drops;
- current and peak connect/reconnect/message/control operations;
- pending transactions against per-equipment and global limits;
- tracked Host sessions/listeners/operations and Equipment peer/listener counts;
- per-process working set, managed heap, GC, thread/handle, thread-pool, and OS socket observations;
- first failure and any cancellation or forced-termination marker.

Rate fields require explicit units. The workload check's historical `msg/s` label is numerically the Primary/request-reply pair rate. JSON `messagesPerSecond` is request rate plus response rate, normally two data messages per successful pair. Linktest and other HSMS control frames are not application message or response-latency samples, but their encoded frame lengths contribute to byte rate and their work contributes to pending-control metrics. The current runner preserves the last non-zero interval in sustained-profile final results; the WPF reader falls back for earlier JSON whose terminal no-delta rate was zero. Retain the final totals and interval meaning together.

For multi-process evidence, review the Host and every worker separately. A healthy parent snapshot cannot mask a worker with residual sessions, listeners, operations, queue depth, or sockets.

## Acceptance criteria

A soak is eligible for `Passed` only if all criteria selected before the run are met. At minimum:

- the intended duration completed without unplanned process exit;
- the requested peer population reached the required connected/selected threshold;
- the planned traffic completed with no unexplained loss or correlation error;
- unintended timeout count is zero for profiles that require it;
- queue and operation peaks do not exceed declared bounds;
- any diagnostic drop is disclosed and its evidence impact assessed;
- healthy peers remain healthy during planned reconnect/fault stages;
- latency and rate remain within the declared run-specific thresholds;
- no sustained-rise warning remains unexplained;
- Host and every worker report zero tracked resources after deterministic cleanup;
- no child required kill fallback in a successful run;
- final result and expected checkpoint files are readable and identity-consistent.

An operator interruption, machine sleep, unrelated port collision, evidence write failure, missing checkpoint, or forced process kill makes the run incomplete or failed; it must not be reported as passed.

## Memory trend review

The built-in sustained-rise heuristic needs at least five snapshots. It inspects a recent tail of up to six samples and flags monotonic growth greater than 64 MiB across that tail. It is a warning, not a leak detector.

For each warning:

1. align the samples with workload phase, rate, queue depth, and pending count;
2. compare working set and managed heap;
3. inspect GC counts and any delayed response backlog;
4. confirm whether sessions, listeners, or sockets accumulated;
5. repeat the same run on the same build and machine;
6. use a dedicated profiler only after reproducing the trend.

Do not dismiss a deterministic cleanup failure because forced GC reduced memory. Cleanup before forced GC is the primary lifecycle evidence.

## Reconnect and Host-restart soak

Reconnect and Host-restart require extra observations:

- recovery start/end timestamps and duration;
- requested drop count and actual affected Equipment IDs in masked/generalized form;
- peak reconnect concurrency and queue/pending behavior;
- unaffected peers' continuous request/response health;
- restored connected/selected count;
- old versus new `ConnectionId` for each affected Host context;
- zero inherited pending/control transaction state;
- worker PID and endpoint continuity for multi-process Host restart;
- normal exit and zero cleanup for both Host children and all workers.

The recorded 20-device/two-worker multi-process Host-restart run remains `Experimental` despite its `Passed` run status. A long Host-restart soak is `Not Run`.

## Cancellation and cleanup

Use Ctrl+C or the owning orchestration cancellation path. The expected sequence is:

1. stop issuing new workload;
2. propagate cooperative worker stop requests;
3. drain bounded work and result writes;
4. disconnect and dispose Host contexts;
5. dispose Equipment peers and listeners;
6. wait for owned children within the configured shutdown deadline;
7. use owned-child-tree kill fallback only after the deadline;
8. retain cancellation and cleanup evidence;
9. return CLI exit code 130 for caller cancellation.

After any abnormal termination, inspect only processes and ports created for the recorded run. Never terminate unrelated processes by name or clear a broad directory.

## Artifact set

Retain outside the repository:

- final result JSON;
- checkpoint JSON files actually produced;
- per-worker and Host-child result files for multi-process runs;
- validated endpoint manifest and identity-matched control evidence needed to audit the run;
- bounded stdout/stderr tails from failed children;
- machine/build/run configuration record;
- operator notes for interruptions or external peers.

Control artifacts may contain local process and port information. Mask or omit sensitive values before sharing. Raw final JSON, periodic snapshots, worker/control manifests, and logs are not committed regardless of size. Customer or internal material is reference-only and must not become normative evidence.

The verification baseline accompanying these results is: FactoryScale and WPF Release builds both 0 warnings/0 errors; FactoryScale tests 42/42 passed; WPF integration tests 19/19 passed; SECS/GEM Core tests 222 passed; whole-solution tests 664 passed with one already-known Ontology failure. Public SECS/GEM Core and NuGet APIs, package versions, tags, and releases were unchanged.

Update [FACTORY_SCALE_TEST_MATRIX.md](FACTORY_SCALE_TEST_MATRIX.md) only after reviewing the complete bundle. Performance interpretation is described in [FACTORY_SCALE_PERFORMANCE.md](FACTORY_SCALE_PERFORMANCE.md).
