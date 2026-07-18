using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>\brief Account Override -> Tier Policy -> System Default 순서로 미디어 정책을 계산합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media quota policy resolver functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaQuotaPolicyResolver : IMediaQuotaPolicyResolver
{
    /// <summary>
    /// \if KO
    /// <para>global Settings 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the global settings value.</para>
    /// \endif
    /// </summary>
    private readonly IGlobalSettingsStore _globalSettings;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="MediaQuotaPolicyResolver"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="MediaQuotaPolicyResolver"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="globalSettings">
    /// \if KO
    /// <para>global Settings에 사용할 <c>IGlobalSettingsStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IGlobalSettingsStore</c> value used for global settings.</para>
    /// \endif
    /// </param>
    public MediaQuotaPolicyResolver(IGlobalSettingsStore globalSettings)
    {
        _globalSettings = globalSettings;
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Async 작업을 수행합니다.</para>
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
    public async Task<EffectiveMediaPolicy> ResolveAsync(TenantConfig tenant, CancellationToken ct = default)
    {
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);
        settings.Normalize();

        var tier = tenant.HasPremiumPlan
            ? settings.MediaPolicy.PremiumTier
            : settings.MediaPolicy.FreeTier;

        tier ??= settings.MediaPolicy.SystemDefault;
        tier.Normalize(settings.MediaPolicy.SystemDefault);

        return EffectiveMediaPolicy.From(
            tier,
            tenant.MediaPolicyOverride,
            tenant.MaxVideoSizeMb,
            tenant.MaxVideoCount);
    }
}
