using System.IO;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Forms;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Portfolio Admin View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates portfolio admin view model functionality and related state.</para>
/// \endif
/// </summary>
public class PortfolioAdminViewModel
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
    /// <para>resumes 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the resumes value.</para>
    /// \endif
    /// </summary>
    private readonly IResumeStore _resumes;
    /// <summary>
    /// \if KO
    /// <para>contacts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the contacts value.</para>
    /// \endif
    /// </summary>
    private readonly IContactStore _contacts;
    /// <summary>
    /// \if KO
    /// <para>media 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the media value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaService _media;
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
    /// <para>Is Authenticated 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is authenticated value.</para>
    /// \endif
    /// </summary>
    public bool IsAuthenticated { get; private set; }
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
    /// <para>Is Signed In 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is signed in value.</para>
    /// \endif
    /// </summary>
    public bool IsSignedIn { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Linked To Current User 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is linked to current user value.</para>
    /// \endif
    /// </summary>
    public bool IsLinkedToCurrentUser { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Owner 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is owner value.</para>
    /// \endif
    /// </summary>
    public bool IsOwner { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Effective Admin Users 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the effective admin users value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<PortfolioAdminUser> EffectiveAdminUsers { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Current User Label 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the current user label value.</para>
    /// \endif
    /// </summary>
    public string CurrentUserLabel { get; private set; } = "";
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
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Is Uploading 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is uploading value.</para>
    /// \endif
    /// </summary>
    public bool IsUploading { get; set; }

    /// <summary>
    /// \if KO
    /// <para>Config 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the config value.</para>
    /// \endif
    /// </summary>
    public PortfolioConfig? Config { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Projects 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the projects value.</para>
    /// \endif
    /// </summary>
    public List<ProjectItem> Projects { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Resume 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the resume value.</para>
    /// \endif
    /// </summary>
    public ResumeInfo Resume { get; private set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Messages 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the messages value.</para>
    /// \endif
    /// </summary>
    public List<ContactMessage> Messages { get; private set; } = [];

    // 프로젝트 편집
    /// <summary>
    /// \if KO
    /// <para>Editing Project 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the editing project value.</para>
    /// \endif
    /// </summary>
    public ProjectItem? EditingProject { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Pending Files 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the pending files value.</para>
    /// \endif
    /// </summary>
    public List<IBrowserFile> PendingFiles { get; } = [];
    /// <summary>
    /// \if KO
    /// <para>Pending Videos 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the pending videos value.</para>
    /// \endif
    /// </summary>
    public List<IBrowserFile> PendingVideos { get; } = [];

    // 새 스킬 그룹 편집
    /// <summary>
    /// \if KO
    /// <para>New Skill Category 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new skill category value.</para>
    /// \endif
    /// </summary>
    public string NewSkillCategory { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>New Skill Names 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new skill names value.</para>
    /// \endif
    /// </summary>
    public string NewSkillNames { get; set; } = "";

    // 경력 편집
    /// <summary>
    /// \if KO
    /// <para>Editing Exp 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the editing exp value.</para>
    /// \endif
    /// </summary>
    public ExperienceItem? EditingExp { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Editing Edu 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the editing edu value.</para>
    /// \endif
    /// </summary>
    public EducationItem? EditingEdu { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PortfolioAdminViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PortfolioAdminViewModel"/> class with the specified settings.</para>
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
    /// <param name="resumes">
    /// \if KO
    /// <para>resumes에 사용할 <c>IResumeStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IResumeStore</c> value used for resumes.</para>
    /// \endif
    /// </param>
    /// <param name="contacts">
    /// \if KO
    /// <para>contacts에 사용할 <c>IContactStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IContactStore</c> value used for contacts.</para>
    /// \endif
    /// </param>
    /// <param name="media">
    /// \if KO
    /// <para>media에 사용할 <c>IMediaService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaService</c> value used for media.</para>
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

    /// <summary>
    /// \if KO
    /// <para>Initialize Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize async operation.</para>
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
    /// <para>Initialize Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the initialize async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Login Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login async operation.</para>
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
    /// <para>Login Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the login async operation.</para>
    /// \endif
    /// </returns>
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

        var verification = DreaminePasswordHasher.VerifyPassword(LoginPassword, cfg.PasswordHash, out var upgradedHash);
        if (verification is PasswordHashVerificationResult.Failed) { StatusMessage = "❌ 비밀번호가 틀렸습니다."; return false; }
        IsAuthenticated = true;

        if (verification is PasswordHashVerificationResult.SuccessRehashNeeded && upgradedHash is not null)
        {
            cfg.PasswordHash = upgradedHash;
            await _tenants.SaveAsync(cfg).ConfigureAwait(false);
        }

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

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
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
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
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
    /// <summary>
    /// \if KO
    /// <para>Logout 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the logout operation.</para>
    /// \endif
    /// </summary>
    public void Logout() { IsAuthenticated = false; IsLoaded = false; LoginPassword = ""; StatusMessage = ""; EditingProject = null; }

    /// <summary>
    /// \if KO
    /// <para>New Project 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the new project operation.</para>
    /// \endif
    /// </summary>
    public void NewProject() => EditingProject = new ProjectItem();
    /// <summary>
    /// \if KO
    /// <para>Edit Project 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the edit project operation.</para>
    /// \endif
    /// </summary>
    /// <param name="p">
    /// \if KO
    /// <para>p에 사용할 <c>ProjectItem</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProjectItem</c> value used for p.</para>
    /// \endif
    /// </param>
    public void EditProject(ProjectItem p) { EditingProject = p.ShallowClone(); PendingFiles.Clear(); PendingVideos.Clear(); }
    /// <summary>
    /// \if KO
    /// <para>Cancel Edit 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel edit.</para>
    /// \endif
    /// </summary>
    public void CancelEdit() { EditingProject = null; PendingFiles.Clear(); PendingVideos.Clear(); }

    /// <summary>
    /// \if KO
    /// <para>Project Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves project async data.</para>
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
    /// <para>Save Project Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save project async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Project Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete project async operation.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Project Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete project async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteProjectAsync(string slug, string projectId)
    {
        await _projects.DeleteAsync(slug, projectId);
        Projects = await _projects.GetAllAsync(slug);
        StatusMessage = "✅ 삭제 완료.";
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Project Media Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete project media async operation.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Project Media Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete project media async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteProjectMediaAsync(string slug, string projectId, string fileName)
    {
        await _media.DeleteAsync(slug, projectId, fileName);
        if (EditingProject != null)
            EditingProject.WorkImages = EditingProject.WorkImages.Where(f => f != fileName).ToArray();
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Project Cover Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete project cover async operation.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Project Cover Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete project cover async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteProjectCoverAsync(string slug, string projectId)
    {
        if (EditingProject == null || string.IsNullOrWhiteSpace(EditingProject.ImageFileName)) return;
        var fn = EditingProject.ImageFileName;
        if (!fn.StartsWith('/'))
            await _media.DeleteAsync(slug, projectId, fn);
        EditingProject.ImageFileName = null;
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Project Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete project video async operation.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Project Video Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete project video async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteProjectVideoAsync(string slug, string projectId, string fileName)
    {
        await _media.DeleteAsync(slug, projectId, fileName);
        if (EditingProject != null)
            EditingProject.VideoFileNames = EditingProject.VideoFileNames.Where(f => f != fileName).ToArray();
    }

    /// <summary>
    /// \if KO
    /// <para>Media Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the media url value.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Media Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get media url operation.</para>
    /// \endif
    /// </returns>
    public string GetMediaUrl(string slug, string projectId, string fileName) =>
        _media.GetMediaUrl(slug, projectId, fileName);

    /// <summary>
    /// \if KO
    /// <para>Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the image url value.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Image Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get image url operation.</para>
    /// \endif
    /// </returns>
    public string GetImageUrl(string slug, string projectId, string fileName) =>
        IsExternalUrl(fileName) ? fileName : _media.GetMediaUrl(slug, projectId, fileName);

    /// <summary>
    /// \if KO
    /// <para>Video Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video url value.</para>
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Video Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get video url operation.</para>
    /// \endif
    /// </returns>
    public string GetVideoUrl(string slug, string projectId, string fileName) =>
        IsExternalUrl(fileName) ? fileName : _media.GetMediaUrl(slug, projectId, fileName);

    /// <summary>
    /// \if KO
    /// <para>Is External Url 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is external url.</para>
    /// \endif
    /// </summary>
    /// <param name="url">
    /// \if KO
    /// <para>url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for url.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is External Url 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is external url condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsExternalUrl(string url) =>
        url.StartsWith('/') || url.StartsWith("http://") || url.StartsWith("https://");

    /// <summary>
    /// \if KO
    /// <para>Profile Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the profile image url value.</para>
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
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Profile Image Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get profile image url operation.</para>
    /// \endif
    /// </returns>
    public string GetProfileImageUrl(string slug, string fileName) =>
        _media.GetProfileImageUrl(slug, fileName);

    // ── Resume ──────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Resume Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves resume async data.</para>
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
    /// <para>Save Resume Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save resume async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveResumeAsync(string slug)
    {
        await _resumes.SaveAsync(slug, Resume);
        StatusMessage = "✅ Resume 저장 완료.";
    }

    /// <summary>
    /// \if KO
    /// <para>Skill Group 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the skill group item.</para>
    /// \endif
    /// </summary>
    public void AddSkillGroup()
    {
        if (string.IsNullOrWhiteSpace(NewSkillCategory)) return;
        var skills = NewSkillNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        Resume.SkillGroups.Add(new SkillGroup { Category = NewSkillCategory.Trim(), Skills = skills });
        NewSkillCategory = "";
        NewSkillNames = "";
    }

    /// <summary>
    /// \if KO
    /// <para>Skill Group 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the skill group item.</para>
    /// \endif
    /// </summary>
    /// <param name="g">
    /// \if KO
    /// <para>g에 사용할 <c>SkillGroup</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SkillGroup</c> value used for g.</para>
    /// \endif
    /// </param>
    public void RemoveSkillGroup(SkillGroup g) => Resume.SkillGroups.Remove(g);

    /// <summary>
    /// \if KO
    /// <para>New Experience 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the new experience operation.</para>
    /// \endif
    /// </summary>
    public void NewExperience() => EditingExp = new ExperienceItem();
    /// <summary>
    /// \if KO
    /// <para>Edit Experience 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the edit experience operation.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    public void EditExperience(ExperienceItem e) => EditingExp = e;
    /// <summary>
    /// \if KO
    /// <para>Cancel Exp 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel exp.</para>
    /// \endif
    /// </summary>
    public void CancelExp() => EditingExp = null;
    /// <summary>
    /// \if KO
    /// <para>Experience 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves experience data.</para>
    /// \endif
    /// </summary>
    public void SaveExperience()
    {
        if (EditingExp == null) return;
        if (!Resume.Experiences.Contains(EditingExp))
            Resume.Experiences.Add(EditingExp);
        EditingExp = null;
    }
    /// <summary>
    /// \if KO
    /// <para>Experience 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the experience item.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    public void RemoveExperience(ExperienceItem e) => Resume.Experiences.Remove(e);

    /// <summary>
    /// \if KO
    /// <para>New Education 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the new education operation.</para>
    /// \endif
    /// </summary>
    public void NewEducation() => EditingEdu = new EducationItem();
    /// <summary>
    /// \if KO
    /// <para>Edit Education 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the edit education operation.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    public void EditEducation(EducationItem e) => EditingEdu = e;
    /// <summary>
    /// \if KO
    /// <para>Cancel Edu 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel edu.</para>
    /// \endif
    /// </summary>
    public void CancelEdu() => EditingEdu = null;
    /// <summary>
    /// \if KO
    /// <para>Education 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves education data.</para>
    /// \endif
    /// </summary>
    public void SaveEducation()
    {
        if (EditingEdu == null) return;
        if (!Resume.Educations.Contains(EditingEdu))
            Resume.Educations.Add(EditingEdu);
        EditingEdu = null;
    }
    /// <summary>
    /// \if KO
    /// <para>Education 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the education item.</para>
    /// \endif
    /// </summary>
    /// <param name="e">
    /// \if KO
    /// <para>이벤트와 관련된 데이터를 포함합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Contains data associated with the event.</para>
    /// \endif
    /// </param>
    public void RemoveEducation(EducationItem e) => Resume.Educations.Remove(e);

    // ── 설정 ────────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Config Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves config async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Config Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save config async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveConfigAsync()
    {
        if (Config == null) return;
        Config.PasswordHash = DreaminePasswordHasher.HashPlainTextForStorage(Config.PasswordHash);
        await _tenants.SaveAsync(Config);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
        StatusMessage = "✅ 설정이 저장되었습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Admin Async 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the admin async item.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Remove Admin Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the remove admin async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Change Password Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the change password async operation.</para>
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
    /// <param name="current">
    /// \if KO
    /// <para>current에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for current.</para>
    /// \endif
    /// </param>
    /// <param name="next">
    /// \if KO
    /// <para>next에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for next.</para>
    /// \endif
    /// </param>
    /// <param name="confirm">
    /// \if KO
    /// <para>confirm에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for confirm.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Change Password Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the change password async operation.</para>
    /// \endif
    /// </returns>
    public async Task<bool> ChangePasswordAsync(string slug, string current, string next, string confirm)
    {
        if (Config == null) { StatusMessage = "❌ 설정을 먼저 불러오세요."; return false; }
        if (!DreaminePasswordHasher.VerifyPassword(current, Config.PasswordHash)) { StatusMessage = "❌ 현재 비밀번호가 틀렸습니다."; return false; }
        if (next.Length < 8) { StatusMessage = "❌ 새 비밀번호는 8자 이상이어야 합니다."; return false; }
        if (next != confirm) { StatusMessage = "❌ 새 비밀번호가 일치하지 않습니다."; return false; }
        Config.PasswordHash = DreaminePasswordHasher.HashPassword(next);
        await _tenants.SaveAsync(Config);
        StatusMessage = "✅ 비밀번호가 변경되었습니다.";
        return true;
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Profile Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload profile image async operation.</para>
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
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Upload Profile Image Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload profile image async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadProfileImageAsync(string slug, IBrowserFile file)
    {
        if (Config == null) return;
        IsUploading = true;
        var saved = await _media.SaveProfileImageAsync(slug, file);
        Config.ProfileImageFileName = saved;
        IsUploading = false;
    }

    // ── 연락처 메시지 ────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Mark Read Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the mark read async operation.</para>
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
    /// <param name="msgId">
    /// \if KO
    /// <para>msg Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for msg id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Mark Read Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the mark read async operation.</para>
    /// \endif
    /// </returns>
    public async Task MarkReadAsync(string slug, string msgId)
    {
        await _contacts.MarkReadAsync(slug, msgId);
        Messages = await _contacts.GetAllAsync(slug);
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Message Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete message async operation.</para>
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
    /// <param name="msgId">
    /// \if KO
    /// <para>msg Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for msg id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Message Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete message async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteMessageAsync(string slug, string msgId)
    {
        await _contacts.DeleteAsync(slug, msgId);
        Messages = await _contacts.GetAllAsync(slug);
        StatusMessage = "✅ 메시지 삭제 완료.";
    }

    // ── 계정 삭제 ────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Delete Self Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete self async operation.</para>
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
    /// <para>Delete Self Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete self async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteSelfAsync(string slug)
    {
        await _tenants.DeleteAsync(slug);
        IsAuthenticated = false;
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh Current User Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh current user async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Refresh Current User Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the refresh current user async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Admin Users 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the admin users value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Admin Users 작업에서 생성한 <c>List&lt;PortfolioAdminUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;PortfolioAdminUser&gt;</c> result produced by the get admin users operation.</para>
    /// \endif
    /// </returns>
    private static List<PortfolioAdminUser> GetAdminUsers(PortfolioConfig config) => config.AdminUsers ??= [];

    /// <summary>
    /// \if KO
    /// <para>Is Owner User 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is owner user.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>PortfolioCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Owner User 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is owner user condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsOwnerUser(PortfolioConfig config, PortfolioCurrentUser user) =>
        user.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(config.OwnerUserId) &&
        string.Equals(config.OwnerUserId, user.Id, StringComparison.Ordinal);

    /// <summary>
    /// \if KO
    /// <para>Is Admin User 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is admin user.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>PortfolioCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Admin User 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is admin user condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsAdminUser(PortfolioConfig config, PortfolioCurrentUser user)
    {
        if (!user.IsAuthenticated) return false;
        if (IsOwnerUser(config, user)) return true;
        return GetAdminUsers(config).Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
    }

    /// <summary>
    /// \if KO
    /// <para>Ensure Admin User 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure admin user operation.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>PortfolioCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <param name="role">
    /// \if KO
    /// <para>role에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for role.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Effective Admin Users 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the effective admin users value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Effective Admin Users 작업에서 생성한 <c>IReadOnlyList&lt;PortfolioAdminUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;PortfolioAdminUser&gt;</c> result produced by the build effective admin users operation.</para>
    /// \endif
    /// </returns>
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

/// <summary>
/// \if KO
/// <para>Project Item Extensions 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates project item extensions functionality and related state.</para>
/// \endif
/// </summary>
internal static class ProjectItemExtensions
{
    /// <summary>
    /// \if KO
    /// <para>Shallow Clone 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the shallow clone operation.</para>
    /// \endif
    /// </summary>
    /// <param name="p">
    /// \if KO
    /// <para>p에 사용할 <c>ProjectItem</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProjectItem</c> value used for p.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Shallow Clone 작업에서 생성한 <c>ProjectItem</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProjectItem</c> result produced by the shallow clone operation.</para>
    /// \endif
    /// </returns>
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
