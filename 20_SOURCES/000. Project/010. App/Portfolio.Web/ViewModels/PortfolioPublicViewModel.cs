using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.ViewModels;

public class PortfolioPublicViewModel
{
    private readonly IPortfolioTenantStore _tenants;
    private readonly IProjectStore _projects;
    private readonly IResumeStore _resumes;
    private readonly IContactStore _contacts;
    private readonly IMediaService _media;

    public PortfolioConfig? Config { get; private set; }
    public List<ProjectItem> Projects { get; private set; } = [];
    public ResumeInfo Resume { get; private set; } = new();
    public string StatusMessage { get; set; } = "";

    // 연락처 폼
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactMessage { get; set; } = "";

    // 필터/검색
    public string Query { get; set; } = "";
    public string SelectedTag { get; set; } = "";
    public string SelectedCategory { get; set; } = "";

    public List<ProjectItem> Personal => FilteredProjects.Where(p => p.Category == ProjectCategory.Personal).ToList();
    public List<ProjectItem> Work => FilteredProjects.Where(p => p.Category == ProjectCategory.Work).ToList();
    public List<ProjectItem> Public => FilteredProjects.Where(p => p.Category == ProjectCategory.Public).ToList();

    public IEnumerable<string> AllTags => Projects
        .SelectMany(p => p.Tags)
        .Distinct()
        .OrderBy(t => t);

    private List<ProjectItem> FilteredProjects
    {
        get
        {
            var q = Projects.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(Query))
            {
                var lq = Query.ToLowerInvariant();
                q = q.Where(p => p.Title.ToLower().Contains(lq)
                               || p.Description.ToLower().Contains(lq)
                               || p.Tags.Any(t => t.ToLower().Contains(lq)));
            }
            if (!string.IsNullOrWhiteSpace(SelectedTag))
                q = q.Where(p => p.Tags.Contains(SelectedTag));
            if (!string.IsNullOrWhiteSpace(SelectedCategory) &&
                Enum.TryParse<ProjectCategory>(SelectedCategory, out var cat))
                q = q.Where(p => p.Category == cat);
            return q.ToList();
        }
    }

    public PortfolioPublicViewModel(
        IPortfolioTenantStore tenants,
        IProjectStore projects,
        IResumeStore resumes,
        IContactStore contacts,
        IMediaService media)
    {
        _tenants = tenants;
        _projects = projects;
        _resumes = resumes;
        _contacts = contacts;
        _media = media;
    }

    public async Task LoadAsync(string slug)
    {
        Config = await _tenants.GetAsync(slug);
        if (Config == null) return;
        Projects = await _projects.GetAllAsync(slug);
        Resume = await _resumes.GetAsync(slug);
    }

    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        _media.GetMediaUrl(slug, projectId, fileName);

    // 이미 절대 경로(/로 시작)이면 그대로, 아니면 GetMediaUrl로 조합
    public string GetImageUrl(string slug, string projectId, string fileName) =>
        fileName.StartsWith('/') ? fileName : _media.GetMediaUrl(slug, projectId, fileName);

    public string GetProfileImageUrl(string slug, string fileName) =>
        _media.GetProfileImageUrl(slug, fileName);

    public async Task<bool> SendContactAsync(string slug)
    {
        StatusMessage = "";
        if (string.IsNullOrWhiteSpace(ContactName)) { StatusMessage = "❌ 이름을 입력하세요."; return false; }
        if (string.IsNullOrWhiteSpace(ContactMessage)) { StatusMessage = "❌ 메시지를 입력하세요."; return false; }

        var msg = new ContactMessage
        {
            SenderName = ContactName.Trim(),
            Email = ContactEmail.Trim(),
            Message = ContactMessage.Trim(),
            SentAt = DateTime.Now,
        };
        await _contacts.SaveAsync(slug, msg);
        ContactName = ContactEmail = ContactMessage = "";
        StatusMessage = "✅ 메시지가 전송되었습니다.";
        return true;
    }
}
