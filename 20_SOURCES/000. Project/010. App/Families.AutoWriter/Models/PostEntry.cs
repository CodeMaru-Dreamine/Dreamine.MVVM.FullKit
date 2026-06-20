namespace FamiliesAutoWriter.Models;

public enum MediaPosition { Bottom, Top }

public sealed class PostEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime PostedAt { get; set; } = DateTime.Now;
    public string AlbumId { get; set; } = "";
    public List<string> PhotoFileNames { get; set; } = [];
    public List<string> VideoFileNames { get; set; } = [];
    public bool IsPinned { get; set; } = false;
    public MediaPosition MediaPosition { get; set; } = MediaPosition.Bottom;
}
