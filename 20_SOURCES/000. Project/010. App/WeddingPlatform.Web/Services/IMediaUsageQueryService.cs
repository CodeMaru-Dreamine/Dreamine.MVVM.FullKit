using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \brief 테넌트 파일 시스템에서 이미지/영상 사용량을 조회합니다.
/// </summary>
public interface IMediaUsageQueryService
{
    /// <summary>
    /// \brief 단일 테넌트의 미디어 사용량을 조회합니다.
    /// </summary>
    Task<TenantMediaUsageSummary> GetTenantUsageAsync(TenantConfig tenant, CancellationToken ct = default);

    /// <summary>
    /// \brief 여러 테넌트의 미디어 사용량을 조회합니다.
    /// </summary>
    Task<IReadOnlyDictionary<string, TenantMediaUsageSummary>> GetTenantUsageAsync(IEnumerable<TenantConfig> tenants, CancellationToken ct = default);
}
