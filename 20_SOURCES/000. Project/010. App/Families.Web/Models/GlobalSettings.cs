namespace FamiliesApp.Models;

/// <summary>
/// \if KO
/// <para>\file GlobalSettings.cs \brief 전체 사이트(모든 가족 앨범 계정 공통) 설정. 슈퍼어드민에서 편집.</para>
/// \endif
/// \if EN
/// <para>Encapsulates global settings functionality and related state.</para>
/// \endif
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>
    /// \if KO
    /// <para>이미지(사진/커버) 업로드 최대 용량(MB). 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max image size mb value.</para>
    /// \endif
    /// </summary>
    public int MaxImageSizeMb { get; set; } = 20;
    /// <summary>
    /// \if KO
    /// <para>동영상 업로드 최대 용량(MB). 0이면 무제한.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video size mb value.</para>
    /// \endif
    /// </summary>
    public int MaxVideoSizeMb { get; set; } = 500;
}
