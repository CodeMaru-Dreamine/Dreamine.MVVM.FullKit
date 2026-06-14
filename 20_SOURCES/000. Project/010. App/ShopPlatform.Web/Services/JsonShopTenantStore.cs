using System.Text.Json;
using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>파일시스템 기반 테넌트 스토어. App_Data/Shops/{slug}/config.json.</summary>
public sealed class JsonShopTenantStore : IShopTenantStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public JsonShopTenantStore(ShopOptions opts) => _root = opts.ResolvedDataPath;

    public async Task<ShopConfig?> GetAsync(string slug)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ShopConfig>(stream, _json);
    }

    public async Task<IReadOnlyList<ShopConfig>> GetAllAsync()
    {
        if (!Directory.Exists(_root)) return Array.Empty<ShopConfig>();
        var list = new List<ShopConfig>();
        foreach (var dir in Directory.EnumerateDirectories(_root))
        {
            var path = Path.Combine(dir, "config.json");
            if (!File.Exists(path)) continue;
            await using var stream = File.OpenRead(path);
            var cfg = await JsonSerializer.DeserializeAsync<ShopConfig>(stream, _json);
            if (cfg != null) list.Add(cfg);
        }
        return list;
    }

    public async Task SaveAsync(ShopConfig config)
    {
        var dir = ShopDir(config.Slug);
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(config, _json);
        await File.WriteAllTextAsync(ConfigPath(config.Slug), json);
    }

    public Task DeleteAsync(string slug)
    {
        var dir = ShopDir(slug);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string slug) =>
        Task.FromResult(File.Exists(ConfigPath(slug)));

    private string ShopDir(string slug) => Path.Combine(_root, slug);
    private string ConfigPath(string slug) => Path.Combine(ShopDir(slug), "config.json");
}
