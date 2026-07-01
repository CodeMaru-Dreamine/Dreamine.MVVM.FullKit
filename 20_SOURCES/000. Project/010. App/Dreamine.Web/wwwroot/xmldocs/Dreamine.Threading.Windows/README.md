# Dreamine.Threading.Windows

**Dreamine.Threading.Windows** provides Windows-specific threading services for Dreamine.Threading.

This package implements platform-dependent features such as CPU affinity, timer resolution, Windows CPU information, and process CPU usage measurement.  
It depends on `Dreamine.Threading` and supplies concrete services for Windows desktop applications.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

The core `Dreamine.Threading` package defines abstractions and scheduling policies.  
This package provides Windows implementations for those abstractions.

```text
Dreamine.Threading
 ├─ IThreadAffinityService
 ├─ ITimerResolutionService
 ├─ ICpuUsageProvider
 └─ CPU/core-related abstractions

Dreamine.Threading.Windows
 ├─ WindowsThreadAffinityService
 ├─ WindowsTimerResolutionService
 ├─ WindowsCpuInfoProvider
 └─ WindowsProcessCpuUsageProvider
```

## Responsibilities

This package is responsible for:

- applying CPU affinity to the current Windows thread
- controlling timer resolution when supported
- exposing Windows CPU information
- measuring current process CPU usage
- providing Windows-specific registration helpers
- keeping P/Invoke code outside the core threading package

## Package Structure

```text
Dreamine.Threading.Windows
├─ Native
│  ├─ Kernel32NativeMethods.cs
│  └─ WinMmNativeMethods.cs
│
├─ Services
│  ├─ WindowsCpuInfoProvider.cs
│  ├─ WindowsProcessCpuUsageProvider.cs
│  ├─ WindowsThreadAffinityService.cs
│  └─ WindowsTimerResolutionService.cs
│
└─ Registration
   └─ DreamineThreadingWindowsRegistration.cs
```

## Services

### WindowsThreadAffinityService

Implements:

```text
IThreadAffinityService
```

It applies CPU affinity to the current Windows thread using Windows native APIs.

```text
CoreIndex = 2
 → current thread affinity mask is set to CPU core 2
```

### WindowsTimerResolutionService

Implements:

```text
ITimerResolutionService
```

It controls timer resolution using `timeBeginPeriod` and `timeEndPeriod`.

This is useful when high precision polling is required, but it should be used carefully because timer resolution may affect the whole system.

### WindowsCpuInfoProvider

Provides Windows CPU information such as logical processor count.

```text
Environment.ProcessorCount
```

### WindowsProcessCpuUsageProvider

Implements:

```text
ICpuUsageProvider
```

It calculates current process CPU usage using:

```text
Process.GetCurrentProcess().TotalProcessorTime
Elapsed wall-clock time
Environment.ProcessorCount
```

This provider is used by `AdaptiveCpuCyclePolicy` to apply dynamic delay when CPU usage rises.

## Adaptive CPU Delay Integration

With `WindowsProcessCpuUsageProvider`, zero-interval workers can be throttled by CPU usage.

Example behavior:

```text
CPU >= 70% -> 5 ms delay
CPU >= 50% -> 3 ms delay
CPU >= 30% -> 1 ms delay
CPU <  30% -> 0 ms delay
```

This is especially useful for FA-style monitoring loops where `IntervalMs = 0` is required, but CPU saturation should be controlled.

## Registration

Current registration style:

```csharp
using Dreamine.Threading.Windows.Registration;

DreamineThreadingWindowsRegistration.Register();
```

This registers Windows-specific services such as:

```text
IThreadAffinityService  -> WindowsThreadAffinityService
ITimerResolutionService -> WindowsTimerResolutionService
ICpuUsageProvider       -> WindowsProcessCpuUsageProvider
WindowsCpuInfoProvider
```

> This package does not extend `DMContainer` through partial class across assemblies. Registration is provided through a dedicated registration class.

## Design Notes

Windows-specific APIs must not be placed inside `Dreamine.Threading`.

This package exists to preserve the following dependency direction:

```text
Dreamine.Threading
        ↑
Dreamine.Threading.Windows
```

The core package remains platform-independent, while this package handles Windows-only behavior.

## Validation Scenario

This package was validated with the following setup:

```text
High Adaptive Jobs: 5
High Raw Jobs:      5
Normal Jobs:        30
Total Jobs:         40
```

Observed behavior:

```text
CPU affinity was applied to dedicated workers.
Adaptive workers used process CPU usage to reduce cycle rate.
Raw zero-interval workers ran at full speed.
Normal workers stayed on 100ms polling.
Overall CPU usage remained stable around 25–30% in the sample run.
```

## Related Packages

```text
Dreamine.Threading
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
```

## Status

Implemented:

- Windows CPU affinity service
- Windows timer resolution service
- Windows CPU information provider
- Windows process CPU usage provider
- Windows registration helper

Planned improvements:

- restore-token based affinity recovery
- configurable timer resolution period
- richer CPU usage metrics
- optional core-zero exclusion policy
- integration tests for Windows-specific behavior

## License

MIT License
