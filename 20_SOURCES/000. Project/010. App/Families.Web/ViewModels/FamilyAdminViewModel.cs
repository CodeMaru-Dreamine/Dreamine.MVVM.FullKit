using System.IO;
using Dreamine.Identity;
using FamiliesApp.Models;
using FamiliesApp.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace FamiliesApp.ViewModels;

/// <summary>
/// \if KO
/// <para>Family Admin View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates family admin view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class FamilyAdminViewModel
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
    /// <para>user Context 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the user context value.</para>
    /// \endif
    /// </summary>
    private readonly FamilyUserContext _userContext;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="FamilyAdminViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="FamilyAdminViewModel"/> class with the specified settings.</para>
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
    /// <param name="userContext">
    /// \if KO
    /// <para>user Context에 사용할 <c>FamilyUserContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyUserContext</c> value used for user context.</para>
    /// \endif
    /// </param>
    public FamilyAdminViewModel(IFamilyTenantStore tenants, IPostStore posts,
        IAlbumStore albums, IMediaService media, FamilyUserContext userContext)
    {
        _tenants = tenants;
        _posts = posts;
        _albums = albums;
        _media = media;
        _userContext = userContext;
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
    /// <para>Is Authenticated 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is authenticated value.</para>
    /// \endif
    /// </summary>
    public bool IsAuthenticated { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Signed In 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is signed in value.</para>
    /// \endif
    /// </summary>
    public bool IsSignedIn { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Linked To Current User 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is linked to current user value.</para>
    /// \endif
    /// </summary>
    public bool IsLinkedToCurrentUser { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Owner 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is owner value.</para>
    /// \endif
    /// </summary>
    public bool IsOwner { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; private set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Current User Label 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the current user label value.</para>
    /// \endif
    /// </summary>
    public string CurrentUserLabel { get; private set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Is Uploading 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is uploading value.</para>
    /// \endif
    /// </summary>
    public bool IsUploading { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Login Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the login password value.</para>
    /// \endif
    /// </summary>
    public string LoginPassword { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Effective Admin Users 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the effective admin users value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<FamilyAdminUser> EffectiveAdminUsers =>
        Config is null ? [] : BuildEffectiveAdminUsers(Config);

    // ── 포스트 편집 ─────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Editing Post 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the editing post value.</para>
    /// \endif
    /// </summary>
    public PostEntry? EditingPost { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Pending Files 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pending files value.</para>
    /// \endif
    /// </summary>
    public List<IBrowserFile> PendingFiles { get; private set; } = new();

    // ── 앨범 편집 ───────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>New Album Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new album name value.</para>
    /// \endif
    /// </summary>
    public string NewAlbumName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>New Album Desc 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the new album desc value.</para>
    /// \endif
    /// </summary>
    public string NewAlbumDesc { get; set; } = "";

    /// <summary>
    /// \if KO
    /// <para>Login Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login async operation.</para>
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
    /// <para>Login Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the login async operation.</para>
    /// \endif
    /// </returns>
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

        var verification = DreaminePasswordHasher.VerifyPassword(LoginPassword, config.PasswordHash, out var upgradedHash);
        IsAuthenticated = verification is not PasswordHashVerificationResult.Failed;
        if (!IsAuthenticated)
        {
            StatusMessage = "비밀번호가 틀렸습니다.";
            return false;
        }

        if (verification is PasswordHashVerificationResult.SuccessRehashNeeded && upgradedHash is not null)
        {
            config.PasswordHash = upgradedHash;
            await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
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

    /// <summary>
    /// \if KO
    /// <para>Initialize Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize async operation.</para>
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
    /// <para>Initialize Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the initialize async operation.</para>
    /// \endif
    /// </returns>
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
        Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false)
                 ?? new FamilyConfig { Slug = slug };
        Posts = await _posts.GetAllAsync(slug, ct).ConfigureAwait(false);
        Albums = await _albums.GetAllAsync(slug, ct).ConfigureAwait(false);
        IsLoaded = true;
    }

    /// <summary>
    /// \if KO
    /// <para>Admin Async 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the admin async item.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
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
    /// <para>Remove Admin Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the remove admin async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Refresh Current User Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh current user async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Refresh Current User Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the refresh current user async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Is Owner User 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is owner user.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>FamilyConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>FamilyCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Owner User 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is owner user condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsOwnerUser(FamilyConfig config, FamilyCurrentUser user) =>
        user.IsAuthenticated &&
        string.Equals(config.OwnerUserId, user.Id, StringComparison.Ordinal);

    /// <summary>
    /// \if KO
    /// <para>Is Admin User 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is admin user.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>FamilyConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>FamilyCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Admin User 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is admin user condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsAdminUser(FamilyConfig config, FamilyCurrentUser user) =>
        user.IsAuthenticated &&
        (IsOwnerUser(config, user) ||
         config.AdminUsers.Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal)));

    /// <summary>
    /// \if KO
    /// <para>Ensure Admin User 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the ensure admin user operation.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>FamilyConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>FamilyCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyCurrentUser</c> value used for user.</para>
    /// \endif
    /// </param>
    /// <param name="role">
    /// \if KO
    /// <para>role에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for role.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Effective Admin Users 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the effective admin users value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>FamilyConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Effective Admin Users 작업에서 생성한 <c>IReadOnlyList&lt;FamilyAdminUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;FamilyAdminUser&gt;</c> result produced by the build effective admin users operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Config Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves config async data.</para>
    /// \endif
    /// </summary>
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
    /// <para>Save Config Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save config async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveConfigAsync(CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            Config.PasswordHash = DreaminePasswordHasher.HashPlainTextForStorage(Config.PasswordHash);
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            StatusMessage = "설정이 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
    }

    /// <summary>
    /// \if KO
    /// <para>New Post 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the new post operation.</para>
    /// \endif
    /// </summary>
    public void NewPost()
    {
        EditingPost = new PostEntry { PostedAt = DateTime.Now };
        PendingFiles.Clear();
        StatusMessage = "";
    }

    /// <summary>
    /// \if KO
    /// <para>Edit Post 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the edit post operation.</para>
    /// \endif
    /// </summary>
    /// <param name="post">
    /// \if KO
    /// <para>post에 사용할 <c>PostEntry</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PostEntry</c> value used for post.</para>
    /// \endif
    /// </param>
    public void EditPost(PostEntry post)
    {
        EditingPost = post;
        PendingFiles.Clear();
        StatusMessage = "";
    }

    /// <summary>
    /// \if KO
    /// <para>Cancel Edit 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel edit.</para>
    /// \endif
    /// </summary>
    public void CancelEdit()
    {
        EditingPost = null;
        PendingFiles.Clear();
    }

    // 파일 선택 즉시 업로드 — 편집 상태 유지, 포스트 메타는 저장하지 않음
    /// <summary>
    /// \if KO
    /// <para>Upload Pending Files Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload pending files async operation.</para>
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
    /// <para>Upload Pending Files Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload pending files async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Post Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves post async data.</para>
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
    /// <para>Save Post Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save post async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Post Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete post async operation.</para>
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
    /// <para>Delete Post Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete post async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Post Media Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete post media async operation.</para>
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
    /// <param name="fileName">
    /// \if KO
    /// <para>file Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for file name.</para>
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
    /// <para>Delete Post Media Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete post media async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Upload Cover Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload cover async operation.</para>
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
    /// <param name="file">
    /// \if KO
    /// <para>file에 사용할 <c>IBrowserFile</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IBrowserFile</c> value used for file.</para>
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
    /// <para>Upload Cover Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload cover async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Album Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the album async item.</para>
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
    /// <para>Add Album Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the add album async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Album Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete album async operation.</para>
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
    /// <para>Delete Album Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete album async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Delete Self Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete self async operation.</para>
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
    /// <para>Delete Self Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete self async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteSelfAsync(string slug, CancellationToken ct = default)
    {
        await _tenants.DeleteAsync(slug, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Cover Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the cover url value.</para>
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
    /// <para>Get Cover Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get cover url operation.</para>
    /// \endif
    /// </returns>
    public string GetCoverUrl(string slug, string fileName) => _media.GetCoverUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Media Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the media url value.</para>
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
    public string GetMediaUrl(string slug, string postId, string fileName) => _media.GetMediaUrl(slug, postId, fileName);
    /// <summary>
    /// \if KO
    /// <para>Thumb Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the thumb url value.</para>
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
    public string GetThumbUrl(string slug, string postId, string fileName) => _media.GetThumbUrl(slug, postId, fileName);

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
}
