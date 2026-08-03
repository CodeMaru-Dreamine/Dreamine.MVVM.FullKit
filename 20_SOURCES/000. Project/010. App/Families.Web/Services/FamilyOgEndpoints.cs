using System.Globalization;
using System.IO;
using Dreamine.AppSecurity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FamiliesApp.Services;

/// <summary>
/// Exposes only the one tenant image explicitly selected for public Open Graph previews.
/// All other files beneath the tenant data root remain behind the authenticated
/// <c>/family-data</c> pipeline.
/// </summary>
public static class FamilyOgEndpoints
{
    private const string DefaultImagePath = "/img/og-platform.png";
    private const string PublicCacheControl = "public, max-age=3600, must-revalidate, stale-while-revalidate=86400";

    private static readonly IReadOnlyDictionary<string, string> ImageContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    public static IEndpointConventionBuilder MapFamilyOgImages(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/og/families/{slug}", HandleAsync);

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        string slug,
        IFamilyTenantStore tenants,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!FamilyAccessService.IsValidSlug(slug))
        {
            return DefaultImage(context);
        }

        try
        {
            var config = await tenants.GetAsync(slug, ct).ConfigureAwait(false);
            if (config is null
                || !TryValidateOgFileName(config.OgImageFileName, out var contentType))
            {
                return DefaultImage(context);
            }

            var tenantRoot = tenants.GetTenantDataPath(slug);
            var fullPath = StoragePathGuard.ResolveUnderRoot(tenantRoot, config.OgImageFileName);
            if (!File.Exists(fullPath)
                || (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
            {
                return DefaultImage(context);
            }

            var fileInfo = new FileInfo(fullPath);
            var entityTag = FormattableString.Invariant(
                $"W/\"{fileInfo.Length:x}-{fileInfo.LastWriteTimeUtc.Ticks:x}\"");

            context.Response.Headers[HeaderNames.CacheControl] = PublicCacheControl;
            context.Response.Headers[HeaderNames.ETag] = entityTag;
            context.Response.Headers[HeaderNames.LastModified] =
                fileInfo.LastWriteTimeUtc.ToString("R", CultureInfo.InvariantCulture);
            context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

            if (MatchesEntityTag(context.Request.Headers[HeaderNames.IfNoneMatch].ToString(), entityTag))
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            return Results.File(fullPath, contentType, enableRangeProcessing: true);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or InvalidOperationException
                or NotSupportedException)
        {
            loggerFactory.CreateLogger("FamiliesApp.Services.FamilyOgEndpoints")
                .LogWarning(exception, "The family OG image could not be served for {Slug}.", slug);
            return DefaultImage(context);
        }
    }

    private static bool TryValidateOgFileName(string? fileName, out string contentType)
    {
        contentType = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName)
            || !fileName.StartsWith("og_", StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            return false;
        }

        return ImageContentTypes.TryGetValue(Path.GetExtension(fileName), out contentType!);
    }

    private static bool MatchesEntityTag(string ifNoneMatch, string entityTag) =>
        !string.IsNullOrWhiteSpace(ifNoneMatch)
        && ifNoneMatch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(candidate => candidate == "*" || string.Equals(candidate, entityTag, StringComparison.Ordinal));

    private static IResult DefaultImage(HttpContext context)
    {
        context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=300";
        context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return Results.Redirect(DefaultImagePath);
    }
}
