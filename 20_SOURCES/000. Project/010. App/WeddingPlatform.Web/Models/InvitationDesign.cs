using System.Text.Json.Serialization;
using Wedding.Common;

namespace WeddingPlatform.Models;

/// <summary>
/// \if KO
/// <para>Design Settings 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates design settings functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DesignSettings
{
    /// <summary>
    /// \if KO
    /// <para>Theme Key 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the theme key value.</para>
    /// \endif
    /// </summary>
    public string ThemeKey { get; set; } = "rose";
    /// <summary>
    /// \if KO
    /// <para>Layout Mode 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the layout mode value.</para>
    /// \endif
    /// </summary>
    public WeddingLayoutMode LayoutMode { get; set; } = WeddingLayoutMode.WebPage;
    /// <summary>
    /// \if KO
    /// <para>Hero Placement 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero placement value.</para>
    /// \endif
    /// </summary>
    public HeroPlacement HeroPlacement { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Music Button Placement 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the music button placement value.</para>
    /// \endif
    /// </summary>
    public WeddingFloatingPosition MusicButtonPlacement { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Story Chapters 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the story chapters value.</para>
    /// \endif
    /// </summary>
    public List<StoryChapter> StoryChapters { get; set; } = WeddingStoryChapterDefaults.Create();
    /// <summary>
    /// \if KO
    /// <para>Section Order 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the section order value.</para>
    /// \endif
    /// </summary>
    public List<string> SectionOrder { get; set; } =
        WeddingSectionOrderCatalog.InvitationRecommendedOrder.ToList();
    /// <summary>
    /// \if KO
    /// <para>Section Visibility 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the section visibility value.</para>
    /// \endif
    /// </summary>
    public Dictionary<string, bool> SectionVisibility { get; set; } = new();
}

/// <summary>
/// \if KO
/// <para>Hero Placement 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hero placement functionality and related state.</para>
/// \endif
/// </summary>
public sealed class HeroPlacement
{
    /// <summary>
    /// \if KO
    /// <para>Invite Top 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite top value.</para>
    /// \endif
    /// </summary>
    public HeroPanelPlacement InviteTop { get; set; } = new("top", "center", "top", "center");
    /// <summary>
    /// \if KO
    /// <para>Invite Bottom 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the invite bottom value.</para>
    /// \endif
    /// </summary>
    public HeroPanelPlacement InviteBottom { get; set; } = new("bottom", "center", "bottom", "center");
    /// <summary>
    /// \if KO
    /// <para>Thank You 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the thank you value.</para>
    /// \endif
    /// </summary>
    public HeroPanelPlacement ThankYou { get; set; } = new("top", "center", "top", "center");
}

/// <summary>
/// \if KO
/// <para>Hero Panel Placement 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates hero panel placement functionality and related state.</para>
/// \endif
/// </summary>
public sealed class HeroPanelPlacement
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="HeroPanelPlacement"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="HeroPanelPlacement"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public HeroPanelPlacement()
    {
    }

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="HeroPanelPlacement"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="HeroPanelPlacement"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="desktopVertical">
    /// \if KO
    /// <para>desktop Vertical에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for desktop vertical.</para>
    /// \endif
    /// </param>
    /// <param name="desktopHorizontal">
    /// \if KO
    /// <para>desktop Horizontal에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for desktop horizontal.</para>
    /// \endif
    /// </param>
    /// <param name="mobileVertical">
    /// \if KO
    /// <para>mobile Vertical에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for mobile vertical.</para>
    /// \endif
    /// </param>
    /// <param name="mobileHorizontal">
    /// \if KO
    /// <para>mobile Horizontal에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for mobile horizontal.</para>
    /// \endif
    /// </param>
    public HeroPanelPlacement(string desktopVertical, string desktopHorizontal, string mobileVertical, string mobileHorizontal)
    {
        DesktopVertical = desktopVertical;
        DesktopHorizontal = desktopHorizontal;
        MobileVertical = mobileVertical;
        MobileHorizontal = mobileHorizontal;
    }

    /// <summary>
    /// \if KO
    /// <para>Desktop Vertical 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the desktop vertical value.</para>
    /// \endif
    /// </summary>
    public string DesktopVertical { get; set; } = "top";
    /// <summary>
    /// \if KO
    /// <para>Desktop Horizontal 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the desktop horizontal value.</para>
    /// \endif
    /// </summary>
    public string DesktopHorizontal { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Mobile Vertical 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mobile vertical value.</para>
    /// \endif
    /// </summary>
    public string MobileVertical { get; set; } = "top";
    /// <summary>
    /// \if KO
    /// <para>Mobile Horizontal 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mobile horizontal value.</para>
    /// \endif
    /// </summary>
    public string MobileHorizontal { get; set; } = "center";
    /// <summary>
    /// \if KO
    /// <para>Desktop X 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the desktop x value.</para>
    /// \endif
    /// </summary>
    public double? DesktopX { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Desktop Y 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the desktop y value.</para>
    /// \endif
    /// </summary>
    public double? DesktopY { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Mobile X 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mobile x value.</para>
    /// \endif
    /// </summary>
    public double? MobileX { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Mobile Y 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mobile y value.</para>
    /// \endif
    /// </summary>
    public double? MobileY { get; set; }

    /// <summary>
    /// \if KO
    /// <para>Has Desktop Custom Position 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has desktop custom position value.</para>
    /// \endif
    /// </summary>
    public bool HasDesktopCustomPosition => DesktopX.HasValue && DesktopY.HasValue;
    /// <summary>
    /// \if KO
    /// <para>Has Mobile Custom Position 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has mobile custom position value.</para>
    /// \endif
    /// </summary>
    public bool HasMobileCustomPosition => MobileX.HasValue && MobileY.HasValue;
}

/// <summary>
/// \if KO
/// <para>Invitation Design Catalog 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates invitation design catalog functionality and related state.</para>
/// \endif
/// </summary>
public static class InvitationDesignCatalog
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
    /// <para>Normalize 작업을 수행합니다.</para>
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
        config.DesignSettings ??= new DesignSettings();
        config.DesignSettings.HeroPlacement ??= new HeroPlacement();
        config.DesignSettings.HeroPlacement.InviteTop ??= new HeroPanelPlacement();
        config.DesignSettings.HeroPlacement.InviteBottom ??= new HeroPanelPlacement();
        config.DesignSettings.HeroPlacement.ThankYou ??= new HeroPanelPlacement();
        config.DesignSettings.MusicButtonPlacement ??= new WeddingFloatingPosition();
        config.DesignSettings.StoryChapters = WeddingStoryChapterDefaults.Normalize(config.DesignSettings.StoryChapters);
        config.UnlockedLayoutModes ??= new();
        config.UnlockedThemeKeys ??= new();

        var themeKey = !string.IsNullOrWhiteSpace(config.DesignSettings.ThemeKey)
            ? config.DesignSettings.ThemeKey
            : config.ThemeName;
        config.DesignSettings.ThemeKey = WeddingThemeCatalog.NormalizeKey(themeKey);
        config.ThemeName = config.DesignSettings.ThemeKey;

        config.DesignSettings.LayoutMode = ResolveLayoutMode(config.DesignSettings.LayoutMode, config.InvitationStyle);
        config.InvitationStyle = ToLegacyLayoutKey(config.DesignSettings.LayoutMode);
        config.DesignSettings.SectionOrder = WeddingSectionOrderCatalog.NormalizeInvitationOrder(
            config.DesignSettings.SectionOrder,
            GetLayout(config.DesignSettings.LayoutMode).SupportedSections);
        config.DesignSettings.SectionVisibility ??= new Dictionary<string, bool>();

        SyncPlacementFromLegacy(config);
        NormalizePlacement(config.DesignSettings.HeroPlacement.InviteTop, "top");
        NormalizePlacement(config.DesignSettings.HeroPlacement.InviteBottom, "bottom");
        NormalizePlacement(config.DesignSettings.HeroPlacement.ThankYou, "top");
        SyncLegacyFromPlacement(config);
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Layout Mode 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve layout mode operation.</para>
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
    public static WeddingLayoutMode ResolveLayoutMode(WeddingLayoutMode mode, string? legacyStyle)
    {
        if (mode != WeddingLayoutMode.Unknown && WeddingLayoutCatalog.Instance.Exists(mode))
        {
            return mode;
        }

        return FromLegacyLayoutKey(legacyStyle);
    }

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
    /// <para>From Legacy Layout Key 작업에서 생성한 <c>WeddingLayoutMode</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingLayoutMode</c> result produced by the from legacy layout key operation.</para>
    /// \endif
    /// </returns>
    public static WeddingLayoutMode FromLegacyLayoutKey(string? key) =>
        WeddingLayoutCatalog.FromLegacyKey(key);

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
    /// <para>Sync Placement From Legacy 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync placement from legacy operation.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Sync Legacy From Placement 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync legacy from placement operation.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Normalize Placement 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize placement operation.</para>
    /// \endif
    /// </summary>
    /// <param name="placement">
    /// \if KO
    /// <para>placement에 사용할 <c>HeroPanelPlacement</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HeroPanelPlacement</c> value used for placement.</para>
    /// \endif
    /// </param>
    /// <param name="verticalFallback">
    /// \if KO
    /// <para>vertical Fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for vertical fallback.</para>
    /// \endif
    /// </param>
    private static void NormalizePlacement(HeroPanelPlacement placement, string verticalFallback)
    {
        placement.DesktopVertical = NormalizeOption(placement.DesktopVertical, ["top", "middle", "bottom"], verticalFallback);
        placement.DesktopHorizontal = NormalizeOption(placement.DesktopHorizontal, ["left", "center", "right"], "center");
        placement.MobileVertical = NormalizeOption(placement.MobileVertical, ["top", "middle", "bottom"], verticalFallback);
        placement.MobileHorizontal = NormalizeOption(placement.MobileHorizontal, ["left", "center", "right"], "center");
    }

    /// <summary>
    /// \if KO
    /// <para>First Non Blank 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the first non blank operation.</para>
    /// \endif
    /// </summary>
    /// <param name="preferred">
    /// \if KO
    /// <para>preferred에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for preferred.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>First Non Blank 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the first non blank operation.</para>
    /// \endif
    /// </returns>
    private static string FirstNonBlank(string? preferred, string fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    /// <summary>
    /// \if KO
    /// <para>Normalize Option 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize option operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="allowed">
    /// \if KO
    /// <para>allowed에 사용할 <c>string[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> value used for allowed.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Option 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize option operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeOption(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) && allowed.Contains(normalized) ? normalized : fallback;
    }
}
