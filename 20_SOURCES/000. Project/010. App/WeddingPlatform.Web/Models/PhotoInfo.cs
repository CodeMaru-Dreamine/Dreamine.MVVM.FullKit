namespace WeddingPlatform.Models;

public sealed class PhotoInfo
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string ThumbUrl { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}
