using System.IO;
using WeddingPlatform.Models;
using WeddingPlatform.Services;
using Wedding.Common;

namespace WeddingPlatform.ViewModels;

public sealed class WeddingSuperAdminViewModel
{
    private readonly ITenantStore _tenants;
    private readonly WeddingOptions _opts;
    private readonly IGlobalSettingsStore _globalSettings;
    private readonly WeddingUserContext _userContext;
    private readonly IMediaUsageQueryService _mediaUsage;
    private readonly IMediaMigrationService _mediaMigration;
    private readonly ISuperAdminSessionTokenService _superAdminTokens;

    public WeddingSuperAdminViewModel(
        ITenantStore tenants,
        WeddingOptions opts,
        IGlobalSettingsStore globalSettings,
        WeddingUserContext userContext,
        IMediaUsageQueryService mediaUsage,
        IMediaMigrationService mediaMigration,
        ISuperAdminSessionTokenService superAdminTokens)
    {
        _tenants = tenants;
        _opts = opts;
        _globalSettings = globalSettings;
        _userContext = userContext;
        _mediaUsage = mediaUsage;
        _mediaMigration = mediaMigration;
        _superAdminTokens = superAdminTokens;
    }

    /// <summary>동영상 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxVideoSizeMb { get; set; } = 50;
    /// <summary>계정당 동영상 업로드 최대 개수(기본값). 0이면 무제한.</summary>
    public int MaxVideoCount { get; set; } = 1;
    /// <summary>전체/등급별 미디어 정책입니다.</summary>
    public MediaPolicySettings MediaPolicy { get; set; } = MediaPolicySettings.CreateDefault();
    /// <summary>테넌트별 미디어 사용량 요약입니다.</summary>
    public IReadOnlyDictionary<string, TenantMediaUsageSummary> MediaUsage { get; private set; }
        = new Dictionary<string, TenantMediaUsageSummary>(StringComparer.OrdinalIgnoreCase);
    /// <summary>현재 미디어 사용량이 실제 파일 시스템 스캔으로 갱신된 값인지 여부입니다.</summary>
    public bool IsMediaUsageAccurate { get; private set; }
    /// <summary>테넌트별 이미지 최적화 마이그레이션 상태입니다.</summary>
    public IReadOnlyDictionary<string, MediaMigrationTenantStatus> MigrationStatuses { get; private set; }
        = new Dictionary<string, MediaMigrationTenantStatus>(StringComparer.OrdinalIgnoreCase);

    public async Task LoadGlobalSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);
        settings.Normalize();
        MediaPolicy = settings.MediaPolicy;
        MaxVideoSizeMb = MediaPolicy.FreeTier.VideoMaxFileSizeMb;
        MaxVideoCount = MediaPolicy.FreeTier.VideoMaxCount;
    }

    public async Task SaveGlobalSettingsAsync(CancellationToken ct = default)
    {
        MediaPolicy.Normalize();
        MaxVideoSizeMb = MediaPolicy.FreeTier.VideoMaxFileSizeMb;
        MaxVideoCount = MediaPolicy.FreeTier.VideoMaxCount;
        await _globalSettings.SaveAsync(
            new GlobalSettings
            {
                MaxVideoSizeMb = MaxVideoSizeMb,
                MaxVideoCount = MaxVideoCount,
                MediaPolicy = MediaPolicy
            }, ct)
            .ConfigureAwait(false);
        StatusMessage = "✅ 전체 설정이 저장되었습니다.";
    }

    public bool IsAuthenticated { get; private set; }
    public string LoginPassword { get; set; } = "";
    public string SuperAdminSessionToken { get; private set; } = "";

    public Task<bool> LoginAsync()
    {
        IsAuthenticated = _opts.SuperAdminPassword == LoginPassword;
        StatusMessage = IsAuthenticated ? "" : "비밀번호가 틀렸습니다.";
        SuperAdminSessionToken = IsAuthenticated ? _superAdminTokens.CreateToken() : "";
        return Task.FromResult(IsAuthenticated);
    }

    public void RestoreSession()
    {
        IsAuthenticated = true;
        StatusMessage = "";
    }

    public IReadOnlyList<TenantConfig> Tenants { get; private set; } = [];
    public string StatusMessage { get; private set; } = "";
    public bool IsLoaded { get; private set; }

    public string NewSlug { get; set; } = "";
    public string NewCoupleName { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public DateTime NewWeddingDate { get; set; } = DateTime.Today.AddMonths(3);

    public async Task LoadAsync(CancellationToken ct = default)
    {
        await ReloadTenantsAsync(ct).ConfigureAwait(false);
        await LoadGlobalSettingsAsync(ct).ConfigureAwait(false);
        MigrationStatuses = await _mediaMigration.GetAllAsync(ct).ConfigureAwait(false);
        MediaUsage = BuildFastMediaUsage();
        IsMediaUsageAccurate = false;
        IsLoaded = true;
        StatusMessage = "";
    }

    /// <summary>
    /// \brief 실제 파일 시스템을 스캔해 미디어 사용량을 갱신합니다.
    /// </summary>
    public async Task RefreshMediaUsageAsync(CancellationToken ct = default)
    {
        MigrationStatuses = await _mediaMigration.GetAllAsync(ct).ConfigureAwait(false);
        MediaUsage = await _mediaUsage.GetTenantUsageAsync(Tenants, ct).ConfigureAwait(false);
        IsMediaUsageAccurate = true;
    }

    private async Task ReloadTenantsAsync(CancellationToken ct)
    {
        Tenants = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
        foreach (var tenant in Tenants)
        {
            InvitationDesignCatalog.Normalize(tenant);
        }
    }

    private IReadOnlyDictionary<string, TenantMediaUsageSummary> BuildFastMediaUsage()
    {
        var result = new Dictionary<string, TenantMediaUsageSummary>(StringComparer.OrdinalIgnoreCase);
        foreach (var tenant in Tenants)
        {
            if (MediaUsage.TryGetValue(tenant.Slug, out var existing))
            {
                existing.ImageCount = tenant.GalleryFileNames.Count;
                existing.VideoCount = tenant.VideoFileNames.Count;
                existing.MigrationState = ResolveMigrationState(tenant);
                result[tenant.Slug] = existing;
                continue;
            }

            result[tenant.Slug] = new TenantMediaUsageSummary
            {
                Slug = tenant.Slug,
                ImageCount = tenant.GalleryFileNames.Count,
                VideoCount = tenant.VideoFileNames.Count,
                MigrationState = ResolveMigrationState(tenant),
                LastModified = tenant.CreatedAt
            };
        }

        return result;
    }

    private MediaMigrationState ResolveMigrationState(TenantConfig tenant)
    {
        if (MigrationStatuses.TryGetValue(tenant.Slug, out var status))
        {
            return status.State;
        }

        if (tenant.GalleryFileNames.Count == 0) return MediaMigrationState.Skipped;
        return tenant.GalleryFileNames.All(x => Path.GetExtension(x).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            ? MediaMigrationState.Completed
            : MediaMigrationState.Pending;
    }

    public async Task<bool> CreateTenantAsync(CancellationToken ct = default)
    {
        var slug = NewSlug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(slug))
        { StatusMessage = "슬러그(URL ID)를 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewCoupleName))
        { StatusMessage = "커플 이름을 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewPassword))
        { StatusMessage = "어드민 비밀번호를 입력해주세요."; return false; }

        if (slug == "admin")
        { StatusMessage = "'admin'은 슬러그로 사용할 수 없습니다."; return false; }

        if (await _tenants.ExistsAsync(slug, ct).ConfigureAwait(false))
        { StatusMessage = $"이미 존재하는 슬러그입니다: {slug}"; return false; }

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        var config = new TenantConfig
        {
            Slug = slug,
            CoupleName = NewCoupleName.Trim(),
            WeddingDate = NewWeddingDate,
            PasswordHash = NewPassword,
            Mode = WeddingSiteMode.Invite,
            IsPublished = true
        };

        if (user.IsAuthenticated)
        {
            config.OwnerUserId = user.Id;
            config.OwnerProvider = user.Provider;
            config.OwnerEmail = user.Email;
            config.OwnerDisplayName = user.DisplayName;
            config.OwnerLinkedAt = DateTime.Now;
            config.AdminUsers.Add(new WeddingAdminUser
            {
                UserId = user.Id,
                Provider = user.Provider,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = "Owner",
                AddedAt = DateTime.Now
            });
        }

        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);

        NewSlug = "";
        NewCoupleName = "";
        NewPassword = "";
        NewWeddingDate = DateTime.Today.AddMonths(3);

        StatusMessage = $"✅ '{slug}' 청첩장이 생성되었습니다.";
        return true;
    }

    public async Task SetDisplayAsync(string slug, bool showOnHome, int pinOrder, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.ShowOnHome = showOnHome;
        config.PinOrder = pinOrder;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 노출 설정 저장됨";
    }

    public async Task DeleteTenantAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            await _tenants.DeleteAsync(slug, ct).ConfigureAwait(false);
            await LoadAsync(ct).ConfigureAwait(false);
            StatusMessage = $"🗑 '{slug}' 삭제 완료.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"삭제 오류: {ex.Message}";
        }
    }

    /// <summary>
    /// \brief 계정(테넌트)별 동영상 업로드 용량 제한을 설정합니다.
    /// </summary>
    /// <param name="maxVideoSizeMb">null이면 전체 설정값을 따름, 0이면 무제한, 그 외는 MB 단위 제한.</param>
    public async Task SetMaxVideoSizeAsync(string slug, int? maxVideoSizeMb, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.MediaPolicyOverride ??= new MediaPolicyOverride();
        config.MediaPolicyOverride.VideoMaxFileSizeMb = NormalizeNullableNonNegative(maxVideoSizeMb);
        config.MaxVideoSizeMb = null;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 동영상 용량 제한 저장됨";
    }

    /// <summary>
    /// \brief 계정(테넌트)별 동영상 업로드 최대 개수를 설정합니다.
    /// </summary>
    /// <param name="maxVideoCount">null이면 전체 설정값을 따름, 0이면 무제한, 그 외는 개수 제한.</param>
    public async Task SetMaxVideoCountAsync(string slug, int? maxVideoCount, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.MediaPolicyOverride ??= new MediaPolicyOverride();
        config.MediaPolicyOverride.VideoMaxCount = NormalizeNullableNonNegative(maxVideoCount);
        config.MaxVideoCount = null;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 동영상 개수 제한 저장됨";
    }

    /// <summary>
    /// \brief 계정별 미디어 정책 override를 저장합니다.
    /// </summary>
    public async Task SaveMediaOverrideAsync(TenantConfig tenant, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(tenant.Slug, ct).ConfigureAwait(false);
        if (config is null) return;

        config.MediaPolicyOverride = NormalizeOverride(tenant.MediaPolicyOverride);
        config.MaxVideoSizeMb = null;
        config.MaxVideoCount = null;

        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{tenant.Slug}' 미디어 정책 override 저장됨";
    }

    /// <summary>
    /// \brief 계정별 미디어 정책 override를 제거해 등급 정책을 따르게 합니다.
    /// </summary>
    public async Task ResetMediaOverrideAsync(string slug, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;

        config.MediaPolicyOverride = null;
        config.MaxVideoSizeMb = null;
        config.MaxVideoCount = null;

        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 계정별 미디어 override 초기화됨";
    }

    /// <summary>
    /// \brief 테넌트 이미지 최적화 마이그레이션을 백그라운드로 시작합니다.
    /// </summary>
    public async Task QueueImageMigrationAsync(string slug, CancellationToken ct = default)
    {
        await _mediaMigration.QueueTenantAsync(slug, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 이미지 최적화 작업이 대기열에 등록되었습니다.";
    }

    /// <summary>
    /// \brief 실패한 이미지 최적화 마이그레이션을 재시도합니다.
    /// </summary>
    public async Task RetryImageMigrationAsync(string slug, CancellationToken ct = default)
    {
        await _mediaMigration.RetryTenantAsync(slug, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 이미지 최적화 실패 항목을 재시도합니다.";
    }

    public async Task SetUnlockedLayoutsAsync(string slug, IEnumerable<string> unlockedLayoutModes, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;

        config.UnlockedLayoutModes = NormalizeUnlockedLayoutKeys(unlockedLayoutModes);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 레이아웃 권한 저장됨";
    }

    public async Task SetUnlockedThemesAsync(string slug, IEnumerable<string> unlockedThemeKeys, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;

        config.UnlockedThemeKeys = NormalizeUnlockedThemeKeys(unlockedThemeKeys);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 테마 권한 저장됨";
    }

    private static List<string> NormalizeUnlockedLayoutKeys(IEnumerable<string> unlockedLayoutModes) =>
        unlockedLayoutModes
            .Select(WeddingLayoutCatalog.FromLegacyKey)
            .Select(mode => WeddingLayoutCatalog.Instance.Find(mode))
            .Where(option => option is { Tier: WeddingLayoutTier.Premium })
            .Select(option => option!.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

    private static List<string> NormalizeUnlockedThemeKeys(IEnumerable<string> unlockedThemeKeys) =>
        unlockedThemeKeys
            .Select(WeddingThemeCatalog.NormalizeKey)
            .Select(key => WeddingThemeCatalog.Instance.Find(key))
            .Where(option => option is { Tier: WeddingThemeTier.Premium })
            .Select(option => option!.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

    private static int? NormalizeNullableNonNegative(int? value) =>
        value.HasValue ? Math.Max(0, value.Value) : null;

    private static MediaPolicyOverride? NormalizeOverride(MediaPolicyOverride? value)
    {
        if (value is null || value.IsEmpty)
        {
            return null;
        }

        value.ImageMaxCount = NormalizeNullableNonNegative(value.ImageMaxCount);
        value.ImageOptimizedMaxStorageMb = NormalizeNullableNonNegative(value.ImageOptimizedMaxStorageMb);
        value.ImageOriginalMaxStorageMb = NormalizeNullableNonNegative(value.ImageOriginalMaxStorageMb);
        value.ImageMaxLongSidePx = NormalizeNullableNonNegative(value.ImageMaxLongSidePx);
        value.ImageQuality = value.ImageQuality.HasValue ? Math.Clamp(value.ImageQuality.Value, 1, 100) : null;
        value.VideoMaxFileSizeMb = NormalizeNullableNonNegative(value.VideoMaxFileSizeMb);
        value.VideoMaxCount = NormalizeNullableNonNegative(value.VideoMaxCount);
        value.VideoMaxStorageMb = NormalizeNullableNonNegative(value.VideoMaxStorageMb);
        value.ImageOutputFormat = string.IsNullOrWhiteSpace(value.ImageOutputFormat) ? null : value.ImageOutputFormat.Trim();
        return value.IsEmpty ? null : value;
    }
}
