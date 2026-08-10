using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.SecsGem.Interop.Runtime;

internal interface IEquipmentEventSink
{
    void Diagnostic(EquipmentLogIdentity identity, SecsDiagnosticEvent value);
    void Message(EquipmentLogIdentity identity, string direction, SecsMessage message);
    void Info(EquipmentLogIdentity identity, string category, string summary);
    void Error(EquipmentLogIdentity identity, string category, Exception exception);
}

internal sealed class NullEquipmentEventSink : IEquipmentEventSink
{
    internal static NullEquipmentEventSink Instance { get; } = new();
    private NullEquipmentEventSink() { }
    public void Diagnostic(EquipmentLogIdentity identity, SecsDiagnosticEvent value) { }
    public void Message(EquipmentLogIdentity identity, string direction, SecsMessage message) { }
    public void Info(EquipmentLogIdentity identity, string category, string summary) { }
    public void Error(EquipmentLogIdentity identity, string category, Exception exception) { }
}

internal sealed class SafeEquipmentEventSink : IEquipmentEventSink
{
    private readonly IEquipmentEventSink _inner;
    internal SafeEquipmentEventSink(IEquipmentEventSink? inner) => _inner = inner ?? NullEquipmentEventSink.Instance;

    public void Diagnostic(EquipmentLogIdentity identity, SecsDiagnosticEvent value) =>
        Invoke(() => _inner.Diagnostic(identity, value));
    public void Message(EquipmentLogIdentity identity, string direction, SecsMessage message) =>
        Invoke(() => _inner.Message(identity, direction, message));
    public void Info(EquipmentLogIdentity identity, string category, string summary) =>
        Invoke(() => _inner.Info(identity, category, summary));
    public void Error(EquipmentLogIdentity identity, string category, Exception exception) =>
        Invoke(() => _inner.Error(identity, category, exception));

    private static void Invoke(Action action)
    {
        try { action(); }
        catch (Exception exception) { System.Diagnostics.Trace.TraceError("Equipment event sink failed: {0}", exception); }
    }
}

internal sealed class EquipmentDiagnosticSink(
    IEquipmentEventSink sink,
    Func<EquipmentLogIdentity> identity,
    Action<SecsDiagnosticEvent>? diagnosticObserved = null) : ISecsDiagnosticSink
{
    public void Emit(SecsDiagnosticEvent diagnosticEvent)
    {
        try { diagnosticObserved?.Invoke(diagnosticEvent); }
        finally { sink.Diagnostic(identity(), diagnosticEvent); }
    }
}
