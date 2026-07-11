using Microsoft.AspNetCore.Components.Forms;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

public interface IPhotoService
{
    Task<IReadOnlyList<PhotoInfo>> GetGalleryAsync(string slug, CancellationToken ct = default);
    Task<string> UploadAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadMusicAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadThankYouOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task DeleteVideoAsync(string slug, string fileName, CancellationToken ct = default);
    Task DeleteAsync(string slug, string fileName, CancellationToken ct = default);
    string GetPhotoUrl(string slug, string fileName);
    string GetThumbUrl(string slug, string fileName);
    string GetHeroUrl(string slug, string fileName);
    string GetRoadMapUrl(string slug, string fileName);
    string GetMusicUrl(string slug, string fileName);
    string GetVideoUrl(string slug, string fileName);
}
