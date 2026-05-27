using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \brief InMemory PLC 테스트 ViewModel입니다.
/// </summary>
public partial class InMemoryPlcTabViewModel : ViewModelBase
{
    /// <summary>
    /// \brief InMemory PLC 테스트 이벤트 처리 객체입니다.
    /// </summary>
    [DreamineEvent]
    private InMemoryPlcTabEvent _event;

    /// <summary>
    /// \brief InMemory PLC Client 선택 명령입니다.
    /// </summary>
    [DreamineCommand("Event.UseInMemory")]
    private partial void UseInMemory();
}
