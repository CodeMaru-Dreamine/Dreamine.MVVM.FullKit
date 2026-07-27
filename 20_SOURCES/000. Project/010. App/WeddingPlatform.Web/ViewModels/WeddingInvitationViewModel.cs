using Markdig;
using Wedding.Common;
using Wedding.Layouts.Contracts;
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
    private readonly IWeddingLayoutCatalogRegistry _layoutRegistry;
    private WeddingLayoutCatalog _layoutCatalogSnapshot = WeddingLayoutCatalog.Instance;
    private WeddingLayoutOption? _effectiveLayout;
    private string? _previewLayoutKey;
    private string? _previewLayoutVersion;
    private bool? _previewFollowActiveLayoutVersion;

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
    public WeddingInvitationViewModel(
        ITenantStore tenants,
        IPhotoService photos,
        IWeddingLayoutCatalogRegistry layoutRegistry)
    {
        _tenants = tenants;
        _photos = photos;
        _layoutRegistry = layoutRegistry;
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

    /// <summary>현재 요청이 사용하는 승인된 선언형 레이아웃 패키지입니다.</summary>
    public WeddingLayoutPublishedPackage? LayoutPackage { get; private set; }

    /// <summary>
    /// WPF 편집기와 Web이 공유하는 검증된 블록 정의입니다.
    /// null이면 기존 6종 Razor 호환 렌더러를 사용합니다.
    /// </summary>
    public LayoutDefinition? DynamicLayoutDefinition => LayoutPackage?.Definition;

    public bool UsesDynamicLayout => DynamicLayoutDefinition is not null;

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

    /// <summary>포토북 레이아웃 전용 페이지 목록입니다. PhotoBook 이 아니면 참조하지 않아도 됩니다.</summary>
    public IReadOnlyList<PhotoBookPage> PhotoBookPages => DesignSettings.PhotoBookPages;

    /// <summary>카드 레이아웃 전용 강조 카드 목록입니다. Card 가 아니면 참조하지 않아도 됩니다.</summary>
    public IReadOnlyList<CardHighlight> CardHighlights => DesignSettings.CardHighlights;

    /// <summary>
    /// 지정한 섹션 키에 매핑된 카드 강조 오버라이드를 반환합니다. 매칭이 없으면 null 입니다.
    /// 여러 항목이 같은 섹션에 매핑되어 있으면 Order 가 낮은 항목이 우선합니다.
    /// </summary>
    public CardHighlight? ResolveCardHighlight(string sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey)) return null;
        return DesignSettings.CardHighlights
            .Where(x => string.Equals(x.SectionKey, sectionKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Order)
            .FirstOrDefault();
    }
    /// <summary>
    /// \if KO
    /// <para>Theme Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the theme name value.</para>
    /// \endif
    /// </summary>
    public string ThemeName =>
        string.Equals(DesignSettings.ThemeKey, WeddingThemeCatalog.CustomThemeKey, StringComparison.OrdinalIgnoreCase)
        && Config?.HasPremiumPlan == true
            ? WeddingThemeCatalog.CustomThemeKey
            : InvitationDesignCatalog.GetTheme(DesignSettings.ThemeKey).Key;

    /// <summary>
    /// Premium 사용자 정의 테마에 적용할 안전한 CSS 변수입니다.
    /// 프리셋 테마이거나 권한이 없으면 빈 사전을 반환합니다.
    /// </summary>
    public IReadOnlyDictionary<string, string> ThemeCssVariables
    {
        get
        {
            if (!string.Equals(ThemeName, WeddingThemeCatalog.CustomThemeKey, StringComparison.OrdinalIgnoreCase))
            {
                return new Dictionary<string, string>();
            }

            var palette = WeddingThemePaletteGenerator.ResolveForRendering(
                DesignSettings.CustomTheme);
            return CreateThemeOverrides(
                palette.Primary,
                palette.Dark,
                palette.Accent,
                palette.Accent,
                palette.Background,
                palette.PanelBackground,
                palette.Text,
                palette.MutedText,
                palette.Border,
                palette.ButtonBackground,
                palette.ButtonText);
        }
    }
    /// <summary>
    /// \if KO
    /// <para>Layout Mode 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the layout mode value.</para>
    /// \endif
    /// </summary>
    public WeddingLayoutMode LayoutMode =>
        _effectiveLayout?.Mode
        ?? (DesignSettings.LayoutMode == WeddingLayoutMode.Unknown
            ? InvitationDesignCatalog.FromLegacyLayoutKey(Config?.InvitationStyle)
            : DesignSettings.LayoutMode);

    /// <summary>현재 청첩장에 고정된 카탈로그 레이아웃 키입니다.</summary>
    public string LayoutKey => _effectiveLayout?.Key
        ?? (string.IsNullOrWhiteSpace(DesignSettings.LayoutKey)
            ? WeddingLayoutCatalog.ToLegacyKey(LayoutMode)
            : WeddingLayoutKeys.Normalize(DesignSettings.LayoutKey));

    /// <summary>현재 청첩장에 고정된 레이아웃 버전입니다.</summary>
    public string LayoutVersion
    {
        get
        {
            var descriptor = _layoutCatalogSnapshot.FindDescriptor(LayoutKey);
            return _effectiveLayout?.Version
                ?? (WeddingLayoutVersion.IsValid(DesignSettings.LayoutVersion)
                ? DesignSettings.LayoutVersion
                : descriptor?.CurrentVersion ?? WeddingLayoutVersion.Initial);
        }
    }

    /// <summary>현재 키와 버전에 해당하는 불변 릴리스입니다.</summary>
    public WeddingLayoutRelease? LayoutRelease =>
        _layoutCatalogSnapshot.FindRelease(LayoutKey, LayoutVersion);
    /// <summary>
    /// \if KO
    /// <para>Layout Descriptor 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the layout descriptor value.</para>
    /// \endif
    /// </summary>
    public WeddingLayoutOption LayoutDescriptor =>
        _effectiveLayout
        ?? _layoutCatalogSnapshot.Find(LayoutKey)
        ?? InvitationDesignCatalog.GetLayout(LayoutMode);

    /// <summary>승인된 패키지의 검증된 스타일 토큰을 현재 레이아웃 루트에만 적용합니다.</summary>
    public string LayoutStyle
    {
        get
        {
            if (LayoutPackage is null) return "";

            // 패키지는 레이아웃 제작자가 제안한 기본 팔레트입니다. 테넌트가 고른
            // 기본 제공 테마(또는 Premium 사용자 정의 테마)는 같은 의미의 토큰을
            // 마지막에 덮어써서 레이아웃 버전과 무관하게 항상 사용자 선택을 따릅니다.
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var token in LayoutPackage.Definition.StyleTokens)
            {
                var variable = CssVariable(token.Token);
                if (variable is not null)
                {
                    merged[variable] = token.Value;
                }
            }

            foreach (var (variable, value) in BuildTenantThemeOverrides())
            {
                merged[variable] = value;
            }

            return string.Concat(
                merged
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => $"{x.Key}:{x.Value};"));
        }
    }
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
    public IReadOnlyList<string> OrderedSections
    {
        get
        {
            IReadOnlyList<string> requestedOrder =
                LayoutPackage?.Definition.SectionOrder is { Count: > 0 } packageOrder
                    ? packageOrder.Select(ToLegacySectionKey).ToArray()
                    : DesignSettings.SectionOrder;
            var ordered = WeddingSectionOrderCatalog.NormalizeInvitationOrder(
                requestedOrder,
                LayoutDescriptor.SupportedSections);
            var visibility = DesignSettings.SectionVisibility;
            if (visibility is null || visibility.Count == 0)
            {
                return ordered;
            }

            // hero 는 항상 노출 유지(각 레이아웃의 대표 영역). 그 외 섹션은 명시적으로 false 인 경우에만 숨김.
            return ordered
                .Where(section =>
                    string.Equals(section, "hero", StringComparison.OrdinalIgnoreCase)
                    || !visibility.TryGetValue(section, out var visible)
                    || visible)
                .ToList();
        }
    }
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
    public IReadOnlyList<AccountInfo> Accounts =>
        Config?.Accounts
            .Where(AccountInfo.HasDisplayableContent)
            .ToArray()
        ?? [];

    /// <summary>
    /// 별도 iframe 회로가 저장소에서 읽은 계좌 목록을 관리자 화면의 편집 중
    /// 스냅샷으로 교체합니다. 이 메서드는 미리보기 페이지에서만 호출하며
    /// 저장소에는 아무 것도 기록하지 않습니다.
    /// </summary>
    public void ApplyAdminPreviewAccounts(
        IReadOnlyList<AccountInfo>? accounts)
    {
        if (Config is null)
        {
            return;
        }

        Config.Accounts = accounts?
            .Where(account => account is not null)
            .Take(8)
            .Select(AccountInfo.CloneForPreview)
            .ToList()
            ?? [];
    }

    /// <summary>검색 결과와 브라우저 제목에 사용할 문구입니다.</summary>
    public string SearchTitle => WeddingSeoService.ResolveSearchTitle(Config);

    /// <summary>검색 결과 설명 메타 태그에 사용할 문구입니다.</summary>
    public string SearchDescription => WeddingSeoService.ResolveSearchDescription(Config);

    /// <summary>현재 청첩장이 검색 엔진 색인을 허용하는지 여부입니다.</summary>
    public bool IsSearchIndexingEnabled => WeddingSeoService.IsIndexingEnabled(Config);

    /// <summary>검색 엔진과 공유 서비스에 제공할 고정 canonical URL입니다.</summary>
    public string CanonicalUrl => Config is null
        ? WeddingSeoService.SiteBaseUrl + "/"
        : $"{WeddingSeoService.SiteBaseUrl}/{Uri.EscapeDataString(Config.Slug)}";

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
    /// 모든 레거시 히어로 렌더러가 공유하는 PC/폰별 이미지 맞춤 및 초점 CSS 변수입니다.
    /// </summary>
    public string HeroImageStyle => BuildHeroImageStyle(DesignSettings.HeroImagePresentation);
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
    public Task LoadAsync(string slug, CancellationToken ct = default) =>
        LoadAsync(
            slug,
            previewLayoutKey: null,
            previewLayoutVersion: null,
            previewFollowActiveLayoutVersion: null,
            previewThemeSelection: null,
            ct);

    /// <summary>
    /// 저장되지 않은 관리자 레이아웃 선택을 현재 미리보기 요청에만 적용해 불러옵니다.
    /// 공개 테넌트 설정은 변경하지 않습니다.
    /// </summary>
    public Task LoadAsync(
        string slug,
        string? previewLayoutKey,
        string? previewLayoutVersion,
        bool? previewFollowActiveLayoutVersion,
        CancellationToken ct = default) =>
        LoadAsync(
            slug,
            previewLayoutKey,
            previewLayoutVersion,
            previewFollowActiveLayoutVersion,
            previewThemeSelection: null,
            ct);

    /// <summary>
    /// 저장되지 않은 관리자 레이아웃과 테마 선택을 현재 미리보기 요청에만 적용해 불러옵니다.
    /// 공개 테넌트 설정은 변경하지 않습니다.
    /// </summary>
    public async Task LoadAsync(
        string slug,
        string? previewLayoutKey,
        string? previewLayoutVersion,
        bool? previewFollowActiveLayoutVersion,
        WeddingThemePreviewSelection? previewThemeSelection,
        CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }
        InvitationDesignCatalog.Normalize(Config);
        _layoutCatalogSnapshot = _layoutRegistry.Current;
        ConfigurePreviewLayoutSelection(
            previewLayoutKey,
            previewLayoutVersion,
            previewFollowActiveLayoutVersion);
        ConfigurePreviewThemeSelection(previewThemeSelection);
        ResolveEffectiveLayout();

        var all = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
        var sorted = ApplyGalleryOrder(all, Config.GalleryFileNames);
        AllPhotos = sorted;
        Gallery = sorted.Take(10).ToList();
        IsLoaded = true;
    }

    private void ConfigurePreviewLayoutSelection(
        string? layoutKey,
        string? layoutVersion,
        bool? followActiveLayoutVersion)
    {
        _previewLayoutKey = null;
        _previewLayoutVersion = null;
        _previewFollowActiveLayoutVersion = null;

        if (!followActiveLayoutVersion.HasValue
            || string.IsNullOrWhiteSpace(layoutKey))
        {
            return;
        }

        var key = layoutKey.Trim();
        if (!WeddingLayoutKeys.IsValid(key))
        {
            return;
        }

        var descriptor = _layoutCatalogSnapshot.FindDescriptor(key);
        if (descriptor is null)
        {
            return;
        }

        var version = followActiveLayoutVersion.Value
            ? descriptor.CurrentVersion
            : layoutVersion?.Trim() ?? "";
        if (!WeddingLayoutVersion.IsValid(version)
            || _layoutCatalogSnapshot.FindRelease(descriptor.Key, version) is null)
        {
            return;
        }

        _previewLayoutKey = descriptor.Key;
        _previewLayoutVersion = version;
        _previewFollowActiveLayoutVersion = followActiveLayoutVersion.Value;
    }

    private void ConfigurePreviewThemeSelection(
        WeddingThemePreviewSelection? selection)
    {
        if (Config is null || selection is null)
        {
            return;
        }

        var requestedKey = selection.ThemeKey?.Trim();
        if (string.Equals(
                requestedKey,
                WeddingThemeCatalog.CustomThemeKey,
                StringComparison.OrdinalIgnoreCase))
        {
            var palette = selection.Palette;
            if (!Config.HasPremiumPlan
                || palette is null
                || !IsValidPreviewPalette(palette))
            {
                return;
            }

            var custom = Config.DesignSettings.CustomTheme
                ?? new CustomWeddingThemeSettings();
            custom.BaseColor = palette.BaseColor;
            custom.Primary = palette.Primary;
            custom.Dark = palette.Dark;
            custom.Accent = palette.Accent;
            custom.Text = palette.Text;
            custom.MutedText = palette.MutedText;
            custom.Background = palette.Background;
            custom.PanelBackground = palette.PanelBackground;
            custom.ButtonBackground = palette.ButtonBackground;
            custom.ButtonText = palette.ButtonText;
            custom.Border = palette.Border;
            Config.DesignSettings.CustomTheme = custom;
            Config.DesignSettings.ThemeKey = WeddingThemeCatalog.CustomThemeKey;
            Config.ThemeName = WeddingThemeCatalog.CustomThemeKey;
            return;
        }

        var option = WeddingThemeCatalog.Instance.Find(requestedKey);
        if (option is null || !option.IsImplemented)
        {
            return;
        }

        var access = new WeddingThemeAccessState
        {
            HasPremiumPlan = Config.HasPremiumPlan,
            UnlockedThemeKeys = Config.UnlockedThemeKeys,
        };
        if (!new WeddingThemeAccessPolicy().CanUse(option, access))
        {
            return;
        }

        Config.DesignSettings.ThemeKey = option.Key;
        Config.ThemeName = option.Key;
    }

    private static bool IsValidPreviewPalette(WeddingThemePalette palette) =>
        WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.BaseColor, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.Primary, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.Dark, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.Accent, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.Text, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.MutedText, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.Background, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.PanelBackground, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.ButtonBackground, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.ButtonText, out _)
        && WeddingThemePaletteGenerator.TryNormalizeHexColor(palette.Border, out _);

    private void ResolveEffectiveLayout()
    {
        if (Config is null)
        {
            ApplyBuiltInLayoutFallback();
            return;
        }

        var hasPreviewSelection =
            _previewFollowActiveLayoutVersion.HasValue
            && !string.IsNullOrWhiteSpace(_previewLayoutKey);
        var requestedKey = WeddingLayoutKeys.Normalize(
            hasPreviewSelection
                ? _previewLayoutKey
                : Config.DesignSettings.LayoutKey);
        var descriptor = _layoutCatalogSnapshot.FindDescriptor(requestedKey);
        var followActiveLayoutVersion = hasPreviewSelection
            ? _previewFollowActiveLayoutVersion!.Value
            : Config.DesignSettings.FollowActiveLayoutVersion;
        var pinnedVersion = hasPreviewSelection
            ? _previewLayoutVersion
            : Config.DesignSettings.LayoutVersion;
        var requestedVersion = followActiveLayoutVersion
            ? descriptor?.CurrentVersion
            : WeddingLayoutVersion.IsValid(pinnedVersion)
                ? pinnedVersion!.Trim()
                : descriptor?.CurrentVersion;
        var release = descriptor is null || string.IsNullOrWhiteSpace(requestedVersion)
            ? null
            : _layoutCatalogSnapshot.FindRelease(descriptor.Key, requestedVersion);

        if (descriptor is null || release is null || !release.IsImplemented)
        {
            ApplyBuiltInLayoutFallback();
            return;
        }

        WeddingLayoutPublishedPackage? package = null;
        var renderMode = descriptor.LegacyMode;
        var label = descriptor.Label;
        var description = descriptor.Description;
        var tier = descriptor.Tier;
        if (!descriptor.IsBuiltIn)
        {
            if (!_layoutRegistry.PublishedPackages.TryGetValue(
                    release.Id,
                    out var publishedPackage))
            {
                ApplyBuiltInLayoutFallback();
                return;
            }

            package = publishedPackage;

            // 신규 등록 경로는 기존 Razor 레이아웃을 베이스로 삼지 않습니다.
            // Unknown은 아래 DynamicInvitationLayout 진입을 뜻하는 호환 경계 값입니다.
            renderMode = WeddingLayoutMode.Unknown;
            label = publishedPackage.Manifest.Label;
            description = publishedPackage.Manifest.Description;
        }

        // 등급은 개별 릴리스 manifest가 아니라 LayoutKey 단위의 서버 정책을
        // 반영한 descriptor에서만 가져옵니다. 따라서 버전을 올리거나 롤백해도
        // Free/Premium 권한 정책은 바뀌지 않습니다.
        var canUse = tier == WeddingLayoutTier.Free
            || Config.HasPremiumPlan;
        if (!canUse)
        {
            ApplyBuiltInLayoutFallback();
            return;
        }

        LayoutPackage = package;
        _effectiveLayout = new WeddingLayoutOption(
            renderMode,
            label,
            description,
            tier,
            release.IsImplemented,
            release.CssClass,
            release.UsesBottomNavigation,
            release.SupportedSections)
        {
            CatalogKey = descriptor.Key,
            Version = release.Version,
        };
    }

    private void ApplyBuiltInLayoutFallback()
    {
        LayoutPackage = null;
        _effectiveLayout = WeddingLayoutCatalog.Instance.Find(WeddingLayoutKeys.OnePage);
    }

    private IReadOnlyDictionary<string, string> BuildTenantThemeOverrides()
    {
        if (string.Equals(ThemeName, WeddingThemeCatalog.CustomThemeKey, StringComparison.OrdinalIgnoreCase))
        {
            var palette = WeddingThemePaletteGenerator.ResolveForRendering(
                DesignSettings.CustomTheme);

            return CreateThemeOverrides(
                palette.Primary,
                palette.Dark,
                palette.Accent,
                palette.Accent,
                palette.Background,
                palette.PanelBackground,
                palette.Text,
                palette.MutedText,
                palette.Border,
                palette.ButtonBackground,
                palette.ButtonText);
        }

        return WeddingThemeCatalog.NormalizeKey(ThemeName) switch
        {
            "ivory" => CreateThemeOverrides(
                "#b8a99a", "#4a3f38", "#8a7060", "#8a7060", "#ebe0d0",
                "rgba(253,250,244,.9)", "#4a3f38", "#756a63",
                "rgba(184,169,154,.35)"),
            "forest" => CreateThemeOverrides(
                "#6b8f71", "#2d4a32", "#4a6b50", "#4a6b50", "#d9e8dd",
                "rgba(238,246,240,.9)", "#2d4a32", "#536c57",
                "rgba(107,143,113,.35)"),
            "navy" => CreateThemeOverrides(
                "#3d5a80", "#1a2a3a", "#98c1d9", "#98c1d9", "#dae2f0",
                "rgba(238,242,251,.9)", "#1a2a3a", "#526274",
                "rgba(61,90,128,.32)"),
            "blush" => CreateThemeOverrides(
                "#d4a5a5", "#5a3535", "#b07575", "#b07575", "#f7dede",
                "rgba(253,242,242,.9)", "#5a3535", "#7c5a5a",
                "rgba(212,165,165,.38)"),
            _ => CreateThemeOverrides(
                "#c8a882", "#3a2e28", "#a07850", "#a07850", "#f4e8d4",
                "rgba(255,252,243,.88)", "#3a2e28", "#6d5a50",
                "rgba(200,168,130,.32)"),
        };
    }

    private static IReadOnlyDictionary<string, string> CreateThemeOverrides(
        string primary,
        string dark,
        string secondary,
        string accent,
        string background,
        string surface,
        string text,
        string mutedText,
        string border,
        string? buttonBackground = null,
        string? buttonText = null) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--w-primary"] = primary,
            ["--w-dark"] = dark,
            ["--w-secondary"] = secondary,
            ["--w-accent"] = accent,
            ["--w-bg"] = background,
            ["--w-panel-bg"] = surface,
            ["--w-text"] = text,
            ["--w-muted-text"] = mutedText,
            ["--w-border"] = border,
            ["--w-button-bg"] = buttonBackground ?? primary,
            ["--w-button-text"] = buttonText ?? "#ffffff",
            ["--w-nav-bg"] = surface,
            ["--w-nav-text"] = text,
            ["--w-shadow"] = $"0 4px 24px {dark}1f",
        };

    private static string? CssVariable(LayoutStyleToken token) => token switch
    {
        LayoutStyleToken.PrimaryColor => "--w-primary",
        LayoutStyleToken.SecondaryColor => "--w-secondary",
        LayoutStyleToken.AccentColor => "--w-accent",
        LayoutStyleToken.BackgroundColor => "--w-bg",
        LayoutStyleToken.SurfaceColor => "--w-panel-bg",
        LayoutStyleToken.TextColor => "--w-text",
        LayoutStyleToken.MutedTextColor => "--w-muted-text",
        LayoutStyleToken.BorderColor => "--w-border",
        LayoutStyleToken.ButtonBackgroundColor => "--w-button-bg",
        LayoutStyleToken.ButtonTextColor => "--w-button-text",
        LayoutStyleToken.NavigationBackgroundColor => "--w-nav-bg",
        LayoutStyleToken.NavigationTextColor => "--w-nav-text",
        _ => null,
    };

    private static string ToLegacySectionKey(LayoutSectionKey section) => section switch
    {
        LayoutSectionKey.Hero => "hero",
        LayoutSectionKey.Invitation => "info",
        LayoutSectionKey.Calendar => "calendar",
        LayoutSectionKey.Gallery => "gallery",
        LayoutSectionKey.Story => "story",
        LayoutSectionKey.Video => "video",
        LayoutSectionKey.Location => "details",
        LayoutSectionKey.Accounts => "gift",
        LayoutSectionKey.Guestbook => "guestbook",
        LayoutSectionKey.Contact => "contact",
        _ => "hero",
    };

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
    private static string NormalizeHexColor(string? value, string fallback)
    {
        var normalized = value?.Trim();
        if (normalized is null || normalized.Length != 7 || normalized[0] != '#')
        {
            return fallback;
        }

        return normalized.AsSpan(1).ToString().All(Uri.IsHexDigit)
            ? normalized.ToLowerInvariant()
            : fallback;
    }

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

    private static string BuildHeroImageStyle(HeroImagePresentationSettings presentation)
    {
        var desktopFit = NormalizeOption(
            presentation.DesktopFit,
            [HeroImagePresentationSettings.Contain, HeroImagePresentationSettings.Cover],
            HeroImagePresentationSettings.Contain);
        var mobileFit = NormalizeOption(
            presentation.MobileFit,
            [HeroImagePresentationSettings.Contain, HeroImagePresentationSettings.Cover],
            HeroImagePresentationSettings.Contain);

        var desktopX = ClampPercent(
            desktopFit == HeroImagePresentationSettings.Cover
                ? presentation.DesktopCrop.X + presentation.DesktopCrop.Width / 2
                : presentation.DesktopFocusX).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        var desktopY = ClampPercent(
            desktopFit == HeroImagePresentationSettings.Cover
                ? presentation.DesktopCrop.Y + presentation.DesktopCrop.Height / 2
                : presentation.DesktopFocusY).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        var mobileX = ClampPercent(
            mobileFit == HeroImagePresentationSettings.Cover
                ? presentation.MobileCrop.X + presentation.MobileCrop.Width / 2
                : presentation.MobileFocusX).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        var mobileY = ClampPercent(
            mobileFit == HeroImagePresentationSettings.Cover
                ? presentation.MobileCrop.Y + presentation.MobileCrop.Height / 2
                : presentation.MobileFocusY).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        var desktopCrop = BuildCropVariables("desktop", presentation.DesktopCrop);
        var mobileCrop = BuildCropVariables("mobile", presentation.MobileCrop);

        return $"--w-hero-image-fit-desktop:{desktopFit};" +
               $"--w-hero-image-position-desktop:{desktopX}% {desktopY}%;" +
               $"--w-hero-image-crop-desktop-enabled:{(desktopFit == HeroImagePresentationSettings.Cover ? 1 : 0)};" +
               desktopCrop +
               $"--w-hero-image-fit-mobile:{mobileFit};" +
               $"--w-hero-image-position-mobile:{mobileX}% {mobileY}%;" +
               $"--w-hero-image-crop-mobile-enabled:{(mobileFit == HeroImagePresentationSettings.Cover ? 1 : 0)};" +
               mobileCrop;
    }

    private static string BuildCropVariables(string viewport, HeroImageCropRegion crop)
    {
        static string Percent(double value) =>
            Math.Clamp(value, 0, 100).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        return $"--w-hero-image-crop-{viewport}-x:{Percent(crop.X)};" +
               $"--w-hero-image-crop-{viewport}-y:{Percent(crop.Y)};" +
               $"--w-hero-image-crop-{viewport}-width:{Percent(crop.Width)};" +
               $"--w-hero-image-crop-{viewport}-height:{Percent(crop.Height)};";
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
