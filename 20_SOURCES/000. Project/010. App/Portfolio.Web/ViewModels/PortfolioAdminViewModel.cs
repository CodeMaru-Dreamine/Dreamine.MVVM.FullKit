using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.ViewModels;

public class PortfolioAdminViewModel
{
    private readonly IPortfolioTenantStore _tenants;
    private readonly IProjectStore _projects;
    private readonly IResumeStore _resumes;
    private readonly IContactStore _contacts;
    private readonly IMediaService _media;
    private readonly PortfolioUserContext _userContext;

    public bool IsAuthenticated { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsSignedIn { get; private set; }
    public bool IsLinkedToCurrentUser { get; private set; }
    public bool IsOwner { get; private set; }
    public IReadOnlyList<PortfolioAdminUser> EffectiveAdminUsers { get; private set; } = [];
    public string CurrentUserLabel { get; private set; } = "";
    public string LoginPassword { get; set; } = "";
    public string StatusMessage { get; set; } = "";
    public bool IsUploading { get; set; }

    public PortfolioConfig? Config { get; private set; }
    public List<ProjectItem> Projects { get; private set; } = [];
    public ResumeInfo Resume { get; private set; } = new();
    public List<ContactMessage> Messages { get; private set; } = [];

    // 프로젝트 편집
    public ProjectItem? EditingProject { get; private set; }
    public List<IBrowserFile> PendingFiles { get; } = [];
    public List<IBrowserFile> PendingVideos { get; } = [];

    // 새 스킬 그룹 편집
    public string NewSkillCategory { get; set; } = "";
    public string NewSkillNames { get; set; } = "";

    // 경력 편집
    public ExperienceItem? EditingExp { get; private set; }
    public EducationItem? EditingEdu { get; private set; }

    public PortfolioAdminViewModel(
        IPortfolioTenantStore tenants,
        IProjectStore projects,
        IResumeStore resumes,
        IContactStore contacts,
        IMediaService media,
        PortfolioUserContext userContext)
    {
        _tenants = tenants;
        _projects = projects;
        _resumes = resumes;
        _contacts = contacts;
        _media = media;
        _userContext = userContext;
    }

    public async Task InitializeAsync(string slug)
    {
        StatusMessage = "";
        await RefreshCurrentUserAsync().ConfigureAwait(false);

        var cfg = await _tenants.GetAsync(slug).ConfigureAwait(false);
        if (cfg is null)
        {
            return;
        }

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        IsOwner = IsOwnerUser(cfg, user);
        IsLinkedToCurrentUser = IsAdminUser(cfg, user);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(cfg);

        if (IsLinkedToCurrentUser)
        {
            IsAuthenticated = true;
            await LoadAsync(slug).ConfigureAwait(false);
        }
    }

    public async Task<bool> LoginAsync(string slug)
    {
        var cfg = await _tenants.GetAsync(slug);
        if (cfg == null) { StatusMessage = "❌ 존재하지 않는 포트폴리오입니다."; return false; }

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        await RefreshCurrentUserAsync().ConfigureAwait(false);

        if (IsAdminUser(cfg, user))
        {
            IsAuthenticated = true;
            IsLinkedToCurrentUser = true;
            IsOwner = IsOwnerUser(cfg, user);
            EffectiveAdminUsers = BuildEffectiveAdminUsers(cfg);
            StatusMessage = "";
            return true;
        }

        if (Hash(LoginPassword) != cfg.PasswordHash) { StatusMessage = "❌ 비밀번호가 틀렸습니다."; return false; }
        IsAuthenticated = true;

        if (user.IsAuthenticated && string.IsNullOrWhiteSpace(cfg.OwnerUserId))
        {
            cfg.OwnerUserId = user.Id;
            cfg.OwnerProvider = user.Provider;
            cfg.OwnerEmail = user.Email;
            cfg.OwnerDisplayName = user.DisplayName;
            cfg.OwnerLinkedAt = DateTime.Now;
            EnsureAdminUser(cfg, user, "Owner");
            await _tenants.SaveAsync(cfg).ConfigureAwait(false);
            IsLinkedToCurrentUser = true;
            IsOwner = true;
            EffectiveAdminUsers = BuildEffectiveAdminUsers(cfg);
            StatusMessage = "✅ CodeMaru/Dreamine 계정에 대표 관리자로 연결되었습니다.";
        }
        else if (user.IsAuthenticated)
        {
            EnsureAdminUser(cfg, user, "Admin");
            await _tenants.SaveAsync(cfg).ConfigureAwait(false);
            IsLinkedToCurrentUser = true;
            IsOwner = IsOwnerUser(cfg, user);
            EffectiveAdminUsers = BuildEffectiveAdminUsers(cfg);
            StatusMessage = "✅ 현재 CodeMaru/Dreamine 계정이 이 포트폴리오의 관리자로 추가되었습니다.";
        }
        else if (string.IsNullOrWhiteSpace(cfg.OwnerUserId))
        {
            StatusMessage = "✅ 로그인 성공. 공용 계정에 연결하려면 먼저 CodeMaru/Dreamine 로그인을 해주세요.";
        }
        else
        {
            StatusMessage = "";
        }

        return true;
    }

    public async Task LoadAsync(string slug)
    {
        Config = await _tenants.GetAsync(slug);
        if (Config == null) return;
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
        Projects = await _projects.GetAllAsync(slug);
        Resume = await _resumes.GetAsync(slug);
        Messages = await _contacts.GetAllAsync(slug);
        // 기존 데이터 마이그레이션: ImageFileName → WorkImages
        foreach (var p in Projects.Where(p =>
            !string.IsNullOrWhiteSpace(p.ImageFileName) && p.WorkImages.Length == 0))
            p.WorkImages = [p.ImageFileName!];
        IsLoaded = true;
    }

    // ── 프로젝트 ────────────────────────────────────────────────
    public void Logout() { IsAuthenticated = false; IsLoaded = false; LoginPassword = ""; StatusMessage = ""; EditingProject = null; }

    public void NewProject() => EditingProject = new ProjectItem();
    public void EditProject(ProjectItem p) { EditingProject = p.ShallowClone(); PendingFiles.Clear(); PendingVideos.Clear(); }
    public void CancelEdit() { EditingProject = null; PendingFiles.Clear(); PendingVideos.Clear(); }

    public async Task SaveProjectAsync(string slug)
    {
        if (EditingProject == null) return;
        StatusMessage = "";
        if (string.IsNullOrWhiteSpace(EditingProject.Title)) { StatusMessage = "❌ 제목을 입력하세요."; return; }

        if (PendingFiles.Count > 0 || PendingVideos.Count > 0)
        {
            IsUploading = true;
            foreach (var f in PendingFiles)
            {
                var saved = await _media.SaveAsync(slug, EditingProject.Id, f);
                if (EditingProject.Category == ProjectCategory.Work)
                {
                    if (!EditingProject.WorkImages.Contains(saved))
                        EditingProject.WorkImages = [.. EditingProject.WorkImages, saved];
                    if (EditingProject.ImageFileName == null)
                        EditingProject.ImageFileName = saved;
                }
                else
                {
                    // Personal / Public: WorkImages에 모아두고 첫 번째를 커버로
                    if (!EditingProject.WorkImages.Contains(saved))
                        EditingProject.WorkImages = [.. EditingProject.WorkImages, saved];
                    EditingProject.ImageFileName ??= saved;
                }
            }
            foreach (var v in PendingVideos)
            {
                var saved = await _media.SaveVideoAsync(slug, EditingProject.Id, v);
                if (!EditingProject.VideoFileNames.Contains(saved))
                    EditingProject.VideoFileNames = [.. EditingProject.VideoFileNames, saved];
            }
            PendingFiles.Clear();
            PendingVideos.Clear();
            IsUploading = false;
        }

        // ImageFileName이 있지만 WorkImages가 비어있으면 자동 포함
        if (!string.IsNullOrWhiteSpace(EditingProject.ImageFileName) &&
            !EditingProject.WorkImages.Contains(EditingProject.ImageFileName))
            EditingProject.WorkImages = [EditingProject.ImageFileName, .. EditingProject.WorkImages];

        await _projects.SaveAsync(slug, EditingProject);
        Projects = await _projects.GetAllAsync(slug);
        EditingProject = null;
        StatusMessage = "✅ 저장되었습니다.";
    }

    public async Task DeleteProjectAsync(string slug, string projectId)
    {
        await _projects.DeleteAsync(slug, projectId);
        Projects = await _projects.GetAllAsync(slug);
        StatusMessage = "✅ 삭제 완료.";
    }

    public async Task DeleteProjectMediaAsync(string slug, string projectId, string fileName)
    {
        await _media.DeleteAsync(slug, projectId, fileName);
        if (EditingProject != null)
            EditingProject.WorkImages = EditingProject.WorkImages.Where(f => f != fileName).ToArray();
    }

    public async Task DeleteProjectCoverAsync(string slug, string projectId)
    {
        if (EditingProject == null || string.IsNullOrWhiteSpace(EditingProject.ImageFileName)) return;
        var fn = EditingProject.ImageFileName;
        if (!fn.StartsWith('/'))
            await _media.DeleteAsync(slug, projectId, fn);
        EditingProject.ImageFileName = null;
    }

    public async Task DeleteProjectVideoAsync(string slug, string projectId, string fileName)
    {
        await _media.DeleteAsync(slug, projectId, fileName);
        if (EditingProject != null)
            EditingProject.VideoFileNames = EditingProject.VideoFileNames.Where(f => f != fileName).ToArray();
    }

    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        _media.GetMediaUrl(slug, projectId, fileName);

    public string GetImageUrl(string slug, string projectId, string fileName) =>
        IsExternalUrl(fileName) ? fileName : _media.GetMediaUrl(slug, projectId, fileName);

    public string GetVideoUrl(string slug, string projectId, string fileName) =>
        IsExternalUrl(fileName) ? fileName : _media.GetMediaUrl(slug, projectId, fileName);

    private static bool IsExternalUrl(string url) =>
        url.StartsWith('/') || url.StartsWith("http://") || url.StartsWith("https://");

    public string GetProfileImageUrl(string slug, string fileName) =>
        _media.GetProfileImageUrl(slug, fileName);

    // ── Resume ──────────────────────────────────────────────────
    public async Task SaveResumeAsync(string slug)
    {
        await _resumes.SaveAsync(slug, Resume);
        StatusMessage = "✅ Resume 저장 완료.";
    }

    public void AddSkillGroup()
    {
        if (string.IsNullOrWhiteSpace(NewSkillCategory)) return;
        var skills = NewSkillNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        Resume.SkillGroups.Add(new SkillGroup { Category = NewSkillCategory.Trim(), Skills = skills });
        NewSkillCategory = "";
        NewSkillNames = "";
    }

    public void RemoveSkillGroup(SkillGroup g) => Resume.SkillGroups.Remove(g);

    public void NewExperience() => EditingExp = new ExperienceItem();
    public void EditExperience(ExperienceItem e) => EditingExp = e;
    public void CancelExp() => EditingExp = null;
    public void SaveExperience()
    {
        if (EditingExp == null) return;
        if (!Resume.Experiences.Contains(EditingExp))
            Resume.Experiences.Add(EditingExp);
        EditingExp = null;
    }
    public void RemoveExperience(ExperienceItem e) => Resume.Experiences.Remove(e);

    public void NewEducation() => EditingEdu = new EducationItem();
    public void EditEducation(EducationItem e) => EditingEdu = e;
    public void CancelEdu() => EditingEdu = null;
    public void SaveEducation()
    {
        if (EditingEdu == null) return;
        if (!Resume.Educations.Contains(EditingEdu))
            Resume.Educations.Add(EditingEdu);
        EditingEdu = null;
    }
    public void RemoveEducation(EducationItem e) => Resume.Educations.Remove(e);

    // ── 설정 ────────────────────────────────────────────────────
    public async Task SaveConfigAsync()
    {
        if (Config == null) return;
        await _tenants.SaveAsync(Config);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
        StatusMessage = "✅ 설정이 저장되었습니다.";
    }

    public async Task RemoveAdminAsync(string userId)
    {
        if (Config is null) return;
        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        if (!IsOwnerUser(Config, user))
        {
            StatusMessage = "❌ 대표 관리자만 관리자 계정을 삭제할 수 있습니다.";
            return;
        }

        if (string.Equals(Config.OwnerUserId, userId, StringComparison.Ordinal))
        {
            StatusMessage = "❌ 대표 관리자는 삭제할 수 없습니다.";
            return;
        }

        var removed = GetAdminUsers(Config).RemoveAll(x => string.Equals(x.UserId, userId, StringComparison.Ordinal));
        if (removed == 0)
        {
            StatusMessage = "❌ 삭제할 관리자를 찾을 수 없습니다.";
            return;
        }

        await _tenants.SaveAsync(Config).ConfigureAwait(false);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
        StatusMessage = "✅ 관리자 계정이 삭제되었습니다.";
    }

    public async Task<bool> ChangePasswordAsync(string slug, string current, string next, string confirm)
    {
        if (Config == null) { StatusMessage = "❌ 설정을 먼저 불러오세요."; return false; }
        if (Hash(current) != Config.PasswordHash) { StatusMessage = "❌ 현재 비밀번호가 틀렸습니다."; return false; }
        if (next.Length < 4) { StatusMessage = "❌ 새 비밀번호는 4자 이상이어야 합니다."; return false; }
        if (next != confirm) { StatusMessage = "❌ 새 비밀번호가 일치하지 않습니다."; return false; }
        Config.PasswordHash = Hash(next);
        await _tenants.SaveAsync(Config);
        StatusMessage = "✅ 비밀번호가 변경되었습니다.";
        return true;
    }

    public async Task UploadProfileImageAsync(string slug, IBrowserFile file)
    {
        if (Config == null) return;
        IsUploading = true;
        var saved = await _media.SaveProfileImageAsync(slug, file);
        Config.ProfileImageFileName = saved;
        IsUploading = false;
    }

    // ── 연락처 메시지 ────────────────────────────────────────────
    public async Task MarkReadAsync(string slug, string msgId)
    {
        await _contacts.MarkReadAsync(slug, msgId);
        Messages = await _contacts.GetAllAsync(slug);
    }

    public async Task DeleteMessageAsync(string slug, string msgId)
    {
        await _contacts.DeleteAsync(slug, msgId);
        Messages = await _contacts.GetAllAsync(slug);
        StatusMessage = "✅ 메시지 삭제 완료.";
    }

    // ── 계정 삭제 ────────────────────────────────────────────────
    public async Task DeleteSelfAsync(string slug)
    {
        await _tenants.DeleteAsync(slug);
        IsAuthenticated = false;
    }

    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLower();

    private async Task RefreshCurrentUserAsync()
    {
        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        IsSignedIn = user.IsAuthenticated;
        CurrentUserLabel = user.IsAuthenticated
            ? string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Provider
                : $"{user.DisplayName} ({user.Provider})"
            : "";
    }

    private static List<PortfolioAdminUser> GetAdminUsers(PortfolioConfig config) => config.AdminUsers ??= [];

    private static bool IsOwnerUser(PortfolioConfig config, PortfolioCurrentUser user) =>
        user.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(config.OwnerUserId) &&
        string.Equals(config.OwnerUserId, user.Id, StringComparison.Ordinal);

    private static bool IsAdminUser(PortfolioConfig config, PortfolioCurrentUser user)
    {
        if (!user.IsAuthenticated) return false;
        if (IsOwnerUser(config, user)) return true;
        return GetAdminUsers(config).Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
    }

    private static void EnsureAdminUser(PortfolioConfig config, PortfolioCurrentUser user, string role)
    {
        if (!user.IsAuthenticated) return;

        var users = GetAdminUsers(config);
        var existing = users.FirstOrDefault(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
        if (existing is null)
        {
            users.Add(new PortfolioAdminUser
            {
                UserId = user.Id,
                Provider = user.Provider,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = role,
                AddedAt = DateTime.Now
            });
            return;
        }

        existing.Provider = user.Provider;
        existing.Email = user.Email;
        existing.DisplayName = user.DisplayName;
        if (role == "Owner")
        {
            existing.Role = "Owner";
        }
    }

    private static IReadOnlyList<PortfolioAdminUser> BuildEffectiveAdminUsers(PortfolioConfig config)
    {
        var users = GetAdminUsers(config);
        if (!string.IsNullOrWhiteSpace(config.OwnerUserId) &&
            users.All(x => !string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
        {
            users.Insert(0, new PortfolioAdminUser
            {
                UserId = config.OwnerUserId,
                Provider = config.OwnerProvider,
                Email = config.OwnerEmail,
                DisplayName = config.OwnerDisplayName,
                Role = "Owner",
                AddedAt = config.OwnerLinkedAt ?? config.CreatedAt
            });
        }

        foreach (var user in users.Where(x => string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
        {
            user.Role = "Owner";
        }

        return users
            .OrderByDescending(x => string.Equals(x.Role, "Owner", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.AddedAt)
            .ToList();
    }
}

internal static class ProjectItemExtensions
{
    internal static ProjectItem ShallowClone(this ProjectItem p) => new()
    {
        Id = p.Id, Title = p.Title, Description = p.Description,
        Tags = [.. p.Tags], Year = p.Year, Emoji = p.Emoji,
        ImageFileName = p.ImageFileName, GitHub = p.GitHub,
        LiveUrl = p.LiveUrl, DocUrl = p.DocUrl, Category = p.Category,
        SortOrder = p.SortOrder, VideoFileNames = [.. p.VideoFileNames], WorkImages = [.. p.WorkImages],
        WorkSubtitle = p.WorkSubtitle, WorkBullets = [.. p.WorkBullets],
        WorkPeriod = p.WorkPeriod, WorkRole = p.WorkRole,
        WorkTech = p.WorkTech, WorkCompany = p.WorkCompany,
    };
}
