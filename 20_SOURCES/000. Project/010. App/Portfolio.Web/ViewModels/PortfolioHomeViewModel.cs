using Dreamine.Identity;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Portfolio Home View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates portfolio home view model functionality and related state.</para>
/// \endif
/// </summary>
public class PortfolioHomeViewModel
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IPortfolioTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>projects 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the projects value.</para>
    /// \endif
    /// </summary>
    private readonly IProjectStore _projects;
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly PortfolioOptions _opts;
    /// <summary>
    /// \if KO
    /// <para>user Context 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the user context value.</para>
    /// \endif
    /// </summary>
    private readonly PortfolioUserContext _userContext;
    private readonly PortfolioTenantCreationLimiter _tenantCreationLimiter;
    private readonly PortfolioCircuitClientContext _clientContext;
    private readonly PortfolioLoginRateLimiter _loginRateLimiter;
    private static readonly TimeSpan SuperAdminSessionLifetime = TimeSpan.FromMinutes(30);
    private DateTimeOffset? _superAdminAuthenticatedUntil;

    /// <summary>
    /// \if KO
    /// <para>Tenants 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tenants value.</para>
    /// \endif
    /// </summary>
    public List<PortfolioConfig> Tenants { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Project Counts 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the project counts value.</para>
    /// \endif
    /// </summary>
    public Dictionary<string, int> ProjectCounts { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Is Authenticated 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is authenticated value.</para>
    /// \endif
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            if (_superAdminAuthenticatedUntil is null) return false;
            if (_superAdminAuthenticatedUntil > DateTimeOffset.UtcNow) return true;

            ClearSuperAdminSession();
            StatusMessage = "❌ 관리자 세션이 만료되었습니다. 다시 로그인해 주세요.";
            return false;
        }
    }
    /// <summary>
    /// \if KO
    /// <para>Login Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the login password value.</para>
    /// \endif
    /// </summary>
    public string LoginPassword { get; set; } = "";

    // 신규 생성 폼
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
    /// <para>New Owner Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new owner name value.</para>
    /// \endif
    /// </summary>
    public string NewOwnerName { get; set; } = "";
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
    /// <para>지정한 설정으로 <see cref="PortfolioHomeViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PortfolioHomeViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IPortfolioTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPortfolioTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="projects">
    /// \if KO
    /// <para>projects에 사용할 <c>IProjectStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IProjectStore</c> value used for projects.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>PortfolioOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    /// <param name="userContext">
    /// \if KO
    /// <para>user Context에 사용할 <c>PortfolioUserContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioUserContext</c> value used for user context.</para>
    /// \endif
    /// </param>
    public PortfolioHomeViewModel(
        IPortfolioTenantStore tenants,
        IProjectStore projects,
        PortfolioOptions opts,
        PortfolioUserContext userContext,
        PortfolioTenantCreationLimiter tenantCreationLimiter,
        PortfolioCircuitClientContext clientContext,
        PortfolioLoginRateLimiter loginRateLimiter)
    {
        _tenants = tenants;
        _projects = projects;
        _opts = opts;
        _userContext = userContext;
        _tenantCreationLimiter = tenantCreationLimiter;
        _clientContext = clientContext;
        _loginRateLimiter = loginRateLimiter;
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadAsync()
    {
        Tenants = await _tenants.GetAllAsync();
        var counts = new Dictionary<string, int>();
        foreach (var t in Tenants)
        {
            var list = await _projects.GetAllAsync(t.Slug);
            counts[t.Slug] = list.Count;
        }
        ProjectCounts = counts;
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Async 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the tenant async value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Tenant Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the create tenant async operation.</para>
    /// \endif
    /// </returns>
    public Task<bool> CreateTenantAsync() => CreateTenantCoreAsync(enforcePublicLimit: true);

    private async Task<bool> CreateTenantCoreAsync(bool enforcePublicLimit)
    {
        if (!await _tenantCreationLimiter.TryEnterCreationAsync().ConfigureAwait(false))
        {
            StatusMessage = "❌ 포트폴리오 생성이 진행 중입니다. 잠시만 기다려 주세요.";
            return false;
        }

        try
        {
            StatusMessage = "";
            if (string.IsNullOrWhiteSpace(NewSlug))   { StatusMessage = "❌ URL 주소를 입력하세요."; return false; }
            if (string.IsNullOrWhiteSpace(NewOwnerName)) { StatusMessage = "❌ 이름을 입력하세요."; return false; }
            if (NewPassword.Length < 8)               { StatusMessage = "❌ 비밀번호는 8자 이상이어야 합니다."; return false; }

            var slug = NewSlug.Trim().ToLowerInvariant();
            if (slug.Length > 64 || slug.Any(character =>
                    !(character is >= 'a' and <= 'z') &&
                    !(character is >= '0' and <= '9') &&
                    character != '-'))
            {
                StatusMessage = "❌ URL 주소는 64자 이하의 영문 소문자, 숫자, 하이픈(-)만 사용할 수 있습니다.";
                return false;
            }
            if (enforcePublicLimit && !_tenantCreationLimiter.TryAcquire(
                    _clientContext.RemoteIpAddress, slug, NewOwnerName))
            {
                StatusMessage = "❌ 생성 요청이 너무 많습니다. 한 시간 후 다시 시도해 주세요.";
                return false;
            }

            var existing = await _tenants.GetAsync(slug);
            if (existing != null) { StatusMessage = "❌ 이미 사용 중인 주소입니다."; return false; }

            var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
            var cfg = new PortfolioConfig
            {
                Slug = slug,
                OwnerName = NewOwnerName.Trim(),
                Title = "개발자",
                Bio = "",
                ThemeName = "dark",
                PasswordHash = DreaminePasswordHasher.HashPassword(NewPassword),
                ShowOnHome = true,
                CreatedAt = DateTime.Now,
            };

            if (user.IsAuthenticated)
            {
                cfg.OwnerUserId = user.Id;
                cfg.OwnerProvider = user.Provider;
                cfg.OwnerEmail = user.Email;
                cfg.OwnerDisplayName = user.DisplayName;
                cfg.OwnerLinkedAt = DateTime.Now;
                cfg.AdminUsers.Add(new PortfolioAdminUser
                {
                    UserId = user.Id,
                    Provider = user.Provider,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Role = "Owner",
                    AddedAt = DateTime.Now
                });
            }

            await _tenants.SaveAsync(cfg);
            StatusMessage = $"✅ '{slug}' 포트폴리오가 생성되었습니다!";
            return true;
        }
        finally
        {
            _tenantCreationLimiter.ExitCreation();
        }
    }

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
        if (!_loginRateLimiter.TryBeginAttempt(
                "superadmin", _clientContext.RemoteIpAddress, "portfolio", permitLimit: 3))
        {
            StatusMessage = "❌ 로그인 시도가 너무 많습니다. 15분 후 다시 시도해 주세요.";
            return Task.FromResult(false);
        }

        if (DreaminePasswordHasher.VerifyPassword(LoginPassword, _opts.SuperAdminPassword))
        {
            _loginRateLimiter.Reset("superadmin", _clientContext.RemoteIpAddress, "portfolio");
            _superAdminAuthenticatedUntil = DateTimeOffset.UtcNow.Add(SuperAdminSessionLifetime);
            LoginPassword = "";
            StatusMessage = "";
            return Task.FromResult(true);
        }
        StatusMessage = "❌ 비밀번호가 틀렸습니다.";
        return Task.FromResult(false);
    }

    /// <summary>Ends the local super-admin session and clears sensitive state.</summary>
    public void LogoutSuperAdmin()
    {
        ClearSuperAdminSession();
        StatusMessage = "";
    }

    /// <summary>Loads service-wide data only while the super-admin session is active.</summary>
    public async Task<bool> LoadSuperAdminAsync()
    {
        if (!EnsureSuperAdminSession()) return false;
        await LoadAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>Creates a tenant from the super-admin screen with an explicit session check.</summary>
    public async Task<bool> CreateTenantAsSuperAdminAsync()
    {
        if (!EnsureSuperAdminSession()) return false;
        return await CreateTenantCoreAsync(enforcePublicLimit: false).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves tenant async data.</para>
    /// \endif
    /// </summary>
    /// <param name="cfg">
    /// \if KO
    /// <para>cfg에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for cfg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save tenant async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveTenantAsync(PortfolioConfig cfg)
    {
        if (!EnsureSuperAdminSession()) return;
        await _tenants.SaveAsync(cfg);
        StatusMessage = $"✅ '{cfg.Slug}' 저장 완료.";
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
    /// <returns>
    /// \if KO
    /// <para>Delete Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete tenant async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteTenantAsync(string slug)
    {
        if (!EnsureSuperAdminSession()) return;
        await _tenants.DeleteAsync(slug);
        await LoadAsync();
        StatusMessage = $"✅ '{slug}' 삭제 완료.";
    }

    private bool EnsureSuperAdminSession()
    {
        if (IsAuthenticated) return true;
        if (string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessage = "❌ 관리자 세션이 만료되었습니다. 다시 로그인해 주세요.";
        }
        return false;
    }

    private void ClearSuperAdminSession()
    {
        _superAdminAuthenticatedUntil = null;
        LoginPassword = "";
        NewPassword = "";
        Tenants = [];
        ProjectCounts = [];
    }

}
