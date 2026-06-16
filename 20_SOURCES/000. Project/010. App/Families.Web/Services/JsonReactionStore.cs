using System.IO;
using System.Text.Json;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

public sealed class JsonReactionStore : IReactionStore
{
    private readonly IFamilyTenantStore _tenants;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonReactionStore(IFamilyTenantStore tenants) => _tenants = tenants;

    private string ReactionsDir(string slug) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "reactions");

    private string ReactionPath(string slug, string postId) =>
        Path.Combine(ReactionsDir(slug), $"{Sanitize(postId)}.json");

    public async Task<ReactionSummary> GetAsync(string slug, string postId, CancellationToken ct = default)
    {
        var path = ReactionPath(slug, postId);
        if (!File.Exists(path)) return new ReactionSummary { PostId = postId };

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ReactionSummary>(fs, _jsonOpts, ct).ConfigureAwait(false)
                   ?? new ReactionSummary { PostId = postId };
        }
        finally { _gate.Release(); }
    }

    public async Task AddReactionAsync(string slug, string postId, string emoji, CancellationToken ct = default)
    {
        var summary = await GetAsync(slug, postId, ct).ConfigureAwait(false);
        summary.EmojiCounts.TryGetValue(emoji, out int current);
        summary.EmojiCounts[emoji] = current + 1;
        await SaveAsync(slug, summary, ct).ConfigureAwait(false);
    }

    public async Task AddCommentAsync(string slug, string postId, CommentEntry comment, CancellationToken ct = default)
    {
        var summary = await GetAsync(slug, postId, ct).ConfigureAwait(false);
        comment.PostId = postId;
        summary.Comments.Add(comment);
        await SaveAsync(slug, summary, ct).ConfigureAwait(false);
    }

    public async Task DeleteCommentAsync(string slug, string postId, string commentId, CancellationToken ct = default)
    {
        var summary = await GetAsync(slug, postId, ct).ConfigureAwait(false);
        summary.Comments.RemoveAll(c => c.Id == commentId);
        await SaveAsync(slug, summary, ct).ConfigureAwait(false);
    }

    private async Task SaveAsync(string slug, ReactionSummary summary, CancellationToken ct = default)
    {
        var dir = ReactionsDir(slug);
        Directory.CreateDirectory(dir);

        var path = ReactionPath(slug, summary.PostId);
        var tmp = path + ".tmp";

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, summary, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
    }

    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
