using System.Text.Json.Serialization;
using Wedding.Common;

namespace WeddingThankYou.Models;

/// <summary>
/// \if KO
/// <para>Thank You Layout Mode 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates thank you layout mode functionality and related state.</para>
/// \endif
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThankYouLayoutMode
{
    /// <summary>
    /// \if KO
    /// <para>Unknown 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the unknown value.</para>
    /// \endif
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// \if KO
    /// <para>One Page 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the one page value.</para>
    /// \endif
    /// </summary>
    OnePage = 1,
    /// <summary>
    /// \if KO
    /// <para>Tabs 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the tabs value.</para>
    /// \endif
    /// </summary>
    Tabs = 2,
}

/// <summary>
/// \if KO
/// <para>감사장 디자인 카탈로그 — 레이아웃/테마를 한 곳에서 정의해 새 스타일 추가를 단순화합니다. WeddingPlatform.Web의 InvitationDesignCatalog와 동일한 패턴을 유지합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates thank you design catalog functionality and related state.</para>
/// \endif
/// </summary>
public static class ThankYouDesignCatalog
{
    /// <summary>
    /// \if KO
    /// <para>Themes 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the themes value.</para>
    /// \endif
    /// </summary>
    public static IReadOnlyList<WeddingThemeOption> Themes => WeddingThemeCatalog.Options;

    /// <summary>
    /// \if KO
    /// <para>Layouts 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the layouts value.</para>
    /// \endif
    /// </summary>
    public static IReadOnlyList<WeddingLayoutOption> Layouts => WeddingLayoutCatalog.Options;

    /// <summary>
    /// \if KO
    /// <para>Layout 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the layout value.</para>
    /// \endif
    /// </summary>
    /// <param name="mode">
    /// \if KO
    /// <para>mode에 사용할 <c>WeddingLayoutMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutMode</c> value used for mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Layout 작업에서 생성한 <c>WeddingLayoutOption</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutOption</c> result produced by the get layout operation.</para>
    /// \endif
    /// </returns>
    public static WeddingLayoutOption GetLayout(WeddingLayoutMode mode) =>
        WeddingLayoutCatalog.Instance.Find(mode) ?? WeddingLayoutCatalog.Options[0];

    /// <summary>
    /// \if KO
    /// <para>Theme 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the theme value.</para>
    /// \endif
    /// </summary>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Theme 작업에서 생성한 <c>WeddingThemeOption</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingThemeOption</c> result produced by the get theme operation.</para>
    /// \endif
    /// </returns>
    public static WeddingThemeOption GetTheme(string? key) =>
        WeddingThemeCatalog.Instance.Find(key) ?? WeddingThemeCatalog.Options[0];

    /// <summary>
    /// \if KO
    /// <para>From Legacy Layout Key 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the from legacy layout key operation.</para>
    /// \endif
    /// </summary>
    /// <param name="key">
    /// \if KO
    /// <para>key에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for key.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>From Legacy Layout Key 작업에서 생성한 <c>ThankYouLayoutMode</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ThankYouLayoutMode</c> result produced by the from legacy layout key operation.</para>
    /// \endif
    /// </returns>
    public static ThankYouLayoutMode FromLegacyLayoutKey(string? key) =>
        key?.Trim().ToLowerInvariant() switch
        {
            "tabs" => ThankYouLayoutMode.Tabs,
            _ => ThankYouLayoutMode.OnePage,
        };

    /// <summary>
    /// \if KO
    /// <para>To Legacy Layout Key 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to legacy layout key operation.</para>
    /// \endif
    /// </summary>
    /// <param name="mode">
    /// \if KO
    /// <para>mode에 사용할 <c>ThankYouLayoutMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ThankYouLayoutMode</c> value used for mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Legacy Layout Key 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to legacy layout key operation.</para>
    /// \endif
    /// </returns>
    public static string ToLegacyLayoutKey(ThankYouLayoutMode mode) =>
        mode == ThankYouLayoutMode.Tabs ? "tabs" : "onepage";

    /// <summary>
    /// \if KO
    /// <para>To Wedding Layout Mode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to wedding layout mode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="mode">
    /// \if KO
    /// <para>mode에 사용할 <c>ThankYouLayoutMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ThankYouLayoutMode</c> value used for mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Wedding Layout Mode 작업에서 생성한 <c>WeddingLayoutMode</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutMode</c> result produced by the to wedding layout mode operation.</para>
    /// \endif
    /// </returns>
    public static WeddingLayoutMode ToWeddingLayoutMode(ThankYouLayoutMode mode) =>
        mode == ThankYouLayoutMode.Tabs ? WeddingLayoutMode.TabMenu : WeddingLayoutMode.WebPage;

    /// <summary>
    /// \if KO
    /// <para>From Wedding Layout Mode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the from wedding layout mode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="mode">
    /// \if KO
    /// <para>mode에 사용할 <c>WeddingLayoutMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutMode</c> value used for mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>From Wedding Layout Mode 작업에서 생성한 <c>ThankYouLayoutMode</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ThankYouLayoutMode</c> result produced by the from wedding layout mode operation.</para>
    /// \endif
    /// </returns>
    public static ThankYouLayoutMode FromWeddingLayoutMode(WeddingLayoutMode mode) =>
        mode == WeddingLayoutMode.TabMenu ? ThankYouLayoutMode.Tabs : ThankYouLayoutMode.OnePage;

    /// <summary>
    /// \if KO
    /// <para>To Legacy Layout Key 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to legacy layout key operation.</para>
    /// \endif
    /// </summary>
    /// <param name="mode">
    /// \if KO
    /// <para>mode에 사용할 <c>WeddingLayoutMode</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutMode</c> value used for mode.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Legacy Layout Key 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to legacy layout key operation.</para>
    /// \endif
    /// </returns>
    public static string ToLegacyLayoutKey(WeddingLayoutMode mode) =>
        WeddingLayoutCatalog.ToLegacyKey(mode);

    /// <summary>
    /// \if KO
    /// <para>Resolve Layout Mode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve layout mode operation.</para>
    /// \endif
    /// </summary>
    /// <param name="legacyStyle">
    /// \if KO
    /// <para>legacy Style에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for legacy style.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Layout Mode 작업에서 생성한 <c>WeddingLayoutMode</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutMode</c> result produced by the resolve layout mode operation.</para>
    /// \endif
    /// </returns>
    public static WeddingLayoutMode ResolveLayoutMode(string? legacyStyle)
    {
        return WeddingLayoutCatalog.FromLegacyKey(legacyStyle);
    }

    /// <summary>
    /// \if KO
    /// <para>테넌트 설정을 정규화 — 테마/레이아웃 값이 없거나 유효하지 않으면 기본값으로 채웁니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    public static void Normalize(TenantConfig config)
    {
        config.UnlockedLayoutModes ??= new();
        config.UnlockedThemeKeys ??= new();
        config.SectionOrder ??= WeddingSectionOrderCatalog.ThankYouRecommendedOrder.ToList();
        config.SectionVisibility ??= new(StringComparer.OrdinalIgnoreCase);
        config.HeroPanelPlacement ??= new WeddingFloatingPosition();
        config.HeroTopPanelPlacement ??= CloneFloatingPosition(config.HeroPanelPlacement);
        config.HeroBottomPanelPlacement ??= new WeddingFloatingPosition();
        config.MusicButtonPlacement ??= new WeddingFloatingPosition();
        config.CustomTheme ??= new CustomWeddingThemeSettings();
        config.HeroDesktopCrop ??= new HeroImageCropRegion();
        config.HeroMobileCrop ??= new HeroImageCropRegion();
        config.CeremonyNote = NormalizeCeremonyNoteLineBreaks(config.CeremonyNote);
        NormalizeCustomTheme(config.CustomTheme);
        NormalizeHeroImagePresentation(config);
        config.StoryChapters = WeddingStoryChapterDefaults.Normalize(config.StoryChapters);
        config.PhotoBookPages = WeddingPhotoBookPageDefaults.Normalize(config.PhotoBookPages);
        config.CardHighlights = WeddingCardHighlightDefaults.Normalize(config.CardHighlights);
        // Wedding의 기본 카드는 info/details/gift를 대상으로 하지만 감사장은
        // message/gallery/guestbook가 대응 섹션이다. 신규·기본 데이터만 의미에 맞게 이관한다.
        foreach (var highlight in config.CardHighlights)
        {
            highlight.SectionKey = highlight.SectionKey switch
            {
                "info" => "message",
                "details" => "gallery",
                "gift" => "guestbook",
                _ => highlight.SectionKey,
            };
        }

        // 테마
        var themeKey = !string.IsNullOrWhiteSpace(config.ThemeName) ? config.ThemeName : "rose";
        config.ThemeName = WeddingThemeCatalog.NormalizeKey(themeKey);

        // 레이아웃
        var mode = ResolveLayoutMode(config.ThankYouStyle);
        config.ThankYouStyle = ToLegacyLayoutKey(mode);
        config.SectionOrder = WeddingSectionOrderCatalog.NormalizeThankYouOrder(config.SectionOrder);
        config.SectionVisibility = config.SectionVisibility
            .Where(pair => config.SectionOrder.Contains(pair.Key, StringComparer.OrdinalIgnoreCase)
                && !string.Equals(pair.Key, "hero", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static WeddingFloatingPosition CloneFloatingPosition(WeddingFloatingPosition source) => new()
    {
        DesktopX = source.DesktopX,
        DesktopY = source.DesktopY,
        MobileX = source.MobileX,
        MobileY = source.MobileY,
    };

    private static string NormalizeCeremonyNoteLineBreaks(string? value)
    {
        // Older records stored textarea line breaks as numeric HTML entities. Decode only
        // CR/LF entities here so HTML ceremony-note content otherwise remains untouched.
        var normalized = (value ?? string.Empty)
            .Replace("&#10;", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("&#xA;", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("&#x0A;", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("&#13;", "\r", StringComparison.OrdinalIgnoreCase)
            .Replace("&#xD;", "\r", StringComparison.OrdinalIgnoreCase)
            .Replace("&#x0D;", "\r", StringComparison.OrdinalIgnoreCase);

        return normalized
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static void NormalizeHeroImagePresentation(TenantConfig config)
    {
        config.HeroDesktopFit = NormalizeHeroFit(config.HeroDesktopFit);
        config.HeroMobileFit = NormalizeHeroFit(config.HeroMobileFit);
        config.HeroDesktopFocusX = NormalizePercent(config.HeroDesktopFocusX);
        config.HeroDesktopFocusY = NormalizePercent(config.HeroDesktopFocusY);
        config.HeroMobileFocusX = NormalizePercent(config.HeroMobileFocusX);
        config.HeroMobileFocusY = NormalizePercent(config.HeroMobileFocusY);
        NormalizeCropRegion(config.HeroDesktopCrop);
        NormalizeCropRegion(config.HeroMobileCrop);
    }

    private static string NormalizeHeroFit(string? value) =>
        string.Equals(value, "cover", StringComparison.OrdinalIgnoreCase) ? "cover" : "contain";

    private static double NormalizePercent(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 50;

    private static void NormalizeCropRegion(HeroImageCropRegion crop)
    {
        crop.X = double.IsFinite(crop.X) ? Math.Clamp(crop.X, 0, 95) : 0;
        crop.Y = double.IsFinite(crop.Y) ? Math.Clamp(crop.Y, 0, 95) : 0;
        crop.Width = double.IsFinite(crop.Width) ? Math.Clamp(crop.Width, 5, 100 - crop.X) : 100 - crop.X;
        crop.Height = double.IsFinite(crop.Height) ? Math.Clamp(crop.Height, 5, 100 - crop.Y) : 100 - crop.Y;
    }

    /// <summary>
    /// 기존 5색 JSON과 새 10토큰 JSON을 모두 안전한 6자리 HEX 값으로 정규화합니다.
    /// 이미 유효한 사용자 색은 보존하고, 없거나 잘못된 확장 토큰만 대표색 기반
    /// WCAG 팔레트 값으로 채웁니다.
    /// </summary>
    public static void NormalizeCustomTheme(CustomWeddingThemeSettings custom)
    {
        ArgumentNullException.ThrowIfNull(custom);

        var baseFallback = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Primary,
            WeddingThemePaletteGenerator.DefaultBaseColor);
        var baseColor = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.BaseColor,
            baseFallback);
        var generated = WeddingThemePaletteGenerator.Generate(baseColor);

        var name = custom.Name?.Trim() ?? "";
        custom.Name = string.IsNullOrWhiteSpace(name)
            ? "나만의 테마"
            : name[..Math.Min(name.Length, 40)];
        custom.BaseColor = baseColor;
        custom.Primary = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Primary,
            generated.Primary);
        custom.Dark = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Dark,
            generated.Dark);
        custom.Accent = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Accent,
            generated.Accent);
        custom.Text = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Text,
            generated.Text);
        custom.MutedText = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.MutedText,
            generated.MutedText);
        custom.Background = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Background,
            generated.Background);
        custom.PanelBackground = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.PanelBackground,
            generated.PanelBackground);
        custom.ButtonBackground = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.ButtonBackground,
            generated.ButtonBackground);
        custom.ButtonText = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.ButtonText,
            generated.ButtonText);
        custom.Border = WeddingThemePaletteGenerator.NormalizeHexColor(
            custom.Border,
            generated.Border);
    }
}
