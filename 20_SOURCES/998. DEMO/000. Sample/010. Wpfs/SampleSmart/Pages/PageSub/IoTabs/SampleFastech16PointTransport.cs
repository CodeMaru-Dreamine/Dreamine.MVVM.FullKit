using System.Text;
using Dreamine.IO.Abstractions.Results;
using Dreamine.IO.Fastech.Ethernet.Transport;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// Provides an in-memory transport for the SampleSmart 16-in/16-out Fastech Ethernet I/O demo.
/// </summary>
public sealed class SampleFastech16PointTransport : IFastechEthernetIoTransport
{
    private readonly bool[] _inputs = new bool[16];
    private readonly bool[] _outputs = new bool[16];
    private bool _disposed;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Sets a simulated input value.
    /// </summary>
    /// <param name="channel">The input channel.</param>
    /// <param name="value">The input value.</param>
    public void SetInput(int channel, bool value)
    {
        if (channel is < 0 or >= 16)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "Channel must be between 0 and 15.");
        }

        _inputs[channel] = value;
    }

    /// <summary>
    /// Toggles every even input point for a quick visual test.
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

    /// <inheritdoc />
    public Task<IoResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        IsConnected = true;

        return Task.FromResult(IoResult.Success());
    }

    /// <inheritdoc />
    public Task<IoResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;

        return Task.FromResult(IoResult.Success());
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _disposed = true;
        IsConnected = false;

        return ValueTask.CompletedTask;
    }

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

    private static string ToBitString(IReadOnlyList<bool> values)
    {
        return new string(values.Select(x => x ? '1' : '0').ToArray());
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
