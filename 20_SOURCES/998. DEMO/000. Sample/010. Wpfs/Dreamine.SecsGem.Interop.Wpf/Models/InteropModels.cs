using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

public sealed class ConnectionSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7000;
    public SecsConnectionMode Mode { get; set; } = SecsConnectionMode.Active;
    public SecsRole Role { get; set; } = SecsRole.Host;
    public ushort SessionId { get; set; }
    public bool AutoReconnect { get; set; }
    public int T3Seconds { get; set; } = 45;
    public int T5Seconds { get; set; } = 10;
    public int T6Seconds { get; set; } = 10;
    public int T7Seconds { get; set; } = 10;
    public int T8Seconds { get; set; } = 10;

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
    string Summary, string? Message, string? RawHex)
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
            const string marker = "SB=";
            var start = Summary.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "--";
            start += marker.Length;
            var end = Summary.IndexOf(' ', start);
            return end < 0 ? Summary[start..] : Summary[start..end];
        }
    }

    public string Result => Level == "Error" ? "Error" : Category == "SECS-II" ? "Transferred" : "Observed";
}

public sealed record SelfTestSummary(DateTimeOffset StartedAt, DateTimeOffset CompletedAt, int Requested,
    int Passed, int Failed, int TimeoutCount, int ReconnectCycles, int LinktestCount,
    double MessagesPerSecond, double MinimumLatencyMs, double AverageLatencyMs, double MaximumLatencyMs,
    double P95LatencyMs, string? FirstFailure, string Result, IReadOnlyList<string> Checks);

public enum SecsItemEditorFormat
{
    List, Binary, Boolean, Ascii, Int8, Int16, Int32, Int64, UInt8, UInt16, UInt32, UInt64, Float32, Float64
}
