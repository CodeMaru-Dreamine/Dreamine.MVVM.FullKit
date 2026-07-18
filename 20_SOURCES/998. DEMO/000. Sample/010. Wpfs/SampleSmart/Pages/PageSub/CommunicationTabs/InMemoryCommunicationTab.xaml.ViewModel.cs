using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \if KO
/// <para>\brief InMemory Communication 샘플 탭 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory communication tab view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class InMemoryCommunicationTabViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief InMemory Communication 샘플 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private InMemoryCommunicationTabEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 송신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the in memory send text value.</para>
    /// \endif
    /// </summary>
    public string InMemorySendText
    {
        get => Event.InMemorySendText;
        set => Event.InMemorySendText = value;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory 수동 수신 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the in memory receive text value.</para>
    /// \endif
    /// </summary>
    public string InMemoryReceiveText
    {
        get => Event.InMemoryReceiveText;
        set => Event.InMemoryReceiveText = value;
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 채널 추가 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the channel item.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.AddChannel")]
    private partial void AddChannel();

    /// <summary>
    /// \if KO
    /// <para>\brief 연결 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the connect operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Connect")]
    private partial void Connect();

    /// <summary>
    /// \if KO
    /// <para>\brief 연결 해제 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the disconnect operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Disconnect")]
    private partial void Disconnect();

    /// <summary>
    /// \if KO
    /// <para>\brief 테스트 송신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send test operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.SendTest")]
    private partial void SendTest();

    /// <summary>
    /// \if KO
    /// <para>\brief 테스트 수신 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the receive test operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ReceiveTest")]
    private partial void ReceiveTest();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="InMemoryCommunicationTabViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="InMemoryCommunicationTabViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>InMemoryCommunicationTabEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the in-memory communication tab.</para>
    /// \endif
    /// </param>
    public InMemoryCommunicationTabViewModel(InMemoryCommunicationTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}