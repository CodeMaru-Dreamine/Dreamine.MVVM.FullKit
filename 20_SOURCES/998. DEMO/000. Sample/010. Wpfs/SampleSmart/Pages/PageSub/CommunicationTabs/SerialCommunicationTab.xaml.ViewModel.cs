using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub.CommunicationTabs;

/// <summary>
/// \if KO
/// <para>\brief Serial 통신 샘플 탭 ViewModel입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates serial communication tab view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class SerialCommunicationTabViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>event 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    [DreamineEvent]
    private SerialCommunicationTabEvent _event;

    /// <summary>
    /// \if KO
    /// <para>\brief Serial 통신 샘플 탭 제목입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the title value.</para>
    /// \endif
    /// </summary>
    public string Title => "Serial Communication";

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="SerialCommunicationTabViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SerialCommunicationTabViewModel"/> class.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>SerialCommunicationTabEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The event handler used by the serial communication tab.</para>
    /// \endif
    /// </param>
    public SerialCommunicationTabViewModel(SerialCommunicationTabEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _event = @event;
    }
}