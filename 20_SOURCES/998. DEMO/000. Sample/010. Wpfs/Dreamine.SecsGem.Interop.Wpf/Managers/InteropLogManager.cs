using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class InteropLogManager
{
    public const int MaximumEntries = 10_000;
    private const int DrainBatchSize = 250;
    private readonly Queue<InteropLogEntry> _pending = new();
    private readonly object _pendingGate = new();
    private readonly object _collectionGate = new();
    private int _drainScheduled;
    private int _paused;
    public ObservableCollection<InteropLogEntry> Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> Secs1Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> Secs2Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> DiagnosticsEntries { get; } = new();
    public event EventHandler<InteropLogEntry>? EntryAdded;

    public void Diagnostic(SecsDiagnosticEvent value) => Diagnostic(value, null);

    internal void Diagnostic(SecsDiagnosticEvent value, EquipmentLogIdentity? identity) => Add(WithIdentity(new(DateTimeOffset.Now,
        value.Kind is SecsDiagnosticKind.ProtocolError or SecsDiagnosticKind.Timeout ? "Error" : "Info",
        "HSMS", "--", value.Message, value.State?.ToString(), null), identity));

    public void Message(string direction, SecsMessage message) => Message(direction, message, null);

    internal void Message(string direction, SecsMessage message, EquipmentLogIdentity? identity)
    {
        var frame = new HsmsFrameCodec().Encode(new HsmsDataMessage(message));
        Add(WithIdentity(new(DateTimeOffset.Now, "Info", "SECS-II", direction,
            $"S{message.Stream.Value}F{message.Function.Value}{(message.ReplyExpected ? " W" : string.Empty)} SB=0x{message.SystemBytes.Value:X8}",
            SecsItemText.Format(message.Item), string.Join(" ", frame.Select(value => value.ToString("X2"))),
            CorrelationSystemBytes: message.SystemBytes.Value), identity));
    }

    public void Info(string category, string summary) => Add(new(DateTimeOffset.Now, "Info", category, "--", summary, null, null));
    public void Error(string category, Exception exception) => Add(new(DateTimeOffset.Now, "Error", category, "--", exception.Message, exception.GetType().Name, null));
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
        if (entry.Category == "SECS-II" && entry.RawHex is not null)
        {
            Secs1Entries.Add(entry);
            Secs2Entries.Add(entry);
            while (Secs1Entries.Count > MaximumEntries) Secs1Entries.RemoveAt(0);
            while (Secs2Entries.Count > MaximumEntries) Secs2Entries.RemoveAt(0);
        }
        else
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

internal sealed class EquipmentInteropDiagnosticSink(
    InteropLogManager log,
    Func<EquipmentLogIdentity> identity,
    Action<SecsDiagnosticEvent>? diagnosticObserved = null) : ISecsDiagnosticSink
{
    public void Emit(SecsDiagnosticEvent diagnosticEvent)
    {
        try { diagnosticObserved?.Invoke(diagnosticEvent); }
        finally { log.Diagnostic(diagnosticEvent, identity()); }
    }
}

internal static class SecsItemText
{
    public static string Format(SecsItem? item) => Format(item, 0);

    private static string Format(SecsItem? item, int depth)
    {
        var indent = new string(' ', depth * 2);
        return item switch
        {
            null => $"{indent}<none>",
            SecsListItem list when list.Count == 0 => $"{indent}<L[0]>",
            SecsListItem list => $"{indent}<L[{list.Count}]\n{string.Join(Environment.NewLine, list.Items.Select(value => Format(value, depth + 1)))}\n{indent}>",
            SecsAsciiItem ascii => $"{indent}<A[{ascii.Value.Length}] \"{ascii.Value}\">",
            SecsBinaryItem value => $"{indent}<B[{value.Values.Length}] {string.Join(" ", value.Values.ToArray().Select(x => $"0x{x:X2}"))}>",
            SecsBooleanItem value => Values(indent, "BOOLEAN", value.Values.ToArray()),
            SecsInt8Item value => Values(indent, "I1", value.Values.ToArray()),
            SecsInt16Item value => Values(indent, "I2", value.Values.ToArray()),
            SecsInt32Item value => Values(indent, "I4", value.Values.ToArray()),
            SecsInt64Item value => Values(indent, "I8", value.Values.ToArray()),
            SecsUInt8Item value => Values(indent, "U1", value.Values.ToArray()),
            SecsUInt16Item value => Values(indent, "U2", value.Values.ToArray()),
            SecsUInt32Item value => Values(indent, "U4", value.Values.ToArray()),
            SecsUInt64Item value => Values(indent, "U8", value.Values.ToArray()),
            SecsFloat32Item value => Values(indent, "F4", value.Values.ToArray()),
            SecsFloat64Item value => Values(indent, "F8", value.Values.ToArray()),
            _ => $"{indent}<{item.Format}>"
        };
    }

    private static string Values<T>(string indent, string format, T[] values) =>
        $"{indent}<{format}[{values.Length}] {string.Join(" ", values)}>";
}
