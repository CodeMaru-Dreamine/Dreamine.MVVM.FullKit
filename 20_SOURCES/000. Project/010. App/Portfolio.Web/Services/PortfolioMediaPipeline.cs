using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;

namespace PortfolioApp.Services;

/// <summary>
/// Serves public portfolio media without exposing tenant configuration, resumes,
/// project metadata, contact messages, or password hashes from the data root.
/// </summary>
internal static class PortfolioMediaPipeline
{
    private const string RequestPrefix = "/portfolio-data";

    /// <summary>
    /// Adds the restricted portfolio media branch before the Blazor catch-all route.
    /// </summary>
    public static void UsePortfolioMedia(this WebApplication app, PortfolioOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        var dataRoot = Path.GetFullPath(options.ResolvedDataPath);
        Directory.CreateDirectory(dataRoot);

        var contentTypes = CreateContentTypeProvider();

        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments(RequestPrefix, out var remaining))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (!HttpMethods.IsGet(context.Request.Method)
                && !HttpMethods.IsHead(context.Request.Method))
            {
                context.Response.Headers["Allow"] = "GET, HEAD";
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                return;
            }

            var relative = remaining.Value?.TrimStart('/') ?? string.Empty;
            var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget ?? string.Empty;
            if (HasEncodedPathControl(rawTarget)
                || !TryResolveMediaPath(dataRoot, relative, out var fullPath)
                || !File.Exists(fullPath)
                || !contentTypes.TryGetContentType(fullPath, out var contentType))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.Headers.CacheControl = "public, max-age=300";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            if (HttpMethods.IsHead(context.Request.Method))
            {
                context.Response.ContentType = contentType;
                context.Response.ContentLength = new FileInfo(fullPath).Length;
                context.Response.StatusCode = StatusCodes.Status200OK;
                return;
            }

            await Results.File(fullPath, contentType, enableRangeProcessing: true)
                .ExecuteAsync(context)
                .ConfigureAwait(false);
        });
    }

    private static FileExtensionContentTypeProvider CreateContentTypeProvider()
    {
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings.Clear();
        contentTypes.Mappings[".jpg"] = "image/jpeg";
        contentTypes.Mappings[".jpeg"] = "image/jpeg";
        contentTypes.Mappings[".png"] = "image/png";
        contentTypes.Mappings[".webp"] = "image/webp";
        contentTypes.Mappings[".gif"] = "image/gif";
        contentTypes.Mappings[".mp4"] = "video/mp4";
        contentTypes.Mappings[".webm"] = "video/webm";
        contentTypes.Mappings[".ogg"] = "video/ogg";
        return contentTypes;
    }

    private static bool HasEncodedPathControl(string rawTarget)
    {
        var queryIndex = rawTarget.IndexOf('?');
        var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;
        return rawPath.Contains("%2e", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains("%2f", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains("%5c", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains("%25", StringComparison.OrdinalIgnoreCase)
               || rawPath.Contains('\\')
               || rawPath.Contains("//", StringComparison.Ordinal);
    }

    private static bool TryResolveMediaPath(
        string dataRoot,
        string relativePath,
        out string fullPath)
    {
        fullPath = string.Empty;

        var segments = relativePath.Split('/', StringSplitOptions.None);
        if (segments.Length < 4
            || !IsValidIdentifier(segments[0])
            || !string.Equals(segments[1], "media", StringComparison.OrdinalIgnoreCase)
            || segments.Skip(2).Any(IsInvalidPathSegment))
        {
            return false;
        }

        try
        {
            var tenantRoot = Path.GetFullPath(Path.Combine(dataRoot, segments[0]));
            var dataPrefix = dataRoot.TrimEnd(
                                 Path.DirectorySeparatorChar,
                                 Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;
            if (!tenantRoot.StartsWith(dataPrefix, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(Path.Combine(tenantRoot, "config.json")))
            {
                return false;
            }

            var mediaRoot = Path.GetFullPath(Path.Combine(tenantRoot, "media"));
            var mediaPrefix = mediaRoot.TrimEnd(
                                  Path.DirectorySeparatorChar,
                                  Path.AltDirectorySeparatorChar)
                              + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(mediaRoot, Path.Combine(segments[2..])));
            if (!candidate.StartsWith(mediaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool IsValidIdentifier(string value) =>
        value.Length is > 0 and <= 80
        && value.All(character =>
            character is >= 'a' and <= 'z'
            or >= 'A' and <= 'Z'
            or >= '0' and <= '9'
            or '-' or '_');

    private static bool IsInvalidPathSegment(string segment) =>
        string.IsNullOrWhiteSpace(segment)
        || segment is "." or ".."
        || segment.Contains(':')
        || segment.Contains('\\')
        || segment.Contains('\0');
}
