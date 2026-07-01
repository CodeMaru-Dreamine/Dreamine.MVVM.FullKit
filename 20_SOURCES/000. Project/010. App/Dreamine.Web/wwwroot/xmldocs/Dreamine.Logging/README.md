# Dreamine.Logging

Dreamine.Logging provides the core logging infrastructure for Dreamine applications.
It defines logger abstractions, structured log entries, in-memory diagnostics storage, text formatting, composite sinks, daily text file output, and an async queue sink for high-frequency multi-threaded logging.

[➡️ 한국어 문서 보기](./README_KO.md)

## Purpose

This package is the UI-independent logging core for Dreamine-based applications.
It is designed to keep caller threads free from direct file I/O and UI update costs by separating log creation from log output.

`Dreamine.Logging` must not depend on WPF or any other presentation framework.
WPF-specific log panels are provided by `Dreamine.Logging.Wpf`.

## Features

- `IDreamineLogger` abstraction
- `DreamineLogger` default logger implementation
- Log levels: `Trace`, `Debug`, `Info`, `Warning`, `Error`, `Fatal`
- `DreamineLogEntry` structured log model
- `IDreamineLogSink` for pluggable output targets
- `CompositeLogSink` for writing to multiple sinks
- `AsyncQueueSink` for bounded, background, non-blocking log dispatch
- `InMemoryLogStore`: bounded ring buffer (default capacity 1000, O(1) eviction) for runtime diagnostics and UI integration
- `DreamineTextLogFormatter` for plain text formatting
- `TextFileLogSink` for daily text log files with buffered writes and controlled flush
- Graceful shutdown support through `AsyncQueueSink.ShutdownAsync(...)`

## Recommended Architecture

For normal applications, place `AsyncQueueSink` between the logger and the actual sink chain.
This keeps worker threads from blocking on file I/O or UI-bound storage updates.

```text
Application threads
  -> IDreamineLogger
     -> DreamineLogger
        -> AsyncQueueSink
           -> CompositeLogSink
              ├─► InMemoryLogStore   (raises LogAdded events)
              └─► TextFileLogSink
```

`DreamineLogPanelViewModel` is *not* part of this chain.
The view model is a separate observer that subscribes to `InMemoryLogStore.LogAdded`.
See the `Dreamine.Logging.Wpf` document for the WPF presentation flow.

## Why AsyncQueueSink Exists

A logging system is often called from thread loops, background workers, communication handlers, motion/IO polling jobs, and UI commands.
If every log call writes directly to file or updates UI-bound storage, the caller thread can be delayed.
Under sustained logging, the application can also accumulate pending dispatcher operations or unbounded visible log entries.

`AsyncQueueSink` solves the caller-thread delay problem:

- producers enqueue log entries quickly;
- one background worker drains the queue;
- the queue is bounded by capacity;
- when the queue is full, the oldest pending entry is dropped instead of blocking the caller (`BoundedChannelFullMode.DropOldest`);
- the cumulative drop count is observable via the `AsyncQueueSink.DroppedCount` property;
- shutdown can drain pending logs before process exit.

The WPF-side display bound is handled by `Dreamine.Logging.Wpf`.

## Example Registration

The built-in registration configures the sink chain and returns an async-disposable shutdown handle.

```csharp
private static IAsyncDisposable? _loggingShutdown;

private static void RegisterLogging()
{
    _loggingShutdown = DreamineLoggingRegistration.Register(new DreamineLoggingOptions
    {
        Category = "SampleSmart",
        LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs")
    });
}
```

## Shutdown

When the async registration handle is used, dispose it during application shutdown.
This prevents recent logs from being lost and also disposes the inner sink chain, including file handles.

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

## Text File Output

`TextFileLogSink` writes daily log files.

```text
Logs/yyyy-MM-dd.log
```

Example:

```text
[2026-05-09 18:30:22.718] [Info] [SampleSmart] [T1] Application initialized.
```

`TextFileLogSink` keeps the current daily file open instead of opening and closing the file for every entry.
It flushes after a configured number of writes and flushes immediately for `Error` or higher entries.

## Thread Safety

`DreamineLogger`, `AsyncQueueSink`, `CompositeLogSink`, `InMemoryLogStore`, and `TextFileLogSink`
are designed for shared, application-wide singleton usage.
The recommended registration is one logger instance, one async sink, and one sink chain for the application.

`TextFileLogSink` and `InMemoryLogStore` are guarded by internal locks, so they remain
thread-safe even when called directly without `AsyncQueueSink`. Routing through the
queue is still recommended to avoid blocking caller threads on file I/O.

For high-frequency loops, avoid writing logs on every cycle in production.
Prefer logging only on:

- state changes;
- warnings or errors;
- operation start/end;
- diagnostic mode;
- throttled intervals.

## Important Notes

- Register `IDreamineLogger` as a `DreamineLogger` whose sink chain includes `AsyncQueueSink`.
- Register `IDreamineLogSink` as the `AsyncQueueSink` when external consumers may resolve a sink directly.
- Do not include the same sink instance both in a logger's sink list and inside a `CompositeLogSink` registered to the same logger; the entry would be written twice.
- Keep WPF UI components out of this package.
- Use `Dreamine.Logging.Wpf` for visible log panels.

## Relationship with Dreamine.Logging.Wpf

```text
Dreamine.Logging.Wpf
  -> Dreamine.Logging
```

The dependency direction must stay one-way.
`Dreamine.Logging` is the core layer; `Dreamine.Logging.Wpf` is the presentation adapter.

## Future Roadmap

- `DreamineLoggerOptions` (a single options object combining capacity, batch size, file directory, etc.)
- `DMContainer.UseDreamineLogging(...)` (an extension method that collapses the registration boilerplate above into one line)
- A `DreamineLogger` convenience constructor for `(single sink, level, category)`
- File rolling policy (size- or count-based, in addition to daily)
- Database sink
- External logging service sink
- Dropped-entry reporting hook for `AsyncQueueSink` (event-based notification)
- Optional log throttling helper

## License

MIT License
