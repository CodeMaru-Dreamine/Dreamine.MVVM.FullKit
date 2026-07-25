namespace DreamineWeb.Services;

public static class YouTubeUrlHelper
{
    public static string? GetThumbnailUrl(string? url)
    {
        var id = GetVideoId(url);
        return id is null ? null : $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";
    }

    private static string? GetVideoId(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.ToLowerInvariant();
        if (host is "youtu.be" or "www.youtu.be")
            return Clean(uri.AbsolutePath.Trim('/'));

        if (!host.EndsWith("youtube.com", StringComparison.Ordinal)) return null;
        if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            return query.TryGetValue("v", out var id) ? Clean(id.ToString()) : null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 && segments[0] is "embed" or "shorts" or "live"
            ? Clean(segments[1])
            : null;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var id = value.Split('?', '&', '#')[0];
        return id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_') ? id : null;
    }
}
