using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Dreamine.Identity;
using Microsoft.AspNetCore.Components.Forms;
using Wedding.Common;
using WeddingPlatform.Models;
using WeddingPlatform.Services;

namespace WeddingPlatform.ViewModels;

/// <summary>
/// \if KO
/// <para>Wedding Admin View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates wedding admin view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class WeddingAdminViewModel
{
    /// <summary>
    /// \if KO
    /// <para>tenants 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the tenants value.</para>
    /// \endif
    /// </summary>
    private readonly ITenantStore _tenants;
    /// <summary>
    /// \if KO
    /// <para>photos 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the photos value.</para>
    /// \endif
    /// </summary>
    private readonly IPhotoService _photos;
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly WeddingOptions _opts;
    /// <summary>
    /// \if KO
    /// <para>user Context 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the user context value.</para>
    /// \endif
    /// </summary>
    private readonly WeddingUserContext _userContext;
    /// <summary>
    /// \if KO
    /// <para>media Policy Resolver 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the media policy resolver value.</para>
    /// \endif
    /// </summary>
    private readonly IMediaQuotaPolicyResolver _mediaPolicyResolver;
    /// <summary>
    /// \if KO
    /// <para>super Admin Tokens 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the super admin tokens value.</para>
    /// \endif
    /// </summary>
    private readonly ISuperAdminSessionTokenService _superAdminTokens;

    /// <summary>
    /// \if KO
    /// <para>geocode Http 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the geocode http value.</para>
    /// \endif
    /// </summary>
    private static readonly HttpClient _geocodeHttp = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CodemaruWeddingPlatform/1.0 (contact: admin@codemaru.co.kr)" } }
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="WeddingAdminViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="WeddingAdminViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="tenants">
    /// \if KO
    /// <para>tenants에 사용할 <c>ITenantStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ITenantStore</c> value used for tenants.</para>
    /// \endif
    /// </param>
    /// <param name="photos">
    /// \if KO
    /// <para>photos에 사용할 <c>IPhotoService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPhotoService</c> value used for photos.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>WeddingOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    /// <param name="userContext">
    /// \if KO
    /// <para>user Context에 사용할 <c>WeddingUserContext</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingUserContext</c> value used for user context.</para>
    /// \endif
    /// </param>
    /// <param name="mediaPolicyResolver">
    /// \if KO
    /// <para>media Policy Resolver에 사용할 <c>IMediaQuotaPolicyResolver</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IMediaQuotaPolicyResolver</c> value used for media policy resolver.</para>
    /// \endif
    /// </param>
    /// <param name="superAdminTokens">
    /// \if KO
    /// <para>super Admin Tokens에 사용할 <c>ISuperAdminSessionTokenService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ISuperAdminSessionTokenService</c> value used for super admin tokens.</para>
    /// \endif
    /// </param>
    public WeddingAdminViewModel(
        ITenantStore tenants,
        IPhotoService photos,
        WeddingOptions opts,
        WeddingUserContext userContext,
        IMediaQuotaPolicyResolver mediaPolicyResolver,
        ISuperAdminSessionTokenService superAdminTokens)
    {
        _tenants = tenants;
        _photos = photos;
        _opts = opts;
        _userContext = userContext;
        _mediaPolicyResolver = mediaPolicyResolver;
        _superAdminTokens = superAdminTokens;
    }

    /// <summary>
    /// \if KO
    /// <para>동영상 업로드 최대 용량 안내 문구 (예: "최대 200MB" 또는 "무제한").</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video size label value.</para>
    /// \endif
    /// </summary>
    public string MaxVideoSizeLabel { get; private set; } = "최대 200MB";
    /// <summary>
    /// \if KO
    /// <para>동영상 업로드 최대 개수 (0이면 무제한).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the max video count value.</para>
    /// \endif
    /// </summary>
    public int MaxVideoCount { get; private set; } = 6;
    /// <summary>
    /// \if KO
    /// <para>현재 계정에 적용되는 최종 미디어 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the effective media policy value.</para>
    /// \endif
    /// </summary>
    public EffectiveMediaPolicy? EffectiveMediaPolicy { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Config 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the config value.</para>
    /// \endif
    /// </summary>
    public TenantConfig? Config { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Gallery 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the gallery value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<PhotoInfo> Gallery { get; private set; } = [];
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
    /// <para>Effective Admin Users 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the effective admin users value.</para>
    /// \endif
    /// </summary>
    public IReadOnlyList<WeddingAdminUser> EffectiveAdminUsers { get; private set; } = [];
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
    /// <para>Is Geocoding 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is geocoding value.</para>
    /// \endif
    /// </summary>
    public bool IsGeocoding { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Geocode Status 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the geocode status value.</para>
    /// \endif
    /// </summary>
    public string GeocodeStatus { get; private set; } = "";

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
        IsOwner = IsOwnerUser(config, user);
        IsLinkedToCurrentUser = IsAdminUser(config, user);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(config);

        if (IsLinkedToCurrentUser)
        {
            IsAuthenticated = true;
            await LoadAsync(slug, ct).ConfigureAwait(false);
        }
    }

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
            EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
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
            EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
            StatusMessage = "CodeMaru/Dreamine 계정에 대표 관리자로 연결되었습니다. 다음부터는 공용 로그인으로 관리할 수 있습니다.";
        }
        else if (user.IsAuthenticated)
        {
            EnsureAdminUser(config, user, "Admin");
            await _tenants.SaveAsync(config, ct).ConfigureAwait(false);
            IsLinkedToCurrentUser = true;
            IsOwner = IsOwnerUser(config, user);
            EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
            StatusMessage = "현재 CodeMaru/Dreamine 계정이 이 청첩장의 관리자로 추가되었습니다.";
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
    /// <para>Login As Super Admin Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login as super admin async operation.</para>
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
    /// <param name="sessionToken">
    /// \if KO
    /// <para>session Token에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for session token.</para>
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
    /// <para>Login As Super Admin Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the login as super admin async operation.</para>
    /// \endif
    /// </returns>
    public async Task<bool> LoginAsSuperAdminAsync(string slug, string? sessionToken, CancellationToken ct = default)
    {
        if (!_superAdminTokens.ValidateToken(sessionToken))
        {
            return false;
        }

        var config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
        if (config is null)
        {
            StatusMessage = "존재하지 않는 슬러그입니다.";
            return false;
        }

        IsAuthenticated = true;
        IsLinkedToCurrentUser = false;
        IsOwner = false;
        EffectiveAdminUsers = BuildEffectiveAdminUsers(config);
        StatusMessage = "슈퍼관리자 권한으로 접속했습니다.";
        await LoadAsync(slug, ct).ConfigureAwait(false);
        return true;
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
                 ?? new TenantConfig { Slug = slug };
        InvitationDesignCatalog.Normalize(Config);
        Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);

        EffectiveMediaPolicy = await _mediaPolicyResolver.ResolveAsync(Config, ct).ConfigureAwait(false);

        var effectiveMb = EffectiveMediaPolicy.VideoMaxFileSizeMb;
        MaxVideoSizeLabel = effectiveMb <= 0 ? "무제한" : $"최대 {effectiveMb}MB";

        MaxVideoCount = EffectiveMediaPolicy.VideoMaxCount;

        IsLoaded = true;
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
            if (!ValidateLayoutForSave(Config))
            {
                return;
            }

            if (!ValidateThemeForSave(Config))
            {
                return;
            }

            InvitationDesignCatalog.Normalize(Config);
            Config.PasswordHash = DreaminePasswordHasher.HashPlainTextForStorage(Config.PasswordHash);
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
            StatusMessage = "설정이 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"저장 오류: {ex.Message}"; }
    }

    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the status message value.</para>
    /// \endif
    /// </summary>
    /// <param name="message">
    /// \if KO
    /// <para>처리할 메시지입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The message to process.</para>
    /// \endif
    /// </param>
    public void SetStatusMessage(string message) => StatusMessage = message;

    /// <summary>
    /// \if KO
    /// <para>Story Chapter Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves story chapter async data.</para>
    /// \endif
    /// </summary>
    /// <param name="chapter">
    /// \if KO
    /// <para>chapter에 사용할 <c>StoryChapter</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>StoryChapter</c> value used for chapter.</para>
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
    /// <para>Save Story Chapter Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save story chapter async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveStoryChapterAsync(StoryChapter chapter, CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            InvitationDesignCatalog.Normalize(Config);
            var chapters = Config.DesignSettings.StoryChapters;
            var index = chapters.FindIndex(x => x.ChapterNumber == chapter.ChapterNumber);
            var normalized = WeddingStoryChapterDefaults.Clone(chapter);
            normalized.Label = string.IsNullOrWhiteSpace(normalized.Label)
                ? $"CHAPTER {Math.Max(1, normalized.ChapterNumber):00}"
                : normalized.Label.Trim();
            normalized.Title = string.IsNullOrWhiteSpace(normalized.Title)
                ? "스토리"
                : normalized.Title.Trim();

            if (index >= 0)
            {
                chapters[index] = normalized;
            }
            else
            {
                chapters.Add(normalized);
                Config.DesignSettings.StoryChapters = WeddingStoryChapterDefaults.Normalize(chapters);
            }

            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            StatusMessage = $"{normalized.Label} 챕터가 저장되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"챕터 저장 오류: {ex.Message}";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Story Chapter Async 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the story chapter async item.</para>
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
    /// <para>Add Story Chapter Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the add story chapter async operation.</para>
    /// \endif
    /// </returns>
    public async Task AddStoryChapterAsync(CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            InvitationDesignCatalog.Normalize(Config);
            var chapters = Config.DesignSettings.StoryChapters;
            var nextNumber = chapters.Count == 0 ? 1 : chapters.Max(x => x.ChapterNumber) + 1;
            chapters.Add(new StoryChapter
            {
                ChapterNumber = nextNumber,
                Label = $"CHAPTER {nextNumber:00}",
                Title = "새로운 이야기",
            });

            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            StatusMessage = $"CHAPTER {nextNumber:00} 챕터가 추가되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"챕터 추가 오류: {ex.Message}";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Story Chapter Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete story chapter async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="chapterNumber">
    /// \if KO
    /// <para>chapter Number에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for chapter number.</para>
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
    /// <para>Delete Story Chapter Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete story chapter async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteStoryChapterAsync(int chapterNumber, CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            InvitationDesignCatalog.Normalize(Config);
            if (chapterNumber <= 4)
            {
                StatusMessage = "기본 4개 챕터는 유지됩니다.";
                return;
            }

            var removed = Config.DesignSettings.StoryChapters.RemoveAll(x => x.ChapterNumber == chapterNumber);
            if (removed == 0)
            {
                StatusMessage = "삭제할 챕터를 찾을 수 없습니다.";
                return;
            }

            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            StatusMessage = $"CHAPTER {chapterNumber:00} 챕터가 삭제되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"챕터 삭제 오류: {ex.Message}";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Layout For Save 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the layout for save value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Validate Layout For Save 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the validate layout for save condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool ValidateLayoutForSave(TenantConfig config)
    {
        config.DesignSettings ??= new DesignSettings();
        if (!WeddingLayoutCatalog.IsKnownKey(config.InvitationStyle))
        {
            StatusMessage = "저장 오류: 존재하지 않는 레이아웃입니다.";
            return false;
        }

        var mode = config.DesignSettings.LayoutMode;
        if (mode == WeddingLayoutMode.Unknown)
        {
            mode = InvitationDesignCatalog.FromLegacyLayoutKey(config.InvitationStyle);
            config.DesignSettings.LayoutMode = mode;
        }

        var option = WeddingLayoutCatalog.Instance.Find(mode);
        if (option is null)
        {
            StatusMessage = "저장 오류: 존재하지 않는 레이아웃입니다.";
            return false;
        }

        if (!option.IsImplemented)
        {
            StatusMessage = "저장 오류: 아직 준비 중인 레이아웃입니다.";
            return false;
        }

        var access = new WeddingLayoutAccessState
        {
            HasPremiumPlan = config.HasPremiumPlan,
            UnlockedLayouts = config.UnlockedLayoutModes
                .Select(WeddingLayoutCatalog.FromLegacyKey)
                .Where(x => x != WeddingLayoutMode.Unknown)
                .ToArray(),
        };

        if (!new WeddingLayoutAccessPolicy().CanUse(option, access))
        {
            StatusMessage = "저장 오류: 프리미엄 레이아웃은 플랜 또는 잠금 해제 후 저장할 수 있습니다.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// \if KO
    /// <para>Theme For Save 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the theme for save value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Validate Theme For Save 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the validate theme for save condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private bool ValidateThemeForSave(TenantConfig config)
    {
        config.DesignSettings ??= new DesignSettings();
        config.UnlockedThemeKeys ??= new();
        if (!WeddingThemeCatalog.IsKnownKey(config.DesignSettings.ThemeKey))
        {
            StatusMessage = "저장 오류: 존재하지 않는 테마입니다.";
            return false;
        }

        var option = WeddingThemeCatalog.Instance.Find(config.DesignSettings.ThemeKey);
        if (option is null)
        {
            StatusMessage = "저장 오류: 존재하지 않는 테마입니다.";
            return false;
        }

        if (!option.IsImplemented)
        {
            StatusMessage = "저장 오류: 아직 준비 중인 테마입니다.";
            return false;
        }

        var access = new WeddingThemeAccessState
        {
            HasPremiumPlan = config.HasPremiumPlan,
            UnlockedThemeKeys = config.UnlockedThemeKeys
                .Select(WeddingThemeCatalog.NormalizeKey)
                .Where(WeddingThemeCatalog.IsKnownKey)
                .ToArray(),
        };

        if (!new WeddingThemeAccessPolicy().CanUse(option, access))
        {
            StatusMessage = "저장 오류: 프리미엄 테마는 플랜 또는 잠금 해제 후 저장할 수 있습니다.";
            return false;
        }

        config.DesignSettings.ThemeKey = option.Key;
        config.ThemeName = option.Key;
        return true;
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
        if (Config is null) return;
        var user = await _userContext.GetCurrentAsync().ConfigureAwait(false);
        if (!IsOwnerUser(Config, user))
        {
            StatusMessage = "대표 관리자만 관리자 계정을 삭제할 수 있습니다.";
            return;
        }

        if (string.Equals(Config.OwnerUserId, userId, StringComparison.Ordinal))
        {
            StatusMessage = "대표 관리자는 삭제할 수 없습니다.";
            return;
        }

        var removed = GetAdminUsers(Config).RemoveAll(x => string.Equals(x.UserId, userId, StringComparison.Ordinal));
        if (removed == 0)
        {
            StatusMessage = "삭제할 관리자를 찾을 수 없습니다.";
            return;
        }

        await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
        EffectiveAdminUsers = BuildEffectiveAdminUsers(Config);
        StatusMessage = "관리자 계정이 삭제되었습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Gallery Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload gallery async operation.</para>
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
    /// <para>Upload Gallery Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload gallery async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadGalleryAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        StatusMessage = "";
        try
        {
            await _photos.UploadAsync(slug, file, ct).ConfigureAwait(false);
            // Config도 갱신해야 이후 설정 저장 시 GalleryFileNames가 날아가지 않음
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = $"{file.Name} 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Hero Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload hero async operation.</para>
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
    /// <para>Upload Hero Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload hero async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadHeroAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _photos.UploadHeroAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "히어로 이미지 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"히어로 업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Photo Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete photo async operation.</para>
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
    /// <para>Delete Photo Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete photo async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeletePhotoAsync(string slug, string fileName, CancellationToken ct = default)
    {
        try
        {
            await _photos.DeleteAsync(slug, fileName, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            Gallery = await _photos.GetGalleryAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "삭제 완료";
        }
        catch (Exception ex) { StatusMessage = $"삭제 오류: {ex.Message}"; }
    }

    /// <summary>
    /// \if KO
    /// <para>Move Gallery Photo Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the move gallery photo async operation.</para>
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
    /// <param name="offset">
    /// \if KO
    /// <para>offset에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for offset.</para>
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
    /// <para>Move Gallery Photo Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the move gallery photo async operation.</para>
    /// \endif
    /// </returns>
    public async Task MoveGalleryPhotoAsync(string fileName, int offset, CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            NormalizeGalleryFileOrder();
            var index = Config.GalleryFileNames.FindIndex(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return;

            var target = Math.Clamp(index + offset, 0, Config.GalleryFileNames.Count - 1);
            if (index == target) return;

            Config.GalleryFileNames.RemoveAt(index);
            Config.GalleryFileNames.Insert(target, fileName);
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            Gallery = await _photos.GetGalleryAsync(Config.Slug, ct).ConfigureAwait(false);
            StatusMessage = "사진 노출 순서가 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"순서 변경 오류: {ex.Message}"; }
    }

    /// <summary>
    /// \if KO
    /// <para>Move Gallery Photo To Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the move gallery photo to async operation.</para>
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
    /// <param name="first">
    /// \if KO
    /// <para>first에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for first.</para>
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
    /// <para>Move Gallery Photo To Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the move gallery photo to async operation.</para>
    /// \endif
    /// </returns>
    public async Task MoveGalleryPhotoToAsync(string fileName, bool first, CancellationToken ct = default)
    {
        if (Config is null) return;
        try
        {
            NormalizeGalleryFileOrder();
            var index = Config.GalleryFileNames.FindIndex(x => string.Equals(x, fileName, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return;

            Config.GalleryFileNames.RemoveAt(index);
            Config.GalleryFileNames.Insert(first ? 0 : Config.GalleryFileNames.Count, fileName);
            await _tenants.SaveAsync(Config, ct).ConfigureAwait(false);
            Gallery = await _photos.GetGalleryAsync(Config.Slug, ct).ConfigureAwait(false);
            StatusMessage = "사진 노출 순서가 저장되었습니다.";
        }
        catch (Exception ex) { StatusMessage = $"순서 변경 오류: {ex.Message}"; }
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Gallery File Order 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize gallery file order operation.</para>
    /// \endif
    /// </summary>
    private void NormalizeGalleryFileOrder()
    {
        if (Config is null) return;
        var existing = Gallery.Select(x => x.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Config.GalleryFileNames = Config.GalleryFileNames
            .Where(existing.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var photo in Gallery)
        {
            if (!Config.GalleryFileNames.Contains(photo.FileName, StringComparer.OrdinalIgnoreCase))
            {
                Config.GalleryFileNames.Add(photo.FileName);
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Road Map Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload road map async operation.</para>
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
    /// <para>Upload Road Map Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload road map async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadRoadMapAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _photos.UploadRoadMapAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "약도 이미지 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"약도 업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Geocode Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the geocode async operation.</para>
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
    /// <para>Geocode Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the geocode async operation.</para>
    /// \endif
    /// </returns>
    public async Task GeocodeAsync(CancellationToken ct = default)
    {
        if (Config is null) return;
        var query = string.IsNullOrWhiteSpace(Config.VenueAddress) ? Config.VenueName : Config.VenueAddress;
        if (string.IsNullOrWhiteSpace(query)) { GeocodeStatus = "❗ 예식장 이름 또는 주소를 먼저 입력해주세요."; return; }

        IsGeocoding = true;
        GeocodeStatus = "";
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit=1&accept-language=ko";
            var results = await _geocodeHttp.GetFromJsonAsync<JsonElement[]>(url, ct).ConfigureAwait(false);
            if (results is null || results.Length == 0)
            {
                GeocodeStatus = "❌ 좌표를 찾을 수 없습니다. 주소를 더 자세히 입력해 보세요.";
                return;
            }
            var item = results[0];
            Config.VenueLat = double.Parse(item.GetProperty("lat").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            Config.VenueLng = double.Parse(item.GetProperty("lon").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            GeocodeStatus = $"✅ 좌표 검색 완료 — {item.GetProperty("display_name").GetString()}";
        }
        catch { GeocodeStatus = "❌ 좌표 검색 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요."; }
        finally { IsGeocoding = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Generate Map Links 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the generate map links operation.</para>
    /// \endif
    /// </summary>
    public void GenerateMapLinks()
    {
        if (Config is null) return;

        var name = Uri.EscapeDataString(Config.VenueName);
        var addr = Uri.EscapeDataString(
            string.IsNullOrWhiteSpace(Config.VenueAddress) ? Config.VenueName : Config.VenueAddress);
        var lat = Config.VenueLat;
        var lng = Config.VenueLng;
        bool hasCoords = lat != 0 && lng != 0;
        var latS = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lngS = lng.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // 카카오맵 — 좌표 있으면 핀, 없으면 검색
        Config.MapLinkKakao = hasCoords
            ? $"https://map.kakao.com/link/map/{name},{latS},{lngS}"
            : $"https://map.kakao.com/link/search/{addr}";

        // 네이버지도 — 좌표 있으면 좌표 검색, 없으면 주소 검색
        Config.MapLinkNaver = hasCoords
            ? $"https://map.naver.com/v5/search/{addr}?c={lngS},{latS},15,0,0,0,dh"
            : $"https://map.naver.com/v5/search/{addr}";

        // 아틀란
        if (!string.IsNullOrWhiteSpace(_opts.AtlanAuthKey) && hasCoords)
            Config.MapLinkAtlan = $"http://m.atlan.co.kr/searchPlus/linkAtlan.do?shareType=kakao&coordX={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&coordY={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&title={name}&AuthKey={_opts.AtlanAuthKey}";

        // T맵
        if (!string.IsNullOrWhiteSpace(_opts.TmapAppKey) && hasCoords)
            Config.MapLinkTmap = $"https://apis.openapi.sk.com/tmap/app/routes?appKey={_opts.TmapAppKey}&name={name}&lon={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

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
    public string GetThumbUrl(string slug, string fileName) => _photos.GetThumbUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Photo Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the photo url value.</para>
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
    /// <para>Get Photo Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get photo url operation.</para>
    /// \endif
    /// </returns>
    public string GetPhotoUrl(string slug, string fileName) => _photos.GetPhotoUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Hero Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the hero url value.</para>
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
    /// <para>Get Hero Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get hero url operation.</para>
    /// \endif
    /// </returns>
    public string GetHeroUrl(string slug, string fileName) => _photos.GetHeroUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Road Map Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the road map url value.</para>
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
    /// <para>Get Road Map Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get road map url operation.</para>
    /// \endif
    /// </returns>
    public string GetRoadMapUrl(string slug, string fileName) => _photos.GetRoadMapUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Music Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the music url value.</para>
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
    /// <para>Get Music Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get music url operation.</para>
    /// \endif
    /// </returns>
    public string GetMusicUrl(string slug, string fileName) => _photos.GetMusicUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Og Image Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the og image url value.</para>
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
    /// <para>Get Og Image Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get og image url operation.</para>
    /// \endif
    /// </returns>
    public string GetOgImageUrl(string slug, string fileName) => _photos.GetHeroUrl(slug, fileName);
    /// <summary>
    /// \if KO
    /// <para>Video Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video url value.</para>
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
    /// <para>Get Video Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get video url operation.</para>
    /// \endif
    /// </returns>
    public string GetVideoUrl(string slug, string fileName) => _photos.GetVideoUrl(slug, fileName);

    /// <summary>
    /// \if KO
    /// <para>Upload Og Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload og image async operation.</para>
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
    /// <para>Upload Og Image Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload og image async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _photos.UploadOgImageAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "미리보기 이미지 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"미리보기 이미지 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Thank You Og Image Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload thank you og image async operation.</para>
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
    /// <para>Upload Thank You Og Image Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload thank you og image async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadThankYouOgImageAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _photos.UploadThankYouOgImageAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "감사장 미리보기 이미지 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"감사장 미리보기 이미지 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload video async operation.</para>
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
    /// <para>Upload Video Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload video async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadVideoAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _photos.UploadVideoAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "동영상 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"동영상 업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Video Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete video async operation.</para>
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
    /// <para>Delete Video Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete video async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteVideoAsync(string slug, string fileName, CancellationToken ct = default)
    {
        try
        {
            await _photos.DeleteVideoAsync(slug, fileName, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "동영상 삭제 완료";
        }
        catch (Exception ex) { StatusMessage = $"동영상 삭제 오류: {ex.Message}"; }
    }

    /// <summary>
    /// \if KO
    /// <para>Upload Music Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the upload music async operation.</para>
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
    /// <para>Upload Music Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the upload music async operation.</para>
    /// \endif
    /// </returns>
    public async Task UploadMusicAsync(string slug, IBrowserFile file, CancellationToken ct = default)
    {
        IsUploading = true;
        try
        {
            await _photos.UploadMusicAsync(slug, file, ct).ConfigureAwait(false);
            Config = await _tenants.GetAsync(slug, ct).ConfigureAwait(false);
            StatusMessage = "배경 음악 업로드 완료";
        }
        catch (Exception ex) { StatusMessage = $"음악 업로드 오류: {ex.Message}"; }
        finally { IsUploading = false; }
    }

    /// <summary>
    /// \if KO
    /// <para>Admin Users 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the admin users value.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Admin Users 작업에서 생성한 <c>List&lt;WeddingAdminUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;WeddingAdminUser&gt;</c> result produced by the get admin users operation.</para>
    /// \endif
    /// </returns>
    private static List<WeddingAdminUser> GetAdminUsers(TenantConfig config) => config.AdminUsers ??= [];

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
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>WeddingCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingCurrentUser</c> value used for user.</para>
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
    private static bool IsOwnerUser(TenantConfig config, WeddingCurrentUser user) =>
        user.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(config.OwnerUserId) &&
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
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>WeddingCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingCurrentUser</c> value used for user.</para>
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
    private static bool IsAdminUser(TenantConfig config, WeddingCurrentUser user)
    {
        if (!user.IsAuthenticated) return false;
        if (IsOwnerUser(config, user)) return true;
        return GetAdminUsers(config).Any(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
    }

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
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="user">
    /// \if KO
    /// <para>user에 사용할 <c>WeddingCurrentUser</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingCurrentUser</c> value used for user.</para>
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
    private static void EnsureAdminUser(TenantConfig config, WeddingCurrentUser user, string role)
    {
        if (!user.IsAuthenticated) return;

        var users = GetAdminUsers(config);
        var existing = users.FirstOrDefault(x => string.Equals(x.UserId, user.Id, StringComparison.Ordinal));
        if (existing is null)
        {
            users.Add(new WeddingAdminUser
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
        if (role == "Owner")
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
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Effective Admin Users 작업에서 생성한 <c>IReadOnlyList&lt;WeddingAdminUser&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;WeddingAdminUser&gt;</c> result produced by the build effective admin users operation.</para>
    /// \endif
    /// </returns>
    private static IReadOnlyList<WeddingAdminUser> BuildEffectiveAdminUsers(TenantConfig config)
    {
        var users = GetAdminUsers(config);
        if (!string.IsNullOrWhiteSpace(config.OwnerUserId) &&
            users.All(x => !string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
        {
            users.Insert(0, new WeddingAdminUser
            {
                UserId = config.OwnerUserId,
                Provider = config.OwnerProvider,
                Email = config.OwnerEmail,
                DisplayName = config.OwnerDisplayName,
                Role = "Owner",
                AddedAt = config.OwnerLinkedAt ?? config.CreatedAt
            });
        }

        foreach (var user in users.Where(x => string.Equals(x.UserId, config.OwnerUserId, StringComparison.Ordinal)))
        {
            user.Role = "Owner";
        }

        return users
            .OrderByDescending(x => string.Equals(x.Role, "Owner", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.AddedAt)
            .ToList();
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Hero Panel Position 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize hero panel position operation.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    private static void NormalizeHeroPanelPosition(TenantConfig config)
    {
        InvitationDesignCatalog.Normalize(config);
        config.InviteHeroTopVerticalDesktop = NormalizeOption(config.InviteHeroTopVerticalDesktop, ["top", "middle", "bottom"], "top");
        config.InviteHeroTopHorizontalDesktop = NormalizeOption(config.InviteHeroTopHorizontalDesktop, ["left", "center", "right"], "center");
        config.InviteHeroTopVerticalMobile = NormalizeOption(config.InviteHeroTopVerticalMobile, ["top", "middle", "bottom"], "top");
        config.InviteHeroTopHorizontalMobile = NormalizeOption(config.InviteHeroTopHorizontalMobile, ["left", "center", "right"], "center");
        config.InviteHeroBottomVerticalDesktop = NormalizeOption(config.InviteHeroBottomVerticalDesktop, ["top", "middle", "bottom"], "bottom");
        config.InviteHeroBottomHorizontalDesktop = NormalizeOption(config.InviteHeroBottomHorizontalDesktop, ["left", "center", "right"], "center");
        config.InviteHeroBottomVerticalMobile = NormalizeOption(config.InviteHeroBottomVerticalMobile, ["top", "middle", "bottom"], "bottom");
        config.InviteHeroBottomHorizontalMobile = NormalizeOption(config.InviteHeroBottomHorizontalMobile, ["left", "center", "right"], "center");
        config.HeroPanelVerticalDesktop = NormalizeOption(config.HeroPanelVerticalDesktop, ["top", "middle", "bottom"], "top");
        config.HeroPanelHorizontalDesktop = NormalizeOption(config.HeroPanelHorizontalDesktop, ["left", "center", "right"], "center");
        config.HeroPanelVerticalMobile = NormalizeOption(config.HeroPanelVerticalMobile, ["top", "middle", "bottom"], "top");
        config.HeroPanelHorizontalMobile = NormalizeOption(config.HeroPanelHorizontalMobile, ["left", "center", "right"], "center");
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Option 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize option operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="allowed">
    /// \if KO
    /// <para>allowed에 사용할 <c>string[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string[]</c> value used for allowed.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Option 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize option operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeOption(string? value, string[] allowed, string fallback)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) && allowed.Contains(normalized) ? normalized : fallback;
    }
}
