using Dreamine.MVVM.ViewModels;

namespace DreamineWeb.ViewModels.Demos;

/// <summary>
/// \if KO
/// <para>전구 데모 상태(Model). Dreamine 3분할에서 "상태"를 담당합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates light bulb model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LightBulbModel
{
    /// <summary>
    /// \if KO
    /// <para>Is On 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is on value.</para>
    /// \endif
    /// </summary>
    public bool IsOn { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Toggle Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the toggle count value.</para>
    /// \endif
    /// </summary>
    public int ToggleCount { get; set; }
}

/// <summary>
/// \if KO
/// <para>전구 데모 로직(Event). Dreamine 3분할에서 "동작"을 담당합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates light bulb event functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LightBulbEvent
{
    /// <summary>
    /// \if KO
    /// <para>model 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the model value.</para>
    /// \endif
    /// </summary>
    private readonly LightBulbModel _model;
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbEvent"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbEvent"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="model">
    /// \if KO
    /// <para>model에 사용할 <c>LightBulbModel</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LightBulbModel</c> value used for model.</para>
    /// \endif
    /// </param>
    public LightBulbEvent(LightBulbModel model) => _model = model;

    /// <summary>
    /// \if KO
    /// <para>Toggle 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle operation.</para>
    /// \endif
    /// </summary>
    public void Toggle()
    {
        _model.IsOn = !_model.IsOn;
        _model.ToggleCount++;
    }

    /// <summary>
    /// \if KO
    /// <para>값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the value.</para>
    /// \endif
    /// </summary>
    /// <param name="on">
    /// \if KO
    /// <para>on에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for on.</para>
    /// \endif
    /// </param>
    public void Set(bool on)
    {
        if (_model.IsOn == on) return;
        _model.IsOn = on;
        _model.ToggleCount++;
    }
}

/// <summary>
/// \if KO
/// <para>전구 데모 ViewModel(바인딩). 실제 WPF 샘플에서는 [DreamineModel]/[DreamineEvent]/[DreamineCommand]로 자동 주입·생성되지만, 이 라이브 데모는 브라우저에서 바로 실행되도록 수동 연결한 버전입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates light bulb view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LightBulbViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>model 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the model value.</para>
    /// \endif
    /// </summary>
    private readonly LightBulbModel _model = new();
    /// <summary>
    /// \if KO
    /// <para>event 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the event value.</para>
    /// \endif
    /// </summary>
    private readonly LightBulbEvent _event;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LightBulbViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LightBulbViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public LightBulbViewModel() => _event = new LightBulbEvent(_model);

    /// <summary>
    /// \if KO
    /// <para>전구 점등 상태. 체크박스와 양방향 바인딩됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is on value.</para>
    /// \endif
    /// </summary>
    public bool IsOn
    {
        get => _model.IsOn;
        set
        {
            _event.Set(value);
            RaiseState();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>토글된 총 횟수(Event가 누적).</para>
    /// \endif
    /// \if EN
    /// <para>Gets the toggle count value.</para>
    /// \endif
    /// </summary>
    public int ToggleCount => _model.ToggleCount;

    /// <summary>
    /// \if KO
    /// <para>상태 텍스트.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the status text value.</para>
    /// \endif
    /// </summary>
    public string StatusText => _model.IsOn ? "ON" : "OFF";

    /// <summary>
    /// \if KO
    /// <para>전구를 토글합니다(버튼 커맨드에 해당).</para>
    /// \endif
    /// \if EN
    /// <para>Performs the toggle operation.</para>
    /// \endif
    /// </summary>
    public void Toggle()
    {
        _event.Toggle();
        RaiseState();
    }

    /// <summary>
    /// \if KO
    /// <para>Raise State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the raise state operation.</para>
    /// \endif
    /// </summary>
    private void RaiseState()
    {
        OnPropertyChanged(nameof(IsOn));
        OnPropertyChanged(nameof(ToggleCount));
        OnPropertyChanged(nameof(StatusText));
    }
}
