using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wedding.Common;
using Wedding.Layouts.Contracts;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// Stores private submissions and publishes approved immutable releases. Only the
/// small declarative JSON contract is accepted; this service has no asset/markup upload path.
/// </summary>
public sealed class FileSystemWeddingLayoutSubmissionService :
    IWeddingLayoutSubmissionService,
    IDisposable
{
    private const string SubmissionMetadataFileName = "submission.json";
    private const string PackageFileName = "package.json";
    private const string ManifestFileName = "manifest.json";
    private const string DefinitionFileName = "layout.json";
    private const string ApprovalFileName = "approval.json";
    private const string ArchiveMetadataFileName = "archive.json";
    private const string ArchivedSubmissionDirectoryName = "submission";
    private const string ArchivedReleaseDirectoryName = "release";
    private const string ArchivedActivePointerFileName = "active-pointer.json";
    private const int MaximumUploadBytes = 384 * 1024;
    private const int MaximumSubmissions = 1_000;
    private const int MaximumPendingPerTenant = 20;
    private const int MaximumArchivedLayoutKeys = 500;
    private const int MaximumArchivedItems = 10_000;
    private const int MaximumArchivedReleasesPerKey = 1_000;
    private const int MaximumPurgedHistoryItems = 100_000;
    private const string SuperAdminSubmitterUserId = "super-admin";

    private static readonly JsonSerializerOptions WorkflowJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    private static readonly JsonSerializerOptions PackageJsonOptions =
        LayoutPackageJson.CreateOptions(indented: true);

    private readonly ITenantStore _tenants;
    private readonly ISuperAdminSessionTokenService _superAdminTokens;
    private readonly ISuperAdminAuditLog _audit;
    private readonly IWeddingLayoutCatalogRegistry _registry;
    private readonly string _layoutPackagesRoot;
    private readonly string _submissionsRoot;
    private readonly string _releasesRoot;
    private readonly string _activeRoot;
    private readonly string _policiesRoot;
    private readonly string _stagingRoot;
    private readonly string _archiveRoot;
    private readonly string _archivedSubmissionsRoot;
    private readonly string _archivedReleasesRoot;
    private readonly string _purgedArchivesRoot;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public event EventHandler<WeddingLayoutSubmissionsChangedEventArgs>? Changed;

    public FileSystemWeddingLayoutSubmissionService(
        WeddingOptions options,
        ITenantStore tenants,
        ISuperAdminSessionTokenService superAdminTokens,
        ISuperAdminAuditLog audit,
        IWeddingLayoutCatalogRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(options);
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _superAdminTokens = superAdminTokens
            ?? throw new ArgumentNullException(nameof(superAdminTokens));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

        var publicWeddingRoot = Path.GetFullPath(options.ResolvedDataPath);
        var appDataRoot = Directory.GetParent(
                Path.TrimEndingDirectorySeparator(publicWeddingRoot))
            ?.FullName
            ?? throw new InvalidOperationException(
                $"Cannot resolve the App_Data parent of '{publicWeddingRoot}'.");

        _layoutPackagesRoot = Path.GetFullPath(
            Path.Combine(appDataRoot, "LayoutPackages"));
        _submissionsRoot = Path.Combine(_layoutPackagesRoot, "Submissions");
        _releasesRoot = Path.Combine(_layoutPackagesRoot, "Releases");
        _activeRoot = Path.Combine(_layoutPackagesRoot, "Active");
        _policiesRoot = Path.Combine(_layoutPackagesRoot, "Policies");
        _stagingRoot = Path.Combine(_layoutPackagesRoot, "Staging");
        _archiveRoot = Path.Combine(_layoutPackagesRoot, "Archive");
        _archivedSubmissionsRoot = Path.Combine(_archiveRoot, "Submissions");
        _archivedReleasesRoot = Path.Combine(_archiveRoot, "Releases");
        _purgedArchivesRoot = Path.Combine(_archiveRoot, "Purged");

        if (IsSameOrChildPath(_layoutPackagesRoot, publicWeddingRoot))
        {
            throw new InvalidOperationException(
                "Layout package workflow data must not be stored below the public Wedding data directory.");
        }
    }

    public async Task<WeddingLayoutSubmissionRecord> SubmitAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        Stream packageJson,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(packageJson);

        var tenant = await RequireTenantAdministratorAsync(
                tenantSlug,
                actor,
                cancellationToken)
            .ConfigureAwait(false);
        return await SubmitCoreAsync(
                tenant,
                actor.Id,
                NormalizeActorLabel(actor.DisplayName, actor.Email, actor.Id),
                packageJson,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WeddingLayoutSubmissionRecord> SubmitAsSuperAdminAsync(
        string tenantSlug,
        string superAdminSessionToken,
        string submittedBy,
        Stream packageJson,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(packageJson);
        RequireSuperAdmin(superAdminSessionToken);
        submittedBy = ValidateReviewText(
            submittedBy,
            nameof(submittedBy),
            1,
            120);

        var tenant = await RequireTenantAsync(tenantSlug, cancellationToken)
            .ConfigureAwait(false);
        var record = await SubmitCoreAsync(
                tenant,
                SuperAdminSubmitterUserId,
                submittedBy,
                packageJson,
                cancellationToken)
            .ConfigureAwait(false);
        await _audit.WriteAsync(
                "SubmitLayoutAsSuperAdmin",
                tenant.Slug,
                $"SubmissionId={record.SubmissionId}; Layout={record.LayoutKey}@{record.LayoutVersion}; SubmittedBy={submittedBy}",
                CancellationToken.None)
            .ConfigureAwait(false);
        return record;
    }

    private async Task<WeddingLayoutSubmissionRecord> SubmitCoreAsync(
        TenantConfig tenant,
        string submittedByUserId,
        string submittedByDisplayName,
        Stream packageJson,
        CancellationToken cancellationToken)
    {
        var package = await ReadAndValidatePackageAsync(packageJson, cancellationToken)
            .ConfigureAwait(false);

        if (WeddingLayoutCatalog.Instance.FindDescriptor(package.Manifest.Key) is not null)
        {
            throw new InvalidDataException(
                $"Layout key '{package.Manifest.Key}' collides with a built-in layout.");
        }

        var packageBytes = JsonSerializer.SerializeToUtf8Bytes(
            package,
            PackageJsonOptions);
        var packageHash = Convert.ToHexString(SHA256.HashData(packageBytes));
        var submissionId = Guid.NewGuid().ToString("N");
        var record = new WeddingLayoutSubmissionRecord
        {
            SubmissionId = submissionId,
            TenantSlug = tenant.Slug,
            SubmittedByUserId = submittedByUserId,
            SubmittedByDisplayName = submittedByDisplayName,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Status = WeddingLayoutSubmissionStatus.Pending,
            LayoutKey = package.Manifest.Key,
            LayoutVersion = package.Manifest.Version,
            LayoutLabel = package.Manifest.Label,
            LegacyManifestTierSnapshot = package.Manifest.Tier,
            PackageSha256 = packageHash,
        };

        var committed = false;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var submissions = await ReadAllSubmissionRecordsNoLockAsync(cancellationToken)
                .ConfigureAwait(false);
            if (submissions.Count >= MaximumSubmissions)
            {
                throw new InvalidOperationException(
                    $"Layout submission storage reached its {MaximumSubmissions}-item limit.");
            }

            if (submissions.Count(x =>
                    x.Status == WeddingLayoutSubmissionStatus.Pending
                    && string.Equals(
                        x.TenantSlug,
                        tenant.Slug,
                        StringComparison.OrdinalIgnoreCase))
                >= MaximumPendingPerTenant)
            {
                throw new InvalidOperationException(
                    $"A tenant may have at most {MaximumPendingPerTenant} pending layout submissions.");
            }

            await EnsureLayoutIdentityAvailableNoLockAsync(
                    record,
                    submissions,
                    cancellationToken)
                .ConfigureAwait(false);

            var stagingDirectory = CreateStagingDirectory($"submit-{submissionId}");
            try
            {
                await File.WriteAllBytesAsync(
                        Path.Combine(stagingDirectory, PackageFileName),
                        packageBytes,
                        cancellationToken)
                    .ConfigureAwait(false);
                await WriteJsonFileAsync(
                        Path.Combine(stagingDirectory, SubmissionMetadataFileName),
                        record,
                        WorkflowJsonOptions,
                        cancellationToken)
                    .ConfigureAwait(false);

                Directory.Move(
                    stagingDirectory,
                    SubmissionDirectory(submissionId));
                committed = true;
            }
            finally
            {
                TryDeleteEmptyDirectory(stagingDirectory);
            }
        }
        finally
        {
            _gate.Release();
            if (committed)
            {
                NotifyChanged(record, WeddingLayoutSubmissionChangeKind.Submitted);
            }
        }

        return record;
    }

    public async Task<IReadOnlyList<WeddingLayoutSubmissionRecord>> ListOwnAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var tenant = await RequireTenantAdministratorAsync(
                tenantSlug,
                actor,
                cancellationToken)
            .ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            return (await ReadAllSubmissionRecordsNoLockAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Where(x => string.Equals(
                    x.TenantSlug,
                    tenant.Slug,
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<WeddingLayoutSubmissionRecord>> ListAllAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            return (await ReadAllSubmissionRecordsNoLockAsync(cancellationToken)
                    .ConfigureAwait(false))
                .OrderBy(x => x.Status)
                .ThenByDescending(x => x.SubmittedAtUtc)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<LayoutDefinitionPolicy>> ListDefinitionPoliciesAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();

            // Older builds retained a custom LayoutKey policy even after its last
            // recoverable/live artifact was permanently purged. Prune only keys
            // for which a permanent-delete receipt exists; a deliberately
            // pre-classified key with no submissions must remain available.
            var customPolicyKeys = _registry.DefinitionPolicies.Keys.ToArray();
            if (customPolicyKeys.Length > 0)
            {
                var stagingDirectory = CreateStagingDirectory(
                    $"policy-prune-{Guid.NewGuid():N}");
                try
                {
                    await PruneOrphanedDefinitionPoliciesNoLockAsync(
                            customPolicyKeys,
                            stagingDirectory,
                            requirePurgedReceipt: true,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // A non-empty directory is intentionally retained if a
                    // failed restore needs operator attention.
                    TryDeleteEmptyDirectory(stagingDirectory);
                }
            }

            var builtInPolicies = WeddingLayoutCatalog.Instance.Descriptors
                .Where(x => x.IsBuiltIn)
                .Select(x => new LayoutDefinitionPolicy
                {
                    SchemaVersion = LayoutDefinitionPolicy.SupportedSchemaVersion,
                    LayoutKey = x.Key,
                    Tier = x.Tier == WeddingLayoutTier.Premium
                        ? LayoutTier.Premium
                        : LayoutTier.Free,
                    ClassifiedBy = "built-in-catalog",
                    ClassifiedAtUtc = DateTimeOffset.UnixEpoch,
                    Reason = "Protected application-owned layout classification.",
                    Revision = 0,
                });
            return builtInPolicies
                .Concat(_registry.DefinitionPolicies.Values)
                .OrderBy(x => x.LayoutKey, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<WeddingLayoutDefinitionPolicyChangeResult>
        SetDefinitionTierAsync(
            string layoutKey,
            LayoutTier tier,
            string superAdminSessionToken,
            string changedBy,
            string reason,
            CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);
        var key = layoutKey?.Trim() ?? "";
        if (!WeddingLayoutKeys.IsValid(key)
            || !string.Equals(key, layoutKey, StringComparison.Ordinal)
            || !Enum.IsDefined(tier))
        {
            throw new InvalidDataException(
                "The layout key and tier must be canonical values.");
        }

        if (WeddingLayoutCatalog.Instance.FindDescriptor(key) is not null)
        {
            throw new InvalidOperationException(
                $"Built-in layout '{key}' cannot be reclassified.");
        }

        changedBy = ValidateReviewText(changedBy, nameof(changedBy), 1, 120);
        reason = ValidateReviewText(reason, nameof(reason), 1, 300);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var path = DefinitionPolicyPath(key);
            var previous = File.Exists(path)
                ? await ReadDefinitionPolicyNoLockAsync(key, cancellationToken)
                    .ConfigureAwait(false)
                : null;
            var isUnconfirmedLegacySeed = string.Equals(
                previous?.ClassifiedBy,
                "legacy-migration",
                StringComparison.Ordinal);
            var reclassified = previous is not null
                && !isUnconfirmedLegacySeed
                && previous.Tier != tier;
            var policy = previous is not null
                && !isUnconfirmedLegacySeed
                && previous.Tier == tier
                ? previous
                : new LayoutDefinitionPolicy
                {
                    SchemaVersion = LayoutDefinitionPolicy.SupportedSchemaVersion,
                    LayoutKey = key,
                    Tier = tier,
                    ClassifiedBy = changedBy,
                    ClassifiedAtUtc = DateTimeOffset.UtcNow,
                    Reason = reason,
                    Revision = previous is null ? 1 : checked(previous.Revision + 1),
                };

            if (!ReferenceEquals(policy, previous))
            {
                await AtomicReplaceJsonAsync(
                        path,
                        policy,
                        WorkflowJsonOptions,
                        cancellationToken,
                        WeddingLayoutDefinitionPolicyFileNames.CreateTemporaryPath)
                    .ConfigureAwait(false);
            }

            WeddingLayoutReloadResult reload;
            Exception? reloadException = null;
            try
            {
                reload = await _registry.ReloadAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                reloadException = ex;
                reload = new WeddingLayoutReloadResult(
                    false,
                    false,
                    _registry.PublishedPackages.Count,
                    ex.Message);
            }

            if (!reload.Succeeded && !ReferenceEquals(policy, previous))
            {
                Exception? restoreException = null;
                try
                {
                    if (previous is null)
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        await AtomicReplaceJsonAsync(
                                path,
                                previous,
                                WorkflowJsonOptions,
                                CancellationToken.None,
                                WeddingLayoutDefinitionPolicyFileNames.CreateTemporaryPath)
                            .ConfigureAwait(false);
                    }

                    var restored = await _registry.ReloadAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    if (!restored.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"The previous definition policy was restored on disk, but its catalog reload failed: {restored.Error}");
                    }
                }
                catch (Exception ex)
                {
                    restoreException = ex;
                }

                var message =
                    $"The definition tier was not changed because catalog reload failed: {reload.Error}";
                if (restoreException is not null)
                {
                    throw new InvalidOperationException(
                        $"{message} Restoring the previous policy also failed: {restoreException.Message}",
                        reloadException is null
                            ? restoreException
                            : new AggregateException(reloadException, restoreException));
                }

                throw new InvalidOperationException(message, reloadException);
            }

            if (!reload.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The definition policy could not be loaded: {reload.Error}",
                    reloadException);
            }

            await _audit.WriteAsync(
                    reclassified
                        ? "ReclassifyLayoutDefinition"
                        : "ClassifyLayoutDefinition",
                    key,
                    $"Tier={policy.Tier}; Revision={policy.Revision}; ChangedBy={changedBy}; Reason={reason}",
                    CancellationToken.None)
                .ConfigureAwait(false);
            return new WeddingLayoutDefinitionPolicyChangeResult(
                policy,
                reload,
                reclassified);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LayoutPackage> GetOwnPackageAsync(
        string submissionId,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(actor);
        if (!actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var record = await ReadSubmissionRecordNoLockAsync(
                    submissionId,
                    cancellationToken)
                .ConfigureAwait(false);
            await RequireTenantAdministratorAsync(
                    record.TenantSlug,
                    actor,
                    cancellationToken)
                .ConfigureAwait(false);
            return await ReadStoredPackageNoLockAsync(record, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LayoutPackage> GetPackageForReviewAsync(
        string submissionId,
        string superAdminSessionToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var record = await ReadSubmissionRecordNoLockAsync(
                    submissionId,
                    cancellationToken)
                .ConfigureAwait(false);
            return await ReadStoredPackageNoLockAsync(record, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<WeddingLayoutApprovalResult> ApproveAsync(
        string submissionId,
        string superAdminSessionToken,
        string approvedBy,
        bool activate = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);
        approvedBy = ValidateReviewText(approvedBy, nameof(approvedBy), 1, 120);

        WeddingLayoutSubmissionRecord? committedApproval = null;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var record = await ReadSubmissionRecordNoLockAsync(
                    submissionId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (record.Status == WeddingLayoutSubmissionStatus.Rejected)
            {
                throw new InvalidOperationException("A rejected submission cannot be approved.");
            }

            _ = await ReadDefinitionPolicyNoLockAsync(
                    record.LayoutKey,
                    cancellationToken)
                .ConfigureAwait(false);
            var package = await ReadStoredPackageNoLockAsync(record, cancellationToken)
                .ConfigureAwait(false);
            await PublishReleaseNoLockAsync(
                    record,
                    package,
                    approvedBy,
                    cancellationToken)
                .ConfigureAwait(false);

            WeddingLayoutReloadResult reload;
            if (activate)
            {
                var activation = await ActivateNoLockAsync(
                        package.Manifest.Key,
                        package.Manifest.Version,
                        approvedBy,
                        "Approved release",
                        cancellationToken)
                    .ConfigureAwait(false);
                reload = activation.Reload;
            }
            else
            {
                reload = await _registry.ReloadAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (!reload.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"The immutable release was published, but catalog reload failed: {reload.Error}");
                }
            }

            var approved = record with
            {
                Status = WeddingLayoutSubmissionStatus.Approved,
                ReviewedBy = approvedBy,
                ReviewedAtUtc = DateTimeOffset.UtcNow,
                ReviewReason = activate
                    ? "Approved and activated"
                    : "Approved",
            };
            var completionToken = activate
                ? CancellationToken.None
                : cancellationToken;
            await ReplaceSubmissionRecordNoLockAsync(approved, completionToken)
                .ConfigureAwait(false);
            committedApproval = approved;
            await _audit.WriteAsync(
                    "ApproveLayoutSubmission",
                    approved.TenantSlug,
                    $"SubmissionId={approved.SubmissionId}; Layout={approved.LayoutKey}@{approved.LayoutVersion}; Activated={activate}",
                    completionToken)
                .ConfigureAwait(false);

            return new WeddingLayoutApprovalResult(approved, reload, activate);
        }
        finally
        {
            _gate.Release();
            if (committedApproval is not null)
            {
                NotifyChanged(
                    committedApproval,
                    WeddingLayoutSubmissionChangeKind.Approved);
            }
        }
    }

    public async Task<WeddingLayoutSubmissionRecord> RejectAsync(
        string submissionId,
        string superAdminSessionToken,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);
        rejectedBy = ValidateReviewText(rejectedBy, nameof(rejectedBy), 1, 120);
        reason = ValidateReviewText(reason, nameof(reason), 1, 500);

        WeddingLayoutSubmissionRecord? committedRejection = null;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var record = await ReadSubmissionRecordNoLockAsync(
                    submissionId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (record.Status == WeddingLayoutSubmissionStatus.Approved)
            {
                throw new InvalidOperationException(
                    "An approved immutable release cannot be changed to rejected.");
            }

            var rejected = record with
            {
                Status = WeddingLayoutSubmissionStatus.Rejected,
                ReviewedBy = rejectedBy,
                ReviewedAtUtc = DateTimeOffset.UtcNow,
                ReviewReason = reason,
            };
            await ReplaceSubmissionRecordNoLockAsync(rejected, cancellationToken)
                .ConfigureAwait(false);
            committedRejection = rejected;
            await _audit.WriteAsync(
                    "RejectLayoutSubmission",
                    rejected.TenantSlug,
                    $"SubmissionId={rejected.SubmissionId}; Layout={rejected.LayoutKey}@{rejected.LayoutVersion}; Reason={reason}",
                    cancellationToken)
                .ConfigureAwait(false);
            return rejected;
        }
        finally
        {
            _gate.Release();
            if (committedRejection is not null)
            {
                NotifyChanged(
                    committedRejection,
                    WeddingLayoutSubmissionChangeKind.Rejected);
            }
        }
    }

    public async Task<WeddingLayoutArchiveResult> ArchiveAsync(
        string submissionId,
        string superAdminSessionToken,
        string archivedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);
        archivedBy = ValidateReviewText(archivedBy, nameof(archivedBy), 1, 120);
        reason = ValidateReviewText(reason, nameof(reason), 1, 300);

        WeddingLayoutSubmissionRecord? committedArchive = null;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var record = await ReadSubmissionRecordNoLockAsync(
                    submissionId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (WeddingLayoutCatalog.Instance.FindDescriptor(record.LayoutKey) is not null)
            {
                throw new InvalidOperationException(
                    $"Built-in layout '{record.LayoutKey}' cannot be archived.");
            }

            var archivesPublishedRelease =
                record.Status == WeddingLayoutSubmissionStatus.Approved;
            string? releaseDirectory = null;
            string? activePointerPath = null;
            var archivesActivePointer = false;
            var blocksAllKeyReferences = false;

            if (archivesPublishedRelease)
            {
                var published = await ReadPublishedPackageNoLockAsync(
                        record.LayoutKey,
                        record.LayoutVersion,
                        cancellationToken)
                    .ConfigureAwait(false);
                ValidatePublishedArchiveLineage(record, published);

                releaseDirectory = ReleaseDirectory(
                    record.LayoutKey,
                    record.LayoutVersion);
                var releaseVersions = ReadPublishedReleaseVersionsNoLock(record.LayoutKey);
                if (!releaseVersions.Contains(record.LayoutVersion, StringComparer.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Published release '{record.LayoutKey}@{record.LayoutVersion}' was not found in its key directory.");
                }

                var isOnlyRelease = releaseVersions.Count == 1;
                activePointerPath = ActivePointerPath(record.LayoutKey);
                WeddingLayoutActiveReleasePointer? activePointer = null;
                if (File.Exists(activePointerPath))
                {
                    activePointer = await ReadJsonFileAsync<WeddingLayoutActiveReleasePointer>(
                            activePointerPath,
                            WorkflowJsonOptions,
                            32 * 1024,
                            cancellationToken)
                        .ConfigureAwait(false);
                    ValidateStoredActivePointer(
                        activePointer,
                        activePointerPath,
                        record.LayoutKey);
                    if (!releaseVersions.Contains(
                            activePointer.Version,
                            StringComparer.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"Active pointer '{activePointer.Key}@{activePointer.Version}' references a missing release.");
                    }
                }

                var isActiveRelease = activePointer is not null
                    && string.Equals(
                        activePointer.Version,
                        record.LayoutVersion,
                        StringComparison.Ordinal);
                if (isActiveRelease && !isOnlyRelease)
                {
                    throw new InvalidOperationException(
                        $"Active release '{record.LayoutKey}@{record.LayoutVersion}' cannot be archived while another version exists. Activate or roll back to another version first.");
                }

                archivesActivePointer = isActiveRelease;
                blocksAllKeyReferences = isActiveRelease || isOnlyRelease;
                var tenantReferences = await ReadTenantLayoutReferencesNoLockAsync(
                        record.LayoutKey,
                        record.LayoutVersion,
                        cancellationToken)
                    .ConfigureAwait(false);
                ThrowIfRemovalHasTenantReferences(
                    record.LayoutKey,
                    record.LayoutVersion,
                    tenantReferences,
                    blocksAllKeyReferences);
            }

            var archiveDirectory = archivesPublishedRelease
                ? ArchivedReleaseDirectory(record.LayoutKey, record.LayoutVersion)
                : ArchivedSubmissionDirectory(record.SubmissionId);
            if (Directory.Exists(archiveDirectory) || File.Exists(archiveDirectory))
            {
                throw new InvalidOperationException(
                    $"Layout submission '{record.SubmissionId}' already has an archive destination.");
            }

            var stagingDirectory = CreateStagingDirectory(
                $"archive-{record.SubmissionId}-{Guid.NewGuid():N}");
            var archivedSubmissionPath = Path.Combine(
                stagingDirectory,
                ArchivedSubmissionDirectoryName);
            var archivedReleasePath = Path.Combine(
                stagingDirectory,
                ArchivedReleaseDirectoryName);
            var archivedPointerPath = Path.Combine(
                stagingDirectory,
                ArchivedActivePointerFileName);
            var tombstonePath = Path.Combine(
                stagingDirectory,
                ArchiveMetadataFileName);
            var tombstone = new WeddingLayoutArchiveRecord
            {
                SchemaVersion = WeddingLayoutArchiveRecord.SupportedSchemaVersion,
                SubmissionId = record.SubmissionId,
                OriginalStatus = record.Status,
                TenantSlug = record.TenantSlug,
                LayoutKey = record.LayoutKey,
                LayoutVersion = record.LayoutVersion,
                PackageSha256 = record.PackageSha256,
                PublishedReleaseArchived = archivesPublishedRelease,
                ActivePointerArchived = archivesActivePointer,
                ArchivedBy = archivedBy,
                ArchivedAtUtc = DateTimeOffset.UtcNow,
                Reason = reason,
            };

            var submissionDirectory = SubmissionDirectory(record.SubmissionId);
            var submissionMoved = false;
            var releaseMoved = false;
            var pointerMoved = false;
            var archiveCommitted = false;
            WeddingLayoutReloadResult? reload = null;
            try
            {
                await WriteJsonFileAsync(
                        tombstonePath,
                        tombstone,
                        WorkflowJsonOptions,
                        cancellationToken)
                    .ConfigureAwait(false);

                Directory.Move(submissionDirectory, archivedSubmissionPath);
                submissionMoved = true;

                // Moving a sole active pointer first keeps every watcher-observed
                // intermediate state valid: an unreferenced release may remain
                // published briefly, but a pointer never targets a missing release.
                if (archivesActivePointer)
                {
                    File.Move(activePointerPath!, archivedPointerPath);
                    pointerMoved = true;
                }

                if (archivesPublishedRelease)
                {
                    Directory.Move(releaseDirectory!, archivedReleasePath);
                    releaseMoved = true;

                    // The first source move is the commit boundary. Cancellation
                    // must not strand a partially archived release.
                    reload = await _registry.ReloadAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    if (!reload.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"The release was not archived because catalog reload failed: {reload.Error}");
                    }

                    // Close the pre-check/reload race as far as the current
                    // file-backed architecture permits. A reference appearing
                    // during the move causes a full disk and catalog rollback.
                    var postMoveReferences =
                        await ReadTenantLayoutReferencesNoLockAsync(
                                record.LayoutKey,
                                record.LayoutVersion,
                                CancellationToken.None)
                            .ConfigureAwait(false);
                    ThrowIfRemovalHasTenantReferences(
                        record.LayoutKey,
                        record.LayoutVersion,
                        postMoveReferences,
                        blocksAllKeyReferences);
                }

                EnsureDirectory(
                    Path.GetDirectoryName(archiveDirectory)
                    ?? throw new InvalidDataException(
                        "The archive destination directory is unavailable."));
                Directory.Move(stagingDirectory, archiveDirectory);
                archiveCommitted = true;
                committedArchive = record;
            }
            catch (Exception archiveException)
            {
                if (!archiveCommitted)
                {
                    try
                    {
                        await RestoreArchiveStagingNoLockAsync(
                                stagingDirectory,
                                submissionDirectory,
                                releaseDirectory,
                                activePointerPath,
                                submissionMoved,
                                releaseMoved,
                                pointerMoved,
                                archivesPublishedRelease)
                            .ConfigureAwait(false);
                    }
                    catch (Exception recoveryException)
                    {
                        throw new InvalidOperationException(
                            $"Archiving layout '{record.LayoutKey}@{record.LayoutVersion}' failed, and restoring its files also failed.",
                            new AggregateException(
                                archiveException,
                                recoveryException));
                    }
                }

                throw;
            }
            finally
            {
                TryDeleteEmptyDirectory(stagingDirectory);
            }

            await _audit.WriteAsync(
                    archivesPublishedRelease
                        ? "ArchiveLayoutRelease"
                        : "ArchiveLayoutSubmission",
                    record.TenantSlug,
                    $"SubmissionId={record.SubmissionId}; Layout={record.LayoutKey}@{record.LayoutVersion}; Status={record.Status}; ActivePointerArchived={archivesActivePointer}; Reason={reason}",
                    CancellationToken.None)
                .ConfigureAwait(false);

            return new WeddingLayoutArchiveResult(
                record,
                archivesPublishedRelease,
                archivesActivePointer,
                reload);
        }
        finally
        {
            _gate.Release();
            if (committedArchive is not null)
            {
                NotifyChanged(
                    committedArchive,
                    WeddingLayoutSubmissionChangeKind.Archived);
            }
        }
    }

    public async Task<IReadOnlyList<WeddingLayoutArchiveRecord>> ListArchivedAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            return (await ReadAllArchiveLocationsNoLockAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(location => location.Record)
                .Where(record => !record.PayloadPurged)
                .OrderByDescending(record => record.ArchivedAtUtc)
                .ThenBy(record => record.LayoutKey, StringComparer.Ordinal)
                .ThenByDescending(record => record.LayoutVersion, StringComparer.Ordinal)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<WeddingLayoutPurgeResult> PurgeArchivedAsync(
        string submissionId,
        string superAdminSessionToken,
        string purgedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);
        if (!IsSubmissionId(submissionId))
        {
            throw new InvalidDataException("The archived submission id is invalid.");
        }

        purgedBy = ValidateReviewText(purgedBy, nameof(purgedBy), 1, 120);
        reason = ValidateReviewText(reason, nameof(reason), 1, 300);

        WeddingLayoutArchiveRecord? committedPurge = null;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var matches = (await ReadAllArchiveLocationsNoLockAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Where(location => string.Equals(
                    location.Record.SubmissionId,
                    submissionId,
                    StringComparison.Ordinal))
                .Take(2)
                .ToArray();
            if (matches.Length == 0)
            {
                throw new KeyNotFoundException(
                    $"Archived layout submission '{submissionId}' was not found. Move it to the archive before permanently deleting it.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidDataException(
                    $"Archived layout submission '{submissionId}' has duplicate retention records.");
            }

            var location = matches[0];
            var archived = location.Record;
            var purgeStagingDirectory =
                PermanentPurgeStagingDirectory(archived.SubmissionId);
            if (WeddingLayoutCatalog.Instance.FindDescriptor(archived.LayoutKey)
                is not null)
            {
                throw new InvalidOperationException(
                    $"Built-in layout '{archived.LayoutKey}' cannot be permanently deleted.");
            }

            if (archived.PayloadPurged)
            {
                MovePurgedArchiveToHistoryNoLock(location);
                await PruneOrphanedDefinitionPoliciesNoLockAsync(
                        [archived.LayoutKey],
                        purgeStagingDirectory,
                        requirePurgedReceipt: true,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                DeleteCompletedPurgeStagingNoLock(purgeStagingDirectory);
                var policyPreserved = IsDefinitionPolicyPreservedNoLock(
                    archived.LayoutKey);
                committedPurge = archived;
                return new WeddingLayoutPurgeResult(
                    archived,
                    AlreadyPurged: true,
                    DefinitionPolicyPreserved: policyPreserved);
            }

            var blockAllKeyReferences = false;
            if (archived.PublishedReleaseArchived)
            {
                blockAllKeyReferences =
                    await EnsureArchivedReleaseCanBePurgedNoLockAsync(
                            archived,
                            cancellationToken)
                        .ConfigureAwait(false);
                var tenantReferences =
                    await ReadTenantLayoutReferencesNoLockAsync(
                            archived.LayoutKey,
                            archived.LayoutVersion,
                            cancellationToken)
                        .ConfigureAwait(false);
                ThrowIfRemovalHasTenantReferences(
                    archived.LayoutKey,
                    archived.LayoutVersion,
                    tenantReferences,
                    blockAllKeyReferences);
            }

            ValidateRecoverableArchivePayloadShape(location);
            var purgedArchiveDirectory = PurgedArchiveDirectory(
                archived.SubmissionId);
            if (Directory.Exists(purgedArchiveDirectory)
                || File.Exists(purgedArchiveDirectory))
            {
                throw new InvalidDataException(
                    $"Purged archive history for submission '{submissionId}' already exists.");
            }

            if (Directory.Exists(purgeStagingDirectory)
                || File.Exists(purgeStagingDirectory))
            {
                throw new InvalidOperationException(
                    $"Permanent-delete staging for submission '{submissionId}' already exists.");
            }

            Directory.CreateDirectory(purgeStagingDirectory);
            RejectReparsePoint(purgeStagingDirectory);
            var stagedSubmissionPath = Path.Combine(
                purgeStagingDirectory,
                ArchivedSubmissionDirectoryName);
            var stagedReleasePath = Path.Combine(
                purgeStagingDirectory,
                ArchivedReleaseDirectoryName);
            var stagedPointerPath = Path.Combine(
                purgeStagingDirectory,
                ArchivedActivePointerFileName);
            var archivedSubmissionPath = Path.Combine(
                location.DirectoryPath,
                ArchivedSubmissionDirectoryName);
            var archivedReleasePath = Path.Combine(
                location.DirectoryPath,
                ArchivedReleaseDirectoryName);
            var archivedPointerPath = Path.Combine(
                location.DirectoryPath,
                ArchivedActivePointerFileName);
            var submissionMoved = false;
            var releaseMoved = false;
            var pointerMoved = false;
            var purgedMetadataWritten = false;
            var archiveMovedToHistory = false;
            var tombstoneCommitted = false;
            try
            {
                Directory.Move(archivedSubmissionPath, stagedSubmissionPath);
                submissionMoved = true;
                if (archived.ActivePointerArchived)
                {
                    File.Move(archivedPointerPath, stagedPointerPath);
                    pointerMoved = true;
                }

                if (archived.PublishedReleaseArchived)
                {
                    Directory.Move(archivedReleasePath, stagedReleasePath);
                    releaseMoved = true;

                    // Re-check after detaching the payload. Tenant configuration
                    // and the active pointer are independently file-backed and
                    // could have changed after the initial guard.
                    blockAllKeyReferences =
                        await EnsureArchivedReleaseCanBePurgedNoLockAsync(
                                archived,
                                CancellationToken.None)
                            .ConfigureAwait(false);
                    var postMoveReferences =
                        await ReadTenantLayoutReferencesNoLockAsync(
                                archived.LayoutKey,
                                archived.LayoutVersion,
                                CancellationToken.None)
                            .ConfigureAwait(false);
                    ThrowIfRemovalHasTenantReferences(
                        archived.LayoutKey,
                        archived.LayoutVersion,
                        postMoveReferences,
                        blockAllKeyReferences);
                }

                var purged = archived with
                {
                    PayloadPurged = true,
                    PurgedBy = purgedBy,
                    PurgedAtUtc = DateTimeOffset.UtcNow,
                    PurgeReason = reason,
                };
                await AtomicReplaceJsonAsync(
                        Path.Combine(
                            location.DirectoryPath,
                            ArchiveMetadataFileName),
                        purged,
                        WorkflowJsonOptions,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                purgedMetadataWritten = true;
                Directory.Move(
                    location.DirectoryPath,
                    purgedArchiveDirectory);
                archiveMovedToHistory = true;
                tombstoneCommitted = true;
                committedPurge = purged;

                if (archived.PublishedReleaseArchived)
                {
                    TryDeleteEmptyDirectory(
                        ArchivedReleaseKeyDirectory(archived.LayoutKey));
                }

                await PruneOrphanedDefinitionPoliciesNoLockAsync(
                        [archived.LayoutKey],
                        purgeStagingDirectory,
                        requirePurgedReceipt: true,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                var policyPreserved = IsDefinitionPolicyPreservedNoLock(
                    archived.LayoutKey);
                DeleteCompletedPurgeStagingNoLock(purgeStagingDirectory);
                await _audit.WriteAsync(
                        archived.PublishedReleaseArchived
                            ? "PurgeArchivedLayoutRelease"
                            : "PurgeArchivedLayoutSubmission",
                        archived.TenantSlug,
                        $"SubmissionId={archived.SubmissionId}; Layout={archived.LayoutKey}@{archived.LayoutVersion}; Status={archived.OriginalStatus}; DefinitionPolicyPreserved={policyPreserved}; Reason={reason}",
                        CancellationToken.None)
                    .ConfigureAwait(false);
                return new WeddingLayoutPurgeResult(
                    purged,
                    AlreadyPurged: false,
                    DefinitionPolicyPreserved: policyPreserved);
            }
            catch (Exception purgeException)
            {
                if (!tombstoneCommitted)
                {
                    try
                    {
                        await RestoreUncommittedPurgeNoLockAsync(
                            location.DirectoryPath,
                            purgedArchiveDirectory,
                            purgeStagingDirectory,
                            archived,
                            submissionMoved,
                            releaseMoved,
                            pointerMoved,
                            purgedMetadataWritten,
                            archiveMovedToHistory)
                            .ConfigureAwait(false);
                    }
                    catch (Exception recoveryException)
                    {
                        throw new InvalidOperationException(
                            $"Permanently deleting archived layout '{archived.LayoutKey}@{archived.LayoutVersion}' failed, and restoring its archived payload also failed.",
                            new AggregateException(
                                purgeException,
                                recoveryException));
                    }
                }

                throw;
            }
            finally
            {
                TryDeleteEmptyDirectory(purgeStagingDirectory);
            }
        }
        finally
        {
            _gate.Release();
            if (committedPurge is not null)
            {
                NotifyChanged(
                    committedPurge,
                    WeddingLayoutSubmissionChangeKind.Purged);
            }
        }
    }

    public Task<WeddingLayoutActivationResult> ActivateAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason = "Activate",
        CancellationToken cancellationToken = default) =>
        ChangeActiveReleaseAsync(
            layoutKey,
            version,
            superAdminSessionToken,
            activatedBy,
            reason,
            "ActivateLayoutRelease",
            cancellationToken);

    public Task<WeddingLayoutActivationResult> RollbackAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason,
        CancellationToken cancellationToken = default) =>
        ChangeActiveReleaseAsync(
            layoutKey,
            version,
            superAdminSessionToken,
            activatedBy,
            reason,
            "RollbackLayoutRelease",
            cancellationToken);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<WeddingLayoutActivationResult> ChangeActiveReleaseAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason,
        string auditAction,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        RequireSuperAdmin(superAdminSessionToken);
        activatedBy = ValidateReviewText(activatedBy, nameof(activatedBy), 1, 120);
        reason = ValidateReviewText(reason, nameof(reason), 1, 300);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureStorageRoots();
            var result = await ActivateNoLockAsync(
                    layoutKey,
                    version,
                    activatedBy,
                    reason,
                    cancellationToken)
                .ConfigureAwait(false);

            var package = await ReadPublishedPackageNoLockAsync(
                    result.Active.Key,
                    result.Active.Version,
                    CancellationToken.None)
                .ConfigureAwait(false);
            await _audit.WriteAsync(
                    auditAction,
                    package.Approval.OwnerTenantSlug,
                    $"Layout={result.Active.Key}@{result.Active.Version}; Previous={result.Previous?.Version ?? "(none)"}; Reason={reason}",
                    CancellationToken.None)
                .ConfigureAwait(false);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<WeddingLayoutActivationResult> ActivateNoLockAsync(
        string layoutKey,
        string version,
        string activatedBy,
        string reason,
        CancellationToken cancellationToken)
    {
        var key = layoutKey?.Trim() ?? "";
        var releaseVersion = version?.Trim() ?? "";
        if (!WeddingLayoutKeys.IsValid(key)
            || !string.Equals(key, layoutKey, StringComparison.Ordinal)
            || !WeddingLayoutVersion.IsValid(releaseVersion)
            || !string.Equals(releaseVersion, version, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Layout key and version must be canonical values.");
        }

        _ = await ReadPublishedPackageNoLockAsync(
                key,
                releaseVersion,
                cancellationToken)
            .ConfigureAwait(false);

        var pointerPath = ActivePointerPath(key);
        var previous = File.Exists(pointerPath)
            ? await ReadJsonFileAsync<WeddingLayoutActiveReleasePointer>(
                    pointerPath,
                    WorkflowJsonOptions,
                    32 * 1024,
                    cancellationToken)
                .ConfigureAwait(false)
            : null;
        var active = new WeddingLayoutActiveReleasePointer
        {
            SchemaVersion = WeddingLayoutActiveReleasePointer.SupportedSchemaVersion,
            Key = key,
            Version = releaseVersion,
            ActivatedBy = activatedBy,
            ActivatedAtUtc = DateTimeOffset.UtcNow,
            Reason = reason,
        };

        await AtomicReplaceJsonAsync(
                pointerPath,
                active,
                WorkflowJsonOptions,
                cancellationToken,
                WeddingLayoutActivePointerFileNames.CreateTemporaryPath)
            .ConfigureAwait(false);

        // The rename above is the commit point. Once it succeeds, caller
        // cancellation must not strand a disk pointer that was never loaded,
        // nor prevent restoration after a rejected reload.
        WeddingLayoutReloadResult reload;
        Exception? reloadException = null;
        try
        {
            reload = await _registry.ReloadAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            reloadException = ex;
            reload = new WeddingLayoutReloadResult(
                false,
                false,
                _registry.PublishedPackages.Count,
                ex.Message);
        }

        if (!reload.Succeeded)
        {
            Exception? restoreException = null;
            try
            {
                if (previous is null)
                {
                    File.Delete(pointerPath);
                }
                else
                {
                    await AtomicReplaceJsonAsync(
                            pointerPath,
                            previous,
                            WorkflowJsonOptions,
                            CancellationToken.None,
                            WeddingLayoutActivePointerFileNames.CreateTemporaryPath)
                        .ConfigureAwait(false);
                }

                var restored = await _registry.ReloadAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                if (!restored.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"The previous active pointer was restored on disk, but its catalog reload failed: {restored.Error}");
                }
            }
            catch (Exception ex)
            {
                restoreException = ex;
            }

            var message =
                $"Active release was not changed because catalog reload failed: {reload.Error}";
            if (restoreException is not null)
            {
                throw new InvalidOperationException(
                    $"{message} Restoring the previous active pointer also failed: {restoreException.Message}",
                    reloadException is null
                        ? restoreException
                        : new AggregateException(reloadException, restoreException));
            }

            throw new InvalidOperationException(message, reloadException);
        }

        return new WeddingLayoutActivationResult(active, previous, reload);
    }

    private async Task PublishReleaseNoLockAsync(
        WeddingLayoutSubmissionRecord submission,
        LayoutPackage package,
        string approvedBy,
        CancellationToken cancellationToken)
    {
        var releaseDirectory = ReleaseDirectory(
            package.Manifest.Key,
            package.Manifest.Version);
        if (Directory.Exists(releaseDirectory))
        {
            var existing = await ReadPublishedPackageNoLockAsync(
                    package.Manifest.Key,
                    package.Manifest.Version,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(
                    existing.Approval.SubmissionId,
                    submission.SubmissionId,
                    StringComparison.Ordinal)
                || !string.Equals(
                    existing.Approval.PackageSha256,
                    submission.PackageSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Immutable release '{package.Manifest.Key}@{package.Manifest.Version}' already exists.");
            }

            // Idempotent recovery after a previous reload/metadata failure.
            return;
        }

        var approval = new WeddingLayoutReleaseApproval
        {
            SchemaVersion = WeddingLayoutReleaseApproval.SupportedSchemaVersion,
            SubmissionId = submission.SubmissionId,
            ApprovedBy = approvedBy,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            OwnerTenantSlug = submission.TenantSlug,
            PackageSha256 = submission.PackageSha256,
        };
        var stagingDirectory = CreateStagingDirectory(
            $"publish-{package.Manifest.Key}-{package.Manifest.Version}-{Guid.NewGuid():N}");
        try
        {
            await WriteJsonFileAsync(
                    Path.Combine(stagingDirectory, ManifestFileName),
                    package.Manifest,
                    PackageJsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);
            await WriteJsonFileAsync(
                    Path.Combine(stagingDirectory, DefinitionFileName),
                    package.Definition,
                    PackageJsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);
            await WriteJsonFileAsync(
                    Path.Combine(stagingDirectory, ApprovalFileName),
                    approval,
                    WorkflowJsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            var keyDirectory = Path.GetDirectoryName(releaseDirectory)!;
            Directory.CreateDirectory(keyDirectory);
            RejectReparsePoint(keyDirectory);
            Directory.Move(stagingDirectory, releaseDirectory);
        }
        finally
        {
            TryDeleteEmptyDirectory(stagingDirectory);
        }
    }

    private async Task EnsureLayoutIdentityAvailableNoLockAsync(
        WeddingLayoutSubmissionRecord candidate,
        IReadOnlyList<WeddingLayoutSubmissionRecord> submissions,
        CancellationToken cancellationToken)
    {
        var conflictingSubmission = submissions.FirstOrDefault(x =>
            x.Status != WeddingLayoutSubmissionStatus.Rejected
            && string.Equals(x.LayoutKey, candidate.LayoutKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.LayoutVersion, candidate.LayoutVersion, StringComparison.Ordinal));
        if (conflictingSubmission is not null)
        {
            throw new InvalidOperationException(
                $"Layout release '{candidate.LayoutKey}@{candidate.LayoutVersion}' already has a submission.");
        }

        var foreignSubmission = submissions.FirstOrDefault(x =>
            x.Status != WeddingLayoutSubmissionStatus.Rejected
            && string.Equals(x.LayoutKey, candidate.LayoutKey, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                x.TenantSlug,
                candidate.TenantSlug,
                StringComparison.OrdinalIgnoreCase));
        if (foreignSubmission is not null)
        {
            throw new UnauthorizedAccessException(
                $"Layout key '{candidate.LayoutKey}' belongs to another tenant.");
        }

        await EnsureArchivedLayoutIdentityAvailableNoLockAsync(
                candidate,
                cancellationToken)
            .ConfigureAwait(false);

        var keyDirectory = Path.Combine(_releasesRoot, candidate.LayoutKey);
        if (!Directory.Exists(keyDirectory))
        {
            return;
        }

        RejectReparsePoint(keyDirectory);
        foreach (var releaseDirectory in Directory.EnumerateDirectories(keyDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(releaseDirectory);
            var approvalPath = Path.Combine(releaseDirectory, ApprovalFileName);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidDataException(
                    $"Published release '{releaseDirectory}' is missing approval metadata.");
            }

            var approval = await ReadJsonFileAsync<WeddingLayoutReleaseApproval>(
                    approvalPath,
                    WorkflowJsonOptions,
                    64 * 1024,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(
                    approval.OwnerTenantSlug,
                    candidate.TenantSlug,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    $"Layout key '{candidate.LayoutKey}' belongs to another tenant.");
            }
        }

        if (Directory.Exists(ReleaseDirectory(candidate.LayoutKey, candidate.LayoutVersion)))
        {
            throw new InvalidOperationException(
                $"Immutable release '{candidate.LayoutKey}@{candidate.LayoutVersion}' already exists.");
        }
    }

    private async Task EnsureArchivedLayoutIdentityAvailableNoLockAsync(
        WeddingLayoutSubmissionRecord candidate,
        CancellationToken cancellationToken)
    {
        var archives = await ReadAllArchiveLocationsNoLockAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var location in archives)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var archived = location.Record;
            if (archived.PayloadPurged)
            {
                MovePurgedArchiveToHistoryNoLock(location);
                if (string.Equals(
                        archived.LayoutKey,
                        candidate.LayoutKey,
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        archived.TenantSlug,
                        candidate.TenantSlug,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException(
                        $"Layout key '{candidate.LayoutKey}' belongs to another tenant.");
                }

                continue;
            }

            if (string.Equals(
                    archived.LayoutKey,
                    candidate.LayoutKey,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    archived.LayoutVersion,
                    candidate.LayoutVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Layout release '{candidate.LayoutKey}@{candidate.LayoutVersion}' is archived and must be permanently deleted before its identity can be reused.");
            }

            if (!string.Equals(
                    archived.LayoutKey,
                    candidate.LayoutKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(
                    archived.TenantSlug,
                    candidate.TenantSlug,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    $"Layout key '{candidate.LayoutKey}' belongs to another tenant.");
            }
        }
    }

    private async Task<IReadOnlyList<ArchivedLayoutLocation>>
        ReadAllArchiveLocationsNoLockAsync(
            CancellationToken cancellationToken)
    {
        var locations = new List<ArchivedLayoutLocation>();
        var submissionDirectories = Directory
            .EnumerateDirectories(_archivedSubmissionsRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Take(MaximumArchivedItems + 1)
            .ToArray();
        if (submissionDirectories.Length > MaximumArchivedItems)
        {
            throw new InvalidDataException(
                $"Archived layout storage exceeds its {MaximumArchivedItems}-item limit.");
        }

        foreach (var directory in submissionDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(directory);
            var submissionId = Path.GetFileName(directory);
            if (!IsSubmissionId(submissionId))
            {
                throw new InvalidDataException(
                    $"Unexpected archived submission directory '{submissionId}'.");
            }

            var record = await ReadJsonFileAsync<WeddingLayoutArchiveRecord>(
                    Path.Combine(directory, ArchiveMetadataFileName),
                    WorkflowJsonOptions,
                    64 * 1024,
                    cancellationToken)
                .ConfigureAwait(false);
            ValidateArchiveTombstone(
                record,
                record.LayoutKey,
                record.LayoutVersion,
                publishedReleaseRequired: false);
            if (!string.Equals(
                    record.SubmissionId,
                    submissionId,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Archived submission directory '{submissionId}' does not match its retention record.");
            }

            locations.Add(new ArchivedLayoutLocation(
                record,
                directory,
                IsPurgedHistory: false));
        }

        var keyDirectories = Directory
            .EnumerateDirectories(_archivedReleasesRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Take(MaximumArchivedLayoutKeys + 1)
            .ToArray();
        if (keyDirectories.Length > MaximumArchivedLayoutKeys)
        {
            throw new InvalidDataException(
                $"Archived layout storage exceeds its {MaximumArchivedLayoutKeys}-key limit.");
        }

        foreach (var keyDirectory in keyDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(keyDirectory);
            var key = Path.GetFileName(keyDirectory);
            if (!WeddingLayoutKeys.IsValid(key))
            {
                throw new InvalidDataException(
                    $"Unexpected archived layout key directory '{key}'.");
            }

            var versionDirectories = Directory
                .EnumerateDirectories(keyDirectory)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Take(MaximumArchivedReleasesPerKey + 1)
                .ToArray();
            if (versionDirectories.Length > MaximumArchivedReleasesPerKey)
            {
                throw new InvalidDataException(
                    $"Archived layout key '{key}' exceeds its {MaximumArchivedReleasesPerKey}-release limit.");
            }

            foreach (var directory in versionDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (locations.Count >= MaximumArchivedItems)
                {
                    throw new InvalidDataException(
                        $"Archived layout storage exceeds its {MaximumArchivedItems}-item limit.");
                }

                RejectReparsePoint(directory);
                var version = Path.GetFileName(directory);
                if (!WeddingLayoutVersion.IsValid(version))
                {
                    throw new InvalidDataException(
                        $"Unexpected archived layout version directory '{version}'.");
                }

                var record = await ReadJsonFileAsync<WeddingLayoutArchiveRecord>(
                        Path.Combine(directory, ArchiveMetadataFileName),
                        WorkflowJsonOptions,
                        64 * 1024,
                        cancellationToken)
                    .ConfigureAwait(false);
                ValidateArchiveTombstone(
                    record,
                    key,
                    version,
                    publishedReleaseRequired: true);
                locations.Add(new ArchivedLayoutLocation(
                    record,
                    directory,
                    IsPurgedHistory: false));
            }
        }

        var purgedDirectories = Directory
            .EnumerateDirectories(_purgedArchivesRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Take(MaximumPurgedHistoryItems + 1)
            .ToArray();
        if (purgedDirectories.Length > MaximumPurgedHistoryItems)
        {
            throw new InvalidDataException(
                $"Purged layout history exceeds its {MaximumPurgedHistoryItems}-item limit.");
        }

        foreach (var directory in purgedDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(directory);
            var submissionId = Path.GetFileName(directory);
            if (!IsSubmissionId(submissionId))
            {
                throw new InvalidDataException(
                    $"Unexpected purged archive directory '{submissionId}'.");
            }

            ValidateExactArchiveEntries(
                directory,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    ArchiveMetadataFileName,
                });
            var record = await ReadJsonFileAsync<WeddingLayoutArchiveRecord>(
                    Path.Combine(directory, ArchiveMetadataFileName),
                    WorkflowJsonOptions,
                    64 * 1024,
                    cancellationToken)
                .ConfigureAwait(false);
            ValidateArchiveTombstone(
                record,
                record.LayoutKey,
                record.LayoutVersion,
                publishedReleaseRequired: record.PublishedReleaseArchived);
            if (!record.PayloadPurged
                || !string.Equals(
                    record.SubmissionId,
                    submissionId,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Purged archive directory '{submissionId}' does not match its retention record.");
            }

            locations.Add(new ArchivedLayoutLocation(
                record,
                directory,
                IsPurgedHistory: true));
        }

        var duplicateSubmission = locations
            .GroupBy(
                location => location.Record.SubmissionId,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Skip(1).Any());
        if (duplicateSubmission is not null)
        {
            throw new InvalidDataException(
                $"Archived submission '{duplicateSubmission.Key}' has duplicate retention records.");
        }

        return locations;
    }

    private async Task<bool> EnsureArchivedReleaseCanBePurgedNoLockAsync(
        WeddingLayoutArchiveRecord archived,
        CancellationToken cancellationToken)
    {
        if (Directory.Exists(
                ReleaseDirectory(
                    archived.LayoutKey,
                    archived.LayoutVersion)))
        {
            throw new InvalidOperationException(
                $"Archived release '{archived.LayoutKey}@{archived.LayoutVersion}' also exists in live release storage and cannot be permanently deleted.");
        }

        var liveVersions = ReadPublishedReleaseVersionsNoLock(
            archived.LayoutKey);
        var activePointerPath = ActivePointerPath(archived.LayoutKey);
        if (File.Exists(activePointerPath))
        {
            var activePointer =
                await ReadJsonFileAsync<WeddingLayoutActiveReleasePointer>(
                        activePointerPath,
                        WorkflowJsonOptions,
                        32 * 1024,
                        cancellationToken)
                    .ConfigureAwait(false);
            ValidateStoredActivePointer(
                activePointer,
                activePointerPath,
                archived.LayoutKey);
            if (string.Equals(
                    activePointer.Version,
                    archived.LayoutVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Archived release '{archived.LayoutKey}@{archived.LayoutVersion}' is referenced by the current active pointer and cannot be permanently deleted.");
            }

            if (!liveVersions.Contains(
                    activePointer.Version,
                    StringComparer.Ordinal))
            {
                throw new InvalidDataException(
                    $"Active pointer '{activePointer.Key}@{activePointer.Version}' references a missing live release.");
            }
        }

        // With no other published version, every follower of the LayoutKey is
        // considered a blocking recovery dependency. Otherwise only an exact
        // version pin blocks purging this retired version.
        return liveVersions.Count == 0;
    }

    private static void ValidateRecoverableArchivePayloadShape(
        ArchivedLayoutLocation location)
    {
        var archived = location.Record;
        var expectedRootEntries = new HashSet<string>(
            StringComparer.Ordinal)
        {
            ArchiveMetadataFileName,
            ArchivedSubmissionDirectoryName,
        };
        if (archived.PublishedReleaseArchived)
        {
            expectedRootEntries.Add(ArchivedReleaseDirectoryName);
        }

        if (archived.ActivePointerArchived)
        {
            expectedRootEntries.Add(ArchivedActivePointerFileName);
        }

        ValidateExactArchiveEntries(
            location.DirectoryPath,
            expectedRootEntries);
        ValidateExactArchiveEntries(
            Path.Combine(
                location.DirectoryPath,
                ArchivedSubmissionDirectoryName),
            new HashSet<string>(StringComparer.Ordinal)
            {
                SubmissionMetadataFileName,
                PackageFileName,
            });
        if (archived.PublishedReleaseArchived)
        {
            ValidateExactArchiveEntries(
                Path.Combine(
                    location.DirectoryPath,
                    ArchivedReleaseDirectoryName),
                new HashSet<string>(StringComparer.Ordinal)
                {
                    ManifestFileName,
                    DefinitionFileName,
                    ApprovalFileName,
                });
        }
    }

    private static void ValidateExactArchiveEntries(
        string directory,
        IReadOnlySet<string> expectedEntries)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Required archived payload directory '{Path.GetFileName(directory)}' was not found.");
        }

        RejectReparsePoint(directory);
        var entries = Directory
            .EnumerateFileSystemEntries(directory)
            .ToArray();
        var actualNames = entries
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);
        if (!actualNames.SetEquals(expectedEntries))
        {
            throw new InvalidDataException(
                $"Archived payload directory '{Path.GetFileName(directory)}' contains missing or unexpected entries.");
        }

        foreach (var entry in entries)
        {
            RejectReparsePoint(entry);
        }
    }

    private async Task<IReadOnlySet<string>>
        PruneOrphanedDefinitionPoliciesNoLockAsync(
            IEnumerable<string> candidateKeys,
            string stagingDirectory,
            bool requirePurgedReceipt,
            CancellationToken cancellationToken)
    {
        var removed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keys = candidateKeys
            .Where(WeddingLayoutKeys.IsValid)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key =>
                WeddingLayoutCatalog.Instance.FindDescriptor(key) is null)
            .ToArray();
        if (keys.Length == 0)
        {
            return removed;
        }

        var liveOrRecoverableKeys = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var purgedReceiptKeys = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var submission in await ReadAllSubmissionRecordsNoLockAsync(
                             cancellationToken)
                         .ConfigureAwait(false))
            {
                liveOrRecoverableKeys.Add(submission.LayoutKey);
            }

            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (liveOrRecoverableKeys.Contains(key)
                    || ReadPublishedReleaseVersionsNoLock(key).Count > 0
                    || File.Exists(ActivePointerPath(key)))
                {
                    liveOrRecoverableKeys.Add(key);
                }
            }

            var archiveCheckKeys = keys
                .Where(key => !liveOrRecoverableKeys.Contains(key))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (archiveCheckKeys.Count == 0)
            {
                return removed;
            }

            foreach (var archive in await ReadAllArchiveLocationsNoLockAsync(
                             cancellationToken)
                         .ConfigureAwait(false))
            {
                if (!archiveCheckKeys.Contains(archive.Record.LayoutKey))
                {
                    continue;
                }

                if (archive.Record.PayloadPurged)
                {
                    purgedReceiptKeys.Add(archive.Record.LayoutKey);
                }
                else
                {
                    liveOrRecoverableKeys.Add(archive.Record.LayoutKey);
                }
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Storage that cannot be proved artifact-free is never eligible for
            // automatic policy deletion.
            return removed;
        }

        var orphanKeys = keys
            .Where(key => !liveOrRecoverableKeys.Contains(key))
            .Where(key =>
                !requirePurgedReceipt || purgedReceiptKeys.Contains(key))
            .ToArray();
        if (orphanKeys.Length == 0)
        {
            return removed;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var fullStagingDirectory = Path.GetFullPath(stagingDirectory);
        if (!IsSameOrChildPath(fullStagingDirectory, _stagingRoot))
        {
            throw new InvalidDataException(
                "The definition-policy cleanup staging path is invalid.");
        }

        EnsureDirectory(fullStagingDirectory);
        RejectReparsePoint(fullStagingDirectory);
        var movedPolicies = new List<(string Source, string Staged)>();
        try
        {
            foreach (var key in orphanKeys)
            {
                var source = DefinitionPolicyPath(key);
                if (!File.Exists(source))
                {
                    continue;
                }

                RejectReparsePoint(source);
                var staged = Path.Combine(
                    fullStagingDirectory,
                    $"definition-policy-{key}.json");
                if (File.Exists(staged) || Directory.Exists(staged))
                {
                    throw new IOException(
                        $"Definition-policy cleanup staging for '{key}' already exists.");
                }

                File.Move(source, staged);
                movedPolicies.Add((source, staged));
            }

            var reloadSucceeded = await TryReloadCatalogNoThrowAsync()
                .ConfigureAwait(false);
            if (!reloadSucceeded)
            {
                await RestoreDefinitionPoliciesNoLockAsync(movedPolicies)
                    .ConfigureAwait(false);
                return removed;
            }
        }
        catch (Exception cleanupException)
        {
            try
            {
                await RestoreDefinitionPoliciesNoLockAsync(movedPolicies)
                    .ConfigureAwait(false);
            }
            catch (Exception restoreException)
            {
                throw new InvalidOperationException(
                    "Definition-policy cleanup failed, and restoring the preserved policies also failed.",
                    new AggregateException(cleanupException, restoreException));
            }

            return removed;
        }

        foreach (var (_, staged) in movedPolicies)
        {
            try
            {
                File.Delete(staged);
            }
            catch
            {
                // The authoritative policy has already been removed and the
                // registry snapshot was committed. A private staging remnant is
                // ignored and can be removed by the normal purge cleanup/retry.
            }
        }

        removed.UnionWith(orphanKeys);
        return removed;
    }

    private async Task RestoreDefinitionPoliciesNoLockAsync(
        IReadOnlyList<(string Source, string Staged)> movedPolicies)
    {
        var restoreErrors = new List<Exception>();
        foreach (var (source, staged) in movedPolicies.Reverse())
        {
            try
            {
                if (!File.Exists(staged))
                {
                    continue;
                }

                if (File.Exists(source) || Directory.Exists(source))
                {
                    throw new IOException(
                        $"Definition-policy restore destination '{source}' already exists.");
                }

                File.Move(staged, source);
            }
            catch (Exception ex)
            {
                restoreErrors.Add(ex);
            }
        }

        if (restoreErrors.Count > 0)
        {
            throw new AggregateException(
                "One or more definition policies could not be restored.",
                restoreErrors);
        }

        // The first rejected reload retains its last-known-good snapshot, but an
        // explicit non-cancelable reload also reconciles any watcher-observed
        // intermediate deletion with the restored files.
        await TryReloadCatalogNoThrowAsync().ConfigureAwait(false);
    }

    private async Task<bool> TryReloadCatalogNoThrowAsync()
    {
        try
        {
            var reload = await _registry.ReloadAsync(CancellationToken.None)
                .ConfigureAwait(false);
            return reload.Succeeded;
        }
        catch
        {
            return false;
        }
    }

    private bool IsDefinitionPolicyPreservedNoLock(string layoutKey) =>
        WeddingLayoutCatalog.Instance.FindDescriptor(layoutKey) is not null
        || File.Exists(DefinitionPolicyPath(layoutKey))
        || _registry.DefinitionPolicies.ContainsKey(layoutKey);

    private string PermanentPurgeStagingDirectory(string submissionId)
    {
        if (!IsSubmissionId(submissionId))
        {
            throw new InvalidDataException(
                "The permanent-delete staging submission id is invalid.");
        }

        var path = Path.GetFullPath(
            Path.Combine(_stagingRoot, $"purge-{submissionId}"));
        if (!IsSameOrChildPath(path, _stagingRoot))
        {
            throw new InvalidDataException(
                "The permanent-delete staging path is invalid.");
        }

        return path;
    }

    private void MovePurgedArchiveToHistoryNoLock(
        ArchivedLayoutLocation location)
    {
        if (!location.Record.PayloadPurged)
        {
            throw new InvalidOperationException(
                "Only permanently deleted archive records can be moved to purge history.");
        }

        if (location.IsPurgedHistory)
        {
            DeleteCompletedPurgeStagingNoLock(
                PermanentPurgeStagingDirectory(
                    location.Record.SubmissionId));
            return;
        }

        ValidateExactArchiveEntries(
            location.DirectoryPath,
            new HashSet<string>(StringComparer.Ordinal)
            {
                ArchiveMetadataFileName,
            });
        var historyDirectory = PurgedArchiveDirectory(
            location.Record.SubmissionId);
        if (Directory.Exists(historyDirectory)
            || File.Exists(historyDirectory))
        {
            throw new InvalidDataException(
                $"Purged archive history for submission '{location.Record.SubmissionId}' already exists.");
        }

        Directory.Move(location.DirectoryPath, historyDirectory);
        if (location.Record.PublishedReleaseArchived)
        {
            TryDeleteEmptyDirectory(
                ArchivedReleaseKeyDirectory(location.Record.LayoutKey));
        }

        DeleteCompletedPurgeStagingNoLock(
            PermanentPurgeStagingDirectory(
                location.Record.SubmissionId));
    }

    private static void DeleteCompletedPurgeStagingNoLock(string stagingDirectory)
    {
        if (!Directory.Exists(stagingDirectory))
        {
            return;
        }

        RejectReparsePointsRecursively(stagingDirectory);
        Directory.Delete(stagingDirectory, recursive: true);
    }

    private static void RejectReparsePointsRecursively(string root)
    {
        RejectReparsePoint(root);
        var pending = new Stack<string>();
        pending.Push(root);
        var inspected = 0;
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                if (++inspected > 32)
                {
                    throw new InvalidDataException(
                        "Permanent-delete staging contains too many payload entries.");
                }

                RejectReparsePoint(entry);
                if (Directory.Exists(entry))
                {
                    pending.Push(entry);
                }
            }
        }
    }

    private static async Task RestoreUncommittedPurgeNoLockAsync(
        string archiveDirectory,
        string purgedArchiveDirectory,
        string stagingDirectory,
        WeddingLayoutArchiveRecord archived,
        bool submissionMoved,
        bool releaseMoved,
        bool pointerMoved,
        bool purgedMetadataWritten,
        bool archiveMovedToHistory)
    {
        var recoveryErrors = new List<Exception>();
        if (archiveMovedToHistory)
        {
            try
            {
                if (!Directory.Exists(purgedArchiveDirectory))
                {
                    throw new DirectoryNotFoundException(
                        "The purged archive history is unavailable for recovery.");
                }

                if (Directory.Exists(archiveDirectory)
                    || File.Exists(archiveDirectory))
                {
                    throw new IOException(
                        $"Archive recovery destination '{archiveDirectory}' already exists.");
                }

                EnsureDirectory(
                    Path.GetDirectoryName(archiveDirectory)
                    ?? throw new InvalidDataException(
                        "The archive recovery parent directory is unavailable."));
                Directory.Move(purgedArchiveDirectory, archiveDirectory);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        if (purgedMetadataWritten)
        {
            try
            {
                await AtomicReplaceJsonAsync(
                        Path.Combine(
                            archiveDirectory,
                            ArchiveMetadataFileName),
                        archived,
                        WorkflowJsonOptions,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        try
        {
            RestorePurgeStagingNoLock(
                archiveDirectory,
                stagingDirectory,
                submissionMoved,
                releaseMoved,
                pointerMoved);
        }
        catch (Exception ex)
        {
            recoveryErrors.Add(ex);
        }

        if (recoveryErrors.Count > 0)
        {
            throw new AggregateException(
                "One or more archived layout payloads could not be restored.",
                recoveryErrors);
        }
    }

    private static void RestorePurgeStagingNoLock(
        string archiveDirectory,
        string stagingDirectory,
        bool submissionMoved,
        bool releaseMoved,
        bool pointerMoved)
    {
        var recoveryErrors = new List<Exception>();

        void RestoreDirectory(string name, bool moved)
        {
            if (!moved)
            {
                return;
            }

            try
            {
                var source = Path.Combine(stagingDirectory, name);
                var destination = Path.Combine(archiveDirectory, name);
                if (!Directory.Exists(source))
                {
                    throw new DirectoryNotFoundException(
                        $"Staged archived payload '{name}' is unavailable for recovery.");
                }

                if (Directory.Exists(destination)
                    || File.Exists(destination))
                {
                    throw new IOException(
                        $"Archived payload recovery destination '{destination}' already exists.");
                }

                Directory.Move(source, destination);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        RestoreDirectory(ArchivedReleaseDirectoryName, releaseMoved);
        if (pointerMoved)
        {
            try
            {
                var source = Path.Combine(
                    stagingDirectory,
                    ArchivedActivePointerFileName);
                var destination = Path.Combine(
                    archiveDirectory,
                    ArchivedActivePointerFileName);
                if (!File.Exists(source))
                {
                    throw new FileNotFoundException(
                        "The staged archived active pointer is unavailable for recovery.",
                        source);
                }

                if (File.Exists(destination)
                    || Directory.Exists(destination))
                {
                    throw new IOException(
                        $"Archived pointer recovery destination '{destination}' already exists.");
                }

                File.Move(source, destination);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        RestoreDirectory(ArchivedSubmissionDirectoryName, submissionMoved);
        if (recoveryErrors.Count > 0)
        {
            throw new AggregateException(
                "One or more permanently deleted archive payloads could not be restored.",
                recoveryErrors);
        }

        TryDeleteEmptyDirectory(stagingDirectory);
    }

    private IReadOnlyList<string> ReadPublishedReleaseVersionsNoLock(string layoutKey)
    {
        var keyDirectory = Path.GetFullPath(Path.Combine(_releasesRoot, layoutKey));
        if (!IsSameOrChildPath(keyDirectory, _releasesRoot)
            || !Directory.Exists(keyDirectory))
        {
            return [];
        }

        RejectReparsePoint(keyDirectory);
        var versions = new List<string>();
        foreach (var releaseDirectory in Directory
                     .EnumerateDirectories(keyDirectory)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            RejectReparsePoint(releaseDirectory);
            var version = Path.GetFileName(releaseDirectory);
            if (!WeddingLayoutVersion.IsValid(version))
            {
                throw new InvalidDataException(
                    $"Unexpected published layout version directory '{version}'.");
            }

            versions.Add(version);
        }

        return versions;
    }

    private static void ValidatePublishedArchiveLineage(
        WeddingLayoutSubmissionRecord submission,
        WeddingLayoutPublishedPackage published)
    {
        if (!string.Equals(
                published.Manifest.Key,
                submission.LayoutKey,
                StringComparison.Ordinal)
            || !string.Equals(
                published.Manifest.Version,
                submission.LayoutVersion,
                StringComparison.Ordinal)
            || !string.Equals(
                published.Approval.SubmissionId,
                submission.SubmissionId,
                StringComparison.Ordinal)
            || !string.Equals(
                published.Approval.OwnerTenantSlug,
                submission.TenantSlug,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                published.Approval.PackageSha256,
                submission.PackageSha256,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Approved submission '{submission.SubmissionId}' does not match its immutable published release.");
        }
    }

    private static void ValidateStoredActivePointer(
        WeddingLayoutActiveReleasePointer pointer,
        string path,
        string expectedKey)
    {
        if (pointer.SchemaVersion
                != WeddingLayoutActiveReleasePointer.SupportedSchemaVersion
            || !string.Equals(pointer.Key, expectedKey, StringComparison.Ordinal)
            || !string.Equals(
                Path.GetFileNameWithoutExtension(path),
                pointer.Key,
                StringComparison.Ordinal)
            || !WeddingLayoutVersion.IsValid(pointer.Version)
            || string.IsNullOrWhiteSpace(pointer.ActivatedBy)
            || pointer.ActivatedBy.Length > 120
            || pointer.ActivatedBy.Any(char.IsControl)
            || pointer.ActivatedAtUtc == default
            || pointer.ActivatedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)
            || string.IsNullOrWhiteSpace(pointer.Reason)
            || pointer.Reason.Length > 300
            || pointer.Reason.Any(char.IsControl))
        {
            throw new InvalidDataException(
                $"Active layout pointer '{Path.GetFileName(path)}' is invalid.");
        }
    }

    private async Task<IReadOnlyList<TenantLayoutReference>>
        ReadTenantLayoutReferencesNoLockAsync(
            string layoutKey,
            string layoutVersion,
            CancellationToken cancellationToken)
    {
        var tenants = await _tenants.GetAllAsync(cancellationToken)
            .ConfigureAwait(false);
        var references = new List<TenantLayoutReference>();
        foreach (var tenant in tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var design = tenant.DesignSettings;
            var tenantLayoutKey = string.IsNullOrWhiteSpace(design?.LayoutKey)
                ? WeddingLayoutCatalog.ToLegacyKey(
                    InvitationDesignCatalog.ResolveLayoutMode(
                        design?.LayoutMode ?? WeddingLayoutMode.Unknown,
                        tenant.InvitationStyle))
                : WeddingLayoutKeys.Normalize(design.LayoutKey);
            if (!string.Equals(
                    tenantLayoutKey,
                    layoutKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hasPinnedVersion = design is not null
                && !design.FollowActiveLayoutVersion
                && WeddingLayoutVersion.IsValid(design.LayoutVersion);
            var pinnedVersion = hasPinnedVersion
                ? design?.LayoutVersion.Trim() ?? ""
                : "";
            references.Add(new TenantLayoutReference(
                tenant.Slug,
                hasPinnedVersion
                    && string.Equals(
                        pinnedVersion,
                        layoutVersion,
                        StringComparison.Ordinal),
                !hasPinnedVersion));
        }

        return references;
    }

    private static void ThrowIfRemovalHasTenantReferences(
        string layoutKey,
        string layoutVersion,
        IReadOnlyList<TenantLayoutReference> tenantReferences,
        bool blockAllKeyReferences)
    {
        var blocking = tenantReferences
            .Where(reference =>
                blockAllKeyReferences || reference.IsPinnedToTargetVersion)
            .OrderBy(reference => reference.TenantSlug, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (blocking.Length == 0)
        {
            return;
        }

        var visibleSlugs = string.Join(
            ", ",
            blocking.Take(10).Select(reference => reference.TenantSlug));
        var remainder = blocking.Length > 10
            ? $" (+{blocking.Length - 10} more)"
            : "";
        var referenceKind = blockAllKeyReferences
            ? "the layout key"
            : "that exact release";
        throw new InvalidOperationException(
            $"Layout release '{layoutKey}@{layoutVersion}' cannot be removed because {blocking.Length} tenant(s) reference {referenceKind}: {visibleSlugs}{remainder}.");
    }

    private async Task RestoreArchiveStagingNoLockAsync(
        string stagingDirectory,
        string submissionDirectory,
        string? releaseDirectory,
        string? activePointerPath,
        bool submissionMoved,
        bool releaseMoved,
        bool pointerMoved,
        bool reloadRequired)
    {
        var recoveryErrors = new List<Exception>();
        if (releaseMoved)
        {
            try
            {
                var archivedReleasePath = Path.Combine(
                    stagingDirectory,
                    ArchivedReleaseDirectoryName);
                if (!Directory.Exists(archivedReleasePath))
                {
                    throw new DirectoryNotFoundException(
                        "The staged release is unavailable for recovery.");
                }

                EnsureDirectory(
                    Path.GetDirectoryName(releaseDirectory!)
                    ?? throw new InvalidDataException(
                        "The release recovery directory is unavailable."));
                if (Directory.Exists(releaseDirectory))
                {
                    throw new IOException(
                        $"Release recovery destination '{releaseDirectory}' already exists.");
                }

                Directory.Move(archivedReleasePath, releaseDirectory!);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        if (pointerMoved)
        {
            try
            {
                if (releaseDirectory is not null
                    && !Directory.Exists(releaseDirectory))
                {
                    throw new IOException(
                        "The active pointer cannot be restored before its release.");
                }

                var archivedPointerPath = Path.Combine(
                    stagingDirectory,
                    ArchivedActivePointerFileName);
                if (!File.Exists(archivedPointerPath))
                {
                    throw new FileNotFoundException(
                        "The staged active pointer is unavailable for recovery.",
                        archivedPointerPath);
                }

                if (File.Exists(activePointerPath))
                {
                    throw new IOException(
                        $"Active pointer recovery destination '{activePointerPath}' already exists.");
                }

                File.Move(archivedPointerPath, activePointerPath!);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        if (submissionMoved)
        {
            try
            {
                var archivedSubmissionPath = Path.Combine(
                    stagingDirectory,
                    ArchivedSubmissionDirectoryName);
                if (!Directory.Exists(archivedSubmissionPath))
                {
                    throw new DirectoryNotFoundException(
                        "The staged submission is unavailable for recovery.");
                }

                if (Directory.Exists(submissionDirectory))
                {
                    throw new IOException(
                        $"Submission recovery destination '{submissionDirectory}' already exists.");
                }

                Directory.Move(archivedSubmissionPath, submissionDirectory);
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        if (reloadRequired)
        {
            try
            {
                var restored = await _registry.ReloadAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                if (!restored.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"The archived files were restored, but catalog reload failed: {restored.Error}");
                }
            }
            catch (Exception ex)
            {
                recoveryErrors.Add(ex);
            }
        }

        if (recoveryErrors.Count > 0)
        {
            throw new AggregateException(
                "One or more archived layout files could not be restored.",
                recoveryErrors);
        }

        var tombstonePath = Path.Combine(
            stagingDirectory,
            ArchiveMetadataFileName);
        if (File.Exists(tombstonePath))
        {
            File.Delete(tombstonePath);
        }

        TryDeleteEmptyDirectory(stagingDirectory);
    }

    private async Task<TenantConfig> RequireTenantAdministratorAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var tenant = await RequireTenantAsync(tenantSlug, cancellationToken)
            .ConfigureAwait(false);
        var isOwner = !string.IsNullOrWhiteSpace(tenant.OwnerUserId)
            && string.Equals(tenant.OwnerUserId, actor.Id, StringComparison.Ordinal);
        var isAdmin = tenant.AdminUsers?.Any(x =>
            string.Equals(x.UserId, actor.Id, StringComparison.Ordinal)) == true;
        if (!isOwner && !isAdmin)
        {
            throw new UnauthorizedAccessException(
                "Only an authenticated tenant owner or administrator may submit layouts.");
        }

        return tenant;
    }

    private async Task<TenantConfig> RequireTenantAsync(
        string tenantSlug,
        CancellationToken cancellationToken)
    {
        var slug = tenantSlug?.Trim() ?? "";
        if (!WeddingLayoutKeys.IsValid(slug))
        {
            throw new InvalidDataException("The tenant slug is invalid.");
        }

        return await _tenants.GetAsync(slug, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Tenant '{slug}' was not found.");
    }

    private void RequireSuperAdmin(string sessionToken)
    {
        if (!_superAdminTokens.ValidateToken(sessionToken))
        {
            throw new UnauthorizedAccessException(
                "A valid super-administrator session is required.");
        }
    }

    private static async Task<LayoutPackage> ReadAndValidatePackageAsync(
        Stream packageJson,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadBoundedStreamAsync(
                packageJson,
                MaximumUploadBytes,
                cancellationToken)
            .ConfigureAwait(false);
        LayoutPackage package;
        try
        {
            package = JsonSerializer.Deserialize<LayoutPackage>(
                    bytes,
                    LayoutPackageJson.CreateOptions())
                ?? throw new InvalidDataException("The layout package is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"The layout package JSON is invalid: {ex.Message}",
                ex);
        }

        var validation = LayoutPackageValidator.Validate(package);
        if (!validation.IsValid)
        {
            throw new LayoutPackageValidationException(validation.Errors);
        }

        return LayoutPackageCanonicalizer.Canonicalize(package);
    }

    private async Task<LayoutPackage> ReadStoredPackageNoLockAsync(
        WeddingLayoutSubmissionRecord record,
        CancellationToken cancellationToken)
    {
        var packagePath = Path.Combine(
            SubmissionDirectory(record.SubmissionId),
            PackageFileName);
        var bytes = await ReadBoundedFileAsync(
                packagePath,
                MaximumUploadBytes,
                cancellationToken)
            .ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(hash, record.PackageSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Submission '{record.SubmissionId}' package hash does not match its metadata.");
        }

        await using var stream = new MemoryStream(bytes, writable: false);
        var package = await ReadAndValidatePackageAsync(stream, cancellationToken)
            .ConfigureAwait(false);
        if (!string.Equals(package.Manifest.Key, record.LayoutKey, StringComparison.Ordinal)
            || !string.Equals(package.Manifest.Version, record.LayoutVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Submission '{record.SubmissionId}' package identity does not match its metadata.");
        }

        return package;
    }

    private async Task<WeddingLayoutPublishedPackage> ReadPublishedPackageNoLockAsync(
        string key,
        string version,
        CancellationToken cancellationToken)
    {
        var directory = ReleaseDirectory(key, version);
        if (!Directory.Exists(directory))
        {
            throw new KeyNotFoundException(
                $"Published release '{key}@{version}' was not found.");
        }

        RejectReparsePoint(directory);
        var manifest = await ReadJsonFileAsync<LayoutManifest>(
                Path.Combine(directory, ManifestFileName),
                PackageJsonOptions,
                64 * 1024,
                cancellationToken)
            .ConfigureAwait(false);
        var definition = await ReadJsonFileAsync<LayoutDefinition>(
                Path.Combine(directory, DefinitionFileName),
                PackageJsonOptions,
                256 * 1024,
                cancellationToken)
            .ConfigureAwait(false);
        var approval = await ReadJsonFileAsync<WeddingLayoutReleaseApproval>(
                Path.Combine(directory, ApprovalFileName),
                WorkflowJsonOptions,
                64 * 1024,
                cancellationToken)
            .ConfigureAwait(false);
        var package = new LayoutPackage { Manifest = manifest, Definition = definition };
        var validation = LayoutPackageValidator.Validate(package);
        if (!validation.IsValid)
        {
            throw new LayoutPackageValidationException(validation.Errors);
        }

        return new WeddingLayoutPublishedPackage(
            LayoutPackageCanonicalizer.Canonicalize(package),
            approval);
    }

    private async Task<IReadOnlyList<WeddingLayoutSubmissionRecord>>
        ReadAllSubmissionRecordsNoLockAsync(CancellationToken cancellationToken)
    {
        var directories = Directory
            .EnumerateDirectories(_submissionsRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Take(MaximumSubmissions + 1)
            .ToArray();
        if (directories.Length > MaximumSubmissions)
        {
            throw new InvalidDataException(
                $"Layout submission storage exceeds its {MaximumSubmissions}-item limit.");
        }

        var records = new List<WeddingLayoutSubmissionRecord>(directories.Length);
        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RejectReparsePoint(directory);
            var submissionId = Path.GetFileName(directory);
            if (!IsSubmissionId(submissionId))
            {
                throw new InvalidDataException(
                    $"Unexpected layout submission directory '{submissionId}'.");
            }

            records.Add(await ReadSubmissionRecordNoLockAsync(
                    submissionId,
                    cancellationToken)
                .ConfigureAwait(false));
        }

        return records;
    }

    private async Task<WeddingLayoutSubmissionRecord> ReadSubmissionRecordNoLockAsync(
        string submissionId,
        CancellationToken cancellationToken)
    {
        if (!IsSubmissionId(submissionId))
        {
            throw new InvalidDataException("The submission id is invalid.");
        }

        var directory = SubmissionDirectory(submissionId);
        if (!Directory.Exists(directory))
        {
            throw new KeyNotFoundException(
                $"Layout submission '{submissionId}' was not found.");
        }

        RejectReparsePoint(directory);
        var record = await ReadJsonFileAsync<WeddingLayoutSubmissionRecord>(
                Path.Combine(directory, SubmissionMetadataFileName),
                WorkflowJsonOptions,
                64 * 1024,
                cancellationToken)
            .ConfigureAwait(false);
        ValidateSubmissionRecord(record, submissionId);
        return record;
    }

    private async Task ReplaceSubmissionRecordNoLockAsync(
        WeddingLayoutSubmissionRecord record,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            SubmissionDirectory(record.SubmissionId),
            SubmissionMetadataFileName);
        await AtomicReplaceJsonAsync(
                path,
                record,
                WorkflowJsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<LayoutDefinitionPolicy> ReadDefinitionPolicyNoLockAsync(
        string layoutKey,
        CancellationToken cancellationToken)
    {
        var path = DefinitionPolicyPath(layoutKey);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Layout '{layoutKey}' must be classified as Free or Premium before approval.");
        }

        var policy = await ReadJsonFileAsync<LayoutDefinitionPolicy>(
                path,
                WorkflowJsonOptions,
                64 * 1024,
                cancellationToken)
            .ConfigureAwait(false);
        ValidateDefinitionPolicy(policy, layoutKey);
        return policy;
    }

    private static void ValidateDefinitionPolicy(
        LayoutDefinitionPolicy policy,
        string expectedKey)
    {
        if (policy.SchemaVersion != LayoutDefinitionPolicy.SupportedSchemaVersion
            || !string.Equals(
                policy.LayoutKey,
                expectedKey,
                StringComparison.Ordinal)
            || !WeddingLayoutKeys.IsValid(policy.LayoutKey)
            || !Enum.IsDefined(policy.Tier)
            || string.IsNullOrWhiteSpace(policy.ClassifiedBy)
            || policy.ClassifiedBy.Length > 120
            || policy.ClassifiedBy.Any(char.IsControl)
            || policy.ClassifiedBy.Contains('<')
            || policy.ClassifiedBy.Contains('>')
            || policy.ClassifiedAtUtc == default
            || policy.ClassifiedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)
            || string.IsNullOrWhiteSpace(policy.Reason)
            || policy.Reason.Length > 300
            || policy.Reason.Any(char.IsControl)
            || policy.Reason.Contains('<')
            || policy.Reason.Contains('>')
            || policy.Revision < 1)
        {
            throw new InvalidDataException(
                $"Layout definition policy '{expectedKey}' is invalid.");
        }
    }

    private static void ValidateSubmissionRecord(
        WeddingLayoutSubmissionRecord record,
        string expectedId)
    {
        if (record.SchemaVersion != WeddingLayoutSubmissionRecord.SupportedSchemaVersion
            || !string.Equals(record.SubmissionId, expectedId, StringComparison.Ordinal)
            || !IsSubmissionId(record.SubmissionId)
            || !WeddingLayoutKeys.IsValid(record.TenantSlug)
            || !WeddingLayoutKeys.IsValid(record.LayoutKey)
            || !WeddingLayoutVersion.IsValid(record.LayoutVersion)
            || string.IsNullOrWhiteSpace(record.SubmittedByUserId)
            || record.SubmittedAtUtc == default
            || !Enum.IsDefined(record.Status)
            || !Enum.IsDefined(record.LegacyManifestTierSnapshot)
            || record.PackageSha256.Length != 64
            || record.PackageSha256.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException(
                $"Layout submission '{expectedId}' metadata is invalid.");
        }
    }

    private static void ValidateArchiveTombstone(
        WeddingLayoutArchiveRecord tombstone,
        string expectedKey,
        string expectedVersion,
        bool publishedReleaseRequired)
    {
        var statusMatchesPayload = publishedReleaseRequired
            ? tombstone.OriginalStatus == WeddingLayoutSubmissionStatus.Approved
            : tombstone.OriginalStatus is WeddingLayoutSubmissionStatus.Pending
                or WeddingLayoutSubmissionStatus.Rejected;
        if (tombstone.SchemaVersion
                != WeddingLayoutArchiveRecord.SupportedSchemaVersion
            || !IsSubmissionId(tombstone.SubmissionId)
            || !statusMatchesPayload
            || !WeddingLayoutKeys.IsValid(tombstone.TenantSlug)
            || !string.Equals(
                tombstone.LayoutKey,
                expectedKey,
                StringComparison.Ordinal)
            || !string.Equals(
                tombstone.LayoutVersion,
                expectedVersion,
                StringComparison.Ordinal)
            || !WeddingLayoutKeys.IsValid(tombstone.LayoutKey)
            || !WeddingLayoutVersion.IsValid(tombstone.LayoutVersion)
            || tombstone.PackageSha256.Length != 64
            || tombstone.PackageSha256.Any(character => !Uri.IsHexDigit(character))
            || tombstone.PublishedReleaseArchived != publishedReleaseRequired
            || (tombstone.ActivePointerArchived
                && !tombstone.PublishedReleaseArchived)
            || string.IsNullOrWhiteSpace(tombstone.ArchivedBy)
            || tombstone.ArchivedBy.Length > 120
            || tombstone.ArchivedBy.Any(char.IsControl)
            || tombstone.ArchivedBy.Contains('<')
            || tombstone.ArchivedBy.Contains('>')
            || tombstone.ArchivedAtUtc == default
            || tombstone.ArchivedAtUtc > DateTimeOffset.UtcNow.AddMinutes(5)
            || string.IsNullOrWhiteSpace(tombstone.Reason)
            || tombstone.Reason.Length > 300
            || tombstone.Reason.Any(char.IsControl)
            || tombstone.Reason.Contains('<')
            || tombstone.Reason.Contains('>')
            || (tombstone.PayloadPurged
                && (string.IsNullOrWhiteSpace(tombstone.PurgedBy)
                    || tombstone.PurgedBy.Length > 120
                    || tombstone.PurgedBy.Any(char.IsControl)
                    || tombstone.PurgedBy.Contains('<')
                    || tombstone.PurgedBy.Contains('>')
                    || tombstone.PurgedAtUtc is null
                    || tombstone.PurgedAtUtc == default
                    || tombstone.PurgedAtUtc
                        > DateTimeOffset.UtcNow.AddMinutes(5)
                    || string.IsNullOrWhiteSpace(tombstone.PurgeReason)
                    || tombstone.PurgeReason.Length > 300
                    || tombstone.PurgeReason.Any(char.IsControl)
                    || tombstone.PurgeReason.Contains('<')
                    || tombstone.PurgeReason.Contains('>')))
            || (!tombstone.PayloadPurged
                && (!string.IsNullOrEmpty(tombstone.PurgedBy)
                    || tombstone.PurgedAtUtc is not null
                    || !string.IsNullOrEmpty(tombstone.PurgeReason))))
        {
            throw new InvalidDataException(
                $"Archived release '{expectedKey}@{expectedVersion}' has invalid tombstone metadata.");
        }
    }

    private void EnsureStorageRoots()
    {
        EnsureDirectory(_layoutPackagesRoot);
        EnsureDirectory(_submissionsRoot);
        EnsureDirectory(_releasesRoot);
        EnsureDirectory(_activeRoot);
        EnsureDirectory(_policiesRoot);
        EnsureDirectory(_stagingRoot);
        EnsureDirectory(_archiveRoot);
        EnsureDirectory(_archivedSubmissionsRoot);
        EnsureDirectory(_archivedReleasesRoot);
        EnsureDirectory(_purgedArchivesRoot);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        RejectReparsePoint(path);
    }

    private string CreateStagingDirectory(string name)
    {
        var path = Path.Combine(_stagingRoot, name);
        if (!IsSameOrChildPath(path, _stagingRoot))
        {
            throw new InvalidDataException("The staging path is invalid.");
        }

        Directory.CreateDirectory(path);
        RejectReparsePoint(path);
        return path;
    }

    private string SubmissionDirectory(string submissionId)
    {
        if (!IsSubmissionId(submissionId))
        {
            throw new InvalidDataException("The submission id is invalid.");
        }

        return Path.Combine(_submissionsRoot, submissionId);
    }

    private string ArchivedSubmissionDirectory(string submissionId)
    {
        if (!IsSubmissionId(submissionId))
        {
            throw new InvalidDataException("The archived submission id is invalid.");
        }

        var path = Path.GetFullPath(
            Path.Combine(_archivedSubmissionsRoot, submissionId));
        if (!IsSameOrChildPath(path, _archivedSubmissionsRoot))
        {
            throw new InvalidDataException(
                "The archived submission path is invalid.");
        }

        return path;
    }

    private string ArchivedReleaseKeyDirectory(string key)
    {
        if (!WeddingLayoutKeys.IsValid(key))
        {
            throw new InvalidDataException(
                "The archived layout key is invalid.");
        }

        var path = Path.GetFullPath(Path.Combine(_archivedReleasesRoot, key));
        if (!IsSameOrChildPath(path, _archivedReleasesRoot))
        {
            throw new InvalidDataException(
                "The archived layout key path is invalid.");
        }

        return path;
    }

    private string ArchivedReleaseDirectory(string key, string version)
    {
        if (!WeddingLayoutVersion.IsValid(version))
        {
            throw new InvalidDataException(
                "The archived layout release version is invalid.");
        }

        var path = Path.GetFullPath(
            Path.Combine(ArchivedReleaseKeyDirectory(key), version));
        if (!IsSameOrChildPath(path, _archivedReleasesRoot))
        {
            throw new InvalidDataException(
                "The archived layout release path is invalid.");
        }

        return path;
    }

    private string PurgedArchiveDirectory(string submissionId)
    {
        if (!IsSubmissionId(submissionId))
        {
            throw new InvalidDataException(
                "The purged archive submission id is invalid.");
        }

        var path = Path.GetFullPath(
            Path.Combine(_purgedArchivesRoot, submissionId));
        if (!IsSameOrChildPath(path, _purgedArchivesRoot))
        {
            throw new InvalidDataException(
                "The purged archive history path is invalid.");
        }

        return path;
    }

    private string ReleaseDirectory(string key, string version)
    {
        if (!WeddingLayoutKeys.IsValid(key) || !WeddingLayoutVersion.IsValid(version))
        {
            throw new InvalidDataException("The layout release identity is invalid.");
        }

        var path = Path.GetFullPath(Path.Combine(_releasesRoot, key, version));
        if (!IsSameOrChildPath(path, _releasesRoot))
        {
            throw new InvalidDataException("The layout release path is invalid.");
        }

        return path;
    }

    private string ActivePointerPath(string key)
    {
        if (!WeddingLayoutKeys.IsValid(key))
        {
            throw new InvalidDataException("The layout key is invalid.");
        }

        return Path.Combine(_activeRoot, key + ".json");
    }

    private string DefinitionPolicyPath(string key)
    {
        if (!WeddingLayoutKeys.IsValid(key))
        {
            throw new InvalidDataException("The layout definition key is invalid.");
        }

        return Path.Combine(_policiesRoot, key + ".json");
    }

    private static async Task<byte[]> ReadBoundedStreamAsync(
        Stream source,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (!source.CanRead)
        {
            throw new InvalidDataException("The layout package stream is not readable.");
        }

        if (source.CanSeek && source.Length - source.Position > maximumBytes)
        {
            throw new InvalidDataException(
                $"The layout package exceeds the {maximumBytes}-byte limit.");
        }

        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                return destination.ToArray();
            }

            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidDataException(
                    $"The layout package exceeds the {maximumBytes}-byte limit.");
            }

            destination.Write(buffer, 0, read);
        }
    }

    private static async Task<T> ReadJsonFileAsync<T>(
        string path,
        JsonSerializerOptions options,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        var bytes = await ReadBoundedFileAsync(path, maximumBytes, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            return JsonSerializer.Deserialize<T>(bytes, options)
                ?? throw new InvalidDataException(
                    $"JSON file '{Path.GetFileName(path)}' is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"JSON file '{Path.GetFileName(path)}' is invalid: {ex.Message}",
                ex);
        }
    }

    private static async Task<byte[]> ReadBoundedFileAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Required layout workflow file '{Path.GetFileName(path)}' was not found.",
                path);
        }

        RejectReparsePoint(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException(
                $"File '{Path.GetFileName(path)}' exceeds the {maximumBytes}-byte limit.");
        }

        return await ReadBoundedStreamAsync(stream, maximumBytes, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task WriteJsonFileAsync<T>(
        string path,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken)
            .ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task AtomicReplaceJsonAsync<T>(
        string path,
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken,
        Func<string, string>? temporaryPathFactory = null)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidDataException("The destination directory is unavailable.");
        EnsureDirectory(directory);
        var temporaryPath = temporaryPathFactory is null
            ? Path.Combine(
                directory,
                $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp")
            : temporaryPathFactory(path);
        if (!string.Equals(
                Path.GetDirectoryName(Path.GetFullPath(temporaryPath)),
                Path.GetFullPath(directory),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The atomic replacement temporary file must share its destination directory.");
        }

        try
        {
            await WriteJsonFileAsync(
                    temporaryPath,
                    value,
                    options,
                    cancellationToken)
                .ConfigureAwait(false);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string ValidateReviewText(
        string? value,
        string parameterName,
        int minimumLength,
        int maximumLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < minimumLength
            || text.Length > maximumLength
            || text.Any(char.IsControl)
            || text.Contains('<')
            || text.Contains('>'))
        {
            throw new ArgumentException(
                $"A plain-text value between {minimumLength} and {maximumLength} characters is required.",
                parameterName);
        }

        return text;
    }

    private static string NormalizeActorLabel(
        string? displayName,
        string? email,
        string userId)
    {
        var candidate = string.IsNullOrWhiteSpace(displayName)
            ? email
            : displayName;
        candidate = string.IsNullOrWhiteSpace(candidate) ? userId : candidate;
        var safe = new string(candidate
            .Where(character =>
                !char.IsControl(character)
                && character is not '<' and not '>')
            .Take(120)
            .ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Authenticated user" : safe.Trim();
    }

    private void NotifyChanged(
        WeddingLayoutSubmissionRecord submission,
        WeddingLayoutSubmissionChangeKind kind) =>
        NotifyChanged(new WeddingLayoutSubmissionsChangedEventArgs(
            kind,
            submission.TenantSlug,
            submission.SubmissionId,
            submission.LayoutKey,
            submission.LayoutVersion));

    private void NotifyChanged(
        WeddingLayoutArchiveRecord archive,
        WeddingLayoutSubmissionChangeKind kind) =>
        NotifyChanged(new WeddingLayoutSubmissionsChangedEventArgs(
            kind,
            archive.TenantSlug,
            archive.SubmissionId,
            archive.LayoutKey,
            archive.LayoutVersion));

    private void NotifyChanged(WeddingLayoutSubmissionsChangedEventArgs args)
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        // A disconnected UI observer must never roll back or fail a durable
        // submission mutation. Each circuit re-queries storage independently.
        foreach (EventHandler<WeddingLayoutSubmissionsChangedEventArgs> handler
                 in handlers.GetInvocationList())
        {
            try
            {
                handler(this, args);
            }
            catch
            {
                // Notifications are cache/view invalidation hints only.
            }
        }
    }

    private static bool IsSubmissionId(string? value) =>
        value is { Length: 32 }
        && value.All(character =>
            character is >= '0' and <= '9'
            or >= 'a' and <= 'f');

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException(
                $"Reparse points are not allowed in layout workflow storage: '{path}'.");
        }
    }

    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)
                && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch
        {
            // Staging cleanup is best effort; storage validation never trusts its contents.
        }
    }

    private static bool IsSameOrChildPath(string candidate, string root)
    {
        var relative = Path.GetRelativePath(
            Path.GetFullPath(root),
            Path.GetFullPath(candidate));
        return relative == "."
            || (!relative.Equals("..", StringComparison.Ordinal)
                && !relative.StartsWith(
                    $"..{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal)
                && !Path.IsPathRooted(relative));
    }

    private sealed record TenantLayoutReference(
        string TenantSlug,
        bool IsPinnedToTargetVersion,
        bool FollowsActiveVersion);

    private sealed record ArchivedLayoutLocation(
        WeddingLayoutArchiveRecord Record,
        string DirectoryPath,
        bool IsPurgedHistory);

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}

public sealed class LayoutPackageValidationException : Exception
{
    public LayoutPackageValidationException(
        IReadOnlyList<LayoutValidationError> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public IReadOnlyList<LayoutValidationError> Errors { get; }

    private static string BuildMessage(IReadOnlyList<LayoutValidationError>? errors)
    {
        if (errors is null || errors.Count == 0)
        {
            return "The layout package is invalid.";
        }

        return "The layout package is invalid: "
            + string.Join(
                "; ",
                errors.Take(10).Select(error =>
                    $"{error.Path} [{error.Code}] {error.Message}"));
    }
}
