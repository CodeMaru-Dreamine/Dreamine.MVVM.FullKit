using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \brief Serial 통신 샘플 탭 ViewModel입니다.
/// </summary>
public partial class SerialCommunicationTabViewModel : ViewModelBase
{
    [DreamineEvent]
    private SerialCommunicationTabEvent _event;

    /// <summary>
    /// \brief Serial 통신 샘플 탭 제목입니다.
    /// </summary>
    public string Title => "Serial Communication";

    /// <summary>
    /// Initializes a new instance of the <see cref="SerialCommunicationTabViewModel"/> class.
    /// </summary>
    /// <param name="event">The event handler used by the serial communication tab.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <c>null</c>.
    /// </exception>
    public SerialCommunicationTabViewModel(SerialCommunicationTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}