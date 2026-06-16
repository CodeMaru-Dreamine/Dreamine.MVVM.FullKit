using PortfolioApp.Models;

namespace PortfolioApp.Services;

public interface IResumeStore
{
    Task<ResumeInfo> GetAsync(string slug);
    Task SaveAsync(string slug, ResumeInfo resume);
}
