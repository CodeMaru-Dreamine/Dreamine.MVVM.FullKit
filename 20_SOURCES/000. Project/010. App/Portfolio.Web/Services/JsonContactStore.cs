using System.IO;
using System.Text.Json;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class JsonContactStore : IContactStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public JsonContactStore(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

    private string Dir(string slug) => Path.Combine(_root, slug, "contacts");
    private string Path_(string slug, string id) => Path.Combine(Dir(slug), $"{id}.json");

    public async Task<List<ContactMessage>> GetAllAsync(string slug)
    {
        var dir = Dir(slug);
        if (!Directory.Exists(dir)) return [];
        var list = new List<ContactMessage>();
        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(f);
                var msg = JsonSerializer.Deserialize<ContactMessage>(json);
                if (msg != null) list.Add(msg);
            }
            catch { }
        }
        return [.. list.OrderByDescending(m => m.SentAt)];
    }

    public async Task SaveAsync(string slug, ContactMessage msg)
    {
        Directory.CreateDirectory(Dir(slug));
        var json = JsonSerializer.Serialize(msg, _json);
        await File.WriteAllTextAsync(Path_(slug, msg.Id), json);
    }

    public Task DeleteAsync(string slug, string msgId)
    {
        var path = Path_(slug, msgId);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task MarkReadAsync(string slug, string msgId)
    {
        var path = Path_(slug, msgId);
        if (!File.Exists(path)) return;
        var json = await File.ReadAllTextAsync(path);
        var msg = JsonSerializer.Deserialize<ContactMessage>(json);
        if (msg is null) return;
        msg.IsRead = true;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(msg, _json));
    }
}
