using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \if KO
/// <para>\brief RabbitMQ 통신 샘플 탭 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates rabbit mq communication tab view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class RabbitMqCommunicationTabViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 통신 샘플 탭 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private RabbitMqCommunicationTabEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief RabbitMQ 통신 샘플 탭 제목입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the title value.</para>
    /// \endif
    /// </summary>
    public string Title => "RabbitMQ Communication";

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="RabbitMqCommunicationTabViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="RabbitMqCommunicationTabViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>RabbitMqCommunicationTabEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the RabbitMQ communication tab.</para>
    /// \endif
    /// </param>
    public RabbitMqCommunicationTabViewModel(RabbitMqCommunicationTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}