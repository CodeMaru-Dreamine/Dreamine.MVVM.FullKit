using System.IO;
using Dreamine.Identity;
using WeddingPlatform.Models;
using WeddingPlatform.Services;
using Wedding.Common;

namespace WeddingPlatform.ViewModels;

/// <summary>
/// \if KO
/// <para>Wedding Super Admin View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding super admin view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WeddingSuperAdminViewModel
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
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly WeddingOptions _opts;
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
    /// <para>user Context 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the user context value.</para>
    /// \endif
    /// </summary>
    private readonly WeddingUserContext _userContext;
    /// <summary>
    /// \if KO
    /// <para>media Usage 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the media usage value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaUsageQueryService _mediaUsage;
    /// <summary>
    /// \if KO
    /// <para>media Migration 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the media migration value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaMigrationService _mediaMigration;
    /// <summary>
    /// \if KO
    /// <para>super Admin Tokens 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the super admin tokens value.</para>
    /// \endif
    /// </summary>
    private readonly ISuperAdminSessionTokenService _superAdminTokens;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="WeddingSuperAdminViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="WeddingSuperAdminViewModel"/> class with the specified settings.</para>
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
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>WeddingOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingOptions</c> value used for opts.</para>
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
    /// <param name="userContext">
    /// \if KO
    /// <para>user Context에 사용할 <c>WeddingUserContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingUserContext</c> value used for user context.</para>
    /// \endif
    /// </param>
    /// <param name="mediaUsage">
    /// \if KO
    /// <para>media Usage에 사용할 <c>IMediaUsageQueryService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaUsageQueryService</c> value used for media usage.</para>
    /// \endif
    /// </param>
    /// <param name="mediaMigration">
    /// \if KO
    /// <para>media Migration에 사용할 <c>IMediaMigrationService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaMigrationService</c> value used for media migration.</para>
    /// \endif
    /// </param>
    /// <param name="superAdminTokens">
    /// \if KO
    /// <para>super Admin Tokens에 사용할 <c>ISuperAdminSessionTokenService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ISuperAdminSessionTokenService</c> value used for super admin tokens.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>동영상 업로드 최대 용량(MB). 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video size mb value.</para>
    /// \endif
    /// </summary>
    public int MaxVideoSizeMb { get; set; } = 50;
    /// <summary>
    /// \if KO
    /// <para>계정당 동영상 업로드 최대 개수(기본값). 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video count value.</para>
    /// \endif
    /// </summary>
    public int MaxVideoCount { get; set; } = 1;
    /// <summary>
    /// \if KO
    /// <para>전체/등급별 미디어 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the media policy value.</para>
    /// \endif
    /// </summary>
    public MediaPolicySettings MediaPolicy { get; set; } = MediaPolicySettings.CreateDefault();
    /// <summary>
    /// \if KO
    /// <para>테넌트별 미디어 사용량 요약입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the media usage value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyDictionary<string, TenantMediaUsageSummary> MediaUsage { get; private set; }
        = new Dictionary<string, TenantMediaUsageSummary>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// \if KO
    /// <para>현재 미디어 사용량이 실제 파일 시스템 스캔으로 갱신된 값인지 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is media usage accurate value.</para>
    /// \endif
    /// </summary>
    public bool IsMediaUsageAccurate { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>테넌트별 이미지 최적화 마이그레이션 상태입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the migration statuses value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyDictionary<string, MediaMigrationTenantStatus> MigrationStatuses { get; private set; }
        = new Dictionary<string, MediaMigrationTenantStatus>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>Global Settings Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads global settings async data.</para>
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
    /// <para>Load Global Settings Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load global settings async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadGlobalSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);
        settings.Normalize();
        MediaPolicy = settings.MediaPolicy;
        MaxVideoSizeMb = MediaPolicy.FreeTier.VideoMaxFileSizeMb;
        MaxVideoCount = MediaPolicy.FreeTier.VideoMaxCount;
    }

    /// <summary>
    /// \if KO
    /// <para>Global Settings Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves global settings async data.</para>
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
    /// <para>Save Global Settings Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save global settings async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Is Authenticated 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is authenticated value.</para>
    /// \endif
    /// </summary>
    public bool IsAuthenticated { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Login Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the login password value.</para>
    /// \endif
    /// </summary>
    public string LoginPassword { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Super Admin Session Token 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the super admin session token value.</para>
    /// \endif
    /// </summary>
    public string SuperAdminSessionToken { get; private set; } = "";

    /// <summary>
    /// \if KO
    /// <para>Login Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Login Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the login async operation.</para>
    /// \endif
    /// </returns>
    public Task<bool> LoginAsync()
    {
        IsAuthenticated = DreaminePasswordHasher.VerifyPassword(LoginPassword, _opts.SuperAdminPassword);
        StatusMessage = IsAuthenticated ? "" : "비밀번호가 틀렸습니다.";
        SuperAdminSessionToken = IsAuthenticated ? _superAdminTokens.CreateToken() : "";
        return Task.FromResult(IsAuthenticated);
    }

    /// <summary>
    /// \if KO
    /// <para>Restore Session 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the restore session operation.</para>
    /// \endif
    /// </summary>
    public void RestoreSession()
    {
        IsAuthenticated = true;
        StatusMessage = "";
    }

    /// <summary>
    /// \if KO
    /// <para>Tenants 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tenants value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<TenantConfig> Tenants { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; private set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Is Loaded 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is loaded value.</para>
    /// \endif
    /// </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>New Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new slug value.</para>
    /// \endif
    /// </summary>
    public string NewSlug { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>New Couple Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new couple name value.</para>
    /// \endif
    /// </summary>
    public string NewCoupleName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>New Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new password value.</para>
    /// \endif
    /// </summary>
    public string NewPassword { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>New Wedding Date 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new wedding date value.</para>
    /// \endif
    /// </summary>
    public DateTime NewWeddingDate { get; set; } = DateTime.Today.AddMonths(3);

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
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 실제 파일 시스템을 스캔해 미디어 사용량을 갱신합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh media usage async operation.</para>
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
    /// <para>Refresh Media Usage Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the refresh media usage async operation.</para>
    /// \endif
    /// </returns>
    public async Task RefreshMediaUsageAsync(CancellationToken ct = default)
    {
        MigrationStatuses = await _mediaMigration.GetAllAsync(ct).ConfigureAwait(false);
        MediaUsage = await _mediaUsage.GetTenantUsageAsync(Tenants, ct).ConfigureAwait(false);
        IsMediaUsageAccurate = true;
    }

    /// <summary>
    /// \if KO
    /// <para>Reload Tenants Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reload tenants async operation.</para>
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
    /// <para>Reload Tenants Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the reload tenants async operation.</para>
    /// \endif
    /// </returns>
    private async Task ReloadTenantsAsync(CancellationToken ct)
    {
        Tenants = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
        foreach (var tenant in Tenants)
        {
            InvitationDesignCatalog.Normalize(tenant);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Fast Media Usage 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the fast media usage value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Build Fast Media Usage 작업에서 생성한 <c>IReadOnlyDictionary&lt;string, TenantMediaUsageSummary&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyDictionary&lt;string, TenantMediaUsageSummary&gt;</c> result produced by the build fast media usage operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Resolve Migration State 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve migration state operation.</para>
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
    /// <para>Resolve Migration State 작업에서 생성한 <c>MediaMigrationState</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaMigrationState</c> result produced by the resolve migration state operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Tenant Async 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the tenant async value.</para>
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
    /// <para>Create Tenant Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the create tenant async operation.</para>
    /// \endif
    /// </returns>
    public async Task<bool> CreateTenantAsync(CancellationToken ct = default)
    {
        var slug = NewSlug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(slug))
        { StatusMessage = "슬러그(URL ID)를 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewCoupleName))
        { StatusMessage = "커플 이름을 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewPassword))
        { StatusMessage = "어드민 비밀번호를 입력해주세요."; return false; }

        if (NewPassword.Length < 8)
        { StatusMessage = "어드민 비밀번호는 8자 이상이어야 합니다."; return false; }

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
            PasswordHash = DreaminePasswordHasher.HashPassword(NewPassword),
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

    /// <summary>
    /// \if KO
    /// <para>Display Async 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the display async value.</para>
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
    /// <param name="showOnHome">
    /// \if KO
    /// <para>show On Home에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for show on home.</para>
    /// \endif
    /// </param>
    /// <param name="pinOrder">
    /// \if KO
    /// <para>pin Order에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for pin order.</para>
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
    /// <para>Set Display Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set display async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Tenant Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete tenant async operation.</para>
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
    /// <para>Delete Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete tenant async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 계정(테넌트)별 동영상 업로드 용량 제한을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the max video size async value.</para>
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
    /// <param name="maxVideoSizeMb">
    /// \if KO
    /// <para>null이면 전체 설정값을 따름, 0이면 무제한, 그 외는 MB 단위 제한.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> value used for max video size mb.</para>
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
    /// <para>Set Max Video Size Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set max video size async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 계정(테넌트)별 동영상 업로드 최대 개수를 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the max video count async value.</para>
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
    /// <param name="maxVideoCount">
    /// \if KO
    /// <para>null이면 전체 설정값을 따름, 0이면 무제한, 그 외는 개수 제한.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> value used for max video count.</para>
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
    /// <para>Set Max Video Count Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set max video count async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 계정별 미디어 정책 override를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves media override async data.</para>
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
    /// <para>Save Media Override Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save media override async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 계정별 미디어 정책 override를 제거해 등급 정책을 따르게 합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset media override async operation.</para>
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
    /// <para>Reset Media Override Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the reset media override async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 테넌트 이미지 최적화 마이그레이션을 백그라운드로 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queue image migration async operation.</para>
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
    /// <para>Queue Image Migration Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the queue image migration async operation.</para>
    /// \endif
    /// </returns>
    public async Task QueueImageMigrationAsync(string slug, CancellationToken ct = default)
    {
        await _mediaMigration.QueueTenantAsync(slug, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 이미지 최적화 작업이 대기열에 등록되었습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>\brief 실패한 이미지 최적화 마이그레이션을 재시도합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the retry image migration async operation.</para>
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
    /// <para>Retry Image Migration Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the retry image migration async operation.</para>
    /// \endif
    /// </returns>
    public async Task RetryImageMigrationAsync(string slug, CancellationToken ct = default)
    {
        await _mediaMigration.RetryTenantAsync(slug, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 이미지 최적화 실패 항목을 재시도합니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Unlocked Layouts Async 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the unlocked layouts async value.</para>
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
    /// <param name="unlockedLayoutModes">
    /// \if KO
    /// <para>unlocked Layout Modes에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for unlocked layout modes.</para>
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
    /// <para>Set Unlocked Layouts Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set unlocked layouts async operation.</para>
    /// \endif
    /// </returns>
    public async Task SetUnlockedLayoutsAsync(string slug, IEnumerable<string> unlockedLayoutModes, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;

        config.UnlockedLayoutModes = NormalizeUnlockedLayoutKeys(unlockedLayoutModes);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 레이아웃 권한 저장됨";
    }

    /// <summary>
    /// \if KO
    /// <para>Unlocked Themes Async 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the unlocked themes async value.</para>
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
    /// <param name="unlockedThemeKeys">
    /// \if KO
    /// <para>unlocked Theme Keys에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for unlocked theme keys.</para>
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
    /// <para>Set Unlocked Themes Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set unlocked themes async operation.</para>
    /// \endif
    /// </returns>
    public async Task SetUnlockedThemesAsync(string slug, IEnumerable<string> unlockedThemeKeys, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;

        config.UnlockedThemeKeys = NormalizeUnlockedThemeKeys(unlockedThemeKeys);
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 테마 권한 저장됨";
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Unlocked Layout Keys 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize unlocked layout keys operation.</para>
    /// \endif
    /// </summary>
    /// <param name="unlockedLayoutModes">
    /// \if KO
    /// <para>unlocked Layout Modes에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for unlocked layout modes.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Unlocked Layout Keys 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the normalize unlocked layout keys operation.</para>
    /// \endif
    /// </returns>
    private static List<string> NormalizeUnlockedLayoutKeys(IEnumerable<string> unlockedLayoutModes) =>
        unlockedLayoutModes
            .Select(WeddingLayoutCatalog.FromLegacyKey)
            .Select(mode => WeddingLayoutCatalog.Instance.Find(mode))
            .Where(option => option is { Tier: WeddingLayoutTier.Premium })
            .Select(option => option!.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

    /// <summary>
    /// \if KO
    /// <para>Normalize Unlocked Theme Keys 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize unlocked theme keys operation.</para>
    /// \endif
    /// </summary>
    /// <param name="unlockedThemeKeys">
    /// \if KO
    /// <para>unlocked Theme Keys에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for unlocked theme keys.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Unlocked Theme Keys 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the normalize unlocked theme keys operation.</para>
    /// \endif
    /// </returns>
    private static List<string> NormalizeUnlockedThemeKeys(IEnumerable<string> unlockedThemeKeys) =>
        unlockedThemeKeys
            .Select(WeddingThemeCatalog.NormalizeKey)
            .Select(key => WeddingThemeCatalog.Instance.Find(key))
            .Where(option => option is { Tier: WeddingThemeTier.Premium })
            .Select(option => option!.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

    /// <summary>
    /// \if KO
    /// <para>Normalize Nullable Non Negative 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize nullable non negative operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Nullable Non Negative 작업에서 생성한 <c>int?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> result produced by the normalize nullable non negative operation.</para>
    /// \endif
    /// </returns>
    private static int? NormalizeNullableNonNegative(int? value) =>
        value.HasValue ? Math.Max(0, value.Value) : null;

    /// <summary>
    /// \if KO
    /// <para>Normalize Override 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize override operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Override 작업에서 생성한 <c>MediaPolicyOverride?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaPolicyOverride?</c> result produced by the normalize override operation.</para>
    /// \endif
    /// </returns>
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
