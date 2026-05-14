using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Serial;

/// <summary>
/// \brief Serial Port 테스트 ViewModel입니다.
/// </summary>
public partial class SerialPortTestViewModel : ViewModelBase
{
    /// <summary>
    /// \brief Serial Port 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private SerialPortTestViewEvent _event;

    /// <summary>
    /// \brief 선택 가능한 Serial Port 이름 목록입니다.
    /// </summary>
    public IReadOnlyList<string> SerialPortNames => Event.SerialPortNames;

    /// <summary>
    /// \brief 선택 가능한 BaudRate 목록입니다.
    /// </summary>
    public IReadOnlyList<int> BaudRates => Event.BaudRates;

    /// <summary>
    /// \brief 선택 가능한 Serial 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> SerialProtocols => Event.SerialProtocols;

    /// <summary>
    /// \brief 선택 가능한 Serial 문자열 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> SerialEncodings => Event.SerialEncodings;

    /// <summary>
    /// \brief 선택된 Serial Port 이름입니다.
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
    /// \brief 선택된 BaudRate입니다.
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
    /// \brief 선택된 Serial 프로토콜입니다.
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
    /// \brief 선택된 Serial 문자열 인코딩입니다.
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
    /// \brief Serial 송신 문자열입니다.
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
    /// \brief 현재 Serial 선택 상태 요약 문자열입니다.
    /// </summary>
    public string SerialSelectionSummary => Event.SerialSelectionSummary;

    /// <summary>
    /// \brief Serial Port 목록 갱신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.RefreshPorts")]
    private partial void RefreshPorts();

    /// <summary>
    /// \brief Serial Port 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ConnectSerial")]
    private partial void ConnectSerial();

    /// <summary>
    /// \brief Serial Port 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.DisconnectSerial")]
    private partial void DisconnectSerial();

    /// <summary>
    /// \brief Serial 메시지 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendSerial")]
    private partial void SendSerial();
}
