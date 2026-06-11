using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Udp;

/// <summary>
/// \brief UDP Peer Loopback 테스트 ViewModel입니다.
/// </summary>
public partial class UdpPeerTestViewModel : ViewModelBase
{
    /// <summary>
    /// \brief UDP Peer Loopback 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private UdpPeerTestViewEvent _event;

    /// <summary>
    /// \brief 선택 가능한 UDP 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> UdpProtocols => Event.UdpProtocols;

    /// <summary>
    /// \brief 선택된 UDP 프로토콜입니다.
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
    /// \brief 선택 가능한 UDP PlainText 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> UdpTextEncodings => Event.UdpTextEncodings;

    /// <summary>
    /// \brief 선택된 UDP PlainText 인코딩입니다.
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
    /// \brief UDP 송신 문자열입니다.
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
    /// \brief UDP Peer 엔드포인트 요약입니다.
    /// </summary>
    public string UdpEndpointSummary => Event.UdpEndpointSummary;

    /// <summary>
    /// \brief UDP Peer A와 Peer B를 모두 시작하는 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StartUdpLoopback")]
    private partial void StartUdpLoopback();

    /// <summary>
    /// \brief UDP Peer A와 Peer B를 모두 종료하는 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StopUdpLoopback")]
    private partial void StopUdpLoopback();

    /// <summary>
    /// \brief UDP Peer A 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ConnectUdpPeerA")]
    private partial void ConnectUdpPeerA();

    /// <summary>
    /// \brief UDP Peer B 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ConnectUdpPeerB")]
    private partial void ConnectUdpPeerB();

    /// <summary>
    /// \brief UDP Peer A 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.DisconnectUdpPeerA")]
    private partial void DisconnectUdpPeerA();

    /// <summary>
    /// \brief UDP Peer B 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.DisconnectUdpPeerB")]
    private partial void DisconnectUdpPeerB();

    /// <summary>
    /// \brief UDP Peer A에서 Peer B로 메시지를 송신하는 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendUdpPeerA")]
    private partial void SendUdpPeerA();

    /// <summary>
    /// \brief UDP Peer B에서 Peer A로 메시지를 송신하는 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendUdpPeerB")]
    private partial void SendUdpPeerB();

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpPeerTestViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the UDP peer test view.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public UdpPeerTestViewModel(UdpPeerTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
