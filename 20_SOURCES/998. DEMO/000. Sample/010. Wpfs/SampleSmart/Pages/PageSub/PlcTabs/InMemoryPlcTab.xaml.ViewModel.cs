using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.PlcTabs;

/// <summary>
/// \if KO
/// <para>\brief InMemory PLC 테스트 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates in memory plc tab view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class InMemoryPlcTabViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>\brief InMemory PLC 테스트 이벤트 처리 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private InMemoryPlcTabEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief InMemory PLC Client 선택 명령입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the use in memory operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.UseInMemory")]
    private partial void UseInMemory();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="InMemoryPlcTabViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="InMemoryPlcTabViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>InMemoryPlcTabEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the in-memory PLC tab.</para>
    /// \endif
    /// </param>
    public InMemoryPlcTabViewModel(InMemoryPlcTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}
