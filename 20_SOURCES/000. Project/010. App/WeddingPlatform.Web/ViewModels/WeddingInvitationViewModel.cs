using Markdig;
using Wedding.Common;
using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

/// <summary>
/// \if KO
/// <para>Wedding Invitation View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding invitation view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WeddingInvitationViewModel
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly ITenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>photos 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the photos value.</para>
    /// \endif
    /// </summary>
    private readonly IPhotoService _photos;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="WeddingInvitationViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="WeddingInvitationViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>ITenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ITenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="photos">
    /// \if KO
    /// <para>photos에 사용할 <c>IPhotoService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPhotoService</c> value used for photos.</para>
    /// \endif
    /// </param>
    public WeddingInvitationViewModel(ITenantStore tenants, IPhotoService photos)
    {
        _tenants = tenants;
        _photos = photos;
    }

    /// <summary>
    /// \if KO
    /// <para>Config 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the config value.</para>
    /// \endif
    /// </summary>
    public TenantConfig? Config { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>그리드 표시용 — 최신 10개</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the gallery value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<PhotoInfo> Gallery { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>라이트박스/자동재생용 — 전체</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the all photos value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<PhotoInfo> AllPhotos { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Is Loaded 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is loaded value.</para>
    /// \endif
    /// </summary>
    public bool IsLoaded { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Not Found 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the not found value.</para>
    /// \endif
    /// </summary>
    public bool NotFound { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Couple Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the couple name value.</para>
    /// \endif
    /// </summary>
    public string CoupleName => Config?.CoupleName ?? "";
    /// <summary>
    /// \if KO
    /// <para>Hero Title 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero title value.</para>
    /// \endif
    /// </summary>
    public string HeroTitle => string.IsNullOrWhiteSpace(Config?.HeroTitle) ? "Save The Date" : Config.HeroTitle;
    /// <summary>
    /// \if KO
    /// <para>Video Urls 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video urls value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> VideoUrls => Config?.VideoFileNames
        .Select(fn => _photos.GetVideoUrl(Config.Slug, fn))
        .ToList() ?? [];
    /// <summary>
    /// \if KO
    /// <para>Gallery Auto Play Ms 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the gallery auto play ms value.</para>
    /// \endif
    /// </summary>
    public int GalleryAutoPlayMs => Math.Clamp(Config?.GalleryAutoPlaySeconds ?? 3, 1, 30) * 1000;
    /// <summary>
    /// \if KO
    /// <para>Subtitle 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the subtitle value.</para>
    /// \endif
    /// </summary>
    public string Subtitle => Config?.Subtitle ?? "";
    /// <summary>
    /// \if KO
    /// <para>Wedding Date 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the wedding date value.</para>
    /// \endif
    /// </summary>
    public DateTime WeddingDate => Config?.WeddingDate ?? DateTime.Today;
    /// <summary>
    /// \if KO
    /// <para>Wedding Time 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the wedding time value.</para>
    /// \endif
    /// </summary>
    public string WeddingTime => Config?.WeddingTime ?? "";
    /// <summary>
    /// \if KO
    /// <para>Venue Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the venue name value.</para>
    /// \endif
    /// </summary>
    public string VenueName => Config?.VenueName ?? "";
    /// <summary>
    /// \if KO
    /// <para>Venue Address 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the venue address value.</para>
    /// \endif
    /// </summary>
    public string VenueAddress => Config?.VenueAddress ?? "";
    /// <summary>
    /// \if KO
    /// <para>Story 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the story value.</para>
    /// \endif
    /// </summary>
    public string Story => Config?.Story ?? "";
    /// <summary>
    /// \if KO
    /// <para>Story2 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the story2 value.</para>
    /// \endif
    /// </summary>
    public string Story2 => Config?.Story2 ?? "";
    /// <summary>
    /// \if KO
    /// <para>Mode 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the mode value.</para>
    /// \endif
    /// </summary>
    public WeddingSiteMode Mode => Config?.Mode ?? WeddingSiteMode.Invite;
    /// <summary>
    /// \if KO
    /// <para>Show Thank You Link 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the show thank you link value.</para>
    /// \endif
    /// </summary>
    public bool ShowThankYouLink => Mode == WeddingSiteMode.Both;
    /// <summary>
    /// \if KO
    /// <para>Thank You Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thank you url value.</para>
    /// \endif
    /// </summary>
    public string ThankYouUrl => Config?.ThankYouUrl ?? "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Kakao 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the map link kakao value.</para>
    /// \endif
    /// </summary>
    public string MapLinkKakao => Config?.MapLinkKakao ?? "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Naver 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the map link naver value.</para>
    /// \endif
    /// </summary>
    public string MapLinkNaver => Config?.MapLinkNaver ?? "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Atlan 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the map link atlan value.</para>
    /// \endif
    /// </summary>
    public string MapLinkAtlan => Config?.MapLinkAtlan ?? "";
    /// <summary>
    /// \if KO
    /// <para>Map Link Tmap 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the map link tmap value.</para>
    /// \endif
    /// </summary>
    public string MapLinkTmap => Config?.MapLinkTmap ?? "";
    /// <summary>
    /// \if KO
    /// <para>Venue Lat 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the venue lat value.</para>
    /// \endif
    /// </summary>
    public double VenueLat => Config?.VenueLat ?? 0;
    /// <summary>
    /// \if KO
    /// <para>Venue Lng 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the venue lng value.</para>
    /// \endif
    /// </summary>
    public double VenueLng => Config?.VenueLng ?? 0;
    /// <summary>
    /// \if KO
    /// <para>Has Venue Coords 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has venue coords value.</para>
    /// \endif
    /// </summary>
    public bool HasVenueCoords => VenueLat != 0 && VenueLng != 0;
    /// <summary>
    /// \if KO
    /// <para>Design Settings 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the design settings value.</para>
    /// \endif
    /// </summary>
    public DesignSettings DesignSettings => Config?.DesignSettings ?? new DesignSettings();
    /// <summary>
    /// \if KO
    /// <para>Story Chapters 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the story chapters value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<StoryChapter> StoryChapters => DesignSettings.StoryChapters;
    /// <summary>
    /// \if KO
    /// <para>Theme Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the theme name value.</para>
    /// \endif
    /// </summary>
    public string ThemeName => InvitationDesignCatalog.GetTheme(DesignSettings.ThemeKey).Key;
    /// <summary>
    /// \if KO
    /// <para>Layout Mode 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the layout mode value.</para>
    /// \endif
    /// </summary>
    public WeddingLayoutMode LayoutMode => DesignSettings.LayoutMode == WeddingLayoutMode.Unknown
        ? InvitationDesignCatalog.FromLegacyLayoutKey(Config?.InvitationStyle)
        : DesignSettings.LayoutMode;
    /// <summary>
    /// \if KO
    /// <para>Layout Descriptor 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the layout descriptor value.</para>
    /// \endif
    /// </summary>
    public WeddingLayoutOption LayoutDescriptor => InvitationDesignCatalog.GetLayout(LayoutMode);
    /// <summary>
    /// \if KO
    /// <para>Invitation Style 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invitation style value.</para>
    /// \endif
    /// </summary>
    public string InvitationStyle => InvitationDesignCatalog.ToLegacyLayoutKey(LayoutMode);
    /// <summary>
    /// \if KO
    /// <para>Uses Bottom Navigation 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the uses bottom navigation value.</para>
    /// \endif
    /// </summary>
    public bool UsesBottomNavigation => LayoutDescriptor.UsesBottomNavigation;
    /// <summary>
    /// \if KO
    /// <para>Ordered Sections 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the ordered sections value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<string> OrderedSections =>
        WeddingSectionOrderCatalog.NormalizeInvitationOrder(DesignSettings.SectionOrder, LayoutDescriptor.SupportedSections);
    /// <summary>
    /// \if KO
    /// <para>Ceremony Note Html 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the ceremony note html value.</para>
    /// \endif
    /// </summary>
    public string CeremonyNoteHtml
    {
        get
        {
            var raw = Config?.CeremonyNote ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return "";
            if (IsCeremonyNoteHtml)
                return raw;
            return Markdown.ToHtml(raw, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
        }
    }
    /// <summary>
    /// \if KO
    /// <para>Is Ceremony Note Html 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is ceremony note html value.</para>
    /// \endif
    /// </summary>
    public bool IsCeremonyNoteHtml =>
        string.Equals(Config?.CeremonyNoteFormat, "Html", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>Hero Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero image url value.</para>
    /// \endif
    /// </summary>
    public string HeroImageUrl
    {
        get
        {
            if (Config is null) return "";
            if (!string.IsNullOrWhiteSpace(Config.HeroImageFileName))
                return _photos.GetHeroUrl(Config.Slug, Config.HeroImageFileName);
            return "";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Road Map Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the road map url value.</para>
    /// \endif
    /// </summary>
    public string RoadMapUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.RoadMapFileName)) return "";
            return _photos.GetRoadMapUrl(Config.Slug, Config.RoadMapFileName);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Accounts 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the accounts value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<AccountInfo> Accounts => Config?.Accounts ?? [];

    /// <summary>
    /// \if KO
    /// <para>Og Title 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og title value.</para>
    /// \endif
    /// </summary>
    public string OgTitle => !string.IsNullOrWhiteSpace(Config?.OgTitle)
        ? Config.OgTitle
        : $"{CoupleName} 청첩장";

    /// <summary>
    /// \if KO
    /// <para>Og Description 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og description value.</para>
    /// \endif
    /// </summary>
    public string OgDescription => !string.IsNullOrWhiteSpace(Config?.OgDescription)
        ? Config.OgDescription
        : $"{WeddingDate:yyyy년 MM월 dd일} {VenueName}에서 함께해 주세요.";

    /// <summary>
    /// \if KO
    /// <para>Og Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og image url value.</para>
    /// \endif
    /// </summary>
    public string OgImageUrl
    {
        get
        {
            if (Config is null) return "";
            // OG 전용 이미지 우선, 없으면 히어로 이미지
            var fn = !string.IsNullOrWhiteSpace(Config.OgImageFileName)
                ? Config.OgImageFileName
                : Config.HeroImageFileName;
            return string.IsNullOrWhiteSpace(fn) ? "" : _photos.GetHeroUrl(Config.Slug, fn);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Thank You Og Title 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thank you og title value.</para>
    /// \endif
    /// </summary>
    public string ThankYouOgTitle => !string.IsNullOrWhiteSpace(Config?.ThankYouOgTitle)
        ? Config.ThankYouOgTitle
        : $"{CoupleName} 감사 인사";

    /// <summary>
    /// \if KO
    /// <para>Thank You Og Description 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thank you og description value.</para>
    /// \endif
    /// </summary>
    public string ThankYouOgDescription => !string.IsNullOrWhiteSpace(Config?.ThankYouOgDescription)
        ? Config.ThankYouOgDescription
        : $"{WeddingDate:yyyy년 MM월 dd일} {VenueName}에서의 결혼식을 마쳤습니다. 함께해 주셔서 감사합니다.";

    /// <summary>
    /// \if KO
    /// <para>Thank You Og Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thank you og image url value.</para>
    /// \endif
    /// </summary>
    public string ThankYouOgImageUrl
    {
        get
        {
            if (Config is null) return "";
            var fn = !string.IsNullOrWhiteSpace(Config.ThankYouOgImageFileName)
                ? Config.ThankYouOgImageFileName
                : Config.HeroImageFileName;
            return string.IsNullOrWhiteSpace(fn) ? "" : _photos.GetHeroUrl(Config.Slug, fn);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Music Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the music url value.</para>
    /// \endif
    /// </summary>
    public string MusicUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.MusicFileName)) return "";
            return _photos.GetMusicUrl(Config.Slug, Config.MusicFileName);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Music Button Position 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the music button position value.</para>
    /// \endif
    /// </summary>
    public string MusicButtonPosition => Config?.MusicButtonPosition ?? "bottom";
    /// <summary>
    /// \if KO
    /// <para>Music Button Style 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the music button style value.</para>
    /// \endif
    /// </summary>
    public string MusicButtonStyle => BuildFloatingStyle(DesignSettings.MusicButtonPlacement);
    /// <summary>
    /// \if KO
    /// <para>Has Custom Music Button Position 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has custom music button position value.</para>
    /// \endif
    /// </summary>
    public bool HasCustomMusicButtonPosition =>
        DesignSettings.MusicButtonPlacement.HasDesktop || DesignSettings.MusicButtonPlacement.HasMobile;

    /// <summary>
    /// \if KO
    /// <para>Hero Panel Vertical Desktop 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero panel vertical desktop value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelVerticalDesktop => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.DesktopVertical, ["top", "middle", "bottom"], "top");
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Horizontal Desktop 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero panel horizontal desktop value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelHorizontalDesktop => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.DesktopHorizontal, ["left", "center", "right"], "center");
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Vertical Mobile 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero panel vertical mobile value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelVerticalMobile => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.MobileVertical, ["top", "middle", "bottom"], "top");
    /// <summary>
    /// \if KO
    /// <para>Hero Panel Horizontal Mobile 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero panel horizontal mobile value.</para>
    /// \endif
    /// </summary>
    public string HeroPanelHorizontalMobile => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.MobileHorizontal, ["left", "center", "right"], "center");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Vertical Desktop 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero top vertical desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopVerticalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.DesktopVertical, ["top", "middle", "bottom"], "top");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Horizontal Desktop 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero top horizontal desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopHorizontalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.DesktopHorizontal, ["left", "center", "right"], "center");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Vertical Mobile 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero top vertical mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopVerticalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.MobileVertical, ["top", "middle", "bottom"], "top");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Horizontal Mobile 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero top horizontal mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopHorizontalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.MobileHorizontal, ["left", "center", "right"], "center");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Vertical Desktop 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero bottom vertical desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomVerticalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.DesktopVertical, ["top", "middle", "bottom"], "bottom");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Horizontal Desktop 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero bottom horizontal desktop value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomHorizontalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.DesktopHorizontal, ["left", "center", "right"], "center");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Vertical Mobile 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero bottom vertical mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomVerticalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.MobileVertical, ["top", "middle", "bottom"], "bottom");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Horizontal Mobile 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero bottom horizontal mobile value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomHorizontalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.MobileHorizontal, ["left", "center", "right"], "center");
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Top Style 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero top style value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroTopStyle => BuildHeroPanelStyle(DesignSettings.HeroPlacement.InviteTop);
    /// <summary>
    /// \if KO
    /// <para>Invite Hero Bottom Style 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the invite hero bottom style value.</para>
    /// \endif
    /// </summary>
    public string InviteHeroBottomStyle => BuildHeroPanelStyle(DesignSettings.HeroPlacement.InviteBottom);
    /// <summary>
    /// \if KO
    /// <para>Has Invite Hero Top Custom Position 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has invite hero top custom position value.</para>
    /// \endif
    /// </summary>
    public bool HasInviteHeroTopCustomPosition =>
        DesignSettings.HeroPlacement.InviteTop.HasDesktopCustomPosition || DesignSettings.HeroPlacement.InviteTop.HasMobileCustomPosition;
    /// <summary>
    /// \if KO
    /// <para>Has Invite Hero Bottom Custom Position 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the has invite hero bottom custom position value.</para>
    /// \endif
    /// </summary>
    public bool HasInviteHeroBottomCustomPosition =>
        DesignSettings.HeroPlacement.InviteBottom.HasDesktopCustomPosition || DesignSettings.HeroPlacement.InviteBottom.HasMobileCustomPosition;

    /// <summary>
    /// \if KO
    /// <para>Selected Tab 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tab value.</para>
    /// \endif
    /// </summary>
    public string SelectedTab { get; private set; } = "map";
    /// <summary>
    /// \if KO
    /// <para>Map 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the map value.</para>
    /// \endif
    /// </summary>
    public void SetMap() => SelectedTab = "map";
    /// <summary>
    /// \if KO
    /// <para>Road 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the road value.</para>
    /// \endif
    /// </summary>
    public void SetRoad() => SelectedTab = "road";
    /// <summary>
    /// \if KO
    /// <para>Tab Class 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the tab class operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tab">
    /// \if KO
    /// <para>tab에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tab.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Tab Class 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the tab class operation.</para>
    /// \endif
    /// </returns>
    public string TabClass(string tab) => SelectedTab == tab ? "active" : "";

    /// <summary>
    /// \if KO
    /// <para>Lightbox Open 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the lightbox open value.</para>
    /// \endif
    /// </summary>
    public bool LightboxOpen { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Lightbox Idx 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the lightbox idx value.</para>
    /// \endif
    /// </summary>
    public int LightboxIdx { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Open Lightbox 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the open lightbox operation.</para>
    /// \endif
    /// </summary>
    /// <param name="idx">
    /// \if KO
    /// <para>idx에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for idx.</para>
    /// \endif
    /// </param>
    public void OpenLightbox(int idx)
    {
        LightboxIdx = Math.Clamp(idx, 0, Math.Max(0, AllPhotos.Count - 1));
        LightboxOpen = true;
    }
    /// <summary>
    /// \if KO
    /// <para>Close Lightbox 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the close lightbox operation.</para>
    /// \endif
    /// </summary>
    public void CloseLightbox() => LightboxOpen = false;
    /// <summary>
    /// \if KO
    /// <para>Lightbox Next 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the lightbox next operation.</para>
    /// \endif
    /// </summary>
    public void LightboxNext() => LightboxIdx = (LightboxIdx + 1) % Math.Max(1, AllPhotos.Count);
    /// <summary>
    /// \if KO
    /// <para>Lightbox Prev 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the lightbox prev operation.</para>
    /// \endif
    /// </summary>
    public void LightboxPrev() => LightboxIdx = (LightboxIdx - 1 + Math.Max(1, AllPhotos.Count)) % Math.Max(1, AllPhotos.Count);

    /// <summary>
    /// \if KO
    /// <para>Resolve Story Chapter Photo 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve story chapter photo operation.</para>
    /// \endif
    /// </summary>
    /// <param name="chapter">
    /// \if KO
    /// <para>chapter에 사용할 <c>StoryChapter</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>StoryChapter</c> value used for chapter.</para>
    /// \endif
    /// </param>
    /// <param name="chapterIndex">
    /// \if KO
    /// <para>chapter Index에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for chapter index.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Story Chapter Photo 작업에서 생성한 <c>PhotoInfo?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PhotoInfo?</c> result produced by the resolve story chapter photo operation.</para>
    /// \endif
    /// </returns>
    public PhotoInfo? ResolveStoryChapterPhoto(StoryChapter chapter, int chapterIndex)
    {
        var explicitPhoto = FindPhoto(chapter.PhotoPath) ?? FindPhoto(chapter.PhotoId);
        if (explicitPhoto is not null)
        {
            return explicitPhoto;
        }

        return chapterIndex >= 0 && chapterIndex < AllPhotos.Count
            ? AllPhotos[chapterIndex]
            : null;
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }
        InvitationDesignCatalog.Normalize(Config);

        var all = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
        var sorted = ApplyGalleryOrder(all, Config.GalleryFileNames);
        AllPhotos = sorted;
        Gallery = sorted.Take(10).ToList();
        IsLoaded = true;
    }

    /// <summary>
    /// \if KO
    /// <para>Apply Gallery Order 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the apply gallery order operation.</para>
    /// \endif
    /// </summary>
    /// <param name="photos">
    /// \if KO
    /// <para>photos에 사용할 <c>IReadOnlyList&lt;PhotoInfo&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;PhotoInfo&gt;</c> value used for photos.</para>
    /// \endif
    /// </param>
    /// <param name="order">
    /// \if KO
    /// <para>order에 사용할 <c>IReadOnlyList&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> value used for order.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Apply Gallery Order 작업에서 생성한 <c>List&lt;PhotoInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;PhotoInfo&gt;</c> result produced by the apply gallery order operation.</para>
    /// \endif
    /// </returns>
    private static List<PhotoInfo> ApplyGalleryOrder(IReadOnlyList<PhotoInfo> photos, IReadOnlyList<string> order)
    {
        var orderMap = order
            .Select((fileName, index) => new { fileName, index })
            .GroupBy(x => x.fileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().index, StringComparer.OrdinalIgnoreCase);

        return photos
            .OrderBy(p => orderMap.TryGetValue(p.FileName, out var index) ? index : int.MaxValue)
            .ThenByDescending(p => p.LastModified)
            .ThenByDescending(p => p.FileName)
            .ToList();
    }

    /// <summary>
    /// \if KO
    /// <para>Photo 항목을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the photo item.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Find Photo 작업에서 생성한 <c>PhotoInfo?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PhotoInfo?</c> result produced by the find photo operation.</para>
    /// \endif
    /// </returns>
    private PhotoInfo? FindPhoto(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim();
        return AllPhotos.FirstOrDefault(p =>
            string.Equals(p.FileName, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Url, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.ThumbUrl, key, StringComparison.OrdinalIgnoreCase) ||
            p.Url.EndsWith("/" + key, StringComparison.OrdinalIgnoreCase) ||
            p.ThumbUrl.EndsWith("/" + key, StringComparison.OrdinalIgnoreCase));
    }

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

    /// <summary>
    /// \if KO
    /// <para>Floating Style 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the floating style value.</para>
    /// \endif
    /// </summary>
    /// <param name="position">
    /// \if KO
    /// <para>position에 사용할 <c>WeddingFloatingPosition</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingFloatingPosition</c> value used for position.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Floating Style 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build floating style operation.</para>
    /// \endif
    /// </returns>
    private static string BuildFloatingStyle(WeddingFloatingPosition position)
    {
        var parts = new List<string>();
        if (position.HasDesktop)
        {
            parts.Add($"--w-drag-x:{ClampPercent(position.DesktopX):0.##}%;");
            parts.Add($"--w-drag-y:{ClampPercent(position.DesktopY):0.##}%;");
        }
        if (position.HasMobile)
        {
            parts.Add($"--w-drag-mobile-x:{ClampPercent(position.MobileX):0.##}%;");
            parts.Add($"--w-drag-mobile-y:{ClampPercent(position.MobileY):0.##}%;");
        }
        return string.Concat(parts);
    }

    /// <summary>
    /// \if KO
    /// <para>Hero Panel Style 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the hero panel style value.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Build Hero Panel Style 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build hero panel style operation.</para>
    /// \endif
    /// </returns>
    private static string BuildHeroPanelStyle(HeroPanelPlacement placement)
    {
        var parts = new List<string>();
        if (placement.HasDesktopCustomPosition)
        {
            parts.Add($"--w-drag-x:{ClampPercent(placement.DesktopX):0.##}%;");
            parts.Add($"--w-drag-y:{ClampPercent(placement.DesktopY):0.##}%;");
        }
        if (placement.HasMobileCustomPosition)
        {
            parts.Add($"--w-drag-mobile-x:{ClampPercent(placement.MobileX):0.##}%;");
            parts.Add($"--w-drag-mobile-y:{ClampPercent(placement.MobileY):0.##}%;");
        }
        return string.Concat(parts);
    }

    /// <summary>
    /// \if KO
    /// <para>Clamp Percent 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clamp percent operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Clamp Percent 작업에서 생성한 <c>double</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> result produced by the clamp percent operation.</para>
    /// \endif
    /// </returns>
    private static double ClampPercent(double? value) => Math.Clamp(value ?? 50, 0, 100);
}
