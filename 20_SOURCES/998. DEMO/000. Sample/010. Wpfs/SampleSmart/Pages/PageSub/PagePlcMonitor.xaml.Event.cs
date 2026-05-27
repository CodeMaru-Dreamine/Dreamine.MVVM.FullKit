using Dreamine.PLC.Wpf.ViewModels;
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
        if (!TryGetPort(out var port))
        {
            return;
        }

        _ = _runtime.StartSimulatorServerAsync(Host, port);
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
