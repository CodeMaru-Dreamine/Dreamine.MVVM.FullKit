using Dreamine.Communication.Core.Protocols;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Udp;

/// <summary>
/// \brief UDP Peer Loopback 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class UdpPeerTestViewEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief UdpPeerTestViewEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public UdpPeerTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedUdpProtocol = "PlainText";
        SelectedUdpEncoding = PlainTextProtocolOptions.Utf8EncodingName;
        UdpSendText = "Hello from Dreamine UDP Peer";
    }

    /// <summary>
    /// \brief 선택 가능한 UDP 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> UdpProtocols => _runtime.UdpProtocols;

    /// <summary>
    /// \brief 선택된 UDP 프로토콜입니다.
    /// </summary>
    public string SelectedUdpProtocol { get; set; }

    /// <summary>
    /// \brief 선택 가능한 UDP PlainText 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> UdpTextEncodings => _runtime.UdpTextEncodings;

    /// <summary>
    /// \brief 선택된 UDP PlainText 인코딩입니다.
    /// </summary>
    public string SelectedUdpEncoding { get; set; }

    /// <summary>
    /// \brief UDP 송신 문자열입니다.
    /// </summary>
    public string UdpSendText { get; set; }

    /// <summary>
    /// \brief UDP Peer 엔드포인트 요약입니다.
    /// </summary>
    public string UdpEndpointSummary => "Peer A: 127.0.0.1:16001 -> 127.0.0.1:16002 / Peer B: 127.0.0.1:16002 -> 127.0.0.1:16001";

    /// <summary>
    /// \brief UDP Peer A와 Peer B를 모두 시작합니다.
    /// </summary>
    public void StartUdpLoopback()
    {
        _ = _runtime.StartUdpLoopbackAsync(
            SelectedUdpProtocol,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \brief UDP Peer A와 Peer B를 모두 종료합니다.
    /// </summary>
    public void StopUdpLoopback()
    {
        _ = _runtime.StopAllUdpAsync();
    }

    /// <summary>
    /// \brief UDP Peer A를 연결합니다.
    /// </summary>
    public void ConnectUdpPeerA()
    {
        _ = _runtime.ConnectUdpPeerAAsync(
            SelectedUdpProtocol,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \brief UDP Peer B를 연결합니다.
    /// </summary>
    public void ConnectUdpPeerB()
    {
        _ = _runtime.ConnectUdpPeerBAsync(
            SelectedUdpProtocol,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \brief UDP Peer A 연결을 해제합니다.
    /// </summary>
    public void DisconnectUdpPeerA()
    {
        _ = _runtime.DisconnectUdpPeerAAsync();
    }

    /// <summary>
    /// \brief UDP Peer B 연결을 해제합니다.
    /// </summary>
    public void DisconnectUdpPeerB()
    {
        _ = _runtime.DisconnectUdpPeerBAsync();
    }

    /// <summary>
    /// \brief UDP Peer A에서 Peer B로 메시지를 송신합니다.
    /// </summary>
    public void SendUdpPeerA()
    {
        _ = _runtime.SendUdpPeerAAsync(
            SelectedUdpProtocol,
            UdpSendText,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \brief UDP Peer B에서 Peer A로 메시지를 송신합니다.
    /// </summary>
    public void SendUdpPeerB()
    {
        _ = _runtime.SendUdpPeerBAsync(
            SelectedUdpProtocol,
            UdpSendText,
            SelectedUdpEncoding);
    }
}
