using Dreamine.PLC.Abstractions.Clients;
using Dreamine.PLC.Abstractions.Connections;
using Dreamine.PLC.Core.Clients;
using Dreamine.PLC.Core.Devices;
using Dreamine.PLC.Core.Simulation;
using Dreamine.PLC.Mitsubishi.MC.Clients;
using Dreamine.PLC.Mitsubishi.MC.Options;
using Dreamine.PLC.Mitsubishi.MC.Simulation;
using Dreamine.PLC.Mitsubishi.MxComponent.Clients;
using Dreamine.PLC.Mitsubishi.MxComponent.Options;
using Dreamine.PLC.Omron.CxComponent.Clients;
using Dreamine.PLC.Omron.CxComponent.Options;
using Dreamine.PLC.Omron.Fins.Clients;
using Dreamine.PLC.Omron.Fins.Options;
using Dreamine.PLC.Omron.Fins.Simulation;
using Dreamine.PLC.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \if KO
/// <para>\brief PLC 샘플 전체에서 공유되는 Runtime 컨텍스트입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates plc sample runtime functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PlcSampleRuntime
{
    /// <summary>
    /// \if KO
    /// <para>in Memory Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the in memory client value.</para>
    /// \endif
    /// </summary>
    private readonly InMemoryPlcClient _inMemoryClient = new();
    /// <summary>
    /// \if KO
    /// <para>address Parser 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the address parser value.</para>
    /// \endif
    /// </summary>
    private readonly DefaultPlcAddressParser _addressParser = new();
    /// <summary>
    /// \if KO
    /// <para>active Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the active client value.</para>
    /// \endif
    /// </summary>
    private IPlcClient _activeClient;
    /// <summary>
    /// \if KO
    /// <para>simulator Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the simulator server value.</para>
    /// \endif
    /// </summary>
    private PlcSimulatorServer? _simulatorServer;
    /// <summary>
    /// \if KO
    /// <para>mc Tcp Simulator Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the mc tcp simulator server value.</para>
    /// \endif
    /// </summary>
    private MitsubishiMcTcpSimulatorServer? _mcTcpSimulatorServer;
    /// <summary>
    /// \if KO
    /// <para>mc Udp Simulator Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the mc udp simulator server value.</para>
    /// \endif
    /// </summary>
    private MitsubishiMcUdpSimulatorServer? _mcUdpSimulatorServer;
    /// <summary>
    /// \if KO
    /// <para>fins Tcp Simulator Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the fins tcp simulator server value.</para>
    /// \endif
    /// </summary>
    private OmronFinsTcpSimulatorServer? _finsTcpSimulatorServer;
    /// <summary>
    /// \if KO
    /// <para>fins Udp Simulator Server 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the fins udp simulator server value.</para>
    /// \endif
    /// </summary>
    private OmronFinsUdpSimulatorServer? _finsUdpSimulatorServer;
    /// <summary>
    /// \if KO
    /// <para>simulator Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the simulator client value.</para>
    /// \endif
    /// </summary>
    private PlcSimulatorTcpClient? _simulatorClient;
    /// <summary>
    /// \if KO
    /// <para>mitsubishi Mc Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the mitsubishi mc client value.</para>
    /// \endif
    /// </summary>
    private MitsubishiMcPlcClient? _mitsubishiMcClient;
    /// <summary>
    /// \if KO
    /// <para>mitsubishi Mx Component Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the mitsubishi mx component client value.</para>
    /// \endif
    /// </summary>
    private MitsubishiMxComponentPlcClient? _mitsubishiMxComponentClient;
    /// <summary>
    /// \if KO
    /// <para>omron Fins Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the omron fins client value.</para>
    /// \endif
    /// </summary>
    private OmronFinsPlcClient? _omronFinsClient;
    /// <summary>
    /// \if KO
    /// <para>omron Cx Component Client 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the omron cx component client value.</para>
    /// \endif
    /// </summary>
    private OmronCxComponentPlcClient? _omronCxComponentClient;
    /// <summary>
    /// \if KO
    /// <para>active Server Mode 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the active server mode value.</para>
    /// \endif
    /// </summary>
    private string _activeServerMode = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief PlcSampleRuntime 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PlcSampleRuntime"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public PlcSampleRuntime()
    {
        _activeClient = _inMemoryClient;
        Monitor = new PlcMonitorViewModel(_inMemoryClient, "InMemory PLC");
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Monitor ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the monitor value.</para>
    /// \endif
    /// </summary>
    public PlcMonitorViewModel Monitor { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버 상태 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the server status value.</para>
    /// \endif
    /// </summary>
    public string ServerStatus { get; private set; } = "Server stopped.";

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory PLC Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use in memory client operation.</para>
    /// \endif
    /// </summary>
    public void UseInMemoryClient()
    {
        _activeClient = _inMemoryClient;
        Monitor.SetClient(_inMemoryClient, "InMemory PLC");
        Monitor.StatusMessage = "InMemory PLC client selected.";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Simulator Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use simulator tcp client operation.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>서버 Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>서버 Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
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
    /// \if KO
    /// <para>\brief Mitsubishi MC Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use mitsubishi mc client operation.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>PLC Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>PLC Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <param name="transportText">
    /// \if KO
    /// <para>Transport 문자열입니다. Tcp 또는 Udp를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for transport text.</para>
    /// \endif
    /// </param>
    /// <param name="retryCount">
    /// \if KO
    /// <para>송수신 재시도 횟수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for retry count.</para>
    /// \endif
    /// </param>
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
    /// \if KO
    /// <para>\brief Omron FINS Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use omron fins client operation.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>PLC Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>PLC Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <param name="transportText">
    /// \if KO
    /// <para>Transport 문자열입니다. Tcp 또는 Udp를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for transport text.</para>
    /// \endif
    /// </param>
    /// <param name="retryCount">
    /// \if KO
    /// <para>송수신 재시도 횟수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for retry count.</para>
    /// \endif
    /// </param>
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
    /// \if KO
    /// <para>\brief Mitsubishi MX Component Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use mitsubishi mx component client operation.</para>
    /// \endif
    /// </summary>
    /// <param name="progId">
    /// \if KO
    /// <para>MX Component ProgID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for prog id.</para>
    /// \endif
    /// </param>
    /// <param name="logicalStationNumber">
    /// \if KO
    /// <para>MX Component logical station number입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for logical station number.</para>
    /// \endif
    /// </param>
    public void UseMitsubishiMxComponentClient(string progId, int logicalStationNumber)
    {
        _mitsubishiMxComponentClient = new MitsubishiMxComponentPlcClient(new MitsubishiMxComponentOptions
        {
            ProgId = string.IsNullOrWhiteSpace(progId) ? MitsubishiMxComponentOptions.DefaultProgId : progId.Trim(),
            LogicalStationNumber = logicalStationNumber
        });

        _activeClient = _mitsubishiMxComponentClient;
        Monitor.SetClient(_mitsubishiMxComponentClient, $"Mitsubishi MX Component LS={logicalStationNumber}");
        Monitor.StatusMessage = "Mitsubishi MX Component client selected. Vendor runtime must be installed before Connect.";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Omron CX-Compolet Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use omron cx component client operation.</para>
    /// \endif
    /// </summary>
    /// <param name="progId">
    /// \if KO
    /// <para>CX-Compolet ProgID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for prog id.</para>
    /// \endif
    /// </param>
    /// <param name="peerAddress">
    /// \if KO
    /// <para>PLC peer address입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for peer address.</para>
    /// \endif
    /// </param>
    public void UseOmronCxComponentClient(string progId, string peerAddress)
    {
        _omronCxComponentClient = new OmronCxComponentPlcClient(new OmronCxComponentOptions
        {
            ProgId = string.IsNullOrWhiteSpace(progId) ? "OMRON.Compolet.CJ2Compolet" : progId.Trim(),
            PeerAddress = string.IsNullOrWhiteSpace(peerAddress) ? "127.0.0.1" : peerAddress.Trim()
        });

        _activeClient = _omronCxComponentClient;
        Monitor.SetClient(_omronCxComponentClient, $"Omron CX-Compolet ({peerAddress})");
        Monitor.StatusMessage = "Omron CX-Compolet client selected. Vendor runtime must be installed before Connect.";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 모드에 맞는 PLC Protocol Simulator Server를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start protocol server async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="modeText">
    /// \if KO
    /// <para>서버 모드입니다. SimulatorTcp, McTcp, McUdp를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for mode text.</para>
    /// \endif
    /// </param>
    /// <param name="host">
    /// \if KO
    /// <para>Bind Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>Bind Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Protocol Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start protocol server async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 실행 중인 PLC Protocol Simulator Server를 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop protocol server async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop Protocol Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop protocol server async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 이전 호환성을 위해 SimulatorTcp 서버를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start simulator server async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>Bind Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>Bind Port입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Simulator Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start simulator server async operation.</para>
    /// \endif
    /// </returns>
    public Task StartSimulatorServerAsync(string host, int port)
    {
        return StartProtocolServerAsync("SimulatorTcp", host, port);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 이전 호환성을 위해 실행 중인 서버를 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop simulator server async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Stop Simulator Server Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop simulator server async operation.</para>
    /// \endif
    /// </returns>
    public Task StopSimulatorServerAsync()
    {
        return StopProtocolServerAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>Run Handshake Test Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>\brief Runs a client-server auto-response handshake test using D100 as client write and D101 as server response.</para>
    /// \endif
    /// </summary>
    /// <param name="startValue">
    /// \if KO
    /// <para>start Value에 사용할 <c>short</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The first client value.</para>
    /// \endif
    /// </param>
    /// <param name="iterations">
    /// \if KO
    /// <para>iterations에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The number of handshake iterations.</para>
    /// \endif
    /// </param>
    /// <param name="delayMs">
    /// \if KO
    /// <para>delay Ms에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The delay between iterations in milliseconds.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The cancellation token.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Run Handshake Test Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A task that represents the asynchronous operation.</para>
    /// \endif
    /// </returns>
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


    /// <summary>
    /// \if KO
    /// <para>Mc Simulator Options 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the mc simulator options value.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>host에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>port에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Mc Simulator Options 작업에서 생성한 <c>MitsubishiMcSimulatorServerOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MitsubishiMcSimulatorServerOptions</c> result produced by the create mc simulator options operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Fins Simulator Options 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the fins simulator options value.</para>
    /// \endif
    /// </summary>
    /// <param name="host">
    /// \if KO
    /// <para>host에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for host.</para>
    /// \endif
    /// </param>
    /// <param name="port">
    /// \if KO
    /// <para>port에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for port.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Fins Simulator Options 작업에서 생성한 <c>OmronFinsSimulatorServerOptions</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>OmronFinsSimulatorServerOptions</c> result produced by the create fins simulator options operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Is Any Server Running 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is any server running.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Is Any Server Running 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is any server running condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool IsAnyServerRunning()
    {
        return _simulatorServer is not null
            || _mcTcpSimulatorServer is not null
            || _mcUdpSimulatorServer is not null
            || _finsTcpSimulatorServer is not null
            || _finsUdpSimulatorServer is not null;
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Mode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize mode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="modeText">
    /// \if KO
    /// <para>mode Text에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for mode text.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Mode 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize mode operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeMode(string? modeText)
    {
        return string.IsNullOrWhiteSpace(modeText)
            ? "SIMULATORTCP"
            : modeText.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// \if KO
    /// <para>Simulator Server Status Changed 이벤트 또는 상태 변경을 처리합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Handles the simulator server status changed event or state change.</para>
    /// \endif
    /// </summary>
    /// <param name="sender">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    private void OnSimulatorServerStatusChanged(object? sender, string e)
    {
        ServerStatus = e;
        Monitor.StatusMessage = e;
    }
}
