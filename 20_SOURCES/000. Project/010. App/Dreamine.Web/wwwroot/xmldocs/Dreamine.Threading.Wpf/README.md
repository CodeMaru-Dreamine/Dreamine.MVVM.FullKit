# Dreamine.Threading.Wpf

**Dreamine.Threading.Wpf** provides WPF monitoring UI components for Dreamine.Threading.

This package displays worker thread status, priority, interval, assigned CPU core, affinity state, job count, cycle count, and fault state inside WPF applications.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

The core package `Dreamine.Threading` defines worker threads, polling jobs, scheduling policies, and thread state models.  
This package provides WPF UI components for observing those runtime states.

```text
Dreamine.Threading
 └─ Thread state and manager abstractions

Dreamine.Threading.Wpf
 ├─ Thread monitor view
 ├─ Thread monitor view model
 ├─ WPF UI dispatcher
 └─ status/priority converters
```

## Responsibilities

This package is responsible for:

- displaying registered worker threads
- displaying thread status and priority
- displaying interval and assigned CPU core
- displaying affinity state
- displaying cycle count and job count
- displaying last error information
- refreshing thread monitor data safely on the WPF UI thread
- keeping WPF-specific code outside the core threading package

## Package Structure

```text
Dreamine.Threading.Wpf
├─ Services
│  └─ WpfThreadUiDispatcher.cs
│
├─ ViewModels
│  └─ DreamineThreadMonitorViewModel.cs
│
├─ Views
│  └─ DreamineThreadMonitorView.xaml
│
├─ Converters
│  ├─ ThreadPriorityTextConverter.cs
│  └─ ThreadStatusBrushConverter.cs
│
└─ Registration
   └─ DreamineThreadingWpfRegistration.cs
```

## Thread Monitor View

The monitor view displays thread information in a WPF `DataGrid`.

Displayed columns:

```text
Name
Status
Priority
Interval
Core
Affinity
Jobs
Cycles
Last Error
```

The monitor can be used to observe:

- raw zero-interval workers
- adaptive zero-interval workers
- normal polling workers
- overflow job distribution
- cycle count differences
- affinity assignment
- fault state

## Selection Detail Panel

The lower detail panel displays information about the selected worker thread.

Displayed information includes:

```text
Name
Status
Priority
Interval
Core
Affinity
Job Count
Cycle Count
Started At
Stopped At
Last Error
```

The selection is restored during refresh so that the detail panel remains visible while monitoring updates continue.

## WPF UI Dispatching

`WpfThreadUiDispatcher` ensures that monitor updates are applied on the WPF UI thread.

```text
Background refresh timer
 → IDreamineThreadManager.GetThreadInfos()
 → WpfThreadUiDispatcher.BeginInvoke(...)
 → ObservableCollection update
```

## Registration

Current registration style:

```csharp
using Dreamine.Threading.Wpf.Registration;

DreamineThreadingWpfRegistration.Register();
```

This registers:

```text
WpfThreadUiDispatcher
DreamineThreadMonitorViewModel
DreamineThreadMonitorView
```

> This package does not create or control worker threads directly. It only observes `IDreamineThreadManager`.

## Usage

```xml
<UserControl
    xmlns:threadingViews="clr-namespace:Dreamine.Threading.Wpf.Views;assembly=Dreamine.Threading.Wpf">

    <threadingViews:DreamineThreadMonitorView />

</UserControl>
```

When used inside a wrapper page:

```xml
<threadingViews:DreamineThreadMonitorView
    DataContext="{Binding ThreadMonitor}" />
```

## Design Notes

This package depends on abstractions from `Dreamine.Threading`.  
It should not directly control native Windows thread behavior.

The WPF monitor observes thread state through:

```text
IDreamineThreadManager
DreamineThreadInfo
DreamineThreadStatus
DreamineThreadPriority
```

This keeps the UI layer separated from scheduling and platform logic.

## Validation Scenario

The WPF monitor was validated with:

```text
High Adaptive Jobs: 5
High Raw Jobs:      5
Normal Jobs:        30
Total Jobs:         40
```

Observed behavior in the monitor:

```text
Raw workers
 → very high cycle count

Adaptive workers
 → lower cycle count because adaptive delay was applied

Normal workers
 → stable 100ms cycle behavior

Overflow jobs
 → visible through increased Jobs count on selected workers

Affinity
 → displayed through Core and Affinity columns

Last Error
 → remained empty during successful validation
```

## Related Packages

```text
Dreamine.Threading
Dreamine.Threading.Windows
Dreamine.Threading.Wpf
```

## Status

Implemented:

- WPF Thread Monitor View
- Thread Monitor ViewModel
- Observable thread info list
- periodic refresh
- status brush converter
- priority text converter
- selected thread detail panel
- WPF UI dispatcher
- registration helper

Planned improvements:

- summary header
- total worker count
- total job count
- overflow job count
- adaptive/raw/normal grouping
- refresh interval option
- command buttons for Start / Stop / Pause / Resume
- more stable incremental collection update

## License

MIT License
