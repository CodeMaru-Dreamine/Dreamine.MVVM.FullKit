using FamiliesApp.Models;

namespace FamiliesApp.Services;

public interface IAlbumStore
{
    Task<IReadOnlyList<AlbumInfo>> GetAllAsync(string slug, CancellationToken ct = default);
    Task<AlbumInfo?> GetAsync(string slug, string albumId, CancellationToken ct = default);
    Task SaveAsync(string slug, AlbumInfo album, CancellationToken ct = default);
    Task DeleteAsync(string slug, string albumId, CancellationToken ct = default);
}
