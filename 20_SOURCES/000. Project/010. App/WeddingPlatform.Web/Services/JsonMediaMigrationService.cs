using System.IO;
using System.Text.Json;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>\brief JSON 상태 파일을 사용해 이미지 마이그레이션을 멱등적으로 수행합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json media migration service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonMediaMigrationService : IMediaMigrationService
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
    /// <para>options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the options value.</para>
    /// \endif
    /// </summary>
    private readonly WeddingOptions _options;
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
    /// <para>gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the gate value.</para>
    /// \endif
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>
    /// \if KO
    /// <para>running Gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the running gate value.</para>
    /// \endif
    /// </summary>
    private readonly object _runningGate = new();
    /// <summary>
    /// \if KO
    /// <para>running Slugs 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the running slugs value.</para>
    /// \endif
    /// </summary>
    private readonly HashSet<string> _runningSlugs = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// \if KO
    /// <para>json Options 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json options value.</para>
    /// \endif
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// \if KO
    /// <para>cache 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cache value.</para>
    /// \endif
    /// </summary>
    private Dictionary<string, MediaMigrationTenantStatus>? _cache;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonMediaMigrationService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonMediaMigrationService"/> class with the specified settings.</para>
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
    /// <param name="options">
    /// \if KO
    /// <para>동작을 구성하는 설정입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The options that configure the operation.</para>
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

    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
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
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyDictionary&lt;string, MediaMigrationTenantStatus&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyDictionary&lt;string, MediaMigrationTenantStatus&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyDictionary<string, MediaMigrationTenantStatus>> GetAllAsync(CancellationToken ct = default)
    {
        return await LoadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Status Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant status async value.</para>
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
    /// <para>Get Tenant Status Async 작업에서 생성한 <c>Task&lt;MediaMigrationTenantStatus?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;MediaMigrationTenantStatus?&gt;</c> result produced by the get tenant status async operation.</para>
    /// \endif
    /// </returns>
    public async Task<MediaMigrationTenantStatus?> GetTenantStatusAsync(string slug, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct).ConfigureAwait(false);
        return all.TryGetValue(slug, out var status) ? status : null;
    }

    /// <summary>
    /// \if KO
    /// <para>Queue Tenant Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queue tenant async operation.</para>
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
    /// <para>Queue Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the queue tenant async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Retry Tenant Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the retry tenant async operation.</para>
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
    /// <para>Retry Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the retry tenant async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Process Tenant Safe Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the process tenant safe async operation.</para>
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
    /// <para>Process Tenant Safe Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the process tenant safe async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Process Tenant Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the process tenant async operation.</para>
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
    /// <para>Process Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the process tenant async operation.</para>
    /// \endif
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// \if KO
    /// <para>현재 객체 상태에서 Process Tenant Async 작업을 수행할 수 없는 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the process tenant async operation is not valid for the current object state.</para>
    /// \endif
    /// </exception>
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

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
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
    /// <para>Load Async 작업에서 생성한 <c>Task&lt;Dictionary&lt;string, MediaMigrationTenantStatus&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;Dictionary&lt;string, MediaMigrationTenantStatus&gt;&gt;</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="status">
    /// \if KO
    /// <para>status에 사용할 <c>Dictionary&lt;string, MediaMigrationTenantStatus&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Dictionary&lt;string, MediaMigrationTenantStatus&gt;</c> value used for status.</para>
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
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Status Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the status path value.</para>
    /// \endif
    /// </summary>
    private string StatusPath
    {
        get
        {
            var dataRoot = Path.GetDirectoryName(_options.ResolvedDataPath.TrimEnd(Path.DirectorySeparatorChar))
                ?? Path.Combine(AppContext.BaseDirectory, "App_Data");
            return Path.Combine(dataRoot, "media-migration-status.json");
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Is Running 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is running.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Is Running 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is running condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool IsRunning(string slug)
    {
        lock (_runningGate)
        {
            return _runningSlugs.Contains(slug);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Start Background 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start background operation.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Or Create 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the or create value.</para>
    /// \endif
    /// </summary>
    /// <param name="all">
    /// \if KO
    /// <para>all에 사용할 <c>Dictionary&lt;string, MediaMigrationTenantStatus&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Dictionary&lt;string, MediaMigrationTenantStatus&gt;</c> value used for all.</para>
    /// \endif
    /// </param>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Or Create 작업에서 생성한 <c>MediaMigrationTenantStatus</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaMigrationTenantStatus</c> result produced by the get or create operation.</para>
    /// \endif
    /// </returns>
    private static MediaMigrationTenantStatus GetOrCreate(Dictionary<string, MediaMigrationTenantStatus> all, string slug)
    {
        if (all.TryGetValue(slug, out var status)) return status;
        status = new MediaMigrationTenantStatus { Slug = slug, State = MediaMigrationState.Pending };
        all[slug] = status;
        return status;
    }

    /// <summary>
    /// \if KO
    /// <para>Mark File 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the mark file operation.</para>
    /// \endif
    /// </summary>
    /// <param name="fileStatus">
    /// \if KO
    /// <para>file Status에 사용할 <c>MediaMigrationFileStatus</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaMigrationFileStatus</c> value used for file status.</para>
    /// \endif
    /// </param>
    /// <param name="state">
    /// \if KO
    /// <para>state에 사용할 <c>MediaMigrationState</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaMigrationState</c> value used for state.</para>
    /// \endif
    /// </param>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    private static void MarkFile(MediaMigrationFileStatus fileStatus, MediaMigrationState state, string message)
    {
        fileStatus.State = state;
        fileStatus.Message = message;
        fileStatus.UpdatedAt = DateTime.Now;
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
    /// <param name="sourceFileName">
    /// \if KO
    /// <para>source File Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for source file name.</para>
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
    private string ResolveWritableOutputFormat(string? preferredFormat, string? sourceFileName)
    {
        var preferred = NormalizeFormat(preferredFormat);
        if (_imageOptimization.CanEncode(preferred)) return preferred;

        if (_imageOptimization.CanEncode("jpg")) return "jpg";

        var original = NormalizeFormat(Path.GetExtension(sourceFileName));
        return _imageOptimization.CanEncode(original) ? original : "";
    }

    /// <summary>
    /// \if KO
    /// <para>Progress Message 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the progress message value.</para>
    /// \endif
    /// </summary>
    /// <param name="status">
    /// \if KO
    /// <para>status에 사용할 <c>MediaMigrationTenantStatus</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaMigrationTenantStatus</c> value used for status.</para>
    /// \endif
    /// </param>
    /// <param name="processed">
    /// \if KO
    /// <para>processed에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for processed.</para>
    /// \endif
    /// </param>
    /// <param name="total">
    /// \if KO
    /// <para>total에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for total.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Progress Message 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build progress message operation.</para>
    /// \endif
    /// </returns>
    private static string BuildProgressMessage(MediaMigrationTenantStatus status, int processed, int total)
    {
        var failed = status.Files.Count(x => x.State == MediaMigrationState.Failed);
        var completed = status.Files.Count(x => x.State == MediaMigrationState.Completed);
        var skipped = status.Files.Count(x => x.State == MediaMigrationState.Skipped);
        if (failed > 0) return $"처리 중 ({processed}/{total}, 실패 {failed})";
        return $"처리 중 ({processed}/{total}, 완료 {completed}, 건너뜀 {skipped})";
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Format 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize format operation.</para>
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
    /// <para>Normalize Format 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize format operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeFormat(string? outputFormat)
    {
        var normalized = outputFormat?.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "webp" : normalized;
    }
}
