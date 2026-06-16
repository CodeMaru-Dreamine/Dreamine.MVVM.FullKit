using System.IO;
using System.Text.Json;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

public sealed class JsonAlbumStore : IAlbumStore
{
    private readonly IFamilyTenantStore _tenants;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonAlbumStore(IFamilyTenantStore tenants) => _tenants = tenants;

    private string AlbumsDir(string slug) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "albums");

    private string AlbumPath(string slug, string albumId) =>
        Path.Combine(AlbumsDir(slug), $"{Sanitize(albumId)}.json");

    public async Task<IReadOnlyList<AlbumInfo>> GetAllAsync(string slug, CancellationToken ct = default)
    {
        var dir = AlbumsDir(slug);
        if (!Directory.Exists(dir)) return [];

        var list = new List<AlbumInfo>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var fs = File.OpenRead(file);
                var a = await JsonSerializer.DeserializeAsync<AlbumInfo>(fs, _jsonOpts, ct).ConfigureAwait(false);
                if (a != null) list.Add(a);
            }
            finally { _gate.Release(); }
        }
        return list.OrderBy(a => a.SortOrder == 0 ? int.MaxValue : a.SortOrder)
                   .ThenBy(a => a.CreatedAt)
                   .ToList();
    }

    public async Task<AlbumInfo?> GetAsync(string slug, string albumId, CancellationToken ct = default)
    {
        var path = AlbumPath(slug, albumId);
        if (!File.Exists(path)) return null;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AlbumInfo>(fs, _jsonOpts, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(string slug, AlbumInfo album, CancellationToken ct = default)
    {
        var dir = AlbumsDir(slug);
        Directory.CreateDirectory(dir);

        var path = AlbumPath(slug, album.Id);
        var tmp = path + ".tmp";

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, album, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
    }

    public Task DeleteAsync(string slug, string albumId, CancellationToken ct = default)
    {
        var path = AlbumPath(slug, albumId);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
