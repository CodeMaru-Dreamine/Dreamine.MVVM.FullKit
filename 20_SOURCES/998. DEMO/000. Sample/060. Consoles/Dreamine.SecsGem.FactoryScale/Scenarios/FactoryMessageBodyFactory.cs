using Dreamine.Secs.Abstractions.Model;

namespace Dreamine.SecsGem.FactoryScale.Scenarios;

/// <summary>
/// Builds the scenario metadata and, when requested, an exact-size Binary
/// payload. The HSMS length limit includes the ten-byte header and the encoded
/// SECS-II item headers; the four-byte transport length prefix is not part of
/// the declared HSMS frame length.
/// </summary>
internal static class FactoryMessageBodyFactory
{
    internal const int MaximumHsmsFrameLength = 16 * 1024 * 1024;
    internal const int HsmsHeaderLength = 10;
    internal const int MaximumSecsItemBodyLength = 0x00ff_ffff;
    private static readonly Lazy<int> NormalMaximumPayload = new(() => CalculateMaximumPayloadBytes("normal"));
    private static readonly Lazy<int> BusyMaximumPayload = new(() => CalculateMaximumPayloadBytes("busy"));
    private static readonly Lazy<int> TraceMaximumPayload = new(() => CalculateMaximumPayloadBytes("trace"));
    private static readonly Lazy<int> SoakMaximumPayload = new(() => CalculateMaximumPayloadBytes("soak"));
    private static readonly Lazy<int> LargeMessageMaximumPayload = new(() => CalculateMaximumPayloadBytes("large-message"));

    internal static int MaximumPayloadBytesForScenario(string scenario) =>
        NormalizeScenario(scenario) switch
        {
            "factory-normal" => MaximumPayloadBytes("normal"),
            "factory-busy" => MaximumPayloadBytes("busy"),
            "trace-burst" => MaximumPayloadBytes("trace"),
            "soak" => MaximumPayloadBytes("soak"),
            "large-message" => MaximumPayloadBytes("large-message"),
            _ => MaximumPayloadBytes("large-message")
        };

    internal static int MaximumPayloadBytes(string profile) => profile.ToLowerInvariant() switch
    {
        "normal" => NormalMaximumPayload.Value,
        "busy" => BusyMaximumPayload.Value,
        "trace" => TraceMaximumPayload.Value,
        "soak" => SoakMaximumPayload.Value,
        "large-message" => LargeMessageMaximumPayload.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown message profile.")
    };

    private static int CalculateMaximumPayloadBytes(string profile)
    {
        var available = MaximumHsmsFrameLength - HsmsHeaderLength - FixedPayloadEnvelopeLength(profile);
        var candidate = Math.Min(MaximumSecsItemBodyLength, available - ItemHeaderLength(0));
        while (candidate >= 0 && ItemHeaderLength(candidate) + candidate > available) candidate--;
        if (candidate < 0) throw new InvalidOperationException("The profile metadata exceeds the HSMS frame limit.");
        return candidate;
    }

    internal static int GetMaximumHsmsFrameLength(string profile, int payloadBytes)
    {
        if (payloadBytes < 0 || payloadBytes > MaximumSecsItemBodyLength)
            throw new ArgumentOutOfRangeException(nameof(payloadBytes));
        if (payloadBytes == 0 && !profile.Equals("large-message", StringComparison.OrdinalIgnoreCase))
            return checked(HsmsHeaderLength + MaximumMetadataLength(profile));
        return checked(HsmsHeaderLength + FixedPayloadEnvelopeLength(profile) +
                       ItemHeaderLength(payloadBytes) + payloadBytes);
    }

    internal static SecsItem Create(string profile, long sequence, int payloadBytes)
    {
        var maximum = MaximumPayloadBytes(profile);
        if (payloadBytes < 0 || payloadBytes > maximum)
            throw new ArgumentOutOfRangeException(nameof(payloadBytes), payloadBytes,
                $"Payload must be between 0 and {maximum:N0} bytes for profile '{profile}'.");

        if (profile.Equals("large-message", StringComparison.OrdinalIgnoreCase))
            return new SecsBinaryItem(CreatePayload(payloadBytes));

        var metadata = CreateMetadata(profile, sequence);
        return payloadBytes == 0
            ? metadata
            : new SecsListItem(metadata, new SecsBinaryItem(CreatePayload(payloadBytes)));
    }

    internal static int GetEncodedItemLength(SecsItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var declaredLength = item is SecsListItem list ? list.Count : item.BodyLength;
        return checked(ItemHeaderLength(declaredLength) + item.BodyLength);
    }

    private static SecsItem CreateMetadata(string profile, long sequence) => profile.ToLowerInvariant() switch
    {
        "trace" => new SecsListItem(
            new SecsInt32Item(Enumerable.Range(0, 64)
                .Select(value => unchecked((int)(sequence + value))).ToArray()),
            new SecsListItem(new SecsFloat64Item(sequence, sequence + 0.25, sequence + 0.5),
                new SecsAsciiItem("TRACE"))),
        "busy" => new SecsListItem(new SecsInt64Item(sequence), new SecsAsciiItem("EVENT"),
            new SecsBinaryItem((byte)(sequence & 0xFF), (byte)((sequence >> 8) & 0xFF))),
        "normal" when sequence % 3 == 0 =>
            new SecsListItem(new SecsAsciiItem("STATUS"), new SecsInt64Item(sequence)),
        "normal" when sequence % 3 == 1 =>
            new SecsListItem(new SecsAsciiItem("EVENT"), new SecsUInt32Item(unchecked((uint)sequence))),
        "normal" => new SecsListItem(new SecsAsciiItem("ALARM"),
            new SecsBinaryItem((byte)(sequence & 0xFF))),
        "soak" => new SecsAsciiItem("SOAK"),
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown message profile.")
    };

    private static int MaximumMetadataLength(string profile) => profile.Equals("normal", StringComparison.OrdinalIgnoreCase)
        ? Enumerable.Range(0, 3).Max(sequence => GetEncodedItemLength(CreateMetadata(profile, sequence)))
        : GetEncodedItemLength(CreateMetadata(profile, 0));

    private static int FixedPayloadEnvelopeLength(string profile) =>
        profile.Equals("large-message", StringComparison.OrdinalIgnoreCase)
            ? 0
            : checked(ItemHeaderLength(2) + MaximumMetadataLength(profile));

    private static byte[] CreatePayload(int length)
    {
        var payload = new byte[length];
        for (var index = 0; index < payload.Length; index += 4096)
            payload[index] = unchecked((byte)(index / 4096));
        return payload;
    }

    private static int ItemHeaderLength(int declaredLength)
    {
        if (declaredLength < 0 || declaredLength > MaximumSecsItemBodyLength)
            throw new ArgumentOutOfRangeException(nameof(declaredLength));
        return 1 + (declaredLength <= byte.MaxValue ? 1 : declaredLength <= ushort.MaxValue ? 2 : 3);
    }

    private static string NormalizeScenario(string scenario) => scenario.ToLowerInvariant() switch
    {
        "normal-factory" => "factory-normal",
        "busy-factory" => "factory-busy",
        _ => scenario.ToLowerInvariant()
    };
}
