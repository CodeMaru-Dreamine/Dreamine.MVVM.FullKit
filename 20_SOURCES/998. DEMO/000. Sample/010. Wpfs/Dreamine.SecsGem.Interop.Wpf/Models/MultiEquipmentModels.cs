namespace Dreamine.SecsGem.Interop.Wpf.Models;

internal sealed record EquipmentScaleTiming(int EquipmentCount, double ConnectionElapsedMs);

internal sealed record MultiEquipmentSelfTestSummary(DateTimeOffset StartedAt, DateTimeOffset CompletedAt,
    int MaximumEquipment, int ConnectionsAttempted, int MessagesRequested, int MessagesPassed, int TimeoutCount,
    int ReconnectCount, long MemoryDeltaBytes, double ConnectionsPerSecond, double MessagesPerSecond,
    double MinimumLatencyMs, double AverageLatencyMs, double MaximumLatencyMs, int RemainingSessions,
    int RemainingBackgroundOperations, long TrackedOperationTaskCount, int PeakConcurrentEquipmentOperations,
    IReadOnlyList<EquipmentScaleTiming> ConnectionTimings, string Result, IReadOnlyList<string> Checks);

internal enum InteropHostMode
{
    SingleEquipment,
    MultiEquipmentHost
}
