using System.Collections.ObjectModel;
using System.IO.Ports;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Serial;

/// <summary>
/// \brief Serial Port 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class SerialPortTestViewEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief SerialPortTestViewEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public SerialPortTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SerialPortNames = new ObservableCollection<string>();

        BaudRates =
        [
            9600,
            19200,
            38400,
            57600,
            115200
        ];

        SerialProtocols =
        [
            "PlainText",
            "RawAvailable",
            "RawJson"
        ];

        SelectedBaudRate = 9600;
        SelectedSerialProtocol = "RawAvailable";
        SerialSendText = "test";

        RefreshPorts();
    }

    /// <summary>
    /// \brief 선택 가능한 Serial Port 이름 목록입니다.
    /// </summary>
    public ObservableCollection<string> SerialPortNames { get; }

    /// <summary>
    /// \brief 선택 가능한 BaudRate 목록입니다.
    /// </summary>
    public IReadOnlyList<int> BaudRates { get; }

    /// <summary>
    /// \brief 선택 가능한 Serial 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> SerialProtocols { get; }

    /// <summary>
    /// \brief 선택된 Serial Port 이름입니다.
    /// </summary>
    public string SelectedSerialPortName { get; set; } = string.Empty;

    /// <summary>
    /// \brief 선택된 BaudRate입니다.
    /// </summary>
    public int SelectedBaudRate { get; set; }

    /// <summary>
    /// \brief 선택된 Serial 프로토콜입니다.
    /// </summary>
    public string SelectedSerialProtocol { get; set; } = string.Empty;

    /// <summary>
    /// \brief Serial 송신 문자열입니다.
    /// </summary>
    public string SerialSendText { get; set; } = string.Empty;

    /// <summary>
    /// \brief 현재 Serial 선택 상태 요약 문자열입니다.
    /// </summary>
    public string SerialSelectionSummary =>
        $"Port={SelectedSerialPortName}, BaudRate={SelectedBaudRate}, Protocol={SelectedSerialProtocol}";

    /// <summary>
    /// \brief Serial Port 목록을 갱신합니다.
    /// </summary>
    public void RefreshPorts()
    {
        SerialPortNames.Clear();

        var portNames = SerialPort.GetPortNames()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (portNames.Length == 0)
        {
            AddFallbackPorts();
        }
        else
        {
            foreach (var portName in portNames)
            {
                SerialPortNames.Add(portName);
            }
        }

        if (string.IsNullOrWhiteSpace(SelectedSerialPortName) ||
            !SerialPortNames.Contains(SelectedSerialPortName))
        {
            SelectedSerialPortName = SerialPortNames.FirstOrDefault() ?? string.Empty;
        }
    }

    /// <summary>
    /// \brief Serial Port 연결을 시작합니다.
    /// </summary>
    public void ConnectSerial()
    {
        if (string.IsNullOrWhiteSpace(SelectedSerialPortName))
        {
            return;
        }

        _ = _runtime.ConnectSerialAsync(
            SelectedSerialPortName,
            SelectedBaudRate,
            SelectedSerialProtocol);
    }

    /// <summary>
    /// \brief Serial Port 연결을 해제합니다.
    /// </summary>
    public void DisconnectSerial()
    {
        _ = _runtime.DisconnectSerialAsync();
    }

    /// <summary>
    /// \brief Serial 메시지를 송신합니다.
    /// </summary>
    public void SendSerial()
    {
        if (string.IsNullOrWhiteSpace(SerialSendText))
        {
            SerialSendText = "test";
        }

        _ = _runtime.SendSerialAsync(
            SelectedSerialProtocol,
            SerialSendText);
    }

    private void AddFallbackPorts()
    {
        for (var index = 1; index <= 10; index++)
        {
            SerialPortNames.Add($"COM{index}");
        }
    }
}