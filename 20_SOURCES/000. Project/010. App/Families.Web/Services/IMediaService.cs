using FamiliesApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FamiliesApp.Services;

public interface IMediaService
{
    Task<string> UploadPostMediaAsync(string slug, string postId, IBrowserFile file, CancellationToken ct = default);
    Task<string> UploadCoverAsync(string slug, IBrowserFile file, CancellationToken ct = default);
    Task DeletePostMediaAsync(string slug, string postId, string fileName, CancellationToken ct = default);
    Task<IReadOnlyList<MediaInfo>> GetPostMediaAsync(string slug, string postId, CancellationToken ct = default);
    string GetMediaUrl(string slug, string postId, string fileName);
    string GetThumbUrl(string slug, string postId, string fileName);
    string GetCoverUrl(string slug, string fileName);
}
