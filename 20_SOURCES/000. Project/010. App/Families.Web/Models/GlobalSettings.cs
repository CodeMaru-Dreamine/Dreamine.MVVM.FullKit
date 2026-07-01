namespace FamiliesApp.Models;

/// <summary>
/// \file GlobalSettings.cs
/// \brief 전체 사이트(모든 가족 앨범 계정 공통) 설정. 슈퍼어드민에서 편집.
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>이미지(사진/커버) 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxImageSizeMb { get; set; } = 20;
    /// <summary>동영상 업로드 최대 용량(MB). 0이면 무제한.</summary>
    public int MaxVideoSizeMb { get; set; } = 500;
}
