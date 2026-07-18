using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>\brief 테넌트 파일 시스템에서 이미지/영상 사용량을 조회합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i media usage query service functionality and related state.</para>
/// \endif
/// </summary>
public interface IMediaUsageQueryService
{
    /// <summary>
    /// \if KO
    /// <para>\brief 단일 테넌트의 미디어 사용량을 조회합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant usage async value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenant">
    /// \if KO
    /// <para>tenant에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for tenant.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Tenant Usage Async 작업에서 생성한 <c>Task&lt;TenantMediaUsageSummary&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;TenantMediaUsageSummary&gt;</c> result produced by the get tenant usage async operation.</para>
    /// \endif
    /// </returns>
    Task<TenantMediaUsageSummary> GetTenantUsageAsync(TenantConfig tenant, CancellationToken ct = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 여러 테넌트의 미디어 사용량을 조회합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant usage async value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IEnumerable&lt;TenantConfig&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;TenantConfig&gt;</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Tenant Usage Async 작업에서 생성한 <c>Task&lt;IReadOnlyDictionary&lt;string, TenantMediaUsageSummary&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyDictionary&lt;string, TenantMediaUsageSummary&gt;&gt;</c> result produced by the get tenant usage async operation.</para>
    /// \endif
    /// </returns>
    Task<IReadOnlyDictionary<string, TenantMediaUsageSummary>> GetTenantUsageAsync(IEnumerable<TenantConfig> tenants, CancellationToken ct = default);
}
