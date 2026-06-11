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
    /// Initializes a new instance of the <see cref="PageCommunicationMonitorViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the communication monitor page.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public PageCommunicationMonitorViewModel(PageCommunicationMonitorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
