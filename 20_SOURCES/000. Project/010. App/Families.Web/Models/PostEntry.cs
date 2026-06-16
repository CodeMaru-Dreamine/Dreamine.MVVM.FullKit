namespace FamiliesApp.Models;

public sealed class PostEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    /// <summary>본문 — Markdown</summary>
    public string Content { get; set; } = "";
    public DateTime PostedAt { get; set; } = DateTime.Now;
    /// <summary>소속 앨범 ID. 빈 문자열이면 전체 타임라인에만 표시.</summary>
    public string AlbumId { get; set; } = "";
    public List<string> PhotoFileNames { get; set; } = new();
    public List<string> VideoFileNames { get; set; } = new();
    public bool IsPinned { get; set; } = false;
}
