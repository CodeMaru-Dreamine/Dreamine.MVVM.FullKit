using Dreamine.Communication.Wpf.ViewModels;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// \if KO
/// <para>\brief Dreamine Communication 모니터 샘플 페이지 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates page communication monitor view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class PageCommunicationMonitorViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief Dreamine Communication 모니터 샘플 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private PageCommunicationMonitorEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief Communication Monitor ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the monitor value.</para>
    /// \endif
    /// </summary>
    public CommunicationMonitorViewModel Monitor => Event.Monitor;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PageCommunicationMonitorViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PageCommunicationMonitorViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>PageCommunicationMonitorEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the communication monitor page.</para>
    /// \endif
    /// </param>
    public PageCommunicationMonitorViewModel(PageCommunicationMonitorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
