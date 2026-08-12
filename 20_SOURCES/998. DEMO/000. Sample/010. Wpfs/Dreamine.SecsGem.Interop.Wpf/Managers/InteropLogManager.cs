using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.SecsGem.Interop.Runtime;
using Dreamine.SecsGem.Interop.Runtime.Logging;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class InteropLogManager
{
    public const int MaximumEntries = 10_000;
    internal const int MaximumWireDisplayBytes = 64 * 1024;
    private const int DrainBatchSize = 250;
    private readonly Queue<InteropLogEntry> _pending = new();
    private readonly object _pendingGate = new();
    private readonly object _collectionGate = new();
    private IEquipmentEventSink? _equipmentSink;
    private int _drainScheduled;
    private int _paused;
    public ObservableCollection<InteropLogEntry> Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> Secs1Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> Secs2Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> DiagnosticsEntries { get; } = new();
    public event EventHandler<InteropLogEntry>? EntryAdded;
    internal IEquipmentEventSink EquipmentSink =>
        _equipmentSink ?? Interlocked.CompareExchange(ref _equipmentSink, new WpfEquipmentEventSink(this), null) ?? _equipmentSink!;

    public void Diagnostic(SecsDiagnosticEvent value) => Diagnostic(value, null);

    internal void Diagnostic(SecsDiagnosticEvent value, EquipmentLogIdentity? identity) => Add(WithIdentity(new(DateTimeOffset.Now,
        value.Kind is SecsDiagnosticKind.ProtocolError or SecsDiagnosticKind.Timeout ? "Error" : "Info",
        "HSMS", "--", value.Message, value.State?.ToString(), null), identity));

    public void Message(string direction, SecsMessage message) => Message(direction, message, null);

    internal void Message(string direction, SecsMessage message, EquipmentLogIdentity? identity)
    {
        // This is an application-level semantic event. Exact bytes are supplied separately by
        // the session wire observer; re-encoding here would mislabel canonicalized bytes as wire data.
        Add(WithIdentity(new(DateTimeOffset.Now, "Info", "SECS-II", direction,
            $"S{message.Stream.Value}F{message.Function.Value}{(message.ReplyExpected ? " W" : string.Empty)} SB=0x{message.SystemBytes.Value:X8}",
            SecsItemText.Format(message.Item), null,
            CorrelationSystemBytes: message.SystemBytes.Value), identity));
    }

    internal void Wire(WireLogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.Kind != WireLogRecordKind.Frame)
        {
            Operational(record);
            return;
        }
        var category = record.Stream is null ? "HSMS" : "SECS-II";
        var direction = record.Direction switch
        {
            HsmsWireDirection.Inbound => "RX",
            HsmsWireDirection.Outbound => "TX",
            _ => "--"
        };
        var protocol = record.Stream is { } stream && record.Function is { } function
            ? $"S{stream}F{function}{(record.ReplyExpected == true ? " W" : string.Empty)}"
            : $"SType={record.SType?.ToString() ?? "--"}";
        var correlation = record.SystemBytes is { } systemBytes ? $" SB=0x{systemBytes:X8}" : string.Empty;
        var captured = (record.HeaderBytes?.Length ?? 0) + (record.BodyBytes?.Length ?? 0);
        var summary = $"{protocol}{correlation} FrameBytes={record.ActualFrameBytes} " +
            $"BodyBytes={record.CapturedBodyBytes} Capture={record.CaptureMode}";
        var detail = record.DecodedItem ?? record.TransactionStatus ?? record.DecodeError ?? record.Error;
        Add(new InteropLogEntry(
            record.TimestampUtc,
            record.Error is not null || record.DecodeError is not null ? "Error" : "Info",
            category,
            direction,
            summary,
            detail,
            FormatWireBytes(record.HeaderBytes, record.BodyBytes, captured),
            record.EquipmentId,
            record.ConnectionId,
            record.Endpoint,
            record.HeaderSessionId ?? record.ConfiguredSessionId,
            record.SystemBytes));
    }

    private void Operational(WireLogRecord record)
    {
        var isError = record.Error is not null;
        var summary = record.Kind switch
        {
            WireLogRecordKind.Diagnostic =>
                $"{record.DiagnosticKind?.ToString() ?? "Diagnostic"}; HSMS={record.CurrentHsmsState?.ToString() ?? "--"}",
            WireLogRecordKind.StateTransition =>
                $"TCP {record.PreviousConnectionState?.ToString() ?? "--"} -> {record.CurrentConnectionState?.ToString() ?? "--"}; " +
                $"HSMS {record.PreviousHsmsState?.ToString() ?? "--"} -> {record.CurrentHsmsState?.ToString() ?? "--"}",
            _ => "Operational log record"
        };
        Add(new InteropLogEntry(
            record.TimestampUtc,
            isError ? "Error" : "Info",
            record.Kind == WireLogRecordKind.StateTransition ? "State" : "Diagnostic",
            "--",
            summary,
            record.DiagnosticMessage,
            null,
            record.EquipmentId,
            record.ConnectionId,
            record.Endpoint,
            record.HeaderSessionId ?? record.ConfiguredSessionId,
            record.SystemBytes));
    }

    public void Info(string category, string summary) => Add(new(DateTimeOffset.Now, "Info", category, "--", summary, null, null));
    public void Error(string category, Exception exception) => Add(new(DateTimeOffset.Now, "Error", category, "--", exception.Message, exception.GetType().Name, null));
    internal void SidecarTelemetry(EquipmentSidecarTelemetry telemetry, EquipmentLogIdentity identity)
    {
        if (telemetry.Message is { } message && telemetry.Direction is "Inbound" or "Outbound")
        {
            var direction = telemetry.Direction == "Inbound" ? "RX" : "TX";
            var raw = telemetry.RawHex.Length == 0
                ? null
                : string.Join(" ", Enumerable.Range(0, telemetry.RawHex.Length / 2)
                    .Select(index => telemetry.RawHex.Substring(index * 2, 2)));
            Add(WithIdentity(new InteropLogEntry(
                telemetry.Timestamp,
                string.IsNullOrEmpty(telemetry.Timeout) ? "Info" : "Error",
                "SECS-II",
                direction,
                $"S{message.Stream.Value}F{message.Function.Value}{(message.ReplyExpected ? " W" : string.Empty)} " +
                $"SB=0x{message.SystemBytes.Value:X8} [{telemetry.Result}]",
                SecsItemText.Format(message.Item),
                raw,
                CorrelationSystemBytes: message.SystemBytes.Value,
                ContainsPrivateProfileData: true), identity));
            return;
        }

        var summary = $"External equipment sidecar {telemetry.Direction}: {telemetry.Result}.";
        if (!string.IsNullOrEmpty(telemetry.Timeout)) summary += $" Timeout: {telemetry.Timeout}.";
        Add(WithIdentity(new InteropLogEntry(
            telemetry.Timestamp,
            string.IsNullOrEmpty(telemetry.Timeout) ? "Info" : "Error",
            "External Equipment",
            "--",
            summary,
            telemetry.RawStatus,
            null,
            ContainsPrivateProfileData: true), identity));
    }

    internal void SidecarOutput(
        DateTimeOffset timestamp,
        bool error,
        string line,
        EquipmentLogIdentity identity) =>
        Add(WithIdentity(new InteropLogEntry(
            timestamp,
            error ? "Error" : "Info",
            "External Equipment",
            "--",
            line,
            null,
            null,
            ContainsPrivateProfileData: true), identity));
    internal void Info(string category, string summary, EquipmentLogIdentity identity) =>
        Add(WithIdentity(new(DateTimeOffset.Now, "Info", category, "--", summary, null, null), identity));
    internal void Error(string category, Exception exception, EquipmentLogIdentity identity) =>
        Add(WithIdentity(new(DateTimeOffset.Now, "Error", category, "--", exception.Message, exception.GetType().Name, null), identity));
    public bool IsPaused => Volatile.Read(ref _paused) != 0;

    public void SetPaused(bool paused)
    {
        Volatile.Write(ref _paused, paused ? 1 : 0);
        if (!paused) ScheduleDrain();
    }

    public void Clear() => Dispatch(() =>
    {
        lock (_pendingGate) _pending.Clear();
        lock (_collectionGate)
        {
            Entries.Clear();
            Secs1Entries.Clear();
            Secs2Entries.Clear();
            DiagnosticsEntries.Clear();
        }
    });

    private void Add(InteropLogEntry entry)
    {
        lock (_pendingGate)
        {
            _pending.Enqueue(entry);
            while (_pending.Count > MaximumEntries) _pending.Dequeue();
        }
        ScheduleDrain();
    }

    private void ScheduleDrain()
    {
        if (IsPaused || Interlocked.Exchange(ref _drainScheduled, 1) != 0) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) DrainPending();
        else _ = dispatcher.BeginInvoke(DispatcherPriority.Background, DrainPending);
    }

    private void DrainPending()
    {
        try
        {
            if (IsPaused) return;
            var batch = new List<InteropLogEntry>(DrainBatchSize);
            lock (_pendingGate)
            {
                while (batch.Count < DrainBatchSize && _pending.Count > 0) batch.Add(_pending.Dequeue());
            }
            lock (_collectionGate)
            foreach (var entry in batch) AddCore(entry);
        }
        finally
        {
            Interlocked.Exchange(ref _drainScheduled, 0);
            if (!IsPaused && HasPendingEntries()) ScheduleDrain();
        }
    }

    private bool HasPendingEntries()
    {
        lock (_pendingGate) return _pending.Count > 0;
    }

    private void AddCore(InteropLogEntry entry)
    {
        Entries.Add(entry);
        while (Entries.Count > MaximumEntries) Entries.RemoveAt(0);
        var addedToProtocolView = false;
        if (entry.Category is "SECS-II" or "HSMS" && entry.RawHex is not null)
        {
            Secs1Entries.Add(entry);
            addedToProtocolView = true;
            while (Secs1Entries.Count > MaximumEntries) Secs1Entries.RemoveAt(0);
        }
        if (entry.Category == "SECS-II" && entry.Message is not null)
        {
            Secs2Entries.Add(entry);
            addedToProtocolView = true;
            while (Secs2Entries.Count > MaximumEntries) Secs2Entries.RemoveAt(0);
        }
        if (!addedToProtocolView)
        {
            DiagnosticsEntries.Add(entry);
            while (DiagnosticsEntries.Count > MaximumEntries) DiagnosticsEntries.RemoveAt(0);
        }
        EntryAdded?.Invoke(this, entry);
    }

    private static InteropLogEntry WithIdentity(InteropLogEntry entry, EquipmentLogIdentity? identity) => identity is { } value
        ? entry with
        {
            EquipmentId = value.EquipmentId,
            ConnectionId = value.ConnectionId,
            Endpoint = value.Endpoint,
            SessionId = value.SessionId
        }
        : entry;

    private static string? FormatWireBytes(byte[]? header, byte[]? body, int captured)
    {
        if (captured == 0) return null;
        var displayed = Math.Min(captured, MaximumWireDisplayBytes);
        var builder = new StringBuilder(displayed * 3 + 32);
        var written = 0;
        Append(header);
        Append(body);
        if (captured > displayed) builder.Append(" … [display truncated]");
        return builder.ToString();

        void Append(byte[]? bytes)
        {
            if (bytes is null) return;
            foreach (var value in bytes)
            {
                if (written >= displayed) return;
                if (written++ > 0) builder.Append(' ');
                builder.Append(value.ToString("X2"));
            }
        }
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else _ = dispatcher.BeginInvoke(action);
    }
}

internal sealed class InteropDiagnosticSink(
    InteropLogManager log,
    Action<SecsDiagnosticEvent>? diagnosticObserved = null) : ISecsDiagnosticSink
{
    public void Emit(SecsDiagnosticEvent diagnosticEvent)
    {
        // A bound log view must never delay HSMS control-frame processing. Queue the
        // log entry, then independently notify connection-state observers.
        try { log.Diagnostic(diagnosticEvent); }
        finally { diagnosticObserved?.Invoke(diagnosticEvent); }
    }
}

internal sealed class WpfEquipmentEventSink(InteropLogManager log) : IEquipmentEventSink
{
    public void Diagnostic(EquipmentLogIdentity identity, SecsDiagnosticEvent value) => log.Diagnostic(value, identity);
    public void Message(EquipmentLogIdentity identity, string direction, SecsMessage message) => log.Message(direction, message, identity);
    public void Info(EquipmentLogIdentity identity, string category, string summary) => log.Info(category, summary, identity);
    public void Error(EquipmentLogIdentity identity, string category, Exception exception) => log.Error(category, exception, identity);
}

internal static class SecsItemText
{
    public static string Format(SecsItem? item) =>
        BoundedSecsItemFormatter.Format(item, InteropLogManager.MaximumWireDisplayBytes);
}
