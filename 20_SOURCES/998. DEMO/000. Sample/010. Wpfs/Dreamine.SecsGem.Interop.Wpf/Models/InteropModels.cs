using Dreamine.Secs.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Dreamine.SecsGem.Interop.Runtime.Profiles;

namespace Dreamine.SecsGem.Interop.Wpf.Models;

/// <summary>
/// EN: Selects one mutually exclusive, explicitly Demo-only equipment responder in the Workbench.
/// KO: Workbench에서 상호배타적인 Demo 전용 장비 responder 하나를 선택한다.
/// </summary>
public enum EquipmentResponderProfileKind
{
    /// <summary>EN: Small educational S1/S2 switch, not a GEM profile. KO: GEM 프로필이 아닌 작은 학습용 S1/S2 switch.</summary>
    EducationalDemoOnly,
    /// <summary>EN: Public generic E30-derived subset Demo profile. KO: 공개 범용 E30 파생 부분집합 Demo 프로필.</summary>
    E30DerivedSubsetDemo
}

public sealed partial class ConnectionSettings : ViewModelBase
{
    internal HsmsWireObservationOptions? WireObservation { get; set; }

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
    [DreamineProperty]
    private int _reconnectInitialDelaySeconds = 1;
    [DreamineProperty]
    private int _reconnectMaximumDelaySeconds = 30;
    [DreamineProperty]
    private double _reconnectBackoffMultiplier = 2;
    [DreamineProperty]
    private int _maximumFrameLength = 16 * 1024 * 1024;
    [DreamineProperty]
    private int _maximumMessageLength = 16 * 1024 * 1024 - 10;
    [DreamineProperty]
    private int _maximumNestingDepth = 64;
    [DreamineProperty]
    private int _maximumListItemCount = 65_535;
    [DreamineProperty]
    private string _logPolicyId = ConnectionLogPolicyIds.HeaderOnlyV1;

    public HsmsSessionOptions ToOptions() => new()
    {
        Host = Host,
        Port = Port,
        Mode = Mode,
        Role = Role,
        SessionId = new SecsSessionId(SessionId),
        AutoReconnect = AutoReconnect,
        WireObservation = WireObservation,
        MaximumFrameLength = MaximumFrameLength,
        MaximumMessageLength = MaximumMessageLength,
        MaximumNestingDepth = MaximumNestingDepth,
        MaximumListItemCount = MaximumListItemCount,
        Timers = new HsmsTimerOptions
        {
            T3 = TimeSpan.FromSeconds(T3Seconds), T5 = TimeSpan.FromSeconds(T5Seconds),
            T6 = TimeSpan.FromSeconds(T6Seconds), T7 = TimeSpan.FromSeconds(T7Seconds),
            T8 = TimeSpan.FromSeconds(T8Seconds)
        }
    };

    /// <summary>Creates the complete, credential-free Connection Profile v1 represented by the editor.</summary>
    public SingleConnectionProfileV1 ToProfile()
    {
        var profile = new SingleConnectionProfileV1
        {
            Role = Role,
            Mode = Mode,
            Host = Host,
            Port = Port,
            SessionId = SessionId,
            AutoReconnect = AutoReconnect,
            Timers = new ConnectionTimerProfileV1(
                T3Seconds,
                T5Seconds,
                T6Seconds,
                T7Seconds,
                T8Seconds),
            ReconnectPolicy = new OperationalReconnectPolicyV1(
                ReconnectInitialDelaySeconds,
                ReconnectMaximumDelaySeconds,
                ReconnectBackoffMultiplier),
            SafetyLimits = new ConnectionSafetyLimitsV1(
                MaximumFrameLength,
                MaximumMessageLength,
                MaximumNestingDepth,
                MaximumListItemCount),
            LogPolicyId = LogPolicyId
        };
        profile.Validate();
        return profile;
    }

    /// <summary>
    /// Applies a validated Connection Profile v1 to editor fields and reports which changes require
    /// recreation of an already-created session.
    /// </summary>
    public ConnectionProfileApplyDiff ApplyProfile(SingleConnectionProfileV1 profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.Validate();
        var diff = ConnectionProfileApplyDiff.Compare(ToProfile(), profile);

        Role = profile.Role;
        Mode = profile.Mode;
        Host = profile.Host;
        Port = profile.Port;
        SessionId = profile.SessionId;
        AutoReconnect = profile.AutoReconnect;
        T3Seconds = profile.Timers.T3Seconds;
        T5Seconds = profile.Timers.T5Seconds;
        T6Seconds = profile.Timers.T6Seconds;
        T7Seconds = profile.Timers.T7Seconds;
        T8Seconds = profile.Timers.T8Seconds;
        ReconnectInitialDelaySeconds = profile.ReconnectPolicy.InitialDelaySeconds;
        ReconnectMaximumDelaySeconds = profile.ReconnectPolicy.MaximumDelaySeconds;
        ReconnectBackoffMultiplier = profile.ReconnectPolicy.BackoffMultiplier;
        MaximumFrameLength = profile.SafetyLimits.MaximumFrameLength;
        MaximumMessageLength = profile.SafetyLimits.MaximumMessageLength;
        MaximumNestingDepth = profile.SafetyLimits.MaximumNestingDepth;
        MaximumListItemCount = profile.SafetyLimits.MaximumListItemCount;
        LogPolicyId = profile.LogPolicyId;
        return diff;
    }
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
