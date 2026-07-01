namespace WeddingThankYou.Models;

/// <summary>
/// \file PhotoInfo.cs
/// \brief 갤러리 사진 한 장에 대한 메타데이터.
/// </summary>
public sealed class PhotoInfo
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string ThumbUrl { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}
