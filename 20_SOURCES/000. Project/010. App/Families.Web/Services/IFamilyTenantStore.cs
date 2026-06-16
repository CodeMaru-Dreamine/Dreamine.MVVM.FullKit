using FamiliesApp.Models;

namespace FamiliesApp.Services;

public interface IFamilyTenantStore
{
    Task<FamilyConfig?> GetAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyConfig>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(FamilyConfig config, CancellationToken ct = default);
    Task<bool> ExistsAsync(string slug, CancellationToken ct = default);
    Task DeleteAsync(string slug, CancellationToken ct = default);
    string GetTenantDataPath(string slug);
}
