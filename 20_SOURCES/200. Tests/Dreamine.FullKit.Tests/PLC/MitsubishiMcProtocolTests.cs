using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Mitsubishi.MC.Devices;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>Mitsubishi Mc Protocol Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates mitsubishi mc protocol tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MitsubishiMcProtocolTests
{
    /// <summary>
    /// \if KO
    /// <para>Device Code Mapper Maps Supported Device Types 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the device code mapper maps supported device types operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void DeviceCodeMapper_MapsSupportedDeviceTypes()
    {
        var mapper = new MitsubishiMcDeviceCodeMapper();

        Assert.Equal(MitsubishiMcDeviceCode.D, mapper.Map(PlcDeviceType.D).Value);
        Assert.Equal(MitsubishiMcDeviceCode.M, mapper.Map(PlcDeviceType.M).Value);
        Assert.False(mapper.Map(PlcDeviceType.Unknown).IsSuccess);
    }

    /// <summary>
    /// \if KO
    /// <para>Frame Builder Builds Binary3 E Read Frame 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the frame builder builds binary3 e read frame operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void FrameBuilder_BuildsBinary3EReadFrame()
    {
        var options = new MitsubishiMcConnectionOptions();
        var result = new MitsubishiMcBinary3EFrameBuilder()
            .BuildBatchReadFrame(options, new PlcAddress(PlcDeviceType.D, 100), 2, isBitAccess: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(0x00, result.Value![0]);
        Assert.Equal(0x50, result.Value[1]);
        Assert.Contains((byte)MitsubishiMcDeviceCode.D, result.Value);
    }

    /// <summary>
    /// \if KO
    /// <para>Response Parser Parses Word And Bit Responses 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the response parser parses word and bit responses operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void ResponseParser_ParsesWordAndBitResponses()
    {
        var parser = new MitsubishiMcBinary3EResponseParser();
        var wordFrame = BuildResponse(0x00, 0x00, 0x34, 0x12, 0xFE, 0xFF);
        var bitFrame = BuildResponse(0x00, 0x00, 0x11);

        Assert.Equal(new short[] { 0x1234, -2 }, parser.ParseReadWords(wordFrame, 2).Value);
        Assert.Equal(new[] { true, true }, parser.ParseReadBits(bitFrame, 2).Value);
    }

    /// <summary>
    /// \if KO
    /// <para>Endian Helpers Reject Out Of Range U Int24 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the endian helpers reject out of range u int24 operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void EndianHelpers_RejectOutOfRangeUInt24()
    {
        var bytes = new List<byte>();

        MitsubishiMcEndian.WriteUInt16LittleEndian(bytes, 0x1234);

        Assert.Equal(new byte[] { 0x34, 0x12 }, bytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => MitsubishiMcEndian.WriteUInt24LittleEndian(bytes, -1));
    }

    /// <summary>
    /// \if KO
    /// <para>Response 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the response value.</para>
    /// \endif
    /// </summary>
    /// <param name="data">
    /// \if KO
    /// <para>data에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for data.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Response 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the build response operation.</para>
    /// \endif
    /// </returns>
    private static byte[] BuildResponse(params byte[] data)
    {
        var length = data.Length;
        return new byte[]
        {
            0x00, 0xD0, 0, 0, 0, 0, 0,
            (byte)length, (byte)(length >> 8)
        }.Concat(data).ToArray();
    }
}
