using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

public sealed class WeddingAdminViewModel
{
    private readonly ITenantStore _tenants;
    private readonly IPhotoService _photos;
    private readonly WeddingOptions _opts;

    private static readonly HttpClient _geocodeHttp = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CodemaruWeddingPlatform/1.0 (contact: admin@codemaru.co.kr)" } }
    };

    public WeddingAdminViewModel(ITenantStore tenants, IPhotoService photos, WeddingOptions opts)
    {
        _tenants = tenants;
        _photos = photos;
        _opts = opts;
    }

    public TenantConfig? Config { get; private set; }
    public IReadOnlyList<PhotoInfo> Gallery { get; private set; } = [];
    public bool IsLoaded { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public string StatusMessage { get; private set; } = "";
    public bool IsUploading { get; private set; }
    public bool IsGeocoding { get; private set; }
    public string GeocodeStatus { get; private set; } = "";

    public string LoginPassword { get; set; } = "";

    public async Task<bool> LoginAsync(string slug, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) { StatusMessage = "존재하지 않는 슬러그입니다."; return false; }

        IsAuthenticated = config.PasswordHash == LoginPassword;
        StatusMessage = IsAuthenticated ? "" : "비밀번호가 틀렸습니다.";
        return IsAuthenticated;
    }

    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                 ?? new TenantConfig { Slug = slug };
        Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    public async Task SaveConfigAsync(CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            StatusMessage = "설정이 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
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
}
