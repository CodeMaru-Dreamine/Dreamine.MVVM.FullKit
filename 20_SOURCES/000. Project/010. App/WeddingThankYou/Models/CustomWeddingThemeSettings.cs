namespace WeddingThankYou.Models;

/// <summary>
/// Premium 사용자가 직접 구성하는 감사장 색상 토큰입니다.
/// 값은 저장 시 6자리 HEX 색상으로 검증됩니다.
/// </summary>
public sealed class CustomWeddingThemeSettings
{
    public string Name { get; set; } = "나만의 테마";

    /// <summary>
    /// 자동 팔레트를 만들 때 사용자가 고른 원본 대표 색상입니다.
    /// 기존 데이터에는 이 필드가 없으므로 Primary를 대표 색상으로 사용합니다.
    /// </summary>
    public string? BaseColor { get; set; }

    public string Primary { get; set; } = "#c8a882";
    public string Dark { get; set; } = "#3a2e28";
    public string Text { get; set; } = "#3a2e28";
    public string Background { get; set; } = "#f4e8d4";
    public string PanelBackground { get; set; } = "#fffaf3";

    /// <summary>
    /// 아래 토큰은 기존 JSON과의 하위 호환을 위해 선택 값입니다.
    /// 값이 없는 구버전 테마는 Primary/Text 기반 기본값으로 보완됩니다.
    /// </summary>
    public string? Accent { get; set; }
    public string? MutedText { get; set; }
    public string? ButtonBackground { get; set; }
    public string? ButtonText { get; set; }
    public string? Border { get; set; }
}
