using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

public interface IGuestbookStorage
{
    Task<IReadOnlyList<GuestbookEntry>> LoadAsync(string slug, CancellationToken ct = default);
    Task SaveAsync(string slug, IReadOnlyList<GuestbookEntry> entries, CancellationToken ct = default);
}
