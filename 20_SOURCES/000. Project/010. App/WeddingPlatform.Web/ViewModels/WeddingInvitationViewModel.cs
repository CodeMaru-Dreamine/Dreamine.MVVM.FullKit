using Markdig;
using Wedding.Common;
using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

public sealed class WeddingInvitationViewModel
{
    private readonly ITenantStore _tenants;
    private readonly IPhotoService _photos;

    public WeddingInvitationViewModel(ITenantStore tenants, IPhotoService photos)
    {
        _tenants = tenants;
        _photos = photos;
    }

    public TenantConfig? Config { get; private set; }
    /// <summary>그리드 표시용 — 최신 10개</summary>
    public IReadOnlyList<PhotoInfo> Gallery { get; private set; } = [];
    /// <summary>라이트박스/자동재생용 — 전체</summary>
    public IReadOnlyList<PhotoInfo> AllPhotos { get; private set; } = [];
    public bool IsLoaded { get; private set; }
    public bool NotFound { get; private set; }

    public string CoupleName => Config?.CoupleName ?? "";
    public string HeroTitle => string.IsNullOrWhiteSpace(Config?.HeroTitle) ? "Save The Date" : Config.HeroTitle;
    public IReadOnlyList<string> VideoUrls => Config?.VideoFileNames
        .Select(fn => _photos.GetVideoUrl(Config.Slug, fn))
        .ToList() ?? [];
    public int GalleryAutoPlayMs => Math.Clamp(Config?.GalleryAutoPlaySeconds ?? 3, 1, 30) * 1000;
    public string Subtitle => Config?.Subtitle ?? "";
    public DateTime WeddingDate => Config?.WeddingDate ?? DateTime.Today;
    public string WeddingTime => Config?.WeddingTime ?? "";
    public string VenueName => Config?.VenueName ?? "";
    public string VenueAddress => Config?.VenueAddress ?? "";
    public string Story => Config?.Story ?? "";
    public string Story2 => Config?.Story2 ?? "";
    public WeddingSiteMode Mode => Config?.Mode ?? WeddingSiteMode.Invite;
    public bool ShowThankYouLink => Mode == WeddingSiteMode.Both;
    public string ThankYouUrl => Config?.ThankYouUrl ?? "";
    public string MapLinkKakao => Config?.MapLinkKakao ?? "";
    public string MapLinkNaver => Config?.MapLinkNaver ?? "";
    public string MapLinkAtlan => Config?.MapLinkAtlan ?? "";
    public string MapLinkTmap => Config?.MapLinkTmap ?? "";
    public double VenueLat => Config?.VenueLat ?? 0;
    public double VenueLng => Config?.VenueLng ?? 0;
    public bool HasVenueCoords => VenueLat != 0 && VenueLng != 0;
    public DesignSettings DesignSettings => Config?.DesignSettings ?? new DesignSettings();
    public string ThemeName => InvitationDesignCatalog.GetTheme(DesignSettings.ThemeKey).Key;
    public WeddingLayoutMode LayoutMode => DesignSettings.LayoutMode == WeddingLayoutMode.Unknown
        ? InvitationDesignCatalog.FromLegacyLayoutKey(Config?.InvitationStyle)
        : DesignSettings.LayoutMode;
    public WeddingLayoutOption LayoutDescriptor => InvitationDesignCatalog.GetLayout(LayoutMode);
    public string InvitationStyle => InvitationDesignCatalog.ToLegacyLayoutKey(LayoutMode);
    public bool UsesBottomNavigation => LayoutDescriptor.UsesBottomNavigation;
    public string CeremonyNoteHtml
    {
        get
        {
            var raw = Config?.CeremonyNote ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return "";
            if (string.Equals(Config?.CeremonyNoteFormat, "Html", StringComparison.OrdinalIgnoreCase))
                return raw;
            return Markdown.ToHtml(raw, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
        }
    }

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

    public string RoadMapUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.RoadMapFileName)) return "";
            return _photos.GetRoadMapUrl(Config.Slug, Config.RoadMapFileName);
        }
    }

    public IReadOnlyList<AccountInfo> Accounts => Config?.Accounts ?? [];

    public string OgTitle => !string.IsNullOrWhiteSpace(Config?.OgTitle)
        ? Config.OgTitle
        : $"{CoupleName} 청첩장";

    public string OgDescription => !string.IsNullOrWhiteSpace(Config?.OgDescription)
        ? Config.OgDescription
        : $"{WeddingDate:yyyy년 MM월 dd일} {VenueName}에서 함께해 주세요.";

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

    public string MusicUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.MusicFileName)) return "";
            return _photos.GetMusicUrl(Config.Slug, Config.MusicFileName);
        }
    }

    public string MusicButtonPosition => Config?.MusicButtonPosition ?? "bottom";

    public string HeroPanelVerticalDesktop => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.DesktopVertical, ["top", "middle", "bottom"], "top");
    public string HeroPanelHorizontalDesktop => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.DesktopHorizontal, ["left", "center", "right"], "center");
    public string HeroPanelVerticalMobile => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.MobileVertical, ["top", "middle", "bottom"], "top");
    public string HeroPanelHorizontalMobile => NormalizeOption(DesignSettings.HeroPlacement.ThankYou.MobileHorizontal, ["left", "center", "right"], "center");
    public string InviteHeroTopVerticalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.DesktopVertical, ["top", "middle", "bottom"], "top");
    public string InviteHeroTopHorizontalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.DesktopHorizontal, ["left", "center", "right"], "center");
    public string InviteHeroTopVerticalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.MobileVertical, ["top", "middle", "bottom"], "top");
    public string InviteHeroTopHorizontalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteTop.MobileHorizontal, ["left", "center", "right"], "center");
    public string InviteHeroBottomVerticalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.DesktopVertical, ["top", "middle", "bottom"], "bottom");
    public string InviteHeroBottomHorizontalDesktop => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.DesktopHorizontal, ["left", "center", "right"], "center");
    public string InviteHeroBottomVerticalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.MobileVertical, ["top", "middle", "bottom"], "bottom");
    public string InviteHeroBottomHorizontalMobile => NormalizeOption(DesignSettings.HeroPlacement.InviteBottom.MobileHorizontal, ["left", "center", "right"], "center");

    public string SelectedTab { get; private set; } = "map";
    public void SetMap() => SelectedTab = "map";
    public void SetRoad() => SelectedTab = "road";
    public string TabClass(string tab) => SelectedTab == tab ? "active" : "";

    public bool LightboxOpen { get; private set; }
    public int LightboxIdx { get; private set; }

    public void OpenLightbox(int idx)
    {
        LightboxIdx = Math.Clamp(idx, 0, Math.Max(0, AllPhotos.Count - 1));
        LightboxOpen = true;
    }
    public void CloseLightbox() => LightboxOpen = false;
    public void LightboxNext() => LightboxIdx = (LightboxIdx + 1) % Math.Max(1, AllPhotos.Count);
    public void LightboxPrev() => LightboxIdx = (LightboxIdx - 1 + Math.Max(1, AllPhotos.Count)) % Math.Max(1, AllPhotos.Count);

    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }
        InvitationDesignCatalog.Normalize(Config);

        var all = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
        var sorted = all.OrderByDescending(p => p.LastModified)
                        .ThenByDescending(p => p.FileName)
                        .ToList();
        AllPhotos = sorted;
        Gallery = sorted.Take(10).ToList();
        IsLoaded = true;
    }

    private static string NormalizeOption(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) && allowed.Contains(normalized) ? normalized : fallback;
    }
}
