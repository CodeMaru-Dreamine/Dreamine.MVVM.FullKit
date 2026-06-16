namespace FamiliesApp.Models;

public sealed class ReactionSummary
{
    public string PostId { get; set; } = "";
    /// <summary>이모지 → 누적 카운트 (예: "❤️" → 5)</summary>
    public Dictionary<string, int> EmojiCounts { get; set; } = new();
    public List<CommentEntry> Comments { get; set; } = new();
}
