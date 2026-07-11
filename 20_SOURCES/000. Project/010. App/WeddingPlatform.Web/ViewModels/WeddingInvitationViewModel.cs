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
    public IReadOnlyList<StoryChapter> StoryChapters => DesignSettings.StoryChapters;
    public string ThemeName => InvitationDesignCatalog.GetTheme(DesignSettings.ThemeKey).Key;
    public WeddingLayoutMode LayoutMode => DesignSettings.LayoutMode == WeddingLayoutMode.Unknown
        ? InvitationDesignCatalog.FromLegacyLayoutKey(Config?.InvitationStyle)
        : DesignSettings.LayoutMode;
    public WeddingLayoutOption LayoutDescriptor => InvitationDesignCatalog.GetLayout(LayoutMode);
    public string InvitationStyle => InvitationDesignCatalog.ToLegacyLayoutKey(LayoutMode);
    public bool UsesBottomNavigation => LayoutDescriptor.UsesBottomNavigation;
    public IReadOnlyList<string> OrderedSections =>
        WeddingSectionOrderCatalog.NormalizeInvitationOrder(DesignSettings.SectionOrder, LayoutDescriptor.SupportedSections);
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
    public bool IsCeremonyNoteHtml =>
        string.Equals(Config?.CeremonyNoteFormat, "Html", StringComparison.OrdinalIgnoreCase);

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

    public string ThankYouOgTitle => !string.IsNullOrWhiteSpace(Config?.ThankYouOgTitle)
        ? Config.ThankYouOgTitle
        : $"{CoupleName} 감사 인사";

    public string ThankYouOgDescription => !string.IsNullOrWhiteSpace(Config?.ThankYouOgDescription)
        ? Config.ThankYouOgDescription
        : $"{WeddingDate:yyyy년 MM월 dd일} {VenueName}에서의 결혼식을 마쳤습니다. 함께해 주셔서 감사합니다.";

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

    public string MusicUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.MusicFileName)) return "";
            return _photos.GetMusicUrl(Config.Slug, Config.MusicFileName);
        }
    }

    public string MusicButtonPosition => Config?.MusicButtonPosition ?? "bottom";
    public string MusicButtonStyle => BuildFloatingStyle(DesignSettings.MusicButtonPlacement);
    public bool HasCustomMusicButtonPosition =>
        DesignSettings.MusicButtonPlacement.HasDesktop || DesignSettings.MusicButtonPlacement.HasMobile;

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
    public string InviteHeroTopStyle => BuildHeroPanelStyle(DesignSettings.HeroPlacement.InviteTop);
    public string InviteHeroBottomStyle => BuildHeroPanelStyle(DesignSettings.HeroPlacement.InviteBottom);
    public bool HasInviteHeroTopCustomPosition =>
        DesignSettings.HeroPlacement.InviteTop.HasDesktopCustomPosition || DesignSettings.HeroPlacement.InviteTop.HasMobileCustomPosition;
    public bool HasInviteHeroBottomCustomPosition =>
        DesignSettings.HeroPlacement.InviteBottom.HasDesktopCustomPosition || DesignSettings.HeroPlacement.InviteBottom.HasMobileCustomPosition;

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

    private static string NormalizeOption(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) && allowed.Contains(normalized) ? normalized : fallback;
    }

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

    private static double ClampPercent(double? value) => Math.Clamp(value ?? 50, 0, 100);
}
