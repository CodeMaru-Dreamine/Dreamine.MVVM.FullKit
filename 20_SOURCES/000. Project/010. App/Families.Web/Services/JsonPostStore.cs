using System.IO;
using System.Text.Json;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

public sealed class JsonPostStore : IPostStore
{
    private readonly IFamilyTenantStore _tenants;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonPostStore(IFamilyTenantStore tenants) => _tenants = tenants;

    private string PostsDir(string slug) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "posts");

    private string PostPath(string slug, string postId) =>
        Path.Combine(PostsDir(slug), $"{Sanitize(postId)}.json");

    public async Task<PostEntry?> GetAsync(string slug, string postId, CancellationToken ct = default)
    {
        var path = PostPath(slug, postId);
        if (!File.Exists(path)) return null;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<PostEntry>(fs, _jsonOpts, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<PostEntry>> GetAllAsync(string slug, CancellationToken ct = default)
    {
        var dir = PostsDir(slug);
        if (!Directory.Exists(dir)) return [];

        var list = new List<PostEntry>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var fs = File.OpenRead(file);
                var p = await JsonSerializer.DeserializeAsync<PostEntry>(fs, _jsonOpts, ct).ConfigureAwait(false);
                if (p != null) list.Add(p);
            }
            finally { _gate.Release(); }
        }
        return list.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.PostedAt).ToList();
    }

    public async Task<IReadOnlyList<PostEntry>> GetByAlbumAsync(string slug, string albumId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(slug, ct).ConfigureAwait(false);
        return all.Where(p => p.AlbumId == albumId).ToList();
    }

    public async Task SaveAsync(string slug, PostEntry post, CancellationToken ct = default)
    {
        var dir = PostsDir(slug);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(_tenants.GetTenantDataPath(slug), "media", post.Id));

        var path = PostPath(slug, post.Id);
        var tmp = path + ".tmp";

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, post, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
    }

    public Task DeleteAsync(string slug, string postId, CancellationToken ct = default)
    {
        var path = PostPath(slug, postId);
        if (File.Exists(path)) File.Delete(path);

        var mediaDir = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId);
        if (Directory.Exists(mediaDir)) Directory.Delete(mediaDir, recursive: true);

        return Task.CompletedTask;
    }

    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
