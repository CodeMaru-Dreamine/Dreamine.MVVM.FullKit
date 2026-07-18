using Dreamine.Communication.Core.Protocols;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \if KO
/// <para>\brief TCP Client 테스트 이벤트 처리 클래스입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tcp client test view event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TcpClientTestViewEvent
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
    /// <para>\brief TcpClientTestViewEvent 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="TcpClientTestViewEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="runtime">
    /// \if KO
    /// <para>Communication 샘플 공유 런타임입니다.</para>
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
    public TcpClientTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedTcpClientProtocol = "RawAvailable";
        SelectedTcpClientEncoding = PlainTextProtocolOptions.Utf8EncodingName;
        TcpClientSendText = "test";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Client 프로토콜 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp client protocols value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpClientProtocols => _runtime.TcpProtocols;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택 가능한 TCP Client 문자열 인코딩 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tcp client encodings value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> TcpClientEncodings => _runtime.TextEncodings;

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Client 프로토콜입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp client protocol value.</para>
    /// \endif
    /// </summary>
    public string SelectedTcpClientProtocol { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief 선택된 TCP Client 문자열 인코딩입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tcp client encoding value.</para>
    /// \endif
    /// </summary>
    public string SelectedTcpClientEncoding { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tcp client send text value.</para>
    /// \endif
    /// </summary>
    public string TcpClientSendText { get; set; }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client를 연결합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect client operation.</para>
    /// \endif
    /// </summary>
    public void ConnectClient()
    {
        _ = _runtime.ConnectTcpClientAsync(
            SelectedTcpClientProtocol,
            SelectedTcpClientEncoding);
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client 연결을 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect client operation.</para>
    /// \endif
    /// </summary>
    public void DisconnectClient()
    {
        _ = _runtime.DisconnectTcpClientAsync();
    }

    /// <summary>
    /// \if KO
    /// <para>\brief TCP Client에서 문자열을 송신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send client operation.</para>
    /// \endif
    /// </summary>
    public void SendClient()
    {
        _ = _runtime.SendTcpClientAsync(
            SelectedTcpClientProtocol,
            TcpClientSendText,
            SelectedTcpClientEncoding);
    }
}
