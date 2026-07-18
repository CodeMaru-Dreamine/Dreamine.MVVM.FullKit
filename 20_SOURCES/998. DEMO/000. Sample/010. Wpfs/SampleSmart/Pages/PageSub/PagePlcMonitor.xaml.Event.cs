using Dreamine.PLC.Wpf.ViewModels;
using Dreamine.PLC.Mitsubishi.MxComponent.Options;
using SampleSmart.Pages.PageSub.PlcTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \if KO
/// <para>\brief Dreamine PLC 모니터 샘플 페이지 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates page plc monitor event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PagePlcMonitorEvent
{
    /// <summary>
    /// \if KO
    /// <para>runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime value.</para>
    /// \endif
    /// </summary>
    private readonly PlcSampleRuntime _runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief PagePlcMonitorEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PagePlcMonitorEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>PLC 샘플 실행 컨텍스트입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PlcSampleRuntime</c> value used for runtime.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public PagePlcMonitorEvent(PlcSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Monitor ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the monitor value.</para>
    /// \endif
    /// </summary>
    public PlcMonitorViewModel Monitor => _runtime.Monitor;

    /// <summary>
    /// \if KO
    /// <para>\brief Simulator Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// \if KO
    /// <para>\brief Simulator Port 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the port text value.</para>
    /// \endif
    /// </summary>
    public string PortText { get; set; } = "55000";


    /// <summary>
    /// \if KO
    /// <para>\brief PLC Client 선택 모드 문자열입니다. SimulatorTcp, McTcp, McUdp, FinsTcp, FinsUdp, MxComponent, CxComponent를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the client mode text value.</para>
    /// \endif
    /// </summary>
    public string ClientModeText { get; set; } = "SimulatorTcp";

    /// <summary>
    /// \if KO
    /// <para>\brief MX Component ProgID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mx prog id value.</para>
    /// \endif
    /// </summary>
    public string MxProgId { get; set; } = MitsubishiMxComponentOptions.DefaultProgId;

    /// <summary>
    /// \if KO
    /// <para>\brief MX Component logical station number 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mx logical station number text value.</para>
    /// \endif
    /// </summary>
    public string MxLogicalStationNumberText { get; set; } = "0";

    /// <summary>
    /// \if KO
    /// <para>\brief CX-Compolet ProgID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cx prog id value.</para>
    /// \endif
    /// </summary>
    public string CxProgId { get; set; } = "OMRON.Compolet.CJ2Compolet";

    /// <summary>
    /// \if KO
    /// <para>\brief CX-Compolet peer address입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cx peer address value.</para>
    /// \endif
    /// </summary>
    public string CxPeerAddress { get; set; } = "127.0.0.1";


    /// <summary>
    /// \if KO
    /// <para>\brief Mitsubishi MC Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc host value.</para>
    /// \endif
    /// </summary>
    public string McHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// \if KO
    /// <para>\brief Mitsubishi MC Port 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc port text value.</para>
    /// \endif
    /// </summary>
    public string McPortText { get; set; } = "5000";

    /// <summary>
    /// \if KO
    /// <para>\brief Mitsubishi MC Transport 문자열입니다. Tcp 또는 Udp를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc transport text value.</para>
    /// \endif
    /// </summary>
    public string McTransportText { get; set; } = "Tcp";

    /// <summary>
    /// \if KO
    /// <para>\brief Mitsubishi MC Retry Count 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc retry count text value.</para>
    /// \endif
    /// </summary>
    public string McRetryCountText { get; set; } = "1";

    /// <summary>
    /// \if KO
    /// <para>\brief Handshake 시작 값 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake start value text value.</para>
    /// \endif
    /// </summary>
    public string HandshakeStartValueText { get; set; } = "1000";

    /// <summary>
    /// \if KO
    /// <para>\brief Handshake 반복 횟수 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake iterations text value.</para>
    /// \endif
    /// </summary>
    public string HandshakeIterationsText { get; set; } = "100";

    /// <summary>
    /// \if KO
    /// <para>\brief Handshake 반복 간격 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake delay ms text value.</para>
    /// \endif
    /// </summary>
    public string HandshakeDelayMsText { get; set; } = "10";

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory PLC Client를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use in memory operation.</para>
    /// \endif
    /// </summary>
    public void UseInMemory()
    {
        _runtime.UseInMemoryClient();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버를 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start server operation.</para>
    /// \endif
    /// </summary>
    public void StartServer()
    {
        var mode = string.IsNullOrWhiteSpace(ClientModeText) ? "SimulatorTcp" : ClientModeText.Trim();
        if (mode.Equals("MxComponent", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("CxComponent", StringComparison.OrdinalIgnoreCase))
        {
            _runtime.Monitor.StatusMessage = "MxComponent/CxComponent modes use vendor runtime directly. Press Use Client, then Connect.";
            return;
        }

        if (!TryGetPort(out var port))
        {
            return;
        }

        _ = _runtime.StartProtocolServerAsync(ClientModeText, Host, port);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버를 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop server operation.</para>
    /// \endif
    /// </summary>
    public void StopServer()
    {
        _ = _runtime.StopProtocolServerAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 PLC Client를 모니터에 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use selected client operation.</para>
    /// \endif
    /// </summary>
    public void UseSelectedClient()
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        var mode = string.IsNullOrWhiteSpace(ClientModeText) ? "SimulatorTcp" : ClientModeText.Trim();
        switch (mode.ToUpperInvariant())
        {
            case "SIMULATORTCP":
                _runtime.UseSimulatorTcpClient(Host, port);
                return;
            case "MCTCP":
                UseMitsubishiMcClient(Host, port, "Tcp");
                return;
            case "MCUDP":
                UseMitsubishiMcClient(Host, port, "Udp");
                return;
            case "FINSTCP":
                UseOmronFinsClient(Host, port, "Tcp");
                return;
            case "FINSUDP":
                UseOmronFinsClient(Host, port, "Udp");
                return;
            case "MXCOMPONENT":
                UseMitsubishiMxComponentClient();
                return;
            case "CXCOMPONENT":
                UseOmronCxComponentClient();
                return;
            default:
                _runtime.Monitor.StatusMessage = "Client mode must be SimulatorTcp, McTcp, McUdp, FinsTcp, FinsUdp, MxComponent, or CxComponent.";
                return;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Use Mitsubishi Mx Component Client 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use mitsubishi mx component client operation.</para>
    /// \endif
    /// </summary>
    private void UseMitsubishiMxComponentClient()
    {
        if (!int.TryParse(MxLogicalStationNumberText, out var logicalStationNumber) || logicalStationNumber < 0)
        {
            _runtime.Monitor.StatusMessage = "MX logical station number must be zero or greater.";
            return;
        }

        var progId = string.IsNullOrWhiteSpace(MxProgId)
            ? MitsubishiMxComponentOptions.DefaultProgId
            : MxProgId;

        _runtime.UseMitsubishiMxComponentClient(progId, logicalStationNumber);
    }

    /// <summary>
    /// \if KO
    /// <para>Use Omron Cx Component Client 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use omron cx component client operation.</para>
    /// \endif
    /// </summary>
    private void UseOmronCxComponentClient()
    {
        _runtime.UseOmronCxComponentClient(CxProgId, CxPeerAddress);
    }


    /// <summary>
    /// \if KO
    /// <para>Use Omron Fins Client 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use omron fins client operation.</para>
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
    /// <param name="transportText">
    /// \if KO
    /// <para>transport Text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for transport text.</para>
    /// \endif
    /// </param>
    private void UseOmronFinsClient(string host, int port, string transportText)
    {
        if (!int.TryParse(McRetryCountText, out var retryCount) || retryCount <= 0)
        {
            _runtime.Monitor.StatusMessage = "FINS retry count must be greater than zero.";
            return;
        }

        _runtime.UseOmronFinsClient(host, port, transportText, retryCount);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator TCP Client를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use tcp client operation.</para>
    /// \endif
    /// </summary>
    public void UseTcpClient()
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        _runtime.UseSimulatorTcpClient(Host, port);
    }


    /// <summary>
    /// \if KO
    /// <para>Use Mitsubishi Mc Client 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use mitsubishi mc client operation.</para>
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
    /// <param name="transportText">
    /// \if KO
    /// <para>transport Text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for transport text.</para>
    /// \endif
    /// </param>
    private void UseMitsubishiMcClient(string host, int port, string transportText)
    {
        if (!int.TryParse(McRetryCountText, out var retryCount) || retryCount <= 0)
        {
            _runtime.Monitor.StatusMessage = "MC retry count must be greater than zero.";
            return;
        }

        _runtime.UseMitsubishiMcClient(host, port, transportText, retryCount);
    }


    /// <summary>
    /// \if KO
    /// <para>\brief Mitsubishi MC Client를 선택합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use mc client operation.</para>
    /// \endif
    /// </summary>
    public void UseMcClient()
    {
        if (!int.TryParse(McPortText, out var port) || port is <= 0 or > 65535)
        {
            _runtime.Monitor.StatusMessage = "MC port must be between 1 and 65535.";
            return;
        }

        if (!int.TryParse(McRetryCountText, out var retryCount) || retryCount <= 0)
        {
            _runtime.Monitor.StatusMessage = "MC retry count must be greater than zero.";
            return;
        }

        _runtime.UseMitsubishiMcClient(McHost, port, McTransportText, retryCount);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief D100/D101 기반 자동 응답 Handshake 테스트를 실행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run handshake test operation.</para>
    /// \endif
    /// </summary>
    public void RunHandshakeTest()
    {
        if (!short.TryParse(HandshakeStartValueText, out var startValue))
        {
            _runtime.Monitor.StatusMessage = "Handshake start value must be a 16-bit integer.";
            return;
        }

        if (!int.TryParse(HandshakeIterationsText, out var iterations) || iterations <= 0)
        {
            _runtime.Monitor.StatusMessage = "Handshake iterations must be greater than zero.";
            return;
        }

        if (!int.TryParse(HandshakeDelayMsText, out var delayMs) || delayMs < 0)
        {
            _runtime.Monitor.StatusMessage = "Handshake delay must be zero or greater.";
            return;
        }

        _ = _runtime.RunHandshakeTestAsync(startValue, iterations, delayMs);
    }

    /// <summary>
    /// \if KO
    /// <para>Get Port 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to get port and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
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
    /// <para>Try Get Port 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the try get port condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool TryGetPort(out int port)
    {
        if (!int.TryParse(PortText, out port) || port is <= 0 or > 65535)
        {
            _runtime.Monitor.StatusMessage = "Port must be between 1 and 65535.";
            return false;
        }

        return true;
    }
}
