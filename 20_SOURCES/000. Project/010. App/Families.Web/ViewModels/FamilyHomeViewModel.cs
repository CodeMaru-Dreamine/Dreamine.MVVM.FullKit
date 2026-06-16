using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

public sealed class FamilyHomeViewModel
{
    private readonly IFamilyTenantStore _tenants;
    private readonly FamilyOptions _opts;

    public FamilyHomeViewModel(IFamilyTenantStore tenants, FamilyOptions opts)
    {
        _tenants = tenants;
        _opts = opts;
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
        IsLoaded = true;
        StatusMessage = "";
    }

    public async Task<bool> CreateTenantAsync(CancellationToken ct = default)
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
}
