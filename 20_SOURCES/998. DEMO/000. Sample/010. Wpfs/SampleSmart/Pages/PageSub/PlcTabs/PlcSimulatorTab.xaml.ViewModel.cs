using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \if KO
/// <para>\brief PLC Simulator TCP 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates plc simulator tab view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class PlcSimulatorTabViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator TCP 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private PlcSimulatorTabEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief Simulator Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the host value.</para>
    /// \endif
    /// </summary>
    public string Host
    {
        get => Event.Host;
        set
        {
            if (Event.Host == value)
            {
                return;
            }

            Event.Host = value;
            OnPropertyChanged(nameof(Host));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Simulator Port 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the port text value.</para>
    /// \endif
    /// </summary>
    public string PortText
    {
        get => Event.PortText;
        set
        {
            if (Event.PortText == value)
            {
                return;
            }

            Event.PortText = value;
            OnPropertyChanged(nameof(PortText));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버 시작 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start server operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StartServer")]
    private partial void StartServer();

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator 서버 중지 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop server operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.StopServer")]
    private partial void StopServer();

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Simulator TCP Client 선택 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use tcp client operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseTcpClient")]
    private partial void UseTcpClient();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PlcSimulatorTabViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PlcSimulatorTabViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>PlcSimulatorTabEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the PLC simulator tab.</para>
    /// \endif
    /// </param>
    public PlcSimulatorTabViewModel(PlcSimulatorTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
