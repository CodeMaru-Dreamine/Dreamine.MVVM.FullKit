namespace DreamineWeb.Models;

/// <summary>
/// \if KO
/// <para>Site Settings 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates site settings functionality and related state.</para>
/// \endif
/// </summary>
public class SiteSettings
{
    /// <summary>
    /// \if KO
    /// <para>Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title value.</para>
    /// \endif
    /// </summary>
    public string Title       { get; set; } = "Dreamine";
    /// <summary>
    /// \if KO
    /// <para>Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string Description { get; set; } = "Modular Architecture for Real Applications";
    /// <summary>
    /// \if KO
    /// <para>Icon Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the icon url value.</para>
    /// \endif
    /// </summary>
    public string IconUrl     { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Git Hub Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the git hub url value.</para>
    /// \endif
    /// </summary>
    public string GitHubUrl   { get; set; } = "https://github.com/CodeMaru-Dreamine";

    // Open Graph / SNS 공유
    /// <summary>
    /// \if KO
    /// <para>Og Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og title value.</para>
    /// \endif
    /// </summary>
    public string OgTitle       { get; set; } = "";   // 비면 Title 사용
    /// <summary>
    /// \if KO
    /// <para>Og Description 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og description value.</para>
    /// \endif
    /// </summary>
    public string OgDescription { get; set; } = "";   // 비면 Description 사용
    /// <summary>
    /// \if KO
    /// <para>Og Image Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og image url value.</para>
    /// \endif
    /// </summary>
    public string OgImageUrl    { get; set; } = "/img/og-dreamine.png";
    /// <summary>
    /// \if KO
    /// <para>Og Site Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og site name value.</para>
    /// \endif
    /// </summary>
    public string OgSiteName    { get; set; } = "Dreamine";
    /// <summary>
    /// \if KO
    /// <para>Og Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og url value.</para>
    /// \endif
    /// </summary>
    public string OgUrl         { get; set; } = "https://dreamine.kr/";
    /// <summary>
    /// \if KO
    /// <para>Twitter Card 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the twitter card value.</para>
    /// \endif
    /// </summary>
    public string TwitterCard   { get; set; } = "summary_large_image";
}
