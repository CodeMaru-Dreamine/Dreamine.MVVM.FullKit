using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Family Album View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family album view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyAlbumViewModel
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
    /// <para>albums 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the albums value.</para>
    /// \endif
    /// </summary>
    private readonly IAlbumStore _albums;
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
    /// <para>지정한 설정으로 <see cref="FamilyAlbumViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="FamilyAlbumViewModel"/> class with the specified settings.</para>
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
    /// <param name="albums">
    /// \if KO
    /// <para>albums에 사용할 <c>IAlbumStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IAlbumStore</c> value used for albums.</para>
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
    public FamilyAlbumViewModel(IFamilyTenantStore tenants, IPostStore posts,
        IAlbumStore albums, IMediaService media)
    {
        _tenants = tenants;
        _posts = posts;
        _albums = albums;
        _media = media;
    }

    /// <summary>
    /// \if KO
    /// <para>Page Size 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the page size value.</para>
    /// \endif
    /// </summary>
    public int PageSize => Math.Clamp(Config?.PageSize ?? 20, 5, 100);

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
    /// <para>Posts 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the posts value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<PostEntry> Posts { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Albums 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the albums value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<AlbumInfo> Albums { get; private set; } = [];
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
    /// <para>Current Page 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the current page value.</para>
    /// \endif
    /// </summary>
    public int CurrentPage { get; private set; } = 1;
    /// <summary>
    /// \if KO
    /// <para>Total Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the total count value.</para>
    /// \endif
    /// </summary>
    public int TotalCount { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Total Pages 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the total pages value.</para>
    /// \endif
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

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
    /// <para>Bio 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the bio value.</para>
    /// \endif
    /// </summary>
    public string Bio => Config?.Bio ?? "";
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
    /// <para>Cover Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the cover image url value.</para>
    /// \endif
    /// </summary>
    public string CoverImageUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.CoverImageFileName)) return "";
            return _media.GetCoverUrl(Config.Slug, Config.CoverImageFileName);
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Og Title 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og title value.</para>
    /// \endif
    /// </summary>
    public string OgTitle => !string.IsNullOrWhiteSpace(Config?.OgTitle)
        ? Config.OgTitle
        : $"{FamilyName} 가족 앨범";

    /// <summary>
    /// \if KO
    /// <para>Og Description 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og description value.</para>
    /// \endif
    /// </summary>
    public string OgDescription => !string.IsNullOrWhiteSpace(Config?.OgDescription)
        ? Config.OgDescription
        : $"{FamilyName}의 소중한 순간들을 담은 가족 앨범입니다.";

    /// <summary>
    /// \if KO
    /// <para>Og Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og image url value.</para>
    /// \endif
    /// </summary>
    public string OgImageUrl
    {
        get
        {
            if (Config is null) return "";
            var fn = !string.IsNullOrWhiteSpace(Config.OgImageFileName)
                ? Config.OgImageFileName
                : Config.CoverImageFileName;
            return string.IsNullOrWhiteSpace(fn) ? "" : _media.GetCoverUrl(Config.Slug, fn);
        }
    }

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
    private static bool IsExternalUrl(string s) =>
        s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// \if KO
    /// <para>Media Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the media url value.</para>
    /// \endif
    /// </summary>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
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
    public string GetMediaUrl(string postId, string fileName) =>
        IsExternalUrl(fileName) ? fileName : _media.GetMediaUrl(Config?.Slug ?? "", postId, fileName);

    /// <summary>
    /// \if KO
    /// <para>Thumb Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thumb url value.</para>
    /// \endif
    /// </summary>
    /// <param name="postId">
    /// \if KO
    /// <para>post Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for post id.</para>
    /// \endif
    /// </param>
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
    public string GetThumbUrl(string postId, string fileName) =>
        IsExternalUrl(fileName) ? fileName : _media.GetThumbUrl(Config?.Slug ?? "", postId, fileName);

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
    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }

        Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
        await LoadPageAsync(slug, 1, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    /// <summary>
    /// \if KO
    /// <para>Page Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads page async data.</para>
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
    /// <param name="page">
    /// \if KO
    /// <para>page에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for page.</para>
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
    /// <para>Load Page Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load page async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadPageAsync(string slug, int page, CancellationToken ct = default)
    {
        CurrentPage = page;
        var (items, total) = await _posts.GetPageAsync(slug, page, PageSize, ct).ConfigureAwait(false);
        Posts = items;
        TotalCount = total;
    }

    /// <summary>
    /// \if KO
    /// <para>Album Posts Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the album posts async value.</para>
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
    /// <param name="albumId">
    /// \if KO
    /// <para>album Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for album id.</para>
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
    /// <para>Get Album Posts Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;PostEntry&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;PostEntry&gt;&gt;</c> result produced by the get album posts async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<PostEntry>> GetAlbumPostsAsync(string slug, string albumId, CancellationToken ct = default)
        => await _posts.GetByAlbumAsync(slug, albumId, ct).ConfigureAwait(false);

    /// <summary>
    /// \if KO
    /// <para>Album Page Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the album page async value.</para>
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
    /// <param name="albumId">
    /// \if KO
    /// <para>album Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for album id.</para>
    /// \endif
    /// </param>
    /// <param name="page">
    /// \if KO
    /// <para>page에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for page.</para>
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
    /// <para>Get Album Page Async 작업에서 생성한 <c>Task&lt;(IReadOnlyList&lt;PostEntry&gt; Items, int TotalCount)&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;(IReadOnlyList&lt;PostEntry&gt; Items, int TotalCount)&gt;</c> result produced by the get album page async operation.</para>
    /// \endif
    /// </returns>
    public async Task<(IReadOnlyList<PostEntry> Items, int TotalCount)> GetAlbumPageAsync(
        string slug, string albumId, int page, CancellationToken ct = default)
        => await _posts.GetByAlbumPageAsync(slug, albumId, page, PageSize, ct).ConfigureAwait(false);
}
