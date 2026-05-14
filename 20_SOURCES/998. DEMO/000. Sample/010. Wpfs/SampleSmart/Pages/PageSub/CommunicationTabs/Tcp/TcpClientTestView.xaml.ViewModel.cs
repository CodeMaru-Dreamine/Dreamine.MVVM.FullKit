using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Client 테스트 ViewModel입니다.
/// </summary>
public partial class TcpClientTestViewModel : ViewModelBase
{
    /// <summary>
    /// \brief TCP Client 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private TcpClientTestViewEvent _event;

    /// <summary>
    /// \brief 선택 가능한 TCP Client 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpClientProtocols => Event.TcpClientProtocols;

    /// <summary>
    /// \brief 선택 가능한 TCP Client 문자열 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpClientEncodings => Event.TcpClientEncodings;

    /// <summary>
    /// \brief 선택된 TCP Client 프로토콜입니다.
    /// </summary>
    public string SelectedTcpClientProtocol
    {
        get => Event.SelectedTcpClientProtocol;
        set
        {
            if (Event.SelectedTcpClientProtocol == value)
            {
                return;
            }

            Event.SelectedTcpClientProtocol = value;
            OnPropertyChanged(nameof(SelectedTcpClientProtocol));
        }
    }

    /// <summary>
    /// \brief 선택된 TCP Client 문자열 인코딩입니다.
    /// </summary>
    public string SelectedTcpClientEncoding
    {
        get => Event.SelectedTcpClientEncoding;
        set
        {
            if (Event.SelectedTcpClientEncoding == value)
            {
                return;
            }

            Event.SelectedTcpClientEncoding = value;
            OnPropertyChanged(nameof(SelectedTcpClientEncoding));
        }
    }

    /// <summary>
    /// \brief TCP Client 송신 문자열입니다.
    /// </summary>
    public string TcpClientSendText
    {
        get => Event.TcpClientSendText;
        set => Event.TcpClientSendText = value;
    }

    /// <summary>
    /// \brief TCP Client 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ConnectClient")]
    private partial void ConnectClient();

    /// <summary>
    /// \brief TCP Client 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.DisconnectClient")]
    private partial void DisconnectClient();

    /// <summary>
    /// \brief TCP Client 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendClient")]
    private partial void SendClient();
}
