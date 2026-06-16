using PortfolioApp.Models;

namespace PortfolioApp.Services;

public interface IProjectStore
{
    Task<List<ProjectItem>> GetAllAsync(string slug);
    Task<ProjectItem?> GetAsync(string slug, string projectId);
    Task SaveAsync(string slug, ProjectItem item);
    Task DeleteAsync(string slug, string projectId);
}
