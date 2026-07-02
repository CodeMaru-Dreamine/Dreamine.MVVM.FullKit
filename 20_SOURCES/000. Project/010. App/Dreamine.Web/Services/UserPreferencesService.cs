namespace DreamineWeb.Services;

/// <summary>SignalR 회로(사용자 세션)당 언어 등 UI 환경설정.</summary>
public class UserPreferencesService
{
    public string Language { get; private set; } = "ko";
    public bool IsKorean => Language == "ko";

    public event Action? Changed;

    public void Toggle()
    {
        Language = IsKorean ? "en" : "ko";
        Changed?.Invoke();
    }

    public void Set(string lang)
    {
        Language = lang == "en" ? "en" : "ko";
        Changed?.Invoke();
    }
}
