using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Dreamine.PLC.Wpf.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \if KO
/// <para>\brief Dreamine PLC 모니터 샘플 페이지 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates page plc monitor view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class PagePlcMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief Dreamine PLC 모니터 샘플 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private PagePlcMonitorEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief PLC Monitor ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the monitor value.</para>
    /// \endif
    /// </summary>
    public PlcMonitorViewModel Monitor => Event.Monitor;

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
    /// <para>\brief PLC Client 선택 모드 문자열입니다. SimulatorTcp, McTcp, McUdp, FinsTcp, FinsUdp, MxComponent, CxComponent를 사용합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the client mode text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief MX Component ProgID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mx prog id value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief MX Component logical station number 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mx logical station number text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief CX-Compolet ProgID입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cx prog id value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief CX-Compolet peer address입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the cx peer address value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Mitsubishi MC Host입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc host value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Mitsubishi MC Port 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc port text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Mitsubishi MC Transport 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc transport text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Mitsubishi MC Retry Count 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mc retry count text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Handshake 시작 값 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake start value text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Handshake 반복 횟수 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake iterations text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Handshake 반복 간격 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the handshake delay ms text value.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief InMemory PLC Client 선택 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use in memory operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseInMemory")]
    private partial void UseInMemory();

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
    /// <para>\brief 선택된 PLC Client를 모니터에 연결하는 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use selected client operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseSelectedClient")]
    private partial void UseSelectedClient();


    /// <summary>
    /// \if KO
    /// <para>\brief Mitsubishi MC Client 선택 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use mc client operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseMcClient")]
    private partial void UseMcClient();

    /// <summary>
    /// \if KO
    /// <para>\brief D100/D101 기반 자동 응답 Handshake 테스트 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run handshake test operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.RunHandshakeTest")]
    private partial void RunHandshakeTest();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PagePlcMonitorViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PagePlcMonitorViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>PagePlcMonitorEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the PLC monitor page.</para>
    /// \endif
    /// </param>
    public PagePlcMonitorViewModel(PagePlcMonitorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
