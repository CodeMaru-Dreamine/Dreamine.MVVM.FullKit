# Dreamine.Threading

**Dreamine.Threading** provides the core threading abstraction layer for Dreamine applications.

It defines contracts, models, scheduling policies, worker-thread concepts, polling-job structure, CPU-aware cycle policies, and thread manager infrastructure used by Dreamine-based applications.

> This package is not intended to replace the .NET ThreadPool or Task Parallel Library. Its purpose is to provide a stable Dreamine-level boundary for worker threads, polling jobs, scheduling, CPU-aware delay policies, and core assignment rules.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

Dreamine.Threading is designed to solve common threading problems in equipment software and long-running desktop applications:

- too many raw threads created without a clear policy
- polling loops scattered across services and ViewModels
- CPU core assignment mixed directly into business logic
- thread lifecycle management hidden behind static managers
- UI, OS, and execution policies tightly coupled together
- high-speed polling loops consuming CPU without a throttling policy

Dreamine.Threading separates these concerns into explicit contracts and replaceable services.

## Core Concepts

### Worker Thread

A worker thread represents an actual execution loop.

```text
IDreamineThread
 ├─ Start
 ├─ Stop
 ├─ Pause
 ├─ Resume
 └─ AddJob
```

A worker thread owns the execution loop, lifecycle state, assigned jobs, core assignment information, and cycle count.

### Thread Job

A job is a unit of work executed by a worker thread.

```text
IDreamineThreadJob
 ├─ Name
 ├─ Interval
 ├─ ShouldRun
 └─ ExecuteAsync
```

Registering many jobs does **not** mean creating the same number of OS threads.

### Dedicated Worker and Overflow Polling

Dreamine.Threading supports a policy where only a limited number of dedicated worker threads are created per CPU core.

Example:

```text
Logical cores: 2
AutoThreadsPerCore: 2

Dedicated worker capacity:
2 cores × 2 workers = 4 workers
```

If 40 thread jobs are registered:

```text
4 jobs  -> dedicated worker threads
36 jobs -> overflow polling jobs assigned to existing workers
```

This prevents uncontrolled thread creation and reduces unnecessary context switching.

### Zero-Interval High-Speed Workers

`IntervalMs = 0` is supported.

In Dreamine.Threading, zero interval means:

```text
Run as fast as possible.
```

This is useful for FA/equipment software scenarios such as high-speed IO scan, interlock scan, motion state scan, emergency condition scan, and fast sequence state scan.

However, a zero-interval loop can consume CPU aggressively. For that reason, Dreamine.Threading separates two policies:

```text
Raw zero-interval worker
 → IntervalMs = 0
 → UseAdaptiveCpuDelay = false

Adaptive zero-interval worker
 → IntervalMs = 0
 → UseAdaptiveCpuDelay = true
```

### Adaptive CPU Delay

`AdaptiveCpuCyclePolicy` can dynamically add delay when process CPU usage rises.

Example policy:

```text
CPU >= 70% -> 5 ms delay
CPU >= 50% -> 3 ms delay
CPU >= 30% -> 1 ms delay
CPU <  30% -> 0 ms delay
```

This allows FA-style high-speed loops while avoiding uncontrolled CPU saturation.

### YieldWhenIntervalIsZero

`YieldWhenIntervalIsZero` controls whether a zero-interval worker yields the CPU when no delay is applied.

```text
YieldWhenIntervalIsZero = true
 → Thread.Yield() is called when delay is 0

YieldWhenIntervalIsZero = false
 → full-speed loop without explicit yield
```

For strict FA-style high-speed loops, `false` may be used. For general applications, `true` is safer.

## Project Scope

This package contains:

- threading interfaces
- threading models and options
- worker thread abstractions
- thread job abstractions
- core assignment models
- cycle context model
- fixed interval cycle policy
- adaptive CPU cycle policy
- overflow polling policy
- auto core allocation policy
- thread manager and scheduler services

This package does **not** contain:

- Windows CPU affinity implementation
- Windows timer resolution implementation
- Windows process CPU usage provider implementation
- WPF monitoring UI

Those responsibilities are handled by separate packages:

```text
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
```

## Package Structure

```text
Dreamine.Threading
├─ Interfaces
│  ├─ ICpuUsageProvider.cs
│  ├─ IDreamineThread.cs
│  ├─ IDreamineThreadJob.cs
│  ├─ IDreamineThreadManager.cs
│  ├─ IDreamineThreadScheduler.cs
│  ├─ IThreadAffinityService.cs
│  ├─ IThreadCoreAllocator.cs
│  ├─ IThreadCyclePolicy.cs
│  └─ ITimerResolutionService.cs
│
├─ Models
│  ├─ DreamineThreadCoreAssignment.cs
│  ├─ DreamineThreadCoreMode.cs
│  ├─ DreamineThreadCycleContext.cs
│  ├─ DreamineThreadInfo.cs
│  ├─ DreamineThreadJobOptions.cs
│  ├─ DreamineThreadOptions.cs
│  ├─ DreamineThreadPriority.cs
│  └─ DreamineThreadStatus.cs
│
├─ Policies
│  ├─ AdaptiveCpuCyclePolicy.cs
│  ├─ FixedIntervalCyclePolicy.cs
│  └─ OverflowPollingPolicy.cs
│
├─ Allocators
│  └─ AutoCoreAllocator.cs
│
└─ Services
   ├─ DreamineThread.cs
   ├─ DreamineThreadJob.cs
   ├─ DreamineThreadManager.cs
   └─ DreamineThreadScheduler.cs
```

## Basic Usage

```csharp
using Dreamine.Threading.Allocators;
using Dreamine.Threading.Models;
using Dreamine.Threading.Policies;
using Dreamine.Threading.Services;

var manager = new DreamineThreadManager(
    new AutoCoreAllocator(2),
    new FixedIntervalCyclePolicy(),
    new DreamineThreadScheduler());

for (var i = 0; i < 40; i++)
{
    var index = i;

    manager.Register(
        new DreamineThreadOptions
        {
            Name = $"Job-{index}",
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            IntervalMs = 10,
            OverflowPollingIntervalMs = 100
        },
        token =>
        {
            Console.WriteLine($"Job {index} tick");
            return ValueTask.CompletedTask;
        });
}
```

With 2 logical cores and `AutoThreadsPerCore = 2`, this creates up to 4 dedicated worker threads. Remaining jobs are registered as overflow polling jobs.

## High-Speed and Normal Job Example

```csharp
// High-speed jobs with adaptive CPU delay.
for (var i = 0; i < 5; i++)
{
    var index = i;

    threadManager.Register(
        new DreamineThreadOptions
        {
            Name = $"HighMonitor-Adaptive-{index:00}",
            Priority = DreamineThreadPriority.High,
            IntervalMs = 0,
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            OverflowPollingIntervalMs = 10,
            AutoStart = true,
            UseHighPrecisionTimer = false,
            YieldWhenIntervalIsZero = false,
            UseAdaptiveCpuDelay = true
        },
        token => ValueTask.CompletedTask);
}

// High-speed jobs without adaptive CPU delay.
for (var i = 0; i < 5; i++)
{
    var index = i;

    threadManager.Register(
        new DreamineThreadOptions
        {
            Name = $"HighMonitor-Raw-{index:00}",
            Priority = DreamineThreadPriority.High,
            IntervalMs = 0,
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            OverflowPollingIntervalMs = 10,
            AutoStart = true,
            UseHighPrecisionTimer = false,
            YieldWhenIntervalIsZero = false,
            UseAdaptiveCpuDelay = false
        },
        token => ValueTask.CompletedTask);
}

// Normal polling jobs.
for (var i = 0; i < 30; i++)
{
    var index = i;

    threadManager.Register(
        new DreamineThreadOptions
        {
            Name = $"NormalThread-{index:00}",
            Priority = DreamineThreadPriority.Normal,
            IntervalMs = 100,
            CoreMode = DreamineThreadCoreMode.Auto,
            AutoThreadsPerCore = 2,
            OverflowPollingIntervalMs = 500,
            AutoStart = true,
            UseHighPrecisionTimer = false,
            YieldWhenIntervalIsZero = true,
            UseAdaptiveCpuDelay = true
        },
        token => ValueTask.CompletedTask);
}
```

Expected behavior on a 16-logical-core machine:

```text
AutoThreadsPerCore = 2
Dedicated worker capacity = 16 × 2 = 32

Total jobs = 40
Dedicated workers = 32
Overflow jobs = 8
```

## Runtime Validation Example

A sample validation scenario was tested with:

```text
High Adaptive Jobs: 5
High Raw Jobs:      5
Normal Jobs:        30
Total Jobs:         40
```

Observed behavior:

```text
Raw 0ms workers
 → high cycle count
 → full-speed execution

Adaptive 0ms workers
 → lower cycle count
 → CPU-aware delay applied

Normal 100ms workers
 → stable low-frequency polling

Overall CPU usage
 → remained around 25–30% in the sample run
```

This confirms the intended interaction between core assignment, overflow polling, zero-interval execution, adaptive CPU delay, and WPF monitoring.

## Design Principles

Dreamine.Threading follows these principles:

- separate thread execution from job scheduling
- separate core allocation from worker execution
- separate platform APIs from core abstractions
- separate CPU usage measurement from thread execution
- avoid static thread managers
- avoid starting threads inside constructors
- support testable and replaceable components
- keep WPF and Windows-specific code outside the core package
- allow FA-style zero-interval loops while supporting adaptive CPU protection

## Related Packages

```text
Dreamine.Threading
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
Dreamine.Logging
```

## Status

This package is currently in the early structural stage, but the first runtime validation has been completed.

Implemented:

- Worker Thread / Job separation
- Auto core allocation
- Core-per-thread capacity limit
- Overflow polling job assignment
- Zero-interval high-speed loop support
- Adaptive CPU delay policy
- Cycle context support
- Logging-friendly thread manager design
- WPF monitor integration through a separate package

Planned improvements:

- DMContainer-based simplified registration API
- better summary metrics
- round-robin overflow scheduler
- job-level monitoring statistics
- optional core-zero exclusion
- async queue-based dispatching
- lifecycle diagnostics refinement

## License

MIT License
