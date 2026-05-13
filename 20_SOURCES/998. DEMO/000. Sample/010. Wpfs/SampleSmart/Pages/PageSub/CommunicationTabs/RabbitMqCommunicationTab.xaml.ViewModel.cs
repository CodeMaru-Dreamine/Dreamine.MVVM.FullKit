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
}