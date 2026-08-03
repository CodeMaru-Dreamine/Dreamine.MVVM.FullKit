using System.IO;
using Dreamine.AppSecurity;
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
    private static readonly string[] AllowedImageExts =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic", ".heif", ".avif"];

    // Social preview crawlers do not consistently decode camera-native HEIC/HEIF/AVIF.
    // Keep the public OG surface to broadly supported web image formats.
    private static readonly string[] AllowedOgImageExts = [".jpg", ".png", ".webp"];
    /// <summary>
    /// \if KO
    /// <para>Allowed Video Exts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the allowed video exts value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] AllowedVideoExts = [".mp4", ".webm", ".mov", ".m4v", ".3gp", ".3g2"];

    private static readonly Dictionary<string, string> ExtensionsByContentType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
            ["image/heic"] = ".heic",
            ["image/heif"] = ".heif",
            ["image/avif"] = ".avif",
            ["video/mp4"] = ".mp4",
            ["video/webm"] = ".webm",
            ["video/quicktime"] = ".mov",
            ["video/x-m4v"] = ".m4v",
            ["video/3gpp"] = ".3gp",
            ["video/3gpp2"] = ".3g2"
        };

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
        var (imageLimit, videoLimit) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        var descriptor = ResolveMediaDescriptor(file.Name, file.ContentType, allowVideo: true);
        var limit = descriptor.IsVideo ? videoLimit : imageLimit;
        await using var source = file.OpenReadStream(limit, ct);
        var stored = await StorePostMediaAsync(
            slug, postId, source, file.Name, file.ContentType, file.Size, ct).ConfigureAwait(false);
        return stored.FileName;
    }

    /// <inheritdoc />
    public async Task<StoredMediaFile> UploadPostMediaAsync(
        string slug,
        string postId,
        Stream content,
        string originalFileName,
        string contentType,
        long size,
        CancellationToken ct = default) =>
        await StorePostMediaAsync(
            slug, postId, content, originalFileName, contentType, size, ct).ConfigureAwait(false);

    private async Task<StoredMediaFile> StorePostMediaAsync(
        string slug,
        string postId,
        Stream content,
        string originalFileName,
        string contentType,
        long size,
        CancellationToken ct)
    {
        var descriptor = ResolveMediaDescriptor(originalFileName, contentType, allowVideo: true);
        var (imageLimit, videoLimit) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        var limit = descriptor.IsVideo ? videoLimit : imageLimit;
        ValidateFileSize(size, limit, descriptor.IsVideo ? "동영상" : "이미지");

        var tenantRoot = _tenants.GetTenantDataPath(slug);
        var mediaRoot = StoragePathGuard.ResolveUnderRoot(tenantRoot, "media");
        var mediaDir = StoragePathGuard.ResolveIdentifierDirectory(
            mediaRoot, postId, nameof(postId));
        Directory.CreateDirectory(mediaDir);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{descriptor.Extension}";
        var destination = StoragePathGuard.ResolveUnderRoot(mediaDir, fileName);
        await WriteAtomicallyAsync(content, destination, limit, ct).ConfigureAwait(false);
        return new StoredMediaFile(fileName, descriptor.IsVideo, size);
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
        var (imageLimit, _) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        await using var source = file.OpenReadStream(imageLimit, ct);
        var stored = await StoreCoverAsync(
            slug, source, file.Name, file.ContentType, file.Size, ct).ConfigureAwait(false);
        return stored.FileName;
    }

    /// <inheritdoc />
    public async Task<StoredMediaFile> UploadCoverAsync(
        string slug,
        Stream content,
        string originalFileName,
        string contentType,
        long size,
        CancellationToken ct = default) =>
        await StoreCoverAsync(slug, content, originalFileName, contentType, size, ct)
            .ConfigureAwait(false);

    private async Task<StoredMediaFile> StoreCoverAsync(
        string slug,
        Stream content,
        string originalFileName,
        string contentType,
        long size,
        CancellationToken ct)
    {
        var descriptor = ResolveMediaDescriptor(originalFileName, contentType, allowVideo: false);
        var (imageLimit, _) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        ValidateFileSize(size, imageLimit, "커버 이미지");

        var tenantRoot = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(tenantRoot);
        var fileName = $"cover_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{descriptor.Extension}";
        var destination = StoragePathGuard.ResolveUnderRoot(tenantRoot, fileName);
        await WriteAtomicallyAsync(content, destination, imageLimit, ct).ConfigureAwait(false);

        string? previousFileName = null;
        try
        {
            var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                         ?? new FamilyConfig { Slug = slug };
            previousFileName = config.CoverImageFileName;
            config.CoverImageFileName = fileName;
            await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        }
        catch
        {
            File.Delete(destination);
            throw;
        }

        try
        {
            DeletePreviousCover(tenantRoot, previousFileName, fileName);
        }
        catch (IOException)
        {
            // The new cover is already committed. Stale cover cleanup is best-effort.
        }
        return new StoredMediaFile(fileName, IsVideo: false, SizeBytes: size);
    }

    /// <inheritdoc />
    public async Task<StoredMediaFile> UploadOgImageAsync(
        string slug,
        Stream content,
        string originalFileName,
        string contentType,
        long size,
        CancellationToken ct = default)
    {
        var descriptor = ResolveMediaDescriptor(originalFileName, contentType, allowVideo: false);
        if (!AllowedOgImageExts.Contains(descriptor.Extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Open Graph images must be JPEG, PNG, or WebP files.");
        }
        var (imageLimit, _) = await GetLimitsAsync(slug, ct).ConfigureAwait(false);
        ValidateFileSize(size, imageLimit, "OG 이미지");

        var tenantRoot = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(tenantRoot);
        var fileName = $"og_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{descriptor.Extension}";
        var destination = StoragePathGuard.ResolveUnderRoot(tenantRoot, fileName);
        await WriteAtomicallyAsync(content, destination, imageLimit, ct).ConfigureAwait(false);

        string? previousFileName = null;
        try
        {
            var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                         ?? throw new InvalidOperationException("The family album does not exist.");
            previousFileName = config.OgImageFileName;
            config.OgImageFileName = fileName;
            await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        }
        catch
        {
            File.Delete(destination);
            throw;
        }

        try
        {
            DeletePreviousOgImage(tenantRoot, previousFileName, fileName);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or InvalidOperationException
                or NotSupportedException)
        {
            // The newly selected OG image is already committed. Stale OG cleanup is best-effort.
        }

        return new StoredMediaFile(fileName, IsVideo: false, SizeBytes: size);
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

    private static (string Extension, bool IsVideo) ResolveMediaDescriptor(
        string originalFileName,
        string contentType,
        bool allowVideo)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var extensionIsImage = AllowedImageExts.Contains(extension, StringComparer.OrdinalIgnoreCase);
        var extensionIsVideo = AllowedVideoExts.Contains(extension, StringComparer.OrdinalIgnoreCase);
        if (!extensionIsImage && !extensionIsVideo)
        {
            if (!ExtensionsByContentType.TryGetValue(contentType ?? string.Empty, out extension))
            {
                throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {extension}");
            }
        }
        else if (ExtensionsByContentType.TryGetValue(contentType ?? string.Empty, out var contentExtension))
        {
            var contentIsVideo = AllowedVideoExts.Contains(contentExtension, StringComparer.OrdinalIgnoreCase);
            if (extensionIsVideo != contentIsVideo)
            {
                throw new InvalidOperationException("파일 확장자와 콘텐츠 형식이 일치하지 않습니다.");
            }

            // Prefer the MIME-derived extension so HEIC/AVIF data isn't served as JPEG merely
            // because a mobile browser supplied a misleading camera-roll file name.
            extension = contentExtension;
        }

        var isImage = AllowedImageExts.Contains(extension, StringComparer.OrdinalIgnoreCase);
        var isVideo = AllowedVideoExts.Contains(extension, StringComparer.OrdinalIgnoreCase);
        if (!isImage && !isVideo || isVideo && !allowVideo)
        {
            throw new InvalidOperationException("허용되지 않는 미디어 파일입니다.");
        }

        if (!string.IsNullOrWhiteSpace(contentType)
            && !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            var expectedPrefix = isVideo ? "video/" : "image/";
            if (!contentType.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("파일 확장자와 콘텐츠 형식이 일치하지 않습니다.");
            }
        }

        return (extension, isVideo);
    }

    private static void ValidateFileSize(long size, long limit, string mediaLabel)
    {
        if (size <= 0)
        {
            throw new InvalidOperationException("빈 파일은 업로드할 수 없습니다.");
        }

        if (size > limit)
        {
            throw new InvalidOperationException($"{mediaLabel}는 {FormatLimit(limit)} 이하여야 합니다.");
        }
    }

    private static async Task WriteAtomicallyAsync(
        Stream source,
        string destination,
        long limit,
        CancellationToken ct)
    {
        var temporaryPath = $"{destination}.{Guid.NewGuid():N}.upload";
        try
        {
            await using (var target = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)
                           .ConfigureAwait(false)) > 0)
                {
                    totalBytes += read;
                    if (totalBytes > limit)
                    {
                        throw new InvalidOperationException($"파일이 허용 용량 {FormatLimit(limit)}를 초과했습니다.");
                    }

                    await target.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                }
            }

            File.Move(temporaryPath, destination);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    private static void DeletePreviousCover(
        string tenantRoot,
        string? previousFileName,
        string currentFileName)
    {
        if (string.IsNullOrWhiteSpace(previousFileName)
            || string.Equals(previousFileName, currentFileName, StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(previousFileName), previousFileName, StringComparison.Ordinal))
        {
            return;
        }

        var path = StoragePathGuard.ResolveUnderRoot(tenantRoot, previousFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void DeletePreviousOgImage(
        string tenantRoot,
        string? previousFileName,
        string currentFileName)
    {
        if (string.IsNullOrWhiteSpace(previousFileName)
            || string.Equals(previousFileName, currentFileName, StringComparison.Ordinal)
            || !previousFileName.StartsWith("og_", StringComparison.Ordinal)
            || !string.Equals(Path.GetFileName(previousFileName), previousFileName, StringComparison.Ordinal)
            || !AllowedImageExts.Contains(Path.GetExtension(previousFileName), StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var path = StoragePathGuard.ResolveUnderRoot(tenantRoot, previousFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

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
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("The file name must be a single path segment.", nameof(fileName));
        }

        var mediaRoot = StoragePathGuard.ResolveUnderRoot(_tenants.GetTenantDataPath(slug), "media");
        var postRoot = StoragePathGuard.ResolveIdentifierDirectory(mediaRoot, postId, nameof(postId));
        var path = StoragePathGuard.ResolveUnderRoot(postRoot, fileName);
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
        var mediaRoot = StoragePathGuard.ResolveUnderRoot(_tenants.GetTenantDataPath(slug), "media");
        var dir = StoragePathGuard.ResolveIdentifierDirectory(mediaRoot, postId, nameof(postId));
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
