using Dreamine.PLC.Abstractions.Clients;
using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Core.Clients;
using Dreamine.PLC.Core.Devices;
using Dreamine.PLC.Core.Simulation;
using Dreamine.PLC.Mitsubishi.MC.Clients;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Simulation;
using Dreamine.PLC.Omron.Fins.Clients;
using Dreamine.PLC.Omron.Fins.Options;
using Dreamine.PLC.Omron.Fins.Simulation;
using Dreamine.PLC.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \brief PLC 샘플 전체에서 공유되는 Runtime 컨텍스트입니다.
/// </summary>
public sealed class PlcSampleRuntime
{
    private readonly InMemoryPlcClient _inMemoryClient = new();
    private readonly DefaultPlcAddressParser _addressParser = new();
    private IPlcClient _activeClient;
    private PlcSimulatorServer? _simulatorServer;
    private MitsubishiMcTcpSimulatorServer? _mcTcpSimulatorServer;
    private MitsubishiMcUdpSimulatorServer? _mcUdpSimulatorServer;
    private OmronFinsTcpSimulatorServer? _finsTcpSimulatorServer;
    private OmronFinsUdpSimulatorServer? _finsUdpSimulatorServer;
    private PlcSimulatorTcpClient? _simulatorClient;
    private MitsubishiMcPlcClient? _mitsubishiMcClient;
    private OmronFinsPlcClient? _omronFinsClient;
    private string _activeServerMode = string.Empty;

    /// <summary>
    /// \brief PlcSampleRuntime 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public PlcSampleRuntime()
    {
        _activeClient = _inMemoryClient;
        Monitor = new PlcMonitorViewModel(_inMemoryClient, "InMemory PLC");
    }

    /// <summary>
    /// \brief PLC Monitor ViewModel입니다.
    /// </summary>
    public PlcMonitorViewModel Monitor { get; }

    /// <summary>
    /// \brief PLC Simulator 서버 상태 메시지입니다.
    /// </summary>
    public string ServerStatus { get; private set; } = "Server stopped.";

    /// <summary>
    /// \brief InMemory PLC Client를 모니터에 연결합니다.
    /// </summary>
    public void UseInMemoryClient()
    {
        _activeClient = _inMemoryClient;
        Monitor.SetClient(_inMemoryClient, "InMemory PLC");
        Monitor.StatusMessage = "InMemory PLC client selected.";
    }

    /// <summary>
    /// \brief TCP Simulator Client를 모니터에 연결합니다.
    /// </summary>
    /// <param name="host">서버 Host입니다.</param>
    /// <param name="port">서버 Port입니다.</param>
    public void UseSimulatorTcpClient(string host, int port)
    {
        _simulatorClient = new PlcSimulatorTcpClient(new PlcSimulatorClientOptions
        {
            Host = host,
            Port = port
        });

        _activeClient = _simulatorClient;
        Monitor.SetClient(_simulatorClient, $"PLC Simulator TCP Client ({host}:{port})");
        Monitor.StatusMessage = "PLC Simulator TCP client selected.";
    }


    /// <summary>
    /// \brief Mitsubishi MC Client를 모니터에 연결합니다.
    /// </summary>
    /// <param name="host">PLC Host입니다.</param>
    /// <param name="port">PLC Port입니다.</param>
    /// <param name="transportText">Transport 문자열입니다. Tcp 또는 Udp를 사용합니다.</param>
    /// <param name="retryCount">송수신 재시도 횟수입니다.</param>
    public void UseMitsubishiMcClient(string host, int port, string transportText, int retryCount)
    {
        if (!Enum.TryParse<MitsubishiMcTransportType>(transportText, ignoreCase: true, out var transportType))
        {
            Monitor.StatusMessage = "MC transport must be Tcp or Udp.";
            return;
        }

        _mitsubishiMcClient = new MitsubishiMcPlcClient(new MitsubishiMcConnectionOptions
        {
            Host = host,
            Port = port,
            TransportType = transportType,
            RetryCount = Math.Max(1, retryCount),
            ConnectTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000,
            SendTimeoutMs = 3000
        });

        _activeClient = _mitsubishiMcClient;
        Monitor.SetClient(_mitsubishiMcClient, $"Mitsubishi MC {transportType} ({host}:{port})");
        Monitor.StatusMessage = $"Mitsubishi MC {transportType} client selected.";
    }


    /// <summary>
    /// \brief Omron FINS Client를 모니터에 연결합니다.
    /// </summary>
    /// <param name="host">PLC Host입니다.</param>
    /// <param name="port">PLC Port입니다.</param>
    /// <param name="transportText">Transport 문자열입니다. Tcp 또는 Udp를 사용합니다.</param>
    /// <param name="retryCount">송수신 재시도 횟수입니다.</param>
    public void UseOmronFinsClient(string host, int port, string transportText, int retryCount)
    {
        if (!Enum.TryParse<OmronFinsTransportType>(transportText, ignoreCase: true, out var transportType))
        {
            Monitor.StatusMessage = "FINS transport must be Tcp or Udp.";
            return;
        }

        _omronFinsClient = new OmronFinsPlcClient(new OmronFinsConnectionOptions
        {
            Host = host,
            Port = port,
            TransportType = transportType,
            RetryCount = Math.Max(1, retryCount),
            ConnectTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000,
            DestinationNode = 0x00,
            SourceNode = 0x01
        });

        _activeClient = _omronFinsClient;
        Monitor.SetClient(_omronFinsClient, $"Omron FINS {transportType} ({host}:{port})");
        Monitor.StatusMessage = $"Omron FINS {transportType} client selected.";
    }

    /// <summary>
    /// \brief 선택된 모드에 맞는 PLC Protocol Simulator Server를 시작합니다.
    /// </summary>
    /// <param name="modeText">서버 모드입니다. SimulatorTcp, McTcp, McUdp를 사용합니다.</param>
    /// <param name="host">Bind Host입니다.</param>
    /// <param name="port">Bind Port입니다.</param>
    public async Task StartProtocolServerAsync(string modeText, string host, int port)
    {
        var mode = NormalizeMode(modeText);
        if (IsAnyServerRunning())
        {
            ServerStatus = $"Server already running. mode={_activeServerMode}. Stop the current server first.";
            Monitor.StatusMessage = ServerStatus;
            return;
        }

        switch (mode)
        {
            case "SIMULATORTCP":
                _simulatorServer = new PlcSimulatorServer(new PlcSimulatorServerOptions
                {
                    Host = host,
                    Port = port,
                    EnableAutoWordResponse = true,
                    AutoResponseTriggerAddress = "D100",
                    AutoResponseAddress = "D101",
                    AutoResponseIncrement = 1
                });

                _simulatorServer.StatusChanged += OnSimulatorServerStatusChanged;
                await _simulatorServer.StartAsync();
                _activeServerMode = "SimulatorTcp";
                break;

            case "MCTCP":
                _mcTcpSimulatorServer = new MitsubishiMcTcpSimulatorServer(CreateMcSimulatorOptions(host, port));
                _mcTcpSimulatorServer.StatusChanged += OnSimulatorServerStatusChanged;
                await _mcTcpSimulatorServer.StartAsync();
                _activeServerMode = "McTcp";
                break;

            case "MCUDP":
                _mcUdpSimulatorServer = new MitsubishiMcUdpSimulatorServer(CreateMcSimulatorOptions(host, port));
                _mcUdpSimulatorServer.StatusChanged += OnSimulatorServerStatusChanged;
                await _mcUdpSimulatorServer.StartAsync();
                _activeServerMode = "McUdp";
                break;

            case "FINSTCP":
                _finsTcpSimulatorServer = new OmronFinsTcpSimulatorServer(CreateFinsSimulatorOptions(host, port));
                _finsTcpSimulatorServer.StatusChanged += OnSimulatorServerStatusChanged;
                await _finsTcpSimulatorServer.StartAsync();
                _activeServerMode = "FinsTcp";
                break;

            case "FINSUDP":
                _finsUdpSimulatorServer = new OmronFinsUdpSimulatorServer(CreateFinsSimulatorOptions(host, port));
                _finsUdpSimulatorServer.StatusChanged += OnSimulatorServerStatusChanged;
                await _finsUdpSimulatorServer.StartAsync();
                _activeServerMode = "FinsUdp";
                break;

            default:
                Monitor.StatusMessage = "Server mode must be SimulatorTcp, McTcp, McUdp, FinsTcp, or FinsUdp.";
                return;
        }

        ServerStatus = $"{_activeServerMode} server running. {host}:{port}";
        Monitor.StatusMessage = ServerStatus;
    }

    /// <summary>
    /// \brief 실행 중인 PLC Protocol Simulator Server를 중지합니다.
    /// </summary>
    public async Task StopProtocolServerAsync()
    {
        if (!IsAnyServerRunning())
        {
            ServerStatus = "Server stopped.";
            Monitor.StatusMessage = ServerStatus;
            return;
        }

        if (_simulatorServer is not null)
        {
            _simulatorServer.StatusChanged -= OnSimulatorServerStatusChanged;
            await _simulatorServer.StopAsync();
            _simulatorServer = null;
        }

        if (_mcTcpSimulatorServer is not null)
        {
            _mcTcpSimulatorServer.StatusChanged -= OnSimulatorServerStatusChanged;
            await _mcTcpSimulatorServer.StopAsync();
            _mcTcpSimulatorServer = null;
        }

        if (_mcUdpSimulatorServer is not null)
        {
            _mcUdpSimulatorServer.StatusChanged -= OnSimulatorServerStatusChanged;
            await _mcUdpSimulatorServer.StopAsync();
            _mcUdpSimulatorServer = null;
        }

        if (_finsTcpSimulatorServer is not null)
        {
            _finsTcpSimulatorServer.StatusChanged -= OnSimulatorServerStatusChanged;
            await _finsTcpSimulatorServer.StopAsync();
            _finsTcpSimulatorServer = null;
        }

        if (_finsUdpSimulatorServer is not null)
        {
            _finsUdpSimulatorServer.StatusChanged -= OnSimulatorServerStatusChanged;
            await _finsUdpSimulatorServer.StopAsync();
            _finsUdpSimulatorServer = null;
        }

        _activeServerMode = string.Empty;
        ServerStatus = "Server stopped.";
        Monitor.StatusMessage = ServerStatus;
    }

    /// <summary>
    /// \brief 이전 호환성을 위해 SimulatorTcp 서버를 시작합니다.
    /// </summary>
    /// <param name="host">Bind Host입니다.</param>
    /// <param name="port">Bind Port입니다.</param>
    public Task StartSimulatorServerAsync(string host, int port)
    {
        return StartProtocolServerAsync("SimulatorTcp", host, port);
    }

    /// <summary>
    /// \brief 이전 호환성을 위해 실행 중인 서버를 중지합니다.
    /// </summary>
    public Task StopSimulatorServerAsync()
    {
        return StopProtocolServerAsync();
    }

    /// <summary>
    /// \brief Runs a client-server auto-response handshake test using D100 as client write and D101 as server response.
    /// </summary>
    /// <param name="startValue">The first client value.</param>
    /// <param name="iterations">The number of handshake iterations.</param>
    /// <param name="delayMs">The delay between iterations in milliseconds.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RunHandshakeTestAsync(
        short startValue,
        int iterations,
        int delayMs,
        CancellationToken cancellationToken = default)
    {
        if (iterations <= 0)
        {
            Monitor.StatusMessage = "Handshake iterations must be greater than zero.";
            Monitor.AppendLog("Handshake", "D100/D101", iterations.ToString(), false, Monitor.StatusMessage);
            return;
        }

        if (_activeClient.State != PlcConnectionState.Connected)
        {
            Monitor.StatusMessage = "Handshake test requires a connected PLC client.";
            Monitor.AppendLog("Handshake", "D100/D101", string.Empty, false, Monitor.StatusMessage);
            return;
        }

        var clientWriteAddress = _addressParser.Parse("D100").Value;
        var serverResponseAddress = _addressParser.Parse("D101").Value;
        var value = startValue;

        for (var index = 0; index < iterations; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var writeResult = await _activeClient.WriteWordsAsync(clientWriteAddress, [value], cancellationToken).ConfigureAwait(true);
            if (!writeResult.IsSuccess)
            {
                Monitor.StatusMessage = writeResult.Message ?? "Handshake write failed.";
                Monitor.AppendLog("HandshakeWrite", "D100", value.ToString(), false, Monitor.StatusMessage);
                return;
            }

            var rawExpected = value + 1;
            if (rawExpected > short.MaxValue)
            {
                Monitor.StatusMessage = $"Handshake stopped before 16-bit overflow. last={value}, next={rawExpected}.";
                Monitor.AppendLog("Handshake", "D100/D101", $"last={value}, next={rawExpected}", false, Monitor.StatusMessage);
                return;
            }

            var expected = (short)rawExpected;
            var readResult = await _activeClient.ReadWordsAsync(serverResponseAddress, 1, cancellationToken).ConfigureAwait(true);
            var actual = readResult.Value is { Length: > 0 } ? readResult.Value[0] : short.MinValue;
            if (!readResult.IsSuccess || actual != expected)
            {
                Monitor.StatusMessage = readResult.IsSuccess
                    ? $"Handshake mismatch. expected={expected}, actual={actual}."
                    : readResult.Message ?? "Handshake read failed.";
                Monitor.AppendLog("HandshakeRead", "D101", $"expected={expected}, actual={actual}", false, Monitor.StatusMessage);
                return;
            }

            Monitor.StatusMessage = $"Handshake OK {index + 1}/{iterations}. D100={value}, D101={actual}";
            Monitor.AppendLog("Handshake", "D100/D101", $"{value}->{actual}", true, Monitor.StatusMessage);
            value = actual;

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(true);
            }
        }
    }


    private static MitsubishiMcSimulatorServerOptions CreateMcSimulatorOptions(string host, int port)
    {
        return new MitsubishiMcSimulatorServerOptions
        {
            Host = host,
            Port = port,
            EnableAutoWordResponse = true,
            AutoResponseTriggerDeviceCode = 0xA8,
            AutoResponseTriggerOffset = 100,
            AutoResponseDeviceCode = 0xA8,
            AutoResponseOffset = 101,
            AutoResponseIncrement = 1
        };
    }

    private static OmronFinsSimulatorServerOptions CreateFinsSimulatorOptions(string host, int port)
    {
        return new OmronFinsSimulatorServerOptions
        {
            Host = host,
            Port = port,
            EnableAutoWordResponse = true,
            AutoResponseTriggerOffset = 100,
            AutoResponseOffset = 101,
            AutoResponseIncrement = 1
        };
    }

    private bool IsAnyServerRunning()
    {
        return _simulatorServer is not null
            || _mcTcpSimulatorServer is not null
            || _mcUdpSimulatorServer is not null
            || _finsTcpSimulatorServer is not null
            || _finsUdpSimulatorServer is not null;
    }

    private static string NormalizeMode(string? modeText)
    {
        return string.IsNullOrWhiteSpace(modeText)
            ? "SIMULATORTCP"
            : modeText.Trim().ToUpperInvariant();
    }

    private void OnSimulatorServerStatusChanged(object? sender, string e)
    {
        ServerStatus = e;
        Monitor.StatusMessage = e;
    }
}
