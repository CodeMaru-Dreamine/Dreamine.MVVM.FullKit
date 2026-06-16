namespace PortfolioApp.Models;

public enum ProjectCategory { Personal, Work }

public class ProjectItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public int Year { get; set; } = DateTime.Now.Year;
    public string Emoji { get; set; } = "🛠️";
    public string? ImageFileName { get; set; }
    public string? GitHub { get; set; }
    public string? LiveUrl { get; set; }
    public string? DocUrl { get; set; }
    public ProjectCategory Category { get; set; } = ProjectCategory.Personal;
    public int SortOrder { get; set; } = 0;

    // Work 전용
    public string[] WorkImages { get; set; } = [];
    public string? WorkSubtitle { get; set; }
    public string[] WorkBullets { get; set; } = [];
    public string? WorkPeriod { get; set; }
    public string? WorkRole { get; set; }
    public string? WorkTech { get; set; }
    public string? WorkCompany { get; set; }
}
