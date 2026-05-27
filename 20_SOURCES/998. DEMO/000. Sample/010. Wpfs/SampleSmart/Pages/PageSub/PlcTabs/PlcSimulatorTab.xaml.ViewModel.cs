using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \brief PLC Simulator TCP 테스트 ViewModel입니다.
/// </summary>
public partial class PlcSimulatorTabViewModel : ViewModelBase
{
    /// <summary>
    /// \brief PLC Simulator TCP 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private PlcSimulatorTabEvent _event;

    /// <summary>
    /// \brief Simulator Host입니다.
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
    /// \brief Simulator Port 문자열입니다.
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
    /// \brief PLC Simulator 서버 시작 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StartServer")]
    private partial void StartServer();

    /// <summary>
    /// \brief PLC Simulator 서버 중지 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StopServer")]
    private partial void StopServer();

    /// <summary>
    /// \brief PLC Simulator TCP Client 선택 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseTcpClient")]
    private partial void UseTcpClient();
}
