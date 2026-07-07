using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

public sealed class FamilyHomeViewModel
{
    private readonly IFamilyTenantStore _tenants;
    private readonly FamilyOptions _opts;
    private readonly IGlobalSettingsStore _globalSettings;

    public FamilyHomeViewModel(IFamilyTenantStore tenants, FamilyOptions opts, IGlobalSettingsStore globalSettings)
    {
        _tenants = tenants;
        _opts = opts;
        _globalSettings = globalSettings;
    }

    /// <summary>이미지(사진/커버) 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxImageSizeMb { get; set; } = 20;
    /// <summary>동영상 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxVideoSizeMb { get; set; } = 500;

    public async Task LoadGlobalSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _globalSettings.GetAsync(ct).ConfigureAwait(false);
        MaxImageSizeMb = settings.MaxImageSizeMb;
        MaxVideoSizeMb = settings.MaxVideoSizeMb;
    }

    public async Task SaveGlobalSettingsAsync(CancellationToken ct = default)
    {
        await _globalSettings.SaveAsync(
            new GlobalSettings { MaxImageSizeMb = MaxImageSizeMb, MaxVideoSizeMb = MaxVideoSizeMb }, ct)
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

    public IReadOnlyList<FamilyConfig> Tenants { get; private set; } = [];
    public string StatusMessage { get; private set; } = "";
    public bool IsLoaded { get; private set; }

    public string NewSlug { get; set; } = "";
    public string NewFamilyName { get; set; } = "";
    public string NewPassword { get; set; } = "";

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Tenants = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
        await LoadGlobalSettingsAsync(ct).ConfigureAwait(false);
        IsLoaded = true;
        StatusMessage = "";
    }

    public async Task<bool> CreateTenantAsync(FamilyCurrentUser? owner = null, CancellationToken ct = default)
    {
        var slug = NewSlug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(slug))
        { StatusMessage = "슬러그(URL ID)를 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewFamilyName))
        { StatusMessage = "가족 이름을 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewPassword))
        { StatusMessage = "어드민 비밀번호를 입력해주세요."; return false; }

        if (slug == "admin")
        { StatusMessage = "'admin'은 슬러그로 사용할 수 없습니다."; return false; }

        if (await _tenants.ExistsAsync(slug, ct).ConfigureAwait(false))
        { StatusMessage = $"이미 존재하는 슬러그입니다: {slug}"; return false; }

        var config = new FamilyConfig
        {
            Slug = slug,
            FamilyName = NewFamilyName.Trim(),
            PasswordHash = NewPassword,
            IsPublished = true
        };

        if (owner?.IsAuthenticated == true)
        {
            config.OwnerUserId = owner.Id;
            config.OwnerProvider = owner.Provider;
            config.OwnerEmail = owner.Email;
            config.OwnerDisplayName = owner.DisplayName;
            config.OwnerLinkedAt = DateTime.Now;
            config.AdminUsers.Add(new FamilyAdminUser
            {
                UserId = owner.Id,
                Provider = owner.Provider,
                Email = owner.Email,
                DisplayName = owner.DisplayName,
                Role = "Owner",
                AddedAt = DateTime.Now
            });
        }

        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);

        NewSlug = "";
        NewFamilyName = "";
        NewPassword = "";

        StatusMessage = $"✅ '{slug}' 앨범이 생성되었습니다.";
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
    /// \brief 계정(가족 앨범)별 이미지 업로드 용량 제한을 설정합니다.
    /// </summary>
    /// <param name="maxImageSizeMb">null이면 전체 설정값을 따름, 0이면 무제한, 그 외는 MB 단위 제한.</param>
    public async Task SetMaxImageSizeAsync(string slug, int? maxImageSizeMb, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) return;
        config.MaxImageSizeMb = maxImageSizeMb;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 이미지 용량 제한 저장됨";
    }

    /// <summary>
    /// \brief 계정(가족 앨범)별 동영상 업로드 용량 제한을 설정합니다.
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
}
