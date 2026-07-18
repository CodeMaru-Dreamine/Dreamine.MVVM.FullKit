namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>SignalR 회로(사용자 세션)당 언어 등 UI 환경설정.</para>
/// \endif
/// \if EN
/// <para>Encapsulates user preferences service functionality and related state.</para>
/// \endif
/// </summary>
public class UserPreferencesService
{
    /// <summary>
    /// \if KO
    /// <para>Language 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the language value.</para>
    /// \endif
    /// </summary>
    public string Language { get; private set; } = "ko";
    /// <summary>
    /// \if KO
    /// <para>Is Korean 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is korean value.</para>
    /// \endif
    /// </summary>
    public bool IsKorean => Language == "ko";

    /// <summary>
    /// \if KO
    /// <para>Changed 상황이 발생할 때 알립니다.</para>
    /// \endif
    /// \if EN
    /// <para>Occurs when changed takes place.</para>
    /// \endif
    /// </summary>
    public event Action? Changed;

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
        Language = IsKorean ? "en" : "ko";
        Changed?.Invoke();
    }

    /// <summary>
    /// \if KO
    /// <para>값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the value.</para>
    /// \endif
    /// </summary>
    /// <param name="lang">
    /// \if KO
    /// <para>lang에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for lang.</para>
    /// \endif
    /// </param>
    public void Set(string lang)
    {
        Language = lang == "en" ? "en" : "ko";
        Changed?.Invoke();
    }
}
