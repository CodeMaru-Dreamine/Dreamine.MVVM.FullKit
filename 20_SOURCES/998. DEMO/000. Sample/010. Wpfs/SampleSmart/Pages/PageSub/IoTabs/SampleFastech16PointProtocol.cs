using System.Text;
using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Fastech.Ethernet.Protocol;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// \if KO
/// <para>Sample Fastech16 Point Protocol 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides a tiny sample protocol for the 16-in/16-out Fastech Ethernet I/O demo.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>이 멤버의 동작과 사용 시 고려 사항을 설명합니다.</para>
/// \endif
/// \if EN
/// <para>This is not a real Fastech device protocol. It exercises the Dreamine Fastech Ethernet adapter pipeline without redistributing vendor SDK files or guessing hardware command frames.</para>
/// \endif
/// </remarks>
public sealed class SampleFastech16PointProtocol : IFastechEthernetIoProtocol
{
    /// <summary>
    /// \if KO
    /// <para>Read Digital Inputs Command 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the read digital inputs command value.</para>
    /// \endif
    /// </summary>
    private const string ReadDigitalInputsCommand = "RDI";
    /// <summary>
    /// \if KO
    /// <para>Read Digital Outputs Command 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the read digital outputs command value.</para>
    /// \endif
    /// </summary>
    private const string ReadDigitalOutputsCommand = "RDO";
    /// <summary>
    /// \if KO
    /// <para>Write Digital Outputs Command 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the write digital outputs command value.</para>
    /// \endif
    /// </summary>
    private const string WriteDigitalOutputsCommand = "WDO";
    /// <summary>
    /// \if KO
    /// <para>Ack Response 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the ack response value.</para>
    /// \endif
    /// </summary>
    private const string AckResponse = "OK";

    /// <summary>
    /// \if KO
    /// <para>Read Digital Inputs 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the read digital inputs value.</para>
    /// \endif
    /// </summary>
    /// <param name="points">
    /// \if KO
    /// <para>points에 사용할 <c>IReadOnlyList&lt;IoPoint&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;IoPoint&gt;</c> value used for points.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Read Digital Inputs 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build read digital inputs operation.</para>
    /// \endif
    /// </returns>
    public byte[] BuildReadDigitalInputs(IReadOnlyList<IoPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return Encode($"{ReadDigitalInputsCommand} {points.Count}");
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Digital Inputs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse digital inputs operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>response Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for response frame.</para>
    /// \endif
    /// </param>
    /// <param name="count">
    /// \if KO
    /// <para>count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for count.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Digital Inputs 작업에서 생성한 <c>IoResult&lt;bool[]&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoResult&lt;bool[]&gt;</c> result produced by the parse digital inputs operation.</para>
    /// \endif
    /// </returns>
    public IoResult<bool[]> ParseDigitalInputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return ParseBits(responseFrame, "DI", count);
    }

    /// <summary>
    /// \if KO
    /// <para>Read Digital Outputs 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the read digital outputs value.</para>
    /// \endif
    /// </summary>
    /// <param name="points">
    /// \if KO
    /// <para>points에 사용할 <c>IReadOnlyList&lt;IoPoint&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;IoPoint&gt;</c> value used for points.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Read Digital Outputs 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build read digital outputs operation.</para>
    /// \endif
    /// </returns>
    public byte[] BuildReadDigitalOutputs(IReadOnlyList<IoPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        return Encode($"{ReadDigitalOutputsCommand} {points.Count}");
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Digital Outputs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse digital outputs operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>response Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for response frame.</para>
    /// \endif
    /// </param>
    /// <param name="count">
    /// \if KO
    /// <para>count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for count.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Digital Outputs 작업에서 생성한 <c>IoResult&lt;bool[]&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoResult&lt;bool[]&gt;</c> result produced by the parse digital outputs operation.</para>
    /// \endif
    /// </returns>
    public IoResult<bool[]> ParseDigitalOutputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return ParseBits(responseFrame, "DO", count);
    }

    /// <summary>
    /// \if KO
    /// <para>Write Digital Outputs 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the write digital outputs value.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>IReadOnlyDictionary&lt;IoPoint, bool&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyDictionary&lt;IoPoint, bool&gt;</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Write Digital Outputs 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build write digital outputs operation.</para>
    /// \endif
    /// </returns>
    public byte[] BuildWriteDigitalOutputs(IReadOnlyDictionary<IoPoint, bool> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var orderedValues = values
            .OrderBy(x => x.Key.Module)
            .ThenBy(x => x.Key.Channel)
            .Select(x => x.Value ? '1' : '0');

        return Encode($"{WriteDigitalOutputsCommand} {new string(orderedValues.ToArray())}");
    }

    /// <summary>
    /// \if KO
    /// <para>Read Analog Inputs 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the read analog inputs value.</para>
    /// \endif
    /// </summary>
    /// <param name="points">
    /// \if KO
    /// <para>points에 사용할 <c>IReadOnlyList&lt;AnalogIoPoint&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;AnalogIoPoint&gt;</c> value used for points.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Read Analog Inputs 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build read analog inputs operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// \if KO
    /// <para>요청한 작업이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the requested operation is not supported.</para>
    /// \endif
    /// </exception>
    public byte[] BuildReadAnalogInputs(IReadOnlyList<AnalogIoPoint> points)
    {
        throw new NotSupportedException("The 16/16 sample covers digital I/O only.");
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Analog Inputs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse analog inputs operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>response Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for response frame.</para>
    /// \endif
    /// </param>
    /// <param name="count">
    /// \if KO
    /// <para>count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for count.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Analog Inputs 작업에서 생성한 <c>IoResult&lt;double[]&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoResult&lt;double[]&gt;</c> result produced by the parse analog inputs operation.</para>
    /// \endif
    /// </returns>
    public IoResult<double[]> ParseAnalogInputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return IoResult<double[]>.Failure("The 16/16 sample covers digital I/O only.");
    }

    /// <summary>
    /// \if KO
    /// <para>Read Analog Outputs 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the read analog outputs value.</para>
    /// \endif
    /// </summary>
    /// <param name="points">
    /// \if KO
    /// <para>points에 사용할 <c>IReadOnlyList&lt;AnalogIoPoint&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;AnalogIoPoint&gt;</c> value used for points.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Read Analog Outputs 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build read analog outputs operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// \if KO
    /// <para>요청한 작업이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the requested operation is not supported.</para>
    /// \endif
    /// </exception>
    public byte[] BuildReadAnalogOutputs(IReadOnlyList<AnalogIoPoint> points)
    {
        throw new NotSupportedException("The 16/16 sample covers digital I/O only.");
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Analog Outputs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse analog outputs operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>response Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for response frame.</para>
    /// \endif
    /// </param>
    /// <param name="count">
    /// \if KO
    /// <para>count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for count.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Analog Outputs 작업에서 생성한 <c>IoResult&lt;double[]&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoResult&lt;double[]&gt;</c> result produced by the parse analog outputs operation.</para>
    /// \endif
    /// </returns>
    public IoResult<double[]> ParseAnalogOutputs(IReadOnlyList<byte> responseFrame, int count)
    {
        return IoResult<double[]>.Failure("The 16/16 sample covers digital I/O only.");
    }

    /// <summary>
    /// \if KO
    /// <para>Write Analog Outputs 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the write analog outputs value.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>IReadOnlyDictionary&lt;AnalogIoPoint, double&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyDictionary&lt;AnalogIoPoint, double&gt;</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Write Analog Outputs 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build write analog outputs operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// \if KO
    /// <para>요청한 작업이 지원되지 않는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the requested operation is not supported.</para>
    /// \endif
    /// </exception>
    public byte[] BuildWriteAnalogOutputs(IReadOnlyDictionary<AnalogIoPoint, double> values)
    {
        throw new NotSupportedException("The 16/16 sample covers digital I/O only.");
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Write Response 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse write response operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>response Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for response frame.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Write Response 작업에서 생성한 <c>IoResult</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoResult</c> result produced by the parse write response operation.</para>
    /// \endif
    /// </returns>
    public IoResult ParseWriteResponse(IReadOnlyList<byte> responseFrame)
    {
        var text = Decode(responseFrame);
        return text.Equals(AckResponse, StringComparison.OrdinalIgnoreCase)
            ? IoResult.Success()
            : IoResult.Failure($"Unexpected sample write response: {text}");
    }

    /// <summary>
    /// \if KO
    /// <para>Parse Bits 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse bits operation.</para>
    /// \endif
    /// </summary>
    /// <param name="responseFrame">
    /// \if KO
    /// <para>response Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for response frame.</para>
    /// \endif
    /// </param>
    /// <param name="prefix">
    /// \if KO
    /// <para>prefix에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for prefix.</para>
    /// \endif
    /// </param>
    /// <param name="count">
    /// \if KO
    /// <para>count에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for count.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Bits 작업에서 생성한 <c>IoResult&lt;bool[]&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IoResult&lt;bool[]&gt;</c> result produced by the parse bits operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Encode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the encode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Encode 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the encode operation.</para>
    /// \endif
    /// </returns>
    private static byte[] Encode(string text)
    {
        return Encoding.ASCII.GetBytes(text);
    }

    /// <summary>
    /// \if KO
    /// <para>Decode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the decode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="bytes">
    /// \if KO
    /// <para>bytes에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for bytes.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Decode 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the decode operation.</para>
    /// \endif
    /// </returns>
    private static string Decode(IReadOnlyList<byte> bytes)
    {
        var buffer = bytes as byte[] ?? bytes.ToArray();
        return Encoding.ASCII.GetString(buffer).Trim();
    }
}
