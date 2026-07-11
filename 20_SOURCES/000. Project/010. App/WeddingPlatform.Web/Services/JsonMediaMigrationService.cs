using System.IO;
using System.Text.Json;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \brief JSON 상태 파일을 사용해 이미지 마이그레이션을 멱등적으로 수행합니다.
/// </summary>
public sealed class JsonMediaMigrationService : IMediaMigrationService
{
    private readonly ITenantStore _tenants;
    private readonly WeddingOptions _options;
    private readonly IMediaQuotaPolicyResolver _policyResolver;
    private readonly IImageOptimizationService _imageOptimization;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _runningGate = new();
    private readonly HashSet<string> _runningSlugs = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private Dictionary<string, MediaMigrationTenantStatus>? _cache;

    public JsonMediaMigrationService(
        ITenantStore tenants,
        WeddingOptions options,
        IMediaQuotaPolicyResolver policyResolver,
        IImageOptimizationService imageOptimization)
    {
        _tenants = tenants;
        _options = options;
        _policyResolver = policyResolver;
        _imageOptimization = imageOptimization;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, MediaMigrationTenantStatus>> GetAllAsync(CancellationToken ct = default)
    {
        return await LoadAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MediaMigrationTenantStatus?> GetTenantStatusAsync(string slug, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct).ConfigureAwait(false);
        return all.TryGetValue(slug, out var status) ? status : null;
    }

    /// <inheritdoc />
    public async Task QueueTenantAsync(string slug, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct).ConfigureAwait(false);
        if (!all.TryGetValue(slug, out var status))
        {
            status = new MediaMigrationTenantStatus { Slug = slug, State = MediaMigrationState.Pending };
            all[slug] = status;
        }

        if (status.State is MediaMigrationState.Processing && IsRunning(slug))
        {
            return;
        }

        status.State = MediaMigrationState.Pending;
        status.Message = "변환 대기";
        status.UpdatedAt = DateTime.Now;
        await SaveAsync(all, ct).ConfigureAwait(false);
        StartBackground(slug);
    }

    /// <inheritdoc />
    public async Task RetryTenantAsync(string slug, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct).ConfigureAwait(false);
        if (all.TryGetValue(slug, out var status))
        {
            foreach (var file in status.Files.Where(x => x.State == MediaMigrationState.Failed))
            {
                file.State = MediaMigrationState.Pending;
                file.Message = "재시도 대기";
                file.UpdatedAt = DateTime.Now;
            }
        }

        await SaveAsync(all, ct).ConfigureAwait(false);
        await QueueTenantAsync(slug, ct).ConfigureAwait(false);
    }

    private async Task ProcessTenantSafeAsync(string slug, CancellationToken ct)
    {
        try
        {
            await ProcessTenantAsync(slug, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var all = await LoadAsync(CancellationToken.None).ConfigureAwait(false);
            var status = GetOrCreate(all, slug);
            status.State = MediaMigrationState.Failed;
            status.Message = ex.Message;
            status.UpdatedAt = DateTime.Now;
            await SaveAsync(all, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task ProcessTenantAsync(string slug, CancellationToken ct)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;

        var policy = await _policyResolver.ResolveAsync(config, ct).ConfigureAwait(false);
        var all = await LoadAsync(ct).ConfigureAwait(false);
        var status = GetOrCreate(all, slug);
        status.State = MediaMigrationState.Processing;
        status.Message = "처리 중";
        status.UpdatedAt = DateTime.Now;
        await SaveAsync(all, ct).ConfigureAwait(false);

        var root = _tenants.GetTenantDataPath(slug);
        var galleryDir = Path.Combine(root, "gallery");
        var thumbDir = Path.Combine(root, "thumb");
        var originalDir = Path.Combine(root, "original");
        Directory.CreateDirectory(galleryDir);
        Directory.CreateDirectory(thumbDir);
        Directory.CreateDirectory(originalDir);

        if (config.GalleryFileNames.Count == 0)
        {
            status.State = MediaMigrationState.Skipped;
            status.Message = "변환할 이미지가 없습니다.";
            status.UpdatedAt = DateTime.Now;
            await SaveAsync(all, ct).ConfigureAwait(false);
            return;
        }

        var anyFailed = false;
        var anyProcessed = false;
        var allSkipped = true;
        var total = config.GalleryFileNames.Count;
        var processed = 0;

        foreach (var fileName in config.GalleryFileNames.ToArray())
        {
            ct.ThrowIfCancellationRequested();
            var fileStatus = status.Files.FirstOrDefault(x => string.Equals(x.SourceFileName, fileName, StringComparison.OrdinalIgnoreCase));
            if (fileStatus is { State: MediaMigrationState.Completed or MediaMigrationState.Skipped })
            {
                processed++;
                continue;
            }

            fileStatus ??= new MediaMigrationFileStatus { SourceFileName = fileName };
            if (!status.Files.Contains(fileStatus)) status.Files.Add(fileStatus);

            var sourcePath = Path.Combine(galleryDir, fileName);
            if (!File.Exists(sourcePath))
            {
                MarkFile(fileStatus, MediaMigrationState.Skipped, "원본 파일 없음");
                continue;
            }

            var outputFormat = ResolveWritableOutputFormat(policy.ImageOutputFormat, fileName);
            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                MarkFile(fileStatus, MediaMigrationState.Skipped, $"{policy.ImageOutputFormat} 인코더 없음");
                processed++;
                continue;
            }

            try
            {
                fileStatus.State = MediaMigrationState.Processing;
                fileStatus.Message = $"처리 중 ({processed + 1}/{total})";
                fileStatus.UpdatedAt = DateTime.Now;
                status.Message = fileStatus.Message;
                await SaveAsync(all, ct).ConfigureAwait(false);

                var targetName = $"{Path.GetFileNameWithoutExtension(fileName)}.{outputFormat}";
                var tempPath = Path.Combine(galleryDir, $"{Path.GetFileNameWithoutExtension(fileName)}.migration.tmp.{outputFormat}");
                var finalPath = Path.Combine(galleryDir, targetName);
                var result = await _imageOptimization.OptimizeAsync(sourcePath, tempPath, policy, ct).ConfigureAwait(false);
                if (!result.Succeeded || !File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                    throw new InvalidOperationException(result.Message);
                }

                if (policy.KeepOriginalImages)
                {
                    File.Copy(sourcePath, Path.Combine(originalDir, fileName), overwrite: true);
                }

                File.Move(tempPath, finalPath, overwrite: true);
                File.Copy(finalPath, Path.Combine(thumbDir, targetName), overwrite: true);

                var index = config.GalleryFileNames.FindIndex(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    config.GalleryFileNames[index] = targetName;
                    await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
                }

                if (!policy.KeepOriginalImages && !string.Equals(sourcePath, finalPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(sourcePath);
                    var oldThumb = Path.Combine(thumbDir, fileName);
                    if (File.Exists(oldThumb)) File.Delete(oldThumb);
                }

                fileStatus.TargetFileName = targetName;
                var requestedFormat = NormalizeFormat(policy.ImageOutputFormat);
                var formatMessage = string.Equals(requestedFormat, outputFormat, StringComparison.OrdinalIgnoreCase)
                    ? "완료"
                    : $"완료 ({policy.ImageOutputFormat} 미지원, {outputFormat.ToUpperInvariant()}로 최적화)";
                MarkFile(fileStatus, MediaMigrationState.Completed, formatMessage);
                anyProcessed = true;
                allSkipped = false;
            }
            catch (Exception ex)
            {
                anyFailed = true;
                MarkFile(fileStatus, MediaMigrationState.Failed, ex.Message);
            }

            processed++;
            status.Message = BuildProgressMessage(status, processed, total);
            await SaveAsync(all, ct).ConfigureAwait(false);
        }

        status.State = anyFailed
            ? MediaMigrationState.Failed
            : anyProcessed
                ? MediaMigrationState.Completed
                : allSkipped
                    ? MediaMigrationState.Skipped
                    : MediaMigrationState.Completed;
        status.Message = status.State switch
        {
            MediaMigrationState.Completed => "완료",
            MediaMigrationState.Failed => "실패 항목 있음",
            MediaMigrationState.Skipped => "건너뜀",
            _ => status.Message
        };
        status.UpdatedAt = DateTime.Now;
        await SaveAsync(all, ct).ConfigureAwait(false);
    }

    private async Task<Dictionary<string, MediaMigrationTenantStatus>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cache is not null) return _cache;
            var path = StatusPath;
            if (!File.Exists(path))
            {
                _cache = new Dictionary<string, MediaMigrationTenantStatus>(StringComparer.OrdinalIgnoreCase);
                return _cache;
            }

            await using var stream = File.OpenRead(path);
            _cache = await JsonSerializer.DeserializeAsync<Dictionary<string, MediaMigrationTenantStatus>>(stream, _jsonOptions, ct).ConfigureAwait(false)
                ?? new Dictionary<string, MediaMigrationTenantStatus>(StringComparer.OrdinalIgnoreCase);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveAsync(Dictionary<string, MediaMigrationTenantStatus> status, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatusPath)!);
            var tmp = StatusPath + ".tmp";
            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, status, _jsonOptions, ct).ConfigureAwait(false);
            }

            File.Copy(tmp, StatusPath, overwrite: true);
            File.Delete(tmp);
            _cache = status;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string StatusPath
    {
        get
        {
            var dataRoot = Path.GetDirectoryName(_options.ResolvedDataPath.TrimEnd(Path.DirectorySeparatorChar))
                ?? Path.Combine(AppContext.BaseDirectory, "App_Data");
            return Path.Combine(dataRoot, "media-migration-status.json");
        }
    }

    private bool IsRunning(string slug)
    {
        lock (_runningGate)
        {
            return _runningSlugs.Contains(slug);
        }
    }

    private void StartBackground(string slug)
    {
        lock (_runningGate)
        {
            if (!_runningSlugs.Add(slug))
            {
                return;
            }
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessTenantSafeAsync(slug, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                lock (_runningGate)
                {
                    _runningSlugs.Remove(slug);
                }
            }
        });
    }

    private static MediaMigrationTenantStatus GetOrCreate(Dictionary<string, MediaMigrationTenantStatus> all, string slug)
    {
        if (all.TryGetValue(slug, out var status)) return status;
        status = new MediaMigrationTenantStatus { Slug = slug, State = MediaMigrationState.Pending };
        all[slug] = status;
        return status;
    }

    private static void MarkFile(MediaMigrationFileStatus fileStatus, MediaMigrationState state, string message)
    {
        fileStatus.State = state;
        fileStatus.Message = message;
        fileStatus.UpdatedAt = DateTime.Now;
    }

    private string ResolveWritableOutputFormat(string? preferredFormat, string? sourceFileName)
    {
        var preferred = NormalizeFormat(preferredFormat);
        if (_imageOptimization.CanEncode(preferred)) return preferred;

        if (_imageOptimization.CanEncode("jpg")) return "jpg";

        var original = NormalizeFormat(Path.GetExtension(sourceFileName));
        return _imageOptimization.CanEncode(original) ? original : "";
    }

    private static string BuildProgressMessage(MediaMigrationTenantStatus status, int processed, int total)
    {
        var failed = status.Files.Count(x => x.State == MediaMigrationState.Failed);
        var completed = status.Files.Count(x => x.State == MediaMigrationState.Completed);
        var skipped = status.Files.Count(x => x.State == MediaMigrationState.Skipped);
        if (failed > 0) return $"처리 중 ({processed}/{total}, 실패 {failed})";
        return $"처리 중 ({processed}/{total}, 완료 {completed}, 건너뜀 {skipped})";
    }

    private static string NormalizeFormat(string? outputFormat)
    {
        var normalized = outputFormat?.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "webp" : normalized;
    }
}
