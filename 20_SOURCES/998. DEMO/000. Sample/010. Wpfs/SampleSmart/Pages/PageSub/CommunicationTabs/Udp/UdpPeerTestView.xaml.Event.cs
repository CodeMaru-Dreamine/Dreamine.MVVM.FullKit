using Dreamine.Communication.Core.Protocols;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Udp;

/// <summary>
/// \if KO
/// <para>\brief UDP Peer Loopback 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates udp peer test view event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class UdpPeerTestViewEvent
{
    /// <summary>
    /// \if KO
    /// <para>runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime value.</para>
    /// \endif
    /// </summary>
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief UdpPeerTestViewEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="UdpPeerTestViewEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>Communication 샘플 공유 Runtime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CommunicationSampleRuntime</c> value used for runtime.</para>
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
    public UdpPeerTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedUdpProtocol = "PlainText";
        SelectedUdpEncoding = PlainTextProtocolOptions.Utf8EncodingName;
        UdpSendText = "Hello from Dreamine UDP Peer";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 UDP 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> UdpProtocols => _runtime.UdpProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 UDP 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected udp protocol value.</para>
    /// \endif
    /// </summary>
    public string SelectedUdpProtocol { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 UDP PlainText 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp text encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> UdpTextEncodings => _runtime.UdpTextEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 UDP PlainText 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected udp encoding value.</para>
    /// \endif
    /// </summary>
    public string SelectedUdpEncoding { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the udp send text value.</para>
    /// \endif
    /// </summary>
    public string UdpSendText { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer 엔드포인트 요약입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp endpoint summary value.</para>
    /// \endif
    /// </summary>
    public string UdpEndpointSummary => "Peer A: 127.0.0.1:16001 -> 127.0.0.1:16002 / Peer B: 127.0.0.1:16002 -> 127.0.0.1:16001";

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A와 Peer B를 모두 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start udp loopback operation.</para>
    /// \endif
    /// </summary>
    public void StartUdpLoopback()
    {
        _ = _runtime.StartUdpLoopbackAsync(
            SelectedUdpProtocol,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A와 Peer B를 모두 종료합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop udp loopback operation.</para>
    /// \endif
    /// </summary>
    public void StopUdpLoopback()
    {
        _ = _runtime.StopAllUdpAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A를 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect udp peer a operation.</para>
    /// \endif
    /// </summary>
    public void ConnectUdpPeerA()
    {
        _ = _runtime.ConnectUdpPeerAAsync(
            SelectedUdpProtocol,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer B를 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect udp peer b operation.</para>
    /// \endif
    /// </summary>
    public void ConnectUdpPeerB()
    {
        _ = _runtime.ConnectUdpPeerBAsync(
            SelectedUdpProtocol,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect udp peer a operation.</para>
    /// \endif
    /// </summary>
    public void DisconnectUdpPeerA()
    {
        _ = _runtime.DisconnectUdpPeerAAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer B 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect udp peer b operation.</para>
    /// \endif
    /// </summary>
    public void DisconnectUdpPeerB()
    {
        _ = _runtime.DisconnectUdpPeerBAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A에서 Peer B로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send udp peer a operation.</para>
    /// \endif
    /// </summary>
    public void SendUdpPeerA()
    {
        _ = _runtime.SendUdpPeerAAsync(
            SelectedUdpProtocol,
            UdpSendText,
            SelectedUdpEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer B에서 Peer A로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send udp peer b operation.</para>
    /// \endif
    /// </summary>
    public void SendUdpPeerB()
    {
        _ = _runtime.SendUdpPeerBAsync(
            SelectedUdpProtocol,
            UdpSendText,
            SelectedUdpEncoding);
    }
}
