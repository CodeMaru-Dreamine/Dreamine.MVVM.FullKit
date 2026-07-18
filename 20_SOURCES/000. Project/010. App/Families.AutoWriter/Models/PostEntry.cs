namespace FamiliesAutoWriter.Models;

/// <summary>
/// \if KO
/// <para>Media Position 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media position functionality and related state.</para>
/// \endif
/// </summary>
public enum MediaPosition
{
    /// <summary>
    /// \if KO
    /// <para>미디어를 본문 아래에 배치합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Places the media below the content.</para>
    /// \endif
    /// </summary>
    Bottom,

    /// <summary>
    /// \if KO
    /// <para>미디어를 본문 위에 배치합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Places the media above the content.</para>
    /// \endif
    /// </summary>
    Top
}

/// <summary>
/// \if KO
/// <para>Post Entry 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates post entry functionality and related state.</para>
/// \endif
/// </summary>
public sealed class PostEntry
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// \if KO
    /// <para>Title 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the title value.</para>
    /// \endif
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Content 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the content value.</para>
    /// \endif
    /// </summary>
    public string Content { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Posted At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the posted at value.</para>
    /// \endif
    /// </summary>
    public DateTime PostedAt { get; set; } = DateTime.Now;
    /// <summary>
    /// \if KO
    /// <para>Album Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the album id value.</para>
    /// \endif
    /// </summary>
    public string AlbumId { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Photo File Names 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the photo file names value.</para>
    /// \endif
    /// </summary>
    public List<string> PhotoFileNames { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Video File Names 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video file names value.</para>
    /// \endif
    /// </summary>
    public List<string> VideoFileNames { get; set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Is Pinned 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is pinned value.</para>
    /// \endif
    /// </summary>
    public bool IsPinned { get; set; } = false;
    /// <summary>
    /// \if KO
    /// <para>Media Position 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the media position value.</para>
    /// \endif
    /// </summary>
    public MediaPosition MediaPosition { get; set; } = MediaPosition.Bottom;
}
