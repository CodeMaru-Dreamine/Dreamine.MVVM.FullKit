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
    private readonly FamilyUserContext _userContext;

    public FamilyAdminViewModel(IFamilyTenantStore tenants, IPostStore posts,
        IAlbumStore albums, IMediaService media, FamilyUserContext userContext)
    {
        _tenants = tenants;
        _posts = posts;
        _albums = albums;
        _media = media;
        _userContext = userContext;
    }

    public FamilyConfig? Config { get; private set; }
    public IReadOnlyList<PostEntry> Posts { get; private set; } = [];
    public IReadOnlyList<AlbumInfo> Albums { get; private set; } = [];
    public bool IsLoaded { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool IsSignedIn { get; private set; }
    public bool IsLinkedToCurrentUser { get; private set; }
    public bool IsOwner { get; private set; }
    public string StatusMessage { get; private set; } = "";
    public string CurrentUserLabel { get; private set; } = "";
    public bool IsUploading { get; private set; }
    public string LoginPassword { get; set; } = "";
    public IReadOnlyList<FamilyAdminUser> EffectiveAdminUsers =>
        Config is null ? [] : BuildEffectiveAdminUsers(Config);

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

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        await RefreshCurrentUserAsync().ConfigureAwait(false);

        if (IsAdminUser(config, user))
        {
            IsAuthenticated = true;
            IsLinkedToCurrentUser = true;
            IsOwner = IsOwnerUser(config, user);
            StatusMessage = "";
            return true;
        }

        IsAuthenticated = config.PasswordHash == LoginPassword;
        if (!IsAuthenticated)
        {
            StatusMessage = "비밀번호가 틀렸습니다.";
            return false;
        }

        if (user.IsAuthenticated && string.IsNullOrWhiteSpace(config.OwnerUserId))
        {
            config.OwnerUserId = user.Id;
            config.OwnerProvider = user.Provider;
            config.OwnerEmail = user.Email;
            config.OwnerDisplayName = user.DisplayName;
            config.OwnerLinkedAt = DateTime.Now;
            EnsureAdminUser(config, user, "Owner");
            await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
            IsLinkedToCurrentUser = true;
            IsOwner = true;
            StatusMessage = "CodeMaru/Dreamine 계정에 연결되었습니다. 다음부터는 공용 로그인으로 관리할 수 있습니다.";
        }
        else if (user.IsAuthenticated)
        {
            EnsureAdminUser(config, user, "Admin");
            await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
            IsLinkedToCurrentUser = true;
            IsOwner = IsOwnerUser(config, user);
            StatusMessage = "현재 CodeMaru/Dreamine 계정이 이 가족 앨범의 관리자로 추가되었습니다.";
        }
        else if (!user.IsAuthenticated && string.IsNullOrWhiteSpace(config.OwnerUserId))
        {
            StatusMessage = "로그인은 성공했습니다. 공용 계정에 연결하려면 먼저 CodeMaru/Dreamine 로그인을 해주세요.";
        }
        else
        {
            StatusMessage = "";
        }

        return IsAuthenticated;
    }

    public async Task InitializeAsync(string slug, CancellationToken ct = default)
    {
        StatusMessage = "";
        await RefreshCurrentUserAsync().ConfigureAwait(false);

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null)
        {
            return;
        }

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        IsLinkedToCurrentUser = IsAdminUser(config, user);
        IsOwner = IsOwnerUser(config, user);

        if (IsLinkedToCurrentUser)
        {
            IsAuthenticated = true;
            await LoadAsync(slug, ct).ConfigureAwait(false);
        }
    }

    public async Task LoadAsync(string slug, CancellationToken ct = default)
    {
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                 ?? new FamilyConfig { Slug = slug };
        Posts = await _posts.GetAllAsync(slug, ct).ConfigureAwait(false);
        Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    public async Task RemoveAdminAsync(string userId, CancellationToken ct = default)
    {
        if (Config is null || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        if (!IsOwnerUser(Config, user))
        {
            StatusMessage = "대표 관리자만 관리자를 삭제할 수 있습니다.";
            return;
        }

        if (string.Equals(Config.OwnerUserId, userId, StringComparison.Ordinal))
        {
            StatusMessage = "대표 관리자는 삭제할 수 없습니다.";
            return;
        }

        var removed = Config.AdminUsers.RemoveAll(x =>
            string.Equals(x.UserId, userId, StringComparison.Ordinal)) > 0;
        if (!removed)
        {
            StatusMessage = "삭제할 관리자를 찾을 수 없습니다.";
            return;
        }

        await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
        StatusMessage = "관리자가 삭제되었습니다.";
    }

    private async Task RefreshCurrentUserAsync()
    {
        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        IsSignedIn = user.IsAuthenticated;
        CurrentUserLabel = user.IsAuthenticated
            ? string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Provider
                : $"{user.DisplayName} ({user.Provider})"
            : "";
    }

    private static bool IsOwnerUser(FamilyConfig config, FamilyCurrentUser user) =>
        user.IsAuthenticated &&
        string.Equals(config.OwnerUserId, user.Id, StringComparison.Ordinal);

    private static bool IsAdminUser(FamilyConfig config, FamilyCurrentUser user) =>
        user.IsAuthenticated &&
        (IsOwnerUser(config, user) ||
         config.AdminUsers.Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal)));

    private static void EnsureAdminUser(FamilyConfig config, FamilyCurrentUser user, string role)
    {
        var existing = config.AdminUsers.FirstOrDefault(x =>
            string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
        if (existing is null)
        {
            config.AdminUsers.Add(new FamilyAdminUser
            {
                UserId = user.Id,
                Provider = user.Provider,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = role,
                AddedAt = DateTime.Now
            });
            return;
        }

        existing.Provider = user.Provider;
        existing.Email = user.Email;
        existing.DisplayName = user.DisplayName;
        if (string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase))
        {
            existing.Role = "Owner";
        }
    }

    private static IReadOnlyList<FamilyAdminUser> BuildEffectiveAdminUsers(FamilyConfig config)
    {
        var result = new List<FamilyAdminUser>();
        if (!string.IsNullOrWhiteSpace(config.OwnerUserId))
        {
            result.Add(new FamilyAdminUser
            {
                UserId = config.OwnerUserId,
                Provider = config.OwnerProvider,
                Email = config.OwnerEmail,
                DisplayName = config.OwnerDisplayName,
                Role = "Owner",
                AddedAt = config.OwnerLinkedAt ?? DateTime.MinValue
            });
        }

        foreach (var admin in config.AdminUsers)
        {
            var existing = result.FirstOrDefault(x =>
                string.Equals(x.UserId, admin.UserId, StringComparison.Ordinal));
            if (existing is null)
            {
                result.Add(admin);
            }
            else if (string.Equals(existing.Role, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                existing.Provider = string.IsNullOrWhiteSpace(existing.Provider) ? admin.Provider : existing.Provider;
                existing.Email = string.IsNullOrWhiteSpace(existing.Email) ? admin.Email : existing.Email;
                existing.DisplayName = string.IsNullOrWhiteSpace(existing.DisplayName) ? admin.DisplayName : existing.DisplayName;
            }
        }

        return result
            .OrderBy(x => string.Equals(x.Role, "Owner", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(x => x.AddedAt)
            .ToList();
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
        url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase);
}
