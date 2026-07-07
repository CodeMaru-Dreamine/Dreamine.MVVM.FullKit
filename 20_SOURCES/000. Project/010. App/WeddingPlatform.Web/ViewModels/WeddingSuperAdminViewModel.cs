using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

public sealed class WeddingSuperAdminViewModel
{
    private readonly ITenantStore _tenants;
    private readonly WeddingOptions _opts;
    private readonly IGlobalSettingsStore _globalSettings;
    private readonly WeddingUserContext _userContext;

    public WeddingSuperAdminViewModel(
        ITenantStore tenants,
        WeddingOptions opts,
        IGlobalSettingsStore globalSettings,
        WeddingUserContext userContext)
    {
        _tenants = tenants;
        _opts = opts;
        _globalSettings = globalSettings;
        _userContext = userContext;
    }

    /// <summary>동영상 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxVideoSizeMb { get; set; } = 200;
    /// <summary>계정당 동영상 업로드 최대 개수(기본값). 0이면 무제한.</summary>
    public int MaxVideoCount { get; set; } = 6;

    public async Task LoadGlobalSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);
        MaxVideoSizeMb = settings.MaxVideoSizeMb;
        MaxVideoCount = settings.MaxVideoCount;
    }

    public async Task SaveGlobalSettingsAsync(CancellationToken ct = default)
    {
        await _globalSettings.SaveAsync(
            new GlobalSettings { MaxVideoSizeMb = MaxVideoSizeMb, MaxVideoCount = MaxVideoCount }, ct)
            .ConfigureAwait(false);
        StatusMessage = "✅ 전체 설정이 저장되었습니다.";
    }

    public bool IsAuthenticated { get; private set; }
    public string LoginPassword { get; set; } = "";

    public Task<bool> LoginAsync()
    {
        IsAuthenticated = _opts.SuperAdminPassword == LoginPassword;
        StatusMessage = IsAuthenticated ? "" : "비밀번호가 틀렸습니다.";
        return Task.FromResult(IsAuthenticated);
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
        Tenants = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
        await LoadGlobalSettingsAsync(ct).ConfigureAwait(false);
        IsLoaded = true;
        StatusMessage = "";
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
        config.MaxVideoSizeMb = maxVideoSizeMb;
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
        config.MaxVideoCount = maxVideoCount;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 동영상 개수 제한 저장됨";
    }
}
