using System.IO;
using Dreamine.Secs.Abstractions.Diagnostics;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

public enum EquipmentSidecarState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Exited,
    Faulted,
    Disposed
}

public enum EquipmentSidecarOutputStream
{
    StandardOutput,
    StandardError
}

/// <summary>
/// Classifies a sidecar output record independently from the process stream it
/// was written to. Some sidecars intentionally write all HSMS diagnostics to
/// stderr, including normal frame and state notifications.
/// </summary>
public enum EquipmentSidecarDiagnosticSeverity
{
    Information,
    Error
}

public enum EquipmentSidecarTcpStatus
{
    Unknown,
    Disconnected,
    Listening,
    Connected
}

public enum EquipmentSidecarHsmsStatus
{
    Unknown,
    NotConnected,
    ConnectedNotSelected,
    Selected
}

public sealed record EquipmentSidecarConnectionProjection(
    EquipmentSidecarTcpStatus Tcp,
    EquipmentSidecarHsmsStatus Hsms);

public sealed record EquipmentSidecarDiagnosticClassification(
    string Message,
    EquipmentSidecarDiagnosticSeverity Severity,
    SecsDiagnosticKind? DiagnosticKind,
    int? FrameLength,
    EquipmentSidecarConnectionProjection? Connection)
{
    public bool IsError => Severity == EquipmentSidecarDiagnosticSeverity.Error;
    public bool IsStructuredHsms => DiagnosticKind.HasValue;
}

public sealed record EquipmentSidecarStartOptions
{
    public string ExecutablePath { get; init; } = string.Empty;
    public string BindAddress { get; init; } = string.Empty;
    public int Port { get; init; }
    public ushort SessionId { get; init; }
    public int ConnectionLimit { get; init; }
    public bool RequestHostTime { get; init; } = true;
    public string EvidencePath { get; init; } = string.Empty;
    public bool TelemetryStdout { get; init; } = true;
    public TimeSpan NaturalExitGracePeriod { get; init; } = TimeSpan.FromSeconds(2);

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ExecutablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(BindAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(EvidencePath);

        var extension = Path.GetExtension(ExecutablePath);
        if (!extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The equipment sidecar must be an executable or a .NET assembly.",
                nameof(ExecutablePath));
        }

        if (!File.Exists(ExecutablePath))
            throw new FileNotFoundException("The equipment sidecar was not found.", ExecutablePath);
        if (Port is <= 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
        if (SessionId > 32767) throw new ArgumentOutOfRangeException(nameof(SessionId));
        if (ConnectionLimit <= 0) throw new ArgumentOutOfRangeException(nameof(ConnectionLimit));
        if (NaturalExitGracePeriod < TimeSpan.Zero || NaturalExitGracePeriod > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(NaturalExitGracePeriod));

        _ = Path.GetFullPath(ExecutablePath);
        _ = Path.GetFullPath(EvidencePath);
    }
}

public sealed class EquipmentSidecarOutputEventArgs : EventArgs
{
    public EquipmentSidecarOutputEventArgs(
        DateTimeOffset timestamp,
        EquipmentSidecarOutputStream stream,
        string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        Timestamp = timestamp;
        Stream = stream;
        Line = line;
    }

    public DateTimeOffset Timestamp { get; }
    public EquipmentSidecarOutputStream Stream { get; }
    public string Line { get; }
}

public sealed class EquipmentSidecarStateChangedEventArgs : EventArgs
{
    public EquipmentSidecarStateChangedEventArgs(
        EquipmentSidecarState previousState,
        EquipmentSidecarState currentState,
        string detail = "")
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Detail = detail ?? throw new ArgumentNullException(nameof(detail));
    }

    public EquipmentSidecarState PreviousState { get; }
    public EquipmentSidecarState CurrentState { get; }
    public string Detail { get; }
}

public sealed class EquipmentSidecarExitedEventArgs : EventArgs
{
    public EquipmentSidecarExitedEventArgs(int processId, int exitCode, bool forceStopped)
    {
        ProcessId = processId;
        ExitCode = exitCode;
        ForceStopped = forceStopped;
    }

    public int ProcessId { get; }
    public int ExitCode { get; }
    public bool ForceStopped { get; }
}
