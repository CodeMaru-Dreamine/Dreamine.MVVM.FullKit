using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Light Bulb View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Platform-independent ViewModel for the shared light bulb sample.</para>
/// \endif
/// </summary>
public partial class LightBulbViewModel : ViewModelBase
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
    private LightBulbEvent _event;

    /// <summary>
    /// \if KO
    /// <para>Is On 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is on value.</para>
    /// \endif
    /// </summary>
    public bool IsOn
    {
        get => Event.IsOn;
        set
        {
            if (Event.IsOn == value) return;
            Event.Toggle();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Toggle Count 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the toggle count value.</para>
    /// \endif
    /// </summary>
    public int ToggleCount => Event.ToggleCount;
    /// <summary>
    /// \if KO
    /// <para>Status Text 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the status text value.</para>
    /// \endif
    /// </summary>
    public string StatusText => IsOn ? "ON" : "OFF";

    /// <summary>
    /// \if KO
    /// <para>Toggle 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand("Event.Toggle")]
    private partial void Toggle();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="event">
    /// \if KO
    /// <para>event에 사용할 <c>LightBulbEvent</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbEvent</c> value used for event.</para>
    /// \endif
    /// </param>
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
