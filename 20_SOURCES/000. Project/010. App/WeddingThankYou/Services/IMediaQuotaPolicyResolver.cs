using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \if KO
/// <para>\brief 계정 override, 등급 정책, 시스템 기본값을 병합해 최종 미디어 정책을 계산합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i media quota policy resolver functionality and related state.</para>
/// \endif
/// </summary>
public interface IMediaQuotaPolicyResolver
{
    /// <summary>
    /// \if KO
    /// <para>\brief 지정한 테넌트에 적용할 최종 미디어 정책을 계산합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve async operation.</para>
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
    /// <para>Resolve Async 작업에서 생성한 <c>Task&lt;EffectiveMediaPolicy&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;EffectiveMediaPolicy&gt;</c> result produced by the resolve async operation.</para>
    /// \endif
    /// </returns>
    Task<EffectiveMediaPolicy> ResolveAsync(TenantConfig tenant, CancellationToken ct = default);
}
