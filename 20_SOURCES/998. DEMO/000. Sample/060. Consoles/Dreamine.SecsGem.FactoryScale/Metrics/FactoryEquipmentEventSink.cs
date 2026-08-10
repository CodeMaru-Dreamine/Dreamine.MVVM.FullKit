using System.Collections.Concurrent;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.FactoryScale.Infrastructure;
using Dreamine.SecsGem.Interop.Runtime;

namespace Dreamine.SecsGem.FactoryScale.Metrics;

internal sealed record FactoryDiagnosticRecord(
    DateTimeOffset TimestampUtc,
    string EquipmentId,
    string ConnectionId,
    string Kind,
    string Summary);

/// <summary>
/// Low-allocation Headless event sink. Protocol frames are counted from the
/// diagnostics emitted by HsmsSession; message bodies and raw hex are never
/// formatted on the load path. Only bounded diagnostic summaries are retained.
/// </summary>
internal sealed class FactoryEquipmentEventSink : IEquipmentEventSink, IAsyncDisposable
{
    private const int MaximumRecentDiagnostics = 256;
    private static int _liveDrainWorkerCount;
    private readonly FactoryMetricsCollector _metrics;
    private readonly BoundedDiagnosticQueue<FactoryDiagnosticRecord> _diagnostics;
    private readonly ConcurrentQueue<FactoryDiagnosticRecord> _recent = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _drainTask;
    private readonly TaskCompletionSource _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _disposed;

    internal FactoryEquipmentEventSink(FactoryMetricsCollector metrics, int diagnosticCapacity = 2_048)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _diagnostics = new BoundedDiagnosticQueue<FactoryDiagnosticRecord>("diagnostic-log", diagnosticCapacity);
        _drainTask = DrainAsync(_lifetime.Token);
    }

    internal IReadOnlyList<FactoryDiagnosticRecord> RecentDiagnostics => _recent.ToArray();
    internal BoundedDiagnosticQueue<FactoryDiagnosticRecord> Queue => _diagnostics;
    internal static int LiveDrainWorkerCount => Volatile.Read(ref _liveDrainWorkerCount);

    public void Diagnostic(EquipmentLogIdentity identity, SecsDiagnosticEvent value)
    {
        if (value.FrameLength is { } frameLength)
        {
            if (value.Kind == SecsDiagnosticKind.FrameSent) _metrics.AddBytesSent(frameLength);
            else if (value.Kind == SecsDiagnosticKind.FrameReceived) _metrics.AddBytesReceived(frameLength);
        }

        if (value.Kind == SecsDiagnosticKind.Timeout) _metrics.IncrementTimeout();
        if (value.Kind is not (SecsDiagnosticKind.Timeout or SecsDiagnosticKind.ProtocolError or
            SecsDiagnosticKind.ApplicationError or SecsDiagnosticKind.ConnectionClosed)) return;

        _diagnostics.TryWrite(new FactoryDiagnosticRecord(DateTimeOffset.UtcNow, identity.EquipmentId,
            identity.ConnectionId, value.Kind.ToString(), Limit(value.Message)));
    }

    public void Message(EquipmentLogIdentity identity, string direction, SecsMessage message)
    {
        // Metrics come from the request scheduler and frame diagnostics. Do not
        // encode the frame or stringify a potentially 1 MiB body here.
    }

    public void Info(EquipmentLogIdentity identity, string category, string summary) { }

    public void Error(EquipmentLogIdentity identity, string category, Exception exception)
    {
        _diagnostics.TryWrite(new FactoryDiagnosticRecord(DateTimeOffset.UtcNow, identity.EquipmentId,
            identity.ConnectionId, exception.GetType().Name, Limit(exception.Message)));
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _liveDrainWorkerCount);
        try
        {
            await foreach (var entry in _diagnostics.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                _recent.Enqueue(entry);
                while (_recent.Count > MaximumRecentDiagnostics) _recent.TryDequeue(out _);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        finally { Interlocked.Decrement(ref _liveDrainWorkerCount); }
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
            _diagnostics.Complete();
            try { await _drainTask.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false); }
            catch (TimeoutException)
            {
                _lifetime.Cancel();
                await _drainTask.ConfigureAwait(false);
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

    private static string Limit(string value) => value.Length <= 512 ? value : value[..512];
}
