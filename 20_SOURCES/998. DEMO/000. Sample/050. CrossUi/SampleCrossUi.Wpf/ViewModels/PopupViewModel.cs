using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Wpf.ViewModels;

/// <summary>
/// \if KO
/// <para>Popup 데모 ViewModel. 실제 동작은 <see cref="PopupEvent"/>에 위임하고, 이 클래스는 [DreamineCommand]로 생성되는 forwarding 커맨드만 노출한다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates popup view model functionality and related state.</para>
/// \endif
/// </summary>
public partial class PopupViewModel : ViewModelBase
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
    private PopupEvent _event;

    /// <summary>
    /// \if KO
    /// <para>Last Result 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the last result value.</para>
    /// \endif
    /// </summary>
    public string LastResult => Event.LastResult;

    /// <summary>
    /// \if KO
    /// <para>Show Message Box 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the show message box operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ShowMessageBox")]
    private partial void ShowMessageBox();

    /// <summary>
    /// \if KO
    /// <para>Show Blink Ok 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the show blink ok operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ShowBlinkOk")]
    private partial void ShowBlinkOk();

    /// <summary>
    /// \if KO
    /// <para>Show Blink Alarm 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the show blink alarm operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.ShowBlinkAlarm")]
    private partial void ShowBlinkAlarm();

    /// <summary>
    /// \if KO
    /// <para>Open As Window 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the open as window operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.OpenAsWindow")]
    private partial void OpenAsWindow();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PopupViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PopupViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>PopupEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PopupEvent</c> value used for event.</para>
    /// \endif
    /// </param>
    public PopupViewModel(PopupEvent @event)
    {
        _event = @event;
        Event.Changed += (_, _) => OnPropertyChanged(nameof(LastResult));
    }
}
