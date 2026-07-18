using Dreamine.PLC.Abstractions.Devices;
using Dreamine.PLC.Omron.Fins.Devices;
using Dreamine.PLC.Omron.Fins.Options;
using Dreamine.PLC.Omron.Fins.Protocol;

namespace Dreamine.FullKit.Tests.PLC;

/// <summary>
/// \if KO
/// <para>Omron Fins Protocol Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates omron fins protocol tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class OmronFinsProtocolTests
{
    /// <summary>
    /// \if KO
    /// <para>Memory Area Mapper Maps Device Types And Area Kinds 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the memory area mapper maps device types and area kinds operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Frame Builder Builds Memory Read Frame 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the frame builder builds memory read frame operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Frame Builder Rejects Out Of Range Address With Clear Message 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the frame builder rejects out of range address with clear message operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void FrameBuilder_RejectsOutOfRangeAddressWithClearMessage()
    {
        var options = new OmronFinsConnectionOptions();
        var builder = new OmronFinsFrameBuilder();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.BuildMemoryAreaRead(
                options,
                new PlcAddress(PlcDeviceType.D, 65536),
                1,
                bitAccess: false));

        Assert.Contains("between 0 and 65535", ex.Message);
    }

    /// <summary>
    /// \if KO
    /// <para>Frame Builder Generates Distinct Sids Across Parallel Calls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the frame builder generates distinct sids across parallel calls operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void FrameBuilder_GeneratesDistinctSidsAcrossParallelCalls()
    {
        var options = new OmronFinsConnectionOptions();
        var builder = new OmronFinsFrameBuilder();

        var sids = Enumerable.Range(0, 64)
            .AsParallel()
            .Select(_ => builder.BuildMemoryAreaRead(options, new PlcAddress(PlcDeviceType.D, 0), 1, false)[9])
            .ToArray();

        Assert.Equal(64, sids.Distinct().Count());
    }

    /// <summary>
    /// \if KO
    /// <para>Response Parser Extracts Payload And Parses Words And Bits 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the response parser extracts payload and parses words and bits operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Tcp Packet Wraps And Extracts Frame 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the tcp packet wraps and extracts frame operation.</para>
    /// \endif
    /// </summary>
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
