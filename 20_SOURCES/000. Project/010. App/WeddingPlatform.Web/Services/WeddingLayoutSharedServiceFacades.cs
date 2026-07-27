using System.IO;
using Wedding.Common;
using Wedding.Layouts.Contracts;

namespace WeddingPlatform.Services;

/// <summary>
/// Blazor Server에 공유하는 비소유 카탈로그 파사드입니다.
/// 실제 파일 감시기와 수명은 WPF 루트 호스트가 소유합니다.
/// </summary>
internal sealed class WeddingLayoutCatalogRegistryFacade(
    FileSystemWeddingLayoutCatalogRegistry inner)
    : IWeddingLayoutCatalogRegistry
{
    private readonly FileSystemWeddingLayoutCatalogRegistry _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    public WeddingLayoutCatalog Current => _inner.Current;

    public IReadOnlyDictionary<WeddingLayoutReleaseId, WeddingLayoutPublishedPackage>
        PublishedPackages => _inner.PublishedPackages;

    public IReadOnlyDictionary<string, LayoutDefinitionPolicy> DefinitionPolicies =>
        _inner.DefinitionPolicies;

    public string LayoutPackagesRoot => _inner.LayoutPackagesRoot;

    public event EventHandler<WeddingLayoutCatalogChangedEventArgs>? Changed
    {
        add => _inner.Changed += value;
        remove => _inner.Changed -= value;
    }

    public Task<WeddingLayoutReloadResult> ReloadAsync(
        CancellationToken cancellationToken = default) =>
        _inner.ReloadAsync(cancellationToken);
}

/// <summary>
/// Blazor Server에 공유하는 비소유 제출 워크플로 파사드입니다.
/// Semaphore와 파일 서비스의 Dispose는 WPF 루트 DI 컨테이너만 수행합니다.
/// </summary>
internal sealed class WeddingLayoutSubmissionServiceFacade(
    FileSystemWeddingLayoutSubmissionService inner)
    : IWeddingLayoutSubmissionService
{
    private readonly FileSystemWeddingLayoutSubmissionService _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));

    public event EventHandler<WeddingLayoutSubmissionsChangedEventArgs>? Changed
    {
        add => _inner.Changed += value;
        remove => _inner.Changed -= value;
    }

    public Task<WeddingLayoutSubmissionRecord> SubmitAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        Stream packageJson,
        CancellationToken cancellationToken = default) =>
        _inner.SubmitAsync(tenantSlug, actor, packageJson, cancellationToken);

    public Task<WeddingLayoutSubmissionRecord> SubmitAsSuperAdminAsync(
        string tenantSlug,
        string superAdminSessionToken,
        string submittedBy,
        Stream packageJson,
        CancellationToken cancellationToken = default) =>
        _inner.SubmitAsSuperAdminAsync(
            tenantSlug,
            superAdminSessionToken,
            submittedBy,
            packageJson,
            cancellationToken);

    public Task<IReadOnlyList<WeddingLayoutSubmissionRecord>> ListOwnAsync(
        string tenantSlug,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken = default) =>
        _inner.ListOwnAsync(tenantSlug, actor, cancellationToken);

    public Task<IReadOnlyList<WeddingLayoutSubmissionRecord>> ListAllAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default) =>
        _inner.ListAllAsync(superAdminSessionToken, cancellationToken);

    public Task<LayoutPackage> GetOwnPackageAsync(
        string submissionId,
        WeddingCurrentUser actor,
        CancellationToken cancellationToken = default) =>
        _inner.GetOwnPackageAsync(submissionId, actor, cancellationToken);

    public Task<LayoutPackage> GetPackageForReviewAsync(
        string submissionId,
        string superAdminSessionToken,
        CancellationToken cancellationToken = default) =>
        _inner.GetPackageForReviewAsync(
            submissionId,
            superAdminSessionToken,
            cancellationToken);

    public Task<IReadOnlyList<LayoutDefinitionPolicy>> ListDefinitionPoliciesAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default) =>
        _inner.ListDefinitionPoliciesAsync(
            superAdminSessionToken,
            cancellationToken);

    public Task<WeddingLayoutDefinitionPolicyChangeResult> SetDefinitionTierAsync(
        string layoutKey,
        LayoutTier tier,
        string superAdminSessionToken,
        string changedBy,
        string reason,
        CancellationToken cancellationToken = default) =>
        _inner.SetDefinitionTierAsync(
            layoutKey,
            tier,
            superAdminSessionToken,
            changedBy,
            reason,
            cancellationToken);

    public Task<WeddingLayoutApprovalResult> ApproveAsync(
        string submissionId,
        string superAdminSessionToken,
        string approvedBy,
        bool activate = true,
        CancellationToken cancellationToken = default) =>
        _inner.ApproveAsync(
            submissionId,
            superAdminSessionToken,
            approvedBy,
            activate,
            cancellationToken);

    public Task<WeddingLayoutSubmissionRecord> RejectAsync(
        string submissionId,
        string superAdminSessionToken,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default) =>
        _inner.RejectAsync(
            submissionId,
            superAdminSessionToken,
            rejectedBy,
            reason,
            cancellationToken);

    public Task<WeddingLayoutArchiveResult> ArchiveAsync(
        string submissionId,
        string superAdminSessionToken,
        string archivedBy,
        string reason,
        CancellationToken cancellationToken = default) =>
        _inner.ArchiveAsync(
            submissionId,
            superAdminSessionToken,
            archivedBy,
            reason,
            cancellationToken);

    public Task<IReadOnlyList<WeddingLayoutArchiveRecord>> ListArchivedAsync(
        string superAdminSessionToken,
        CancellationToken cancellationToken = default) =>
        _inner.ListArchivedAsync(
            superAdminSessionToken,
            cancellationToken);

    public Task<WeddingLayoutPurgeResult> PurgeArchivedAsync(
        string submissionId,
        string superAdminSessionToken,
        string purgedBy,
        string reason,
        CancellationToken cancellationToken = default) =>
        _inner.PurgeArchivedAsync(
            submissionId,
            superAdminSessionToken,
            purgedBy,
            reason,
            cancellationToken);

    public Task<WeddingLayoutActivationResult> ActivateAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason = "Activate",
        CancellationToken cancellationToken = default) =>
        _inner.ActivateAsync(
            layoutKey,
            version,
            superAdminSessionToken,
            activatedBy,
            reason,
            cancellationToken);

    public Task<WeddingLayoutActivationResult> RollbackAsync(
        string layoutKey,
        string version,
        string superAdminSessionToken,
        string activatedBy,
        string reason,
        CancellationToken cancellationToken = default) =>
        _inner.RollbackAsync(
            layoutKey,
            version,
            superAdminSessionToken,
            activatedBy,
            reason,
            cancellationToken);
}
