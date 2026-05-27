using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Dreamine.PLC.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine PLC 모니터 샘플 페이지 ViewModel입니다.
/// </summary>
public partial class PagePlcMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \brief Dreamine PLC 모니터 샘플 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private PagePlcMonitorEvent _event;

    /// <summary>
    /// \brief PLC Monitor ViewModel입니다.
    /// </summary>
    public PlcMonitorViewModel Monitor => Event.Monitor;

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
    /// \brief Handshake 시작 값 문자열입니다.
    /// </summary>
    public string HandshakeStartValueText
    {
        get => Event.HandshakeStartValueText;
        set
        {
            if (Event.HandshakeStartValueText == value)
            {
                return;
            }

            Event.HandshakeStartValueText = value;
            OnPropertyChanged(nameof(HandshakeStartValueText));
        }
    }

    /// <summary>
    /// \brief Handshake 반복 횟수 문자열입니다.
    /// </summary>
    public string HandshakeIterationsText
    {
        get => Event.HandshakeIterationsText;
        set
        {
            if (Event.HandshakeIterationsText == value)
            {
                return;
            }

            Event.HandshakeIterationsText = value;
            OnPropertyChanged(nameof(HandshakeIterationsText));
        }
    }

    /// <summary>
    /// \brief Handshake 반복 간격 문자열입니다.
    /// </summary>
    public string HandshakeDelayMsText
    {
        get => Event.HandshakeDelayMsText;
        set
        {
            if (Event.HandshakeDelayMsText == value)
            {
                return;
            }

            Event.HandshakeDelayMsText = value;
            OnPropertyChanged(nameof(HandshakeDelayMsText));
        }
    }

    /// <summary>
    /// \brief InMemory PLC Client 선택 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseInMemory")]
    private partial void UseInMemory();

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

    /// <summary>
    /// \brief D100/D101 기반 자동 응답 Handshake 테스트 명령입니다.
    /// </summary>
    [DreamineCommand("Event.RunHandshakeTest")]
    private partial void RunHandshakeTest();
}
