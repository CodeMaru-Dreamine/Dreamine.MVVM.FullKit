using Markdig;
using Wedding.Common;
using WeddingThankYou.Models;
using WeddingThankYou.Services;

namespace WeddingThankYou.ViewModels
{
	/// <summary>
	/// \if KO
	/// <para>\file InvitationViewModel.cs \brief ThankYou 페이지 전용 ViewModel — 슬러그별 TenantConfig + 갤러리를 로드합니다. \details - WeddingPlatform.Web의 WeddingInvitationViewModel을 ThankYou 전용으로 트리밍. - ITenantStore/IPhotoService 기반으로 동작하며, 일반 POCO + private set 패턴을 사용합니다. \author CodeMaru</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates invitation view model functionality and related state.</para>
	/// \endif
	/// </summary>
	public sealed class InvitationViewModel
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
		/// <para>지정한 설정으로 <see cref="InvitationViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Initializes a new instance of the <see cref="InvitationViewModel"/> class with the specified settings.</para>
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
		public InvitationViewModel(ITenantStore tenants, IPhotoService photos)
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
		public string HeroTitle => string.IsNullOrWhiteSpace(Config?.HeroTitle) ? "Thank You" : Config.HeroTitle;
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
		/// <para>Story Chapters 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the story chapters value.</para>
		/// \endif
		/// </summary>
		public IReadOnlyList<StoryChapter> StoryChapters => Config?.StoryChapters ?? WeddingStoryChapterDefaults.Create();
		/// <summary>
		/// \if KO
		/// <para>Theme Name 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the theme name value.</para>
		/// \endif
		/// </summary>
		public string ThemeName => Config?.ThemeName ?? "rose";
		/// <summary>
		/// \if KO
		/// <para>Thank You Style 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the thank you style value.</para>
		/// \endif
		/// </summary>
		public string ThankYouStyle => Config?.ThankYouStyle ?? "onepage";
		/// <summary>
		/// \if KO
		/// <para>Layout Mode 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the layout mode value.</para>
		/// \endif
		/// </summary>
		public WeddingLayoutMode LayoutMode => WeddingLayoutCatalog.FromLegacyKey(ThankYouStyle);
		/// <summary>
		/// \if KO
		/// <para>Ordered Sections 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the ordered sections value.</para>
		/// \endif
		/// </summary>
		public IReadOnlyList<string> OrderedSections => WeddingSectionOrderCatalog.NormalizeThankYouOrder(Config?.SectionOrder);
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
		/// <para>Hero Panel Vertical Desktop 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the hero panel vertical desktop value.</para>
		/// \endif
		/// </summary>
		public string HeroPanelVerticalDesktop => Config?.HeroPanelVerticalDesktop ?? "top";
		/// <summary>
		/// \if KO
		/// <para>Hero Panel Horizontal Desktop 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the hero panel horizontal desktop value.</para>
		/// \endif
		/// </summary>
		public string HeroPanelHorizontalDesktop => Config?.HeroPanelHorizontalDesktop ?? "center";
		/// <summary>
		/// \if KO
		/// <para>Hero Panel Vertical Mobile 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the hero panel vertical mobile value.</para>
		/// \endif
		/// </summary>
		public string HeroPanelVerticalMobile => Config?.HeroPanelVerticalMobile ?? "top";
		/// <summary>
		/// \if KO
		/// <para>Hero Panel Horizontal Mobile 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the hero panel horizontal mobile value.</para>
		/// \endif
		/// </summary>
		public string HeroPanelHorizontalMobile => Config?.HeroPanelHorizontalMobile ?? "center";
		/// <summary>
		/// \if KO
		/// <para>Hero Panel Style 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the hero panel style value.</para>
		/// \endif
		/// </summary>
		public string HeroPanelStyle => BuildFloatingStyle(Config?.HeroPanelPlacement ?? new WeddingFloatingPosition());
		/// <summary>
		/// \if KO
		/// <para>Has Custom Hero Panel Position 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the has custom hero panel position value.</para>
		/// \endif
		/// </summary>
		public bool HasCustomHeroPanelPosition =>
			Config?.HeroPanelPlacement.HasDesktop == true || Config?.HeroPanelPlacement.HasMobile == true;

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
		/// <para>Has Map Links 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the has map links value.</para>
		/// \endif
		/// </summary>
		public bool HasMapLinks =>
			!string.IsNullOrWhiteSpace(MapLinkKakao) ||
			!string.IsNullOrWhiteSpace(MapLinkNaver) ||
			!string.IsNullOrWhiteSpace(MapLinkAtlan) ||
			!string.IsNullOrWhiteSpace(MapLinkTmap);
		/// <summary>
		/// \if KO
		/// <para>Has Map Section 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the has map section value.</para>
		/// \endif
		/// </summary>
		public bool HasMapSection => HasVenueCoords || !string.IsNullOrWhiteSpace(RoadMapUrl) || HasMapLinks;
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
		/// <para>Accounts 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the accounts value.</para>
		/// \endif
		/// </summary>
		public IReadOnlyList<AccountInfo> Accounts => Config?.Accounts ?? [];
		/// <summary>
		/// \if KO
		/// <para>Invitation Url 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the invitation url value.</para>
		/// \endif
		/// </summary>
		public string InvitationUrl => NormalizeLinkUrl(Config?.InvitationUrl);

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
			: $"{CoupleName} 감사 인사";

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
			: $"{WeddingDate:yyyy년 MM월 dd일} {VenueName}에서의 결혼식을 마쳤습니다. 함께해 주셔서 감사합니다.";

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
				var fn = !string.IsNullOrWhiteSpace(Config.OgImageFileName)
					? Config.OgImageFileName
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
		public string MusicButtonStyle => BuildFloatingStyle(Config?.MusicButtonPlacement ?? new WeddingFloatingPosition());
		/// <summary>
		/// \if KO
		/// <para>Has Custom Music Button Position 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the has custom music button position value.</para>
		/// \endif
		/// </summary>
		public bool HasCustomMusicButtonPosition =>
			Config?.MusicButtonPlacement.HasDesktop == true || Config?.MusicButtonPlacement.HasMobile == true;

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
			ThankYouDesignCatalog.Normalize(Config);

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

		/// <summary>
		/// \if KO
		/// <para>Normalize Link Url 작업을 수행합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Performs the normalize link url operation.</para>
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
		/// <para>Normalize Link Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>string</c> result produced by the normalize link url operation.</para>
		/// \endif
		/// </returns>
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
