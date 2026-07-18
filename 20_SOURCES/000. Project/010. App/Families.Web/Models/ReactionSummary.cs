namespace FamiliesApp.Models;

/// <summary>
/// \if KO
/// <para>Reaction Summary 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates reaction summary functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ReactionSummary
{
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
    /// <para>이모지 → 누적 카운트 (예: "❤️" → 5)</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the emoji counts value.</para>
    /// \endif
    /// </summary>
    public Dictionary<string, int> EmojiCounts { get; set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Comments 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the comments value.</para>
    /// \endif
    /// </summary>
    public List<CommentEntry> Comments { get; set; } = new();
}
