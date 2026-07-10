using System.Text.Json.Serialization;
using Wedding.Common;

namespace WeddingPlatform.Models;

public sealed class DesignSettings
{
    public string ThemeKey { get; set; } = "rose";
    public WeddingLayoutMode LayoutMode { get; set; } = WeddingLayoutMode.WebPage;
    public HeroPlacement HeroPlacement { get; set; } = new();
    public List<string> SectionOrder { get; set; } =
        ["hero", "story", "info", "details", "video", "gallery", "guestbook", "gift"];
    public Dictionary<string, bool> SectionVisibility { get; set; } = new();
}

public sealed class HeroPlacement
{
    public HeroPanelPlacement InviteTop { get; set; } = new("top", "center", "top", "center");
    public HeroPanelPlacement InviteBottom { get; set; } = new("bottom", "center", "bottom", "center");
    public HeroPanelPlacement ThankYou { get; set; } = new("top", "center", "top", "center");
}

public sealed class HeroPanelPlacement
{
    public HeroPanelPlacement()
    {
    }

    public HeroPanelPlacement(string desktopVertical, string desktopHorizontal, string mobileVertical, string mobileHorizontal)
    {
        DesktopVertical = desktopVertical;
        DesktopHorizontal = desktopHorizontal;
        MobileVertical = mobileVertical;
        MobileHorizontal = mobileHorizontal;
    }

    public string DesktopVertical { get; set; } = "top";
    public string DesktopHorizontal { get; set; } = "center";
    public string MobileVertical { get; set; } = "top";
    public string MobileHorizontal { get; set; } = "center";
}

public sealed record ThemeDescriptor(
    string Key,
    string Primary,
    string Accent,
    string Label);

public static class InvitationDesignCatalog
{
    public static readonly IReadOnlyList<ThemeDescriptor> Themes =
    [
        new("rose", "#c8a882", "#a07850", "로즈 골드 (기본)"),
        new("ivory", "#b8a99a", "#8a7060", "아이보리 크림"),
        new("forest", "#6b8f71", "#4a6b50", "포레스트 그린"),
        new("navy", "#3d5a80", "#98c1d9", "네이비 & 골드"),
        new("blush", "#d4a5a5", "#b07575", "블러쉬 핑크"),
    ];

    public static IReadOnlyList<WeddingLayoutOption> Layouts => WeddingLayoutCatalog.Options;

    public static WeddingLayoutOption GetLayout(WeddingLayoutMode mode) =>
        WeddingLayoutCatalog.Instance.Find(mode) ?? WeddingLayoutCatalog.Options[0];

    public static ThemeDescriptor GetTheme(string? key) =>
        Themes.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase)) ?? Themes[0];

    public static void Normalize(TenantConfig config)
    {
        config.DesignSettings ??= new DesignSettings();
        config.DesignSettings.HeroPlacement ??= new HeroPlacement();
        config.DesignSettings.HeroPlacement.InviteTop ??= new HeroPanelPlacement();
        config.DesignSettings.HeroPlacement.InviteBottom ??= new HeroPanelPlacement();
        config.DesignSettings.HeroPlacement.ThankYou ??= new HeroPanelPlacement();
        config.DesignSettings.SectionOrder ??= ["hero", "story", "info", "details", "video", "gallery", "guestbook", "gift"];
        config.DesignSettings.SectionVisibility ??= new Dictionary<string, bool>();

        var themeKey = !string.IsNullOrWhiteSpace(config.DesignSettings.ThemeKey)
            ? config.DesignSettings.ThemeKey
            : config.ThemeName;
        config.DesignSettings.ThemeKey = GetTheme(themeKey).Key;
        config.ThemeName = config.DesignSettings.ThemeKey;

        config.DesignSettings.LayoutMode = ResolveLayoutMode(config.DesignSettings.LayoutMode, config.InvitationStyle);
        config.InvitationStyle = ToLegacyLayoutKey(config.DesignSettings.LayoutMode);

        SyncPlacementFromLegacy(config);
        NormalizePlacement(config.DesignSettings.HeroPlacement.InviteTop, "top");
        NormalizePlacement(config.DesignSettings.HeroPlacement.InviteBottom, "bottom");
        NormalizePlacement(config.DesignSettings.HeroPlacement.ThankYou, "top");
        SyncLegacyFromPlacement(config);
    }

    public static WeddingLayoutMode ResolveLayoutMode(WeddingLayoutMode mode, string? legacyStyle)
    {
        if (mode != WeddingLayoutMode.Unknown && WeddingLayoutCatalog.Instance.Exists(mode))
        {
            return mode;
        }

        return FromLegacyLayoutKey(legacyStyle);
    }

    public static WeddingLayoutMode FromLegacyLayoutKey(string? key) =>
        WeddingLayoutCatalog.FromLegacyKey(key);

    public static string ToLegacyLayoutKey(WeddingLayoutMode mode) =>
        WeddingLayoutCatalog.ToLegacyKey(mode);

    private static void SyncPlacementFromLegacy(TenantConfig config)
    {
        var placement = config.DesignSettings.HeroPlacement;
        placement.InviteTop.DesktopVertical = FirstNonBlank(placement.InviteTop.DesktopVertical, config.InviteHeroTopVerticalDesktop);
        placement.InviteTop.DesktopHorizontal = FirstNonBlank(placement.InviteTop.DesktopHorizontal, config.InviteHeroTopHorizontalDesktop);
        placement.InviteTop.MobileVertical = FirstNonBlank(placement.InviteTop.MobileVertical, config.InviteHeroTopVerticalMobile);
        placement.InviteTop.MobileHorizontal = FirstNonBlank(placement.InviteTop.MobileHorizontal, config.InviteHeroTopHorizontalMobile);

        placement.InviteBottom.DesktopVertical = FirstNonBlank(placement.InviteBottom.DesktopVertical, config.InviteHeroBottomVerticalDesktop);
        placement.InviteBottom.DesktopHorizontal = FirstNonBlank(placement.InviteBottom.DesktopHorizontal, config.InviteHeroBottomHorizontalDesktop);
        placement.InviteBottom.MobileVertical = FirstNonBlank(placement.InviteBottom.MobileVertical, config.InviteHeroBottomVerticalMobile);
        placement.InviteBottom.MobileHorizontal = FirstNonBlank(placement.InviteBottom.MobileHorizontal, config.InviteHeroBottomHorizontalMobile);

        placement.ThankYou.DesktopVertical = FirstNonBlank(placement.ThankYou.DesktopVertical, config.HeroPanelVerticalDesktop);
        placement.ThankYou.DesktopHorizontal = FirstNonBlank(placement.ThankYou.DesktopHorizontal, config.HeroPanelHorizontalDesktop);
        placement.ThankYou.MobileVertical = FirstNonBlank(placement.ThankYou.MobileVertical, config.HeroPanelVerticalMobile);
        placement.ThankYou.MobileHorizontal = FirstNonBlank(placement.ThankYou.MobileHorizontal, config.HeroPanelHorizontalMobile);
    }

    private static void SyncLegacyFromPlacement(TenantConfig config)
    {
        var placement = config.DesignSettings.HeroPlacement;
        config.InviteHeroTopVerticalDesktop = placement.InviteTop.DesktopVertical;
        config.InviteHeroTopHorizontalDesktop = placement.InviteTop.DesktopHorizontal;
        config.InviteHeroTopVerticalMobile = placement.InviteTop.MobileVertical;
        config.InviteHeroTopHorizontalMobile = placement.InviteTop.MobileHorizontal;
        config.InviteHeroBottomVerticalDesktop = placement.InviteBottom.DesktopVertical;
        config.InviteHeroBottomHorizontalDesktop = placement.InviteBottom.DesktopHorizontal;
        config.InviteHeroBottomVerticalMobile = placement.InviteBottom.MobileVertical;
        config.InviteHeroBottomHorizontalMobile = placement.InviteBottom.MobileHorizontal;
        config.HeroPanelVerticalDesktop = placement.ThankYou.DesktopVertical;
        config.HeroPanelHorizontalDesktop = placement.ThankYou.DesktopHorizontal;
        config.HeroPanelVerticalMobile = placement.ThankYou.MobileVertical;
        config.HeroPanelHorizontalMobile = placement.ThankYou.MobileHorizontal;
    }

    private static void NormalizePlacement(HeroPanelPlacement placement, string verticalFallback)
    {
        placement.DesktopVertical = NormalizeOption(placement.DesktopVertical, ["top", "middle", "bottom"], verticalFallback);
        placement.DesktopHorizontal = NormalizeOption(placement.DesktopHorizontal, ["left", "center", "right"], "center");
        placement.MobileVertical = NormalizeOption(placement.MobileVertical, ["top", "middle", "bottom"], verticalFallback);
        placement.MobileHorizontal = NormalizeOption(placement.MobileHorizontal, ["left", "center", "right"], "center");
    }

    private static string FirstNonBlank(string? preferred, string fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    private static string NormalizeOption(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) && allowed.Contains(normalized) ? normalized : fallback;
    }
}
