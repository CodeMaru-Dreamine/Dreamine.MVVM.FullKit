using System.IO;
using System.Text.Json;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>Json Reaction Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json reaction store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonReactionStore : IReactionStore
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly IFamilyTenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the gate value.</para>
    /// \endif
    /// </summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// \if KO
    /// <para>json Opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json opts value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonReactionStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonReactionStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>IFamilyTenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IFamilyTenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    public JsonReactionStore(IFamilyTenantStore tenants) => _tenants = tenants;

    /// <summary>
    /// \if KO
    /// <para>Reactions Dir 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reactions dir operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Reactions Dir 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the reactions dir operation.</para>
    /// \endif
    /// </returns>
    private string ReactionsDir(string slug) =>
        Path.Combine(_tenants.GetTenantDataPath(slug), "reactions");

    /// <summary>
    /// \if KO
    /// <para>Reaction Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reaction path operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Reaction Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the reaction path operation.</para>
    /// \endif
    /// </returns>
    private string ReactionPath(string slug, string postId) =>
        Path.Combine(ReactionsDir(slug), $"{Sanitize(postId)}.json");

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
    public async Task<ReactionSummary> GetAsync(string slug, string postId, CancellationToken ct = default)
    {
        var path = ReactionPath(slug, postId);
        if (!File.Exists(path)) return new ReactionSummary { PostId = postId };

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<ReactionSummary>(fs, _jsonOpts, ct).ConfigureAwait(false)
                   ?? new ReactionSummary { PostId = postId };
        }
        finally { _gate.Release(); }
    }

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
    public async Task AddReactionAsync(string slug, string postId, string emoji, CancellationToken ct = default)
    {
        var summary = await GetAsync(slug, postId, ct).ConfigureAwait(false);
        summary.EmojiCounts.TryGetValue(emoji, out int current);
        summary.EmojiCounts[emoji] = current + 1;
        await SaveAsync(slug, summary, ct).ConfigureAwait(false);
    }

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
    public async Task AddCommentAsync(string slug, string postId, CommentEntry comment, CancellationToken ct = default)
    {
        var summary = await GetAsync(slug, postId, ct).ConfigureAwait(false);
        comment.PostId = postId;
        summary.Comments.Add(comment);
        await SaveAsync(slug, summary, ct).ConfigureAwait(false);
    }

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
    public async Task DeleteCommentAsync(string slug, string postId, string commentId, CancellationToken ct = default)
    {
        var summary = await GetAsync(slug, postId, ct).ConfigureAwait(false);
        summary.Comments.RemoveAll(c => c.Id == commentId);
        await SaveAsync(slug, summary, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
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
    /// <param name="summary">
    /// \if KO
    /// <para>summary에 사용할 <c>ReactionSummary</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ReactionSummary</c> value used for summary.</para>
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
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    private async Task SaveAsync(string slug, ReactionSummary summary, CancellationToken ct = default)
    {
        var dir = ReactionsDir(slug);
        Directory.CreateDirectory(dir);

        var path = ReactionPath(slug, summary.PostId);
        var tmp = path + ".tmp";

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, summary, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// \if KO
    /// <para>Sanitize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sanitize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sanitize 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the sanitize operation.</para>
    /// \endif
    /// </returns>
    private static string Sanitize(string id) =>
        string.Concat(id.Split(Path.GetInvalidFileNameChars()));
}
