namespace FamiliesApp.Models;

public sealed class MediaInfo
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string ThumbUrl { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsVideo => FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                        || FileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                        || FileName.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
                        || FileName.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase);
}
