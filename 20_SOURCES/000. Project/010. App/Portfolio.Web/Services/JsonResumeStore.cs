using System.IO;
using System.Text.Json;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

public class JsonResumeStore : IResumeStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public JsonResumeStore(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

    private string Path_(string slug) => Path.Combine(_root, slug, "resume.json");

    public async Task<ResumeInfo> GetAsync(string slug)
    {
        var path = Path_(slug);
        if (!File.Exists(path)) return new ResumeInfo();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ResumeInfo>(json) ?? new ResumeInfo();
    }

    public async Task SaveAsync(string slug, ResumeInfo resume)
    {
        var json = JsonSerializer.Serialize(resume, _json);
        await File.WriteAllTextAsync(Path_(slug), json);
    }
}
