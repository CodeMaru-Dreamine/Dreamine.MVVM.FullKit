using Markdig;
using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

public sealed class FamilyPostViewModel
{
    private readonly IFamilyTenantStore _tenants;
    private readonly IPostStore _posts;
    private readonly IReactionStore _reactions;
    private readonly IMediaService _media;

    private static readonly MarkdownPipeline _mdPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public FamilyPostViewModel(IFamilyTenantStore tenants, IPostStore posts,
        IReactionStore reactions, IMediaService media)
    {
        _tenants = tenants;
        _posts = posts;
        _reactions = reactions;
        _media = media;
    }

    public FamilyConfig? Config { get; private set; }
    public PostEntry? Post { get; private set; }
    public ReactionSummary Reactions { get; private set; } = new();
    public bool IsLoaded { get; private set; }
    public bool NotFound { get; private set; }

    public string ContentHtml => string.IsNullOrWhiteSpace(Post?.Content)
        ? ""
        : Markdown.ToHtml(Post.Content, _mdPipeline);

    public bool AllowReactions => Config?.AllowReactions ?? true;
    public bool AllowComments => Config?.AllowComments ?? true;
    public string ThemeName => Config?.ThemeName ?? "warm";
    public string FamilyName => Config?.FamilyName ?? "";

    public string GetMediaUrl(string fileName) =>
        Post is null ? "" : _media.GetMediaUrl(Config?.Slug ?? "", Post.Id, fileName);

    public string GetThumbUrl(string fileName) =>
        Post is null ? "" : _media.GetThumbUrl(Config?.Slug ?? "", Post.Id, fileName);

    public string GetVideoUrl(string fileName) =>
        IsExternalUrl(fileName) ? fileName : GetMediaUrl(fileName);

    public static bool IsExternalUrl(string s) =>
        s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    public static bool IsYouTube(string url) =>
        url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);

    public static string GetYouTubeEmbedUrl(string url)
    {
        if (url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
        {
            var id = url.Split("youtu.be/")[1].Split('?')[0];
            return $"https://www.youtube.com/embed/{id}";
        }
        var m = System.Text.RegularExpressions.Regex.Match(url, @"[?&]v=([^&]+)");
        return m.Success ? $"https://www.youtube.com/embed/{m.Groups[1].Value}" : url;
    }

    // ── 댓글 작성 ──────────────────────────────────────────
    public string CommentAuthor { get; set; } = "";
    public string CommentBody { get; set; } = "";
    public string CommentStatus { get; private set; } = "";
    public bool IsSavingComment { get; private set; }

    public async Task LoadAsync(string slug, string postId, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }

        Post = await _posts.GetAsync(slug, postId, ct).ConfigureAwait(false);
        if (Post is null) { NotFound = true; IsLoaded = true; return; }

        Reactions = await _reactions.GetAsync(slug, postId, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    public async Task AddReactionAsync(string slug, string emoji, CancellationToken ct = default)
    {
        if (Post is null || !AllowReactions) return;
        await _reactions.AddReactionAsync(slug, Post.Id, emoji, ct).ConfigureAwait(false);
        Reactions = await _reactions.GetAsync(slug, Post.Id, ct).ConfigureAwait(false);
    }

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

    public async Task DeleteCommentAsync(string slug, string commentId, CancellationToken ct = default)
    {
        if (Post is null) return;
        await _reactions.DeleteCommentAsync(slug, Post.Id, commentId, ct).ConfigureAwait(false);
        Reactions = await _reactions.GetAsync(slug, Post.Id, ct).ConfigureAwait(false);
    }
}
