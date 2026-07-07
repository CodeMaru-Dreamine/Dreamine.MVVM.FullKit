using System.Security.Cryptography;
using System.Text;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.ViewModels;

public class PortfolioHomeViewModel
{
    private readonly IPortfolioTenantStore _tenants;
    private readonly IProjectStore _projects;
    private readonly PortfolioOptions _opts;
    private readonly PortfolioUserContext _userContext;

    public List<PortfolioConfig> Tenants { get; private set; } = [];
    public Dictionary<string, int> ProjectCounts { get; private set; } = [];
    public string StatusMessage { get; set; } = "";
    public bool IsAuthenticated { get; private set; }
    public string LoginPassword { get; set; } = "";

    // 신규 생성 폼
    public string NewSlug { get; set; } = "";
    public string NewOwnerName { get; set; } = "";
    public string NewPassword { get; set; } = "";

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

    public async Task<bool> CreateTenantAsync()
    {
        StatusMessage = "";
        if (string.IsNullOrWhiteSpace(NewSlug))   { StatusMessage = "❌ URL 주소를 입력하세요."; return false; }
        if (string.IsNullOrWhiteSpace(NewOwnerName)) { StatusMessage = "❌ 이름을 입력하세요."; return false; }
        if (NewPassword.Length < 4)               { StatusMessage = "❌ 비밀번호는 4자 이상이어야 합니다."; return false; }

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
            PasswordHash = Hash(NewPassword),
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

    public Task<bool> LoginAsync()
    {
        if (Hash(LoginPassword) == Hash(_opts.SuperAdminPassword))
        {
            IsAuthenticated = true;
            StatusMessage = "";
            return Task.FromResult(true);
        }
        StatusMessage = "❌ 비밀번호가 틀렸습니다.";
        return Task.FromResult(false);
    }

    public async Task SaveTenantAsync(PortfolioConfig cfg)
    {
        await _tenants.SaveAsync(cfg);
        StatusMessage = $"✅ '{cfg.Slug}' 저장 완료.";
    }

    public async Task DeleteTenantAsync(string slug)
    {
        await _tenants.DeleteAsync(slug);
        await LoadAsync();
        StatusMessage = $"✅ '{slug}' 삭제 완료.";
    }

    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLower();
}
