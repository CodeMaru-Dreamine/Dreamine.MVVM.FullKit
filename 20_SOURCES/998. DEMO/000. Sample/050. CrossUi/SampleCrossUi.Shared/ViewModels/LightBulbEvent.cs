namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Light Bulb Event 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Contains the UI-independent behavior for the light bulb sample.</para>
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
    /// <para>Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when changed takes place.</para>
    /// \endif
    /// </summary>
    public event EventHandler? Changed;

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
    public LightBulbEvent(LightBulbModel model)
    {
        _model = model;
    }

    /// <summary>
    /// \if KO
    /// <para>Is On 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is on value.</para>
    /// \endif
    /// </summary>
    public bool IsOn => _model.IsOn;
    /// <summary>
    /// \if KO
    /// <para>Toggle Count 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the toggle count value.</para>
    /// \endif
    /// </summary>
    public int ToggleCount => _model.ToggleCount;

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
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
