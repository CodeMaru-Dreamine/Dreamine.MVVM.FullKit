using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Serial;

/// <summary>
/// \if KO
/// <para>\brief Serial Port 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates serial port test view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class SerialPortTestViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief Serial Port 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private SerialPortTestViewEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial Port 이름 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial port names value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> SerialPortNames => Event.SerialPortNames;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 BaudRate 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the baud rates value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<int> BaudRates => Event.BaudRates;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> SerialProtocols => Event.SerialProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> SerialEncodings => Event.SerialEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 Serial Port 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected serial port name value.</para>
    /// \endif
    /// </summary>
    public string SelectedSerialPortName
    {
        get => Event.SelectedSerialPortName;
        set
        {
            if (Event.SelectedSerialPortName == value)
            {
                return;
            }

            Event.SelectedSerialPortName = value;
            OnPropertyChanged(nameof(SelectedSerialPortName));
            OnPropertyChanged(nameof(SerialSelectionSummary));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 BaudRate입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected baud rate value.</para>
    /// \endif
    /// </summary>
    public int SelectedBaudRate
    {
        get => Event.SelectedBaudRate;
        set
        {
            if (Event.SelectedBaudRate == value)
            {
                return;
            }

            Event.SelectedBaudRate = value;
            OnPropertyChanged(nameof(SelectedBaudRate));
            OnPropertyChanged(nameof(SerialSelectionSummary));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 Serial 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected serial protocol value.</para>
    /// \endif
    /// </summary>
    public string SelectedSerialProtocol
    {
        get => Event.SelectedSerialProtocol;
        set
        {
            if (Event.SelectedSerialProtocol == value)
            {
                return;
            }

            Event.SelectedSerialProtocol = value;
            OnPropertyChanged(nameof(SelectedSerialProtocol));
            OnPropertyChanged(nameof(SerialSelectionSummary));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 Serial 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected serial encoding value.</para>
    /// \endif
    /// </summary>
    public string SelectedSerialEncoding
    {
        get => Event.SelectedSerialEncoding;
        set
        {
            if (Event.SelectedSerialEncoding == value)
            {
                return;
            }

            Event.SelectedSerialEncoding = value;
            OnPropertyChanged(nameof(SelectedSerialEncoding));
            OnPropertyChanged(nameof(SerialSelectionSummary));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Serial 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the serial send text value.</para>
    /// \endif
    /// </summary>
    public string SerialSendText
    {
        get => Event.SerialSendText;
        set
        {
            if (Event.SerialSendText == value)
            {
                return;
            }

            Event.SerialSendText = value;
            OnPropertyChanged(nameof(SerialSendText));
        }
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 Serial 선택 상태 요약 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial selection summary value.</para>
    /// \endif
    /// </summary>
    public string SerialSelectionSummary => Event.SerialSelectionSummary;

    /// <summary>
    /// \if KO
    /// <para>\brief Serial Port 목록 갱신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh ports operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.RefreshPorts")]
    private partial void RefreshPorts();

    /// <summary>
    /// \if KO
    /// <para>\brief Serial Port 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect serial operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ConnectSerial")]
    private partial void ConnectSerial();

    /// <summary>
    /// \if KO
    /// <para>\brief Serial Port 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect serial operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.DisconnectSerial")]
    private partial void DisconnectSerial();

    /// <summary>
    /// \if KO
    /// <para>\brief Serial 메시지 송신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send serial operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SendSerial")]
    private partial void SendSerial();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="SerialPortTestViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SerialPortTestViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>SerialPortTestViewEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the serial port test view.</para>
    /// \endif
    /// </param>
    public SerialPortTestViewModel(SerialPortTestViewEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
