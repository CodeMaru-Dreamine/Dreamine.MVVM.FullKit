using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;

namespace DreamineWeb.ViewModels;

/// <summary>
/// \if KO
/// <para>Admin View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates admin view model functionality and related state.</para>
/// \endif
/// </summary>
public class AdminViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the store value.</para>
    /// \endif
    /// </summary>
    private readonly ILibraryStore _store;
    /// <summary>
    /// \if KO
    /// <para>playground Store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the playground store value.</para>
    /// \endif
    /// </summary>
    private readonly IPlaygroundStore _playgroundStore;
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly DreamineOptions _opts;
    /// <summary>
    /// \if KO
    /// <para>auto Linker 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the auto linker value.</para>
    /// \endif
    /// </summary>
    private readonly XmlDocAutoLinker _autoLinker;
    /// <summary>
    /// \if KO
    /// <para>catalog Sync 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the catalog sync value.</para>
    /// \endif
    /// </summary>
    private readonly LibraryCatalogSyncService _catalogSync;
    /// <summary>
    /// \if KO
    /// <para>version Sync 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the version sync value.</para>
    /// \endif
    /// </summary>
    private readonly VersionSyncService _versionSync;
    /// <summary>
    /// \if KO
    /// <para>site Settings 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the site settings value.</para>
    /// \endif
    /// </summary>
    private readonly SiteSettingsService _siteSettings;
    /// <summary>
    /// \if KO
    /// <para>auth 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the auth value.</para>
    /// \endif
    /// </summary>
    private readonly AdminAuthService _auth;

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
    /// <para>Login Password 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the login password value.</para>
    /// \endif
    /// </summary>
    public string LoginPassword { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Libraries 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the libraries value.</para>
    /// \endif
    /// </summary>
    public List<LibraryInfo> Libraries { get; private set; } = [];

    // 라이브러리 편집
    /// <summary>
    /// \if KO
    /// <para>Editing 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the editing value.</para>
    /// \endif
    /// </summary>
    public LibraryInfo? Editing { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Editing 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is editing value.</para>
    /// \endif
    /// </summary>
    public bool IsEditing => Editing is not null;

    // 사이트 설정 편집
    /// <summary>
    /// \if KO
    /// <para>Site Edit 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the site edit value.</para>
    /// \endif
    /// </summary>
    public SiteSettings SiteEdit { get; private set; } = new();
    /// <summary>
    /// \if KO
    /// <para>Is Site Editing 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is site editing value.</para>
    /// \endif
    /// </summary>
    public bool IsSiteEditing { get; private set; }

    // 체험(Playground) 데모
    /// <summary>
    /// \if KO
    /// <para>Demos 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the demos value.</para>
    /// \endif
    /// </summary>
    public List<PlaygroundDemo> Demos { get; private set; } = [];
    /// <summary>
    /// \if KO
    /// <para>Editing Demo 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the editing demo value.</para>
    /// \endif
    /// </summary>
    public PlaygroundDemo? EditingDemo { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Is Demo Editing 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is demo editing value.</para>
    /// \endif
    /// </summary>
    public bool IsDemoEditing => EditingDemo is not null;

    // 패스워드 변경
    /// <summary>
    /// \if KO
    /// <para>Pw Current 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pw current value.</para>
    /// \endif
    /// </summary>
    public string PwCurrent  { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Pw New 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pw new value.</para>
    /// \endif
    /// </summary>
    public string PwNew      { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Pw Confirm 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the pw confirm value.</para>
    /// \endif
    /// </summary>
    public string PwConfirm  { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="AdminViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="AdminViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="store">
    /// \if KO
    /// <para>store에 사용할 <c>ILibraryStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILibraryStore</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <param name="playgroundStore">
    /// \if KO
    /// <para>playground Store에 사용할 <c>IPlaygroundStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IPlaygroundStore</c> value used for playground store.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    /// <param name="autoLinker">
    /// \if KO
    /// <para>auto Linker에 사용할 <c>XmlDocAutoLinker</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>XmlDocAutoLinker</c> value used for auto linker.</para>
    /// \endif
    /// </param>
    /// <param name="catalogSync">
    /// \if KO
    /// <para>catalog Sync에 사용할 <c>LibraryCatalogSyncService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LibraryCatalogSyncService</c> value used for catalog sync.</para>
    /// \endif
    /// </param>
    /// <param name="versionSync">
    /// \if KO
    /// <para>version Sync에 사용할 <c>VersionSyncService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>VersionSyncService</c> value used for version sync.</para>
    /// \endif
    /// </param>
    /// <param name="siteSettings">
    /// \if KO
    /// <para>site Settings에 사용할 <c>SiteSettingsService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SiteSettingsService</c> value used for site settings.</para>
    /// \endif
    /// </param>
    /// <param name="auth">
    /// \if KO
    /// <para>auth에 사용할 <c>AdminAuthService</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AdminAuthService</c> value used for auth.</para>
    /// \endif
    /// </param>
    public AdminViewModel(
        ILibraryStore store,
        IPlaygroundStore playgroundStore,
        DreamineOptions opts,
        XmlDocAutoLinker autoLinker,
        LibraryCatalogSyncService catalogSync,
        VersionSyncService versionSync,
        SiteSettingsService siteSettings,
        AdminAuthService auth)
    {
        _store = store;
        _playgroundStore = playgroundStore;
        _opts = opts;
        _autoLinker = autoLinker;
        _catalogSync = catalogSync;
        _versionSync = versionSync;
        _siteSettings = siteSettings;
        _auth = auth;
    }

    /// <summary>
    /// \if KO
    /// <para>Login Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the login async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Login Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the login async operation.</para>
    /// \endif
    /// </returns>
    public Task<bool> LoginAsync()
    {
        if (_auth.Verify(LoginPassword))
        {
            IsAuthenticated = true;
            StatusMessage = string.Empty;
            return Task.FromResult(true);
        }
        StatusMessage = "비밀번호가 올바르지 않습니다.";
        return Task.FromResult(false);
    }

    /// <summary>
    /// \if KO
    /// <para>Change Password Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the change password async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Change Password Async 작업에서 생성한 <c>Task&lt;string?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;string?&gt;</c> result produced by the change password async operation.</para>
    /// \endif
    /// </returns>
    public async Task<string?> ChangePasswordAsync()
    {
        if (!_auth.Verify(PwCurrent))
            return "현재 비밀번호가 올바르지 않습니다.";
        if (string.IsNullOrWhiteSpace(PwNew) || PwNew.Length < 8)
            return "새 비밀번호는 8자 이상이어야 합니다.";
        if (PwNew != PwConfirm)
            return "새 비밀번호와 확인이 일치하지 않습니다.";

        await _auth.ChangePasswordAsync(PwNew);
        PwCurrent = PwNew = PwConfirm = string.Empty;
        return null; // null = success
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadAsync()
    {
        Libraries = await _store.GetAllAsync();
        Demos = await _playgroundStore.GetAllAsync();
        StatusMessage = string.Empty;
    }

    // ── 라이브러리 편집 ──────────────────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Start New 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start new operation.</para>
    /// \endif
    /// </summary>
    public void StartNew()
    {
        Editing = new LibraryInfo { Id = Guid.NewGuid().ToString("N")[..8] };
        IsSiteEditing = false;
        EditingDemo = null;
    }

    /// <summary>
    /// \if KO
    /// <para>Start Edit 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start edit operation.</para>
    /// \endif
    /// </summary>
    /// <param name="lib">
    /// \if KO
    /// <para>lib에 사용할 <c>LibraryInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>LibraryInfo</c> value used for lib.</para>
    /// \endif
    /// </param>
    public void StartEdit(LibraryInfo lib)
    {
        Editing = new LibraryInfo
        {
            Id = lib.Id,
            Name = lib.Name,
            Description = lib.Description,
            DescriptionEn = lib.DescriptionEn,
            Category = lib.Category,
            Status = lib.Status,
            Version = lib.Version,
            NuGetId = lib.NuGetId,
            RepoUrl = lib.RepoUrl,
            XmlDocPath = lib.XmlDocPath,
            SourceProjectPath = lib.SourceProjectPath,
            TargetFramework = lib.TargetFramework,
            Dependencies = [.. lib.Dependencies],
            Tags = [.. lib.Tags],
            SortOrder = lib.SortOrder,
            IsVisible = lib.IsVisible
        };
        IsSiteEditing = false;
        EditingDemo = null;
    }

    /// <summary>
    /// \if KO
    /// <para>Cancel Edit 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel edit.</para>
    /// \endif
    /// </summary>
    public void CancelEdit() => Editing = null;

    /// <summary>
    /// \if KO
    /// <para>Edit Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves edit async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Edit Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save edit async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveEditAsync()
    {
        if (Editing is null) return;
        await _store.SaveAsync(Editing);
        Editing = null;
        await LoadAsync();
        StatusMessage = "✅ 저장됐습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
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
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteAsync(string id)
    {
        await _store.DeleteAsync(id);
        await LoadAsync();
        StatusMessage = "✅ 삭제됐습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Auto Link Xml Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the auto link xml async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Auto Link Xml Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the auto link xml async operation.</para>
    /// \endif
    /// </returns>
    public async Task AutoLinkXmlAsync()
    {
        var count = await _autoLinker.LinkAsync();
        await LoadAsync();
        StatusMessage = count > 0
            ? $"✅ XML 문서 {count}개 자동 연결됐습니다."
            : "XML 파일을 찾지 못했습니다. 빌드 여부를 확인하세요.";
    }

    /// <summary>
    /// \if KO
    /// <para>Sync Versions Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync versions async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Sync Versions Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the sync versions async operation.</para>
    /// \endif
    /// </returns>
    public async Task SyncVersionsAsync()
    {
        var count = await _versionSync.SyncAsync();
        await LoadAsync();
        StatusMessage = count switch
        {
            -1 => "LibrarySourceRoot 경로가 설정되지 않았거나 존재하지 않습니다.",
            0 => "모든 버전이 최신입니다.",
            _ => $"✅ {count}개 라이브러리 버전이 동기화됐습니다."
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Sync Catalog Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync catalog async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Sync Catalog Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the sync catalog async operation.</para>
    /// \endif
    /// </returns>
    public async Task SyncCatalogAsync()
    {
        var result = await _catalogSync.SyncAsync();
        await LoadAsync();
        StatusMessage = result.Added switch
        {
            -1 => "LibrarySourceRoot 경로가 설정되지 않았거나 존재하지 않습니다.",
            _ when result.Added == 0 && result.Updated == 0 => $"프로젝트 {result.ScannedProjects}개 확인 완료. 변경 사항은 없습니다.",
            _ => $"✅ 프로젝트 {result.ScannedProjects}개 스캔: 신규 {result.Added}개, 갱신 {result.Updated}개 반영됐습니다."
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Editing Tags 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the editing tags value.</para>
    /// \endif
    /// </summary>
    /// <param name="raw">
    /// \if KO
    /// <para>raw에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for raw.</para>
    /// \endif
    /// </param>
    public void SetEditingTags(string raw)
    {
        if (Editing is null) return;
        Editing.Tags = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    // ── 체험(Playground) 데모 편집 ──────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Start New Demo 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start new demo operation.</para>
    /// \endif
    /// </summary>
    public void StartNewDemo()
    {
        EditingDemo = new PlaygroundDemo
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            SortOrder = (Demos.Count == 0 ? 0 : Demos.Max(d => d.SortOrder)) + 10
        };
        Editing = null;
        IsSiteEditing = false;
    }

    /// <summary>
    /// \if KO
    /// <para>Start Edit Demo 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start edit demo operation.</para>
    /// \endif
    /// </summary>
    /// <param name="demo">
    /// \if KO
    /// <para>demo에 사용할 <c>PlaygroundDemo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PlaygroundDemo</c> value used for demo.</para>
    /// \endif
    /// </param>
    public void StartEditDemo(PlaygroundDemo demo)
    {
        EditingDemo = new PlaygroundDemo
        {
            Id = demo.Id,
            Title = demo.Title,
            TitleEn = demo.TitleEn,
            Description = demo.Description,
            DescriptionEn = demo.DescriptionEn,
            SortOrder = demo.SortOrder,
            IsVisible = demo.IsVisible,
            NavLabel = demo.NavLabel,
            BlazorCode = demo.BlazorCode,
            WpfCode = demo.WpfCode,
            WinFormsCode = demo.WinFormsCode,
            MauiCode = demo.MauiCode,
            VmCode = demo.VmCode,
            WpfShot = demo.WpfShot,
            WinFormsShot = demo.WinFormsShot,
            MauiShot = demo.MauiShot
        };
        Editing = null;
        IsSiteEditing = false;
    }

    /// <summary>
    /// \if KO
    /// <para>Cancel Demo Edit 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel demo edit.</para>
    /// \endif
    /// </summary>
    public void CancelDemoEdit() => EditingDemo = null;

    /// <summary>
    /// \if KO
    /// <para>Demo Edit Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves demo edit async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Demo Edit Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save demo edit async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveDemoEditAsync()
    {
        if (EditingDemo is null) return;
        await _playgroundStore.SaveAsync(EditingDemo);
        EditingDemo = null;
        await LoadAsync();
        StatusMessage = "✅ 체험 데모가 저장됐습니다.";
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Demo Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete demo async operation.</para>
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
    /// <para>Delete Demo Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete demo async operation.</para>
    /// \endif
    /// </returns>
    public async Task DeleteDemoAsync(string id)
    {
        await _playgroundStore.DeleteAsync(id);
        await LoadAsync();
        StatusMessage = "✅ 체험 데모가 삭제됐습니다.";
    }

    // ── 사이트 설정 ──────────────────────────────────────────────

    /// <summary>
    /// \if KO
    /// <para>Start Site Edit 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start site edit operation.</para>
    /// \endif
    /// </summary>
    public void StartSiteEdit()
    {
        var cur = _siteSettings.Current;
        SiteEdit = new SiteSettings
        {
            Title       = cur.Title,
            Description = cur.Description,
            IconUrl     = cur.IconUrl,
            GitHubUrl   = cur.GitHubUrl,
            OgTitle       = cur.OgTitle,
            OgDescription = cur.OgDescription,
            OgImageUrl    = cur.OgImageUrl,
            OgSiteName    = cur.OgSiteName,
            OgUrl         = cur.OgUrl,
            TwitterCard   = cur.TwitterCard,
        };
        IsSiteEditing = true;
        Editing = null;
        EditingDemo = null;
    }

    /// <summary>
    /// \if KO
    /// <para>Cancel Site Edit 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether cancel site edit.</para>
    /// \endif
    /// </summary>
    public void CancelSiteEdit() => IsSiteEditing = false;

    /// <summary>
    /// \if KO
    /// <para>Site Settings Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves site settings async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Save Site Settings Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save site settings async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveSiteSettingsAsync()
    {
        await _siteSettings.SaveAsync(SiteEdit);
        IsSiteEditing = false;
        StatusMessage = "✅ 사이트 설정이 저장됐습니다.";
    }
}
