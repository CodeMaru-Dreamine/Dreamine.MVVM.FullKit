using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Json Library Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json library store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonLibraryStore : ILibraryStore
{
    /// <summary>
    /// \if KO
    /// <para>path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the path value.</para>
    /// \endif
    /// </summary>
    private readonly string _path;
    /// <summary>
    /// \if KO
    /// <para>cache 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cache value.</para>
    /// \endif
    /// </summary>
    private List<LibraryInfo>? _cache;
    /// <summary>
    /// \if KO
    /// <para>lock 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the lock value.</para>
    /// \endif
    /// </summary>
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// \if KO
    /// <para>json 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonLibraryStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonLibraryStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public JsonLibraryStore(DreamineOptions opts)
    {
        var dir = opts.ResolvedDataPath;
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "libraries.json");
    }

    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;List&lt;LibraryInfo&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;LibraryInfo&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
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

            bool dirty = false;

            // 시드에 있지만 JSON에 없는 항목 추가
            var missing = SeedDefaults().Where(s => !_cache.Any(c => c.Id == s.Id)).ToList();
            if (missing.Count > 0)
            {
                _cache.AddRange(missing);
                dirty = true;
            }

            // 기존 항목에 시드에서 채울 수 있는 필드 백필
            var seedMap = SeedDefaults().ToDictionary(s => s.Id);
            foreach (var lib in _cache)
            {
                if (!seedMap.TryGetValue(lib.Id, out var seed)) continue;

                if (string.IsNullOrEmpty(lib.DescriptionEn) && !string.IsNullOrEmpty(seed.DescriptionEn))
                { lib.DescriptionEn = seed.DescriptionEn; dirty = true; }

                if (string.IsNullOrEmpty(lib.RepoUrl) && !string.IsNullOrEmpty(seed.RepoUrl))
                { lib.RepoUrl = seed.RepoUrl; dirty = true; }
            }

            if (dirty) await PersistAsync(_cache);

            return _cache;
        }
        finally { _lock.Release(); }
    }

    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;LibraryInfo?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;LibraryInfo?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<LibraryInfo?> GetAsync(string id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Persist Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the persist async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="list">
    /// \if KO
    /// <para>list에 사용할 <c>List&lt;LibraryInfo&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;LibraryInfo&gt;</c> value used for list.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Persist Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the persist async operation.</para>
    /// \endif
    /// </returns>
    private async Task PersistAsync(List<LibraryInfo> list)
    {
        var json = JsonSerializer.Serialize(list, _json);
        await File.WriteAllTextAsync(_path, json);
    }

    /// <summary>
    /// \if KO
    /// <para>Seed Defaults 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the seed defaults operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Seed Defaults 작업에서 생성한 <c>List&lt;LibraryInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;LibraryInfo&gt;</c> result produced by the seed defaults operation.</para>
    /// \endif
    /// </returns>
    private static List<LibraryInfo> SeedDefaults() =>
    [
        new() {
            Id = "mvvm-core", Name = "Dreamine.MVVM.Core", Category = "MVVM", SortOrder = 1, Status = "stable", IsVisible = true,
            Tags = ["mvvm","core"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Core",
            Description   = "ViewModel 기반 핵심 인프라. RelayCommand, ObservableObject, DI 자동 등록 포함.",
            DescriptionEn = "Core infrastructure for ViewModel-based architecture. Includes RelayCommand, ObservableObject, and automatic DI registration."
        },
        new() {
            Id = "mvvm-viewmodels", Name = "Dreamine.MVVM.ViewModels", Category = "MVVM", SortOrder = 2, Status = "stable", IsVisible = true,
            Tags = ["mvvm","viewmodel"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.ViewModels",
            Description   = "DreamineViewModel 베이스 클래스, IsBusy·StatusMessage 등 공통 상태 관리.",
            DescriptionEn = "DreamineViewModel base classes with common state management — IsBusy, StatusMessage, and lifecycle hooks."
        },
        new() {
            Id = "mvvm-interfaces", Name = "Dreamine.MVVM.Interfaces", Category = "MVVM", SortOrder = 3, Status = "stable", IsVisible = true,
            Tags = ["mvvm","interface"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Interfaces",
            Description   = "IViewModel, INavigationService 등 핵심 추상화 인터페이스 모음.",
            DescriptionEn = "Core abstraction interfaces — IViewModel, INavigationService, and related contracts."
        },
        new() {
            Id = "mvvm-behaviors", Name = "Dreamine.MVVM.Behaviors", Category = "MVVM", SortOrder = 4, Status = "stable", IsVisible = true,
            Tags = ["mvvm","behavior"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors",
            Description   = "플랫폼 독립 Behavior 베이스. EventToCommand 등 바인딩 헬퍼 포함.",
            DescriptionEn = "Platform-independent Behavior base. Includes EventToCommand and other binding helpers."
        },
        new() {
            Id = "mvvm-behaviors-core", Name = "Dreamine.MVVM.Behaviors.Core", Category = "MVVM", SortOrder = 5, Status = "stable", IsVisible = true,
            Tags = ["mvvm","behavior","core"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors.Core",
            Description   = "플랫폼 공통 Behavior 코어 인터페이스 및 기반 클래스.",
            DescriptionEn = "Core interfaces and base classes for cross-platform behaviors."
        },
        new() {
            Id = "mvvm-behaviors-wpf", Name = "Dreamine.MVVM.Behaviors.Wpf", Category = "WPF", SortOrder = 6, Status = "stable", IsVisible = true,
            Tags = ["wpf","behavior"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Behaviors.Wpf",
            Description   = "WPF 전용 Behavior 확장 — 드래그·드롭, 포커스, 키 트리거 등.",
            DescriptionEn = "WPF-specific Behavior extensions — drag-drop, focus, key trigger, and more."
        },
        new() {
            Id = "mvvm-locators", Name = "Dreamine.MVVM.Locators", Category = "MVVM", SortOrder = 7, Status = "stable", IsVisible = true,
            Tags = ["mvvm","locator"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Locators",
            Description   = "ServiceLocator 패턴 구현. DI 컨테이너를 통한 ViewModel 조회.",
            DescriptionEn = "ServiceLocator pattern implementation for ViewModel resolution via DI container."
        },
        new() {
            Id = "mvvm-locators-wpf", Name = "Dreamine.MVVM.Locators.Wpf", Category = "WPF", SortOrder = 8, Status = "stable", IsVisible = true,
            Tags = ["wpf","locator"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Locators.Wpf",
            Description   = "WPF DataTemplate 자동 연결 + DesignTime 지원 ViewModel Locator.",
            DescriptionEn = "WPF DataTemplate auto-wiring and design-time ViewModel Locator support."
        },
        new() {
            Id = "mvvm-extensions", Name = "Dreamine.MVVM.Extensions", Category = "MVVM", SortOrder = 9, Status = "stable", IsVisible = true,
            Tags = ["mvvm","extensions"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Extensions",
            Description   = "IServiceCollection 확장 메서드 — ViewModel/Service 일괄 등록 등.",
            DescriptionEn = "IServiceCollection extension methods for bulk ViewModel and Service registration."
        },
        new() {
            Id = "mvvm-generators", Name = "Dreamine.MVVM.Generators", Category = "MVVM", SortOrder = 10, Status = "stable", IsVisible = true,
            Tags = ["mvvm","generator","source-generator"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Generators",
            Description   = "Source Generator — [AutoRegister], [AutoNotify] 어트리뷰트 기반 코드 자동 생성.",
            DescriptionEn = "Source Generator for [AutoRegister] and [AutoNotify] attribute-driven code generation."
        },
        new() {
            Id = "mvvm-attributes", Name = "Dreamine.MVVM.Attributes", Category = "MVVM", SortOrder = 11, Status = "stable", IsVisible = true,
            Tags = ["mvvm","attribute"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Attributes",
            Description   = "Generator·Framework에서 사용하는 어트리뷰트 정의 어셈블리.",
            DescriptionEn = "Attribute definitions used by source generators and the Dreamine framework."
        },
        new() {
            Id = "mvvm-wpf", Name = "Dreamine.MVVM.Wpf", Category = "WPF", SortOrder = 12, Status = "stable", IsVisible = true,
            Tags = ["wpf","mvvm"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.MVVM.Wpf",
            Description   = "WPF 진입점 통합 패키지 — App 부트스트랩, Window 생명주기 관리.",
            DescriptionEn = "WPF entry-point integration package — App bootstrap and Window lifecycle management."
        },
        new() {
            Id = "hybrid", Name = "Dreamine.Hybrid", Category = "Hybrid", SortOrder = 20, Status = "stable", IsVisible = true,
            Tags = ["hybrid","blazor"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Hybrid",
            Description   = "Blazor Server를 Kestrel로 임베드하는 크로스플랫폼 하이브리드 호스트 코어.",
            DescriptionEn = "Cross-platform hybrid host core that embeds a Blazor Server via Kestrel."
        },
        new() {
            Id = "hybrid-wpf", Name = "Dreamine.Hybrid.Wpf", Category = "Hybrid", SortOrder = 21, Status = "stable", IsVisible = true,
            Tags = ["hybrid","wpf","blazor"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Hybrid.Wpf",
            Description   = "WPF + 임베디드 Blazor Server 하이브리드 패턴. WebView2 통합 포함.",
            DescriptionEn = "WPF + embedded Blazor Server hybrid pattern with full WebView2 integration."
        },
        new() {
            Id = "ui-abstractions", Name = "Dreamine.UI.Abstractions", Category = "UI", SortOrder = 30, Status = "stable", IsVisible = true,
            Tags = ["ui","abstractions"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Abstractions",
            Description   = "플랫폼 공통 UI 인터페이스 — IPopupService, IThemeService 등.",
            DescriptionEn = "Common UI interfaces across platforms — IPopupService, IThemeService, and more."
        },
        new() {
            Id = "ui-wpf", Name = "Dreamine.UI.Wpf", Category = "UI", SortOrder = 31, Status = "stable", IsVisible = true,
            Tags = ["ui","wpf"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Wpf",
            Description   = "WPF 전용 UI 통합 패키지 — 테마 적용, 팝업 서비스 연결.",
            DescriptionEn = "WPF UI integration package — theme application and popup service wiring."
        },
        new() {
            Id = "ui-wpf-controls", Name = "Dreamine.UI.Wpf.Controls", Category = "UI", SortOrder = 32, Status = "stable", IsVisible = true,
            Tags = ["ui","wpf","controls"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Wpf.Controls",
            Description   = "다크 테마 WPF 커스텀 컨트롤 — DreamineButton, DreamineCheckLed, DreamineExpander 등.",
            DescriptionEn = "Dark-theme WPF custom controls — DreamineButton, DreamineCheckLed, DreamineExpander, and more."
        },
        new() {
            Id = "ui-winforms", Name = "Dreamine.UI.WinForms", Category = "UI", SortOrder = 33, Status = "stable", IsVisible = true,
            Tags = ["ui","winforms","controls"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.WinForms",
            Description   = "WinForms 다크 테마 컨트롤 라이브러리 — WPF Controls와 API 패리티 유지.",
            DescriptionEn = "WinForms dark-theme control library maintaining API parity with WPF Controls."
        },
        new() {
            Id = "ui-blazor", Name = "Dreamine.UI.Blazor", Category = "UI", SortOrder = 34, Status = "stable", IsVisible = true,
            Tags = ["ui","blazor","components"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Blazor",
            Description   = "Blazor 다크 테마 컴포넌트 라이브러리 — DreamineDialogService, Popup 등.",
            DescriptionEn = "Blazor dark-theme component library — DreamineDialogService, Popup, and modal components."
        },
        new() {
            Id = "ui-wpf-equipment", Name = "Dreamine.UI.Wpf.Equipment", Category = "UI", SortOrder = 36, Status = "stable", IsVisible = true,
            Tags = ["ui","wpf","equipment","industrial"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Wpf.Equipment",
            Description   = "산업 설비 특화 WPF 컨트롤 — 계기판, 게이지, 상태 표시 등.",
            DescriptionEn = "Industrial equipment-specialized WPF controls — dashboards, gauges, and status displays."
        },
        new() {
            Id = "ui-wpf-themes", Name = "Dreamine.UI.Wpf.Themes", Category = "UI", SortOrder = 37, Status = "stable", IsVisible = true,
            Tags = ["ui","wpf","themes"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Wpf.Themes",
            Description   = "WPF 다크 테마 리소스 딕셔너리 — 색상 팔레트, 공통 스타일 정의.",
            DescriptionEn = "WPF dark-theme ResourceDictionary — color palette and shared control style definitions."
        },
        new() {
            Id = "ui-maui", Name = "Dreamine.UI.Maui", Category = "UI", SortOrder = 38, Status = "stable", IsVisible = true,
            Tags = ["ui","maui","controls"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.UI.Maui",
            Description   = ".NET MAUI 다크 테마 컨트롤 라이브러리 — Cross-platform API 패리티.",
            DescriptionEn = ".NET MAUI dark-theme control library with cross-platform API parity."
        },
        new() {
            Id = "identity", Name = "Dreamine.Identity", Category = "Identity", SortOrder = 39, Status = "stable", IsVisible = true,
            Tags = ["identity","oauth","auth","cookie"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Identity",
            Description   = "OAuth 통합 로그인 라이브러리 — Google/Naver/Kakao 소셜 로그인, 로컬 이메일 로그인, 공용 쿠키/DataProtection 키 공유로 서비스 간 세션 공유.",
            DescriptionEn = "OAuth-integrated login library — Google/Naver/Kakao social login, local email login, shared cookie and DataProtection keys for cross-service session sharing."
        },
        new() {
            Id = "logging", Name = "Dreamine.Logging", Category = "Infrastructure", SortOrder = 40, Status = "stable", IsVisible = true,
            Tags = ["logging"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Logging",
            Description   = "구조화된 로깅 코어 — ILogSink, LogEntry, 파일/메모리 싱크 포함.",
            DescriptionEn = "Structured logging core — ILogSink, LogEntry, file and memory sink support."
        },
        new() {
            Id = "logging-wpf", Name = "Dreamine.Logging.Wpf", Category = "Infrastructure", SortOrder = 41, Status = "stable", IsVisible = true,
            Tags = ["logging","wpf"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Logging.Wpf",
            Description   = "WPF 전용 로그 싱크 — UI 스레드 안전 로그 바인딩.",
            DescriptionEn = "WPF-specific log sink — UI thread-safe log binding for real-time display."
        },
        new() {
            Id = "threading", Name = "Dreamine.Threading", Category = "Infrastructure", SortOrder = 50, Status = "stable", IsVisible = true,
            Tags = ["threading","async"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Threading",
            Description   = "비동기 작업 헬퍼 — AsyncQueue, PeriodicWorker, CancellationScope.",
            DescriptionEn = "Async task helpers — AsyncQueue, PeriodicWorker, and CancellationScope."
        },
        new() {
            Id = "threading-windows", Name = "Dreamine.Threading.Windows", Category = "Infrastructure", SortOrder = 51, Status = "stable", IsVisible = true,
            Tags = ["threading","windows"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Threading.Windows",
            Description   = "Windows 전용 스레딩 확장 — Dispatcher 통합, Windows 이벤트 핸들링.",
            DescriptionEn = "Windows-specific threading extensions — Dispatcher integration and Windows event handling."
        },
        new() {
            Id = "threading-wpf", Name = "Dreamine.Threading.Wpf", Category = "Infrastructure", SortOrder = 52, Status = "stable", IsVisible = true,
            Tags = ["threading","wpf"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Threading.Wpf",
            Description   = "WPF Dispatcher 기반 스레딩 헬퍼 — UI 스레드 안전 작업 큐.",
            DescriptionEn = "WPF Dispatcher-based threading helpers — UI thread-safe task queue and scheduling."
        },
        new() {
            Id = "database-abstractions", Name = "Dreamine.Database.Abstractions", Category = "Database", SortOrder = 60, Status = "stable", IsVisible = true,
            Tags = ["database","abstractions"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Database.Abstractions",
            Description   = "데이터베이스 공통 추상화 — IDbConnectionFactory, ITransaction 인터페이스.",
            DescriptionEn = "Common database abstractions — IDbConnectionFactory, ITransaction, and IRepository interfaces."
        },
        new() {
            Id = "database-core", Name = "Dreamine.Database.Core", Category = "Database", SortOrder = 61, Status = "stable", IsVisible = true,
            Tags = ["database","dapper"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Database.Core",
            Description   = "데이터베이스 코어 구현 — IRepository, IUnitOfWork, Dapper 통합.",
            DescriptionEn = "Core database implementation — IRepository, IUnitOfWork, and Dapper integration."
        },
        new() {
            Id = "database-sqlite", Name = "Dreamine.Database.Sqlite", Category = "Database", SortOrder = 62, Status = "stable", IsVisible = true,
            Tags = ["database","sqlite"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Database.Sqlite",
            Description   = "SQLite 전용 드라이버 및 마이그레이션 지원.",
            DescriptionEn = "SQLite-specific driver with schema migration support."
        },
        new() {
            Id = "database-mysql", Name = "Dreamine.Database.MySql", Category = "Database", SortOrder = 63, Status = "stable", IsVisible = true,
            Tags = ["database","mysql"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Database.MySql",
            Description   = "MySQL / MariaDB 전용 드라이버 및 연결 관리.",
            DescriptionEn = "MySQL / MariaDB driver with connection management and query helpers."
        },
        new() {
            Id = "database-sqlserver", Name = "Dreamine.Database.SqlServer", Category = "Database", SortOrder = 64, Status = "stable", IsVisible = true,
            Tags = ["database","sqlserver"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Database.SqlServer",
            Description   = "SQL Server 전용 드라이버 및 벌크 삽입 지원.",
            DescriptionEn = "SQL Server driver with bulk insert and stored procedure support."
        },
        new() {
            Id = "database-oracle", Name = "Dreamine.Database.Oracle", Category = "Database", SortOrder = 65, Status = "stable", IsVisible = true,
            Tags = ["database","oracle"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Database.Oracle",
            Description   = "Oracle DB 전용 드라이버 및 시퀀스 지원.",
            DescriptionEn = "Oracle DB driver with sequence, stored procedure, and CLOB support."
        },
        new() {
            Id = "comm-abstractions", Name = "Dreamine.Communication.Abstractions", Category = "Communication", SortOrder = 70, Status = "stable", IsVisible = true,
            Tags = ["communication","abstractions"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.Abstractions",
            Description   = "통신 공통 추상화 — IClientProxy, IServerHub, IMessageSerializer 인터페이스.",
            DescriptionEn = "Common communication abstractions — IClientProxy, IServerHub, and IMessageSerializer interfaces."
        },
        new() {
            Id = "comm-core", Name = "Dreamine.Communication.Core", Category = "Communication", SortOrder = 71, Status = "stable", IsVisible = true,
            Tags = ["communication","core"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.Core",
            Description   = "통신 코어 구현 — 연결 관리, 재연결, 메시지 라우팅.",
            DescriptionEn = "Communication core implementation — connection management, auto-reconnect, and message routing."
        },
        new() {
            Id = "comm-sockets", Name = "Dreamine.Communication.Sockets", Category = "Communication", SortOrder = 72, Status = "stable", IsVisible = true,
            Tags = ["communication","sockets","tcp","udp"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.Sockets",
            Description   = "TCP/UDP 소켓 통신 구현 — 고성능 비동기 소켓 클라이언트/서버.",
            DescriptionEn = "TCP/UDP socket communication — high-performance async socket client and server."
        },
        new() {
            Id = "comm-serial", Name = "Dreamine.Communication.Serial", Category = "Communication", SortOrder = 73, Status = "stable", IsVisible = true,
            Tags = ["communication","serial","rs232","rs485"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.Serial",
            Description   = "RS-232/485 시리얼 통신 — 자동 재연결, 프레임 파싱 지원.",
            DescriptionEn = "RS-232/485 serial communication with auto-reconnect and frame parsing support."
        },
        new() {
            Id = "comm-rabbitmq", Name = "Dreamine.Communication.RabbitMQ", Category = "Communication", SortOrder = 74, Status = "stable", IsVisible = true,
            Tags = ["communication","rabbitmq","messaging"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.RabbitMQ",
            Description   = "RabbitMQ 메시지 브로커 통합 — Exchange/Queue/Routing 추상화.",
            DescriptionEn = "RabbitMQ message broker integration — Exchange, Queue, and Routing key abstraction."
        },
        new() {
            Id = "comm-wpf", Name = "Dreamine.Communication.Wpf", Category = "Communication", SortOrder = 75, Status = "stable", IsVisible = true,
            Tags = ["communication","wpf"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.Wpf",
            Description   = "WPF 통신 연동 패키지 — UI 바인딩 친화적 통신 상태 관리.",
            DescriptionEn = "WPF communication binding package — UI-friendly connection state and status management."
        },
        new() {
            Id = "comm-fullkit", Name = "Dreamine.Communication.FullKit", Category = "Communication", SortOrder = 76, Status = "stable", IsVisible = true,
            Tags = ["communication","fullkit","bundle"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.Communication.FullKit",
            Description   = "통신 라이브러리 전체 번들 — Sockets, Serial, RabbitMQ 일괄 참조.",
            DescriptionEn = "Full communication bundle — Sockets, Serial, and RabbitMQ in a single reference."
        },
        new() {
            Id = "io-abstractions", Name = "Dreamine.IO.Abstractions", Category = "IO", SortOrder = 80, Status = "stable", IsVisible = true,
            Tags = ["io","abstractions"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.IO.Abstractions",
            Description   = "I/O 디바이스 공통 추상화 — IIoDevice, IIoChannel 인터페이스.",
            DescriptionEn = "Common I/O device abstractions — IIoDevice and IIoChannel interfaces."
        },
        new() {
            Id = "io-fastech-ethernet", Name = "Dreamine.IO.Fastech.Ethernet", Category = "IO", SortOrder = 81, Status = "stable", IsVisible = true,
            Tags = ["io","fastech","ethernet","motion"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.IO.Fastech.Ethernet",
            Description   = "Fastech EtherNet/IP 모션 컨트롤러 드라이버.",
            DescriptionEn = "Fastech EtherNet/IP motion controller driver with DI/DO and analog I/O support."
        },
        new() {
            Id = "plc-abstractions", Name = "Dreamine.PLC.Abstractions", Category = "PLC", SortOrder = 90, Status = "stable", IsVisible = true,
            Tags = ["plc","abstractions"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Abstractions",
            Description   = "PLC 공통 추상화 — IPlcDriver, IPlcTag, IPlcMonitor 인터페이스.",
            DescriptionEn = "Common PLC abstractions — IPlcDriver, IPlcTag, and IPlcMonitor interfaces."
        },
        new() {
            Id = "plc-core", Name = "Dreamine.PLC.Core", Category = "PLC", SortOrder = 91, Status = "stable", IsVisible = true,
            Tags = ["plc","core"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Core",
            Description   = "PLC 코어 구현 — 태그 캐싱, 폴링 스케줄러, 연결 관리.",
            DescriptionEn = "PLC core implementation — tag caching, polling scheduler, and connection management."
        },
        new() {
            Id = "plc-mitsubishi-mc", Name = "Dreamine.PLC.Mitsubishi.MC", Category = "PLC", SortOrder = 92, Status = "stable", IsVisible = true,
            Tags = ["plc","mitsubishi","mc","melsec"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Mitsubishi.MC",
            Description   = "미쓰비시 MELSEC MC 프로토콜 드라이버 (3E/4E 프레임).",
            DescriptionEn = "Mitsubishi MELSEC MC protocol driver supporting 3E and 4E frame formats."
        },
        new() {
            Id = "plc-mitsubishi-mx", Name = "Dreamine.PLC.Mitsubishi.MxComponent", Category = "PLC", SortOrder = 93, Status = "stable", IsVisible = true,
            Tags = ["plc","mitsubishi","mxcomponent"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Mitsubishi.MxComponent",
            Description   = "미쓰비시 MX Component COM 기반 드라이버.",
            DescriptionEn = "Mitsubishi MX Component COM-based driver for iQ-R and iQ-F series PLCs."
        },
        new() {
            Id = "plc-omron-cx", Name = "Dreamine.PLC.Omron.CxComponent", Category = "PLC", SortOrder = 94, Status = "stable", IsVisible = true,
            Tags = ["plc","omron","cxone"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Omron.CxComponent",
            Description   = "옴론 CX-One 기반 드라이버 — FINS/UDP 프로토콜.",
            DescriptionEn = "Omron CX-One based driver using FINS/UDP protocol for CJ and CS series PLCs."
        },
        new() {
            Id = "plc-omron-fins", Name = "Dreamine.PLC.Omron.Fins", Category = "PLC", SortOrder = 95, Status = "stable", IsVisible = true,
            Tags = ["plc","omron","fins","udp","tcp"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Omron.Fins",
            Description   = "옴론 FINS 프로토콜 직접 구현 — UDP/TCP 이중 지원.",
            DescriptionEn = "Direct Omron FINS protocol implementation with UDP and TCP dual transport support."
        },
        new() {
            Id = "plc-wpf", Name = "Dreamine.PLC.Wpf", Category = "PLC", SortOrder = 96, Status = "stable", IsVisible = true,
            Tags = ["plc","wpf","monitoring"],
            RepoUrl       = "https://github.com/CodeMaru-Dreamine/Dreamine.PLC.Wpf",
            Description   = "WPF PLC 모니터링 컨트롤 — 실시간 태그 바인딩, 알람 표시.",
            DescriptionEn = "WPF PLC monitoring controls — real-time tag binding and alarm display panel."
        },
    ];
}
