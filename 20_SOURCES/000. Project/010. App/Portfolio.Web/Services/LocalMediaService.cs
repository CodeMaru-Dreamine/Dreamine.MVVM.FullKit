using System.IO;
using Microsoft.AspNetCore.Components.Forms;

namespace PortfolioApp.Services;

public class LocalMediaService : IMediaService
{
    private readonly string _root;
    private const long MaxImageBytes = 20 * 1024 * 1024;

    public LocalMediaService(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

    public async Task<string> SaveAsync(string slug, string projectId, IBrowserFile file)
    {
        var dir = Path.Combine(_root, slug, "media", projectId);
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var safe = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, safe);
        await using var fs = new FileStream(path, FileMode.Create);
        await file.OpenReadStream(MaxImageBytes).CopyToAsync(fs);
        return safe;
    }

    public Task DeleteAsync(string slug, string projectId, string fileName)
    {
        var path = Path.Combine(_root, slug, "media", projectId, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<string> SaveProfileImageAsync(string slug, IBrowserFile file)
    {
        var dir = Path.Combine(_root, slug, "media", "_profile");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var safe = $"profile{ext}";
        var path = Path.Combine(dir, safe);
        await using var fs = new FileStream(path, FileMode.Create);
        await file.OpenReadStream(MaxImageBytes).CopyToAsync(fs);
        return safe;
    }

    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        $"/portfolio-data/{slug}/media/{projectId}/{Uri.EscapeDataString(fileName)}";

    public string GetProfileImageUrl(string slug, string fileName) =>
        $"/portfolio-data/{slug}/media/_profile/{Uri.EscapeDataString(fileName)}";
}
