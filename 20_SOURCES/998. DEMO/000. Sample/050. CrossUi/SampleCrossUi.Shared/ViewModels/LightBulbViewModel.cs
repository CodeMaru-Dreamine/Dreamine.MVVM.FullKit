using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// Platform-independent ViewModel for the shared light bulb sample.
/// </summary>
public partial class LightBulbViewModel : ViewModelBase
{
    [DreamineEvent]
    private LightBulbEvent _event;

    public bool IsOn
    {
        get => Event.IsOn;
        set
        {
            if (Event.IsOn == value) return;
            Event.Toggle();
        }
    }

    public int ToggleCount => Event.ToggleCount;
    public string StatusText => IsOn ? "ON" : "OFF";

    [DreamineCommand("Event.Toggle")]
    private partial void Toggle();

    public LightBulbViewModel(LightBulbEvent @event)
    {
        _event = @event;
        Event.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(IsOn));
            OnPropertyChanged(nameof(ToggleCount));
            OnPropertyChanged(nameof(StatusText));
        };
    }
}
