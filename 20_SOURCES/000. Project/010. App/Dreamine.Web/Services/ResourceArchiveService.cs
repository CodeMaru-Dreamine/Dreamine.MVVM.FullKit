using System.IO;
using System.IO.Compression;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace DreamineWeb.Services;

/// <summary>관리자가 공개할 샘플 프로젝트 ZIP을 저장하고 검증합니다.</summary>
public sealed class ResourceArchiveService
{
    private const long MaxArchiveSize = 500L * 1024 * 1024;
    private const long MaxThumbnailSize = 10L * 1024 * 1024;
    private readonly string _uploadRoot = Path.Combine(
        AppContext.BaseDirectory, "wwwroot", "uploads", "resources");

    [SuppressMessage("Security", "S5693", Justification = "The file size is checked and OpenReadStream enforces MaxArchiveSize before any data is copied.")]
    public async Task<string> SaveArchiveAsync(IBrowserFile file, string resourceId, CancellationToken cancellationToken = default)
    {
        if (file.Size <= 0)
            throw new InvalidOperationException("빈 파일은 업로드할 수 없습니다.");
        if (file.Size > MaxArchiveSize)
            throw new InvalidOperationException("ZIP 파일은 최대 500MB까지 업로드할 수 있습니다.");
        if (!string.Equals(Path.GetExtension(file.Name), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ZIP 파일만 업로드할 수 있습니다.");

        Directory.CreateDirectory(_uploadRoot);
        var folder = SafeSegment(resourceId);
        var targetDirectory = Path.Combine(_uploadRoot, folder);
        Directory.CreateDirectory(targetDirectory);
        var fileName = $"sample-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
        var path = Path.Combine(targetDirectory, fileName);

        try
        {
            await using (var input = file.OpenReadStream(MaxArchiveSize, cancellationToken))
            await using (var output = File.Create(path))
                await input.CopyToAsync(output, cancellationToken);

            using var archive = ZipFile.OpenRead(path);
            _ = archive.Entries.Count;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw new InvalidOperationException("올바른 ZIP 압축 파일인지 확인해주세요.");
        }

        return $"/uploads/resources/{folder}/{fileName}";
    }

    [SuppressMessage("Security", "S5693", Justification = "The file size is checked and OpenReadStream enforces MaxThumbnailSize before any data is copied.")]
    public async Task<string> SaveThumbnailAsync(IBrowserFile file, string resourceId, CancellationToken cancellationToken = default)
    {
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ".jpg", [".jpeg"] = ".jpg", [".png"] = ".png",
            [".webp"] = ".webp", [".gif"] = ".gif"
        };
        var extension = Path.GetExtension(file.Name);
        if (file.Size <= 0 || file.Size > MaxThumbnailSize)
            throw new InvalidOperationException("썸네일은 10MB 이하 이미지만 업로드할 수 있습니다.");
        if (!allowed.TryGetValue(extension, out var normalizedExtension))
            throw new InvalidOperationException("JPG, PNG, WebP 또는 GIF 이미지만 업로드할 수 있습니다.");

        var folder = SafeSegment(resourceId);
        var targetDirectory = Path.Combine(_uploadRoot, folder);
        Directory.CreateDirectory(targetDirectory);
        var fileName = $"thumbnail-{DateTime.UtcNow:yyyyMMddHHmmss}{normalizedExtension}";
        var path = Path.Combine(targetDirectory, fileName);
        await using var input = file.OpenReadStream(MaxThumbnailSize, cancellationToken);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, cancellationToken);
        return $"/uploads/resources/{folder}/{fileName}";
    }

    private static string SafeSegment(string value)
    {
        var safe = new string(value.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "resource" : safe;
    }
}
