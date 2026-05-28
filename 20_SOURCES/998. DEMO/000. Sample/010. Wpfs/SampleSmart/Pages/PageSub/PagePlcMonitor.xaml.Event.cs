using Dreamine.PLC.Wpf.ViewModels;
using Dreamine.PLC.Mitsubishi.MxComponent.Options;
using SampleSmart.Pages.PageSub.PlcTabs;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine PLC 모니터 샘플 페이지 이벤트 처리 클래스입니다.
/// </summary>
public sealed class PagePlcMonitorEvent
{
    private readonly PlcSampleRuntime _runtime;

    /// <summary>
    /// \brief PagePlcMonitorEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">PLC 샘플 실행 컨텍스트입니다.</param>
    public PagePlcMonitorEvent(PlcSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \brief PLC Monitor ViewModel입니다.
    /// </summary>
    public PlcMonitorViewModel Monitor => _runtime.Monitor;

    /// <summary>
    /// \brief Simulator Host입니다.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// \brief Simulator Port 문자열입니다.
    /// </summary>
    public string PortText { get; set; } = "55000";


    /// <summary>
    /// \brief PLC Client 선택 모드 문자열입니다. SimulatorTcp, McTcp, McUdp, FinsTcp, FinsUdp, MxComponent, CxComponent를 사용합니다.
    /// </summary>
    public string ClientModeText { get; set; } = "SimulatorTcp";

    /// <summary>
    /// \brief MX Component ProgID입니다.
    /// </summary>
    public string MxProgId { get; set; } = MitsubishiMxComponentOptions.DefaultProgId;

    /// <summary>
    /// \brief MX Component logical station number 문자열입니다.
    /// </summary>
    public string MxLogicalStationNumberText { get; set; } = "0";

    /// <summary>
    /// \brief CX-Compolet ProgID입니다.
    /// </summary>
    public string CxProgId { get; set; } = "OMRON.Compolet.CJ2Compolet";

    /// <summary>
    /// \brief CX-Compolet peer address입니다.
    /// </summary>
    public string CxPeerAddress { get; set; } = "127.0.0.1";


    /// <summary>
    /// \brief Mitsubishi MC Host입니다.
    /// </summary>
    public string McHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// \brief Mitsubishi MC Port 문자열입니다.
    /// </summary>
    public string McPortText { get; set; } = "5000";

    /// <summary>
    /// \brief Mitsubishi MC Transport 문자열입니다. Tcp 또는 Udp를 사용합니다.
    /// </summary>
    public string McTransportText { get; set; } = "Tcp";

    /// <summary>
    /// \brief Mitsubishi MC Retry Count 문자열입니다.
    /// </summary>
    public string McRetryCountText { get; set; } = "1";

    /// <summary>
    /// \brief Handshake 시작 값 문자열입니다.
    /// </summary>
    public string HandshakeStartValueText { get; set; } = "1000";

    /// <summary>
    /// \brief Handshake 반복 횟수 문자열입니다.
    /// </summary>
    public string HandshakeIterationsText { get; set; } = "100";

    /// <summary>
    /// \brief Handshake 반복 간격 문자열입니다.
    /// </summary>
    public string HandshakeDelayMsText { get; set; } = "10";

    /// <summary>
    /// \brief InMemory PLC Client를 선택합니다.
    /// </summary>
    public void UseInMemory()
    {
        _runtime.UseInMemoryClient();
    }

    /// <summary>
    /// \brief PLC Simulator 서버를 시작합니다.
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
    /// \brief PLC Simulator 서버를 중지합니다.
    /// </summary>
    public void StopServer()
    {
        _ = _runtime.StopProtocolServerAsync();
    }

    /// <summary>
    /// \brief 선택된 PLC Client를 모니터에 연결합니다.
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

    private void UseOmronCxComponentClient()
    {
        _runtime.UseOmronCxComponentClient(CxProgId, CxPeerAddress);
    }


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
    /// \brief PLC Simulator TCP Client를 선택합니다.
    /// </summary>
    public void UseTcpClient()
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        _runtime.UseSimulatorTcpClient(Host, port);
    }


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
    /// \brief Mitsubishi MC Client를 선택합니다.
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
    /// \brief D100/D101 기반 자동 응답 Handshake 테스트를 실행합니다.
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
