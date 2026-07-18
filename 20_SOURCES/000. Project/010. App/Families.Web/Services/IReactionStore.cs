using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>I Reaction Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i reaction store functionality and related state.</para>
/// \endif
/// </summary>
public interface IReactionStore
{
    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ReactionSummary&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ReactionSummary&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    Task<ReactionSummary> GetAsync(string slug, string postId, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Reaction Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the reaction async item.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="emoji">
    /// \if KO
    /// <para>emoji에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for emoji.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Add Reaction Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the add reaction async operation.</para>
    /// \endif
    /// </returns>
    Task AddReactionAsync(string slug, string postId, string emoji, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Comment Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the comment async item.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="comment">
    /// \if KO
    /// <para>comment에 사용할 <c>CommentEntry</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CommentEntry</c> value used for comment.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Add Comment Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the add comment async operation.</para>
    /// \endif
    /// </returns>
    Task AddCommentAsync(string slug, string postId, CommentEntry comment, CancellationToken ct = default);
    /// <summary>
    /// \if KO
    /// <para>Delete Comment Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete comment async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
    /// <param name="commentId">
    /// \if KO
    /// <para>comment Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for comment id.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Comment Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete comment async operation.</para>
    /// \endif
    /// </returns>
    Task DeleteCommentAsync(string slug, string postId, string commentId, CancellationToken ct = default);
}
