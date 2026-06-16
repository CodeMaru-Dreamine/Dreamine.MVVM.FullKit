using FamiliesApp.Models;

namespace FamiliesApp.Services;

public interface IReactionStore
{
    Task<ReactionSummary> GetAsync(string slug, string postId, CancellationToken ct = default);
    Task AddReactionAsync(string slug, string postId, string emoji, CancellationToken ct = default);
    Task AddCommentAsync(string slug, string postId, CommentEntry comment, CancellationToken ct = default);
    Task DeleteCommentAsync(string slug, string postId, string commentId, CancellationToken ct = default);
}
