using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dreamine.AppSecurity;
using Dreamine.Identity;
using FamiliesApp.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace FamiliesApp.Services;

/// <summary>Issues and validates tenant-bound guest access grants.</summary>
public sealed class FamilyAccessService
{
    private const string CookiePrefix = "families-access-";
    private const string GuestClaimType = "families:guest-access";
    private static readonly TimeSpan GrantLifetime = TimeSpan.FromHours(24);
    private readonly IDataProtector _protector;

    public FamilyAccessService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("Families.Web.GuestAccess.v1");
    }

    public static string EffectiveViewerPasswordHash(FamilyConfig config) =>
        string.IsNullOrWhiteSpace(config.ViewerPasswordHash)
            ? config.PasswordHash
            : config.ViewerPasswordHash;

    public static bool UsesLegacyAdminPassword(FamilyConfig config) =>
        string.IsNullOrWhiteSpace(config.ViewerPasswordHash);

    public static bool IsValidSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)
            || slug.Length > StoragePathGuard.MaxIdentifierLength
            || !slug.IsNormalized(NormalizationForm.FormC))
        {
            return false;
        }

        return slug.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    }

    /// <summary>
    /// Validates the narrower public URL rule used only when a new tenant is created.
    /// Existing Unicode or underscore slugs remain readable for backward compatibility.
    /// </summary>
    public static bool IsValidNewSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= StoragePathGuard.MaxIdentifierLength
        && slug.All(character =>
            character is >= 'a' and <= 'z'
            || character is >= '0' and <= '9'
            || character == '-');

    public bool VerifyViewerPassword(FamilyConfig config, string? password)
    {
        var effectiveHash = EffectiveViewerPasswordHash(config);
        return !string.IsNullOrWhiteSpace(password)
               && !string.IsNullOrWhiteSpace(effectiveHash)
               && DreaminePasswordHasher.VerifyPassword(password, effectiveHash);
    }

    public bool HasAccess(FamilyConfig config, ClaimsPrincipal principal)
    {
        var current = FamilyUserContext.FromPrincipal(principal);
        if (current.IsAuthenticated
            && (string.Equals(config.OwnerUserId, current.Id, StringComparison.Ordinal)
                || config.AdminUsers.Any(admin =>
                    string.Equals(admin.UserId, current.Id, StringComparison.Ordinal))))
        {
            return true;
        }

        var expected = BuildClaimValue(config.Slug, EffectiveViewerPasswordHash(config));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return principal.FindAll(GuestClaimType).Any(claim =>
            TryReadClaim(claim.Value, out var value, out var expiresAt)
            && expiresAt > now
            && FixedTimeEquals(value, expected));
    }

    public void AddGuestClaims(ClaimsPrincipal principal, IRequestCookieCollection cookies)
    {
        var claims = new List<Claim>();
        foreach (var cookie in cookies)
        {
            if (!cookie.Key.StartsWith(CookiePrefix, StringComparison.Ordinal)
                || !TryReadGrant(cookie.Value, out var grant)
                || !IsValidSlug(grant.Slug)
                || grant.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                continue;
            }

            claims.Add(new Claim(
                GuestClaimType,
                $"{grant.Slug}|{grant.PasswordFingerprint}|{grant.ExpiresAtUtc.ToUnixTimeSeconds()}"));
        }

        if (claims.Count > 0)
        {
            principal.AddIdentity(new ClaimsIdentity(claims, "FamiliesGuestAccess"));
        }
    }

    public void IssueCookie(HttpContext context, FamilyConfig config)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(GrantLifetime);
        var grant = new AccessGrant(
            config.Slug,
            Fingerprint(EffectiveViewerPasswordHash(config)),
            expiresAt);
        var protectedValue = _protector.Protect(JsonSerializer.Serialize(grant));

        context.Response.Cookies.Append(
            GetCookieName(config.Slug),
            protectedValue,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Path = "/",
                Expires = expiresAt
            });
    }

    private bool TryReadGrant(string protectedValue, out AccessGrant grant)
    {
        try
        {
            grant = JsonSerializer.Deserialize<AccessGrant>(_protector.Unprotect(protectedValue))!;
            return grant is not null;
        }
        catch (Exception exception) when (
            exception is CryptographicException or JsonException or InvalidOperationException or FormatException)
        {
            grant = default!;
            return false;
        }
    }

    private static string GetCookieName(string slug) =>
        CookiePrefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(slug)))[..24].ToLowerInvariant();

    private static string BuildClaimValue(string slug, string passwordHash) =>
        $"{slug}|{Fingerprint(passwordHash)}";

    private static bool TryReadClaim(string claimValue, out string value, out long expiresAt)
    {
        value = string.Empty;
        expiresAt = 0;
        var separator = claimValue.LastIndexOf('|');
        if (separator <= 0
            || separator == claimValue.Length - 1
            || !long.TryParse(
                claimValue.AsSpan(separator + 1),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out expiresAt))
        {
            return false;
        }

        value = claimValue[..separator];
        return true;
    }

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record AccessGrant(string Slug, string PasswordFingerprint, DateTimeOffset ExpiresAtUtc);
}
