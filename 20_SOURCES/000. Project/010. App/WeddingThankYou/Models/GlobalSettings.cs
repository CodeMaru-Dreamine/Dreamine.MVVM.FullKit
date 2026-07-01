namespace WeddingThankYou.Models;

/// <summary>
/// \file GlobalSettings.cs
/// \brief 전체 사이트(모든 테넌트 공통) 설정. 슈퍼어드민에서 편집.
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>동영상 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxVideoSizeMb { get; set; } = 200;
    /// <summary>계정당 동영상 업로드 최대 개수(기본값). 0이면 무제한.</summary>
    public int MaxVideoCount { get; set; } = 6;
}
