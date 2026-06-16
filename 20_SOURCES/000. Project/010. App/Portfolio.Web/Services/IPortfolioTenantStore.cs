using PortfolioApp.Models;

namespace PortfolioApp.Services;

public interface IPortfolioTenantStore
{
    Task<List<PortfolioConfig>> GetAllAsync();
    Task<PortfolioConfig?> GetAsync(string slug);
    Task SaveAsync(PortfolioConfig config);
    Task DeleteAsync(string slug);
}
