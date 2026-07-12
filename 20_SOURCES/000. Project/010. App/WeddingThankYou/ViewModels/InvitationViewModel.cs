using Markdig;
using Wedding.Common;
using WeddingThankYou.Models;
using WeddingThankYou.Services;

namespace WeddingThankYou.ViewModels
{
	/// <summary>
	/// \file InvitationViewModel.cs
	/// \brief ThankYou 페이지 전용 ViewModel — 슬러그별 TenantConfig + 갤러리를 로드합니다.
	/// \details
	///   - WeddingPlatform.Web의 WeddingInvitationViewModel을 ThankYou 전용으로 트리밍.
	///   - ITenantStore/IPhotoService 기반으로 동작하며, 일반 POCO + private set 패턴을 사용합니다.
	/// \author CodeMaru
	/// </summary>
	public sealed class InvitationViewModel
	{
		private readonly ITenantStore _tenants;
		private readonly IPhotoService _photos;

		public InvitationViewModel(ITenantStore tenants, IPhotoService photos)
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
		public string HeroTitle => string.IsNullOrWhiteSpace(Config?.HeroTitle) ? "Thank You" : Config.HeroTitle;
		public IReadOnlyList<string> VideoUrls => Config?.VideoFileNames
			.Select(fn => _photos.GetVideoUrl(Config.Slug, fn))
			.ToList() ?? [];
		public int GalleryAutoPlayMs => Math.Clamp(Config?.GalleryAutoPlaySeconds ?? 3, 1, 30) * 1000;
		public string Subtitle => Config?.Subtitle ?? "";
		public DateTime WeddingDate => Config?.WeddingDate ?? DateTime.Today;
		public string WeddingTime => Config?.WeddingTime ?? "";
		public string VenueName => Config?.VenueName ?? "";
		public string VenueAddress => Config?.VenueAddress ?? "";
		public double VenueLat => Config?.VenueLat ?? 0;
		public double VenueLng => Config?.VenueLng ?? 0;
		public bool HasVenueCoords => VenueLat != 0 && VenueLng != 0;
		public string Story => Config?.Story ?? "";
		public string Story2 => Config?.Story2 ?? "";
		public IReadOnlyList<StoryChapter> StoryChapters => Config?.StoryChapters ?? WeddingStoryChapterDefaults.Create();
		public string ThemeName => Config?.ThemeName ?? "rose";
		public string ThankYouStyle => Config?.ThankYouStyle ?? "onepage";
		public WeddingLayoutMode LayoutMode => WeddingLayoutCatalog.FromLegacyKey(ThankYouStyle);
		public IReadOnlyList<string> OrderedSections => WeddingSectionOrderCatalog.NormalizeThankYouOrder(Config?.SectionOrder);
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

		public string HeroPanelVerticalDesktop => Config?.HeroPanelVerticalDesktop ?? "top";
		public string HeroPanelHorizontalDesktop => Config?.HeroPanelHorizontalDesktop ?? "center";
		public string HeroPanelVerticalMobile => Config?.HeroPanelVerticalMobile ?? "top";
		public string HeroPanelHorizontalMobile => Config?.HeroPanelHorizontalMobile ?? "center";
		public string HeroPanelStyle => BuildFloatingStyle(Config?.HeroPanelPlacement ?? new WeddingFloatingPosition());
		public bool HasCustomHeroPanelPosition =>
			Config?.HeroPanelPlacement.HasDesktop == true || Config?.HeroPanelPlacement.HasMobile == true;

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

		public string MapLinkKakao => Config?.MapLinkKakao ?? "";
		public string MapLinkNaver => Config?.MapLinkNaver ?? "";
		public string MapLinkAtlan => Config?.MapLinkAtlan ?? "";
		public string MapLinkTmap => Config?.MapLinkTmap ?? "";
		public bool HasMapLinks =>
			!string.IsNullOrWhiteSpace(MapLinkKakao) ||
			!string.IsNullOrWhiteSpace(MapLinkNaver) ||
			!string.IsNullOrWhiteSpace(MapLinkAtlan) ||
			!string.IsNullOrWhiteSpace(MapLinkTmap);
		public bool HasMapSection => HasVenueCoords || !string.IsNullOrWhiteSpace(RoadMapUrl) || HasMapLinks;
		public string SelectedTab { get; private set; } = "map";
		public void SetMap() => SelectedTab = "map";
		public void SetRoad() => SelectedTab = "road";
		public string TabClass(string tab) => SelectedTab == tab ? "active" : "";

		public IReadOnlyList<AccountInfo> Accounts => Config?.Accounts ?? [];
		public string InvitationUrl => NormalizeLinkUrl(Config?.InvitationUrl);

		public string OgTitle => !string.IsNullOrWhiteSpace(Config?.OgTitle)
			? Config.OgTitle
			: $"{CoupleName} 감사 인사";

		public string OgDescription => !string.IsNullOrWhiteSpace(Config?.OgDescription)
			? Config.OgDescription
			: $"{WeddingDate:yyyy년 MM월 dd일} {VenueName}에서의 결혼식을 마쳤습니다. 함께해 주셔서 감사합니다.";

		public string OgImageUrl
		{
			get
			{
				if (Config is null) return "";
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
		public string MusicButtonStyle => BuildFloatingStyle(Config?.MusicButtonPlacement ?? new WeddingFloatingPosition());
		public bool HasCustomMusicButtonPosition =>
			Config?.MusicButtonPlacement.HasDesktop == true || Config?.MusicButtonPlacement.HasMobile == true;

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
			ThankYouDesignCatalog.Normalize(Config);

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

		private static double ClampPercent(double? value) => Math.Clamp(value ?? 50, 0, 100);

		private static string NormalizeLinkUrl(string? value)
		{
			var url = value?.Trim() ?? "";
			if (string.IsNullOrWhiteSpace(url))
			{
				return "";
			}

			if (url.StartsWith("/", StringComparison.Ordinal))
			{
				return url;
			}

			if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)
				&& (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
			{
				return url;
			}

			return $"https://{url}";
		}
	}
}
