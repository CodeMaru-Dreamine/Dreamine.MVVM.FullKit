namespace PortfolioApp.Models;

/// <summary>
/// \if KO
/// <para>Resume Info 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates resume info functionality and related state.</para>
/// \endif
/// </summary>
public class ResumeInfo
{
    /// <summary>
    /// \if KO
    /// <para>Skill Groups 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the skill groups value.</para>
    /// \endif
    /// </summary>
    public List<SkillGroup> SkillGroups { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Experiences 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the experiences value.</para>
    /// \endif
    /// </summary>
    public List<ExperienceItem> Experiences { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Educations 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the educations value.</para>
    /// \endif
    /// </summary>
    public List<EducationItem> Educations { get; set; } = [];
}

/// <summary>
/// \if KO
/// <para>Skill Group 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates skill group functionality and related state.</para>
/// \endif
/// </summary>
public class SkillGroup
{
    /// <summary>
    /// \if KO
    /// <para>Category 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the category value.</para>
    /// \endif
    /// </summary>
    public string Category { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Skills 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the skills value.</para>
    /// \endif
    /// </summary>
    public List<string> Skills { get; set; } = [];
}

/// <summary>
/// \if KO
/// <para>Experience Item 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates experience item functionality and related state.</para>
/// \endif
/// </summary>
public class ExperienceItem
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
    /// <para>Company 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the company value.</para>
    /// \endif
    /// </summary>
    public string Company { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Role 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the role value.</para>
    /// \endif
    /// </summary>
    public string Role { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Period 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the period value.</para>
    /// \endif
    /// </summary>
    public string Period { get; set; } = "";
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
    /// <para>Bullets 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the bullets value.</para>
    /// \endif
    /// </summary>
    public List<string> Bullets { get; set; } = [];
}

/// <summary>
/// \if KO
/// <para>Education Item 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates education item functionality and related state.</para>
/// \endif
/// </summary>
public class EducationItem
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
    /// <para>School 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the school value.</para>
    /// \endif
    /// </summary>
    public string School { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Degree 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the degree value.</para>
    /// \endif
    /// </summary>
    public string Degree { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Period 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the period value.</para>
    /// \endif
    /// </summary>
    public string Period { get; set; } = "";
}
