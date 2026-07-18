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
/// \if KO
/// <para>Io Sample Runtime 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides the shared runtime state for the SampleSmart I/O sample.</para>
/// \endif
/// </summary>
public sealed class IoSampleRuntime : INotifyPropertyChanged, IAsyncDisposable
{
    /// <summary>
    /// \if KO
    /// <para>transport 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the transport value.</para>
    /// \endif
    /// </summary>
    private readonly SampleFastech16PointTransport _transport = new();
    /// <summary>
    /// \if KO
    /// <para>controller 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the controller value.</para>
    /// \endif
    /// </summary>
    private FastechEthernetIoController? _controller;
    /// <summary>
    /// \if KO
    /// <para>real Transport 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the real transport value.</para>
    /// \endif
    /// </summary>
    private UdpFastechEthernetIoTransport? _realTransport;
    /// <summary>
    /// \if KO
    /// <para>real Protocol 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the real protocol value.</para>
    /// \endif
    /// </summary>
    private FastechPlusE16PointProtocol? _realProtocol;
    /// <summary>
    /// \if KO
    /// <para>status Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status message value.</para>
    /// \endif
    /// </summary>
    private string _statusMessage = "Ready. Use Real UDP Controller for hardware, or Sample Controller for UI-only testing.";
    /// <summary>
    /// \if KO
    /// <para>is Connected 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is connected value.</para>
    /// \endif
    /// </summary>
    private bool _isConnected;
    /// <summary>
    /// \if KO
    /// <para>is Sample Mode 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the is sample mode value.</para>
    /// \endif
    /// </summary>
    private bool _isSampleMode;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="IoSampleRuntime"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="IoSampleRuntime"/> class.</para>
    /// \endif
    /// </summary>
    public IoSampleRuntime()
    {
        Inputs = new ObservableCollection<IoPointState>(
            Enumerable.Range(0, 16).Select(i => new IoPointState(0, i, $"DI{i:00}")));

        Outputs = new ObservableCollection<IoPointState>(
            Enumerable.Range(0, 16).Select(i => new IoPointState(0, i, $"DO{i:00}")));
    }

    /// <summary>
    /// \if KO
    /// <para>Property Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when property changed takes place.</para>
    /// \endif
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// \if KO
    /// <para>Inputs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the 16 digital input points.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<IoPointState> Inputs { get; }

    /// <summary>
    /// \if KO
    /// <para>Outputs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the 16 digital output points.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<IoPointState> Outputs { get; }

    /// <summary>
    /// \if KO
    /// <para>Host 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the Fastech Ethernet host text.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; } = "192.168.0.10";

    /// <summary>
    /// \if KO
    /// <para>Port Text 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the Fastech Ethernet port text.</para>
    /// \endif
    /// </summary>
    public string PortText { get; set; } = "3001";

    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current sample status message.</para>
    /// \endif
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
    /// \if KO
    /// <para>Is Connected 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets whether the sample controller is connected.</para>
    /// \endif
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
    /// \if KO
    /// <para>Use Sample Controller 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Uses an in-memory Fastech Ethernet controller for the sample.</para>
    /// \endif
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
    /// \if KO
    /// <para>Use Real Controller 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Uses the real Fastech Ezi-IO Plus-E UDP controller.</para>
    /// \endif
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
    /// \if KO
    /// <para>Connect Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Connects the current I/O controller.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Connect Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>Disconnect Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Disconnects the current I/O controller.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Disconnect Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>Probe Hardware Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sends a real Fastech GetSlaveInfo probe and reports raw frames.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Probe Hardware Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>Refresh Inputs Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Refreshes the 16 digital inputs from the current controller.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Refresh Inputs Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>Outputs Async 데이터를 씁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Writes the 16 digital output values to the current controller.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Write Outputs Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>Outputs Async 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads the 16 digital output values back from the current controller.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Read Outputs Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>Toggle Sample Inputs Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Toggles simulated input values and refreshes the input view.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Toggle Sample Inputs Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
    public async Task ToggleSampleInputsAsync()
    {
        _transport.ToggleInputPattern();
        await RefreshInputsAsync();
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
    public async ValueTask DisposeAsync()
    {
        if (_controller is not null)
        {
            await _controller.DisposeAsync();
        }

        await _transport.DisposeAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>Options 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the options value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Options 작업에서 생성한 <c>FastechEthernetIoOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FastechEthernetIoOptions</c> result produced by the create options operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Ensure Controller 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure controller operation.</para>
    /// \endif
    /// </summary>
    private void EnsureController()
    {
        if (_controller is not null)
        {
            return;
        }

        UseRealController();
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Connected 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure connected operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Ensure Connected 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the ensure connected condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Apply Values 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply values operation.</para>
    /// \endif
    /// </summary>
    /// <param name="points">
    /// \if KO
    /// <para>points에 사용할 <c>IReadOnlyList&lt;IoPointState&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;IoPointState&gt;</c> value used for points.</para>
    /// \endif
    /// </param>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>IReadOnlyList&lt;bool&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;bool&gt;</c> value used for values.</para>
    /// \endif
    /// </param>
    private static void ApplyValues(IReadOnlyList<IoPointState> points, IReadOnlyList<bool> values)
    {
        for (var i = 0; i < points.Count && i < values.Count; i++)
        {
            points[i].Value = values[i];
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Property Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the property changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="propertyName">
    /// \if KO
    /// <para>property Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for property name.</para>
    /// \endif
    /// </param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// \if KO
    /// <para>Mode Text 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the mode text value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Mode Text 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get mode text operation.</para>
    /// \endif
    /// </returns>
    private string GetModeText()
    {
        return _isSampleMode ? "sample" : "real UDP";
    }

    /// <summary>
    /// \if KO
    /// <para>Append Raw Frame Diagnostics 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the append raw frame diagnostics operation.</para>
    /// \endif
    /// </summary>
    private void AppendRawFrameDiagnostics()
    {
        var diagnostics = GetRawFrameDiagnostics();
        if (!string.IsNullOrWhiteSpace(diagnostics))
        {
            StatusMessage = $"{StatusMessage} {diagnostics}";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Raw Frame Diagnostics 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the raw frame diagnostics value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get Raw Frame Diagnostics 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get raw frame diagnostics operation.</para>
    /// \endif
    /// </returns>
    private string GetRawFrameDiagnostics()
    {
        return _realTransport is null || _isSampleMode
            ? string.Empty
            : $"TX={ToHex(_realTransport.LastRequestFrame)} RX={ToHex(_realTransport.LastResponseFrame)}";
    }

    /// <summary>
    /// \if KO
    /// <para>To Hex 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to hex operation.</para>
    /// \endif
    /// </summary>
    /// <param name="bytes">
    /// \if KO
    /// <para>bytes에 사용할 <c>IReadOnlyList&lt;byte&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;byte&gt;</c> value used for bytes.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Hex 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to hex operation.</para>
    /// \endif
    /// </returns>
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
