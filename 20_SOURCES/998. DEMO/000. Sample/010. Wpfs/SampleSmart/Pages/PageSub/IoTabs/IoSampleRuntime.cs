using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Dreamine.IO.Abstractions.Models;
using Dreamine.IO.Fastech.Ethernet.Controllers;
using Dreamine.IO.Fastech.Ethernet.Options;
using Dreamine.IO.Fastech.Ethernet.Protocol;
using Dreamine.IO.Fastech.Ethernet.Transport;

namespace SampleSmart.Pages.PageSub.IoTabs;

/// <summary>
/// Provides the shared runtime state for the SampleSmart I/O sample.
/// </summary>
public sealed class IoSampleRuntime : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly SampleFastech16PointTransport _transport = new();
    private FastechEthernetIoController? _controller;
    private UdpFastechEthernetIoTransport? _realTransport;
    private FastechPlusE16PointProtocol? _realProtocol;
    private string _statusMessage = "Ready. Use Real UDP Controller for hardware, or Sample Controller for UI-only testing.";
    private bool _isConnected;
    private bool _isSampleMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="IoSampleRuntime"/> class.
    /// </summary>
    public IoSampleRuntime()
    {
        Inputs = new ObservableCollection<IoPointState>(
            Enumerable.Range(0, 16).Select(i => new IoPointState(0, i, $"DI{i:00}")));

        Outputs = new ObservableCollection<IoPointState>(
            Enumerable.Range(0, 16).Select(i => new IoPointState(0, i, $"DO{i:00}")));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the 16 digital input points.
    /// </summary>
    public ObservableCollection<IoPointState> Inputs { get; }

    /// <summary>
    /// Gets the 16 digital output points.
    /// </summary>
    public ObservableCollection<IoPointState> Outputs { get; }

    /// <summary>
    /// Gets or sets the Fastech Ethernet host text.
    /// </summary>
    public string Host { get; set; } = "192.168.0.10";

    /// <summary>
    /// Gets or sets the Fastech Ethernet port text.
    /// </summary>
    public string PortText { get; set; } = "3001";

    /// <summary>
    /// Gets the current sample status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets whether the sample controller is connected.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected == value)
            {
                return;
            }

            _isConnected = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Uses an in-memory Fastech Ethernet controller for the sample.
    /// </summary>
    public void UseSampleController()
    {
        _controller = new FastechEthernetIoController(
            CreateOptions(),
            _transport,
            new SampleFastech16PointProtocol());

        _realTransport = null;
        _realProtocol = null;
        _isSampleMode = true;
        IsConnected = false;
        StatusMessage = "Sample Fastech Ethernet 16/16 controller selected.";
    }

    /// <summary>
    /// Uses the real Fastech Ezi-IO Plus-E UDP controller.
    /// </summary>
    public void UseRealController()
    {
        var options = CreateOptions();
        _realTransport = new UdpFastechEthernetIoTransport(options);
        _realProtocol = new FastechPlusE16PointProtocol();
        _controller = new FastechEthernetIoController(options, _realTransport, _realProtocol);

        _isSampleMode = false;
        IsConnected = false;
        StatusMessage = $"Real Fastech Ezi-IO Plus-E UDP controller selected. Target={Host}:{PortText}.";
    }

    /// <summary>
    /// Connects the current I/O controller.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConnectAsync()
    {
        EnsureController();

        if (_controller is null)
        {
            return;
        }

        var result = await _controller.ConnectAsync();
        IsConnected = result.IsSuccess;
        StatusMessage = result.IsSuccess
            ? $"Fastech {GetModeText()} controller connected. Use Probe or Read DI to confirm device response."
            : result.Message ?? $"Failed to connect the Fastech {GetModeText()} controller.";
    }

    /// <summary>
    /// Disconnects the current I/O controller.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DisconnectAsync()
    {
        if (_controller is null)
        {
            return;
        }

        var result = await _controller.DisconnectAsync();
        IsConnected = false;
        StatusMessage = result.IsSuccess
            ? $"Fastech {GetModeText()} controller disconnected."
            : result.Message ?? $"Failed to disconnect the Fastech {GetModeText()} controller.";
    }

    /// <summary>
    /// Sends a real Fastech GetSlaveInfo probe and reports raw frames.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ProbeHardwareAsync()
    {
        if (_isSampleMode || _realTransport is null || _realProtocol is null)
        {
            UseRealController();
        }

        if (!IsConnected)
        {
            await ConnectAsync();
        }

        if (_realTransport is null || _realProtocol is null)
        {
            StatusMessage = "Real Fastech UDP controller is not ready.";
            return;
        }

        var request = _realProtocol.BuildGetSlaveInfo();
        var response = await _realTransport.SendAndReceiveAsync(request, 1000, 0);
        if (!response.IsSuccess || response.Value is null)
        {
            StatusMessage = $"Probe failed: {response.Message}. TX={ToHex(_realTransport.LastRequestFrame)}";
            return;
        }

        var info = _realProtocol.ParseSlaveInfo(response.Value);
        StatusMessage = info.IsSuccess
            ? $"Probe OK: {info.Value}. TX={ToHex(_realTransport.LastRequestFrame)} RX={ToHex(_realTransport.LastResponseFrame)}"
            : $"Probe parse failed: {info.Message}. TX={ToHex(_realTransport.LastRequestFrame)} RX={ToHex(_realTransport.LastResponseFrame)}";
    }

    /// <summary>
    /// Refreshes the 16 digital inputs from the current controller.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RefreshInputsAsync()
    {
        if (!EnsureConnected())
        {
            return;
        }

        var points = Inputs
            .Select(x => new IoPoint(x.Module, x.Channel, x.Name))
            .ToArray();

        var result = await _controller!.DigitalInputs.ReadAsync(points);
        if (!result.IsSuccess || result.Value is null)
        {
            StatusMessage = result.Message ?? "Failed to refresh digital inputs.";
            AppendRawFrameDiagnostics();
            return;
        }

        ApplyValues(Inputs, result.Value);
        StatusMessage = $"Digital inputs refreshed from the Fastech {GetModeText()} controller. {GetRawFrameDiagnostics()}";
    }

    /// <summary>
    /// Writes the 16 digital output values to the current controller.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task WriteOutputsAsync()
    {
        if (!EnsureConnected())
        {
            return;
        }

        var values = Outputs.ToDictionary(
            x => new IoPoint(x.Module, x.Channel, x.Name),
            x => x.Value);

        var result = await _controller!.DigitalOutputs.WriteAsync(values);
        StatusMessage = result.IsSuccess
            ? $"Digital outputs written to the Fastech {GetModeText()} controller. {GetRawFrameDiagnostics()}"
            : result.Message ?? "Failed to write digital outputs.";
        if (!result.IsSuccess)
        {
            AppendRawFrameDiagnostics();
        }
    }

    /// <summary>
    /// Reads the 16 digital output values back from the current controller.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ReadOutputsAsync()
    {
        if (!EnsureConnected())
        {
            return;
        }

        var points = Outputs
            .Select(x => new IoPoint(x.Module, x.Channel, x.Name))
            .ToArray();

        var result = await _controller!.DigitalOutputs.ReadAsync(points);
        if (!result.IsSuccess || result.Value is null)
        {
            StatusMessage = result.Message ?? "Failed to read digital outputs.";
            AppendRawFrameDiagnostics();
            return;
        }

        ApplyValues(Outputs, result.Value);
        StatusMessage = $"Digital outputs read back from the Fastech {GetModeText()} controller. {GetRawFrameDiagnostics()}";
    }

    /// <summary>
    /// Toggles simulated input values and refreshes the input view.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ToggleSampleInputsAsync()
    {
        _transport.ToggleInputPattern();
        await RefreshInputsAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_controller is not null)
        {
            await _controller.DisposeAsync();
        }

        await _transport.DisposeAsync();
    }

    private FastechEthernetIoOptions CreateOptions()
    {
        var port = int.TryParse(PortText, out var parsedPort)
            ? parsedPort
            : 3001;

        return new FastechEthernetIoOptions
        {
            Host = string.IsNullOrWhiteSpace(Host) ? "127.0.0.1" : Host,
            Port = port,
            TransportType = Dreamine.IO.Fastech.Ethernet.Options.FastechEthernetIoTransportType.Udp,
            ExpectedResponseLength = 0
        };
    }

    private void EnsureController()
    {
        if (_controller is not null)
        {
            return;
        }

        UseRealController();
    }

    private bool EnsureConnected()
    {
        EnsureController();

        if (_controller is null || !IsConnected)
        {
            StatusMessage = $"Connect the Fastech {GetModeText()} controller first.";
            return false;
        }

        return true;
    }

    private static void ApplyValues(IReadOnlyList<IoPointState> points, IReadOnlyList<bool> values)
    {
        for (var i = 0; i < points.Count && i < values.Count; i++)
        {
            points[i].Value = values[i];
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string GetModeText()
    {
        return _isSampleMode ? "sample" : "real UDP";
    }

    private void AppendRawFrameDiagnostics()
    {
        var diagnostics = GetRawFrameDiagnostics();
        if (!string.IsNullOrWhiteSpace(diagnostics))
        {
            StatusMessage = $"{StatusMessage} {diagnostics}";
        }
    }

    private string GetRawFrameDiagnostics()
    {
        return _realTransport is null || _isSampleMode
            ? string.Empty
            : $"TX={ToHex(_realTransport.LastRequestFrame)} RX={ToHex(_realTransport.LastResponseFrame)}";
    }

    private static string ToHex(IReadOnlyList<byte> bytes)
    {
        if (bytes.Count == 0)
        {
            return "<none>";
        }

        var builder = new StringBuilder(bytes.Count * 3);
        for (var i = 0; i < bytes.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(bytes[i].ToString("X2"));
        }

        return builder.ToString();
    }
}
