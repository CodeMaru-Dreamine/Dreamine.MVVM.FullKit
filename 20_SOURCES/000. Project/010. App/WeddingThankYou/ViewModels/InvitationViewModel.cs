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
		public string Story => Config?.Story ?? "";
		public string Story2 => Config?.Story2 ?? "";
		public string ThemeName => Config?.ThemeName ?? "rose";
		public string ThankYouStyle => Config?.ThankYouStyle ?? "onepage";
		public WeddingLayoutMode LayoutMode => WeddingLayoutCatalog.FromLegacyKey(ThankYouStyle);
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

		public string HeroPanelVerticalDesktop => Config?.HeroPanelVerticalDesktop ?? "top";
		public string HeroPanelHorizontalDesktop => Config?.HeroPanelHorizontalDesktop ?? "center";
		public string HeroPanelVerticalMobile => Config?.HeroPanelVerticalMobile ?? "top";
		public string HeroPanelHorizontalMobile => Config?.HeroPanelHorizontalMobile ?? "center";

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

			var all = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
			var sorted = all.OrderByDescending(p => p.LastModified)
							.ThenByDescending(p => p.FileName)
							.ToList();
			AllPhotos = sorted;
			Gallery = sorted.Take(10).ToList();
			IsLoaded = true;
		}
	}
}
