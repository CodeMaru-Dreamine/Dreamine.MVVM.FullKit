namespace Dreamine.SecsGem.FactoryScale.Models;

internal enum FactoryRunStatus
{
    NotRun,
    Passed,
    Failed,
    Cancelled,
    Experimental
}

internal enum FactorySocketMetricSource
{
    Unavailable,
    HostRegistry,
    OperatingSystemProcessTable,
    InjectedTestProbe
}

internal sealed record FactoryLatencySnapshot(
    long SampleCount,
    double AverageMilliseconds,
    double P50Milliseconds,
    double P95Milliseconds,
    double P99Milliseconds,
    double MaximumMilliseconds,
    double MinimumMilliseconds,
    string Resolution);

internal sealed record FactoryQueueMetricSnapshot(
    string Name,
    int Capacity,
    int Depth,
    int PeakDepth,
    long Accepted,
    long Dequeued,
    long Rejected,
    long Dropped,
    string FullPolicy);

internal sealed record FactoryEquipmentMetricSnapshot(
    string EquipmentId,
    string ConnectionId,
    string Endpoint,
    ushort SessionId,
    bool Connected,
    bool Selected,
    long Requests,
    long Responses,
    long Timeouts,
    long Failures,
    long ReconnectAttempts,
    long ReconnectSucceeded,
    double AverageResponseMilliseconds,
    double MaximumResponseMilliseconds,
    double MaximumQueueDelayMilliseconds,
    DateTimeOffset? LastActivity,
    string LastError);

/// <summary>
/// A socket measurement is deliberately separate from process handles and from
/// FactoryScale's tracked session count. A null count means that the provider
/// could not make a process-owned operating-system measurement; it is never
/// silently replaced with a context count.
/// </summary>
internal sealed record FactorySocketMetricSnapshot(
    int? OpenSocketCount,
    int? ListenerCount,
    int? EstablishedCount,
    int? TimeWaitCount,
    FactorySocketMetricSource Source,
    bool IsProcessOwnedMeasurement,
    string? Detail)
{
    internal static FactorySocketMetricSnapshot Unavailable(string? detail = null) =>
        new(null, null, null, null, FactorySocketMetricSource.Unavailable, false,
            detail ?? "A process-owned socket metric provider was not configured.");
}

internal sealed record FactoryProcessMetricSnapshot(
    int ProcessId,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    long ManagedHeapBytes,
    long LastGcHeapSizeBytes,
    long LastGcFragmentedBytes,
    long LastGcCommittedBytes,
    long TotalAllocatedBytes,
    long AllocatedBytesSinceStart,
    int Gen0CollectionsSinceStart,
    int Gen1CollectionsSinceStart,
    int Gen2CollectionsSinceStart,
    int? ThreadCount,
    int? HandleCount,
    long ThreadPoolPendingWorkItems,
    long ThreadPoolCompletedWorkItemsSinceStart,
    int ThreadPoolWorkerThreadsInUse,
    int ThreadPoolIoThreadsInUse,
    FactorySocketMetricSnapshot Sockets);

internal sealed record FactoryRuntimeMetricInput
{
    internal int RequestedEquipment { get; init; }
    internal int ConnectedEquipment { get; init; }
    internal int SelectedEquipment { get; init; }
    internal int FailedEquipment { get; init; }
    internal int ReconnectingEquipment { get; init; }
    internal int PendingTransactions { get; init; }
    internal int PeakPendingTransactions { get; init; }
    internal int PendingControlTransactions { get; init; }
    internal int PeakPendingControlTransactions { get; init; }
    internal int TrackedOperations { get; init; }
    internal int PeakTrackedOperations { get; init; }
    internal int ReconnectOperations { get; init; }
    internal int PeakReconnectOperations { get; init; }
    internal int TrackedSessions { get; init; }
    internal int TrackedListeners { get; init; }
    internal FactorySocketMetricSnapshot? SocketMetric { get; init; }
    internal IReadOnlyList<FactoryQueueMetricSnapshot> Queues { get; init; } = Array.Empty<FactoryQueueMetricSnapshot>();
}

internal sealed record FactoryMetricSnapshot(
    DateTimeOffset TimestampUtc,
    TimeSpan Elapsed,
    int RequestedEquipment,
    int ConnectedEquipment,
    int SelectedEquipment,
    int FailedEquipment,
    int ReconnectingEquipment,
    long Requests,
    long Responses,
    long Timeouts,
    long ReconnectAttempts,
    long ReconnectSucceeded,
    long Failures,
    long CorrelationErrors,
    long BytesSent,
    long BytesReceived,
    double RequestsPerSecond,
    double ResponsesPerSecond,
    double MessagesPerSecond,
    double BytesPerSecond,
    int PendingTransactions,
    int PeakPendingTransactions,
    int PendingControlTransactions,
    int PeakPendingControlTransactions,
    int TrackedOperations,
    int PeakTrackedOperations,
    int ReconnectOperations,
    int PeakReconnectOperations,
    int TrackedSessions,
    int TrackedListeners,
    long DroppedDiagnosticLogs,
    FactoryLatencySnapshot ResponseLatency,
    FactoryProcessMetricSnapshot Process,
    IReadOnlyList<FactoryQueueMetricSnapshot> Queues)
{
    public FactoryOwnedWorkerSnapshot OwnedWorkers { get; init; } = new(0, 0, 0, 0);
}

internal sealed record FactoryOwnedWorkerSnapshot(
    int HostMessageWorkers,
    int EquipmentResponderWorkers,
    int DiagnosticDrainWorkers,
    int ResultExportWorkers)
{
    internal int Total => checked(HostMessageWorkers + EquipmentResponderWorkers +
                                  DiagnosticDrainWorkers + ResultExportWorkers);
}

internal sealed record FactoryRunResult(
    int SchemaVersion,
    string Scenario,
    string ExecutionMode,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    FactoryRunStatus Status,
    FactoryMetricSnapshot Final,
    FactoryMetricSnapshot CleanupBeforeForcedGc,
    FactoryMetricSnapshot CleanupAfterForcedGc,
    IReadOnlyList<FactoryMetricSnapshot> Snapshots,
    IReadOnlyList<string> Checks,
    string? FirstFailure,
    string EvidenceScope = "Self-loopback evidence; not external-equipment compatibility or compliance evidence")
{
    public IReadOnlyList<FactoryEquipmentMetricSnapshot> EquipmentMetrics { get; init; } =
        Array.Empty<FactoryEquipmentMetricSnapshot>();
}
