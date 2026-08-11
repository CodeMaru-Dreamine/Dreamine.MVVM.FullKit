using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

public sealed partial class ConnectionSettings : ViewModelBase
{
    [DreamineProperty]
    private string _host = "127.0.0.1";
    [DreamineProperty]
    private int _port = 7000;
    [DreamineProperty]
    private SecsConnectionMode _mode = SecsConnectionMode.Active;
    [DreamineProperty]
    private SecsRole _role = SecsRole.Host;
    [DreamineProperty]
    private ushort _sessionId;
    [DreamineProperty]
    private bool _autoReconnect;
    [DreamineProperty]
    private int _t3Seconds = 45;
    [DreamineProperty]
    private int _t5Seconds = 10;
    [DreamineProperty]
    private int _t6Seconds = 10;
    [DreamineProperty]
    private int _t7Seconds = 10;
    [DreamineProperty]
    private int _t8Seconds = 10;

    public HsmsSessionOptions ToOptions() => new()
    {
        Host = Host,
        Port = Port,
        Mode = Mode,
        Role = Role,
        SessionId = new SecsSessionId(SessionId),
        AutoReconnect = AutoReconnect,
        Timers = new HsmsTimerOptions
        {
            T3 = TimeSpan.FromSeconds(T3Seconds), T5 = TimeSpan.FromSeconds(T5Seconds),
            T6 = TimeSpan.FromSeconds(T6Seconds), T7 = TimeSpan.FromSeconds(T7Seconds),
            T8 = TimeSpan.FromSeconds(T8Seconds)
        }
    };
}

public enum InteropScenarioStatus { NotRun, Running, WaitingForUser, Passed, Failed, NotSupported }

public sealed partial class InteropScenarioResult : ViewModelBase
{
    [DreamineProperty]
    private InteropScenarioStatus _status;
    [DreamineProperty]
    private string _detail = "Not Run";
    [DreamineProperty]
    private TimeSpan _elapsed;
    public InteropScenarioResult(string id, string name, bool external) { Id = id; Name = name; External = external; }
    public string Id { get; }
    public string Name { get; }
    public bool External { get; }
}

public sealed record InteropLogEntry(DateTimeOffset Timestamp, string Level, string Category, string Direction,
    string Summary, string? Message, string? RawHex, string EquipmentId = "", string ConnectionId = "",
    string Endpoint = "", ushort? SessionId = null, uint? CorrelationSystemBytes = null,
    bool ContainsPrivateProfileData = false)
{
    public string SxFy
    {
        get
        {
            if (Category != "SECS-II") return "--";
            var start = Summary.IndexOf('S');
            var end = start < 0 ? -1 : Summary.IndexOf(' ', start);
            return start < 0 ? "--" : end < 0 ? Summary[start..] : Summary[start..end];
        }
    }

    public string SystemBytes
    {
        get
        {
            if (CorrelationSystemBytes is { } value) return $"0x{value:X8}";
            const string marker = "SB=";
            var start = Summary.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "--";
            start += marker.Length;
            var end = Summary.IndexOf(' ', start);
            return end < 0 ? Summary[start..] : Summary[start..end];
        }
    }

    public string Result => Level == "Error" ? "Error" : Category == "SECS-II" ? "Transferred" : "Observed";
    public string CorrelationKey => string.IsNullOrEmpty(ConnectionId) || SessionId is null || CorrelationSystemBytes is null
        ? "--"
        : $"{ConnectionId}:{SessionId.Value}:{CorrelationSystemBytes.Value:X8}";
}

public sealed record SelfTestSummary(DateTimeOffset StartedAt, DateTimeOffset CompletedAt, int Requested,
    int Passed, int Failed, int TimeoutCount, int ReconnectCycles, int LinktestCount,
    double MessagesPerSecond, double MinimumLatencyMs, double AverageLatencyMs, double MaximumLatencyMs,
    double P95LatencyMs, string? FirstFailure, string Result, IReadOnlyList<string> Checks);

public enum SecsItemEditorFormat
{
    List, Binary, Boolean, Ascii, Int8, Int16, Int32, Int64, UInt8, UInt16, UInt32, UInt64, Float32, Float64
}
