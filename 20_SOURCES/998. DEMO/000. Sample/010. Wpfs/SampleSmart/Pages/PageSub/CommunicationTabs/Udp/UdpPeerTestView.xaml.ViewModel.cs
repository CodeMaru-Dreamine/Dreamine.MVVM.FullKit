using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Udp;

/// <summary>
/// \if KO
/// <para>\brief UDP Peer Loopback 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates udp peer test view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class UdpPeerTestViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer Loopback 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private UdpPeerTestViewEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 UDP 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> UdpProtocols => Event.UdpProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 UDP 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected udp protocol value.</para>
    /// \endif
    /// </summary>
    public string SelectedUdpProtocol
    {
        get => Event.SelectedUdpProtocol;
        set
        {
            if (Event.SelectedUdpProtocol == value)
            {
                return;
            }

            Event.SelectedUdpProtocol = value;
            OnPropertyChanged(nameof(SelectedUdpProtocol));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 UDP PlainText 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp text encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> UdpTextEncodings => Event.UdpTextEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 UDP PlainText 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected udp encoding value.</para>
    /// \endif
    /// </summary>
    public string SelectedUdpEncoding
    {
        get => Event.SelectedUdpEncoding;
        set
        {
            if (Event.SelectedUdpEncoding == value)
            {
                return;
            }

            Event.SelectedUdpEncoding = value;
            OnPropertyChanged(nameof(SelectedUdpEncoding));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the udp send text value.</para>
    /// \endif
    /// </summary>
    public string UdpSendText
    {
        get => Event.UdpSendText;
        set
        {
            if (Event.UdpSendText == value)
            {
                return;
            }

            Event.UdpSendText = value;
            OnPropertyChanged(nameof(UdpSendText));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer 엔드포인트 요약입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the udp endpoint summary value.</para>
    /// \endif
    /// </summary>
    public string UdpEndpointSummary => Event.UdpEndpointSummary;

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A와 Peer B를 모두 시작하는 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start udp loopback operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StartUdpLoopback")]
    private partial void StartUdpLoopback();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A와 Peer B를 모두 종료하는 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop udp loopback operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StopUdpLoopback")]
    private partial void StopUdpLoopback();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect udp peer a operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ConnectUdpPeerA")]
    private partial void ConnectUdpPeerA();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer B 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect udp peer b operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ConnectUdpPeerB")]
    private partial void ConnectUdpPeerB();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect udp peer a operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.DisconnectUdpPeerA")]
    private partial void DisconnectUdpPeerA();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer B 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect udp peer b operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.DisconnectUdpPeerB")]
    private partial void DisconnectUdpPeerB();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer A에서 Peer B로 메시지를 송신하는 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send udp peer a operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SendUdpPeerA")]
    private partial void SendUdpPeerA();

    /// <summary>
    /// \if KO
    /// <para>\brief UDP Peer B에서 Peer A로 메시지를 송신하는 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send udp peer b operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SendUdpPeerB")]
    private partial void SendUdpPeerB();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="UdpPeerTestViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="UdpPeerTestViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>UdpPeerTestViewEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the UDP peer test view.</para>
    /// \endif
    /// </param>
    public UdpPeerTestViewModel(UdpPeerTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
