using Dreamine.Identity;
using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Family Home View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family home view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyHomeViewModel
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IFamilyTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly FamilyOptions _opts;
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
    /// <para>지정한 설정으로 <see cref="FamilyHomeViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="FamilyHomeViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IFamilyTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IFamilyTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>FamilyOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyOptions</c> value used for opts.</para>
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
    public FamilyHomeViewModel(IFamilyTenantStore tenants, FamilyOptions opts, IGlobalSettingsStore globalSettings)
    {
        _tenants = tenants;
        _opts = opts;
        _globalSettings = globalSettings;
    }

    /// <summary>
    /// \if KO
    /// <para>이미지(사진/커버) 업로드 최대 용량(MB). 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max image size mb value.</para>
    /// \endif
    /// </summary>
    public int MaxImageSizeMb { get; set; } = 20;
    /// <summary>
    /// \if KO
    /// <para>동영상 업로드 최대 용량(MB). 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video size mb value.</para>
    /// \endif
    /// </summary>
    public int MaxVideoSizeMb { get; set; } = 500;

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
        MaxImageSizeMb = settings.MaxImageSizeMb;
        MaxVideoSizeMb = settings.MaxVideoSizeMb;
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
        await _globalSettings.SaveAsync(
            new GlobalSettings { MaxImageSizeMb = MaxImageSizeMb, MaxVideoSizeMb = MaxVideoSizeMb }, ct)
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
        return Task.FromResult(IsAuthenticated);
    }

    /// <summary>
    /// \if KO
    /// <para>Tenants 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tenants value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<FamilyConfig> Tenants { get; private set; } = [];
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
    /// <para>New Family Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new family name value.</para>
    /// \endif
    /// </summary>
    public string NewFamilyName { get; set; } = "";
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
        Tenants = await _tenants.GetAllAsync(ct).ConfigureAwait(false);
        await LoadGlobalSettingsAsync(ct).ConfigureAwait(false);
        IsLoaded = true;
        StatusMessage = "";
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Async 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the tenant async value.</para>
    /// \endif
    /// </summary>
    /// <param name="owner">
    /// \if KO
    /// <para>owner에 사용할 <c>FamilyCurrentUser?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyCurrentUser?</c> value used for owner.</para>
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
    /// <para>Create Tenant Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the create tenant async operation.</para>
    /// \endif
    /// </returns>
    public async Task<bool> CreateTenantAsync(FamilyCurrentUser? owner = null, CancellationToken ct = default)
    {
        var slug = NewSlug.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(slug))
        { StatusMessage = "슬러그(URL ID)를 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewFamilyName))
        { StatusMessage = "가족 이름을 입력해주세요."; return false; }

        if (string.IsNullOrWhiteSpace(NewPassword))
        { StatusMessage = "어드민 비밀번호를 입력해주세요."; return false; }

        if (NewPassword.Length < 8)
        { StatusMessage = "어드민 비밀번호는 8자 이상이어야 합니다."; return false; }

        if (slug == "admin")
        { StatusMessage = "'admin'은 슬러그로 사용할 수 없습니다."; return false; }

        if (await _tenants.ExistsAsync(slug, ct).ConfigureAwait(false))
        { StatusMessage = $"이미 존재하는 슬러그입니다: {slug}"; return false; }

        var config = new FamilyConfig
        {
            Slug = slug,
            FamilyName = NewFamilyName.Trim(),
            PasswordHash = DreaminePasswordHasher.HashPassword(NewPassword),
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
    /// <para>\brief 계정(가족 앨범)별 이미지 업로드 용량 제한을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the max image size async value.</para>
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
    /// <param name="maxImageSizeMb">
    /// \if KO
    /// <para>null이면 전체 설정값을 따름, 0이면 무제한, 그 외는 MB 단위 제한.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> value used for max image size mb.</para>
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
    /// <para>Set Max Image Size Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set max image size async operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 계정(가족 앨범)별 동영상 업로드 용량 제한을 설정합니다.</para>
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
        config.MaxVideoSizeMb = maxVideoSizeMb;
        await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
        await LoadAsync(ct).ConfigureAwait(false);
        StatusMessage = $"✅ '{slug}' 동영상 용량 제한 저장됨";
    }
}
