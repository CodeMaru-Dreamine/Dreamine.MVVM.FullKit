using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Server 테스트 ViewModel입니다.
/// </summary>
public partial class TcpServerTestViewModel : ViewModelBase
{
    /// <summary>
    /// \brief TCP Server 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private TcpServerTestViewEvent _event;

    /// <summary>
    /// \brief 선택 가능한 TCP Server 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpServerProtocols => Event.TcpServerProtocols;

    /// <summary>
    /// \brief 선택 가능한 TCP Server 문자열 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpServerEncodings => Event.TcpServerEncodings;

    /// <summary>
    /// \brief 선택 가능한 TCP Server 송신 대상 정책 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpServerSendTargetModes => Event.TcpServerSendTargetModes;

    /// <summary>
    /// \brief 선택된 TCP Server 프로토콜입니다.
    /// </summary>
    public string SelectedTcpServerProtocol
    {
        get => Event.SelectedTcpServerProtocol;
        set
        {
            if (Event.SelectedTcpServerProtocol == value)
            {
                return;
            }

            Event.SelectedTcpServerProtocol = value;
            OnPropertyChanged(nameof(SelectedTcpServerProtocol));
        }
    }

    /// <summary>
    /// \brief 선택된 TCP Server 문자열 인코딩입니다.
    /// </summary>
    public string SelectedTcpServerEncoding
    {
        get => Event.SelectedTcpServerEncoding;
        set
        {
            if (Event.SelectedTcpServerEncoding == value)
            {
                return;
            }

            Event.SelectedTcpServerEncoding = value;
            OnPropertyChanged(nameof(SelectedTcpServerEncoding));
        }
    }

    /// <summary>
    /// \brief 선택된 TCP Server 송신 대상 정책입니다.
    /// </summary>
    public string SelectedTcpServerSendTargetMode
    {
        get => Event.SelectedTcpServerSendTargetMode;
        set
        {
            if (Event.SelectedTcpServerSendTargetMode == value)
            {
                return;
            }

            Event.SelectedTcpServerSendTargetMode = value;
            Event.ApplyServerOptions();
            OnPropertyChanged(nameof(SelectedTcpServerSendTargetMode));
        }
    }

    /// <summary>
    /// \brief TCP Server Echo 응답 사용 여부입니다.
    /// </summary>
    public bool IsTcpServerEchoEnabled
    {
        get => Event.IsTcpServerEchoEnabled;
        set
        {
            if (Event.IsTcpServerEchoEnabled == value)
            {
                return;
            }

            Event.IsTcpServerEchoEnabled = value;
            Event.ApplyServerOptions();
            OnPropertyChanged(nameof(IsTcpServerEchoEnabled));
        }
    }

    /// <summary>
    /// \brief TCP Server 송신 문자열입니다.
    /// </summary>
    public string TcpServerSendText
    {
        get => Event.TcpServerSendText;
        set => Event.TcpServerSendText = value;
    }

    /// <summary>
    /// \brief TCP Server 시작 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StartServer")]
    private partial void StartServer();

    /// <summary>
    /// \brief TCP Server 종료 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StopServer")]
    private partial void StopServer();

    /// <summary>
    /// \brief TCP Server 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendServer")]
    private partial void SendServer();
}
