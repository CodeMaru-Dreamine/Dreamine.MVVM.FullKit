using System.Collections.ObjectModel;
using System.IO.Ports;
using Dreamine.Communication.Core.Protocols;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Serial;

/// <summary>
/// \if KO
/// <para>\brief Serial Port 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates serial port test view event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SerialPortTestViewEvent
{
    /// <summary>
    /// \if KO
    /// <para>runtime 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the runtime value.</para>
    /// \endif
    /// </summary>
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \if KO
    /// <para>\brief SerialPortTestViewEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SerialPortTestViewEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>Communication 샘플 공유 Runtime입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CommunicationSampleRuntime</c> value used for runtime.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para>필수 입력 인자 중 하나가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when a required input argument is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
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
        SelectedSerialEncoding = PlainTextProtocolOptions.Utf8EncodingName;
        SerialSendText = "test";

        RefreshPorts();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial Port 이름 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial port names value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<string> SerialPortNames { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 BaudRate 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the baud rates value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<int> BaudRates { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> SerialProtocols { get; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 Serial 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> SerialEncodings => _runtime.TextEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 Serial Port 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected serial port name value.</para>
    /// \endif
    /// </summary>
    public string SelectedSerialPortName { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 BaudRate입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected baud rate value.</para>
    /// \endif
    /// </summary>
    public int SelectedBaudRate { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 Serial 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected serial protocol value.</para>
    /// \endif
    /// </summary>
    public string SelectedSerialProtocol { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 Serial 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected serial encoding value.</para>
    /// \endif
    /// </summary>
    public string SelectedSerialEncoding { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief Serial 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the serial send text value.</para>
    /// \endif
    /// </summary>
    public string SerialSendText { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>\brief 현재 Serial 선택 상태 요약 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the serial selection summary value.</para>
    /// \endif
    /// </summary>
    public string SerialSelectionSummary =>
        $"Port={SelectedSerialPortName}, BaudRate={SelectedBaudRate}, Protocol={SelectedSerialProtocol}, Encoding={SelectedSerialEncoding}";

    /// <summary>
    /// \if KO
    /// <para>\brief Serial Port 목록을 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh ports operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief Serial Port 연결을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect serial operation.</para>
    /// \endif
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
            SelectedSerialProtocol,
            SelectedSerialEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Serial Port 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect serial operation.</para>
    /// \endif
    /// </summary>
    public void DisconnectSerial()
    {
        _ = _runtime.DisconnectSerialAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief Serial 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send serial operation.</para>
    /// \endif
    /// </summary>
    public void SendSerial()
    {
        if (string.IsNullOrWhiteSpace(SerialSendText))
        {
            SerialSendText = "test";
        }

        _ = _runtime.SendSerialAsync(
            SelectedSerialProtocol,
            SerialSendText,
            SelectedSerialEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>Fallback Ports 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the fallback ports item.</para>
    /// \endif
    /// </summary>
    private void AddFallbackPorts()
    {
        for (var index = 1; index <= 10; index++)
        {
            SerialPortNames.Add($"COM{index}");
        }
    }
}
