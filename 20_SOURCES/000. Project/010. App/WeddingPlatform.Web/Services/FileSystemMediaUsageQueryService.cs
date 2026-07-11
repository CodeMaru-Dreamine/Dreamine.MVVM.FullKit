using System.IO;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \brief App_Data 파일 구조를 기준으로 테넌트별 미디어 사용량을 계산합니다.
/// </summary>
public sealed class FileSystemMediaUsageQueryService : IMediaUsageQueryService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".m4v"
    };

    private static readonly HashSet<string> RootMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp3", ".ogg", ".wav", ".aac", ".m4a"
    };

    private readonly ITenantStore _tenants;
    private readonly IMediaMigrationService _migrations;

    public FileSystemMediaUsageQueryService(ITenantStore tenants, IMediaMigrationService migrations)
    {
        _tenants = tenants;
        _migrations = migrations;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, TenantMediaUsageSummary>> GetTenantUsageAsync(IEnumerable<TenantConfig> tenants, CancellationToken ct = default)
    {
        var result = new Dictionary<string, TenantMediaUsageSummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var tenant in tenants)
        {
            result[tenant.Slug] = await GetTenantUsageAsync(tenant, ct).ConfigureAwait(false);
        }

        return result;
    }

    private static MediaMigrationState InferMigrationState(TenantConfig tenant)
    {
        if (tenant.GalleryFileNames.Count == 0) return MediaMigrationState.Skipped;
        return tenant.GalleryFileNames.All(x => Path.GetExtension(x).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            ? MediaMigrationState.Completed
            : MediaMigrationState.Pending;
    }

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

    private static DateTime? MaxDate(params DateTime?[] values)
    {
        var concrete = values.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        return concrete.Length == 0 ? null : concrete.Max();
    }
}
