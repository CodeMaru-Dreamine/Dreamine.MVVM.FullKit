namespace DreamineWeb.Models;

public sealed class LibraryInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }          // 영문 설명 (없으면 Description 사용)
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = "stable";   // stable | beta | wip
    public string Version { get; set; } = "1.0.0";
    public string? NuGetId { get; set; }
    public string? RepoUrl { get; set; }
    public string? XmlDocPath { get; set; }           // 빌드 출력 .xml 경로
    public string? SourceProjectPath { get; set; }    // 원본 .csproj 경로
    public string? TargetFramework { get; set; }
    public string[] Dependencies { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
