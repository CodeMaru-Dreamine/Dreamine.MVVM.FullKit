namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \brief PLC Simulator TCP 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class PlcSimulatorTabEvent
{
    private readonly PlcSampleRuntime _runtime;

    /// <summary>
    /// \brief PlcSimulatorTabEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">PLC 샘플 공유 런타임입니다.</param>
    public PlcSimulatorTabEvent(PlcSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    /// <summary>
    /// \brief Simulator Host입니다.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// \brief Simulator Port 문자열입니다.
    /// </summary>
    public string PortText { get; set; } = "55000";

    /// <summary>
    /// \brief PLC Simulator 서버를 시작합니다.
    /// </summary>
    public void StartServer()
    {
        if (!TryGetPort(out var port))
        {
            return;
        }

        var bindHost = Host == "127.0.0.1" ? "0.0.0.0" : Host;
        _ = _runtime.StartSimulatorServerAsync(bindHost, port);
    }

    /// <summary>
    /// \brief PLC Simulator 서버를 중지합니다.
    /// </summary>
    public void StopServer()
    {
        _ = _runtime.StopSimulatorServerAsync();
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
