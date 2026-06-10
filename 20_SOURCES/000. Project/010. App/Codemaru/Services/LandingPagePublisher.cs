using Codemaru.Models;
using System.IO;

namespace Codemaru.Services;

public sealed record LandingPagePublishResult(string FilePath, string PublicPath, string PublicUrl);

public sealed class LandingPagePublisher
{
    private static readonly char[] InvalidSegmentChars = Path.GetInvalidFileNameChars();

    public Task<LandingPagePublishResult> PublishIndexAsync(
        CardProfile profile,
        string html,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("저장할 HTML이 없습니다.");
        }

        var slugSegments = NormalizeSlug(profile.LandingSlug);
        if (slugSegments.Length == 0)
        {
            throw new InvalidOperationException("랜딩 슬러그를 입력해 주세요.");
        }

        var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var targetDirectory = webRoot;
        foreach (var segment in slugSegments)
        {
            targetDirectory = Path.Combine(targetDirectory, segment);
        }
        Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(targetDirectory, "index.html");
        return WriteAsync(profile, html, filePath, slugSegments, cancellationToken);
    }

    private static async Task<LandingPagePublishResult> WriteAsync(
        CardProfile profile,
        string html,
        string filePath,
        string[] slugSegments,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(filePath, html, cancellationToken).ConfigureAwait(false);

        var publicPath = "/" + string.Join("/", slugSegments) + "/index.html";
        var publicUrl = BuildPublicUrl(profile, slugSegments);

        return new LandingPagePublishResult(filePath, publicPath, publicUrl);
    }

    private static string[] NormalizeSlug(string slug)
    {
        return slug
            .Replace("\\", "/", StringComparison.Ordinal)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static segment => segment is not "." and not "..")
            .Select(NormalizeSegment)
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
    }

    private static string NormalizeSegment(string segment)
    {
        var sanitized = new string(segment
            .Trim()
            .Replace(' ', '-')
            .Select(static c => InvalidSegmentChars.Contains(c) ? '-' : c)
            .ToArray());

        return sanitized.Trim('.', '-');
    }

    private static string BuildPublicUrl(CardProfile profile, string[] slugSegments)
    {
        var website = string.IsNullOrWhiteSpace(profile.Website)
            ? string.Empty
            : profile.Website.Trim().TrimEnd('/');

        var slug = string.Join("/", slugSegments);
        if (string.IsNullOrWhiteSpace(website))
        {
            return "/" + slug + "/";
        }

        return website.EndsWith($"/{slug}", StringComparison.OrdinalIgnoreCase)
            ? website + "/"
            : $"{website}/{slug}/";
    }
}
