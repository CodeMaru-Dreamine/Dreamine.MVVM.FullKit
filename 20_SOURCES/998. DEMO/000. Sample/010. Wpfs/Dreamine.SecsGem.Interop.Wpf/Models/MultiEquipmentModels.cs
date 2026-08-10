using Dreamine.MVVM.ViewModels;
using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

internal sealed class MultiEquipmentHostOptions
{
    public int ConnectConcurrency { get; init; } = 10;
    public int MessageConcurrency { get; init; } = 20;
    public int ReconnectConcurrency { get; init; } = 5;

    public void Validate()
    {
        if (ConnectConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(ConnectConcurrency));
        if (MessageConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(MessageConcurrency));
        if (ReconnectConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(ReconnectConcurrency));
    }
}

internal sealed class EquipmentConnectionDefinition
{
    public string EquipmentId { get; init; } = string.Empty;
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; }
    public SecsConnectionMode Mode { get; init; } = SecsConnectionMode.Active;
    public ushort SessionId { get; init; }
    public bool AutoReconnect { get; init; }
    public int T3Seconds { get; init; } = 10;
    public int T5Seconds { get; init; } = 2;
    public int T6Seconds { get; init; } = 5;
    public int T7Seconds { get; init; } = 10;
    public int T8Seconds { get; init; } = 5;

    public string Endpoint => $"{Host}:{Port}";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(EquipmentId)) throw new ArgumentException("EquipmentId is required.", nameof(EquipmentId));
        if (string.IsNullOrWhiteSpace(Host)) throw new ArgumentException("Host is required.", nameof(Host));
        if (Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
        if (!Enum.IsDefined(Mode) || Mode == SecsConnectionMode.Unspecified) throw new ArgumentOutOfRangeException(nameof(Mode));
        if (SessionId > SecsSessionId.MaximumValue) throw new ArgumentOutOfRangeException(nameof(SessionId));
        if (AutoReconnect && Mode != SecsConnectionMode.Active)
            throw new ArgumentException("Automatic reconnect is available only in Active mode.", nameof(AutoReconnect));
        ValidateTimer(T3Seconds, 120, nameof(T3Seconds));
        ValidateTimer(T5Seconds, 240, nameof(T5Seconds));
        ValidateTimer(T6Seconds, 240, nameof(T6Seconds));
        ValidateTimer(T7Seconds, 240, nameof(T7Seconds));
        ValidateTimer(T8Seconds, 120, nameof(T8Seconds));
    }

    private static void ValidateTimer(int value, int maximum, string name)
    {
        if (value is < 1 || value > maximum)
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between 1 and {maximum} seconds.");
    }
}

internal sealed record EquipmentOperationResult(string EquipmentId, bool Succeeded, string Detail, TimeSpan Elapsed);

internal readonly record struct EquipmentLogIdentity(string EquipmentId, string ConnectionId, string Endpoint,
    ushort SessionId);

internal sealed record MultiEquipmentConfiguration(int SchemaVersion, MultiEquipmentHostOptions Options,
    IReadOnlyList<EquipmentConnectionDefinition> Equipment);

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
