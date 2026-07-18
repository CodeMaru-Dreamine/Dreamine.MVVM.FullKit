namespace SampleCrossUi.Shared.ViewModels;

/// <summary>
/// \if KO
/// <para>Light Bulb Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Shared state for the light bulb sample.</para>
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
