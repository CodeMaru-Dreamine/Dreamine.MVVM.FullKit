using Dreamine.Communication.Abstractions.Enums;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;

namespace Dreamine.SecsGem.Interop.Runtime.Logging;

internal enum WireBodyCaptureMode
{
    Excluded,
    HeaderOnly,
    FullBody
}

internal enum WireLogRecordKind
{
    Frame,
    Diagnostic,
    StateTransition
}

internal sealed record WireBodyCaptureRule(
    byte Stream,
    byte Function,
    HsmsWireDirection? Direction,
    WireBodyCaptureMode Mode,
    int MaximumBodyBytes = 0)
{
    internal void Validate()
    {
        if (Stream is 0 or > 127) throw new ArgumentOutOfRangeException(nameof(Stream));
        if (Direction is { } direction && !Enum.IsDefined(direction))
            throw new ArgumentOutOfRangeException(nameof(Direction));
        if (!Enum.IsDefined(Mode)) throw new ArgumentOutOfRangeException(nameof(Mode));
        if (MaximumBodyBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaximumBodyBytes));
        if (Mode == WireBodyCaptureMode.FullBody && MaximumBodyBytes == 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumBodyBytes));
        if (Mode != WireBodyCaptureMode.FullBody && MaximumBodyBytes != 0)
            throw new ArgumentException("Only FullBody rules can retain body bytes.", nameof(MaximumBodyBytes));
    }
}

internal sealed class WireLogPolicy
{
    internal const int HsmsPrefixAndHeaderLength = 14;
    internal const int MaximumAllowedBodyBytes = 16 * 1024 * 1024;
    private readonly IReadOnlyList<WireBodyCaptureRule> _rules;

    internal WireLogPolicy(
        WireBodyCaptureMode defaultMode = WireBodyCaptureMode.HeaderOnly,
        IEnumerable<WireBodyCaptureRule>? rules = null,
        int maximumDecodedCharacters = 16 * 1024)
    {
        if (!Enum.IsDefined(defaultMode)) throw new ArgumentOutOfRangeException(nameof(defaultMode));
        if (defaultMode == WireBodyCaptureMode.FullBody)
            throw new ArgumentException("FullBody must be an explicit S/F rule, not the default.", nameof(defaultMode));
        if (maximumDecodedCharacters is < 128 or > 1_048_576)
            throw new ArgumentOutOfRangeException(nameof(maximumDecodedCharacters));

        var snapshot = (rules ?? []).ToArray();
        foreach (var rule in snapshot)
        {
            rule.Validate();
            if (rule.MaximumBodyBytes > MaximumAllowedBodyBytes)
                throw new ArgumentOutOfRangeException(nameof(rules), $"A body rule cannot exceed {MaximumAllowedBodyBytes} bytes.");
        }
        var duplicate = snapshot.GroupBy(value => (value.Stream, value.Function, value.Direction))
            .FirstOrDefault(value => value.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Duplicate wire-log rule for S{duplicate.Key.Stream}F{duplicate.Key.Function}.", nameof(rules));

        DefaultMode = defaultMode;
        MaximumDecodedCharacters = maximumDecodedCharacters;
        _rules = snapshot;
    }

    internal WireBodyCaptureMode DefaultMode { get; }
    internal int MaximumDecodedCharacters { get; }
    internal IReadOnlyList<WireBodyCaptureRule> Rules => _rules;

    internal WireBodyCaptureDecision Resolve(HsmsWireDirection direction, byte? stream, byte? function)
    {
        if (stream is null || function is null)
            return new(DefaultMode, 0);
        var rule = _rules.FirstOrDefault(value => value.Stream == stream && value.Function == function &&
            (value.Direction is null || value.Direction == direction));
        return rule is null
            ? new(DefaultMode, 0)
            : new(rule.Mode, rule.MaximumBodyBytes);
    }

    internal HsmsWireObservationOptions CreateObservationOptions(int queueCapacity)
    {
        var maximumCapturedBytes = Math.Max(
            HsmsPrefixAndHeaderLength,
            _rules.Where(rule => rule.Mode == WireBodyCaptureMode.FullBody)
                .Select(rule => checked(HsmsPrefixAndHeaderLength + rule.MaximumBodyBytes))
                .DefaultIfEmpty(HsmsPrefixAndHeaderLength)
                .Max());
        var options = new HsmsWireObservationOptions
        {
            QueueCapacity = queueCapacity,
            MaximumCapturedBytes = maximumCapturedBytes,
            DefaultCaptureMode = DefaultMode switch
            {
                WireBodyCaptureMode.Excluded => HsmsWireCaptureMode.Excluded,
                WireBodyCaptureMode.HeaderOnly => HsmsWireCaptureMode.HeaderOnly,
                _ => throw new InvalidOperationException("FullBody cannot be the default wire-log policy.")
            },
            CaptureRules = _rules.Select(rule => new HsmsWireCaptureRule(
                rule.Stream,
                rule.Function,
                rule.Direction,
                rule.Mode switch
                {
                    WireBodyCaptureMode.Excluded => HsmsWireCaptureMode.Excluded,
                    WireBodyCaptureMode.HeaderOnly => HsmsWireCaptureMode.HeaderOnly,
                    WireBodyCaptureMode.FullBody => HsmsWireCaptureMode.FullFrame,
                    _ => throw new InvalidOperationException($"Unsupported wire-log capture mode {rule.Mode}.")
                },
                rule.Mode == WireBodyCaptureMode.FullBody
                    ? checked(HsmsPrefixAndHeaderLength + rule.MaximumBodyBytes)
                    : 0)).ToArray()
        };
        options.Validate();
        return options;
    }
}

internal readonly record struct WireBodyCaptureDecision(WireBodyCaptureMode Mode, int MaximumBodyBytes);

internal sealed record WireLogIdentity(
    string EquipmentId,
    string ConnectionId,
    string Endpoint,
    ushort SessionId)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(EquipmentId)) throw new ArgumentException("Equipment ID is required.", nameof(EquipmentId));
        if (string.IsNullOrWhiteSpace(ConnectionId)) throw new ArgumentException("Connection ID is required.", nameof(ConnectionId));
        if (string.IsNullOrWhiteSpace(Endpoint)) throw new ArgumentException("Endpoint is required.", nameof(Endpoint));
    }
}

internal sealed record WireLogRecord(
    int SchemaVersion,
    long Sequence,
    long ConnectionEpoch,
    DateTimeOffset TimestampUtc,
    HsmsWireDirection? Direction,
    string EquipmentId,
    string ConnectionId,
    string Endpoint,
    ushort ConfiguredSessionId,
    int ActualFrameBytes,
    int DeclaredFrameLength,
    ushort? HeaderSessionId,
    byte? Stream,
    byte? Function,
    bool? ReplyExpected,
    byte? PType,
    byte? SType,
    uint? SystemBytes,
    WireBodyCaptureMode CaptureMode,
    int CapturedBodyBytes,
    bool SourceCaptureTruncated,
    byte[]? HeaderBytes,
    byte[]? BodyBytes,
    string? DecodedItem,
    string? DecodeError,
    string? TransactionStatus = null,
    string? Error = null,
    WireLogRecordKind Kind = WireLogRecordKind.Frame,
    long? SourceSequence = null,
    SecsDiagnosticKind? DiagnosticKind = null,
    string? DiagnosticMessage = null,
    ConnectionState? PreviousConnectionState = null,
    ConnectionState? CurrentConnectionState = null,
    HsmsConnectionState? PreviousHsmsState = null,
    HsmsConnectionState? CurrentHsmsState = null)
{
    internal const int CurrentSchemaVersion = 1;
    internal string CorrelationKey => HeaderSessionId is null || SystemBytes is null
        ? "--"
        : $"{ConnectionId}:{ConnectionEpoch}:{HeaderSessionId.Value}:{SystemBytes.Value:X8}";
}

internal sealed class WireLogRecorderOptions
{
    internal int QueueCapacity { get; init; } = 512;
    internal TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(5);
    internal WireLogPolicy Policy { get; init; } = new();

    internal void Validate()
    {
        if (QueueCapacity is < 1 or > 65_536) throw new ArgumentOutOfRangeException(nameof(QueueCapacity));
        if (ShutdownTimeout < TimeSpan.FromMilliseconds(100) || ShutdownTimeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(ShutdownTimeout));
        ArgumentNullException.ThrowIfNull(Policy);
    }
}

internal sealed record WireLogHealth(
    long SourceDropped,
    long RecorderDropped,
    long Written,
    bool FlushCompleted,
    string? WriterFailure)
{
    internal bool IsEvidenceEligible => SourceDropped == 0 && RecorderDropped == 0 &&
        FlushCompleted && WriterFailure is null;
}

internal sealed record WireLogFilter(
    HsmsWireDirection? Direction = null,
    byte? Stream = null,
    byte? Function = null,
    uint? SystemBytes = null,
    string? ConnectionId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    bool ErrorsOnly = false)
{
    internal void Validate()
    {
        if (FromUtc is { } from && ToUtc is { } to && from > to)
            throw new ArgumentException("The wire-log time range is reversed.");
    }

    internal bool Matches(WireLogRecord record) =>
        (Direction is null || record.Direction == Direction) &&
        (Stream is null || record.Stream == Stream) &&
        (Function is null || record.Function == Function) &&
        (SystemBytes is null || record.SystemBytes == SystemBytes) &&
        (ConnectionId is null || string.Equals(record.ConnectionId, ConnectionId, StringComparison.Ordinal)) &&
        (FromUtc is null || record.TimestampUtc >= FromUtc) &&
        (ToUtc is null || record.TimestampUtc <= ToUtc) &&
        (!ErrorsOnly || record.Error is not null || record.DecodeError is not null);
}
