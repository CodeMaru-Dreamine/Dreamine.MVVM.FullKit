namespace WeddingPlatform.Models;

/// <summary>
/// 렌더러가 사용하는 완전한 사용자 정의 테마 팔레트입니다.
/// </summary>
public sealed record WeddingThemePalette(
    string BaseColor,
    string Primary,
    string Dark,
    string Accent,
    string Text,
    string MutedText,
    string Background,
    string PanelBackground,
    string ButtonBackground,
    string ButtonText,
    string Border);

/// <summary>
/// 저장하지 않은 관리자 테마를 iframe 요청에만 전달하는 미리보기 값입니다.
/// </summary>
public sealed record WeddingThemePreviewSelection(
    string ThemeKey,
    WeddingThemePalette? Palette);

/// <summary>
/// 대표 색상으로부터 WCAG 대비를 고려한 밝은 청첩장 팔레트를 생성합니다.
/// 기존 사용자 정의 테마의 필수 5색은 그대로 해석하고, 새 선택 토큰이 없을 때만
/// 이전 렌더링 규칙으로 폴백합니다.
/// </summary>
public static class WeddingThemePaletteGenerator
{
    public const string DefaultBaseColor = "#c8a882";

    /// <summary>
    /// 사용자가 고른 대표 색상을 유지하면서 텍스트·버튼·경계선 대비를 자동 보정합니다.
    /// </summary>
    public static WeddingThemePalette Generate(string? baseColor)
    {
        var normalizedBase = NormalizeHexColor(baseColor, DefaultBaseColor);
        var source = Rgb.Parse(normalizedBase);
        var white = new Rgb(255, 255, 255);
        var black = new Rgb(0, 0, 0);

        // 청첩장은 밝은 종이 질감을 유지하되 대표 색상의 색조를 아주 옅게 반영합니다.
        var panel = Mix(white, source, .035);
        var background = Mix(white, source, .12);

        // --w-primary는 텍스트와 아이콘에도 사용되므로 표면 대비 3:1을 확보합니다.
        var primary = EnsureContrast(source, panel, 3.0);
        var accent = EnsureContrast(Mix(source, black, .10), panel, 4.5);
        var dark = EnsureContrast(Mix(source, black, .74), panel, 7.0);
        var text = EnsureContrast(Mix(source, black, .78), background, 7.0);
        var muted = EnsureContrast(Mix(background, text, .62), background, 4.5);
        var border = EnsureContrast(Mix(panel, text, .22), panel, 3.0);

        // 어느 대표 색상을 골라도 흰색/검정 중 더 높은 쪽을 자동 선택하면
        // 일반 크기 버튼 텍스트의 4.5:1 대비를 확보할 수 있습니다.
        var whiteContrast = ContrastRatio(source, white);
        var blackContrast = ContrastRatio(source, black);
        var buttonText = whiteContrast >= blackContrast ? white : black;

        return new WeddingThemePalette(
            normalizedBase,
            primary.ToHex(),
            dark.ToHex(),
            accent.ToHex(),
            text.ToHex(),
            muted.ToHex(),
            background.ToHex(),
            panel.ToHex(),
            normalizedBase,
            buttonText.ToHex(),
            border.ToHex());
    }

    /// <summary>
    /// 자동 생성 팔레트를 기존 DTO와 선택 확장 토큰에 모두 복사합니다.
    /// </summary>
    public static void ApplyGenerated(CustomWeddingThemeSettings target, string? baseColor)
    {
        ArgumentNullException.ThrowIfNull(target);
        var palette = Generate(baseColor);
        target.BaseColor = palette.BaseColor;
        target.Primary = palette.Primary;
        target.Dark = palette.Dark;
        target.Accent = palette.Accent;
        target.Text = palette.Text;
        target.MutedText = palette.MutedText;
        target.Background = palette.Background;
        target.PanelBackground = palette.PanelBackground;
        target.ButtonBackground = palette.ButtonBackground;
        target.ButtonText = palette.ButtonText;
        target.Border = palette.Border;
    }

    /// <summary>
    /// 공개 렌더링용 팔레트입니다. 새 토큰이 없는 기존 JSON은 종전 규칙과 같은 값으로 폴백합니다.
    /// </summary>
    public static WeddingThemePalette ResolveForRendering(CustomWeddingThemeSettings? settings)
    {
        var custom = settings ?? new CustomWeddingThemeSettings();
        var primary = NormalizeHexColor(custom.Primary, DefaultBaseColor);
        var dark = NormalizeHexColor(custom.Dark, "#3a2e28");
        var text = NormalizeHexColor(custom.Text, dark);
        var background = NormalizeHexColor(custom.Background, "#f4e8d4");
        var panel = NormalizeHexColor(custom.PanelBackground, "#fffaf3");

        return new WeddingThemePalette(
            NormalizeHexColor(custom.BaseColor, primary),
            primary,
            dark,
            NormalizeHexColor(custom.Accent, primary),
            text,
            NormalizeHexColor(custom.MutedText, text),
            background,
            panel,
            NormalizeHexColor(custom.ButtonBackground, primary),
            NormalizeHexColor(custom.ButtonText, "#ffffff"),
            TryNormalizeHexColor(custom.Border, out var border) ? border : primary + "52");
    }

    /// <summary>
    /// 색상 입력기에 표시할 팔레트입니다. 8자리 알파 경계선 폴백만 실제 표면색과 합성한 6자리 HEX로 변환합니다.
    /// </summary>
    public static WeddingThemePalette ResolveForEditing(CustomWeddingThemeSettings? settings)
    {
        var palette = ResolveForRendering(settings);
        if (palette.Border.Length == 7)
        {
            return palette;
        }

        var primary = Rgb.Parse(palette.Primary);
        var panel = Rgb.Parse(palette.PanelBackground);
        return palette with { Border = Mix(panel, primary, .32).ToHex() };
    }

    public static bool TryNormalizeHexColor(string? value, out string normalized)
    {
        normalized = value?.Trim().ToLowerInvariant() ?? "";
        return normalized.Length == 7
            && normalized[0] == '#'
            && normalized.AsSpan(1).ToString().All(Uri.IsHexDigit);
    }

    public static string NormalizeHexColor(string? value, string fallback) =>
        TryNormalizeHexColor(value, out var normalized)
            ? normalized
            : fallback;

    public static double ContrastRatio(string foreground, string background) =>
        ContrastRatio(Rgb.Parse(
            NormalizeHexColor(foreground, "#000000")),
            Rgb.Parse(NormalizeHexColor(background, "#ffffff")));

    private static Rgb EnsureContrast(Rgb candidate, Rgb background, double minimum)
    {
        if (ContrastRatio(candidate, background) >= minimum)
        {
            return candidate;
        }

        var black = new Rgb(0, 0, 0);
        var white = new Rgb(255, 255, 255);
        var endpoint = ContrastRatio(black, background) >= ContrastRatio(white, background)
            ? black
            : white;

        for (var step = 1; step <= 100; step++)
        {
            var adjusted = Mix(candidate, endpoint, step / 100d);
            if (ContrastRatio(adjusted, background) >= minimum)
            {
                return adjusted;
            }
        }

        return endpoint;
    }

    private static Rgb Mix(Rgb background, Rgb foreground, double foregroundAmount)
    {
        var amount = Math.Clamp(foregroundAmount, 0d, 1d);
        return new Rgb(
            Blend(background.Red, foreground.Red, amount),
            Blend(background.Green, foreground.Green, amount),
            Blend(background.Blue, foreground.Blue, amount));
    }

    private static byte Blend(byte background, byte foreground, double foregroundAmount) =>
        (byte)Math.Clamp(
            (int)Math.Round(background + ((foreground - background) * foregroundAmount)),
            0,
            255);

    private static double ContrastRatio(Rgb first, Rgb second)
    {
        var lighter = Math.Max(first.RelativeLuminance, second.RelativeLuminance);
        var darker = Math.Min(first.RelativeLuminance, second.RelativeLuminance);
        return (lighter + .05) / (darker + .05);
    }

    private readonly record struct Rgb(byte Red, byte Green, byte Blue)
    {
        public double RelativeLuminance =>
            (.2126 * Linear(Red)) + (.7152 * Linear(Green)) + (.0722 * Linear(Blue));

        public static Rgb Parse(string value) =>
            new(
                Convert.ToByte(value.Substring(1, 2), 16),
                Convert.ToByte(value.Substring(3, 2), 16),
                Convert.ToByte(value.Substring(5, 2), 16));

        public string ToHex() => $"#{Red:x2}{Green:x2}{Blue:x2}";

        private static double Linear(byte channel)
        {
            var normalized = channel / 255d;
            return normalized <= .04045
                ? normalized / 12.92
                : Math.Pow((normalized + .055) / 1.055, 2.4);
        }
    }
}
