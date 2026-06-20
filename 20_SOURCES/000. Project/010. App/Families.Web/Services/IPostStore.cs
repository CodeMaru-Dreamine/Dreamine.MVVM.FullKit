using FamiliesApp.Models;

namespace FamiliesApp.Services;

public interface IPostStore
{
    Task<PostEntry?> GetAsync(string slug, string postId, CancellationToken ct = default);
    Task<IReadOnlyList<PostEntry>> GetAllAsync(string slug, CancellationToken ct = default);
    Task<(IReadOnlyList<PostEntry> Items, int TotalCount)> GetPageAsync(string slug, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<PostEntry>> GetByAlbumAsync(string slug, string albumId, CancellationToken ct = default);
    Task<(IReadOnlyList<PostEntry> Items, int TotalCount)> GetByAlbumPageAsync(string slug, string albumId, int page, int pageSize, CancellationToken ct = default);
    Task SaveAsync(string slug, PostEntry post, CancellationToken ct = default);
    Task DeleteAsync(string slug, string postId, CancellationToken ct = default);
}
