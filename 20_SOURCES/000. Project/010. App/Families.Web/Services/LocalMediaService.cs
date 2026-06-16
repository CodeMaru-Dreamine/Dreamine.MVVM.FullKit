using System.IO;
using FamiliesApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FamiliesApp.Services;

public sealed class LocalMediaService : IMediaService
{
    private readonly IFamilyTenantStore _tenants;
    private const long MaxImageBytes = 20 * 1024 * 1024;
    private const long MaxVideoBytes = 500 * 1024 * 1024;

    private static readonly string[] AllowedImageExts = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] AllowedVideoExts = [".mp4", ".webm", ".mov", ".m4v"];

    public LocalMediaService(IFamilyTenantStore tenants) => _tenants = tenants;

    public async Task<string> UploadPostMediaAsync(string slug, string postId, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        bool isVideo = Array.Exists(AllowedVideoExts, e => e == ext);
        bool isImage = Array.Exists(AllowedImageExts, e => e == ext);

        if (!isVideo && !isImage)
            throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {ext}");

        long limit = isVideo ? MaxVideoBytes : MaxImageBytes;
        if (file.Size > limit)
            throw new InvalidOperationException(isVideo ? "동영상은 500MB 이하여야 합니다." : "이미지는 20MB 이하여야 합니다.");

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
        if (file.Size > MaxImageBytes)
            throw new InvalidOperationException("커버 이미지는 20MB 이하여야 합니다.");

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"cover{ext}";
        await using var dest = File.Create(Path.Combine(dir, fileName));
        await file.OpenReadStream(MaxImageBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new Models.FamilyConfig { Slug = slug };
        config.CoverImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

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
