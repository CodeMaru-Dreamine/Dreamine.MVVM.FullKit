using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Loopback 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp loopback test view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class TcpLoopbackTestViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief TCP Loopback 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private TcpLoopbackTestViewEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Loopback 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp loopback protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackProtocols => Event.TcpLoopbackProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Loopback 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp loopback encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackEncodings => Event.TcpLoopbackEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Loopback 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp loopback protocol value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 선택된 TCP Loopback 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp loopback encoding value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief TCP Loopback 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tcp loopback send text value.</para>
    /// \endif
    /// </summary>
    public string TcpLoopbackSendText
    {
        get => Event.TcpLoopbackSendText;
        set => Event.TcpLoopbackSendText = value;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Loopback 시작 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start loopback operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StartLoopback")]
    private partial void StartLoopback();

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Loopback 종료 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop loopback operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StopLoopback")]
    private partial void StopLoopback();

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client 송신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send client operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SendClient")]
    private partial void SendClient();

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
    /// <para>지정한 설정으로 <see cref="TcpLoopbackTestViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpLoopbackTestViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>TcpLoopbackTestViewEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the TCP loopback test view.</para>
    /// \endif
    /// </param>
    public TcpLoopbackTestViewModel(TcpLoopbackTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
