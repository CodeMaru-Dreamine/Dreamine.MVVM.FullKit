using System.IO;
using Microsoft.AspNetCore.Components.Forms;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>Local Photo Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates local photo service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LocalPhotoService : IPhotoService
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly ITenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>policy Resolver 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the policy resolver value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaQuotaPolicyResolver _policyResolver;
    /// <summary>
    /// \if KO
    /// <para>image Optimization 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the image optimization value.</para>
    /// \endif
    /// </summary>
    private readonly IImageOptimizationService _imageOptimization;
    /// <summary>
    /// \if KO
    /// <para>usage Query 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the usage query value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaUsageQueryService _usageQuery;
    /// <summary>
    /// \if KO
    /// <para>Max Image Upload Bytes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the max image upload bytes value.</para>
    /// \endif
    /// </summary>
    private const long MaxImageUploadBytes = 200 * 1024 * 1024;
    /// <summary>
    /// \if KO
    /// <para>Allowed Exts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the allowed exts value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] AllowedExts = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LocalPhotoService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LocalPhotoService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>ITenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ITenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="policyResolver">
    /// \if KO
    /// <para>policy Resolver에 사용할 <c>IMediaQuotaPolicyResolver</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaQuotaPolicyResolver</c> value used for policy resolver.</para>
    /// \endif
    /// </param>
    /// <param name="imageOptimization">
    /// \if KO
    /// <para>image Optimization에 사용할 <c>IImageOptimizationService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IImageOptimizationService</c> value used for image optimization.</para>
    /// \endif
    /// </param>
    /// <param name="usageQuery">
    /// \if KO
    /// <para>usage Query에 사용할 <c>IMediaUsageQueryService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaUsageQueryService</c> value used for usage query.</para>
    /// \endif
    /// </param>
    public LocalPhotoService(
        ITenantStore tenants,
        IMediaQuotaPolicyResolver policyResolver,
        IImageOptimizationService imageOptimization,
        IMediaUsageQueryService usageQuery)
    {
        _tenants = tenants;
        _policyResolver = policyResolver;
        _imageOptimization = imageOptimization;
        _usageQuery = usageQuery;
    }

    /// <summary>
    /// \if KO
    /// <para>Gallery Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the gallery async value.</para>
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
    /// <para>Get Gallery Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;PhotoInfo&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;PhotoInfo&gt;&gt;</c> result produced by the get gallery async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<PhotoInfo>> GetGalleryAsync(string slug, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return [];

        var list = new List<PhotoInfo>();
        foreach (var fn in config.GalleryFileNames)
        {
            var full = GalleryPath(slug, fn);
            if (!File.Exists(full)) continue;
            var fi = new FileInfo(full);
            list.Add(new PhotoInfo
            {
                FileName = fn,
                Url = GetPhotoUrl(slug, fn),
                ThumbUrl = GetThumbUrl(slug, fn),
                SizeBytes = fi.Length,
                LastModified = fi.LastWriteTime
            });
        }
        return list;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload async operation.</para>
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
    /// <para>Upload Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> UploadAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: true, ct).ConfigureAwait(false);

        var baseName = Path.GetFileNameWithoutExtension(UniqueFileName(file.Name));
        var galleryDir = Path.Combine(_tenants.GetTenantDataPath(slug), "gallery");
        var thumbDir = Path.Combine(_tenants.GetTenantDataPath(slug), "thumb");
        Directory.CreateDirectory(galleryDir);
        Directory.CreateDirectory(thumbDir);

        var fileName = await SaveImageFileAsync(slug, file, galleryDir, baseName, policy, ct).ConfigureAwait(false);
        File.Copy(Path.Combine(galleryDir, fileName), Path.Combine(thumbDir, fileName), overwrite: true);

        if (!config.GalleryFileNames.Contains(fileName))
            config.GalleryFileNames.Add(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Hero Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload hero async operation.</para>
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
    /// <para>Upload Hero Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload hero async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "hero", policy, ct).ConfigureAwait(false);
        config.HeroImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Road Map Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload road map async operation.</para>
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
    /// <para>Upload Road Map Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload road map async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "roadmap", policy, ct).ConfigureAwait(false);
        config.RoadMapFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
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
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteAsync(string slug, string fileName, CancellationToken ct = default)
    {
        var galleryFile = GalleryPath(slug, fileName);
        var thumbFile = Path.Combine(_tenants.GetTenantDataPath(slug), "thumb", fileName);
        if (File.Exists(galleryFile)) File.Delete(galleryFile);
        if (File.Exists(thumbFile)) File.Delete(thumbFile);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.GalleryFileNames.Remove(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Photo Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the photo url value.</para>
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
    /// <para>Get Photo Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get photo url operation.</para>
    /// \endif
    /// </returns>
    public string GetPhotoUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/gallery/{Uri.EscapeDataString(fileName)}";

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
    public string GetThumbUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/thumb/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Hero Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero url value.</para>
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
    /// <para>Get Hero Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get hero url operation.</para>
    /// \endif
    /// </returns>
    public string GetHeroUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Road Map Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the road map url value.</para>
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
    /// <para>Get Road Map Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get road map url operation.</para>
    /// \endif
    /// </returns>
    public string GetRoadMapUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Upload Music Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload music async operation.</para>
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
    /// <para>Upload Music Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload music async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Upload Music Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the upload music async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    public async Task<string> UploadMusicAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not (".mp3" or ".ogg" or ".wav" or ".aac" or ".m4a"))
            throw new InvalidOperationException($"허용되지 않는 오디오 형식입니다: {ext}");
        if (file.Size > 30 * 1024 * 1024)
            throw new InvalidOperationException("음악 파일은 30MB 이하여야 합니다.");

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"bgmusic{ext}";
        var destPath = Path.Combine(dir, fileName);
        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(30 * 1024 * 1024, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        config.MusicFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Music Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the music url value.</para>
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
    /// <para>Get Music Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get music url operation.</para>
    /// \endif
    /// </returns>
    public string GetMusicUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Upload Og Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload og image async operation.</para>
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
    /// <para>Upload Og Image Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload og image async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> UploadOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "og", policy, ct).ConfigureAwait(false);
        config.OgImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Thank You Og Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload thank you og image async operation.</para>
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
    /// <para>Upload Thank You Og Image Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload thank you og image async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> UploadThankYouOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        ValidateImageFile(file);
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        await ValidateImageQuotaAsync(config, policy, file, countGalleryImage: false, ct).ConfigureAwait(false);

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = await SaveImageFileAsync(slug, file, dir, "og-thankyou", policy, ct).ConfigureAwait(false);
        config.ThankYouOgImageFileName = fileName;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload video async operation.</para>
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
    /// <para>Upload Video Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the upload video async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Upload Video Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the upload video async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    public async Task<string> UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext is not (".mp4" or ".webm" or ".mov" or ".m4v"))
            throw new InvalidOperationException($"허용되지 않는 동영상 형식입니다: {ext}. mp4/webm/mov 권장");

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false) ?? new TenantConfig { Slug = slug };
        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);

        var effectiveCount = policy.VideoMaxCount;
        if (effectiveCount > 0 && config.VideoFileNames.Count >= effectiveCount)
            throw new InvalidOperationException($"동영상은 최대 {effectiveCount}개까지 업로드할 수 있습니다.");

        var effectiveMb = policy.VideoMaxFileSizeMb;
        var maxBytes = policy.VideoMaxFileSizeBytes;

        if (file.Size > maxBytes)
            throw new InvalidOperationException($"동영상 파일은 {effectiveMb}MB 이하여야 합니다.");

        if (policy.VideoMaxStorageMb > 0)
        {
            var usage = await _usageQuery.GetTenantUsageAsync(config, ct).ConfigureAwait(false);
            if (usage.VideoBytes + file.Size > policy.VideoMaxStorageBytes)
            {
                throw new InvalidOperationException($"동영상 저장 용량은 최대 {policy.VideoMaxStorageMb}MB까지 사용할 수 있습니다.");
            }
        }

        var dir = _tenants.GetTenantDataPath(slug);
        Directory.CreateDirectory(dir);

        var fileName = $"video-{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
        var destPath = Path.Combine(dir, fileName);
        await using (var dest = File.Create(destPath))
            await file.OpenReadStream(maxBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);

        config.VideoFileNames.Add(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);

        return fileName;
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete video async operation.</para>
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
    /// <para>Delete Video Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete video async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteVideoAsync(string slug, string fileName, CancellationToken ct = default)
    {
        var path = Path.Combine(_tenants.GetTenantDataPath(slug), fileName);
        if (File.Exists(path)) File.Delete(path);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.VideoFileNames.Remove(fileName);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Video Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video url value.</para>
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
    /// <para>Get Video Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get video url operation.</para>
    /// \endif
    /// </returns>
    public string GetVideoUrl(string slug, string fileName) =>
        $"/wedding-data/{slug}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Gallery Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the gallery path operation.</para>
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
    /// <para>Gallery Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the gallery path operation.</para>
    /// \endif
    /// </returns>
    private string GalleryPath(string slug, string fileName) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "gallery", fileName);

    /// <summary>
    /// \if KO
    /// <para>Image Quota Async 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the image quota async value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="policy">
    /// \if KO
    /// <para>policy에 사용할 <c>EffectiveMediaPolicy</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>EffectiveMediaPolicy</c> value used for policy.</para>
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
    /// <param name="countGalleryImage">
    /// \if KO
    /// <para>count Gallery Image에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for count gallery image.</para>
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
    /// <para>Validate Image Quota Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the validate image quota async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Validate Image Quota Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the validate image quota async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    private async Task ValidateImageQuotaAsync(TenantConfig config, EffectiveMediaPolicy policy, IBrowserFile file, bool countGalleryImage, CancellationToken ct)
    {
        if (countGalleryImage && policy.ImageMaxCount > 0 && config.GalleryFileNames.Count >= policy.ImageMaxCount)
        {
            throw new InvalidOperationException($"이미지는 최대 {policy.ImageMaxCount}장까지 업로드할 수 있습니다.");
        }

        if (policy.ImageOptimizedMaxStorageMb > 0)
        {
            var usage = await _usageQuery.GetTenantUsageAsync(config, ct).ConfigureAwait(false);
            if (usage.OptimizedImageBytes + file.Size > policy.ImageOptimizedMaxStorageBytes)
            {
                throw new InvalidOperationException($"이미지 저장 용량은 최대 {policy.ImageOptimizedMaxStorageMb}MB까지 사용할 수 있습니다.");
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Image File Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves image file async data.</para>
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
    /// <param name="destinationDirectory">
    /// \if KO
    /// <para>destination Directory에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for destination directory.</para>
    /// \endif
    /// </param>
    /// <param name="baseName">
    /// \if KO
    /// <para>base Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for base name.</para>
    /// \endif
    /// </param>
    /// <param name="policy">
    /// \if KO
    /// <para>policy에 사용할 <c>EffectiveMediaPolicy</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>EffectiveMediaPolicy</c> value used for policy.</para>
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
    /// <para>Save Image File Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save image file async operation.</para>
    /// \endif
    /// </returns>
    private async Task<string> SaveImageFileAsync(string slug, IBrowserFile file, string destinationDirectory, string baseName, EffectiveMediaPolicy policy, CancellationToken ct)
    {
        var root = _tenants.GetTenantDataPath(slug);
        var originalDir = Path.Combine(root, "original");
        var originalExt = Path.GetExtension(file.Name).ToLowerInvariant();
        var tempPath = Path.Combine(destinationDirectory, $"{baseName}.upload{originalExt}");
        Directory.CreateDirectory(destinationDirectory);

        await using (var dest = File.Create(tempPath))
        {
            await file.OpenReadStream(MaxImageUploadBytes, ct).CopyToAsync(dest, ct).ConfigureAwait(false);
        }

        if (policy.KeepOriginalImages)
        {
            Directory.CreateDirectory(originalDir);
            File.Copy(tempPath, Path.Combine(originalDir, $"{baseName}{originalExt}"), overwrite: true);
        }

        var outputFormat = ResolveWritableOutputFormat(policy.ImageOutputFormat, originalExt);
        if (!string.IsNullOrWhiteSpace(outputFormat))
        {
            var outputExt = $".{outputFormat}";
            var optimizedName = $"{baseName}{outputExt}";
            var optimizedPath = Path.Combine(destinationDirectory, optimizedName);
            var tempOptimizedPath = Path.Combine(destinationDirectory, $"{baseName}.optimized.tmp{outputExt}");
            var result = await _imageOptimization.OptimizeAsync(tempPath, tempOptimizedPath, policy, ct).ConfigureAwait(false);
            if (result.Succeeded && File.Exists(tempOptimizedPath))
            {
                File.Move(tempOptimizedPath, optimizedPath, overwrite: true);
                File.Delete(tempPath);
                return optimizedName;
            }

            if (File.Exists(tempOptimizedPath))
            {
                File.Delete(tempOptimizedPath);
            }
        }

        var fallbackName = $"{baseName}{originalExt}";
        var fallbackPath = Path.Combine(destinationDirectory, fallbackName);
        File.Move(tempPath, fallbackPath, overwrite: true);
        return fallbackName;
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Writable Output Format 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve writable output format operation.</para>
    /// \endif
    /// </summary>
    /// <param name="preferredFormat">
    /// \if KO
    /// <para>preferred Format에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for preferred format.</para>
    /// \endif
    /// </param>
    /// <param name="originalExtension">
    /// \if KO
    /// <para>original Extension에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for original extension.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Resolve Writable Output Format 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the resolve writable output format operation.</para>
    /// \endif
    /// </returns>
    private string ResolveWritableOutputFormat(string? preferredFormat, string? originalExtension)
    {
        var preferred = NormalizeOutputExtension(preferredFormat);
        if (_imageOptimization.CanEncode(preferred)) return preferred;

        if (_imageOptimization.CanEncode("jpg")) return "jpg";

        var original = NormalizeOutputExtension(originalExtension);
        return _imageOptimization.CanEncode(original) ? original : "";
    }

    /// <summary>
    /// \if KO
    /// <para>Image File 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the image file value.</para>
    /// \endif
    /// </summary>
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Validate Image File 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the validate image file operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
    private static void ValidateImageFile(IBrowserFile file)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!Array.Exists(AllowedExts, e => e == ext))
            throw new InvalidOperationException($"허용되지 않는 파일 형식입니다: {ext}");
        if (file.Size > MaxImageUploadBytes)
            throw new InvalidOperationException("이미지 파일 크기는 200MB 이하여야 합니다.");
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Output Extension 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize output extension operation.</para>
    /// \endif
    /// </summary>
    /// <param name="outputFormat">
    /// \if KO
    /// <para>output Format에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for output format.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Output Extension 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize output extension operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeOutputExtension(string? outputFormat)
    {
        var normalized = outputFormat?.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "webp" : normalized;
    }

    /// <summary>
    /// \if KO
    /// <para>Unique File Name 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unique file name operation.</para>
    /// \endif
    /// </summary>
    /// <param name="originalName">
    /// \if KO
    /// <para>original Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for original name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unique File Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the unique file name operation.</para>
    /// \endif
    /// </returns>
    private static string UniqueFileName(string originalName)
    {
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        return $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
    }
}
