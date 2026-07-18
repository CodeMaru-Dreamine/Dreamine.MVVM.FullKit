namespace DreamineWeb.Models;

/// <summary>
/// \if KO
/// <para>문서 허브에 표시할 프로젝트별 문서와 지식 그래프 링크를 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Represents per-project documentation and knowledge-graph links shown in the documentation hub.</para>
/// \endif
/// </summary>
public sealed class DocumentationProjectInfo
{
    /// <summary>
    /// \if KO
    /// <para>URL에 사용하는 프로젝트 식별자를 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the project identifier used in URLs.</para>
    /// \endif
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>프로젝트 표시 이름을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the project display name.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>솔루션 분류 이름을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the solution category name.</para>
    /// \endif
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>프로젝트 버전을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the project version.</para>
    /// \endif
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>대상 프레임워크 목록을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the target framework list.</para>
    /// \endif
    /// </summary>
    public string[] TargetFrameworks { get; set; } = [];

    /// <summary>
    /// \if KO
    /// <para>언어별 프로젝트 설명을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets localized project descriptions.</para>
    /// \endif
    /// </summary>
    public Dictionary<string, string> Descriptions { get; set; } = [];

    /// <summary>
    /// \if KO
    /// <para>컴파일러가 생성한 XML 문서 URL을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the compiler-generated XML documentation URL.</para>
    /// \endif
    /// </summary>
    public string? BuildDocumentUrl { get; set; }

    /// <summary>
    /// \if KO
    /// <para>언어별 Doxygen 문서 URL을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets localized Doxygen documentation URLs.</para>
    /// \endif
    /// </summary>
    public Dictionary<string, string> DoxygenUrls { get; set; } = [];

    /// <summary>
    /// \if KO
    /// <para>언어별 프로젝트 지식 그래프 URL을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets localized project knowledge-graph URLs.</para>
    /// \endif
    /// </summary>
    public Dictionary<string, string> GraphUrls { get; set; } = [];

    /// <summary>
    /// \if KO
    /// <para>요청한 언어의 설명을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Returns the description for the requested language.</para>
    /// \endif
    /// </summary>
    /// <param name="language">
    /// \if KO
    /// <para><c>ko</c> 또는 <c>en</c> 언어 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ko</c> or <c>en</c> language code.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>선택 언어의 설명이며, 없으면 프로젝트 이름입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The localized description, or the project name when unavailable.</para>
    /// \endif
    /// </returns>
    public string GetDescription(string language) =>
        Descriptions.TryGetValue(language, out string? value) ? value : Name;

    /// <summary>
    /// \if KO
    /// <para>요청한 언어의 Doxygen URL을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Returns the Doxygen URL for the requested language.</para>
    /// \endif
    /// </summary>
    /// <param name="language">
    /// \if KO
    /// <para><c>ko</c> 또는 <c>en</c> 언어 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ko</c> or <c>en</c> language code.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Doxygen 문서 URL이며, 없으면 빈 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The Doxygen documentation URL, or an empty string when unavailable.</para>
    /// \endif
    /// </returns>
    public string GetDoxygenUrl(string language) =>
        DoxygenUrls.TryGetValue(language, out string? value) ? value : string.Empty;

    /// <summary>
    /// \if KO
    /// <para>요청한 언어의 지식 그래프 URL을 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Returns the knowledge-graph URL for the requested language.</para>
    /// \endif
    /// </summary>
    /// <param name="language">
    /// \if KO
    /// <para><c>ko</c> 또는 <c>en</c> 언어 코드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ko</c> or <c>en</c> language code.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>지식 그래프 URL이며, 없으면 빈 문자열입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The knowledge-graph URL, or an empty string when unavailable.</para>
    /// \endif
    /// </returns>
    public string GetGraphUrl(string language) =>
        GraphUrls.TryGetValue(language, out string? value) ? value : string.Empty;
}

/// <summary>
/// \if KO
/// <para>프로젝트 문서 카탈로그 파일의 루트 모델입니다.</para>
/// \endif
/// \if EN
/// <para>Represents the root model of the project documentation catalog file.</para>
/// \endif
/// </summary>
public sealed class DocumentationProjectCatalog
{
    /// <summary>
    /// \if KO
    /// <para>카탈로그 생성 시각을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the catalog generation timestamp.</para>
    /// \endif
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>
    /// \if KO
    /// <para>프로젝트 목록을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the project list.</para>
    /// \endif
    /// </summary>
    public List<DocumentationProjectInfo> Projects { get; set; } = [];
}
