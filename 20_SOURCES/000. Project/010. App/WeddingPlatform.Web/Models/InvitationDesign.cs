using System.Text.Json.Serialization;
using Wedding.Common;

namespace WeddingPlatform.Models;

/// <summary>
/// Premium 사용자가 직접 구성하는 청첩장 색상 토큰입니다.
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
    /// 값이 없는 구버전 테마는 기존 Primary/Text 기반 동작을 그대로 사용합니다.
    /// </summary>
    public string? Accent { get; set; }
    public string? MutedText { get; set; }
    public string? ButtonBackground { get; set; }
    public string? ButtonText { get; set; }
    public string? Border { get; set; }
}

/// <summary>
/// 히어로 사진을 화면 크기별로 표시하는 방법입니다.
/// contain은 사진 전체를 보존하고, cover는 영역을 채우되 초점 위치를 기준으로 자릅니다.
/// </summary>
public sealed class HeroImagePresentationSettings
{
    public const string Contain = "contain";
    public const string Cover = "cover";

    public string DesktopFit { get; set; } = Contain;
    public string MobileFit { get; set; } = Contain;
    public double DesktopFocusX { get; set; } = 50;
    public double DesktopFocusY { get; set; } = 50;
    public double MobileFocusX { get; set; } = 50;
    public double MobileFocusY { get; set; } = 50;
    public HeroImageCropRegion DesktopCrop { get; set; } = new();
    public HeroImageCropRegion MobileCrop { get; set; } = new();
}

/// <summary>
/// 원본 이미지 안에서 사용할 영역을 백분율 좌표로 보관합니다.
/// 원본 해상도와 무관하므로 서버 재시작이나 이미지 파생 파일 없이 즉시 적용할 수 있습니다.
/// </summary>
public sealed class HeroImageCropRegion
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 100;
}

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
    /// 레이아웃 버전과 독립적으로 유지되는 사용자 정의 색상 토큰입니다.
    /// </summary>
    public CustomWeddingThemeSettings CustomTheme { get; set; } = new();
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
    /// 카탈로그에서 사용하는 영구 레이아웃 키입니다.
    /// 기존 데이터는 LayoutMode/InvitationStyle에서 자동 해석되며 사용자 패키지 키도 그대로 보존됩니다.
    /// </summary>
    public string LayoutKey { get; set; } = "";

    /// <summary>
    /// 마지막으로 확인·저장된 불변 레이아웃 버전입니다.
    /// FollowActiveLayoutVersion=false일 때는 이 버전에 고정됩니다.
    /// </summary>
    public string LayoutVersion { get; set; } = "";

    /// <summary>
    /// true이면 카탈로그의 원자적 활성 포인터를 따라가므로 활성화와 롤백이
    /// 서버 재시작 없이 즉시 반영됩니다. false이면 LayoutVersion에 고정됩니다.
    /// </summary>
    public bool FollowActiveLayoutVersion { get; set; }
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
    /// 레이아웃 종류와 무관하게 모든 히어로 사진이 공유하는 PC/폰 맞춤·초점 설정입니다.
    /// </summary>
    public HeroImagePresentationSettings HeroImagePresentation { get; set; } = new();

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
    /// <para>포토북 레이아웃 전용 페이지 목록입니다. 다른 레이아웃에서는 사용되지 않으며 데이터는 유지됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Photobook-only page list. Ignored by other layouts but the data is preserved.</para>
    /// \endif
    /// </summary>
    public List<PhotoBookPage> PhotoBookPages { get; set; } = WeddingPhotoBookPageDefaults.Create();

    /// <summary>
    /// \if KO
    /// <para>카드 레이아웃 전용 강조 카드 목록입니다. 다른 레이아웃에서는 사용되지 않으며 데이터는 유지됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Card-layout-only highlight list. Ignored by other layouts but the data is preserved.</para>
    /// \endif
    /// </summary>
    public List<CardHighlight> CardHighlights { get; set; } = WeddingCardHighlightDefaults.Create();
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
        config.DesignSettings.HeroImagePresentation ??= new HeroImagePresentationSettings();
        config.DesignSettings.HeroImagePresentation.DesktopCrop ??= new HeroImageCropRegion();
        config.DesignSettings.HeroImagePresentation.MobileCrop ??= new HeroImageCropRegion();
        config.DesignSettings.MusicButtonPlacement ??= new WeddingFloatingPosition();
        config.DesignSettings.CustomTheme ??= new CustomWeddingThemeSettings();
        config.DesignSettings.StoryChapters = WeddingStoryChapterDefaults.Normalize(config.DesignSettings.StoryChapters);
        config.DesignSettings.PhotoBookPages = WeddingPhotoBookPageDefaults.Normalize(config.DesignSettings.PhotoBookPages);
        config.DesignSettings.CardHighlights = WeddingCardHighlightDefaults.Normalize(config.DesignSettings.CardHighlights);
        config.UnlockedLayoutModes ??= new();
        config.UnlockedThemeKeys ??= new();

        // 이전 버전은 Premium 레이아웃을 계정별 목록으로 허용했습니다.
        // 기존 고객이 업그레이드 후 권한을 잃지 않도록 목록이 남아 있으면
        // 계정 단위 Premium으로 한 번 승격한 뒤 호환 필드는 비웁니다.
        if (!config.HasPremiumPlan && config.UnlockedLayoutModes.Count > 0)
        {
            config.HasPremiumPlan = true;
        }
        config.UnlockedLayoutModes.Clear();

        var themeKey = !string.IsNullOrWhiteSpace(config.DesignSettings.ThemeKey)
            ? config.DesignSettings.ThemeKey
            : config.ThemeName;
        config.DesignSettings.ThemeKey =
            string.Equals(themeKey?.Trim(), WeddingThemeCatalog.CustomThemeKey, StringComparison.OrdinalIgnoreCase)
                ? WeddingThemeCatalog.CustomThemeKey
                : WeddingThemeCatalog.NormalizeKey(themeKey);
        config.ThemeName = config.DesignSettings.ThemeKey;

        var layoutKey = string.IsNullOrWhiteSpace(config.DesignSettings.LayoutKey)
            ? WeddingLayoutCatalog.ToLegacyKey(
                ResolveLayoutMode(config.DesignSettings.LayoutMode, config.InvitationStyle))
            : WeddingLayoutKeys.Normalize(config.DesignSettings.LayoutKey);

        if (!WeddingLayoutKeys.IsValid(layoutKey))
        {
            layoutKey = WeddingLayoutKeys.OnePage;
        }

        config.DesignSettings.LayoutKey = layoutKey;
        var builtInDescriptor = WeddingLayoutCatalog.Instance.FindDescriptor(layoutKey);
        if (builtInDescriptor is not null)
        {
            config.DesignSettings.LayoutMode = builtInDescriptor.LegacyMode;
            config.DesignSettings.LayoutVersion =
                WeddingLayoutVersion.IsValid(config.DesignSettings.LayoutVersion)
                    && WeddingLayoutCatalog.Instance.FindRelease(layoutKey, config.DesignSettings.LayoutVersion) is not null
                    ? config.DesignSettings.LayoutVersion.Trim()
                    : builtInDescriptor.CurrentVersion;
        }
        else
        {
            // 구버전 애플리케이션은 사용자 패키지를 렌더링할 수 없으므로 안전한 내장 fallback을 유지합니다.
            config.DesignSettings.LayoutMode = WeddingLayoutMode.Unknown;
            if (!WeddingLayoutVersion.IsValid(config.DesignSettings.LayoutVersion))
            {
                config.DesignSettings.LayoutVersion = "";
            }
        }

        config.InvitationStyle = ToLegacyLayoutKey(config.DesignSettings.LayoutMode);
        var supportedSections = builtInDescriptor is null
            ? WeddingSectionOrderCatalog.InvitationRecommendedOrder
            : GetLayout(config.DesignSettings.LayoutMode).SupportedSections;
        config.DesignSettings.SectionOrder = WeddingSectionOrderCatalog.NormalizeInvitationOrder(
            config.DesignSettings.SectionOrder,
            supportedSections);
        config.DesignSettings.SectionVisibility ??= new Dictionary<string, bool>();

        SyncPlacementFromLegacy(config);
        NormalizePlacement(config.DesignSettings.HeroPlacement.InviteTop, "top");
        NormalizePlacement(config.DesignSettings.HeroPlacement.InviteBottom, "bottom");
        NormalizePlacement(config.DesignSettings.HeroPlacement.ThankYou, "top");
        NormalizeHeroImagePresentation(config.DesignSettings.HeroImagePresentation);
        SyncLegacyFromPlacement(config);
    }

    /// <summary>
    /// Applies an exact immutable layout release and pins the tenant to it.
    /// Following the active pointer is a separate, explicit opt-in made after
    /// the exact release has been applied.
    /// </summary>
    public static void ApplyLayoutSelection(
        TenantConfig config,
        WeddingLayoutOption option)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(option);

        config.DesignSettings ??= new DesignSettings();
        config.DesignSettings.LayoutMode = option.Mode;
        config.DesignSettings.LayoutKey = option.Key;
        config.DesignSettings.LayoutVersion = option.Version;
        config.DesignSettings.FollowActiveLayoutVersion = false;

        config.InvitationStyle = option.Mode == WeddingLayoutMode.Unknown
            ? WeddingLayoutKeys.OnePage
            : ToLegacyLayoutKey(option.Mode);
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

    private static void NormalizeHeroImagePresentation(HeroImagePresentationSettings presentation)
    {
        presentation.DesktopFit = NormalizeOption(
            presentation.DesktopFit,
            [HeroImagePresentationSettings.Contain, HeroImagePresentationSettings.Cover],
            HeroImagePresentationSettings.Contain);
        presentation.MobileFit = NormalizeOption(
            presentation.MobileFit,
            [HeroImagePresentationSettings.Contain, HeroImagePresentationSettings.Cover],
            HeroImagePresentationSettings.Contain);
        presentation.DesktopFocusX = NormalizePercent(presentation.DesktopFocusX);
        presentation.DesktopFocusY = NormalizePercent(presentation.DesktopFocusY);
        presentation.MobileFocusX = NormalizePercent(presentation.MobileFocusX);
        presentation.MobileFocusY = NormalizePercent(presentation.MobileFocusY);
        NormalizeCropRegion(presentation.DesktopCrop);
        NormalizeCropRegion(presentation.MobileCrop);
    }

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
