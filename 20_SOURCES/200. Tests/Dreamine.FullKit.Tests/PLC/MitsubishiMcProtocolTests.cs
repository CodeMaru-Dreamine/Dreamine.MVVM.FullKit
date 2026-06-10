using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Mitsubishi.MC.Devices;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Protocol;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class MitsubishiMcProtocolTests
{
    [Fact]
    public void DeviceCodeMapper_MapsSupportedDeviceTypes()
    {
        var mapper = new MitsubishiMcDeviceCodeMapper();

        Assert.Equal(MitsubishiMcDeviceCode.D, mapper.Map(PlcDeviceType.D).Value);
        Assert.Equal(MitsubishiMcDeviceCode.M, mapper.Map(PlcDeviceType.M).Value);
        Assert.False(mapper.Map(PlcDeviceType.Unknown).IsSuccess);
    }

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

    [Fact]
    public void ResponseParser_ParsesWordAndBitResponses()
    {
        var parser = new MitsubishiMcBinary3EResponseParser();
        var wordFrame = BuildResponse(0x00, 0x00, 0x34, 0x12, 0xFE, 0xFF);
        var bitFrame = BuildResponse(0x00, 0x00, 0x11);

        Assert.Equal(new short[] { 0x1234, -2 }, parser.ParseReadWords(wordFrame, 2).Value);
        Assert.Equal(new[] { true, true }, parser.ParseReadBits(bitFrame, 2).Value);
    }

    [Fact]
    public void EndianHelpers_RejectOutOfRangeUInt24()
    {
        var bytes = new List<byte>();

        MitsubishiMcEndian.WriteUInt16LittleEndian(bytes, 0x1234);

        Assert.Equal(new byte[] { 0x34, 0x12 }, bytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => MitsubishiMcEndian.WriteUInt24LittleEndian(bytes, -1));
    }

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
