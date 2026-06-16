using Microsoft.AspNetCore.Components.Forms;

namespace PortfolioApp.Services;

public interface IMediaService
{
    Task<string> SaveAsync(string slug, string projectId, IBrowserFile file);
    Task<string> SaveVideoAsync(string slug, string projectId, IBrowserFile file);
    Task DeleteAsync(string slug, string projectId, string fileName);
    Task<string> SaveProfileImageAsync(string slug, IBrowserFile file);
    string GetMediaUrl(string slug, string projectId, string fileName);
    string GetProfileImageUrl(string slug, string fileName);
}
