using System.IO;
using Wedding.Common;
using Wedding.Layouts.Contracts;

namespace WeddingPlatform.Services;

/// <summary>
/// Supplies the currently published layout-catalog snapshot. Implementations replace
/// the whole snapshot atomically so requests already in flight can finish on the old one.
/// </summary>
public interface IWeddingLayoutCatalogRegistry
{
    WeddingLayoutCatalog Current { get; }

    IReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage> PublishedPackages { get; }

    IReadOnlyDictionary<string, LayoutDefinitionPolicy> DefinitionPolicies { get; }

    string LayoutPackagesRoot { get; }

    event EventHandler<WeddingLayoutCatalogChangedEventArgs>? Changed;

    Task<WeddingLayoutReloadResult> ReloadAsync(CancellationToken cancellationToken = default);
}

public sealed class WeddingLayoutCatalogChangedEventArgs : EventArgs
{
    public WeddingLayoutCatalogChangedEventArgs(
        WeddingLayoutCatalog previous,
        WeddingLayoutCatalog current,
        DateTimeOffset changedAtUtc)
    {
        Previous = previous;
        Current = current;
        ChangedAtUtc = changedAtUtc;
    }

    public WeddingLayoutCatalog Previous { get; }

    public WeddingLayoutCatalog Current { get; }

    public DateTimeOffset ChangedAtUtc { get; }
}

public sealed record WeddingLayoutReloadResult(
    bool Succeeded,
    bool Changed,
    int PublishedReleaseCount,
    string? Error);

/// <summary>
/// Server-controlled immutable approval metadata stored beside an authored release.
/// Approval and activation are intentionally outside the portable package contract.
/// </summary>
public sealed record WeddingLayoutReleaseApproval
{
    public const int SupportedSchemaVersion = 1;

    public int SchemaVersion { get; init; }

    public string SubmissionId { get; init; } = "";

    public string ApprovedBy { get; init; } = "";

    public DateTimeOffset ApprovedAtUtc { get; init; }

    public string OwnerTenantSlug { get; init; } = "";

    public string PackageSha256 { get; init; } = "";
}

/// <summary>
/// Atomically replaced pointer selecting the active immutable release for one key.
/// </summary>
public sealed record WeddingLayoutActiveReleasePointer
{
    public const int SupportedSchemaVersion = 1;

    public int SchemaVersion { get; init; }

    public string Key { get; init; } = "";

    public string Version { get; init; } = "";

    public string ActivatedBy { get; init; } = "";

    public DateTimeOffset ActivatedAtUtc { get; init; }

    public string Reason { get; init; } = "";
}

/// <summary>
/// Names the service-owned, same-directory temporary files used to atomically
/// replace active pointers. A crashed writer may leave one behind; readers can
/// identify only this exact shape and safely ignore it.
/// </summary>
internal static class WeddingLayoutActivePointerFileNames
{
    private const string TemporaryPrefix = ".__wlp-active-";
    private const string TemporarySuffix = ".tmp";

    public static string CreateTemporaryPath(string pointerPath)
    {
        var directory = Path.GetDirectoryName(pointerPath)
            ?? throw new InvalidDataException("The active pointer directory is unavailable.");
        var fileName = Path.GetFileName(pointerPath);
        if (!string.Equals(
                Path.GetExtension(fileName),
                ".json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The active pointer file name is invalid.");
        }

        var key = Path.GetFileNameWithoutExtension(fileName);
        if (!WeddingLayoutKeys.IsValid(key))
        {
            throw new InvalidDataException("The active pointer layout key is invalid.");
        }

        // Keeping the temporary file beside its destination preserves the
        // same-volume atomic rename guarantee.
        return Path.Combine(
            directory,
            $"{TemporaryPrefix}{key}.{Guid.NewGuid():N}{TemporarySuffix}");
    }

    public static bool IsOwnedTemporaryFileName(string path)
    {
        var fileName = Path.GetFileName(path);
        if (IsCurrentOwnedTemporaryFileName(fileName))
        {
            return true;
        }

        // Compatibility for an orphan left by the first workflow implementation:
        // .{key}.json.{operation-id}.tmp
        if (!fileName.StartsWith(".", StringComparison.Ordinal)
            || !fileName.EndsWith(TemporarySuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var legacyPayload = fileName[1..^TemporarySuffix.Length];
        var legacySeparator = legacyPayload.LastIndexOf('.');
        if (legacySeparator <= 0 || legacySeparator == legacyPayload.Length - 1)
        {
            return false;
        }

        var destinationFileName = legacyPayload[..legacySeparator];
        var legacyOperationId = legacyPayload[(legacySeparator + 1)..];
        return string.Equals(
                Path.GetExtension(destinationFileName),
                ".json",
                StringComparison.OrdinalIgnoreCase)
            && WeddingLayoutKeys.IsValid(
                Path.GetFileNameWithoutExtension(destinationFileName))
            && Guid.TryParseExact(legacyOperationId, "N", out _);
    }

    private static bool IsCurrentOwnedTemporaryFileName(string fileName)
    {
        if (!fileName.StartsWith(TemporaryPrefix, StringComparison.Ordinal)
            || !fileName.EndsWith(TemporarySuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = fileName[
            TemporaryPrefix.Length..
            ^TemporarySuffix.Length];
        var separator = payload.LastIndexOf('.');
        if (separator <= 0 || separator == payload.Length - 1)
        {
            return false;
        }

        var key = payload[..separator];
        var operationId = payload[(separator + 1)..];
        return WeddingLayoutKeys.IsValid(key)
            && Guid.TryParseExact(operationId, "N", out _);
    }
}

/// <summary>
/// Names service-owned temporary files used for atomic policy replacement.
/// Registry readers ignore only this exact shape.
/// </summary>
internal static class WeddingLayoutDefinitionPolicyFileNames
{
    private const string TemporaryPrefix = ".__wlp-policy-";
    private const string TemporarySuffix = ".tmp";

    public static string CreateTemporaryPath(string policyPath)
    {
        var directory = Path.GetDirectoryName(policyPath)
            ?? throw new InvalidDataException("The definition policy directory is unavailable.");
        var key = Path.GetFileNameWithoutExtension(policyPath);
        if (!string.Equals(Path.GetExtension(policyPath), ".json", StringComparison.OrdinalIgnoreCase)
            || !WeddingLayoutKeys.IsValid(key))
        {
            throw new InvalidDataException("The definition policy file name is invalid.");
        }

        return Path.Combine(
            directory,
            $"{TemporaryPrefix}{key}.{Guid.NewGuid():N}{TemporarySuffix}");
    }

    public static bool IsOwnedTemporaryFileName(string path)
    {
        var fileName = Path.GetFileName(path);
        if (!fileName.StartsWith(TemporaryPrefix, StringComparison.Ordinal)
            || !fileName.EndsWith(TemporarySuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = fileName[TemporaryPrefix.Length..^TemporarySuffix.Length];
        var separator = payload.LastIndexOf('.');
        return separator > 0
            && separator < payload.Length - 1
            && WeddingLayoutKeys.IsValid(payload[..separator])
            && Guid.TryParseExact(payload[(separator + 1)..], "N", out _);
    }
}

/// <summary>
/// A validated, immutable package available to the runtime renderer.
/// </summary>
public sealed record WeddingLayoutPublishedPackage(
    LayoutPackage Package,
    WeddingLayoutReleaseApproval Approval)
{
    public LayoutManifest Manifest => Package.Manifest;

    public LayoutDefinition Definition => Package.Definition;

    public WeddingLayoutReleaseId ReleaseId => new(Manifest.Key, Manifest.Version);
}
