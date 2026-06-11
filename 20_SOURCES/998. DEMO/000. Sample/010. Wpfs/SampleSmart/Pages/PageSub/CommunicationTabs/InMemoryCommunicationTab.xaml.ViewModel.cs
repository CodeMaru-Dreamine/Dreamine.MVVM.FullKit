using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief InMemory Communication 샘플 탭 ViewModel입니다.
/// </summary>
public partial class InMemoryCommunicationTabViewModel : ViewModelBase
{
    /// <summary>
    /// \brief InMemory Communication 샘플 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private InMemoryCommunicationTabEvent _event;

    /// <summary>
    /// \brief InMemory 송신 문자열입니다.
    /// </summary>
    public string InMemorySendText
    {
        get => Event.InMemorySendText;
        set => Event.InMemorySendText = value;
    }

    /// <summary>
    /// \brief InMemory 수동 수신 문자열입니다.
    /// </summary>
    public string InMemoryReceiveText
    {
        get => Event.InMemoryReceiveText;
        set => Event.InMemoryReceiveText = value;
    }

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
    /// Initializes a new instance of the <see cref="InMemoryCommunicationTabViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the in-memory communication tab.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public InMemoryCommunicationTabViewModel(InMemoryCommunicationTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}