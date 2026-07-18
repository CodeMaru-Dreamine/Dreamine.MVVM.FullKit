using System.IO;
using FamiliesApp.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>Local Media Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates local media service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LocalMediaService : IMediaService
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IFamilyTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>global Settings 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the global settings value.</para>
    /// \endif
    /// </summary>
    private readonly IGlobalSettingsStore _globalSettings;

    /// <summary>
    /// \if KO
    /// <para>Allowed Image Exts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the allowed image exts value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] AllowedImageExts = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    /// <summary>
    /// \if KO
    /// <para>Allowed Video Exts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the allowed video exts value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] AllowedVideoExts = [".mp4", ".webm", ".mov", ".m4v"];

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LocalMediaService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LocalMediaService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IFamilyTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IFamilyTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="globalSettings">
    /// \if KO
    /// <para>global Settings에 사용할 <c>IGlobalSettingsStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IGlobalSettingsStore</c> value used for global settings.</para>
    /// \endif
    /// </param>
    public LocalMediaService(IFamilyTenantStore tenants, IGlobalSettingsStore globalSettings)
    {
        _tenants = tenants;
        _globalSettings = globalSettings;
    }

    /// <summary>
    /// \if KO
    /// <para>To Bytes 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to bytes operation.</para>
    /// \endif
    /// </summary>
    /// <param name="mb">
    /// \if KO
    /// <para>mb에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for mb.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>To Bytes 작업에서 생성한 <c>long</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>long</c> result produced by the to bytes operation.</para>
    /// \endif
    /// </returns>
    private static long ToBytes(int mb) => mb <= 0 ? long.MaxValue : mb * 1024L * 1024L;

    /// <summary>
    /// \if KO
    /// <para>Limits Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the limits async value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Limits Async 작업에서 생성한 <c>Task&lt;(long ImageBytes, long VideoBytes)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(long ImageBytes, long VideoBytes)&gt;</c> result produced by the get limits async operation.</para>
    /// \endif
    /// </returns>
    private async Task<(long ImageBytes, long VideoBytes)> GetLimitsAsync(string slug, CancellationToken ct)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);

        var imageMb = config?.MaxImageSizeMb ?? settings.MaxImageSizeMb;
        var videoMb = config?.MaxVideoSizeMb ?? settings.MaxVideoSizeMb;
        return (ToBytes(imageMb), ToBytes(videoMb));
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Post Media Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload post media async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Upload Post Media Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload post media async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Upload Post Media Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the upload post media async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    public async Task<string> UploadPostMediaAsync(string slug, string postId, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        bool isVideo = Array.Exists(AllowedVideoExts, e => e == ext);
        bool isImage = Array.Exists(AllowedImageExts, e => e == ext);

        if (!isVideo && !isImage)
            throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {ext}");

        var (imageLimit, videoLimit) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        var limit = isVideo ? videoLimit : imageLimit;
        if (file.Size > limit)
            throw new InvalidOperationException(isVideo
                ? $"동영상은 {FormatLimit(videoLimit)} 이하여야 합니다."
                : $"이미지는 {FormatLimit(imageLimit)} 이하여야 합니다.");

        var mediaDir = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId);
        Directory.CreateDirectory(mediaDir);

        var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
        var destPath = Path.Combine(mediaDir, fileName);

        await using var dest = File.Create(destPath);
        await file.OpenReadStream(limit, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Cover Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload cover async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Upload Cover Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload cover async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Upload Cover Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the upload cover async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    public async Task<string> UploadCoverAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Array.Exists(AllowedImageExts, e => e == ext))
            throw new InvalidOperationException($"허용되지 않는 이미지 형식입니다: {ext}");

        var (imageLimit, _) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        if (file.Size > imageLimit)
            throw new InvalidOperationException($"커버 이미지는 {FormatLimit(imageLimit)} 이하여야 합니다.");

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"cover{ext}";
        await using var dest = File.Create(Path.Combine(dir, fileName));
        await file.OpenReadStream(imageLimit, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new Models.FamilyConfig { Slug = slug };
        config.CoverImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Format Limit 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the format limit operation.</para>
    /// \endif
    /// </summary>
    /// <param name="bytes">
    /// \if KO
    /// <para>bytes에 사용할 <c>long</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>long</c> value used for bytes.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Format Limit 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the format limit operation.</para>
    /// \endif
    /// </returns>
    private static string FormatLimit(long bytes) =>
        bytes == long.MaxValue ? "무제한" : $"{bytes / (1024 * 1024)}MB";

    /// <summary>
    /// \if KO
    /// <para>Delete Post Media Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete post media async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Post Media Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete post media async operation.</para>
    /// \endif
    /// </returns>
    public Task DeletePostMediaAsync(string slug, string postId, string fileName, CancellationToken ct = default)
    {
        var path = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId, fileName);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>Post Media Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the post media async value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Post Media Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;MediaInfo&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;MediaInfo&gt;&gt;</c> result produced by the get post media async operation.</para>
    /// \endif
    /// </returns>
    public Task<IReadOnlyList<MediaInfo>> GetPostMediaAsync(string slug, string postId, CancellationToken ct = default)
    {
        var dir = Path.Combine(_tenants.GetTenantDataPath(slug), "media", postId);
        if (!Directory.Exists(dir)) return Task.FromResult<IReadOnlyList<MediaInfo>>([]);

        var list = Directory.GetFiles(dir)
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new MediaInfo
                {
                    FileName = fi.Name,
                    Url = GetMediaUrl(slug, postId, fi.Name),
                    ThumbUrl = GetThumbUrl(slug, postId, fi.Name),
                    SizeBytes = fi.Length,
                    LastModified = fi.LastWriteTime
                };
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<MediaInfo>>(list);
    }

    /// <summary>
    /// \if KO
    /// <para>Media Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the media url value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Media Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get media url operation.</para>
    /// \endif
    /// </returns>
    public string GetMediaUrl(string slug, string postId, string fileName) =>
        $"/family-data/{slug}/media/{postId}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Thumb Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thumb url value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Thumb Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get thumb url operation.</para>
    /// \endif
    /// </returns>
    public string GetThumbUrl(string slug, string postId, string fileName) =>
        GetMediaUrl(slug, postId, fileName);

    /// <summary>
    /// \if KO
    /// <para>Cover Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the cover url value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Cover Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get cover url operation.</para>
    /// \endif
    /// </returns>
    public string GetCoverUrl(string slug, string fileName) =>
        $"/family-data/{slug}/{Uri.EscapeDataString(fileName)}";
}
