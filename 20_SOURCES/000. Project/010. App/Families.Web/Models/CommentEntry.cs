namespace FamiliesApp.Models;

/// <summary>
/// \if KO
/// <para>Comment Entry 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates comment entry functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CommentEntry
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
    /// <para>Post Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the post id value.</para>
    /// \endif
    /// </summary>
    public string PostId { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Author Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the author name value.</para>
    /// \endif
    /// </summary>
    public string AuthorName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Body 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the body value.</para>
    /// \endif
    /// </summary>
    public string Body { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
