using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Client 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp client test view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class TcpClientTestViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private TcpClientTestViewEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Client 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp client protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpClientProtocols => Event.TcpClientProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Client 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp client encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpClientEncodings => Event.TcpClientEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Client 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp client protocol value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 선택된 TCP Client 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp client encoding value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief TCP Client 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tcp client send text value.</para>
    /// \endif
    /// </summary>
    public string TcpClientSendText
    {
        get => Event.TcpClientSendText;
        set => Event.TcpClientSendText = value;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect client operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ConnectClient")]
    private partial void ConnectClient();

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect client operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.DisconnectClient")]
    private partial void DisconnectClient();

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
    /// <para>지정한 설정으로 <see cref="TcpClientTestViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpClientTestViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>TcpClientTestViewEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the TCP client test view.</para>
    /// \endif
    /// </param>
    public TcpClientTestViewModel(TcpClientTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
