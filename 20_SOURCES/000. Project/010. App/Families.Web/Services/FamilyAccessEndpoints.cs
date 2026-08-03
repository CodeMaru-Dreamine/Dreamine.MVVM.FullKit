using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FamiliesApp.Services;

public static class FamilyAccessEndpoints
{
    private const long MaximumBodyBytes = 8 * 1024;
    private const int MaximumAttemptsPerWindow = 10;
    private const int MaximumTenantAttemptsPerWindow = 100;
    private const int MaximumTrackedAttemptKeys = 4096;
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, AttemptCounter> Attempts = new();
    private static readonly object AttemptAdmissionSync = new();
    private static readonly SemaphoreSlim PasswordVerificationSlots = new(4, 4);

    public static IEndpointConventionBuilder MapFamilyAccessUnlock(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/api/families/access/unlock", UnlockAsync)
            .DisableAntiforgery();
    }

    private static async Task<IResult> UnlockAsync(
        HttpContext context,
        IFamilyTenantStore tenants,
        FamilyAccessService access,
        CancellationToken ct)
    {
        if (!context.Request.HasFormContentType
            || context.Request.ContentLength is null
            || context.Request.ContentLength <= 0
            || context.Request.ContentLength > MaximumBodyBytes)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var form = await context.Request.ReadFormAsync(ct).ConfigureAwait(false);
        var slug = form["slug"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = SafeReturnUrl(form["returnUrl"].ToString(), slug);

        if (!FamilyAccessService.IsValidSlug(slug))
        {
            return Results.NotFound();
        }

        // Unknown slugs must not consume bounded rate-limit slots. Otherwise an attacker can
        // fill the table with fabricated tenants and force valid families into a global 429.
        var config = await tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null)
        {
            return Results.NotFound();
        }

        if (!TryAcquireAttempt(context, slug, out var retryAfterSeconds))
        {
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        if (!await PasswordVerificationSlots.WaitAsync(TimeSpan.Zero, ct).ConfigureAwait(false))
        {
            context.Response.Headers.RetryAfter = "1";
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        bool isValid;
        try
        {
            isValid = access.VerifyViewerPassword(config, password);
        }
        finally
        {
            PasswordVerificationSlots.Release();
        }

        if (!isValid)
        {
            return Results.Redirect(AddError(returnUrl));
        }

        access.IssueCookie(context, config);
        return Results.Redirect(returnUrl);
    }

    private static string SafeReturnUrl(string? value, string slug)
    {
        var fallback = FamilyAccessService.IsValidSlug(slug)
            ? $"/{Uri.EscapeDataString(slug)}"
            : "/";
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith("/", StringComparison.Ordinal)
            || value.StartsWith("//", StringComparison.Ordinal)
            || value.Contains('\\')
            || value.Contains('\r')
            || value.Contains('\n'))
        {
            return fallback;
        }

        var path = value.Split('?', '#')[0];
        if (ContainsEncodedPathControl(path))
        {
            return fallback;
        }

        string decodedPath;
        try
        {
            decodedPath = Uri.UnescapeDataString(path);
        }
        catch (UriFormatException)
        {
            return fallback;
        }

        if (decodedPath.Contains('%')
            || decodedPath.Contains('\\')
            || decodedPath.Contains("//", StringComparison.Ordinal))
        {
            return fallback;
        }

        var segments = decodedPath.Split('/', StringSplitOptions.None);
        if (segments.Length < 2
            || segments[0].Length != 0
            || !string.Equals(segments[1], slug, StringComparison.OrdinalIgnoreCase)
            || segments.Skip(1).Any(segment => segment is "." or ".." || segment.Length == 0))
        {
            return fallback;
        }

        return value;
    }

    private static bool ContainsEncodedPathControl(string path) =>
        path.Contains("%2e", StringComparison.OrdinalIgnoreCase)
        || path.Contains("%2f", StringComparison.OrdinalIgnoreCase)
        || path.Contains("%5c", StringComparison.OrdinalIgnoreCase)
        || path.Contains("%25", StringComparison.OrdinalIgnoreCase);

    private static bool TryAcquireAttempt(
        HttpContext context,
        string slug,
        out int retryAfterSeconds)
    {
        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!TryGetAttemptCounter($"tenant:{slug}", out var tenantCounter)
            || !TryGetAttemptCounter($"client:{address}|{slug}", out var clientCounter))
        {
            retryAfterSeconds = (int)AttemptWindow.TotalSeconds;
            return false;
        }

        var allowed = clientCounter.TryAcquire(MaximumAttemptsPerWindow, out retryAfterSeconds);
        if (allowed
            && !tenantCounter.TryAcquire(MaximumTenantAttemptsPerWindow, out retryAfterSeconds))
        {
            allowed = false;
        }

        return allowed;
    }

    private static bool TryGetAttemptCounter(string key, out AttemptCounter counter)
    {
        if (Attempts.TryGetValue(key, out counter!))
        {
            return true;
        }

        lock (AttemptAdmissionSync)
        {
            if (Attempts.TryGetValue(key, out counter!))
            {
                return true;
            }

            if (Attempts.Count >= MaximumTrackedAttemptKeys)
            {
                var staleBefore = DateTimeOffset.UtcNow - AttemptWindow - AttemptWindow;
                foreach (var entry in Attempts
                             .OrderBy(item => item.Value.LastSeenUtc)
                             .Where(item => item.Value.LastSeenUtc < staleBefore))
                {
                    Attempts.TryRemove(entry.Key, out _);
                    if (Attempts.Count < MaximumTrackedAttemptKeys)
                    {
                        break;
                    }
                }
            }

            if (Attempts.Count >= MaximumTrackedAttemptKeys)
            {
                counter = null!;
                return false;
            }

            counter = new AttemptCounter();
            Attempts[key] = counter;
            return true;
        }
    }

    private static string AddError(string returnUrl)
    {
        var fragmentIndex = returnUrl.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? returnUrl[fragmentIndex..] : string.Empty;
        var withoutFragment = fragmentIndex >= 0 ? returnUrl[..fragmentIndex] : returnUrl;
        var separator = withoutFragment.Contains('?') ? '&' : '?';
        return $"{withoutFragment}{separator}accessError=1{fragment}";
    }

    private sealed class AttemptCounter
    {
        private readonly object _sync = new();
        private DateTimeOffset _windowStartUtc = DateTimeOffset.UtcNow;
        private int _attempts;

        public DateTimeOffset LastSeenUtc { get; private set; } = DateTimeOffset.UtcNow;

        public bool TryAcquire(int maximumAttempts, out int retryAfterSeconds)
        {
            lock (_sync)
            {
                var now = DateTimeOffset.UtcNow;
                LastSeenUtc = now;
                if (now - _windowStartUtc >= AttemptWindow)
                {
                    _windowStartUtc = now;
                    _attempts = 0;
                }

                var remaining = AttemptWindow - (now - _windowStartUtc);
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
                if (_attempts >= maximumAttempts)
                {
                    return false;
                }

                _attempts++;
                return true;
            }
        }
    }
}
