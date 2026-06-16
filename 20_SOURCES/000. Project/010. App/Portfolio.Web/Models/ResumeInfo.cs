namespace PortfolioApp.Models;

public class ResumeInfo
{
    public List<SkillGroup> SkillGroups { get; set; } = [];
    public List<ExperienceItem> Experiences { get; set; } = [];
    public List<EducationItem> Educations { get; set; } = [];
}

public class SkillGroup
{
    public string Category { get; set; } = "";
    public List<string> Skills { get; set; } = [];
}

public class ExperienceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string Period { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Bullets { get; set; } = [];
}

public class EducationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string School { get; set; } = "";
    public string Degree { get; set; } = "";
    public string Period { get; set; } = "";
}
