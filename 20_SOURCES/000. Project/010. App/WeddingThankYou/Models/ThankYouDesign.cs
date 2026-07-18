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
        config.HeroPanelPlacement ??= new WeddingFloatingPosition();
        config.MusicButtonPlacement ??= new WeddingFloatingPosition();
        config.StoryChapters = WeddingStoryChapterDefaults.Normalize(config.StoryChapters);

        // 테마
        var themeKey = !string.IsNullOrWhiteSpace(config.ThemeName) ? config.ThemeName : "rose";
        config.ThemeName = WeddingThemeCatalog.NormalizeKey(themeKey);

        // 레이아웃
        var mode = ResolveLayoutMode(config.ThankYouStyle);
        config.ThankYouStyle = ToLegacyLayoutKey(mode);
        config.SectionOrder = WeddingSectionOrderCatalog.NormalizeThankYouOrder(config.SectionOrder);
    }
}
