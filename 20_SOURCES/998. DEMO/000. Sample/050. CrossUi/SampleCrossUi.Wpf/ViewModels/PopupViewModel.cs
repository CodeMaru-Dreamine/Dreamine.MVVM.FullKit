using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Wpf.ViewModels;

/// <summary>
/// Popup 데모 ViewModel. 실제 동작은 <see cref="PopupEvent"/>에 위임하고,
/// 이 클래스는 [DreamineCommand]로 생성되는 forwarding 커맨드만 노출한다.
/// </summary>
public partial class PopupViewModel : ViewModelBase
{
    [DreamineEvent]
    private PopupEvent _event;

    public string LastResult => Event.LastResult;

    [DreamineCommand("Event.ShowMessageBox")]
    private partial void ShowMessageBox();

    [DreamineCommand("Event.ShowBlinkOk")]
    private partial void ShowBlinkOk();

    [DreamineCommand("Event.ShowBlinkAlarm")]
    private partial void ShowBlinkAlarm();

    [DreamineCommand("Event.OpenAsWindow")]
    private partial void OpenAsWindow();

    public PopupViewModel(PopupEvent @event)
    {
        _event = @event;
        Event.Changed += (_, _) => OnPropertyChanged(nameof(LastResult));
    }
}
