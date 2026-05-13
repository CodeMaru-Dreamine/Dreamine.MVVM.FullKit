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
    /// \brief 선택된 TCP Loopback 프로토콜입니다.
    /// </summary>
    public string SelectedTcpLoopbackProtocol
    {
        get => Event.SelectedTcpLoopbackProtocol;
        set => Event.SelectedTcpLoopbackProtocol = value;
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
}