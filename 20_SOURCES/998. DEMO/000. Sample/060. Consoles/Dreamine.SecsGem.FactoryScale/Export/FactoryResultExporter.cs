using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.FactoryScale.Models;

namespace Dreamine.SecsGem.FactoryScale.Export;

internal sealed class FactoryResultExporter : IAsyncDisposable
{
    private static int _liveWorkerCount;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly BoundedWorkQueue<ExportWork> _queue;
    private readonly ConcurrentDictionary<long, ExportWork> _pending = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Func<string, object, CancellationToken, Task> _writer;
    private readonly TimeSpan _drainTimeout;
    private readonly TimeSpan _cancellationTimeout;
    private readonly Task _worker;
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _nextWorkId;
    private int _disposed;

    internal FactoryResultExporter(
        int capacity = 64,
        TimeSpan? drainTimeout = null,
        TimeSpan? cancellationTimeout = null,
        Func<string, object, CancellationToken, Task>? writer = null)
    {
        _drainTimeout = drainTimeout ?? TimeSpan.FromSeconds(30);
        _cancellationTimeout = cancellationTimeout ?? TimeSpan.FromSeconds(5);
        if (_drainTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(drainTimeout));
        if (_cancellationTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(cancellationTimeout));
        _writer = writer ?? WriteAtomicAsync;
        _queue = new BoundedWorkQueue<ExportWork>("result-export", capacity,
            BoundedBusinessQueueFullPolicy.Wait, singleReader: true);
        _worker = RunAsync(_lifetime.Token);
    }

    internal static int LiveWorkerCount => Volatile.Read(ref _liveWorkerCount);
    internal FactoryQueueMetricSnapshot CaptureMetrics() => _queue.CaptureMetrics();

    internal Task ExportResultAsync(string path, FactoryRunResult result, CancellationToken cancellationToken) =>
        EnqueueAsync(path, result, cancellationToken);

    internal Task ExportSnapshotAsync(string path, FactoryMetricSnapshot snapshot, CancellationToken cancellationToken) =>
        EnqueueAsync(path, snapshot, cancellationToken);

    private async Task EnqueueAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var work = new ExportWork(Interlocked.Increment(ref _nextWorkId), Path.GetFullPath(path), value!,
            completion, cancellationToken);
        if (!_pending.TryAdd(work.Id, work))
            throw new InvalidOperationException("The result export work identifier was duplicated.");
        try
        {
            if (!await _queue.EnqueueAsync(work, cancellationToken).ConfigureAwait(false))
                throw new IOException("The bounded result export queue rejected the artifact.");
        }
        catch
        {
            _pending.TryRemove(work.Id, out _);
            throw;
        }
        await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _liveWorkerCount);
        try
        {
            await foreach (var work in _queue.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (work.CallerCancellation.IsCancellationRequested)
                {
                    CompleteCanceled(work, work.CallerCancellation);
                    continue;
                }
                try
                {
                    using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        work.CallerCancellation, cancellationToken);
                    await _writer(work.Path, work.Value, linkedCancellation.Token).ConfigureAwait(false);
                    Complete(work);
                }
                catch (OperationCanceledException) when (work.CallerCancellation.IsCancellationRequested)
                {
                    CompleteCanceled(work, work.CallerCancellation);
                }
                catch (Exception exception) { Complete(work, exception); }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            FailAllPending(exception);
            throw;
        }
        finally
        {
            FailAllPending(new ObjectDisposedException(nameof(FactoryResultExporter),
                "The result export worker stopped before completing queued work."));
            Interlocked.Decrement(ref _liveWorkerCount);
        }
    }

    private static async Task WriteAtomicAsync(string path, object value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Result path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             64 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, value.GetType(), JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }
        try
        {
            _queue.Complete();
            try { await _worker.WaitAsync(_drainTimeout).ConfigureAwait(false); }
            catch (TimeoutException)
            {
                var timeout = new TimeoutException(
                    $"The result export queue did not drain within {_drainTimeout}.");
                _lifetime.Cancel();
                FailAllPending(timeout);
                try { await _worker.WaitAsync(_cancellationTimeout).ConfigureAwait(false); }
                catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
                catch (TimeoutException)
                {
                    _ = _worker.ContinueWith(static task => _ = task.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    throw new TimeoutException(
                        $"The result export worker did not stop within {_cancellationTimeout} after cancellation.",
                        timeout);
                }
                throw timeout;
            }
            _disposeCompletion.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
            throw;
        }
        finally { _lifetime.Dispose(); }
    }

    private void Complete(ExportWork work)
    {
        if (_pending.TryRemove(work.Id, out _)) work.Completion.TrySetResult();
    }

    private void Complete(ExportWork work, Exception exception)
    {
        if (_pending.TryRemove(work.Id, out _)) work.Completion.TrySetException(exception);
    }

    private void CompleteCanceled(ExportWork work, CancellationToken cancellationToken)
    {
        if (_pending.TryRemove(work.Id, out _)) work.Completion.TrySetCanceled(cancellationToken);
    }

    private void FailAllPending(Exception exception)
    {
        foreach (var work in _pending.Values) Complete(work, exception);
    }

    private sealed record ExportWork(
        long Id,
        string Path,
        object Value,
        TaskCompletionSource Completion,
        CancellationToken CallerCancellation);
}
