using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class JsonProjectStore : IProjectStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonProjectStore(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

    private string Dir(string slug) => Path.Combine(_root, slug, "projects");
    private string Path_(string slug, string id) => Path.Combine(Dir(slug), $"{id}.json");

    public async Task<List<ProjectItem>> GetAllAsync(string slug)
    {
        var dir = Dir(slug);
        if (!Directory.Exists(dir)) return [];
        var list = new List<ProjectItem>();
        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(f);
                var item = JsonSerializer.Deserialize<ProjectItem>(json, _json);
                if (item != null) list.Add(item);
            }
            catch { }
        }
        return [.. list.OrderBy(p => p.SortOrder == 0 ? int.MaxValue : p.SortOrder)
                       .ThenByDescending(p => p.Year)];
    }

    public async Task<ProjectItem?> GetAsync(string slug, string projectId)
    {
        var path = Path_(slug, projectId);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ProjectItem>(json, _json);
    }

    public async Task SaveAsync(string slug, ProjectItem item)
    {
        Directory.CreateDirectory(Dir(slug));
        var json = JsonSerializer.Serialize(item, _json);
        await File.WriteAllTextAsync(Path_(slug, item.Id), json);
    }

    public Task DeleteAsync(string slug, string projectId)
    {
        var path = Path_(slug, projectId);
        if (File.Exists(path)) File.Delete(path);
        var mediaDir = System.IO.Path.Combine(_root, slug, "media", projectId);
        if (Directory.Exists(mediaDir)) Directory.Delete(mediaDir, true);
        return Task.CompletedTask;
    }
}
