using System.IO;
using System.Text.Json;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class JsonPortfolioTenantStore : IPortfolioTenantStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public JsonPortfolioTenantStore(PortfolioOptions opts)
    {
        _root = opts.ResolvedDataPath;
        Directory.CreateDirectory(_root);
    }

    private string ConfigPath(string slug) => Path.Combine(_root, slug, "config.json");

    public async Task<List<PortfolioConfig>> GetAllAsync()
    {
        var list = new List<PortfolioConfig>();
        if (!Directory.Exists(_root)) return list;
        foreach (var dir in Directory.GetDirectories(_root))
        {
            var cfg = Path.Combine(dir, "config.json");
            if (!File.Exists(cfg)) continue;
            try
            {
                var json = await File.ReadAllTextAsync(cfg);
                var obj = JsonSerializer.Deserialize<PortfolioConfig>(json);
                if (obj != null) list.Add(obj);
            }
            catch { }
        }
        return list;
    }

    public async Task<PortfolioConfig?> GetAsync(string slug)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<PortfolioConfig>(json);
    }

    public async Task SaveAsync(PortfolioConfig config)
    {
        var dir = Path.Combine(_root, config.Slug);
        Directory.CreateDirectory(Path.Combine(dir, "projects"));
        Directory.CreateDirectory(Path.Combine(dir, "media"));
        Directory.CreateDirectory(Path.Combine(dir, "contacts"));
        var json = JsonSerializer.Serialize(config, _json);
        await File.WriteAllTextAsync(ConfigPath(config.Slug), json);
    }

    public Task DeleteAsync(string slug)
    {
        var dir = Path.Combine(_root, slug);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }
}
