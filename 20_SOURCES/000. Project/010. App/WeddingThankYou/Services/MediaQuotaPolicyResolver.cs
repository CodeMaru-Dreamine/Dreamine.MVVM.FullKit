using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \brief Account Override -> Tier Policy -> System Default 순서로 미디어 정책을 계산합니다.
/// </summary>
public sealed class MediaQuotaPolicyResolver : IMediaQuotaPolicyResolver
{
    private readonly IGlobalSettingsStore _globalSettings;

    public MediaQuotaPolicyResolver(IGlobalSettingsStore globalSettings)
    {
        _globalSettings = globalSettings;
    }

    /// <inheritdoc />
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
