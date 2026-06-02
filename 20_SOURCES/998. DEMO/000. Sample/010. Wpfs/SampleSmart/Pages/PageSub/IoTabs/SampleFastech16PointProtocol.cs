using System.Text;
using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Fastech.Ethernet.Protocol;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// Provides a tiny sample protocol for the 16-in/16-out Fastech Ethernet I/O demo.
/// </summary>
/// <remarks>
/// This is not a real Fastech device protocol. It exercises the Dreamine Fastech Ethernet adapter pipeline
/// without redistributing vendor SDK files or guessing hardware command frames.
/// </remarks>
public sealed class SampleFastech16PointProtocol : IFastechEthernetIoProtocol
{
    private const string ReadDigitalInputsCommand = "RDI";
    private const string ReadDigitalOutputsCommand = "RDO";
    private const string WriteDigitalOutputsCommand = "WDO";
    private const string AckResponse = "OK";

    /// <inheritdoc />
    public byte[] BuildReadDigitalInputs(IReadOnlyList<IoPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return Encode($"{ReadDigitalInputsCommand} {points.Count}");
    }

    /// <inheritdoc />
    public IoResult<bool[]> ParseDigitalInputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return ParseBits(responseFrame, "DI", count);
    }

    /// <inheritdoc />
    public byte[] BuildReadDigitalOutputs(IReadOnlyList<IoPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return Encode($"{ReadDigitalOutputsCommand} {points.Count}");
    }

    /// <inheritdoc />
    public IoResult<bool[]> ParseDigitalOutputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return ParseBits(responseFrame, "DO", count);
    }

    /// <inheritdoc />
    public byte[] BuildWriteDigitalOutputs(IReadOnlyDictionary<IoPoint, bool> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var orderedValues = values
            .OrderBy(x => x.Key.Module)
            .ThenBy(x => x.Key.Channel)
            .Select(x => x.Value ? '1' : '0');

        return Encode($"{WriteDigitalOutputsCommand} {new string(orderedValues.ToArray())}");
    }

    /// <inheritdoc />
    public byte[] BuildReadAnalogInputs(IReadOnlyList<AnalogIoPoint> points)
    {
        throw new NotSupportedException("The 16/16 sample covers digital I/O only.");
    }

    /// <inheritdoc />
    public IoResult<double[]> ParseAnalogInputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return IoResult<double[]>.Failure("The 16/16 sample covers digital I/O only.");
    }

    /// <inheritdoc />
    public byte[] BuildReadAnalogOutputs(IReadOnlyList<AnalogIoPoint> points)
    {
        throw new NotSupportedException("The 16/16 sample covers digital I/O only.");
    }

    /// <inheritdoc />
    public IoResult<double[]> ParseAnalogOutputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return IoResult<double[]>.Failure("The 16/16 sample covers digital I/O only.");
    }

    /// <inheritdoc />
    public byte[] BuildWriteAnalogOutputs(IReadOnlyDictionary<AnalogIoPoint, double> values)
    {
        throw new NotSupportedException("The 16/16 sample covers digital I/O only.");
    }

    /// <inheritdoc />
    public IoResult ParseWriteResponse(IReadOnlyList<byte> responseFrame)
    {
        var text = Decode(responseFrame);
        return text.Equals(AckResponse, StringComparison.OrdinalIgnoreCase)
            ? IoResult.Success()
            : IoResult.Failure($"Unexpected sample write response: {text}");
    }

    private static IoResult<bool[]> ParseBits(IReadOnlyList<byte> responseFrame, string prefix, int count)
    {
        var text = Decode(responseFrame);
        var expectedPrefix = $"{prefix} ";

        if (!text.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return IoResult<bool[]>.Failure($"Unexpected sample response: {text}");
        }

        var bitText = text[expectedPrefix.Length..].Trim();
        if (bitText.Length < count)
        {
            return IoResult<bool[]>.Failure($"The sample response contains {bitText.Length} bits, but {count} were expected.");
        }

        var values = bitText
            .Take(count)
            .Select(x => x == '1')
            .ToArray();

        return IoResult<bool[]>.Success(values);
    }

    private static byte[] Encode(string text)
    {
        return Encoding.ASCII.GetBytes(text);
    }

    private static string Decode(IReadOnlyList<byte> bytes)
    {
        var buffer = bytes as byte[] ?? bytes.ToArray();
        return Encoding.ASCII.GetString(buffer).Trim();
    }
}
