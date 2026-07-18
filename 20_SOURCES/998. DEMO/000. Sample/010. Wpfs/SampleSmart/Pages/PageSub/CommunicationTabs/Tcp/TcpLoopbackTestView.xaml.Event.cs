using Dreamine.Communication.Core.Protocols;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Loopback 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp loopback test view event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TcpLoopbackTestViewEvent
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
    /// <para>\brief TcpLoopbackTestViewEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpLoopbackTestViewEvent"/> class with the specified settings.</para>
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
    public TcpLoopbackTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedTcpLoopbackProtocol = "PlainText";
        SelectedTcpLoopbackEncoding = PlainTextProtocolOptions.Utf8EncodingName;
        TcpLoopbackSendText = "Hello from Dreamine TCP Loopback";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Loopback 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp loopback protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackProtocols => _runtime.TcpProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Loopback 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp loopback encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpLoopbackEncodings => _runtime.TextEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Loopback 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp loopback protocol value.</para>
    /// \endif
    /// </summary>
    public string SelectedTcpLoopbackProtocol { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Loopback 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp loopback encoding value.</para>
    /// \endif
    /// </summary>
    public string SelectedTcpLoopbackEncoding { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Loopback 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tcp loopback send text value.</para>
    /// \endif
    /// </summary>
    public string TcpLoopbackSendText { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 프로토콜로 TCP Server와 TCP Client를 모두 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start loopback operation.</para>
    /// \endif
    /// </summary>
    public void StartLoopback()
    {
        _ = StartLoopbackAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server와 TCP Client를 모두 종료합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop loopback operation.</para>
    /// \endif
    /// </summary>
    public void StopLoopback()
    {
        _ = _runtime.StopAllTcpAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client에서 Server로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send client operation.</para>
    /// \endif
    /// </summary>
    public void SendClient()
    {
        _ = _runtime.SendTcpClientAsync(
            SelectedTcpLoopbackProtocol,
            TcpLoopbackSendText,
            SelectedTcpLoopbackEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Server에서 Client로 메시지를 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send server operation.</para>
    /// \endif
    /// </summary>
    public void SendServer()
    {
        _ = _runtime.SendTcpServerAsync(
            SelectedTcpLoopbackProtocol,
            TcpLoopbackSendText,
            SelectedTcpLoopbackEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>Start Loopback Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start loopback async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Start Loopback Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start loopback async operation.</para>
    /// \endif
    /// </returns>
    private async Task StartLoopbackAsync()
    {
        await _runtime.StartTcpServerAsync(
            SelectedTcpLoopbackProtocol,
            SelectedTcpLoopbackEncoding);

        await _runtime.ConnectTcpClientAsync(
            SelectedTcpLoopbackProtocol,
            SelectedTcpLoopbackEncoding);
    }
}
