using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

public interface ITenantStore
{
    Task<TenantConfig?> GetAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<TenantConfig>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(TenantConfig config, CancellationToken ct = default);
    Task<bool> ExistsAsync(string slug, CancellationToken ct = default);
    Task DeleteAsync(string slug, CancellationToken ct = default);
    string GetTenantDataPath(string slug);
}
