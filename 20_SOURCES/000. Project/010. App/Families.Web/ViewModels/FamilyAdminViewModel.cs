using System.IO;
using FamiliesApp.Models;
using FamiliesApp.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace FamiliesApp.ViewModels;

public sealed class FamilyAdminViewModel
{
    private readonly IFamilyTenantStore _tenants;
    private readonly IPostStore _posts;
    private readonly IAlbumStore _albums;
    private readonly IMediaService _media;

    public FamilyAdminViewModel(IFamilyTenantStore tenants, IPostStore posts,
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
    public bool IsAuthenticated { get; private set; }
    public string StatusMessage { get; private set; } = "";
    public bool IsUploading { get; private set; }
    public string LoginPassword { get; set; } = "";

    // ── 포스트 편집 ─────────────────────────────────────────
    public PostEntry? EditingPost { get; private set; }
    public List<IBrowserFile> PendingFiles { get; private set; } = new();

    // ── 앨범 편집 ───────────────────────────────────────────
    public string NewAlbumName { get; set; } = "";
    public string NewAlbumDesc { get; set; } = "";

    public async Task<bool> LoginAsync(string slug, CancellationToken ct = default)
    {
        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null) { StatusMessage = "존재하지 않는 슬러그입니다."; return false; }

        IsAuthenticated = config.PasswordHash == LoginPassword;
        StatusMessage = IsAuthenticated ? "" : "비밀번호가 틀렸습니다.";
        return IsAuthenticated;
    }

    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                 ?? new FamilyConfig { Slug = slug };
        Posts = await _posts.GetAllAsync(slug, ct).ConfigureAwait(false);
        Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    public async Task SaveConfigAsync(CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            StatusMessage = "설정이 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
    }

    public void NewPost()
    {
        EditingPost = new PostEntry { PostedAt = DateTime.Now };
        PendingFiles.Clear();
        StatusMessage = "";
    }

    public void EditPost(PostEntry post)
    {
        EditingPost = post;
        PendingFiles.Clear();
        StatusMessage = "";
    }

    public void CancelEdit()
    {
        EditingPost = null;
        PendingFiles.Clear();
    }

    // 파일 선택 즉시 업로드 — 편집 상태 유지, 포스트 메타는 저장하지 않음
    public async Task UploadPendingFilesAsync(string slug, CancellationToken ct = default)
    {
        if (EditingPost is null || PendingFiles.Count == 0) return;
        IsUploading = true;
        try
        {
            foreach (var file in PendingFiles)
            {
                var fn = await _media.UploadPostMediaAsync(slug, EditingPost.Id, file, ct).ConfigureAwait(false);
                var ext = Path.GetExtension(fn).ToLowerInvariant();
                if (ext is ".mp4" or ".webm" or ".mov" or ".m4v")
                    EditingPost.VideoFileNames.Add(fn);
                else
                    EditingPost.PhotoFileNames.Add(fn);
            }
            PendingFiles.Clear();
            StatusMessage = "파일이 추가되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    public async Task SavePostAsync(string slug, CancellationToken ct = default)
    {
        if (EditingPost is null) return;
        // 혹시 아직 대기 중인 파일이 있으면 먼저 업로드
        if (PendingFiles.Count > 0)
            await UploadPendingFilesAsync(slug, ct).ConfigureAwait(false);
        IsUploading = true;
        try
        {
            await _posts.SaveAsync(slug, EditingPost, ct).ConfigureAwait(false);
            Posts = await _posts.GetAllAsync(slug, ct).ConfigureAwait(false);
            EditingPost = null;
            StatusMessage = "포스트가 저장되었습니다. ✅";
        }
        catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    public async Task DeletePostAsync(string slug, string postId, CancellationToken ct = default)
    {
        try
        {
            await _posts.DeleteAsync(slug, postId, ct).ConfigureAwait(false);
            Posts = await _posts.GetAllAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "포스트가 삭제되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"삭제 오류: {ex.Message}"; }
    }

    public async Task DeletePostMediaAsync(string slug, string postId, string fileName, CancellationToken ct = default)
    {
        try
        {
            await _media.DeletePostMediaAsync(slug, postId, fileName, ct).ConfigureAwait(false);
            if (EditingPost is not null)
            {
                EditingPost.PhotoFileNames.Remove(fileName);
                EditingPost.VideoFileNames.Remove(fileName);
            }
            StatusMessage = "파일이 삭제되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"삭제 오류: {ex.Message}"; }
    }

    public async Task UploadCoverAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _media.UploadCoverAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "커버 이미지가 업로드되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    public async Task AddAlbumAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewAlbumName)) { StatusMessage = "앨범 이름을 입력해주세요."; return; }
        try
        {
            var album = new AlbumInfo { Name = NewAlbumName.Trim(), Description = NewAlbumDesc.Trim() };
            await _albums.SaveAsync(slug, album, ct).ConfigureAwait(false);
            Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
            NewAlbumName = "";
            NewAlbumDesc = "";
            StatusMessage = "앨범이 추가되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"앨범 추가 오류: {ex.Message}"; }
    }

    public async Task DeleteAlbumAsync(string slug, string albumId, CancellationToken ct = default)
    {
        try
        {
            await _albums.DeleteAsync(slug, albumId, ct).ConfigureAwait(false);
            Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "앨범이 삭제되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"앨범 삭제 오류: {ex.Message}"; }
    }

    public async Task DeleteSelfAsync(string slug, CancellationToken ct = default)
    {
        await _tenants.DeleteAsync(slug, ct).ConfigureAwait(false);
    }

    public string GetCoverUrl(string slug, string fileName) => _media.GetCoverUrl(slug, fileName);
    public string GetMediaUrl(string slug, string postId, string fileName) => _media.GetMediaUrl(slug, postId, fileName);
    public string GetThumbUrl(string slug, string postId, string fileName) => _media.GetThumbUrl(slug, postId, fileName);

    public static bool IsYouTube(string url) =>
        url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);
}
