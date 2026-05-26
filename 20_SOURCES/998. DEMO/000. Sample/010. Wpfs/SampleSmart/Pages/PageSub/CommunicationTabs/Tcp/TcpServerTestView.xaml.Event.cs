using Dreamine.Communication.Core.Protocols;
using Dreamine.Communication.Sockets.Enums;
using SampleSmart.Pages.PageSub.CommunicationTabs;

namespace SampleSmart.Pages.PageSub.CommunicationTabs.Tcp;

/// <summary>
/// \brief TCP Server 테스트 이벤트 처리 클래스입니다.
/// </summary>
public sealed class TcpServerTestViewEvent
{
    private readonly CommunicationSampleRuntime _runtime;

    /// <summary>
    /// \brief TcpServerTestViewEvent 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="runtime">Communication 샘플 공유 Runtime입니다.</param>
    public TcpServerTestViewEvent(CommunicationSampleRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        SelectedTcpServerProtocol = "RawAvailable";
        SelectedTcpServerEncoding = PlainTextProtocolOptions.Utf8EncodingName;
        SelectedTcpServerSendTargetMode = nameof(TcpServerSendTargetMode.Broadcast);
        IsTcpServerEchoEnabled = false;
        TcpServerSendText = "Hello from Dreamine TCP Server";
    }

    /// <summary>
    /// \brief 선택 가능한 TCP Server 프로토콜 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpServerProtocols => _runtime.TcpProtocols;

    /// <summary>
    /// \brief 선택 가능한 TCP Server 문자열 인코딩 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpServerEncodings => _runtime.TextEncodings;

    /// <summary>
    /// \brief 선택 가능한 TCP Server 송신 대상 정책 목록입니다.
    /// </summary>
    public IReadOnlyList<string> TcpServerSendTargetModes => _runtime.TcpServerSendTargetModes;

    /// <summary>
    /// \brief 선택된 TCP Server 프로토콜입니다.
    /// </summary>
    public string SelectedTcpServerProtocol { get; set; }

    /// <summary>
    /// \brief 선택된 TCP Server 문자열 인코딩입니다.
    /// </summary>
    public string SelectedTcpServerEncoding { get; set; }

    /// <summary>
    /// \brief 선택된 TCP Server 송신 대상 정책입니다.
    /// </summary>
    public string SelectedTcpServerSendTargetMode { get; set; }

    /// <summary>
    /// \brief TCP Server Echo 응답 사용 여부입니다.
    /// </summary>
    public bool IsTcpServerEchoEnabled { get; set; }

    /// <summary>
    /// \brief TCP Server 송신 문자열입니다.
    /// </summary>
    public string TcpServerSendText { get; set; }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Server를 시작합니다.
    /// </summary>
    public void StartServer()
    {
        _ = _runtime.StartTcpServerAsync(
            SelectedTcpServerProtocol,
            SelectedTcpServerEncoding,
            SelectedTcpServerSendTargetMode,
            IsTcpServerEchoEnabled);
    }

    /// <summary>
    /// \brief TCP Server를 종료합니다.
    /// </summary>
    public void StopServer()
    {
        _ = _runtime.StopTcpServerAsync();
    }

    /// <summary>
    /// \brief 현재 TCP Server 옵션을 Runtime에 반영합니다.
    /// </summary>
    public void ApplyServerOptions()
    {
        _runtime.UpdateTcpServerOptions(
            SelectedTcpServerProtocol,
            SelectedTcpServerEncoding,
            SelectedTcpServerSendTargetMode,
            IsTcpServerEchoEnabled);
    }

    /// <summary>
    /// \brief 선택된 프로토콜로 TCP Server에서 메시지를 송신합니다.
    /// </summary>
    public void SendServer()
    {
        _ = _runtime.SendTcpServerAsync(
            SelectedTcpServerProtocol,
            TcpServerSendText,
            SelectedTcpServerEncoding,
            SelectedTcpServerSendTargetMode);
    }
}
