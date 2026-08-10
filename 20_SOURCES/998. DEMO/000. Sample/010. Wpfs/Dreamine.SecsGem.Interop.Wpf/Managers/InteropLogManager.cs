using System.Collections.ObjectModel;
using System.Windows;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;
using Dreamine.SecsGem.Interop.Wpf.Models;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

public sealed class InteropLogManager
{
    private const int MaximumEntries = 5000;
    public ObservableCollection<InteropLogEntry> Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> Secs1Entries { get; } = new();
    public ObservableCollection<InteropLogEntry> Secs2Entries { get; } = new();
    public event EventHandler<InteropLogEntry>? EntryAdded;

    public void Diagnostic(SecsDiagnosticEvent value) => Add(new(DateTimeOffset.Now,
        value.Kind is SecsDiagnosticKind.ProtocolError or SecsDiagnosticKind.Timeout ? "Error" : "Info",
        "HSMS", "--", value.Message, value.State?.ToString(), null));

    public void Message(string direction, SecsMessage message)
    {
        var frame = new HsmsFrameCodec().Encode(new HsmsDataMessage(message));
        Add(new(DateTimeOffset.Now, "Info", "SECS-II", direction,
            $"S{message.Stream.Value}F{message.Function.Value}{(message.ReplyExpected ? " W" : string.Empty)} SB=0x{message.SystemBytes.Value:X8}",
            SecsItemText.Format(message.Item), string.Join(" ", frame.Select(value => value.ToString("X2")))));
    }

    public void Info(string category, string summary) => Add(new(DateTimeOffset.Now, "Info", category, "--", summary, null, null));
    public void Error(string category, Exception exception) => Add(new(DateTimeOffset.Now, "Error", category, "--", exception.Message, exception.GetType().Name, null));
    public void Clear() => Dispatch(() =>
    {
        Entries.Clear();
        Secs1Entries.Clear();
        Secs2Entries.Clear();
    });

    private void Add(InteropLogEntry entry) => Dispatch(() =>
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
        EntryAdded?.Invoke(this, entry);
    });

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
