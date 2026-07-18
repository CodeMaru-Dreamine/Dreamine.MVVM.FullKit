using Codemaru.Models;
using System.Globalization;
using System.Text;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>Card Landing Path 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates card landing path functionality and related state.</para>
/// \endif
/// </summary>
public static class CardLandingPath
{
    /// <summary>
    /// \if KO
    /// <para>Normalize For Tenant 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize for tenant operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>CardHybridUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize For Tenant 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize for tenant operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Tenant Slug 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the tenant slug operation.</para>
    /// \endif
    /// </summary>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>CardHybridUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Tenant Slug 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the tenant slug operation.</para>
    /// \endif
    /// </returns>
    public static string TenantSlug(CardHybridUser user)
    {
        var idSlug = Slugify(user.Id);
        return string.IsNullOrWhiteSpace(idSlug) ? "guest" : idSlug;
    }

    /// <summary>
    /// \if KO
    /// <para>Split 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the split operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Split 작업에서 생성한 <c>string[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> result produced by the split operation.</para>
    /// \endif
    /// </returns>
    public static string[] Split(string slug) =>
        (slug ?? string.Empty)
        .Replace("\\", "/", StringComparison.Ordinal)
        .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(static segment => segment is not "." and not "..")
        .ToArray();

    /// <summary>
    /// \if KO
    /// <para>Slugify 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the slugify operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Slugify 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the slugify operation.</para>
    /// \endif
    /// </returns>
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
