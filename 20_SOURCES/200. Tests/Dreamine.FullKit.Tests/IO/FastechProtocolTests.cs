using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Fastech.Ethernet.Options;
using Dreamine.IO.Fastech.Ethernet.Protocol;

namespace Dreamine.FullKit.Tests.IO;

public sealed class FastechProtocolTests
{
    [Fact]
    public void BuildReadDigitalInputs_CreatesExpectedFrameShape()
    {
        var frame = new FastechPlusE16PointProtocol()
            .BuildReadDigitalInputs(new[] { new IoPoint(0, 0) });

        Assert.Equal(0xAA, frame[0]);
        Assert.Equal(3, frame[1]);
        Assert.Equal(0x00, frame[3]);
        Assert.Equal(0xC0, frame[4]);
    }

    [Fact]
    public void ParseDigitalInputs_ReadsBitsFromInputPayload()
    {
        var protocol = new FastechPlusE16PointProtocol();
        var response = new byte[]
        {
            0xAA, 12, 1, 0x00, 0xC0, 0x00,
            0b0000_0101, 0b0000_0010, 0, 0, 0, 0, 0, 0
        };

        var result = protocol.ParseDigitalInputs(response, 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { true, false, true, false, false, false, false, false, false, true }, result.Value);
    }

    [Fact]
    public void BuildWriteDigitalOutputs_RejectsOutOfRangeChannel()
    {
        var protocol = new FastechPlusE16PointProtocol();
        var values = new Dictionary<IoPoint, bool>
        {
            [new IoPoint(0, 16)] = true
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => protocol.BuildWriteDigitalOutputs(values));
    }

    [Fact]
    public void UnsupportedProtocol_ReturnsFailuresForParseMethods()
    {
        var protocol = new UnsupportedFastechEthernetIoProtocol();

        Assert.False(protocol.ParseDigitalInputs(Array.Empty<byte>(), 1).IsSuccess);
        Assert.False(protocol.ParseDigitalOutputs(Array.Empty<byte>(), 1).IsSuccess);
        Assert.False(protocol.ParseAnalogInputs(Array.Empty<byte>(), 1).IsSuccess);
        Assert.Throws<NotSupportedException>(() => protocol.BuildReadDigitalInputs(Array.Empty<IoPoint>()));
    }

    [Fact]
    public void FastechOptions_ExposeDefaults()
    {
        var options = new FastechEthernetIoOptions();

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal(FastechEthernetIoTransportType.Udp, options.TransportType);
    }
}
