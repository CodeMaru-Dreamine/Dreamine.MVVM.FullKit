using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using Wedding.Common;
using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

public sealed class WeddingAdminViewModel
{
    private readonly ITenantStore _tenants;
    private readonly IPhotoService _photos;
    private readonly WeddingOptions _opts;
    private readonly IGlobalSettingsStore _globalSettings;
    private readonly WeddingUserContext _userContext;

    private static readonly HttpClient _geocodeHttp = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CodemaruWeddingPlatform/1.0 (contact: admin@codemaru.co.kr)" } }
    };

    public WeddingAdminViewModel(
        ITenantStore tenants,
        IPhotoService photos,
        WeddingOptions opts,
        IGlobalSettingsStore globalSettings,
        WeddingUserContext userContext)
    {
        _tenants = tenants;
        _photos = photos;
        _opts = opts;
        _globalSettings = globalSettings;
        _userContext = userContext;
    }

    /// <summary>동영상 업로드 최대 용량 안내 문구 (예: "최대 200MB" 또는 "무제한").</summary>
    public string MaxVideoSizeLabel { get; private set; } = "최대 200MB";
    /// <summary>동영상 업로드 최대 개수 (0이면 무제한).</summary>
    public int MaxVideoCount { get; private set; } = 6;

    public TenantConfig? Config { get; private set; }
    public IReadOnlyList<PhotoInfo> Gallery { get; private set; } = [];
    public bool IsLoaded { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool IsSignedIn { get; private set; }
    public bool IsLinkedToCurrentUser { get; private set; }
    public bool IsOwner { get; private set; }
    public IReadOnlyList<WeddingAdminUser> EffectiveAdminUsers { get; private set; } = [];
    public string StatusMessage { get; private set; } = "";
    public string CurrentUserLabel { get; private set; } = "";
    public bool IsUploading { get; private set; }
    public bool IsGeocoding { get; private set; }
    public string GeocodeStatus { get; private set; } = "";

    public string LoginPassword { get; set; } = "";

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
            StatusMessage = "현재 CodeMaru/Dreamine 계정이 이 청첩장의 관리자로 추가되었습니다.";
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

    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                 ?? new TenantConfig { Slug = slug };
        InvitationDesignCatalog.Normalize(Config);
        Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);

        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);

        var effectiveMb = Config.MaxVideoSizeMb ?? settings.MaxVideoSizeMb;
        MaxVideoSizeLabel = effectiveMb <= 0 ? "무제한" : $"최대 {effectiveMb}MB";

        MaxVideoCount = Config.MaxVideoCount ?? settings.MaxVideoCount;

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

            InvitationDesignCatalog.Normalize(Config);
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
            StatusMessage = "설정이 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
    }

    public void SetStatusMessage(string message) => StatusMessage = message;

    private bool ValidateLayoutForSave(TenantConfig config)
    {
        config.DesignSettings ??= new DesignSettings();
        if (!WeddingLayoutCatalog.IsKnownKey(config.InvitationStyle))
        {
            StatusMessage = "저장 오류: 존재하지 않는 레이아웃입니다.";
            return false;
        }

        var mode = config.DesignSettings.LayoutMode;
        if (mode == WeddingLayoutMode.Unknown)
        {
            mode = InvitationDesignCatalog.FromLegacyLayoutKey(config.InvitationStyle);
            config.DesignSettings.LayoutMode = mode;
        }

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
            Config.MapLinkAtlan = $"http://m.atlan.co.kr/searchPlus/linkAtlan.do?shareType=kakao&coordX={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&coordY={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&title={name}&AuthKey={_opts.AtlanAuthKey}";

        // T맵
        if (!string.IsNullOrWhiteSpace(_opts.TmapAppKey) && hasCoords)
            Config.MapLinkTmap = $"https://apis.openapi.sk.com/tmap/app/routes?appKey={_opts.TmapAppKey}&name={name}&lon={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
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

    private static List<WeddingAdminUser> GetAdminUsers(TenantConfig config) => config.AdminUsers ??= [];

    private static bool IsOwnerUser(TenantConfig config, WeddingCurrentUser user) =>
        user.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(config.OwnerUserId) &&
        string.Equals(config.OwnerUserId, user.Id, StringComparison.Ordinal);

    private static bool IsAdminUser(TenantConfig config, WeddingCurrentUser user)
    {
        if (!user.IsAuthenticated) return false;
        if (IsOwnerUser(config, user)) return true;
        return GetAdminUsers(config).Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
    }

    private static void EnsureAdminUser(TenantConfig config, WeddingCurrentUser user, string role)
    {
        if (!user.IsAuthenticated) return;

        var users = GetAdminUsers(config);
        var existing = users.FirstOrDefault(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
        if (existing is null)
        {
            users.Add(new WeddingAdminUser
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

    private static IReadOnlyList<WeddingAdminUser> BuildEffectiveAdminUsers(TenantConfig config)
    {
        var users = GetAdminUsers(config);
        if (!string.IsNullOrWhiteSpace(config.OwnerUserId) &&
            users.All(x => !string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
        {
            users.Insert(0, new WeddingAdminUser
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

    private static void NormalizeHeroPanelPosition(TenantConfig config)
    {
        InvitationDesignCatalog.Normalize(config);
        config.InviteHeroTopVerticalDesktop = NormalizeOption(config.InviteHeroTopVerticalDesktop, ["top", "middle", "bottom"], "top");
        config.InviteHeroTopHorizontalDesktop = NormalizeOption(config.InviteHeroTopHorizontalDesktop, ["left", "center", "right"], "center");
        config.InviteHeroTopVerticalMobile = NormalizeOption(config.InviteHeroTopVerticalMobile, ["top", "middle", "bottom"], "top");
        config.InviteHeroTopHorizontalMobile = NormalizeOption(config.InviteHeroTopHorizontalMobile, ["left", "center", "right"], "center");
        config.InviteHeroBottomVerticalDesktop = NormalizeOption(config.InviteHeroBottomVerticalDesktop, ["top", "middle", "bottom"], "bottom");
        config.InviteHeroBottomHorizontalDesktop = NormalizeOption(config.InviteHeroBottomHorizontalDesktop, ["left", "center", "right"], "center");
        config.InviteHeroBottomVerticalMobile = NormalizeOption(config.InviteHeroBottomVerticalMobile, ["top", "middle", "bottom"], "bottom");
        config.InviteHeroBottomHorizontalMobile = NormalizeOption(config.InviteHeroBottomHorizontalMobile, ["left", "center", "right"], "center");
        config.HeroPanelVerticalDesktop = NormalizeOption(config.HeroPanelVerticalDesktop, ["top", "middle", "bottom"], "top");
        config.HeroPanelHorizontalDesktop = NormalizeOption(config.HeroPanelHorizontalDesktop, ["left", "center", "right"], "center");
        config.HeroPanelVerticalMobile = NormalizeOption(config.HeroPanelVerticalMobile, ["top", "middle", "bottom"], "top");
        config.HeroPanelHorizontalMobile = NormalizeOption(config.HeroPanelHorizontalMobile, ["left", "center", "right"], "center");
    }

    private static string NormalizeOption(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) && allowed.Contains(normalized) ? normalized : fallback;
    }
}
