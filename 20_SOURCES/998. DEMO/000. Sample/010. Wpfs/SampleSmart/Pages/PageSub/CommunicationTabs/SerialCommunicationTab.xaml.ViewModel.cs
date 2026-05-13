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
}