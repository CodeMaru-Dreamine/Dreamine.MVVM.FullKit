namespace WeddingPlatform.Models;

/// <summary>
/// \if KO
/// <para>Photo Info 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates photo info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PhotoInfo
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
}
