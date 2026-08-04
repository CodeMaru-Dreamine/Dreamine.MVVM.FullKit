using System.IO;
using System.Collections.Concurrent;
using Dreamine.AppSecurity;
using Microsoft.AspNetCore.Components.Forms;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Local Media Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates local media service functionality and related state.</para>
/// \endif
/// </summary>
public class LocalMediaService : IMediaService
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly HashSet<string> VideoExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".ogg" };

    /// <summary>
    /// \if KO
    /// <para>root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root value.</para>
    /// \endif
    /// </summary>
    private readonly string _root;
    private readonly long _maxTenantMediaBytes;
    private readonly int _maxTenantVideoCount;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantGates =
        new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// \if KO
    /// <para>Max Image Bytes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the max image bytes value.</para>
    /// \endif
    /// </summary>
    private const long MaxImageBytes = 20 * 1024 * 1024;
    /// <summary>
    /// \if KO
    /// <para>Max Video Bytes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the max video bytes value.</para>
    /// \endif
    /// </summary>
    private const long MaxVideoBytes = 500 * 1024 * 1024;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="LocalMediaService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="LocalMediaService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>PortfolioOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public LocalMediaService(PortfolioOptions opts)
    {
        _root = opts.ResolvedDataPath;
        _maxTenantMediaBytes = opts.MaxTenantMediaBytes > 0
            ? opts.MaxTenantMediaBytes
            : 1024L * 1024 * 1024;
        _maxTenantVideoCount = opts.MaxTenantVideoCount > 0
            ? opts.MaxTenantVideoCount
            : 50;
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> SaveAsync(string slug, string projectId, IBrowserFile file)
    {
        ValidateUpload(file, MaxImageBytes, ImageExtensions, "image");
        SemaphoreSlim gate = GetTenantGate(slug);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            string dir = ResolveProjectMediaDirectory(slug, projectId);
            Directory.CreateDirectory(dir);
            EnsureTenantQuota(slug, file.Size, isVideo: false);
            string ext = Path.GetExtension(file.Name).ToLowerInvariant();
            string safe = $"{Guid.NewGuid():N}{ext}";
            string path = StoragePathGuard.ResolveUnderRoot(dir, safe);
            await SaveFileAsync(file, path, MaxImageBytes).ConfigureAwait(false);
            return safe;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Video Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves video async data.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Save Video Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save video async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> SaveVideoAsync(string slug, string projectId, IBrowserFile file)
    {
        ValidateUpload(file, MaxVideoBytes, VideoExtensions, "video");
        SemaphoreSlim gate = GetTenantGate(slug);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            string dir = ResolveProjectMediaDirectory(slug, projectId);
            Directory.CreateDirectory(dir);
            EnsureTenantQuota(slug, file.Size, isVideo: true);
            string ext = Path.GetExtension(file.Name).ToLowerInvariant();
            string safe = $"vid_{Guid.NewGuid():N}{ext}";
            string path = StoragePathGuard.ResolveUnderRoot(dir, safe);
            await SaveFileAsync(file, path, MaxVideoBytes).ConfigureAwait(false);
            return safe;
        }
        finally
        {
            gate.Release();
        }
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
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
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteAsync(string slug, string projectId, string fileName)
    {
        // External URLs and legacy relative paths are model values, not local files.
        // They are removed from the project model without ever reaching File.Delete.
        if (!IsGeneratedMediaFileName(fileName)) return;

        SemaphoreSlim gate = GetTenantGate(slug);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            string dir = ResolveProjectMediaDirectory(slug, projectId);
            string path = StoragePathGuard.ResolveUnderRoot(dir, fileName);
            if (File.Exists(path)) File.Delete(path);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Profile Image Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves profile image async data.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Save Profile Image Async 작업에서 생성한 <c>Task&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string&gt;</c> result produced by the save profile image async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string> SaveProfileImageAsync(string slug, IBrowserFile file)
    {
        ValidateUpload(file, MaxImageBytes, ImageExtensions, "profile image");
        SemaphoreSlim gate = GetTenantGate(slug);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            string tenantRoot = ResolveTenantRoot(slug);
            string mediaRoot = StoragePathGuard.ResolveUnderRoot(tenantRoot, "media");
            string dir = StoragePathGuard.ResolveUnderRoot(mediaRoot, "_profile");
            Directory.CreateDirectory(dir);
            string ext = Path.GetExtension(file.Name).ToLowerInvariant();
            string safe = $"profile{ext}";
            string path = StoragePathGuard.ResolveUnderRoot(dir, safe);
            long replacedBytes = File.Exists(path) ? new FileInfo(path).Length : 0;
            EnsureTenantQuota(slug, file.Size - replacedBytes, isVideo: false);
            await SaveFileAsync(file, path, MaxImageBytes).ConfigureAwait(false);
            return safe;
        }
        finally
        {
            gate.Release();
        }
    }

    private SemaphoreSlim GetTenantGate(string slug)
    {
        string normalizedSlug = slug.Trim().ToLowerInvariant();
        return _tenantGates.GetOrAdd(normalizedSlug, static _ => new SemaphoreSlim(1, 1));
    }

    private string ResolveTenantRoot(string slug) =>
        StoragePathGuard.ResolveIdentifierDirectory(_root, slug, nameof(slug), normalizeToLower: true);

    private string ResolveProjectMediaDirectory(string slug, string projectId)
    {
        string mediaRoot = StoragePathGuard.ResolveUnderRoot(ResolveTenantRoot(slug), "media");
        return StoragePathGuard.ResolveIdentifierDirectory(mediaRoot, projectId, nameof(projectId));
    }

    private void EnsureTenantQuota(string slug, long additionalBytes, bool isVideo)
    {
        string mediaRoot = StoragePathGuard.ResolveUnderRoot(ResolveTenantRoot(slug), "media");
        Directory.CreateDirectory(mediaRoot);
        FileInfo[] files = new DirectoryInfo(mediaRoot).EnumerateFiles("*", SearchOption.AllDirectories).ToArray();
        long usedBytes = files.Sum(item => item.Length);
        if (additionalBytes > 0 && usedBytes > _maxTenantMediaBytes - additionalBytes)
        {
            throw new InvalidOperationException("포트폴리오 미디어 저장 공간(기본 1GB)을 모두 사용했습니다. 기존 파일을 삭제한 뒤 다시 시도해 주세요.");
        }

        if (isVideo && files.Count(item => VideoExtensions.Contains(item.Extension)) >= _maxTenantVideoCount)
        {
            throw new InvalidOperationException("포트폴리오에 저장할 수 있는 동영상 개수(기본 50개)를 초과했습니다. 기존 동영상을 삭제한 뒤 다시 시도해 주세요.");
        }
    }

    private static async Task SaveFileAsync(IBrowserFile file, string path, long maximumBytes)
    {
        try
        {
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.OpenReadStream(maximumBytes).CopyToAsync(fs).ConfigureAwait(false);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    private static bool IsGeneratedMediaFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            return false;
        }

        string extension = Path.GetExtension(fileName);
        if (!ImageExtensions.Contains(extension) && !VideoExtensions.Contains(extension)) return false;

        string stem = Path.GetFileNameWithoutExtension(fileName);
        string id = stem.StartsWith("vid_", StringComparison.Ordinal) ? stem[4..] : stem;
        return id.Length == 32 && id.All(char.IsAsciiHexDigit);
    }

    private static void ValidateUpload(
        IBrowserFile file,
        long maximumBytes,
        HashSet<string> allowedExtensions,
        string mediaKind)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Size <= 0 || file.Size > maximumBytes)
        {
            throw new InvalidOperationException(
                $"The {mediaKind} file must be between 1 byte and {maximumBytes} bytes.");
        }

        var extension = Path.GetExtension(file.Name);
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"The {mediaKind} file type is not allowed.");
        }
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
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
    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        $"/portfolio-data/{slug}/media/{projectId}/{Uri.EscapeDataString(fileName)}";

    /// <summary>
    /// \if KO
    /// <para>Profile Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the profile image url value.</para>
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
    /// <para>Get Profile Image Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get profile image url operation.</para>
    /// \endif
    /// </returns>
    public string GetProfileImageUrl(string slug, string fileName) =>
        $"/portfolio-data/{slug}/media/_profile/{Uri.EscapeDataString(fileName)}";
}
