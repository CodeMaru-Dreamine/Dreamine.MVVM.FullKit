using System.Text;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Fastech.Ethernet.Transport;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// \if KO
/// <para>Sample Fastech16 Point Transport 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides an in-memory transport for the SampleSmart 16-in/16-out Fastech Ethernet I/O demo.</para>
/// \endif
/// </summary>
public sealed class SampleFastech16PointTransport : IFastechEthernetIoTransport
{
    /// <summary>
    /// \if KO
    /// <para>inputs 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the inputs value.</para>
    /// \endif
    /// </summary>
    private readonly bool[] _inputs = new bool[16];
    /// <summary>
    /// \if KO
    /// <para>outputs 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the outputs value.</para>
    /// \endif
    /// </summary>
    private readonly bool[] _outputs = new bool[16];
    /// <summary>
    /// \if KO
    /// <para>disposed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the disposed value.</para>
    /// \endif
    /// </summary>
    private bool _disposed;

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
    /// <para>Input 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets a simulated input value.</para>
    /// \endif
    /// </summary>
    /// <param name="channel">
    /// \if KO
    /// <para>channel에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input channel.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The input value.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// \if KO
    /// <para>입력 값이 허용 범위를 벗어난 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when an input value is outside the allowed range.</para>
    /// \endif
    /// </exception>
    public void SetInput(int channel, bool value)
    {
        if (channel is < 0 or >= 16)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "Channel must be between 0 and 15.");
        }

        _inputs[channel] = value;
    }

    /// <summary>
    /// \if KO
    /// <para>Toggle Input Pattern 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Toggles every even input point for a quick visual test.</para>
    /// \endif
    /// </summary>
    public void ToggleInputPattern()
    {
        for (var i = 0; i < _inputs.Length; i++)
        {
            _inputs[i] = i % 2 == 0
                ? !_inputs[i]
                : _inputs[i];
        }
    }

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
        ThrowIfDisposed();
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
        ArgumentNullException.ThrowIfNull(requestFrame);
        ThrowIfDisposed();

        if (!IsConnected)
        {
            return Task.FromResult(IoResult<byte[]>.Failure("The sample Fastech transport is not connected."));
        }

        var request = Encoding.ASCII.GetString(requestFrame as byte[] ?? requestFrame.ToArray()).Trim();
        var response = HandleRequest(request);

        return Task.FromResult(IoResult<byte[]>.Success(Encoding.ASCII.GetBytes(response)));
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
        _disposed = true;
        IsConnected = false;

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>Handle Request 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the handle request operation.</para>
    /// \endif
    /// </summary>
    /// <param name="request">
    /// \if KO
    /// <para>request에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for request.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Handle Request 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the handle request operation.</para>
    /// \endif
    /// </returns>
    private string HandleRequest(string request)
    {
        if (request.StartsWith("RDI", StringComparison.OrdinalIgnoreCase))
        {
            return $"DI {ToBitString(_inputs)}";
        }

        if (request.StartsWith("RDO", StringComparison.OrdinalIgnoreCase))
        {
            return $"DO {ToBitString(_outputs)}";
        }

        if (request.StartsWith("WDO ", StringComparison.OrdinalIgnoreCase))
        {
            var bits = request[4..].Trim();
            for (var i = 0; i < _outputs.Length && i < bits.Length; i++)
            {
                _outputs[i] = bits[i] == '1';
            }

            return "OK";
        }

        return "ERR Unknown sample request";
    }

    /// <summary>
    /// \if KO
    /// <para>To Bit String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to bit string operation.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>IReadOnlyList&lt;bool&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;bool&gt;</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Bit String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to bit string operation.</para>
    /// \endif
    /// </returns>
    private static string ToBitString(IReadOnlyList<bool> values)
    {
        return new string(values.Select(x => x ? '1' : '0').ToArray());
    }

    /// <summary>
    /// \if KO
    /// <para>Throw If Disposed 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the throw if disposed operation.</para>
    /// \endif
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
