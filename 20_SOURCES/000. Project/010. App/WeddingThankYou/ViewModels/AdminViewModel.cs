using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using Wedding.Common;
using WeddingThankYou.Models;
using WeddingThankYou.Services;

namespace WeddingThankYou.ViewModels
{
	/// <summary>
	/// \file AdminViewModel.cs
	/// \brief 어드민 페이지 ViewModel — 로그인/설정저장/업로드/지오코딩/지도링크생성.
	/// \details WeddingPlatform.Web의 WeddingAdminViewModel을 거의 그대로 이식.
	/// </summary>
	public sealed class AdminViewModel
	{
		private readonly ITenantStore _tenants;
		private readonly IPhotoService _photos;
		private readonly WeddingOptions _opts;
		private readonly ThankYouUserContext _userContext;
		private readonly IMediaQuotaPolicyResolver _mediaPolicyResolver;
		private readonly ISuperAdminSessionTokenService _superAdminTokens;

		private static readonly HttpClient _geocodeHttp = new()
		{
			DefaultRequestHeaders = { { "User-Agent", "WeddingThankYou/1.0 (contact: admin@codemaru.co.kr)" } }
		};

		public AdminViewModel(
			ITenantStore tenants,
			IPhotoService photos,
			WeddingOptions opts,
			ThankYouUserContext userContext,
			IMediaQuotaPolicyResolver mediaPolicyResolver,
			ISuperAdminSessionTokenService superAdminTokens)
		{
			_tenants = tenants;
			_photos = photos;
			_opts = opts;
			_userContext = userContext;
			_mediaPolicyResolver = mediaPolicyResolver;
			_superAdminTokens = superAdminTokens;
		}

		/// <summary>동영상 업로드 최대 용량 안내 문구 (예: "최대 200MB" 또는 "무제한").</summary>
		public string MaxVideoSizeLabel { get; private set; } = "최대 200MB";
		/// <summary>동영상 업로드 최대 개수 (0이면 무제한).</summary>
		public int MaxVideoCount { get; private set; } = 6;
		/// <summary>현재 계정에 적용되는 최종 미디어 정책입니다.</summary>
		public EffectiveMediaPolicy? EffectiveMediaPolicy { get; private set; }

		public TenantConfig? Config { get; private set; }
		public IReadOnlyList<PhotoInfo> Gallery { get; private set; } = [];
		public bool IsLoaded { get; private set; }
		public bool IsAuthenticated { get; private set; }
		public bool IsSignedIn { get; private set; }
		public bool IsLinkedToCurrentUser { get; private set; }
		public bool IsOwner { get; private set; }
		public IReadOnlyList<ThankYouAdminUser> EffectiveAdminUsers { get; private set; } = [];
		public string StatusMessage { get; private set; } = "";
		public string CurrentUserLabel { get; private set; } = "";
		public bool IsUploading { get; private set; }
		public bool IsGeocoding { get; private set; }
		public string GeocodeStatus { get; private set; } = "";

		public string LoginPassword { get; set; } = "";

		public void SetStatusMessage(string message)
		{
			StatusMessage = message;
		}

		public async Task InitializeAsync(string slug, CancellationToken ct = default)
		{
			StatusMessage = "";
			await RefreshCurrentUserAsync().ConfigureAwait(false);

			var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
			if (config is null)
			{
				return;
			}

			var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
			IsOwner = IsOwnerUser(config, user);
			IsLinkedToCurrentUser = IsAdminUser(config, user);
			EffectiveAdminUsers = BuildEffectiveAdminUsers(config);

			if (IsLinkedToCurrentUser)
			{
				IsAuthenticated = true;
				await LoadAsync(slug, ct).ConfigureAwait(false);
			}
		}

		public async Task<bool> LoginAsync(string slug, CancellationToken ct = default)
		{
			var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
			if (config is null) { StatusMessage = "존재하지 않는 슬러그입니다."; return false; }

			var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
			await RefreshCurrentUserAsync().ConfigureAwait(false);

			if (IsAdminUser(config, user))
			{
				IsAuthenticated = true;
				IsLinkedToCurrentUser = true;
				IsOwner = IsOwnerUser(config, user);
				EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
				StatusMessage = "";
				return true;
			}

			IsAuthenticated = config.PasswordHash == LoginPassword;
			if (!IsAuthenticated)
			{
				StatusMessage = "비밀번호가 틀렸습니다.";
				return false;
			}

			if (user.IsAuthenticated && string.IsNullOrWhiteSpace(config.OwnerUserId))
			{
				config.OwnerUserId = user.Id;
				config.OwnerProvider = user.Provider;
				config.OwnerEmail = user.Email;
				config.OwnerDisplayName = user.DisplayName;
				config.OwnerLinkedAt = DateTime.Now;
				EnsureAdminUser(config, user, "Owner");
				await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
				IsLinkedToCurrentUser = true;
				IsOwner = true;
				EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
				StatusMessage = "CodeMaru/Dreamine 계정에 대표 관리자로 연결되었습니다. 다음부터는 공용 로그인으로 관리할 수 있습니다.";
			}
			else if (user.IsAuthenticated)
			{
				EnsureAdminUser(config, user, "Admin");
				await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
				IsLinkedToCurrentUser = true;
				IsOwner = IsOwnerUser(config, user);
				EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
				StatusMessage = "현재 CodeMaru/Dreamine 계정이 이 감사장의 관리자로 추가되었습니다.";
			}
			else if (!user.IsAuthenticated && string.IsNullOrWhiteSpace(config.OwnerUserId))
			{
				StatusMessage = "로그인은 성공했습니다. 공용 계정에 연결하려면 먼저 CodeMaru/Dreamine 로그인을 해주세요.";
			}
			else
			{
				StatusMessage = "";
			}

			return IsAuthenticated;
		}

		public async Task<bool> LoginAsSuperAdminAsync(string slug, string? sessionToken, CancellationToken ct = default)
		{
			if (!_superAdminTokens.ValidateToken(sessionToken))
			{
				return false;
			}

			var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
			if (config is null)
			{
				StatusMessage = "존재하지 않는 슬러그입니다.";
				return false;
			}

			IsAuthenticated = true;
			IsLinkedToCurrentUser = false;
			IsOwner = false;
			EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
			StatusMessage = "슈퍼관리자 권한으로 접속했습니다.";
			await LoadAsync(slug, ct).ConfigureAwait(false);
			return true;
		}

		public async Task LoadAsync(string slug, CancellationToken ct = default)
		{
			Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
					 ?? new TenantConfig { Slug = slug };
			ThankYouDesignCatalog.Normalize(Config);
			Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
			EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);

			EffectiveMediaPolicy = await _mediaPolicyResolver.ResolveAsync(Config, ct).ConfigureAwait(false);

			var effectiveMb = EffectiveMediaPolicy.VideoMaxFileSizeMb;
			MaxVideoSizeLabel = effectiveMb <= 0 ? "무제한" : $"최대 {effectiveMb}MB";

			MaxVideoCount = EffectiveMediaPolicy.VideoMaxCount;

			IsLoaded = true;
		}

		private async Task RefreshCurrentUserAsync()
		{
			var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
			IsSignedIn = user.IsAuthenticated;
			CurrentUserLabel = user.IsAuthenticated
				? string.IsNullOrWhiteSpace(user.DisplayName)
					? user.Provider
					: $"{user.DisplayName} ({user.Provider})"
				: "";
		}

		public async Task SaveConfigAsync(CancellationToken ct = default)
		{
			if (Config is null) return;
			try
			{
				if (!ValidateLayoutForSave(Config))
				{
					return;
				}

				if (!ValidateThemeForSave(Config))
				{
					return;
				}

				ThankYouDesignCatalog.Normalize(Config);
				await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
				EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
				StatusMessage = "설정이 저장되었습니다.";
			}
			catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
		}

		public async Task SaveStoryChapterAsync(StoryChapter chapter, CancellationToken ct = default)
		{
			if (Config is null) return;
			try
			{
				ThankYouDesignCatalog.Normalize(Config);
				var chapters = Config.StoryChapters;
				var index = chapters.FindIndex(x => x.ChapterNumber == chapter.ChapterNumber);
				var normalized = WeddingStoryChapterDefaults.Clone(chapter);
				normalized.Label = string.IsNullOrWhiteSpace(normalized.Label)
					? $"CHAPTER {Math.Max(1, normalized.ChapterNumber):00}"
					: normalized.Label.Trim();
				normalized.Title = string.IsNullOrWhiteSpace(normalized.Title)
					? "스토리"
					: normalized.Title.Trim();

				if (index >= 0)
				{
					chapters[index] = normalized;
				}
				else
				{
					chapters.Add(normalized);
					Config.StoryChapters = WeddingStoryChapterDefaults.Normalize(chapters);
				}

				await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
				StatusMessage = $"{normalized.Label} 챕터가 저장되었습니다.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"챕터 저장 오류: {ex.Message}";
			}
		}

		public async Task AddStoryChapterAsync(CancellationToken ct = default)
		{
			if (Config is null) return;
			try
			{
				ThankYouDesignCatalog.Normalize(Config);
				var chapters = Config.StoryChapters;
				var nextNumber = chapters.Count == 0 ? 1 : chapters.Max(x => x.ChapterNumber) + 1;
				chapters.Add(new StoryChapter
				{
					ChapterNumber = nextNumber,
					Label = $"CHAPTER {nextNumber:00}",
					Title = "새로운 이야기",
				});

				await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
				StatusMessage = $"CHAPTER {nextNumber:00} 챕터가 추가되었습니다.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"챕터 추가 오류: {ex.Message}";
			}
		}

		public async Task DeleteStoryChapterAsync(int chapterNumber, CancellationToken ct = default)
		{
			if (Config is null) return;
			try
			{
				ThankYouDesignCatalog.Normalize(Config);
				if (chapterNumber <= 4)
				{
					StatusMessage = "기본 4개 챕터는 유지됩니다.";
					return;
				}

				var removed = Config.StoryChapters.RemoveAll(x => x.ChapterNumber == chapterNumber);
				if (removed == 0)
				{
					StatusMessage = "삭제할 챕터를 찾을 수 없습니다.";
					return;
				}

				await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
				StatusMessage = $"CHAPTER {chapterNumber:00} 챕터가 삭제되었습니다.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"챕터 삭제 오류: {ex.Message}";
			}
		}

		private bool ValidateLayoutForSave(TenantConfig config)
		{
			if (!WeddingLayoutCatalog.IsKnownKey(config.ThankYouStyle))
			{
				StatusMessage = "저장 오류: 존재하지 않는 레이아웃입니다.";
				return false;
			}

			var mode = ThankYouDesignCatalog.ResolveLayoutMode(config.ThankYouStyle);
			var option = WeddingLayoutCatalog.Instance.Find(mode);
			if (option is null)
			{
				StatusMessage = "저장 오류: 존재하지 않는 레이아웃입니다.";
				return false;
			}

			if (!option.IsImplemented)
			{
				StatusMessage = "저장 오류: 아직 준비 중인 레이아웃입니다.";
				return false;
			}

			var access = new WeddingLayoutAccessState
			{
				HasPremiumPlan = config.HasPremiumPlan,
				UnlockedLayouts = config.UnlockedLayoutModes
					.Select(WeddingLayoutCatalog.FromLegacyKey)
					.Where(x => x != WeddingLayoutMode.Unknown)
					.ToArray(),
			};

			if (!new WeddingLayoutAccessPolicy().CanUse(option, access))
			{
				StatusMessage = "저장 오류: 프리미엄 레이아웃은 플랜 또는 잠금 해제 후 저장할 수 있습니다.";
				return false;
			}

			return true;
		}

		private bool ValidateThemeForSave(TenantConfig config)
		{
			config.UnlockedThemeKeys ??= new();
			if (!WeddingThemeCatalog.IsKnownKey(config.ThemeName))
			{
				StatusMessage = "저장 오류: 존재하지 않는 테마입니다.";
				return false;
			}

			var option = WeddingThemeCatalog.Instance.Find(config.ThemeName);
			if (option is null)
			{
				StatusMessage = "저장 오류: 존재하지 않는 테마입니다.";
				return false;
			}

			if (!option.IsImplemented)
			{
				StatusMessage = "저장 오류: 아직 준비 중인 테마입니다.";
				return false;
			}

			var access = new WeddingThemeAccessState
			{
				HasPremiumPlan = config.HasPremiumPlan,
				UnlockedThemeKeys = config.UnlockedThemeKeys
					.Select(WeddingThemeCatalog.NormalizeKey)
					.Where(WeddingThemeCatalog.IsKnownKey)
					.ToArray(),
			};

			if (!new WeddingThemeAccessPolicy().CanUse(option, access))
			{
				StatusMessage = "저장 오류: 프리미엄 테마는 플랜 또는 잠금 해제 후 저장할 수 있습니다.";
				return false;
			}

			config.ThemeName = option.Key;
			return true;
		}

		public async Task RemoveAdminAsync(string userId, CancellationToken ct = default)
		{
			if (Config is null) return;
			var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
			if (!IsOwnerUser(Config, user))
			{
				StatusMessage = "대표 관리자만 관리자 계정을 삭제할 수 있습니다.";
				return;
			}

			if (string.Equals(Config.OwnerUserId, userId, StringComparison.Ordinal))
			{
				StatusMessage = "대표 관리자는 삭제할 수 없습니다.";
				return;
			}

			var removed = GetAdminUsers(Config).RemoveAll(x => string.Equals(x.UserId, userId, StringComparison.Ordinal));
			if (removed == 0)
			{
				StatusMessage = "삭제할 관리자를 찾을 수 없습니다.";
				return;
			}

			await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
			EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
			StatusMessage = "관리자 계정이 삭제되었습니다.";
		}

		public async Task UploadGalleryAsync(string slug, IBrowserFile file, CancellationToken ct = default)
		{
			IsUploading = true;
			StatusMessage = "";
			try
			{
				await _photos.UploadAsync(slug, file, ct).ConfigureAwait(false);
				// Config도 갱신해야 이후 설정 저장 시 GalleryFileNames가 날아가지 않음
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = $"{file.Name} 업로드 완료";
			}
			catch (Exception ex) { StatusMessage = $"업로드 오류: {ex.Message}"; }
			finally { IsUploading = false; }
		}

		public async Task UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default)
		{
			IsUploading = true;
			try
			{
				await _photos.UploadHeroAsync(slug, file, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "히어로 이미지 업로드 완료";
			}
			catch (Exception ex) { StatusMessage = $"히어로 업로드 오류: {ex.Message}"; }
			finally { IsUploading = false; }
		}

		public async Task DeletePhotoAsync(string slug, string fileName, CancellationToken ct = default)
		{
			try
			{
				await _photos.DeleteAsync(slug, fileName, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "삭제 완료";
			}
			catch (Exception ex) { StatusMessage = $"삭제 오류: {ex.Message}"; }
		}

		public async Task MoveGalleryPhotoAsync(string fileName, int offset, CancellationToken ct = default)
		{
			if (Config is null) return;
			try
			{
				NormalizeGalleryFileOrder();
				var index = Config.GalleryFileNames.FindIndex(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
				if (index < 0) return;

				var target = Math.Clamp(index + offset, 0, Config.GalleryFileNames.Count - 1);
				if (index == target) return;

				Config.GalleryFileNames.RemoveAt(index);
				Config.GalleryFileNames.Insert(target, fileName);
				await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
				Gallery = await _photos.GetGalleryAsync(Config.Slug, ct).ConfigureAwait(false);
				StatusMessage = "사진 노출 순서가 저장되었습니다.";
			}
			catch (Exception ex) { StatusMessage = $"순서 변경 오류: {ex.Message}"; }
		}

		public async Task MoveGalleryPhotoToAsync(string fileName, bool first, CancellationToken ct = default)
		{
			if (Config is null) return;
			try
			{
				NormalizeGalleryFileOrder();
				var index = Config.GalleryFileNames.FindIndex(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
				if (index < 0) return;

				Config.GalleryFileNames.RemoveAt(index);
				Config.GalleryFileNames.Insert(first ? 0 : Config.GalleryFileNames.Count, fileName);
				await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
				Gallery = await _photos.GetGalleryAsync(Config.Slug, ct).ConfigureAwait(false);
				StatusMessage = "사진 노출 순서가 저장되었습니다.";
			}
			catch (Exception ex) { StatusMessage = $"순서 변경 오류: {ex.Message}"; }
		}

		private void NormalizeGalleryFileOrder()
		{
			if (Config is null) return;
			var existing = Gallery.Select(x => x.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
			Config.GalleryFileNames = Config.GalleryFileNames
				.Where(existing.Contains)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var photo in Gallery)
			{
				if (!Config.GalleryFileNames.Contains(photo.FileName, StringComparer.OrdinalIgnoreCase))
				{
					Config.GalleryFileNames.Add(photo.FileName);
				}
			}
		}

		public async Task UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default)
		{
			IsUploading = true;
			try
			{
				await _photos.UploadRoadMapAsync(slug, file, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "약도 이미지 업로드 완료";
			}
			catch (Exception ex) { StatusMessage = $"약도 업로드 오류: {ex.Message}"; }
			finally { IsUploading = false; }
		}

		public async Task GeocodeAsync(CancellationToken ct = default)
		{
			if (Config is null) return;
			var query = string.IsNullOrWhiteSpace(Config.VenueAddress) ? Config.VenueName : Config.VenueAddress;
			if (string.IsNullOrWhiteSpace(query)) { GeocodeStatus = "❗ 예식장 이름 또는 주소를 먼저 입력해주세요."; return; }

			IsGeocoding = true;
			GeocodeStatus = "";
			try
			{
				var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1&accept-language=ko";
				var results = await _geocodeHttp.GetFromJsonAsync<JsonElement[]>(url, ct).ConfigureAwait(false);
				if (results is null || results.Length == 0)
				{
					GeocodeStatus = "❌ 좌표를 찾을 수 없습니다. 주소를 더 자세히 입력해 보세요.";
					return;
				}
				var item = results[0];
				Config.VenueLat = double.Parse(item.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
				Config.VenueLng = double.Parse(item.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
				GeocodeStatus = $"✅ 좌표 검색 완료 — {item.GetProperty("display_name").GetString()}";
			}
			catch { GeocodeStatus = "❌ 좌표 검색 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요."; }
			finally { IsGeocoding = false; }
		}

		public void GenerateMapLinks()
		{
			if (Config is null) return;

			var name = Uri.EscapeDataString(Config.VenueName);
			var addr = Uri.EscapeDataString(
				string.IsNullOrWhiteSpace(Config.VenueAddress) ? Config.VenueName : Config.VenueAddress);
			var lat = Config.VenueLat;
			var lng = Config.VenueLng;
			bool hasCoords = lat != 0 && lng != 0;
			var latS = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
			var lngS = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

			// 카카오맵 — 좌표 있으면 핀, 없으면 검색
			Config.MapLinkKakao = hasCoords
				? $"https://map.kakao.com/link/map/{name},{latS},{lngS}"
				: $"https://map.kakao.com/link/search/{addr}";

			// 네이버지도 — 좌표 있으면 좌표 검색, 없으면 주소 검색
			Config.MapLinkNaver = hasCoords
				? $"https://map.naver.com/v5/search/{addr}?c={lngS},{latS},15,0,0,0,dh"
				: $"https://map.naver.com/v5/search/{addr}";

			// 아틀란
			if (!string.IsNullOrWhiteSpace(_opts.AtlanAuthKey) && hasCoords)
				Config.MapLinkAtlan = $"http://m.atlan.co.kr/searchPlus/linkAtlan.do?shareType=kakao&coordX={lngS}&coordY={latS}&title={name}&AuthKey={_opts.AtlanAuthKey}";

			// T맵
			if (!string.IsNullOrWhiteSpace(_opts.TmapAppKey) && hasCoords)
				Config.MapLinkTmap = $"https://apis.openapi.sk.com/tmap/app/routes?appKey={_opts.TmapAppKey}&name={name}&lon={lngS}&lat={latS}";
		}

		public string GetThumbUrl(string slug, string fileName) => _photos.GetThumbUrl(slug, fileName);
		public string GetPhotoUrl(string slug, string fileName) => _photos.GetPhotoUrl(slug, fileName);
		public string GetHeroUrl(string slug, string fileName) => _photos.GetHeroUrl(slug, fileName);
		public string GetRoadMapUrl(string slug, string fileName) => _photos.GetRoadMapUrl(slug, fileName);
		public string GetMusicUrl(string slug, string fileName) => _photos.GetMusicUrl(slug, fileName);
		public string GetOgImageUrl(string slug, string fileName) => _photos.GetHeroUrl(slug, fileName);
		public string GetVideoUrl(string slug, string fileName) => _photos.GetVideoUrl(slug, fileName);

		public async Task UploadOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default)
		{
			IsUploading = true;
			try
			{
				await _photos.UploadOgImageAsync(slug, file, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "미리보기 이미지 업로드 완료";
			}
			catch (Exception ex) { StatusMessage = $"미리보기 이미지 오류: {ex.Message}"; }
			finally { IsUploading = false; }
		}

		public async Task UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default)
		{
			IsUploading = true;
			try
			{
				await _photos.UploadVideoAsync(slug, file, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "동영상 업로드 완료";
			}
			catch (Exception ex) { StatusMessage = $"동영상 업로드 오류: {ex.Message}"; }
			finally { IsUploading = false; }
		}

		public async Task DeleteVideoAsync(string slug, string fileName, CancellationToken ct = default)
		{
			try
			{
				await _photos.DeleteVideoAsync(slug, fileName, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "동영상 삭제 완료";
			}
			catch (Exception ex) { StatusMessage = $"동영상 삭제 오류: {ex.Message}"; }
		}

		public async Task UploadMusicAsync(string slug, IBrowserFile file, CancellationToken ct = default)
		{
			IsUploading = true;
			try
			{
				await _photos.UploadMusicAsync(slug, file, ct).ConfigureAwait(false);
				Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
				StatusMessage = "배경 음악 업로드 완료";
			}
			catch (Exception ex) { StatusMessage = $"음악 업로드 오류: {ex.Message}"; }
			finally { IsUploading = false; }
		}

		private static List<ThankYouAdminUser> GetAdminUsers(TenantConfig config) => config.AdminUsers ??= [];

		private static bool IsOwnerUser(TenantConfig config, ThankYouCurrentUser user) =>
			user.IsAuthenticated &&
			!string.IsNullOrWhiteSpace(config.OwnerUserId) &&
			string.Equals(config.OwnerUserId, user.Id, StringComparison.Ordinal);

		private static bool IsAdminUser(TenantConfig config, ThankYouCurrentUser user)
		{
			if (!user.IsAuthenticated) return false;
			if (IsOwnerUser(config, user)) return true;
			return GetAdminUsers(config).Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
		}

		private static void EnsureAdminUser(TenantConfig config, ThankYouCurrentUser user, string role)
		{
			if (!user.IsAuthenticated) return;

			var users = GetAdminUsers(config);
			var existing = users.FirstOrDefault(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
			if (existing is null)
			{
				users.Add(new ThankYouAdminUser
				{
					UserId = user.Id,
					Provider = user.Provider,
					Email = user.Email,
					DisplayName = user.DisplayName,
					Role = role,
					AddedAt = DateTime.Now
				});
				return;
			}

			existing.Provider = user.Provider;
			existing.Email = user.Email;
			existing.DisplayName = user.DisplayName;
			if (role == "Owner")
			{
				existing.Role = "Owner";
			}
		}

		private static IReadOnlyList<ThankYouAdminUser> BuildEffectiveAdminUsers(TenantConfig config)
		{
			var users = GetAdminUsers(config);
			if (!string.IsNullOrWhiteSpace(config.OwnerUserId) &&
				users.All(x => !string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
			{
				users.Insert(0, new ThankYouAdminUser
				{
					UserId = config.OwnerUserId,
					Provider = config.OwnerProvider,
					Email = config.OwnerEmail,
					DisplayName = config.OwnerDisplayName,
					Role = "Owner",
					AddedAt = config.OwnerLinkedAt ?? config.CreatedAt
				});
			}

			foreach (var user in users.Where(x => string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
			{
				user.Role = "Owner";
			}

			return users
				.OrderByDescending(x => string.Equals(x.Role, "Owner", StringComparison.OrdinalIgnoreCase))
				.ThenBy(x => x.AddedAt)
				.ToList();
		}
	}
}
