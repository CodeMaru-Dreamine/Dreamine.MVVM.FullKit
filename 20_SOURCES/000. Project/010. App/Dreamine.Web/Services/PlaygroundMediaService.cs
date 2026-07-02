using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;

namespace DreamineWeb.Services;

public sealed class PlaygroundMediaService
{
    private static readonly Regex UnsafeName = new(@"[^a-zA-Z0-9._-]+", RegexOptions.Compiled);
    private readonly string _uploadRoot;

    public PlaygroundMediaService()
    {
        _uploadRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "playground");
    }

    public async Task<string> SaveAsync(IBrowserFile file, string demoId, string platform, CancellationToken ct = default)
    {
        if (file.Size <= 0) throw new InvalidOperationException("Empty files cannot be uploaded.");

        var safeDemoId = SafeSegment(demoId, "demo");
        var safePlatform = SafeSegment(platform, "platform");
        var extension = Path.GetExtension(file.Name);
        if (string.IsNullOrWhiteSpace(extension)) extension = GuessExtension(file.ContentType);

        var targetDir = Path.Combine(_uploadRoot, safeDemoId);
        Directory.CreateDirectory(targetDir);

        var fileName = $"{safePlatform}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}{extension.ToLowerInvariant()}";
        var path = Path.Combine(targetDir, fileName);

        await using var input = file.OpenReadStream(maxAllowedSize: 200 * 1024 * 1024, cancellationToken: ct);
        await using var output = File.Create(path);
        await input.CopyToAsync(output, ct);

        return $"/uploads/playground/{safeDemoId}/{fileName}";
    }

    private static string SafeSegment(string? value, string fallback)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var safe = UnsafeName.Replace(value, "-").Trim('-', '.');
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    private static string GuessExtension(string? contentType) => contentType?.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/svg+xml" => ".svg",
        "video/mp4" => ".mp4",
        "video/webm" => ".webm",
        "video/quicktime" => ".mov",
        _ => ".bin"
    };
}
