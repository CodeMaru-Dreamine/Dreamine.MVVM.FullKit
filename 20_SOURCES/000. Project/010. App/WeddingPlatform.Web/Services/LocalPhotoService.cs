using System.IO;
using Microsoft.AspNetCore.Components.Forms;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

public sealed class LocalPhotoService : IPhotoService
{
    private readonly ITenantStore _tenants;
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;
    private static readonly string[] AllowedExts = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public LocalPhotoService(ITenantStore tenants) => _tenants = tenants;

    public async Task<IReadOnlyList<PhotoInfo>> GetGalleryAsync(string slug, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return [];

        var list = new List<PhotoInfo>();
        foreach (var fn in config.GalleryFileNames)
        {
            var full = GalleryPath(slug, fn);
            if (!File.Exists(full)) continue;
            var fi = new FileInfo(full);
            list.Add(new PhotoInfo
            {
                FileName = fn,
                Url = GetPhotoUrl(slug, fn),
                ThumbUrl = GetThumbUrl(slug, fn),
                SizeBytes = fi.Length,
                LastModified = fi.LastWriteTime
            });
        }
        return list;
    }

    public async Task<string> UploadAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateFile(file);
        var fileName = UniqueFileName(file.Name);
        var galleryDir = Path.Combine(_tenants.GetTenantDataPath(slug), "gallery");
        var thumbDir = Path.Combine(_tenants.GetTenantDataPath(slug), "thumb");
        Directory.CreateDirectory(galleryDir);
        Directory.CreateDirectory(thumbDir);

        var destPath = Path.Combine(galleryDir, fileName);
        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(MaxFileSizeBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        File.Copy(destPath, Path.Combine(thumbDir, fileName), overwrite: true);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        if (!config.GalleryFileNames.Contains(fileName))
            config.GalleryFileNames.Add(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateFile(file);
        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var fileName = $"hero{ext}";
        var destPath = Path.Combine(dir, fileName);

        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(MaxFileSizeBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        config.HeroImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateFile(file);
        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var fileName = $"roadmap{ext}";
        var destPath = Path.Combine(dir, fileName);

        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(MaxFileSizeBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        config.RoadMapFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task DeleteAsync(string slug, string fileName, CancellationToken ct = default)
    {
        var galleryFile = GalleryPath(slug, fileName);
        var thumbFile = Path.Combine(_tenants.GetTenantDataPath(slug), "thumb", fileName);
        if (File.Exists(galleryFile)) File.Delete(galleryFile);
        if (File.Exists(thumbFile)) File.Delete(thumbFile);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.GalleryFileNames.Remove(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
    }

    public string GetPhotoUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/gallery/{Uri.EscapeDataString(fileName)}";

    public string GetThumbUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/thumb/{Uri.EscapeDataString(fileName)}";

    public string GetHeroUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    public string GetRoadMapUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    public async Task<string> UploadMusicAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not (".mp3" or ".ogg" or ".wav" or ".aac" or ".m4a"))
            throw new InvalidOperationException($"허용되지 않는 오디오 형식입니다: {ext}");
        if (file.Size > 30 * 1024 * 1024)
            throw new InvalidOperationException("음악 파일은 30MB 이하여야 합니다.");

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"bgmusic{ext}";
        var destPath = Path.Combine(dir, fileName);
        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(30 * 1024 * 1024, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        config.MusicFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public string GetMusicUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    public async Task<string> UploadOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateFile(file);
        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var fileName = $"og{ext}";
        var destPath = Path.Combine(dir, fileName);

        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(MaxFileSizeBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        config.OgImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not (".mp4" or ".webm" or ".mov" or ".m4v"))
            throw new InvalidOperationException($"허용되지 않는 동영상 형식입니다: {ext}. mp4/webm/mov 권장");
        if (file.Size > 200 * 1024 * 1024)
            throw new InvalidOperationException("동영상 파일은 200MB 이하여야 합니다.");

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        if (config.VideoFileNames.Count >= 6)
            throw new InvalidOperationException("동영상은 최대 6개까지 업로드할 수 있습니다.");

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"wedding-video-{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
        var destPath = Path.Combine(dir, fileName);
        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(200 * 1024 * 1024, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        config.VideoFileNames.Add(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task DeleteVideoAsync(string slug, string fileName, CancellationToken ct = default)
    {
        var path = Path.Combine(_tenants.GetTenantDataPath(slug), fileName);
        if (File.Exists(path)) File.Delete(path);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.VideoFileNames.Remove(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
    }

    public string GetVideoUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    private string GalleryPath(string slug, string fileName) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "gallery", fileName);

    private static void ValidateFile(IBrowserFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Array.Exists(AllowedExts, e => e == ext))
            throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {ext}");
        if (file.Size > MaxFileSizeBytes)
            throw new InvalidOperationException("파일 크기는 20MB 이하여야 합니다.");
    }

    private static string UniqueFileName(string originalName)
    {
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        return $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
    }
}
