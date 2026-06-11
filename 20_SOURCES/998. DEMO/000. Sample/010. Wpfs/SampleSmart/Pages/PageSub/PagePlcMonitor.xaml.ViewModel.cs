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
    /// \brief PLC Client 선택 모드 문자열입니다. SimulatorTcp, McTcp, McUdp, FinsTcp, FinsUdp, MxComponent, CxComponent를 사용합니다.
    /// </summary>
    public string ClientModeText
    {
        get => Event.ClientModeText;
        set
        {
            if (Event.ClientModeText == value)
            {
                return;
            }

            Event.ClientModeText = value;
            OnPropertyChanged(nameof(ClientModeText));
        }
    }

    /// <summary>
    /// \brief MX Component ProgID입니다.
    /// </summary>
    public string MxProgId
    {
        get => Event.MxProgId;
        set
        {
            if (Event.MxProgId == value)
            {
                return;
            }

            Event.MxProgId = value;
            OnPropertyChanged(nameof(MxProgId));
        }
    }

    /// <summary>
    /// \brief MX Component logical station number 문자열입니다.
    /// </summary>
    public string MxLogicalStationNumberText
    {
        get => Event.MxLogicalStationNumberText;
        set
        {
            if (Event.MxLogicalStationNumberText == value)
            {
                return;
            }

            Event.MxLogicalStationNumberText = value;
            OnPropertyChanged(nameof(MxLogicalStationNumberText));
        }
    }

    /// <summary>
    /// \brief CX-Compolet ProgID입니다.
    /// </summary>
    public string CxProgId
    {
        get => Event.CxProgId;
        set
        {
            if (Event.CxProgId == value)
            {
                return;
            }

            Event.CxProgId = value;
            OnPropertyChanged(nameof(CxProgId));
        }
    }

    /// <summary>
    /// \brief CX-Compolet peer address입니다.
    /// </summary>
    public string CxPeerAddress
    {
        get => Event.CxPeerAddress;
        set
        {
            if (Event.CxPeerAddress == value)
            {
                return;
            }

            Event.CxPeerAddress = value;
            OnPropertyChanged(nameof(CxPeerAddress));
        }
    }

    /// <summary>
    /// \brief Mitsubishi MC Host입니다.
    /// </summary>
    public string McHost
    {
        get => Event.McHost;
        set
        {
            if (Event.McHost == value)
            {
                return;
            }

            Event.McHost = value;
            OnPropertyChanged(nameof(McHost));
        }
    }

    /// <summary>
    /// \brief Mitsubishi MC Port 문자열입니다.
    /// </summary>
    public string McPortText
    {
        get => Event.McPortText;
        set
        {
            if (Event.McPortText == value)
            {
                return;
            }

            Event.McPortText = value;
            OnPropertyChanged(nameof(McPortText));
        }
    }

    /// <summary>
    /// \brief Mitsubishi MC Transport 문자열입니다.
    /// </summary>
    public string McTransportText
    {
        get => Event.McTransportText;
        set
        {
            if (Event.McTransportText == value)
            {
                return;
            }

            Event.McTransportText = value;
            OnPropertyChanged(nameof(McTransportText));
        }
    }

    /// <summary>
    /// \brief Mitsubishi MC Retry Count 문자열입니다.
    /// </summary>
    public string McRetryCountText
    {
        get => Event.McRetryCountText;
        set
        {
            if (Event.McRetryCountText == value)
            {
                return;
            }

            Event.McRetryCountText = value;
            OnPropertyChanged(nameof(McRetryCountText));
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
    /// \brief 선택된 PLC Client를 모니터에 연결하는 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseSelectedClient")]
    private partial void UseSelectedClient();


    /// <summary>
    /// \brief Mitsubishi MC Client 선택 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseMcClient")]
    private partial void UseMcClient();

    /// <summary>
    /// \brief D100/D101 기반 자동 응답 Handshake 테스트 명령입니다.
    /// </summary>
    [DreamineCommand("Event.RunHandshakeTest")]
    private partial void RunHandshakeTest();

    /// <summary>
    /// Initializes a new instance of the <see cref="PagePlcMonitorViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the PLC monitor page.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public PagePlcMonitorViewModel(PagePlcMonitorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
