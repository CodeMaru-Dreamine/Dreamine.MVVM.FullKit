using System.IO;
using Microsoft.AspNetCore.Components.Forms;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

public sealed class LocalPhotoService : IPhotoService
{
    private readonly ITenantStore _tenants;
    private readonly IMediaQuotaPolicyResolver _policyResolver;
    private readonly IImageOptimizationService _imageOptimization;
    private readonly IMediaUsageQueryService _usageQuery;
    private const long MaxImageUploadBytes = 200 * 1024 * 1024;
    private static readonly string[] AllowedExts = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public LocalPhotoService(
        ITenantStore tenants,
        IMediaQuotaPolicyResolver policyResolver,
        IImageOptimizationService imageOptimization,
        IMediaUsageQueryService usageQuery)
    {
        _tenants = tenants;
        _policyResolver = policyResolver;
        _imageOptimization = imageOptimization;
        _usageQuery = usageQuery;
    }

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
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: true, ct).ConfigureAwait(false);

        var baseName = Path.GetFileNameWithoutExtension(UniqueFileName(file.Name));
        var galleryDir = Path.Combine(_tenants.GetTenantDataPath(slug), "gallery");
        var thumbDir = Path.Combine(_tenants.GetTenantDataPath(slug), "thumb");
        Directory.CreateDirectory(galleryDir);
        Directory.CreateDirectory(thumbDir);

        var fileName = await SaveImageFileAsync(slug, file, galleryDir, baseName, policy, ct).ConfigureAwait(false);
        File.Copy(Path.Combine(galleryDir, fileName), Path.Combine(thumbDir, fileName), overwrite: true);

        if (!config.GalleryFileNames.Contains(fileName))
            config.GalleryFileNames.Add(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "hero", policy, ct).ConfigureAwait(false);
        config.HeroImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "roadmap", policy, ct).ConfigureAwait(false);
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
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "og", policy, ct).ConfigureAwait(false);
        config.OgImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not (".mp4" or ".webm" or ".mov" or ".m4v"))
            throw new InvalidOperationException($"허용되지 않는 동영상 형식입니다: {ext}. mp4/webm/mov 권장");

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);

        var effectiveCount = policy.VideoMaxCount;
        if (effectiveCount > 0 && config.VideoFileNames.Count >= effectiveCount)
            throw new InvalidOperationException($"동영상은 최대 {effectiveCount}개까지 업로드할 수 있습니다.");

        var effectiveMb = policy.VideoMaxFileSizeMb;
        var maxBytes = policy.VideoMaxFileSizeBytes;

        if (file.Size > maxBytes)
            throw new InvalidOperationException($"동영상 파일은 {effectiveMb}MB 이하여야 합니다.");

        if (policy.VideoMaxStorageMb > 0)
        {
            var usage = await _usageQuery.GetTenantUsageAsync(config, ct).ConfigureAwait(false);
            if (usage.VideoBytes + file.Size > policy.VideoMaxStorageBytes)
            {
                throw new InvalidOperationException($"동영상 저장 용량은 최대 {policy.VideoMaxStorageMb}MB까지 사용할 수 있습니다.");
            }
        }

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"video-{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
        var destPath = Path.Combine(dir, fileName);
        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(maxBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

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

    private async Task ValidateImageQuotaAsync(TenantConfig config, EffectiveMediaPolicy policy, IBrowserFile file, bool countGalleryImage, CancellationToken ct)
    {
        if (countGalleryImage && policy.ImageMaxCount > 0 && config.GalleryFileNames.Count >= policy.ImageMaxCount)
        {
            throw new InvalidOperationException($"이미지는 최대 {policy.ImageMaxCount}장까지 업로드할 수 있습니다.");
        }

        if (policy.ImageOptimizedMaxStorageMb > 0)
        {
            var usage = await _usageQuery.GetTenantUsageAsync(config, ct).ConfigureAwait(false);
            if (usage.OptimizedImageBytes + file.Size > policy.ImageOptimizedMaxStorageBytes)
            {
                throw new InvalidOperationException($"이미지 저장 용량은 최대 {policy.ImageOptimizedMaxStorageMb}MB까지 사용할 수 있습니다.");
            }
        }
    }

    private async Task<string> SaveImageFileAsync(string slug, IBrowserFile file, string destinationDirectory, string baseName, EffectiveMediaPolicy policy, CancellationToken ct)
    {
        var root = _tenants.GetTenantDataPath(slug);
        var originalDir = Path.Combine(root, "original");
        var originalExt = Path.GetExtension(file.Name).ToLowerInvariant();
        var tempPath = Path.Combine(destinationDirectory, $"{baseName}.upload{originalExt}");
        Directory.CreateDirectory(destinationDirectory);

        await using (var dest = File.Create(tempPath))
        {
            await file.OpenReadStream(MaxImageUploadBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);
        }

        if (policy.KeepOriginalImages)
        {
            Directory.CreateDirectory(originalDir);
            File.Copy(tempPath, Path.Combine(originalDir, $"{baseName}{originalExt}"), overwrite: true);
        }

        var outputFormat = ResolveWritableOutputFormat(policy.ImageOutputFormat, originalExt);
        if (!string.IsNullOrWhiteSpace(outputFormat))
        {
            var outputExt = $".{outputFormat}";
            var optimizedName = $"{baseName}{outputExt}";
            var optimizedPath = Path.Combine(destinationDirectory, optimizedName);
            var tempOptimizedPath = Path.Combine(destinationDirectory, $"{baseName}.optimized.tmp{outputExt}");
            var result = await _imageOptimization.OptimizeAsync(tempPath, tempOptimizedPath, policy, ct).ConfigureAwait(false);
            if (result.Succeeded && File.Exists(tempOptimizedPath))
            {
                File.Move(tempOptimizedPath, optimizedPath, overwrite: true);
                File.Delete(tempPath);
                return optimizedName;
            }

            if (File.Exists(tempOptimizedPath))
            {
                File.Delete(tempOptimizedPath);
            }
        }

        var fallbackName = $"{baseName}{originalExt}";
        var fallbackPath = Path.Combine(destinationDirectory, fallbackName);
        File.Move(tempPath, fallbackPath, overwrite: true);
        return fallbackName;
    }

    private string ResolveWritableOutputFormat(string? preferredFormat, string? originalExtension)
    {
        var preferred = NormalizeOutputExtension(preferredFormat);
        if (_imageOptimization.CanEncode(preferred)) return preferred;

        if (_imageOptimization.CanEncode("jpg")) return "jpg";

        var original = NormalizeOutputExtension(originalExtension);
        return _imageOptimization.CanEncode(original) ? original : "";
    }

    private static void ValidateImageFile(IBrowserFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Array.Exists(AllowedExts, e => e == ext))
            throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {ext}");
        if (file.Size > MaxImageUploadBytes)
            throw new InvalidOperationException("이미지 파일 크기는 200MB 이하여야 합니다.");
    }

    private static string NormalizeOutputExtension(string? outputFormat)
    {
        var normalized = outputFormat?.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "webp" : normalized;
    }

    private static string UniqueFileName(string originalName)
    {
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        return $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
    }
}
