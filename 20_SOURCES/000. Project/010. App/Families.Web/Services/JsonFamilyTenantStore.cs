using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

public sealed class JsonFamilyTenantStore : IFamilyTenantStore
{
    private readonly string _dataRoot;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonFamilyTenantStore(FamilyOptions opts)
    {
        _dataRoot = opts.ResolvedDataPath;
        Directory.CreateDirectory(_dataRoot);
    }

    public string GetTenantDataPath(string slug) =>
        Path.Combine(_dataRoot, Sanitize(slug));

    public async Task<FamilyConfig?> GetAsync(string slug, CancellationToken ct = default)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<FamilyConfig>(fs, _jsonOpts, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<FamilyConfig>> GetAllAsync(CancellationToken ct = default)
    {
        var list = new List<FamilyConfig>();
        if (!Directory.Exists(_dataRoot)) return list;

        foreach (var dir in Directory.GetDirectories(_dataRoot))
        {
            var cfg = Path.Combine(dir, "config.json");
            if (!File.Exists(cfg)) continue;

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var fs = File.OpenRead(cfg);
                var t = await JsonSerializer.DeserializeAsync<FamilyConfig>(fs, _jsonOpts, ct).ConfigureAwait(false);
                if (t != null) list.Add(t);
            }
            finally { _gate.Release(); }
        }
        return list;
    }

    public async Task SaveAsync(FamilyConfig config, CancellationToken ct = default)
    {
        var dir = GetTenantDataPath(config.Slug);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "posts"));
        Directory.CreateDirectory(Path.Combine(dir, "albums"));
        Directory.CreateDirectory(Path.Combine(dir, "reactions"));
        Directory.CreateDirectory(Path.Combine(dir, "media"));

        var tmp = ConfigPath(config.Slug) + ".tmp";
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, config, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, ConfigPath(config.Slug), overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
    }

    public Task<bool> ExistsAsync(string slug, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(ConfigPath(slug)));

    public Task DeleteAsync(string slug, CancellationToken ct = default)
    {
        var dir = GetTenantDataPath(slug);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    private string ConfigPath(string slug) =>
        Path.Combine(GetTenantDataPath(slug), "config.json");

    private static string Sanitize(string slug) =>
        string.Concat(slug.ToLowerInvariant().Split(Path.GetInvalidFileNameChars()));
}
