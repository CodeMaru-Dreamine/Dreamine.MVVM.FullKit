using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;

namespace DreamineWeb.ViewModels;

public class AdminViewModel : ViewModelBase
{
    private readonly ILibraryStore _store;
    private readonly IPlaygroundStore _playgroundStore;
    private readonly DreamineOptions _opts;
    private readonly XmlDocAutoLinker _autoLinker;
    private readonly LibraryCatalogSyncService _catalogSync;
    private readonly VersionSyncService _versionSync;
    private readonly SiteSettingsService _siteSettings;
    private readonly AdminAuthService _auth;

    public bool IsAuthenticated { get; private set; }
    public string LoginPassword { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;

    public List<LibraryInfo> Libraries { get; private set; } = [];

    // 라이브러리 편집
    public LibraryInfo? Editing { get; private set; }
    public bool IsEditing => Editing is not null;

    // 사이트 설정 편집
    public SiteSettings SiteEdit { get; private set; } = new();
    public bool IsSiteEditing { get; private set; }

    // 체험(Playground) 데모
    public List<PlaygroundDemo> Demos { get; private set; } = [];
    public PlaygroundDemo? EditingDemo { get; private set; }
    public bool IsDemoEditing => EditingDemo is not null;

    // 패스워드 변경
    public string PwCurrent  { get; set; } = string.Empty;
    public string PwNew      { get; set; } = string.Empty;
    public string PwConfirm  { get; set; } = string.Empty;

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

    public async Task<string?> ChangePasswordAsync()
    {
        if (!_auth.Verify(PwCurrent))
            return "현재 비밀번호가 올바르지 않습니다.";
        if (string.IsNullOrWhiteSpace(PwNew) || PwNew.Length < 6)
            return "새 비밀번호는 6자 이상이어야 합니다.";
        if (PwNew != PwConfirm)
            return "새 비밀번호와 확인이 일치하지 않습니다.";

        await _auth.ChangePasswordAsync(PwNew);
        PwCurrent = PwNew = PwConfirm = string.Empty;
        return null; // null = success
    }

    public async Task LoadAsync()
    {
        Libraries = await _store.GetAllAsync();
        Demos = await _playgroundStore.GetAllAsync();
        StatusMessage = string.Empty;
    }

    // ── 라이브러리 편집 ──────────────────────────────────────────

    public void StartNew()
    {
        Editing = new LibraryInfo { Id = Guid.NewGuid().ToString("N")[..8] };
        IsSiteEditing = false;
        EditingDemo = null;
    }

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

    public void CancelEdit() => Editing = null;

    public async Task SaveEditAsync()
    {
        if (Editing is null) return;
        await _store.SaveAsync(Editing);
        Editing = null;
        await LoadAsync();
        StatusMessage = "✅ 저장됐습니다.";
    }

    public async Task DeleteAsync(string id)
    {
        await _store.DeleteAsync(id);
        await LoadAsync();
        StatusMessage = "✅ 삭제됐습니다.";
    }

    public async Task AutoLinkXmlAsync()
    {
        var count = await _autoLinker.LinkAsync();
        await LoadAsync();
        StatusMessage = count > 0
            ? $"✅ XML 문서 {count}개 자동 연결됐습니다."
            : "XML 파일을 찾지 못했습니다. 빌드 여부를 확인하세요.";
    }

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

    public void SetEditingTags(string raw)
    {
        if (Editing is null) return;
        Editing.Tags = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    // ── 체험(Playground) 데모 편집 ──────────────────────────────

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

    public void CancelDemoEdit() => EditingDemo = null;

    public async Task SaveDemoEditAsync()
    {
        if (EditingDemo is null) return;
        await _playgroundStore.SaveAsync(EditingDemo);
        EditingDemo = null;
        await LoadAsync();
        StatusMessage = "✅ 체험 데모가 저장됐습니다.";
    }

    public async Task DeleteDemoAsync(string id)
    {
        await _playgroundStore.DeleteAsync(id);
        await LoadAsync();
        StatusMessage = "✅ 체험 데모가 삭제됐습니다.";
    }

    // ── 사이트 설정 ──────────────────────────────────────────────

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

    public void CancelSiteEdit() => IsSiteEditing = false;

    public async Task SaveSiteSettingsAsync()
    {
        await _siteSettings.SaveAsync(SiteEdit);
        IsSiteEditing = false;
        StatusMessage = "✅ 사이트 설정이 저장됐습니다.";
    }
}
