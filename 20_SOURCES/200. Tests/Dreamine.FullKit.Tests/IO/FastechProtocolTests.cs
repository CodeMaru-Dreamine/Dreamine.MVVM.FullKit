using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Fastech.Ethernet.Controllers;
using Dreamine.IO.Fastech.Ethernet.Options;
using Dreamine.IO.Fastech.Ethernet.Protocol;
using Dreamine.IO.Fastech.Ethernet.Transport;

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
    public void PlusE16PointProtocol_ReportsAnalogUnsupportedConsistently()
    {
        var protocol = new FastechPlusE16PointProtocol();
        var point = new AnalogIoPoint(0, 0);

        var inputException = Assert.Throws<NotSupportedException>(() => protocol.BuildReadAnalogInputs([point]));
        var inputParse = protocol.ParseAnalogInputs([], 1);
        var outputException = Assert.Throws<NotSupportedException>(() => protocol.BuildReadAnalogOutputs([point]));
        var outputParse = protocol.ParseAnalogOutputs([], 1);

        Assert.Equal(inputException.Message, inputParse.Message);
        Assert.Equal(outputException.Message, outputParse.Message);
        Assert.False(inputParse.IsSuccess);
        Assert.False(outputParse.IsSuccess);
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
    public async Task Controller_ReturnsFailureWhenParserThrows()
    {
        var transport = new FakeFastechTransport();
        await using var controller = new FastechEthernetIoController(
            new FastechEthernetIoOptions(),
            transport,
            new ThrowingParseProtocol());

        await controller.ConnectAsync();

        var result = await controller.DigitalInputs.ReadAsync([new IoPoint(0, 0)]);

        Assert.False(result.IsSuccess);
        Assert.Contains("parse failed", result.Message);
    }

    [Fact]
    public void FastechOptions_ExposeDefaults()
    {
        var options = new FastechEthernetIoOptions();

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal(FastechEthernetIoTransportType.Udp, options.TransportType);
    }

    private sealed class FakeFastechTransport : IFastechEthernetIoTransport
    {
        public bool IsConnected { get; private set; }

        public Task<IoResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.FromResult(IoResult.Success());
        }

        public Task<IoResult> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.FromResult(IoResult.Success());
        }

        public Task<IoResult<byte[]>> SendAndReceiveAsync(
            IReadOnlyList<byte> requestFrame,
            int receiveTimeoutMs,
            int expectedResponseLength,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IoResult<byte[]>.Success([0xAA, 3, 1, 0, 0xC0, 0]));
        }

        public ValueTask DisposeAsync()
        {
            IsConnected = false;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingParseProtocol : IFastechEthernetIoProtocol
    {
        public byte[] BuildReadDigitalInputs(IReadOnlyList<IoPoint> points)
        {
            return [1];
        }

        public IoResult<bool[]> ParseDigitalInputs(IReadOnlyList<byte> responseFrame, int count)
        {
            throw new InvalidOperationException("parse failed");
        }

        public byte[] BuildReadDigitalOutputs(IReadOnlyList<IoPoint> points)
        {
            return [1];
        }

        public IoResult<bool[]> ParseDigitalOutputs(IReadOnlyList<byte> responseFrame, int count)
        {
            return IoResult<bool[]>.Success([]);
        }

        public byte[] BuildWriteDigitalOutputs(IReadOnlyDictionary<IoPoint, bool> values)
        {
            return [1];
        }

        public byte[] BuildReadAnalogInputs(IReadOnlyList<AnalogIoPoint> points)
        {
            return [1];
        }

        public IoResult<double[]> ParseAnalogInputs(IReadOnlyList<byte> responseFrame, int count)
        {
            return IoResult<double[]>.Success([]);
        }

        public byte[] BuildReadAnalogOutputs(IReadOnlyList<AnalogIoPoint> points)
        {
            return [1];
        }

        public IoResult<double[]> ParseAnalogOutputs(IReadOnlyList<byte> responseFrame, int count)
        {
            return IoResult<double[]>.Success([]);
        }

        public byte[] BuildWriteAnalogOutputs(IReadOnlyDictionary<AnalogIoPoint, double> values)
        {
            return [1];
        }

        public IoResult ParseWriteResponse(IReadOnlyList<byte> responseFrame)
        {
            return IoResult.Success();
        }
    }
}
