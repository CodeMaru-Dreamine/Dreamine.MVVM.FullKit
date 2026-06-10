using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Omron.Fins.Devices;
using Dreamine.PLC.Omron.Fins.Options;
using Dreamine.PLC.Omron.Fins.Protocol;

namespace Dreamine.FullKit.Tests.PLC;

public sealed class OmronFinsProtocolTests
{
    [Fact]
    public void MemoryAreaMapper_MapsDeviceTypesAndAreaKinds()
    {
        var dmWord = OmronFinsMemoryAreaMapper.Map(new PlcAddress(PlcDeviceType.D, 0), bitAccess: false);
        var dmBit = OmronFinsMemoryAreaMapper.Map(new PlcAddress(PlcDeviceType.D, 0), bitAccess: true);

        Assert.Equal(OmronFinsMemoryAreaCode.DmWord, dmWord);
        Assert.Equal(OmronFinsMemoryAreaCode.DmBit, dmBit);
        Assert.Equal(PlcDeviceType.D, OmronFinsMemoryAreaMapper.ToDeviceType(dmWord));
        Assert.True(OmronFinsMemoryAreaMapper.IsBitArea(dmBit));
    }

    [Fact]
    public void FrameBuilder_BuildsMemoryReadFrame()
    {
        var options = new OmronFinsConnectionOptions { DestinationNode = 2, SourceNode = 1 };
        var frame = new OmronFinsFrameBuilder()
            .BuildMemoryAreaRead(options, new PlcAddress(PlcDeviceType.D, 100), 2, bitAccess: false);

        Assert.Equal(0x80, frame[0]);
        Assert.Equal(2, frame[4]);
        Assert.Equal(1, frame[7]);
        Assert.Equal((byte)((ushort)OmronFinsCommand.MemoryAreaRead >> 8), frame[10]);
        Assert.Equal(OmronFinsMemoryAreaCode.DmWord, frame[12]);
    }

    [Fact]
    public void ResponseParser_ExtractsPayloadAndParsesWordsAndBits()
    {
        var parser = new OmronFinsResponseParser();
        var frame = new byte[18];
        frame[14] = 0x12;
        frame[15] = 0x34;
        frame[16] = 0x00;
        frame[17] = 0x01;

        var payload = parser.ExtractPayload(frame).Value!;

        Assert.Equal(new short[] { 0x1234, 1 }, parser.ParseWords(payload, 2).Value);
        Assert.Equal(new[] { true, true }, parser.ParseBits(new byte[] { 1, 2 }, 2).Value);
    }

    [Fact]
    public void TcpPacket_WrapsAndExtractsFrame()
    {
        var frame = new byte[] { 1, 2, 3 };
        var packet = OmronFinsTcpPacket.Wrap(frame);

        Assert.Equal((byte)'F', packet[0]);
        Assert.Equal(frame, OmronFinsTcpPacket.Extract(packet));
        Assert.Throws<InvalidOperationException>(() => OmronFinsTcpPacket.Extract(new byte[] { 1, 2 }));
    }
}
