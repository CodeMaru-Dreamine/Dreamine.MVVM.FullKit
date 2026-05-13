using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Client 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class TcpClientTestViewEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief TcpClientTestViewEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 런타임입니다.</param>
    public TcpClientTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedTcpClientProtocol = "RawAvailable";
        TcpClientSendText = "test";
    }

    /// <summary>
    /// \brief 선택 가능한 TCP Client 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpClientProtocols => _runtime.TcpProtocols;

    /// <summary>
    /// \brief 선택된 TCP Client 프로토콜입니다.
    /// </summary>
    public string SelectedTcpClientProtocol { get; set; }

    /// <summary>
    /// \brief TCP Client 송신 문자열입니다.
    /// </summary>
    public string TcpClientSendText { get; set; }

    /// <summary>
    /// \brief TCP Client를 연결합니다.
    /// </summary>
    public void ConnectClient()
    {
        _ = _runtime.ConnectTcpClientAsync(SelectedTcpClientProtocol);
    }

    /// <summary>
    /// \brief TCP Client 연결을 해제합니다.
    /// </summary>
    public void DisconnectClient()
    {
        _ = _runtime.DisconnectTcpClientAsync();
    }

    /// <summary>
    /// \brief TCP Client에서 문자열을 송신합니다.
    /// </summary>
    public void SendClient()
    {
        _ = _runtime.SendTcpClientAsync(
            SelectedTcpClientProtocol,
            TcpClientSendText);
    }
}