namespace DreamineWeb.Models;

/// <summary>YouTube 영상과 샘플 프로젝트를 1:1로 연결한 학습 자료입니다.</summary>
public sealed class LearningResource
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string YouTubeUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string SampleName { get; set; } = string.Empty;
    public string SampleDownloadUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
