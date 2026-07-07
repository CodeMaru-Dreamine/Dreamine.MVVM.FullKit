using Codemaru.Models;
using System.Globalization;
using System.Text;

namespace Codemaru.Services;

public static class CardLandingPath
{
    public static string NormalizeForTenant(string slug, CardHybridUser user)
    {
        var segments = Split(slug);
        if (segments.Length == 0)
        {
            return $"card/{TenantSlug(user)}/me";
        }

        if (!string.Equals(segments[0], "card", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join("/", segments.Select(Slugify));
        }

        if (segments.Length == 1)
        {
            return $"card/{TenantSlug(user)}/me";
        }

        if (segments.Length == 2)
        {
            return $"card/{TenantSlug(user)}/{Slugify(segments[1])}";
        }

        return string.Join("/", segments.Select(Slugify));
    }

    public static string TenantSlug(CardHybridUser user)
    {
        var idSlug = Slugify(user.Id);
        return string.IsNullOrWhiteSpace(idSlug) ? "guest" : idSlug;
    }

    public static string[] Split(string slug) =>
        (slug ?? string.Empty)
        .Replace("\\", "/", StringComparison.Ordinal)
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(static segment => segment is not "." and not "..")
        .ToArray();

    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousDash = false;

        foreach (var rune in value.Trim().Normalize(NormalizationForm.FormD).EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (rune.Value is >= 'A' and <= 'Z')
            {
                builder.Append((char)(rune.Value + 32));
                previousDash = false;
            }
            else if (rune.Value is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append((char)rune.Value);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
