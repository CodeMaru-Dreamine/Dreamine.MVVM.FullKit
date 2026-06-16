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

    public bool IsAuthenticated { get; private set; }
    public bool IsLoaded { get; private set; }
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
        IMediaService media)
    {
        _tenants = tenants;
        _projects = projects;
        _resumes = resumes;
        _contacts = contacts;
        _media = media;
    }

    public async Task<bool> LoginAsync(string slug)
    {
        var cfg = await _tenants.GetAsync(slug);
        if (cfg == null) { StatusMessage = "❌ 존재하지 않는 포트폴리오입니다."; return false; }
        if (Hash(LoginPassword) != cfg.PasswordHash) { StatusMessage = "❌ 비밀번호가 틀렸습니다."; return false; }
        IsAuthenticated = true;
        StatusMessage = "";
        return true;
    }

    public async Task LoadAsync(string slug)
    {
        Config = await _tenants.GetAsync(slug);
        if (Config == null) return;
        Projects = await _projects.GetAllAsync(slug);
        Resume = await _resumes.GetAsync(slug);
        Messages = await _contacts.GetAllAsync(slug);
        IsLoaded = true;
    }

    // ── 프로젝트 ────────────────────────────────────────────────
    public void NewProject() => EditingProject = new ProjectItem();
    public void EditProject(ProjectItem p) { EditingProject = p.ShallowClone(); PendingFiles.Clear(); }
    public void CancelEdit() { EditingProject = null; PendingFiles.Clear(); }

    public async Task SaveProjectAsync(string slug)
    {
        if (EditingProject == null) return;
        StatusMessage = "";
        if (string.IsNullOrWhiteSpace(EditingProject.Title)) { StatusMessage = "❌ 제목을 입력하세요."; return; }

        if (PendingFiles.Count > 0)
        {
            IsUploading = true;
            foreach (var f in PendingFiles)
            {
                var saved = await _media.SaveAsync(slug, EditingProject.Id, f);
                if (!EditingProject.WorkImages.Contains(saved))
                    EditingProject.WorkImages = [.. EditingProject.WorkImages, saved];
                if (EditingProject.ImageFileName == null)
                    EditingProject.ImageFileName = saved;
            }
            PendingFiles.Clear();
            IsUploading = false;
        }

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

    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        _media.GetMediaUrl(slug, projectId, fileName);

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
        StatusMessage = "✅ 설정이 저장되었습니다.";
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
}

internal static class ProjectItemExtensions
{
    internal static ProjectItem ShallowClone(this ProjectItem p) => new()
    {
        Id = p.Id, Title = p.Title, Description = p.Description,
        Tags = [.. p.Tags], Year = p.Year, Emoji = p.Emoji,
        ImageFileName = p.ImageFileName, GitHub = p.GitHub,
        LiveUrl = p.LiveUrl, DocUrl = p.DocUrl, Category = p.Category,
        SortOrder = p.SortOrder, WorkImages = [.. p.WorkImages],
        WorkSubtitle = p.WorkSubtitle, WorkBullets = [.. p.WorkBullets],
        WorkPeriod = p.WorkPeriod, WorkRole = p.WorkRole,
        WorkTech = p.WorkTech, WorkCompany = p.WorkCompany,
    };
}
