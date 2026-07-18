using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Server 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp server test view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class TcpServerTestViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private TcpServerTestViewEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Server 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp server protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpServerProtocols => Event.TcpServerProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Server 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp server encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpServerEncodings => Event.TcpServerEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Server 송신 대상 정책 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp server send target modes value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpServerSendTargetModes => Event.TcpServerSendTargetModes;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Server 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp server protocol value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 선택된 TCP Server 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp server encoding value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 선택된 TCP Server 송신 대상 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp server send target mode value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief TCP Server Echo 응답 사용 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is tcp server echo enabled value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief TCP Server 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tcp server send text value.</para>
    /// \endif
    /// </summary>
    public string TcpServerSendText
    {
        get => Event.TcpServerSendText;
        set => Event.TcpServerSendText = value;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server 시작 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start server operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StartServer")]
    private partial void StartServer();

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server 종료 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop server operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StopServer")]
    private partial void StopServer();

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server 송신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send server operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SendServer")]
    private partial void SendServer();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="TcpServerTestViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpServerTestViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>TcpServerTestViewEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the TCP server test view.</para>
    /// \endif
    /// </param>
    public TcpServerTestViewModel(TcpServerTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
