# Dreamine.Logging.Wpf

Dreamine.Logging.Wpf provides WPF-specific logging UI components for Dreamine applications.
It connects `Dreamine.Logging` to WPF through a log panel view, a bounded log panel view model, and UI dispatcher helpers designed for high-frequency log updates.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

This package is responsible only for WPF presentation and UI-thread integration.
The core logging pipeline, sinks, formatters, and async queue are provided by `Dreamine.Logging`.

`Dreamine.Logging.Wpf` depends on `Dreamine.Logging`.
`Dreamine.Logging` must not depend on WPF.

## Features

- `DreamineLogPanelView` for displaying logs in WPF
- `DreamineLogPanelViewModel` for binding log entries to the UI
- Bounded visible log collection with `DefaultDisplayCapacity = 1000`
- Batched UI update delivery through `BatchedDispatcher<T>`
- `WpfLogUiDispatcher` for WPF Dispatcher access
- Real-time log display through `IDreamineLogStore.LogAdded`
- Clear support through `DreamineLogPanelViewModel.Clear()`
- Auto-select/auto-scroll support through `AutoScroll` and `EntryAppended`
- Log detail display for selected entries
- Event unsubscription through `Dispose()` to prevent ViewModel retention

## Recommended Architecture

The WPF package should observe the in-memory log store only.
It should not write files, own the logger, or create the async queue.

```text
[Dreamine.Logging]
  Logger
    └─► AsyncQueueSink
          └─► CompositeLogSink
                ├─► InMemoryLogStore ──(LogAdded event)──┐
                └─► TextFileLogSink                      │
                                                         ▼
                                          [Dreamine.Logging.Wpf]
                                          DreamineLogPanelViewModel
                                            └─► DreamineLogPanelView
```

`DreamineLogPanelViewModel` is *not* a child of the sink chain.
It is a separate observer that subscribes to `InMemoryLogStore.LogAdded`,
and forwards updates to the UI thread in batches via `BatchedDispatcher<T>`.

## Why Batched UI Dispatch Exists

A log panel can receive log events from many worker threads.
If each log entry schedules a separate `Dispatcher.BeginInvoke`, the WPF Dispatcher queue can grow faster than the UI can process it.
This creates memory pressure and makes the UI feel frozen.

`BatchedDispatcher<T>` coalesces many incoming log entries into a limited UI-thread batch:

- producers enqueue from any thread;
- only one dispatcher operation is scheduled at a time;
- each UI batch processes up to `MaxBatchSize` entries;
- additional entries are processed in the next pass;
- visible entries are trimmed to the configured display capacity.

## Example XAML Usage

```xml
<UserControl x:Class="SampleSmart.Pages.PageSub.PageLog"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:logViews="clr-namespace:Dreamine.Logging.Wpf.Views;assembly=Dreamine.Logging.Wpf">
    <Grid>
        <logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
    </Grid>
</UserControl>
```

## Example ViewModel Wrapper

```csharp
public sealed class PageLogViewModel : ViewModelBase
{
    public DreamineLogPanelViewModel LogPanel { get; }

    public PageLogViewModel(DreamineLogPanelViewModel logPanel)
    {
        LogPanel = logPanel;
    }
}
```

## Example Registration

The log store must be registered by the core logging registration.
The WPF package only needs the UI dispatcher and log panel view model registration.

```csharp
DMContainer.RegisterSingleton<WpfLogUiDispatcher>(
    new WpfLogUiDispatcher());
DMContainer.RegisterSingleton<ILogUiDispatcher>(
    DMContainer.Resolve<WpfLogUiDispatcher>());

DMContainer.Register<DreamineLogPanelViewModel>(() =>
    new DreamineLogPanelViewModel(
        DMContainer.Resolve<IDreamineLogStore>(),
        DMContainer.Resolve<ILogUiDispatcher>()));
```

If a custom visible capacity is needed:

```csharp
DMContainer.Register<DreamineLogPanelViewModel>(() =>
    new DreamineLogPanelViewModel(
        DMContainer.Resolve<IDreamineLogStore>(),
        DMContainer.Resolve<ILogUiDispatcher>(),
        displayCapacity: 2000));
```

## Full Logging + WPF Registration Example

```csharp
private static IAsyncDisposable? _loggingShutdown;

private static void RegisterLogging()
{
    _loggingShutdown = DreamineLoggingRegistration.Register(new DreamineLoggingOptions
    {
        Category = "SampleSmart",
        LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs")
    });

    DMContainer.RegisterSingleton<WpfLogUiDispatcher>(new WpfLogUiDispatcher());
    DMContainer.RegisterSingleton<ILogUiDispatcher>(
        DMContainer.Resolve<WpfLogUiDispatcher>());

    DMContainer.Register<DreamineLogPanelViewModel>(() =>
        new DreamineLogPanelViewModel(
            DMContainer.Resolve<IDreamineLogStore>(),
            DMContainer.Resolve<ILogUiDispatcher>()));
}
```

## Shutdown

When the logger uses the async registration handle, dispose it on application shutdown.

```csharp
protected override void OnExit(ExitEventArgs e)
{
    try
    {
        _loggingShutdown?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
    catch
    {
        // Shutdown errors must not prevent process exit.
    }
    finally
    {
        base.OnExit(e);
    }
}
```

`OnExit` is a synchronous method, so use `GetAwaiter().GetResult()` rather than
`async void` to ensure the process does not exit before the queue is drained.

## Important Notes

`DreamineLogPanelView` must receive a `DreamineLogPanelViewModel` as its `DataContext`.
When wrapping the log panel inside another page, bind the inner view's `DataContext` explicitly.

```xml
<logViews:DreamineLogPanelView DataContext="{Binding LogPanel}" />
```

The detail text binding should use `Mode=OneWay` because the detail text is read-only.

```xml
<TextBox Text="{Binding SelectedDetailText, Mode=OneWay}" />
```

Dispose `DreamineLogPanelViewModel` when the owning page or window is permanently destroyed.
This unsubscribes from `IDreamineLogStore.LogAdded` and prevents the ViewModel from being retained by the event source.

## Performance Notes

- The visible `Entries` collection is capped, so it does not grow forever.
- UI updates are batched instead of scheduling one Dispatcher operation per log entry.
- The log panel is suitable for diagnostics and monitoring, not for storing unlimited historical logs.
- For long-term history, use `TextFileLogSink` or another persistent sink in `Dreamine.Logging`.
- Avoid logging every cycle in production thread loops unless diagnostic mode is enabled.

## Relationship with Dreamine.Logging

```text
Dreamine.Logging.Wpf
  -> Dreamine.Logging
```

The dependency direction must remain one-way.
The WPF package is a presentation adapter over the core logging package.

## Future Roadmap

- `DMContainer.UseDreamineLoggingWpf()` (registers UI dispatcher and panel view model in one line)
- `DMContainer.UseDreamineLoggingForWpf(...)` (a unified entry point that performs both core and WPF registration at once)
- Built-in clear command binding
- Log level filter
- Search/filter UI
- Export selected logs
- Optional auto-scroll behavior helper
- Optional status indicator for dropped async logs (UI exposing `AsyncQueueSink.DroppedCount`)

## License

MIT License
