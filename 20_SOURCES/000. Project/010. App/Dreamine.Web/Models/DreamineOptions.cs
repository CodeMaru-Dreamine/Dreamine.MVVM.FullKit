using System.IO;

namespace DreamineWeb.Models;

/// <summary>
/// \if KO
/// <para>Dreamine Options 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates dreamine options functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DreamineOptions
{
    /// <summary>
    /// \if KO
    /// <para>Data Path 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the data path value.</para>
    /// \endif
    /// </summary>
    public string DataPath { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Super Admin Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the super admin password value.</para>
    /// \endif
    /// </summary>
    public string SuperAdminPassword { get; set; } = "v1.600000.1oy6GjdnZFCRZpxUY6R4tQ==.1r45CdqBw/2U22r0bF9KwzdIfyCVkkRt/VcoAg19LrQ=";
    /// <summary>
    /// \if KO
    /// <para>Site Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the site title value.</para>
    /// \endif
    /// </summary>
    public string SiteTitle { get; set; } = "Dreamine";
    /// <summary>
    /// \if KO
    /// <para>Site Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the site description value.</para>
    /// \endif
    /// </summary>
    public string SiteDescription { get; set; } = "Modular Architecture for Real Applications";
    /// <summary>
    /// \if KO
    /// <para>Git Hub Org Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the git hub org url value.</para>
    /// \endif
    /// </summary>
    public string GitHubOrgUrl { get; set; } = "https://github.com/CodeMaru-Dreamine";

    /// <summary>
    /// \if KO
    /// <para>라이브러리 소스 루트(버전 자동 동기화용)입니다. 예: <c>D:/Work/.../100. Library</c></para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the library source root value.</para>
    /// \endif
    /// </summary>
    public string LibrarySourceRoot { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Resolved Data Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the resolved data path value.</para>
    /// \endif
    /// </summary>
    public string ResolvedDataPath =>
        string.IsNullOrWhiteSpace(DataPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data")
            : DataPath;
}
