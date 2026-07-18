using Markdig;
using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Family Post View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family post view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyPostViewModel
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
    /// <para>posts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the posts value.</para>
    /// \endif
    /// </summary>
    private readonly IPostStore _posts;
    /// <summary>
    /// \if KO
    /// <para>reactions 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the reactions value.</para>
    /// \endif
    /// </summary>
    private readonly IReactionStore _reactions;
    /// <summary>
    /// \if KO
    /// <para>media 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the media value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaService _media;

    /// <summary>
    /// \if KO
    /// <para>md Pipeline 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the md pipeline value.</para>
    /// \endif
    /// </summary>
    private static readonly MarkdownPipeline _mdPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="FamilyPostViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="FamilyPostViewModel"/> class with the specified settings.</para>
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
    /// <param name="posts">
    /// \if KO
    /// <para>posts에 사용할 <c>IPostStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPostStore</c> value used for posts.</para>
    /// \endif
    /// </param>
    /// <param name="reactions">
    /// \if KO
    /// <para>reactions에 사용할 <c>IReactionStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReactionStore</c> value used for reactions.</para>
    /// \endif
    /// </param>
    /// <param name="media">
    /// \if KO
    /// <para>media에 사용할 <c>IMediaService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaService</c> value used for media.</para>
    /// \endif
    /// </param>
    public FamilyPostViewModel(IFamilyTenantStore tenants, IPostStore posts,
        IReactionStore reactions, IMediaService media)
    {
        _tenants = tenants;
        _posts = posts;
        _reactions = reactions;
        _media = media;
    }

    /// <summary>
    /// \if KO
    /// <para>Config 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the config value.</para>
    /// \endif
    /// </summary>
    public FamilyConfig? Config { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Post 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the post value.</para>
    /// \endif
    /// </summary>
    public PostEntry? Post { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Reactions 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the reactions value.</para>
    /// \endif
    /// </summary>
    public ReactionSummary Reactions { get; private set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Is Loaded 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is loaded value.</para>
    /// \endif
    /// </summary>
    public bool IsLoaded { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Not Found 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the not found value.</para>
    /// \endif
    /// </summary>
    public bool NotFound { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Content Html 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the content html value.</para>
    /// \endif
    /// </summary>
    public string ContentHtml => string.IsNullOrWhiteSpace(Post?.Content)
        ? ""
        : Markdown.ToHtml(Post.Content, _mdPipeline);

    /// <summary>
    /// \if KO
    /// <para>Allow Reactions 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the allow reactions value.</para>
    /// \endif
    /// </summary>
    public bool AllowReactions => Config?.AllowReactions ?? true;
    /// <summary>
    /// \if KO
    /// <para>Allow Comments 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the allow comments value.</para>
    /// \endif
    /// </summary>
    public bool AllowComments => Config?.AllowComments ?? true;
    /// <summary>
    /// \if KO
    /// <para>Theme Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the theme name value.</para>
    /// \endif
    /// </summary>
    public string ThemeName => Config?.ThemeName ?? "warm";
    /// <summary>
    /// \if KO
    /// <para>Family Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the family name value.</para>
    /// \endif
    /// </summary>
    public string FamilyName => Config?.FamilyName ?? "";

    /// <summary>
    /// \if KO
    /// <para>Media Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the media url value.</para>
    /// \endif
    /// </summary>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Media Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get media url operation.</para>
    /// \endif
    /// </returns>
    public string GetMediaUrl(string fileName) =>
        IsExternalUrl(fileName) ? fileName
        : Post is null ? "" : _media.GetMediaUrl(Config?.Slug ?? "", Post.Id, fileName);

    /// <summary>
    /// \if KO
    /// <para>Thumb Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thumb url value.</para>
    /// \endif
    /// </summary>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Thumb Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get thumb url operation.</para>
    /// \endif
    /// </returns>
    public string GetThumbUrl(string fileName) =>
        IsExternalUrl(fileName) ? fileName
        : Post is null ? "" : _media.GetThumbUrl(Config?.Slug ?? "", Post.Id, fileName);

    /// <summary>
    /// \if KO
    /// <para>Video Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video url value.</para>
    /// \endif
    /// </summary>
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Video Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get video url operation.</para>
    /// \endif
    /// </returns>
    public string GetVideoUrl(string fileName) =>
        IsExternalUrl(fileName) ? fileName : GetMediaUrl(fileName);

    /// <summary>
    /// \if KO
    /// <para>Is External Url 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is external url.</para>
    /// \endif
    /// </summary>
    /// <param name="s">
    /// \if KO
    /// <para>이벤트를 발생시킨 객체입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The object that raised the event.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is External Url 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is external url condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public static bool IsExternalUrl(string s) =>
        s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>Is You Tube 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is you tube.</para>
    /// \endif
    /// </summary>
    /// <param name="url">
    /// \if KO
    /// <para>url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for url.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is You Tube 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is you tube condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public static bool IsYouTube(string url) =>
        url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>You Tube Embed Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the you tube embed url value.</para>
    /// \endif
    /// </summary>
    /// <param name="url">
    /// \if KO
    /// <para>url에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for url.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get You Tube Embed Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get you tube embed url operation.</para>
    /// \endif
    /// </returns>
    public static string GetYouTubeEmbedUrl(string url)
    {
        url = url.Trim();

        // 이미 embed URL이면 그대로
        if (url.Contains("/embed/", StringComparison.OrdinalIgnoreCase))
            return url.Split('?')[0]; // 불필요한 쿼리 제거

        // youtu.be/ID 단축 URL
        if (url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
        {
            var after = url.Split(new[] { "youtu.be/" }, StringSplitOptions.None)[1];
            var id = after.Split('?')[0].Split('#')[0].Trim();
            return $"https://www.youtube.com/embed/{id}";
        }

        // youtube.com/shorts/ID
        var shortsMatch = System.Text.RegularExpressions.Regex.Match(
            url, @"youtube\.com/shorts/([^?&#/]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (shortsMatch.Success)
            return $"https://www.youtube.com/embed/{shortsMatch.Groups[1].Value}";

        // youtube.com/watch?v=ID (또는 &v=ID)
        var watchMatch = System.Text.RegularExpressions.Regex.Match(url, @"[?&]v=([^?&#]+)");
        if (watchMatch.Success)
            return $"https://www.youtube.com/embed/{watchMatch.Groups[1].Value}";

        // 그래도 못 찾으면 원본 반환 (브라우저가 처리)
        return url;
    }

    // ── 댓글 작성 ──────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Comment Author 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the comment author value.</para>
    /// \endif
    /// </summary>
    public string CommentAuthor { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Comment Body 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the comment body value.</para>
    /// \endif
    /// </summary>
    public string CommentBody { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Comment Status 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the comment status value.</para>
    /// \endif
    /// </summary>
    public string CommentStatus { get; private set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Is Saving Comment 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is saving comment value.</para>
    /// \endif
    /// </summary>
    public bool IsSavingComment { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
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
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadAsync(string slug, string postId, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }

        Post = await _posts.GetAsync(slug, postId, ct).ConfigureAwait(false);
        if (Post is null) { NotFound = true; IsLoaded = true; return; }

        Reactions = await _reactions.GetAsync(slug, postId, ct).ConfigureAwait(false);
        IsLoaded = true;
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
    public async Task AddReactionAsync(string slug, string emoji, CancellationToken ct = default)
    {
        if (Post is null || !AllowReactions) return;
        await _reactions.AddReactionAsync(slug, Post.Id, emoji, ct).ConfigureAwait(false);
        Reactions = await _reactions.GetAsync(slug, Post.Id, ct).ConfigureAwait(false);
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
    public async Task AddCommentAsync(string slug, CancellationToken ct = default)
    {
        if (Post is null || !AllowComments || IsSavingComment) return;
        if (string.IsNullOrWhiteSpace(CommentAuthor)) { CommentStatus = "이름을 입력해주세요."; return; }
        if (string.IsNullOrWhiteSpace(CommentBody)) { CommentStatus = "댓글 내용을 입력해주세요."; return; }

        IsSavingComment = true;
        try
        {
            var entry = new CommentEntry
            {
                PostId = Post.Id,
                AuthorName = CommentAuthor.Trim(),
                Body = CommentBody.Trim(),
                CreatedAt = DateTime.Now
            };
            await _reactions.AddCommentAsync(slug, Post.Id, entry, ct).ConfigureAwait(false);
            Reactions = await _reactions.GetAsync(slug, Post.Id, ct).ConfigureAwait(false);
            CommentAuthor = "";
            CommentBody = "";
            CommentStatus = "댓글이 등록되었습니다 💬";
        }
        catch { CommentStatus = "등록 중 오류가 발생했습니다."; }
        finally { IsSavingComment = false; }
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
    public async Task DeleteCommentAsync(string slug, string commentId, CancellationToken ct = default)
    {
        if (Post is null) return;
        await _reactions.DeleteCommentAsync(slug, Post.Id, commentId, ct).ConfigureAwait(false);
        Reactions = await _reactions.GetAsync(slug, Post.Id, ct).ConfigureAwait(false);
    }
}
