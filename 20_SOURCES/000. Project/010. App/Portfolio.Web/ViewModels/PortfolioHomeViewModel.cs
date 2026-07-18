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
        PortfolioUserContext userContext)
    {
        _tenants = tenants;
        _projects = projects;
        _opts = opts;
        _userContext = userContext;
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
    public async Task<bool> CreateTenantAsync()
    {
        StatusMessage = "";
        if (string.IsNullOrWhiteSpace(NewSlug))   { StatusMessage = "❌ URL 주소를 입력하세요."; return false; }
        if (string.IsNullOrWhiteSpace(NewOwnerName)) { StatusMessage = "❌ 이름을 입력하세요."; return false; }
        if (NewPassword.Length < 8)               { StatusMessage = "❌ 비밀번호는 8자 이상이어야 합니다."; return false; }

        var slug = NewSlug.Trim().ToLowerInvariant();
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
        if (DreaminePasswordHasher.VerifyPassword(LoginPassword, _opts.SuperAdminPassword))
        {
            IsAuthenticated = true;
            StatusMessage = "";
            return Task.FromResult(true);
        }
        StatusMessage = "❌ 비밀번호가 틀렸습니다.";
        return Task.FromResult(false);
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
        await _tenants.DeleteAsync(slug);
        await LoadAsync();
        StatusMessage = $"✅ '{slug}' 삭제 완료.";
    }

}
