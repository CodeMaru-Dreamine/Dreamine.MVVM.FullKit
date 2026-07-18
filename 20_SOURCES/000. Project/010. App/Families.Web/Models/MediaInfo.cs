namespace FamiliesApp.Models;

/// <summary>
/// \if KO
/// <para>Media Info 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaInfo
{
    /// <summary>
    /// \if KO
    /// <para>File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the file name value.</para>
    /// \endif
    /// </summary>
    public string FileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the url value.</para>
    /// \endif
    /// </summary>
    public string Url { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Thumb Url 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the thumb url value.</para>
    /// \endif
    /// </summary>
    public string ThumbUrl { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Size Bytes 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the size bytes value.</para>
    /// \endif
    /// </summary>
    public long SizeBytes { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Last Modified 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last modified value.</para>
    /// \endif
    /// </summary>
    public DateTime LastModified { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Is Video 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is video value.</para>
    /// \endif
    /// </summary>
    public bool IsVideo => FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                        || FileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                        || FileName.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
                        || FileName.EndsWith(".m4v", StringComparison.OrdinalIgnoreCase);
}
