using Dreamine.Communication.Wpf.ViewModels;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \brief Dreamine Communication 모니터 샘플 페이지 ViewModel입니다.
/// </summary>
public partial class PageCommunicationMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \brief Dreamine Communication 모니터 샘플 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private PageCommunicationMonitorEvent _event;

    /// <summary>
    /// \brief Communication Monitor ViewModel입니다.
    /// </summary>
    public CommunicationMonitorViewModel Monitor => Event.Monitor;

    /// <summary>
    /// \brief 채널 추가 명령입니다.
    /// </summary>
    [DreamineCommand("Event.AddChannel")]
    private partial void AddChannel();

    /// <summary>
    /// \brief 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.Connect")]
    private partial void Connect();

    /// <summary>
    /// \brief 연결 해제 명령입니다.
    /// </summary>
    [DreamineCommand("Event.Disconnect")]
    private partial void Disconnect();

    /// <summary>
    /// \brief 테스트 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendTest")]
    private partial void SendTest();

    /// <summary>
    /// \brief 테스트 수신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ReceiveTest")]
    private partial void ReceiveTest();

    /// <summary>
    /// \brief TCP 서버 시작 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StartTcpServer")]
    private partial void StartTcpServer();

    /// <summary>
    /// \brief TCP 클라이언트 연결 명령입니다.
    /// </summary>
    [DreamineCommand("Event.ConnectTcpClient")]
    private partial void ConnectTcpClient();

    /// <summary>
    /// \brief TCP 테스트 송신 명령입니다.
    /// </summary>
    [DreamineCommand("Event.SendTcp")]
    private partial void SendTcp();

    /// <summary>
    /// \brief TCP 테스트 종료 명령입니다.
    /// </summary>
    [DreamineCommand("Event.StopTcp")]
    private partial void StopTcp();
}