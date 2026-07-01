using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \file ITenantStore.cs
/// \brief 슬러그(테넌트)별 설정(TenantConfig) 저장소 추상화.
/// </summary>
public interface ITenantStore
{
    Task<TenantConfig?> GetAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<TenantConfig>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(TenantConfig config, CancellationToken ct = default);
    Task<bool> ExistsAsync(string slug, CancellationToken ct = default);
    Task DeleteAsync(string slug, CancellationToken ct = default);
    string GetTenantDataPath(string slug);
}
