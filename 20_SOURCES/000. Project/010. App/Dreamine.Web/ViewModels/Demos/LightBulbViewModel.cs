using Dreamine.MVVM.ViewModels;

namespace DreamineWeb.ViewModels.Demos;

/// <summary>
/// 전구 데모 상태(Model). Dreamine 3분할에서 "상태"를 담당합니다.
/// </summary>
public sealed class LightBulbModel
{
    public bool IsOn { get; set; }
    public int ToggleCount { get; set; }
}

/// <summary>
/// 전구 데모 로직(Event). Dreamine 3분할에서 "동작"을 담당합니다.
/// </summary>
public sealed class LightBulbEvent
{
    private readonly LightBulbModel _model;
    public LightBulbEvent(LightBulbModel model) => _model = model;

    public void Toggle()
    {
        _model.IsOn = !_model.IsOn;
        _model.ToggleCount++;
    }

    public void Set(bool on)
    {
        if (_model.IsOn == on) return;
        _model.IsOn = on;
        _model.ToggleCount++;
    }
}

/// <summary>
/// 전구 데모 ViewModel(바인딩). 실제 WPF 샘플에서는 [DreamineModel]/[DreamineEvent]/[DreamineCommand]로
/// 자동 주입·생성되지만, 이 라이브 데모는 브라우저에서 바로 실행되도록 수동 연결한 버전입니다.
/// </summary>
public sealed class LightBulbViewModel : ViewModelBase
{
    private readonly LightBulbModel _model = new();
    private readonly LightBulbEvent _event;

    public LightBulbViewModel() => _event = new LightBulbEvent(_model);

    /// <summary>전구 점등 상태. 체크박스와 양방향 바인딩됩니다.</summary>
    public bool IsOn
    {
        get => _model.IsOn;
        set
        {
            _event.Set(value);
            RaiseState();
        }
    }

    /// <summary>토글된 총 횟수(Event가 누적).</summary>
    public int ToggleCount => _model.ToggleCount;

    /// <summary>상태 텍스트.</summary>
    public string StatusText => _model.IsOn ? "ON" : "OFF";

    /// <summary>전구를 토글합니다(버튼 커맨드에 해당).</summary>
    public void Toggle()
    {
        _event.Toggle();
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(IsOn));
        OnPropertyChanged(nameof(ToggleCount));
        OnPropertyChanged(nameof(StatusText));
    }
}
