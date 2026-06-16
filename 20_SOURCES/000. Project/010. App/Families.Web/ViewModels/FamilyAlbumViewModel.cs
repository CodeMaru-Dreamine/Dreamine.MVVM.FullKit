using FamiliesApp.Models;
using FamiliesApp.Services;

namespace FamiliesApp.ViewModels;

public sealed class FamilyAlbumViewModel
{
    private readonly IFamilyTenantStore _tenants;
    private readonly IPostStore _posts;
    private readonly IAlbumStore _albums;
    private readonly IMediaService _media;

    public FamilyAlbumViewModel(IFamilyTenantStore tenants, IPostStore posts,
        IAlbumStore albums, IMediaService media)
    {
        _tenants = tenants;
        _posts = posts;
        _albums = albums;
        _media = media;
    }

    public FamilyConfig? Config { get; private set; }
    public IReadOnlyList<PostEntry> Posts { get; private set; } = [];
    public IReadOnlyList<AlbumInfo> Albums { get; private set; } = [];
    public bool IsLoaded { get; private set; }
    public bool NotFound { get; private set; }

    public string FamilyName => Config?.FamilyName ?? "";
    public string Bio => Config?.Bio ?? "";
    public string ThemeName => Config?.ThemeName ?? "warm";
    public bool AllowReactions => Config?.AllowReactions ?? true;
    public bool AllowComments => Config?.AllowComments ?? true;

    public string CoverImageUrl
    {
        get
        {
            if (Config is null || string.IsNullOrWhiteSpace(Config.CoverImageFileName)) return "";
            return _media.GetCoverUrl(Config.Slug, Config.CoverImageFileName);
        }
    }

    public string OgTitle => !string.IsNullOrWhiteSpace(Config?.OgTitle)
        ? Config.OgTitle
        : $"{FamilyName} 가족 앨범";

    public string OgDescription => !string.IsNullOrWhiteSpace(Config?.OgDescription)
        ? Config.OgDescription
        : $"{FamilyName}의 소중한 순간들을 담은 가족 앨범입니다.";

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

    public string GetMediaUrl(string postId, string fileName) =>
        _media.GetMediaUrl(Config?.Slug ?? "", postId, fileName);

    public string GetThumbUrl(string postId, string fileName) =>
        _media.GetThumbUrl(Config?.Slug ?? "", postId, fileName);

    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (Config is null) { NotFound = true; IsLoaded = true; return; }

        Posts = await _posts.GetAllAsync(slug, ct).ConfigureAwait(false);
        Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    public async Task<IReadOnlyList<PostEntry>> GetAlbumPostsAsync(string slug, string albumId, CancellationToken ct = default)
        => await _posts.GetByAlbumAsync(slug, albumId, ct).ConfigureAwait(false);
}
