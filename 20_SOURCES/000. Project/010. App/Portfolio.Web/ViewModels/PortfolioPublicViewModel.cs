using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Portfolio Public View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates portfolio public view model functionality and related state.</para>
/// \endif
/// </summary>
public class PortfolioPublicViewModel
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
    /// <para>Has Loaded 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the has loaded value.</para>
    /// \endif
    /// </summary>
    public bool HasLoaded { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; set; } = "";

    // 연락처 폼
    /// <summary>
    /// \if KO
    /// <para>Contact Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the contact name value.</para>
    /// \endif
    /// </summary>
    public string ContactName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Contact Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the contact email value.</para>
    /// \endif
    /// </summary>
    public string ContactEmail { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Contact Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the contact message value.</para>
    /// \endif
    /// </summary>
    public string ContactMessage { get; set; } = "";

    // 필터/검색
    /// <summary>
    /// \if KO
    /// <para>Query 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the query value.</para>
    /// \endif
    /// </summary>
    public string Query { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Selected Tag 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected tag value.</para>
    /// \endif
    /// </summary>
    public string SelectedTag { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Selected Category 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected category value.</para>
    /// \endif
    /// </summary>
    public string SelectedCategory { get; set; } = "";

    /// <summary>
    /// \if KO
    /// <para>Personal 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the personal value.</para>
    /// \endif
    /// </summary>
    public List<ProjectItem> Personal => FilteredProjects.Where(p => p.Category == ProjectCategory.Personal).ToList();
    /// <summary>
    /// \if KO
    /// <para>Work 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the work value.</para>
    /// \endif
    /// </summary>
    public List<ProjectItem> Work => FilteredProjects.Where(p => p.Category == ProjectCategory.Work).ToList();
    /// <summary>
    /// \if KO
    /// <para>Public 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the public value.</para>
    /// \endif
    /// </summary>
    public List<ProjectItem> Public => FilteredProjects.Where(p => p.Category == ProjectCategory.Public).ToList();

    /// <summary>
    /// \if KO
    /// <para>All Tags 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all tags value.</para>
    /// \endif
    /// </summary>
    public IEnumerable<string> AllTags => Projects
        .SelectMany(p => p.Tags)
        .Distinct()
        .OrderBy(t => t);

    /// <summary>
    /// \if KO
    /// <para>Filtered Projects 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the filtered projects value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PortfolioPublicViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PortfolioPublicViewModel"/> class with the specified settings.</para>
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
        HasLoaded = false;
        Config = null;
        Projects = [];
        Resume = new();

        Config = await _tenants.GetAsync(slug);
        if (Config == null)
        {
            HasLoaded = true;
            return;
        }

        Projects = await _projects.GetAllAsync(slug);
        Resume = await _resumes.GetAsync(slug);
        // 기존 데이터 마이그레이션: ImageFileName → WorkImages (표시 전용, 저장 안 함)
        foreach (var p in Projects.Where(p =>
            !string.IsNullOrWhiteSpace(p.ImageFileName) && p.WorkImages.Length == 0))
            p.WorkImages = [p.ImageFileName!];

        HasLoaded = true;
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
    /// <para>Is You Tube 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is you tube.</para>
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
    /// <para>Is You Tube 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is you tube condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public static bool IsYouTube(string url) =>
        url.Contains("youtube.com") || url.Contains("youtu.be");

    /// <summary>
    /// \if KO
    /// <para>You Tube Embed Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the you tube embed url value.</para>
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
    /// <para>Get You Tube Embed Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get you tube embed url operation.</para>
    /// \endif
    /// </returns>
    public static string GetYouTubeEmbedUrl(string url)
    {
        // youtu.be/ID 또는 youtube.com/watch?v=ID → /embed/ID
        var id = "";
        if (url.Contains("youtu.be/"))
            id = url.Split("youtu.be/")[1].Split('?')[0].Split('&')[0];
        else if (url.Contains("v="))
            id = url.Split("v=")[1].Split('&')[0].Split('?')[0];
        return string.IsNullOrWhiteSpace(id) ? url : $"https://www.youtube.com/embed/{id}";
    }

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

    /// <summary>
    /// \if KO
    /// <para>Send Contact Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send contact async operation.</para>
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
    /// <para>Send Contact Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the send contact async operation.</para>
    /// \endif
    /// </returns>
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
