using System.IO;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \if KO
/// <para>\brief App_Data 파일 구조를 기준으로 테넌트별 미디어 사용량을 계산합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates file system media usage query service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FileSystemMediaUsageQueryService : IMediaUsageQueryService
{
    /// <summary>
    /// \if KO
    /// <para>Image Extensions 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the image extensions value.</para>
    /// \endif
    /// </summary>
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    /// <summary>
    /// \if KO
    /// <para>Video Extensions 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the video extensions value.</para>
    /// \endif
    /// </summary>
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".m4v"
    };

    /// <summary>
    /// \if KO
    /// <para>Root Media Extensions 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root media extensions value.</para>
    /// \endif
    /// </summary>
    private static readonly HashSet<string> RootMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp3", ".ogg", ".wav", ".aac", ".m4a"
    };

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
    /// <para>migrations 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the migrations value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaMigrationService _migrations;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="FileSystemMediaUsageQueryService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="FileSystemMediaUsageQueryService"/> class with the specified settings.</para>
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
    /// <param name="migrations">
    /// \if KO
    /// <para>migrations에 사용할 <c>IMediaMigrationService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaMigrationService</c> value used for migrations.</para>
    /// \endif
    /// </param>
    public FileSystemMediaUsageQueryService(ITenantStore tenants, IMediaMigrationService migrations)
    {
        _tenants = tenants;
        _migrations = migrations;
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Usage Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant usage async value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenant">
    /// \if KO
    /// <para>tenant에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for tenant.</para>
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
    /// <para>Get Tenant Usage Async 작업에서 생성한 <c>Task&lt;TenantMediaUsageSummary&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;TenantMediaUsageSummary&gt;</c> result produced by the get tenant usage async operation.</para>
    /// \endif
    /// </returns>
    public async Task<TenantMediaUsageSummary> GetTenantUsageAsync(TenantConfig tenant, CancellationToken ct = default)
    {
        var root = _tenants.GetTenantDataPath(tenant.Slug);
        var galleryDir = Path.Combine(root, "gallery");
        var thumbDir = Path.Combine(root, "thumb");
        var originalDir = Path.Combine(root, "original");
        var status = await _migrations.GetTenantStatusAsync(tenant.Slug, ct).ConfigureAwait(false);

        var summary = new TenantMediaUsageSummary
        {
            Slug = tenant.Slug,
            ImageCount = tenant.GalleryFileNames.Count,
            VideoCount = tenant.VideoFileNames.Count,
            MigrationState = status?.State ?? InferMigrationState(tenant),
            LastModified = tenant.CreatedAt,
        };

        summary.OptimizedImageBytes = SumFiles(galleryDir, ImageExtensions, out var galleryModified);
        var thumbBytes = SumFiles(thumbDir, ImageExtensions, out var thumbModified);
        summary.OriginalImageBytes = SumFiles(originalDir, ImageExtensions, out var originalModified);
        summary.VideoBytes = SumTenantVideos(root, tenant.VideoFileNames, out var videoModified);
        var rootMediaBytes = SumFiles(root, RootMediaExtensions, out var rootModified);
        summary.TotalBytes = summary.OptimizedImageBytes + thumbBytes + summary.OriginalImageBytes + summary.VideoBytes + rootMediaBytes;
        summary.LastModified = MaxDate(galleryModified, thumbModified, originalModified, videoModified, rootModified) ?? tenant.CreatedAt;

        return summary;
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Usage Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant usage async value.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IEnumerable&lt;TenantConfig&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;TenantConfig&gt;</c> value used for tenants.</para>
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
    /// <para>Get Tenant Usage Async 작업에서 생성한 <c>Task&lt;IReadOnlyDictionary&lt;string, TenantMediaUsageSummary&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyDictionary&lt;string, TenantMediaUsageSummary&gt;&gt;</c> result produced by the get tenant usage async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyDictionary<string, TenantMediaUsageSummary>> GetTenantUsageAsync(IEnumerable<TenantConfig> tenants, CancellationToken ct = default)
    {
        var result = new Dictionary<string, TenantMediaUsageSummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var tenant in tenants)
        {
            result[tenant.Slug] = await GetTenantUsageAsync(tenant, ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// \if KO
    /// <para>Infer Migration State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the infer migration state operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tenant">
    /// \if KO
    /// <para>tenant에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for tenant.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Infer Migration State 작업에서 생성한 <c>MediaMigrationState</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaMigrationState</c> result produced by the infer migration state operation.</para>
    /// \endif
    /// </returns>
    private static MediaMigrationState InferMigrationState(TenantConfig tenant)
    {
        if (tenant.GalleryFileNames.Count == 0) return MediaMigrationState.Skipped;
        return tenant.GalleryFileNames.All(x => Path.GetExtension(x).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            ? MediaMigrationState.Completed
            : MediaMigrationState.Pending;
    }

    /// <summary>
    /// \if KO
    /// <para>Sum Tenant Videos 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sum tenant videos operation.</para>
    /// \endif
    /// </summary>
    /// <param name="root">
    /// \if KO
    /// <para>root에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for root.</para>
    /// \endif
    /// </param>
    /// <param name="fileNames">
    /// \if KO
    /// <para>file Names에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for file names.</para>
    /// \endif
    /// </param>
    /// <param name="lastModified">
    /// \if KO
    /// <para>last Modified에 사용할 <c>DateTime?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime?</c> value used for last modified.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sum Tenant Videos 작업에서 생성한 <c>long</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>long</c> result produced by the sum tenant videos operation.</para>
    /// \endif
    /// </returns>
    private static long SumTenantVideos(string root, IEnumerable<string> fileNames, out DateTime? lastModified)
    {
        long total = 0;
        lastModified = null;

        foreach (var fileName in fileNames)
        {
            var path = Path.Combine(root, fileName);
            if (!File.Exists(path) || !VideoExtensions.Contains(Path.GetExtension(path))) continue;
            AddFile(path, ref total, ref lastModified);
        }

        return total;
    }

    /// <summary>
    /// \if KO
    /// <para>Sum Files 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sum files operation.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="allowedExtensions">
    /// \if KO
    /// <para>allowed Extensions에 사용할 <c>ISet&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ISet&lt;string&gt;</c> value used for allowed extensions.</para>
    /// \endif
    /// </param>
    /// <param name="lastModified">
    /// \if KO
    /// <para>last Modified에 사용할 <c>DateTime?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime?</c> value used for last modified.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sum Files 작업에서 생성한 <c>long</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>long</c> result produced by the sum files operation.</para>
    /// \endif
    /// </returns>
    private static long SumFiles(string path, ISet<string> allowedExtensions, out DateTime? lastModified)
    {
        long total = 0;
        lastModified = null;
        if (!Directory.Exists(path)) return 0;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
        {
            if (!allowedExtensions.Contains(Path.GetExtension(file))) continue;
            AddFile(file, ref total, ref lastModified);
        }

        return total;
    }

    /// <summary>
    /// \if KO
    /// <para>File 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the file item.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="total">
    /// \if KO
    /// <para>total에 사용할 <c>long</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>long</c> value used for total.</para>
    /// \endif
    /// </param>
    /// <param name="lastModified">
    /// \if KO
    /// <para>last Modified에 사용할 <c>DateTime?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime?</c> value used for last modified.</para>
    /// \endif
    /// </param>
    private static void AddFile(string path, ref long total, ref DateTime? lastModified)
    {
        try
        {
            var info = new FileInfo(path);
            total += info.Length;
            lastModified = MaxDate(lastModified, info.LastWriteTime);
        }
        catch
        {
            // 파일이 동시에 삭제되는 경우 사용량 계산에서만 제외합니다.
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Max Date 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the max date operation.</para>
    /// \endif
    /// </summary>
    /// <param name="values">
    /// \if KO
    /// <para>values에 사용할 <c>DateTime?[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime?[]</c> value used for values.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Max Date 작업에서 생성한 <c>DateTime?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime?</c> result produced by the max date operation.</para>
    /// \endif
    /// </returns>
    private static DateTime? MaxDate(params DateTime?[] values)
    {
        var concrete = values.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        return concrete.Length == 0 ? null : concrete.Max();
    }
}
