namespace PortfolioApp.Models;

/// <summary>
/// \if KO
/// <para>Project Category 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates project category functionality and related state.</para>
/// \endif
/// </summary>
public enum ProjectCategory
{
    /// <summary>
    /// \if KO
    /// <para>개인 프로젝트를 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a personal project.</para>
    /// \endif
    /// </summary>
    Personal,

    /// <summary>
    /// \if KO
    /// <para>업무 프로젝트를 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a work project.</para>
    /// \endif
    /// </summary>
    Work,

    /// <summary>
    /// \if KO
    /// <para>공개 프로젝트를 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents a public project.</para>
    /// \endif
    /// </summary>
    Public
}

/// <summary>
/// \if KO
/// <para>Project Item 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates project item functionality and related state.</para>
/// \endif
/// </summary>
public class ProjectItem
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// \if KO
    /// <para>Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title value.</para>
    /// \endif
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Tags 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tags value.</para>
    /// \endif
    /// </summary>
    public string[] Tags { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Year 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the year value.</para>
    /// \endif
    /// </summary>
    public int Year { get; set; } = DateTime.Now.Year;
    /// <summary>
    /// \if KO
    /// <para>Emoji 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the emoji value.</para>
    /// \endif
    /// </summary>
    public string Emoji { get; set; } = "🛠️";
    /// <summary>
    /// \if KO
    /// <para>Image File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image file name value.</para>
    /// \endif
    /// </summary>
    public string? ImageFileName { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Git Hub 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the git hub value.</para>
    /// \endif
    /// </summary>
    public string? GitHub { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Live Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the live url value.</para>
    /// \endif
    /// </summary>
    public string? LiveUrl { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Doc Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the doc url value.</para>
    /// \endif
    /// </summary>
    public string? DocUrl { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Category 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the category value.</para>
    /// \endif
    /// </summary>
    public ProjectCategory Category { get; set; } = ProjectCategory.Personal;
    /// <summary>
    /// \if KO
    /// <para>Sort Order 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the sort order value.</para>
    /// \endif
    /// </summary>
    public int SortOrder { get; set; } = 0;

    // 이미지 블러 (0=없음, 1=약하게 4px, 2=중간 8px, 3=강하게 16px) — 사내 로고 등 법적 보호
    /// <summary>
    /// \if KO
    /// <para>Image Blur Level 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image blur level value.</para>
    /// \endif
    /// </summary>
    public int ImageBlurLevel { get; set; } = 0;

    // 미디어
    /// <summary>
    /// \if KO
    /// <para>Video File Names 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video file names value.</para>
    /// \endif
    /// </summary>
    public string[] VideoFileNames { get; set; } = [];

    // Work 전용
    /// <summary>
    /// \if KO
    /// <para>Work Images 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work images value.</para>
    /// \endif
    /// </summary>
    public string[] WorkImages { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Work Subtitle 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work subtitle value.</para>
    /// \endif
    /// </summary>
    public string? WorkSubtitle { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Work Bullets 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work bullets value.</para>
    /// \endif
    /// </summary>
    public string[] WorkBullets { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Work Period 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work period value.</para>
    /// \endif
    /// </summary>
    public string? WorkPeriod { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Work Role 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work role value.</para>
    /// \endif
    /// </summary>
    public string? WorkRole { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Work Tech 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work tech value.</para>
    /// \endif
    /// </summary>
    public string? WorkTech { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Work Company 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the work company value.</para>
    /// \endif
    /// </summary>
    public string? WorkCompany { get; set; }
}
