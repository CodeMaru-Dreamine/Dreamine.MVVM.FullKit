using System.Text.Json.Serialization;
using Wedding.Common;

namespace WeddingThankYou.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThankYouLayoutMode
{
    Unknown = 0,
    OnePage = 1,
    Tabs = 2,
}

/// <summary>
/// 감사장 디자인 카탈로그 — 레이아웃/테마를 한 곳에서 정의해 새 스타일 추가를 단순화합니다.
/// WeddingPlatform.Web의 InvitationDesignCatalog와 동일한 패턴을 유지합니다.
/// </summary>
public static class ThankYouDesignCatalog
{
    public static IReadOnlyList<WeddingThemeOption> Themes => WeddingThemeCatalog.Options;

    public static IReadOnlyList<WeddingLayoutOption> Layouts => WeddingLayoutCatalog.Options;

    public static WeddingLayoutOption GetLayout(WeddingLayoutMode mode) =>
        WeddingLayoutCatalog.Instance.Find(mode) ?? WeddingLayoutCatalog.Options[0];

    public static WeddingThemeOption GetTheme(string? key) =>
        WeddingThemeCatalog.Instance.Find(key) ?? WeddingThemeCatalog.Options[0];

    public static ThankYouLayoutMode FromLegacyLayoutKey(string? key) =>
        key?.Trim().ToLowerInvariant() switch
        {
            "tabs" => ThankYouLayoutMode.Tabs,
            _ => ThankYouLayoutMode.OnePage,
        };

    public static string ToLegacyLayoutKey(ThankYouLayoutMode mode) =>
        mode == ThankYouLayoutMode.Tabs ? "tabs" : "onepage";

    public static WeddingLayoutMode ToWeddingLayoutMode(ThankYouLayoutMode mode) =>
        mode == ThankYouLayoutMode.Tabs ? WeddingLayoutMode.TabMenu : WeddingLayoutMode.WebPage;

    public static ThankYouLayoutMode FromWeddingLayoutMode(WeddingLayoutMode mode) =>
        mode == WeddingLayoutMode.TabMenu ? ThankYouLayoutMode.Tabs : ThankYouLayoutMode.OnePage;

    public static string ToLegacyLayoutKey(WeddingLayoutMode mode) =>
        WeddingLayoutCatalog.ToLegacyKey(mode);

    public static WeddingLayoutMode ResolveLayoutMode(string? legacyStyle)
    {
        return WeddingLayoutCatalog.FromLegacyKey(legacyStyle);
    }

    /// <summary>테넌트 설정을 정규화 — 테마/레이아웃 값이 없거나 유효하지 않으면 기본값으로 채웁니다.</summary>
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
