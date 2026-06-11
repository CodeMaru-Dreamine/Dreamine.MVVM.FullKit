using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief RabbitMQ 통신 샘플 탭 ViewModel입니다.
/// </summary>
public partial class RabbitMqCommunicationTabViewModel : ViewModelBase
{
    /// <summary>
    /// \brief RabbitMQ 통신 샘플 탭 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private RabbitMqCommunicationTabEvent _event;

    /// <summary>
    /// \brief RabbitMQ 통신 샘플 탭 제목입니다.
    /// </summary>
    public string Title => "RabbitMQ Communication";

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqCommunicationTabViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the RabbitMQ communication tab.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public RabbitMqCommunicationTabViewModel(RabbitMqCommunicationTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}