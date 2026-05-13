namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Loopback 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class TcpLoopbackTestViewEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief TcpLoopbackTestViewEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public TcpLoopbackTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedTcpLoopbackProtocol = "PlainText";
        TcpLoopbackSendText = "Hello from Dreamine TCP Loopback";
    }

    /// <summary>
    /// \brief 선택 가능한 TCP Loopback 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackProtocols => _runtime.TcpProtocols;

    /// <summary>
    /// \brief 선택된 TCP Loopback 프로토콜입니다.
    /// </summary>
    public string SelectedTcpLoopbackProtocol { get; set; }

    /// <summary>
    /// \brief TCP Loopback 송신 문자열입니다.
    /// </summary>
    public string TcpLoopbackSendText { get; set; }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Server와 TCP Client를 모두 시작합니다.
    /// </summary>
    public void StartLoopback()
    {
        _ = StartLoopbackAsync();
    }

    /// <summary>
    /// \brief TCP Server와 TCP Client를 모두 종료합니다.
    /// </summary>
    public void StopLoopback()
    {
        _ = _runtime.StopAllTcpAsync();
    }

    /// <summary>
    /// \brief TCP Client에서 Server로 메시지를 송신합니다.
    /// </summary>
    public void SendClient()
    {
        _ = _runtime.SendTcpClientAsync(
            SelectedTcpLoopbackProtocol,
            TcpLoopbackSendText);
    }

    /// <summary>
    /// \brief TCP Server에서 Client로 메시지를 송신합니다.
    /// </summary>
    public void SendServer()
    {
        _ = _runtime.SendTcpServerAsync(
            SelectedTcpLoopbackProtocol,
            TcpLoopbackSendText);
    }

    private async Task StartLoopbackAsync()
    {
        await _runtime.StartTcpServerAsync(SelectedTcpLoopbackProtocol);
        await _runtime.ConnectTcpClientAsync(SelectedTcpLoopbackProtocol);
    }
}