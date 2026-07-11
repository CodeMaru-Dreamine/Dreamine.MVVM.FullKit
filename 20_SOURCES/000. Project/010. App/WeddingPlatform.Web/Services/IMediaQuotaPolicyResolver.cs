using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \brief 계정 override, 등급 정책, 시스템 기본값을 병합해 최종 미디어 정책을 계산합니다.
/// </summary>
public interface IMediaQuotaPolicyResolver
{
    /// <summary>
    /// \brief 지정한 테넌트에 적용할 최종 미디어 정책을 계산합니다.
    /// </summary>
    Task<EffectiveMediaPolicy> ResolveAsync(TenantConfig tenant, CancellationToken ct = default);
}
