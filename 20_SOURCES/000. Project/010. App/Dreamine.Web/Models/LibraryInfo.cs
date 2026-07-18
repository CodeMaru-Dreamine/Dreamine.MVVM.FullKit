namespace DreamineWeb.Models;

/// <summary>
/// \if KO
/// <para>Library Info 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates library info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class LibraryInfo
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Description En 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description en value.</para>
    /// \endif
    /// </summary>
    public string? DescriptionEn { get; set; }          // 영문 설명 (없으면 Description 사용)
    /// <summary>
    /// \if KO
    /// <para>Category 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the category value.</para>
    /// \endif
    /// </summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Status 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status value.</para>
    /// \endif
    /// </summary>
    public string Status { get; set; } = "stable";   // stable | beta | wip
    /// <summary>
    /// \if KO
    /// <para>Version 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the version value.</para>
    /// \endif
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    /// <summary>
    /// \if KO
    /// <para>Nu Get Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the nu get id value.</para>
    /// \endif
    /// </summary>
    public string? NuGetId { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Repo Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the repo url value.</para>
    /// \endif
    /// </summary>
    public string? RepoUrl { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Xml Doc Path 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the xml doc path value.</para>
    /// \endif
    /// </summary>
    public string? XmlDocPath { get; set; }           // 빌드 출력 .xml 경로
    /// <summary>
    /// \if KO
    /// <para>Source Project Path 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the source project path value.</para>
    /// \endif
    /// </summary>
    public string? SourceProjectPath { get; set; }    // 원본 .csproj 경로
    /// <summary>
    /// \if KO
    /// <para>Target Framework 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the target framework value.</para>
    /// \endif
    /// </summary>
    public string? TargetFramework { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Dependencies 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the dependencies value.</para>
    /// \endif
    /// </summary>
    public string[] Dependencies { get; set; } = [];
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
    /// <para>Sort Order 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the sort order value.</para>
    /// \endif
    /// </summary>
    public int SortOrder { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Is Visible 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is visible value.</para>
    /// \endif
    /// </summary>
    public bool IsVisible { get; set; } = true;
    /// <summary>
    /// \if KO
    /// <para>Updated At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the updated at value.</para>
    /// \endif
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
