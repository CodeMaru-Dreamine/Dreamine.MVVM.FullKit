using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Dreamine.Secs.Abstractions.Diagnostics;
using Dreamine.Secs.Abstractions.Hsms;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.SecsGem.Interop.Runtime.Logging;

internal interface IWireBodyDecoder
{
    string? Decode(ReadOnlyMemory<byte> completeFrame, int maximumCharacters);
}

internal sealed class DefaultWireBodyDecoder : IWireBodyDecoder
{
    private readonly HsmsFrameCodec _codec = new();

    public string? Decode(ReadOnlyMemory<byte> completeFrame, int maximumCharacters)
    {
        var message = _codec.Decode(completeFrame);
        return message is HsmsDataMessage data
            ? BoundedSecsItemFormatter.Format(data.SecsMessage.Item, maximumCharacters)
            : null;
    }
}

internal sealed class WireLogRecordFactory(WireLogPolicy policy, IWireBodyDecoder? decoder = null)
{
    private readonly WireLogPolicy _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    private readonly IWireBodyDecoder _decoder = decoder ?? new DefaultWireBodyDecoder();

    internal WireLogRecord Create(HsmsWireObservation observation, WireLogIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(identity);
        identity.Validate();

        var captured = observation.CapturedBytes;
        var header = observation.Header is { } typedHeader
            ? FromHsmsHeader(typedHeader)
            : TryReadHeader(captured.Span);
        var decision = _policy.Resolve(observation.Direction, header?.Stream, header?.Function);
        byte[]? headerBytes = null;
        byte[]? bodyBytes = null;
        string? decoded = null;
        string? decodeError = null;

        if (decision.Mode != WireBodyCaptureMode.Excluded)
        {
            var headerLength = Math.Min(captured.Length, WireLogPolicy.HsmsPrefixAndHeaderLength);
            headerBytes = captured[..headerLength].ToArray();
        }

        if (decision.Mode == WireBodyCaptureMode.FullBody)
        {
            var availableBodyBytes = Math.Max(0, captured.Length - WireLogPolicy.HsmsPrefixAndHeaderLength);
            var bodyLength = Math.Min(availableBodyBytes, decision.MaximumBodyBytes);
            if (bodyLength > 0)
                bodyBytes = captured.Slice(WireLogPolicy.HsmsPrefixAndHeaderLength, bodyLength).ToArray();

            if (availableBodyBytes > decision.MaximumBodyBytes)
            {
                decodeError = "The frame body exceeds the configured FullBody limit, so decoding was skipped.";
            }
            else if (!observation.IsCaptureTruncated && captured.Length == observation.ActualByteCount)
            {
                try { decoded = _decoder.Decode(captured, _policy.MaximumDecodedCharacters); }
                catch (Exception exception) { decodeError = exception.Message; }
            }
            else
            {
                decodeError = "The complete frame was not retained, so body decoding was skipped.";
            }
        }
        else if (decision.Mode == WireBodyCaptureMode.HeaderOnly && header is null)
        {
            decodeError = "The retained wire snapshot does not contain a complete HSMS header.";
        }

        return new(
            WireLogRecord.CurrentSchemaVersion,
            observation.SequenceNumber,
            observation.ConnectionEpoch,
            observation.ObservedAtUtc,
            observation.Direction,
            identity.EquipmentId,
            identity.ConnectionId,
            identity.Endpoint,
            identity.SessionId,
            observation.ActualByteCount,
            observation.DeclaredFrameLength,
            header?.SessionId,
            header?.Stream,
            header?.Function,
            header?.ReplyExpected,
            header?.PType,
            header?.SType,
            header?.SystemBytes,
            decision.Mode,
            bodyBytes?.Length ?? 0,
            observation.IsCaptureTruncated,
            headerBytes,
            bodyBytes,
            decoded,
            decodeError);
    }

    internal WireLogRecord CreateDiagnostic(
        SecsDiagnosticEvent diagnostic,
        WireLogIdentity identity,
        long connectionEpoch,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        ArgumentNullException.ThrowIfNull(identity);
        identity.Validate();
        var header = diagnostic.HsmsHeader is { } typedHeader ? FromHsmsHeader(typedHeader) : null;
        var isError = diagnostic.Kind is SecsDiagnosticKind.Timeout or
            SecsDiagnosticKind.ProtocolError or SecsDiagnosticKind.ApplicationError;
        var safeMessage = $"{diagnostic.Kind} diagnostic; dynamic detail withheld.";
        return new WireLogRecord(
            WireLogRecord.CurrentSchemaVersion,
            0,
            connectionEpoch,
            timestampUtc.ToUniversalTime(),
            null,
            identity.EquipmentId,
            identity.ConnectionId,
            identity.Endpoint,
            identity.SessionId,
            diagnostic.FrameLength ?? 0,
            0,
            header?.SessionId,
            header?.Stream,
            header?.Function,
            header?.ReplyExpected,
            header?.PType,
            header?.SType,
            header?.SystemBytes,
            WireBodyCaptureMode.Excluded,
            0,
            false,
            null,
            null,
            null,
            null,
            TransactionStatus: diagnostic.Kind is SecsDiagnosticKind.PrimarySent or SecsDiagnosticKind.SecondaryReceived
                ? diagnostic.Kind.ToString()
                : null,
            Error: isError ? safeMessage : null,
            Kind: WireLogRecordKind.Diagnostic,
            DiagnosticKind: diagnostic.Kind,
            DiagnosticMessage: safeMessage,
            CurrentHsmsState: diagnostic.State);
    }

    internal WireLogRecord CreateState(
        SecsSessionStateChangedEventArgs transition,
        WireLogIdentity identity,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(transition);
        ArgumentNullException.ThrowIfNull(identity);
        identity.Validate();
        return new WireLogRecord(
            WireLogRecord.CurrentSchemaVersion,
            0,
            transition.ConnectionIdentity.ConnectionEpoch,
            timestampUtc.ToUniversalTime(),
            null,
            identity.EquipmentId,
            identity.ConnectionId,
            identity.Endpoint,
            identity.SessionId,
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            WireBodyCaptureMode.Excluded,
            0,
            false,
            null,
            null,
            null,
            null,
            Kind: WireLogRecordKind.StateTransition,
            PreviousConnectionState: transition.PreviousConnectionState,
            CurrentConnectionState: transition.CurrentConnectionState,
            PreviousHsmsState: transition.PreviousHsmsState,
            CurrentHsmsState: transition.CurrentHsmsState);
    }

    private static ParsedHeader? TryReadHeader(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < WireLogPolicy.HsmsPrefixAndHeaderLength) return null;
        var streamByte = frame[6];
        var isData = frame[9] == (byte)HsmsSType.Data;
        return new(
            BinaryPrimitives.ReadUInt16BigEndian(frame[4..6]),
            isData ? (byte)(streamByte & 0x7F) : null,
            isData ? frame[7] : null,
            isData ? (streamByte & 0x80) != 0 : null,
            frame[8],
            frame[9],
            BinaryPrimitives.ReadUInt32BigEndian(frame[10..14]));
    }

    private static ParsedHeader FromHsmsHeader(HsmsHeader header) => new(
        header.SessionId,
        header.IsData ? header.Stream : null,
        header.IsData ? header.Function : null,
        header.IsData ? header.ReplyExpected : null,
        header.PType,
        header.SType,
        header.SystemBytes.Value);

    private sealed record ParsedHeader(
        ushort SessionId,
        byte? Stream,
        byte? Function,
        bool? ReplyExpected,
        byte PType,
        byte SType,
        uint SystemBytes);
}

internal static class BoundedSecsItemFormatter
{
    internal static string Format(SecsItem? item, int maximumCharacters)
    {
        if (maximumCharacters < 16) throw new ArgumentOutOfRangeException(nameof(maximumCharacters));
        var builder = new StringBuilder(Math.Min(maximumCharacters, 4096));
        Append(builder, item, maximumCharacters, 0);
        if (builder.Length >= maximumCharacters)
        {
            builder.Length = Math.Max(0, maximumCharacters - 1);
            builder.Append('…');
        }
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, SecsItem? item, int limit, int depth)
    {
        if (builder.Length >= limit) return;
        switch (item)
        {
            case null:
                AppendText(builder, "<none>", limit);
                return;
            case SecsListItem list:
                AppendText(builder, $"<L[{list.Count}]", limit);
                foreach (var child in list.Items)
                {
                    AppendText(builder, "\n", limit);
                    AppendText(builder, new string(' ', Math.Min(depth + 1, 32) * 2), limit);
                    Append(builder, child, limit, depth + 1);
                    if (builder.Length >= limit) break;
                }
                AppendText(builder, ">", limit);
                return;
            case SecsAsciiItem ascii:
                AppendText(builder, $"<A[{ascii.Value.Length}] \"", limit);
                AppendText(builder, ascii.Value, limit);
                AppendText(builder, "\">", limit);
                return;
            case SecsBinaryItem binary:
                AppendValues(builder, "B", binary.Values.Span, limit, static value => value.ToString("X2", CultureInfo.InvariantCulture));
                return;
            case SecsJis8Item jis:
                AppendValues(builder, "JIS8", jis.Values.Span, limit, static value => value.ToString("X2", CultureInfo.InvariantCulture));
                return;
            case SecsBooleanItem value:
                AppendValues(builder, "BOOLEAN", value.Values.Span, limit, static item => item ? "true" : "false");
                return;
            case SecsInt8Item value:
                AppendValues(builder, "I1", value.Values.Span, limit, Invariant);
                return;
            case SecsInt16Item value:
                AppendValues(builder, "I2", value.Values.Span, limit, Invariant);
                return;
            case SecsInt32Item value:
                AppendValues(builder, "I4", value.Values.Span, limit, Invariant);
                return;
            case SecsInt64Item value:
                AppendValues(builder, "I8", value.Values.Span, limit, Invariant);
                return;
            case SecsUInt8Item value:
                AppendValues(builder, "U1", value.Values.Span, limit, Invariant);
                return;
            case SecsUInt16Item value:
                AppendValues(builder, "U2", value.Values.Span, limit, Invariant);
                return;
            case SecsUInt32Item value:
                AppendValues(builder, "U4", value.Values.Span, limit, Invariant);
                return;
            case SecsUInt64Item value:
                AppendValues(builder, "U8", value.Values.Span, limit, Invariant);
                return;
            case SecsFloat32Item value:
                AppendValues(builder, "F4", value.Values.Span, limit, Invariant);
                return;
            case SecsFloat64Item value:
                AppendValues(builder, "F8", value.Values.Span, limit, Invariant);
                return;
            default:
                AppendText(builder, $"<{item.Format}>", limit);
                return;
        }
    }

    private static void AppendValues<T>(
        StringBuilder builder,
        string format,
        ReadOnlySpan<T> values,
        int limit,
        Func<T, string> convert)
    {
        AppendText(builder, $"<{format}[{values.Length}]", limit);
        foreach (var value in values)
        {
            AppendText(builder, " ", limit);
            AppendText(builder, convert(value), limit);
            if (builder.Length >= limit) break;
        }
        AppendText(builder, ">", limit);
    }

    private static string Invariant<T>(T value) where T : IFormattable =>
        value.ToString(null, CultureInfo.InvariantCulture);

    private static void AppendText(StringBuilder builder, string text, int limit)
    {
        var remaining = limit - builder.Length;
        if (remaining <= 0) return;
        builder.Append(text.AsSpan(0, Math.Min(text.Length, remaining)));
    }
}
