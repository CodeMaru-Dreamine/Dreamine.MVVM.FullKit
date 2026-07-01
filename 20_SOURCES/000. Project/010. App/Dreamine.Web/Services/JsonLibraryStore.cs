using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public sealed class JsonLibraryStore : ILibraryStore
{
    private readonly string _path;
    private List<LibraryInfo>? _cache;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonLibraryStore(DreamineOptions opts)
    {
        var dir = opts.ResolvedDataPath;
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "libraries.json");
    }

    public async Task<List<LibraryInfo>> GetAllAsync()
    {
        if (_cache is not null) return _cache;

        await _lock.WaitAsync();
        try
        {
            if (_cache is not null) return _cache;

            if (!File.Exists(_path))
            {
                _cache = SeedDefaults();
                await PersistAsync(_cache);
                return _cache;
            }

            var json = await File.ReadAllTextAsync(_path);
            _cache = JsonSerializer.Deserialize<List<LibraryInfo>>(json, _json) ?? [];

            // seed에는 있지만 JSON에 없는 항목 자동 머지 (기존 데이터 보존)
            var missing = SeedDefaults().Where(s => !_cache.Any(c => c.Id == s.Id)).ToList();
            if (missing.Count > 0)
            {
                _cache.AddRange(missing);
                await PersistAsync(_cache);
            }

            return _cache;
        }
        finally { _lock.Release(); }
    }

    public async Task<LibraryInfo?> GetAsync(string id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.Id == id);
    }

    public async Task SaveAsync(LibraryInfo lib)
    {
        await _lock.WaitAsync();
        try
        {
            var all = _cache ?? [];
            var idx = all.FindIndex(x => x.Id == lib.Id);
            lib.UpdatedAt = DateTime.UtcNow;
            if (idx >= 0) all[idx] = lib;
            else all.Add(lib);
            _cache = all;
            await PersistAsync(all);
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var all = _cache ?? [];
            all.RemoveAll(x => x.Id == id);
            _cache = all;
            await PersistAsync(all);
        }
        finally { _lock.Release(); }
    }

    private async Task PersistAsync(List<LibraryInfo> list)
    {
        var json = JsonSerializer.Serialize(list, _json);
        await File.WriteAllTextAsync(_path, json);
    }

    private static List<LibraryInfo> SeedDefaults() =>
    [
        new() { Id = "mvvm-core",          Name = "Dreamine.MVVM.Core",          Category = "MVVM",           Description = "ViewModel 기반 핵심 인프라. RelayCommand, ObservableObject, DI 자동 등록 포함.", Status = "stable", SortOrder = 1,  IsVisible = true, Tags = ["mvvm","core"] },
        new() { Id = "mvvm-viewmodels",    Name = "Dreamine.MVVM.ViewModels",    Category = "MVVM",           Description = "DreamineViewModel 베이스 클래스, IsBusy·StatusMessage 등 공통 상태 관리.", Status = "stable", SortOrder = 2,  IsVisible = true, Tags = ["mvvm","viewmodel"] },
        new() { Id = "mvvm-interfaces",    Name = "Dreamine.MVVM.Interfaces",    Category = "MVVM",           Description = "IViewModel, INavigationService 등 핵심 추상화 인터페이스 모음.", Status = "stable", SortOrder = 3,  IsVisible = true, Tags = ["mvvm","interface"] },
        new() { Id = "mvvm-behaviors",     Name = "Dreamine.MVVM.Behaviors",     Category = "MVVM",           Description = "플랫폼 독립 Behavior 베이스. EventToCommand 등 바인딩 헬퍼 포함.", Status = "stable", SortOrder = 4,  IsVisible = true, Tags = ["mvvm","behavior"] },
        new() { Id = "mvvm-behaviors-wpf", Name = "Dreamine.MVVM.Behaviors.Wpf", Category = "WPF",            Description = "WPF 전용 Behavior 확장 — 드래그·드롭, 포커스, 키 트리거 등.", Status = "stable", SortOrder = 5,  IsVisible = true, Tags = ["wpf","behavior"] },
        new() { Id = "mvvm-locators",      Name = "Dreamine.MVVM.Locators",      Category = "MVVM",           Description = "ServiceLocator 패턴 구현. DI 컨테이너를 통한 ViewModel 조회.", Status = "stable", SortOrder = 6,  IsVisible = true, Tags = ["mvvm","locator"] },
        new() { Id = "mvvm-locators-wpf",  Name = "Dreamine.MVVM.Locators.Wpf",  Category = "WPF",            Description = "WPF DataTemplate 자동 연결 + DesignTime 지원 ViewModel Locator.", Status = "stable", SortOrder = 7,  IsVisible = true, Tags = ["wpf","locator"] },
        new() { Id = "mvvm-extensions",    Name = "Dreamine.MVVM.Extensions",    Category = "MVVM",           Description = "IServiceCollection 확장 메서드 — ViewModel/Service 일괄 등록 등.", Status = "stable", SortOrder = 8,  IsVisible = true, Tags = ["mvvm","extensions"] },
        new() { Id = "mvvm-generators",    Name = "Dreamine.MVVM.Generators",    Category = "MVVM",           Description = "Source Generator — [AutoRegister], [AutoNotify] 어트리뷰트 기반 코드 자동 생성.", Status = "stable", SortOrder = 9,  IsVisible = true, Tags = ["mvvm","generator","source-generator"] },
        new() { Id = "mvvm-attributes",    Name = "Dreamine.MVVM.Attributes",    Category = "MVVM",           Description = "Generator·Framework에서 사용하는 어트리뷰트 정의 어셈블리.", Status = "stable", SortOrder = 10, IsVisible = true, Tags = ["mvvm","attribute"] },
        new() { Id = "mvvm-wpf",           Name = "Dreamine.MVVM.Wpf",           Category = "WPF",            Description = "WPF 진입점 통합 패키지 — App 부트스트랩, Window 생명주기 관리.", Status = "stable", SortOrder = 11, IsVisible = true, Tags = ["wpf","mvvm"] },
        new() { Id = "hybrid",             Name = "Dreamine.Hybrid",             Category = "Hybrid",         Description = "Blazor Server를 Kestrel로 임베드하는 크로스플랫폼 하이브리드 호스트 코어.", Status = "stable", SortOrder = 20, IsVisible = true, Tags = ["hybrid","blazor"] },
        new() { Id = "hybrid-wpf",         Name = "Dreamine.Hybrid.Wpf",         Category = "Hybrid",         Description = "WPF + 임베디드 Blazor Server 하이브리드 패턴. WebView2 통합 포함.", Status = "stable", SortOrder = 21, IsVisible = true, Tags = ["hybrid","wpf","blazor"] },
        new() { Id = "ui-abstractions",    Name = "Dreamine.UI.Abstractions",    Category = "UI",             Description = "플랫폼 공통 UI 인터페이스 — IPopupService, IThemeService 등.", Status = "stable", SortOrder = 30, IsVisible = true, Tags = ["ui","abstractions"] },
        new() { Id = "ui-wpf",             Name = "Dreamine.UI.Wpf",             Category = "UI",             Description = "WPF 전용 UI 통합 패키지 — 테마 적용, 팝업 서비스 연결.", Status = "stable", SortOrder = 31, IsVisible = true, Tags = ["ui","wpf"] },
        new() { Id = "ui-wpf-controls",    Name = "Dreamine.UI.Wpf.Controls",    Category = "UI",             Description = "다크 테마 WPF 커스텀 컨트롤 — DreamineButton, DreamineCheckLed, DreamineExpander 등.", Status = "stable", SortOrder = 32, IsVisible = true, Tags = ["ui","wpf","controls"] },
        new() { Id = "ui-winforms",        Name = "Dreamine.UI.WinForms",        Category = "UI",             Description = "WinForms 다크 테마 컨트롤 라이브러리 — WPF Controls와 API 패리티 유지.", Status = "stable", SortOrder = 33, IsVisible = true, Tags = ["ui","winforms","controls"] },
        new() { Id = "ui-blazor",          Name = "Dreamine.UI.Blazor",          Category = "UI",             Description = "Blazor 다크 테마 컴포넌트 라이브러리 — DreamineDialogService, Popup 등.", Status = "stable", SortOrder = 34, IsVisible = true, Tags = ["ui","blazor","components"] },
        new() { Id = "ui-maui",            Name = "Dreamine.UI.Maui",            Category = "UI",             Description = ".NET MAUI 다크 테마 컨트롤 라이브러리 — Cross-platform API 패리티.", Status = "stable", SortOrder = 35, IsVisible = true, Tags = ["ui","maui","controls"] },
        new() { Id = "logging",            Name = "Dreamine.Logging",            Category = "Infrastructure",  Description = "구조화된 로깅 코어 — ILogSink, LogEntry, 파일/메모리 싱크 포함.", Status = "stable", SortOrder = 40, IsVisible = true, Tags = ["logging"] },
        new() { Id = "logging-wpf",        Name = "Dreamine.Logging.Wpf",        Category = "Infrastructure",  Description = "WPF 전용 로그 싱크 — UI 스레드 안전 로그 바인딩.", Status = "stable", SortOrder = 41, IsVisible = true, Tags = ["logging","wpf"] },
        new() { Id = "threading",          Name = "Dreamine.Threading",          Category = "Infrastructure",  Description = "비동기 작업 헬퍼 — AsyncQueue, PeriodicWorker, CancellationScope.", Status = "stable", SortOrder = 50, IsVisible = true, Tags = ["threading","async"] },
        new() { Id = "database-core",      Name = "Dreamine.Database.Core",      Category = "Database",       Description = "데이터베이스 추상화 코어 — IRepository, IUnitOfWork, Dapper 통합.", Status = "stable", SortOrder = 60, IsVisible = true, Tags = ["database","dapper"] },
    ];
}
