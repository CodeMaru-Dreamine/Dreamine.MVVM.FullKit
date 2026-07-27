using System.IO;
using System.Linq;
using System.Text;

namespace Dreamine.AppSecurity;

/// <summary>
/// Resolves application data paths from validated, single-segment identifiers.
/// </summary>
public static class StoragePathGuard
{
    public const int MaxIdentifierLength = 128;

    /// <summary>
    /// Resolves a directory named by an untrusted identifier beneath <paramref name="root"/>.
    /// </summary>
    public static string ResolveIdentifierDirectory(
        string root,
        string identifier,
        string parameterName,
        bool normalizeToLower = false)
    {
        var validated = ValidateIdentifier(identifier, parameterName);
        if (normalizeToLower)
        {
            validated = validated.ToLowerInvariant();
        }

        return ResolveUnderRoot(root, validated);
    }

    /// <summary>
    /// Resolves a file named by an untrusted identifier beneath <paramref name="root"/>.
    /// </summary>
    public static string ResolveIdentifierFile(
        string root,
        string identifier,
        string extension,
        string parameterName)
    {
        var validated = ValidateIdentifier(identifier, parameterName);
        if (string.IsNullOrEmpty(extension)
            || extension[0] != '.'
            || extension.Skip(1).Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("The extension must contain a leading dot followed by ASCII letters or digits.", nameof(extension));
        }

        return ResolveUnderRoot(root, $"{validated}{extension}");
    }

    /// <summary>
    /// Resolves known relative path segments and verifies that the result remains beneath
    /// <paramref name="root"/>. This method deliberately uses relative-path comparison
    /// instead of a string prefix check so sibling-prefix paths cannot pass.
    /// </summary>
    public static string ResolveUnderRoot(string root, params string[] relativeSegments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(relativeSegments);

        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var candidate = fullRoot;

        foreach (var segment in relativeSegments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                throw new ArgumentException("Path segments cannot be empty.", nameof(relativeSegments));
            }

            if (Path.IsPathRooted(segment))
            {
                throw new ArgumentException("Path segments must be relative.", nameof(relativeSegments));
            }

            candidate = Path.Combine(candidate, segment);
        }

        var fullCandidate = Path.GetFullPath(candidate);
        var relative = Path.GetRelativePath(fullRoot, fullCandidate);

        if (Path.IsPathFullyQualified(relative)
            || relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The resolved path escapes the configured data root.");
        }

        return fullCandidate;
    }

    private static string ValidateIdentifier(string identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("The identifier is required.", parameterName);
        }

        if (identifier.Length > MaxIdentifierLength)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"The identifier cannot exceed {MaxIdentifierLength} characters."),
                parameterName);
        }

        if (!identifier.IsNormalized(NormalizationForm.FormC))
        {
            throw new ArgumentException("The identifier must use Unicode normalization form C.", parameterName);
        }

        foreach (var character in identifier)
        {
            if (!char.IsLetterOrDigit(character) && character is not '-' and not '_')
            {
                throw new ArgumentException(
                    "The identifier may contain only letters, digits, hyphens, and underscores.",
                    parameterName);
            }
        }

        return identifier;
    }
}
