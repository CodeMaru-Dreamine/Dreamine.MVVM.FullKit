using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

public sealed class ConnectionSettings : ViewModelBase
{
    private string _host = "127.0.0.1";
    private int _port = 7000;
    private SecsConnectionMode _mode = SecsConnectionMode.Active;
    private SecsRole _role = SecsRole.Host;
    private ushort _sessionId;
    private bool _autoReconnect;
    private int _t3Seconds = 45;
    private int _t5Seconds = 10;
    private int _t6Seconds = 10;
    private int _t7Seconds = 10;
    private int _t8Seconds = 10;

    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public int Port { get => _port; set => SetProperty(ref _port, value); }
    public SecsConnectionMode Mode { get => _mode; set => SetProperty(ref _mode, value); }
    public SecsRole Role { get => _role; set => SetProperty(ref _role, value); }
    public ushort SessionId { get => _sessionId; set => SetProperty(ref _sessionId, value); }
    public bool AutoReconnect { get => _autoReconnect; set => SetProperty(ref _autoReconnect, value); }
    public int T3Seconds { get => _t3Seconds; set => SetProperty(ref _t3Seconds, value); }
    public int T5Seconds { get => _t5Seconds; set => SetProperty(ref _t5Seconds, value); }
    public int T6Seconds { get => _t6Seconds; set => SetProperty(ref _t6Seconds, value); }
    public int T7Seconds { get => _t7Seconds; set => SetProperty(ref _t7Seconds, value); }
    public int T8Seconds { get => _t8Seconds; set => SetProperty(ref _t8Seconds, value); }

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

public sealed class InteropScenarioResult : ViewModelBase
{
    private InteropScenarioStatus _status;
    private string _detail = "Not Run";
    private TimeSpan _elapsed;
    public InteropScenarioResult(string id, string name, bool external) { Id = id; Name = name; External = external; }
    public string Id { get; }
    public string Name { get; }
    public bool External { get; }
    public InteropScenarioStatus Status { get => _status; set => SetProperty(ref _status, value); }
    public string Detail { get => _detail; set => SetProperty(ref _detail, value); }
    public TimeSpan Elapsed { get => _elapsed; set => SetProperty(ref _elapsed, value); }
}

public sealed record InteropLogEntry(DateTimeOffset Timestamp, string Level, string Category, string Direction,
    string Summary, string? Message, string? RawHex, string EquipmentId = "", string ConnectionId = "",
    string Endpoint = "", ushort? SessionId = null, uint? CorrelationSystemBytes = null)
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
