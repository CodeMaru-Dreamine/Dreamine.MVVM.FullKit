using System.Text.Json;
using Dreamine.Secs.Abstractions.Model;
using Dreamine.Secs.Com.Hsms;

namespace Dreamine.SecsGem.Interop.Wpf.Managers;

internal sealed record EquipmentSidecarTelemetry(
    int SchemaVersion,
    long ConnectionEpoch,
    DateTimeOffset Timestamp,
    string Direction,
    string RawStatus,
    byte? Stream,
    byte? Function,
    uint? SystemBytes,
    string RawHex,
    string Result,
    string Timeout,
    SecsMessage? Message);

internal static class EquipmentSidecarTelemetryParser
{
    internal const string Prefix = "DREAMINE_EQUIPMENT_TELEMETRY ";
    private const int MaximumEncodedFrameBytes = 32 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryParse(string line, out EquipmentSidecarTelemetry? telemetry)
    {
        telemetry = null;
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        try
        {
            var wire = JsonSerializer.Deserialize<WireTelemetry>(line[Prefix.Length..], JsonOptions);
            if (wire is null || wire.SchemaVersion != 1 ||
                string.IsNullOrWhiteSpace(wire.Direction) ||
                string.IsNullOrWhiteSpace(wire.RawStatus))
            {
                return false;
            }

            SecsMessage? message = null;
            var rawHex = string.Concat((wire.RawHex ?? string.Empty)
                .Where(character => !char.IsWhiteSpace(character)));
            if (rawHex.Length > 0)
            {
                if ((rawHex.Length & 1) != 0 || rawHex.Length / 2 > MaximumEncodedFrameBytes)
                    return false;
                var frame = Convert.FromHexString(rawHex);
                if (new HsmsFrameCodec().Decode(frame) is not Dreamine.Secs.Abstractions.Hsms.HsmsDataMessage data)
                    return false;
                message = data.SecsMessage;
                if ((wire.Stream is { } stream && message.Stream.Value != stream) ||
                    (wire.Function is { } function && message.Function.Value != function) ||
                    (wire.SystemBytes is { } systemBytes && message.SystemBytes.Value != systemBytes))
                {
                    return false;
                }
            }

            telemetry = new EquipmentSidecarTelemetry(
                wire.SchemaVersion,
                wire.ConnectionEpoch.GetValueOrDefault(),
                wire.Timestamp,
                wire.Direction,
                wire.RawStatus,
                wire.Stream,
                wire.Function,
                wire.SystemBytes,
                rawHex,
                wire.Result ?? string.Empty,
                wire.Timeout ?? string.Empty,
                message);
            return true;
        }
        catch (Exception)
        {
            // Sidecar stdout is an untrusted process boundary. A malformed or
            // unsupported telemetry record must not break the UI output pump.
            return false;
        }
    }

    private sealed record WireTelemetry(
        int SchemaVersion,
        long? ConnectionEpoch,
        DateTimeOffset Timestamp,
        string? Direction,
        string? RawStatus,
        byte? Stream,
        byte? Function,
        uint? SystemBytes,
        string? RawHex,
        string? Result,
        string? Timeout);
}
