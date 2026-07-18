using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Fastech.Ethernet.Controllers;
using Dreamine.IO.Fastech.Ethernet.Options;
using Dreamine.IO.Fastech.Ethernet.Protocol;
using Dreamine.IO.Fastech.Ethernet.Transport;

namespace Dreamine.FullKit.Tests.IO;

/// <summary>
/// \if KO
/// <para>Fastech Protocol Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates fastech protocol tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FastechProtocolTests
{
    /// <summary>
    /// \if KO
    /// <para>Read Digital Inputs Creates Expected Frame Shape 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the read digital inputs creates expected frame shape value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Parse Digital Inputs Reads Bits From Input Payload 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse digital inputs reads bits from input payload operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Write Digital Outputs Rejects Out Of Range Channel 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the write digital outputs rejects out of range channel value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Plus E16 Point Protocol Reports Analog Unsupported Consistently 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the plus e16 point protocol reports analog unsupported consistently operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Unsupported Protocol Returns Failures For Parse Methods 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unsupported protocol returns failures for parse methods operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void UnsupportedProtocol_ReturnsFailuresForParseMethods()
    {
        var protocol = new UnsupportedFastechEthernetIoProtocol();

        Assert.False(protocol.ParseDigitalInputs(Array.Empty<byte>(), 1).IsSuccess);
        Assert.False(protocol.ParseDigitalOutputs(Array.Empty<byte>(), 1).IsSuccess);
        Assert.False(protocol.ParseAnalogInputs(Array.Empty<byte>(), 1).IsSuccess);
        Assert.Throws<NotSupportedException>(() => protocol.BuildReadDigitalInputs(Array.Empty<IoPoint>()));
    }

    /// <summary>
    /// \if KO
    /// <para>Controller Returns Failure When Parser Throws 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the controller returns failure when parser throws operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Controller Returns Failure When Parser Throws 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the controller returns failure when parser throws operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Fastech Options Expose Defaults 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fastech options expose defaults operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void FastechOptions_ExposeDefaults()
    {
        var options = new FastechEthernetIoOptions();

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal(FastechEthernetIoTransportType.Udp, options.TransportType);
    }

    /// <summary>
    /// \if KO
    /// <para>Fake Fastech Transport 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fake fastech transport functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FakeFastechTransport : IFastechEthernetIoTransport
    {
        /// <summary>
        /// \if KO
        /// <para>Is Connected 값을 가져오거나 설정합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets or sets the is connected value.</para>
        /// \endif
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// \if KO
        /// <para>Connect Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the connect async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Connect Async 작업에서 생성한 <c>Task&lt;IoResult&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;IoResult&gt;</c> result produced by the connect async operation.</para>
        /// \endif
        /// </returns>
        public Task<IoResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = true;
            return Task.FromResult(IoResult.Success());
        }

        /// <summary>
        /// \if KO
        /// <para>Disconnect Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the disconnect async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Disconnect Async 작업에서 생성한 <c>Task&lt;IoResult&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;IoResult&gt;</c> result produced by the disconnect async operation.</para>
        /// \endif
        /// </returns>
        public Task<IoResult> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            IsConnected = false;
            return Task.FromResult(IoResult.Success());
        }

        /// <summary>
        /// \if KO
        /// <para>Send And Receive Async 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the send and receive async operation.</para>
        /// \endif
        /// </summary>
        /// <param name="requestFrame">
        /// \if KO
        /// <para>request Frame에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for request frame.</para>
        /// \endif
        /// </param>
        /// <param name="receiveTimeoutMs">
        /// \if KO
        /// <para>receive Timeout Ms에 사용할 <c>int</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>int</c> value used for receive timeout ms.</para>
        /// \endif
        /// </param>
        /// <param name="expectedResponseLength">
        /// \if KO
        /// <para>expected Response Length에 사용할 <c>int</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>int</c> value used for expected response length.</para>
        /// \endif
        /// </param>
        /// <param name="cancellationToken">
        /// \if KO
        /// <para>취소 요청을 감시하는 토큰입니다.</para>
        /// \endif
        /// \if EN
        /// <para>A token used to observe cancellation requests.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Send And Receive Async 작업에서 생성한 <c>Task&lt;IoResult&lt;byte[]&gt;&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Task&lt;IoResult&lt;byte[]&gt;&gt;</c> result produced by the send and receive async operation.</para>
        /// \endif
        /// </returns>
        public Task<IoResult<byte[]>> SendAndReceiveAsync(
            IReadOnlyList<byte> requestFrame,
            int receiveTimeoutMs,
            int expectedResponseLength,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IoResult<byte[]>.Success([0xAA, 3, 1, 0, 0xC0, 0]));
        }

        /// <summary>
        /// \if KO
        /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Releases resources owned by this instance.</para>
        /// \endif
        /// </summary>
        /// <returns>
        /// \if KO
        /// <para>Dispose Async 작업에서 생성한 <c>ValueTask</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>ValueTask</c> result produced by the dispose async operation.</para>
        /// \endif
        /// </returns>
        public ValueTask DisposeAsync()
        {
            IsConnected = false;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Throwing Parse Protocol 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates throwing parse protocol functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class ThrowingParseProtocol : IFastechEthernetIoProtocol
    {
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
            return [1];
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
        /// <exception cref="InvalidOperationException">
        /// \if KO
        /// <para>현재 객체 상태에서 Parse Digital Inputs 작업을 수행할 수 없는 경우 발생합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Thrown when the parse digital inputs operation is not valid for the current object state.</para>
        /// \endif
        /// </exception>
        public IoResult<bool[]> ParseDigitalInputs(IReadOnlyList<byte> responseFrame, int count)
        {
            throw new InvalidOperationException("parse failed");
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
            return [1];
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
            return IoResult<bool[]>.Success([]);
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
            return [1];
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
        public byte[] BuildReadAnalogInputs(IReadOnlyList<AnalogIoPoint> points)
        {
            return [1];
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
            return IoResult<double[]>.Success([]);
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
        public byte[] BuildReadAnalogOutputs(IReadOnlyList<AnalogIoPoint> points)
        {
            return [1];
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
            return IoResult<double[]>.Success([]);
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
        public byte[] BuildWriteAnalogOutputs(IReadOnlyDictionary<AnalogIoPoint, double> values)
        {
            return [1];
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
            return IoResult.Success();
        }
    }
}
