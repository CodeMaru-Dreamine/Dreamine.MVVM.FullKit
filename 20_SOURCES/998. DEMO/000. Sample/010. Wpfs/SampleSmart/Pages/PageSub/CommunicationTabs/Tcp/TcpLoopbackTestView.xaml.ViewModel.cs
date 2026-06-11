using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Loopback 테스트 ViewModel입니다.
/// </summary>
public partial class TcpLoopbackTestViewModel : ViewModelBase
{
    /// <summary>
    /// \brief TCP Loopback 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private TcpLoopbackTestViewEvent _event;

    /// <summary>
    /// \brief 선택 가능한 TCP Loopback 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackProtocols => Event.TcpLoopbackProtocols;

    /// <summary>
    /// \brief 선택 가능한 TCP Loopback 문자열 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackEncodings => Event.TcpLoopbackEncodings;

    /// <summary>
    /// \brief 선택된 TCP Loopback 프로토콜입니다.
    /// </summary>
    public string SelectedTcpLoopbackProtocol
    {
        get => Event.SelectedTcpLoopbackProtocol;
        set
        {
            if (Event.SelectedTcpLoopbackProtocol == value)
            {
                return;
            }

            Event.SelectedTcpLoopbackProtocol = value;
            OnPropertyChanged(nameof(SelectedTcpLoopbackProtocol));
        }
    }

    /// <summary>
    /// \brief 선택된 TCP Loopback 문자열 인코딩입니다.
    /// </summary>
    public string SelectedTcpLoopbackEncoding
    {
        get => Event.SelectedTcpLoopbackEncoding;
        set
        {
            if (Event.SelectedTcpLoopbackEncoding == value)
            {
                return;
            }

            Event.SelectedTcpLoopbackEncoding = value;
            OnPropertyChanged(nameof(SelectedTcpLoopbackEncoding));
        }
    }

    /// <summary>
    /// \brief TCP Loopback 송신 문자열입니다.
    /// </summary>
    public string TcpLoopbackSendText
    {
        get => Event.TcpLoopbackSendText;
        set => Event.TcpLoopbackSendText = value;
    }

    /// <summary>
    /// \brief TCP Loopback 시작 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StartLoopback")]
    private partial void StartLoopback();

    /// <summary>
    /// \brief TCP Loopback 종료 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StopLoopback")]
    private partial void StopLoopback();

    /// <summary>
    /// \brief TCP Client 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendClient")]
    private partial void SendClient();

    /// <summary>
    /// \brief TCP Server 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendServer")]
    private partial void SendServer();

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpLoopbackTestViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the TCP loopback test view.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public TcpLoopbackTestViewModel(TcpLoopbackTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
