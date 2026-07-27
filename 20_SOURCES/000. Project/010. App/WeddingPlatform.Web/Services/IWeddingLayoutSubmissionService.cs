using System.IO;
using System.Text.Json.Serialization;
using Wedding.Layouts.Contracts;

namespace WeddingPlatform.Services;

public enum WeddingLayoutSubmissionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

public enum WeddingLayoutSubmissionChangeKind
{
    Submitted = 0,
    Approved = 1,
    Rejected = 2,
    Archived = 3,
    Purged = 4,
}

/// <summary>
/// Describes a committed layout-submission workflow change. The notification is
/// process-local and is raised only after the storage mutation has completed.
/// Consumers must re-query the service instead of treating this as the data payload.
/// </summary>
public sealed class WeddingLayoutSubmissionsChangedEventArgs(
    WeddingLayoutSubmissionChangeKind kind,
    string tenantSlug,
    string submissionId,
    string layoutKey,
    string layoutVersion) : EventArgs
{
    public WeddingLayoutSubmissionChangeKind Kind { get; } = kind;

    public string TenantSlug { get; } = tenantSlug;

    public string SubmissionId { get; } = submissionId;

    public string LayoutKey { get; } = layoutKey;

    public string LayoutVersion { get; } = layoutVersion;
}

/// <summary>
/// Server-owned workflow metadata. The portable authored package is stored separately.
/// </summary>
public sealed record WeddingLayoutSubmissionRecord
{
    public const int SupportedSchemaVersion = 1;

    public int SchemaVersion { get; init; } = SupportedSchemaVersion;

    public string SubmissionId { get; init; } = "";

    public string TenantSlug { get; init; } = "";

    public string SubmittedByUserId { get; init; } = "";

    public string SubmittedByDisplayName { get; init; } = "";

    public DateTimeOffset SubmittedAtUtc { get; init; }

    public WeddingLayoutSubmissionStatus Status { get; init; }

    public string LayoutKey { get; init; } = "";

    public string LayoutVersion { get; init; } = "";

    public string LayoutLabel { get; init; } = "";

    /// <summary>
    /// Author-supplied schema-v1 value retained only so existing workflow JSON
    /// files continue to round-trip. It is never an access classification.
    /// </summary>
    [JsonPropertyName("tier")]
    public LayoutTier LegacyManifestTierSnapshot { get; init; }

    public string PackageSha256 { get; init; } = "";

    public string ReviewedBy { get; init; } = "";

    public DateTimeOffset? ReviewedAtUtc { get; init; }

    public string ReviewReason { get; init; } = "";
}

public sealed record WeddingLayoutApprovalResult(
    WeddingLayoutSubmissionRecord Submission,
    WeddingLayoutReloadResult Reload,
    bool Activated);

public sealed record WeddingLayoutActivationResult(
    WeddingLayoutActiveReleasePointer Active,
    WeddingLayoutActiveReleasePointer? Previous,
    WeddingLayoutReloadResult Reload);

public sealed record WeddingLayoutArchiveResult(
    WeddingLayoutSubmissionRecord Submission,
    bool PublishedReleaseArchived,
    bool ActivePointerRemoved,
    WeddingLayoutReloadResult? Reload);

/// <summary>
/// Minimal, server-owned retention record for an archived layout payload.
/// After permanent purge the record moves to submission-id-based history. It
/// preserves LayoutKey ownership without reserving the deleted version identity.
/// </summary>
public sealed record WeddingLayoutArchiveRecord
{
    public const int SupportedSchemaVersion = 1;

    public int SchemaVersion { get; init; } = SupportedSchemaVersion;

    public string SubmissionId { get; init; } = "";

    public WeddingLayoutSubmissionStatus OriginalStatus { get; init; }

    public string TenantSlug { get; init; } = "";

    public string LayoutKey { get; init; } = "";

    public string LayoutVersion { get; init; } = "";

    public string PackageSha256 { get; init; } = "";

    public bool PublishedReleaseArchived { get; init; }

    public bool ActivePointerArchived { get; init; }

    public string ArchivedBy { get; init; } = "";

    public DateTimeOffset ArchivedAtUtc { get; init; }

    public string Reason { get; init; } = "";

    public bool PayloadPurged { get; init; }

    public string PurgedBy { get; init; } = "";

    public DateTimeOffset? PurgedAtUtc { get; init; }

    public string PurgeReason { get; init; } = "";
}

public sealed record WeddingLayoutPurgeResult(
    WeddingLayoutArchiveRecord Archive,
    bool AlreadyPurged,
    bool DefinitionPolicyPreserved);

public sealed record WeddingLayoutDefinitionPolicyChangeResult(
    LayoutDefinitionPolicy Policy,
    WeddingLayoutReloadResult Reload,
    bool Reclassified);

/// <summary>
/// Authenticated tenant submission and signed super-administrator review workflow.
/// UI/API layers should pass the current authenticated user or super-admin session token;
/// the service independently re-checks tenant membership and token validity.
/// </summary>
public interface IWeddingLayoutSubmissionService
{
    /// <summary>
    /// Raised after a submission workflow mutation has been committed. Blazor
    /// circuits can use this to invalidate their own view state and re-query.
    /// </summary>
    event EventHandler<WeddingLayoutSubmissionsChangedEventArgs>? Changed;

    Task<WeddingLayoutSubmissionRecord> SubmitAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        Stream packageJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a package for an existing tenant through an authenticated
    /// super-administrator workflow. The stored submitter identity is
    /// server-owned and is not inferred from the selected tenant.
    /// </summary>
    Task<WeddingLayoutSubmissionRecord> SubmitAsSuperAdminAsync(
        string tenantSlug,
        string superAdminSessionToken,
        string submittedBy,
        Stream packageJson,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeddingLayoutSubmissionRecord>> ListOwnAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WeddingLayoutSubmissionRecord>> ListAllAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default);

    Task<LayoutPackage> GetOwnPackageAsync(
        string submissionId,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken = default);

    Task<LayoutPackage> GetPackageForReviewAsync(
        string submissionId,
        string superAdminSessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the server-owned LayoutKey classification policies. The built-in
    /// layouts are included as protected virtual policies.
    /// </summary>
    Task<IReadOnlyList<LayoutDefinitionPolicy>> ListDefinitionPoliciesAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly classifies or reclassifies one custom LayoutKey. This action
    /// is intentionally separate from package upload, approval and activation.
    /// </summary>
    Task<WeddingLayoutDefinitionPolicyChangeResult> SetDefinitionTierAsync(
        string layoutKey,
        LayoutTier tier,
        string superAdminSessionToken,
        string changedBy,
        string reason,
        CancellationToken cancellationToken = default);

    Task<WeddingLayoutApprovalResult> ApproveAsync(
        string submissionId,
        string superAdminSessionToken,
        string approvedBy,
        bool activate = true,
        CancellationToken cancellationToken = default);

    Task<WeddingLayoutSubmissionRecord> RejectAsync(
        string submissionId,
        string superAdminSessionToken,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a submission from the review list by moving its files into the
    /// server-owned archive. Approved releases are archived only when doing so
    /// cannot break an active pointer or a tenant pinned to that release.
    /// </summary>
    Task<WeddingLayoutArchiveResult> ArchiveAsync(
        string submissionId,
        string superAdminSessionToken,
        string archivedBy,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists recoverable archive entries. Permanently purged entries are kept
    /// only as invisible ownership/audit receipts and are not returned.
    /// </summary>
    Task<IReadOnlyList<WeddingLayoutArchiveRecord>> ListArchivedAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes the archived submission/package/release payload.
    /// This never operates on the live review list: ArchiveAsync must succeed
    /// first. A minimal LayoutKey ownership receipt and the super-administrator
    /// audit event are retained. A custom LayoutKey classification policy is
    /// retained while another live submission, published release, active pointer
    /// or recoverable archive still uses the key; it is removed only after the
    /// key's last recoverable/live artifact is permanently deleted. Built-in
    /// classification policies are application-owned and are never removed.
    /// The deleted LayoutKey@Version identity may then be submitted again by the
    /// same owner.
    /// </summary>
    Task<WeddingLayoutPurgeResult> PurgeArchivedAsync(
        string submissionId,
        string superAdminSessionToken,
        string purgedBy,
        string reason,
        CancellationToken cancellationToken = default);

    Task<WeddingLayoutActivationResult> ActivateAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason = "Activate",
        CancellationToken cancellationToken = default);

    Task<WeddingLayoutActivationResult> RollbackAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason,
        CancellationToken cancellationToken = default);
}
