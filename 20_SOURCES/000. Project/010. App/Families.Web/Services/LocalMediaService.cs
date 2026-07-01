using System.IO;
using FamiliesApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FamiliesApp.Services;

public sealed class LocalMediaService : IMediaService
{
    private readonly IFamilyTenantStore _tenants;
    private readonly IGlobalSettingsStore _globalSettings;

    private static readonly string[] AllowedImageExts = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] AllowedVideoExts = [".mp4", ".webm", ".mov", ".m4v"];

    public LocalMediaService(IFamilyTenantStore tenants, IGlobalSettingsStore globalSettings)
    {
        _tenants = tenants;
        _globalSettings = globalSettings;
    }

    private static long ToBytes(int mb) => mb <= 0 ? long.MaxValue : mb * 1024L * 1024L;

    private async Task<(long ImageBytes, long VideoBytes)> GetLimitsAsync(string slug, CancellationToken ct)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);

        var imageMb = config?.MaxImageSizeMb ?? settings.MaxImageSizeMb;
        var videoMb = config?.MaxVideoSizeMb ?? settings.MaxVideoSizeMb;
        return (ToBytes(imageMb), ToBytes(videoMb));
    }

    public async Task<string> UploadPostMediaAsync(string slug, string postId, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        bool isVideo = Array.Exists(AllowedVideoExts, e => e == ext);
        bool isImage = Array.Exists(AllowedImageExts, e => e == ext);

        if (!isVideo && !isImage)
            throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {ext}");

        var (imageLimit, videoLimit) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        var limit = isVideo ? videoLimit : imageLimit;
        if (file.Size > limit)
            throw new InvalidOperationException(isVideo
                ? $"동영상은 {FormatLimit(videoLimit)} 이하여야 합니다."
                : $"이미지는 {FormatLimit(imageLimit)} 이하여야 합니다.");

        var mediaDir = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId);
        Directory.CreateDirectory(mediaDir);

        var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
        var destPath = Path.Combine(mediaDir, fileName);

        await using var dest = File.Create(destPath);
        await file.OpenReadStream(limit, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        return fileName;
    }

    public async Task<string> UploadCoverAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Array.Exists(AllowedImageExts, e => e == ext))
            throw new InvalidOperationException($"허용되지 않는 이미지 형식입니다: {ext}");

        var (imageLimit, _) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        if (file.Size > imageLimit)
            throw new InvalidOperationException($"커버 이미지는 {FormatLimit(imageLimit)} 이하여야 합니다.");

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"cover{ext}";
        await using var dest = File.Create(Path.Combine(dir, fileName));
        await file.OpenReadStream(imageLimit, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new Models.FamilyConfig { Slug = slug };
        config.CoverImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    private static string FormatLimit(long bytes) =>
        bytes == long.MaxValue ? "무제한" : $"{bytes / (1024 * 1024)}MB";

    public Task DeletePostMediaAsync(string slug, string postId, string fileName, CancellationToken ct = default)
    {
        var path = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MediaInfo>> GetPostMediaAsync(string slug, string postId, CancellationToken ct = default)
    {
        var dir = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId);
        if (!Directory.Exists(dir)) return Task.FromResult<IReadOnlyList<MediaInfo>>([]);

        var list = Directory.GetFiles(dir)
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new MediaInfo
                {
                    FileName = fi.Name,
                    Url = GetMediaUrl(slug, postId, fi.Name),
                    ThumbUrl = GetThumbUrl(slug, postId, fi.Name),
                    SizeBytes = fi.Length,
                    LastModified = fi.LastWriteTime
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<MediaInfo>>(list);
    }

    public string GetMediaUrl(string slug, string postId, string fileName) =>
        $"/family-data/{slug}/media/{postId}/{Uri.EscapeDataString(fileName)}";

    public string GetThumbUrl(string slug, string postId, string fileName) =>
        GetMediaUrl(slug, postId, fileName);

    public string GetCoverUrl(string slug, string fileName) =>
        $"/family-data/{slug}/{Uri.EscapeDataString(fileName)}";
}
