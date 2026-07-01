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
        new() { Id = "mvvm-behaviors",      Name = "Dreamine.MVVM.Behaviors",      Category = "MVVM",           Description = "플랫폼 독립 Behavior 베이스. EventToCommand 등 바인딩 헬퍼 포함.", Status = "stable", SortOrder = 4,  IsVisible = true, Tags = ["mvvm","behavior"] },
        new() { Id = "mvvm-behaviors-core", Name = "Dreamine.MVVM.Behaviors.Core", Category = "MVVM",           Description = "플랫폼 공통 Behavior 코어 인터페이스 및 기반 클래스.", Status = "stable", SortOrder = 5,  IsVisible = true, Tags = ["mvvm","behavior","core"] },
        new() { Id = "mvvm-behaviors-wpf",  Name = "Dreamine.MVVM.Behaviors.Wpf",  Category = "WPF",            Description = "WPF 전용 Behavior 확장 — 드래그·드롭, 포커스, 키 트리거 등.", Status = "stable", SortOrder = 6,  IsVisible = true, Tags = ["wpf","behavior"] },
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
        new() { Id = "ui-wpf-equipment",   Name = "Dreamine.UI.Wpf.Equipment",   Category = "UI",             Description = "산업 설비 특화 WPF 컨트롤 — 계기판, 게이지, 상태 표시 등.", Status = "stable", SortOrder = 36, IsVisible = true, Tags = ["ui","wpf","equipment","industrial"] },
        new() { Id = "ui-wpf-themes",      Name = "Dreamine.UI.Wpf.Themes",      Category = "UI",             Description = "WPF 다크 테마 리소스 딕셔너리 — 색상 팔레트, 공통 스타일 정의.", Status = "stable", SortOrder = 37, IsVisible = true, Tags = ["ui","wpf","themes"] },
        new() { Id = "ui-maui",            Name = "Dreamine.UI.Maui",            Category = "UI",             Description = ".NET MAUI 다크 테마 컨트롤 라이브러리 — Cross-platform API 패리티.", Status = "stable", SortOrder = 38, IsVisible = true, Tags = ["ui","maui","controls"] },
        new() { Id = "logging",            Name = "Dreamine.Logging",            Category = "Infrastructure",  Description = "구조화된 로깅 코어 — ILogSink, LogEntry, 파일/메모리 싱크 포함.", Status = "stable", SortOrder = 40, IsVisible = true, Tags = ["logging"] },
        new() { Id = "logging-wpf",        Name = "Dreamine.Logging.Wpf",        Category = "Infrastructure",  Description = "WPF 전용 로그 싱크 — UI 스레드 안전 로그 바인딩.", Status = "stable", SortOrder = 41, IsVisible = true, Tags = ["logging","wpf"] },
        new() { Id = "threading",          Name = "Dreamine.Threading",          Category = "Infrastructure",  Description = "비동기 작업 헬퍼 — AsyncQueue, PeriodicWorker, CancellationScope.", Status = "stable", SortOrder = 50, IsVisible = true, Tags = ["threading","async"] },
        new() { Id = "threading-windows",  Name = "Dreamine.Threading.Windows",  Category = "Infrastructure",  Description = "Windows 전용 스레딩 확장 — Dispatcher 통합, Windows 이벤트 핸들링.", Status = "stable", SortOrder = 51, IsVisible = true, Tags = ["threading","windows"] },
        new() { Id = "threading-wpf",      Name = "Dreamine.Threading.Wpf",      Category = "Infrastructure",  Description = "WPF Dispatcher 기반 스레딩 헬퍼 — UI 스레드 안전 작업 큐.", Status = "stable", SortOrder = 52, IsVisible = true, Tags = ["threading","wpf"] },
        new() { Id = "database-abstractions", Name = "Dreamine.Database.Abstractions", Category = "Database",  Description = "데이터베이스 공통 추상화 — IDbConnectionFactory, ITransaction 인터페이스.", Status = "stable", SortOrder = 60, IsVisible = true, Tags = ["database","abstractions"] },
        new() { Id = "database-core",      Name = "Dreamine.Database.Core",      Category = "Database",       Description = "데이터베이스 코어 구현 — IRepository, IUnitOfWork, Dapper 통합.", Status = "stable", SortOrder = 61, IsVisible = true, Tags = ["database","dapper"] },
        new() { Id = "database-sqlite",    Name = "Dreamine.Database.Sqlite",    Category = "Database",       Description = "SQLite 전용 드라이버 및 마이그레이션 지원.", Status = "stable", SortOrder = 62, IsVisible = true, Tags = ["database","sqlite"] },
        new() { Id = "database-mysql",     Name = "Dreamine.Database.MySql",     Category = "Database",       Description = "MySQL / MariaDB 전용 드라이버 및 연결 관리.", Status = "stable", SortOrder = 63, IsVisible = true, Tags = ["database","mysql"] },
        new() { Id = "database-sqlserver", Name = "Dreamine.Database.SqlServer", Category = "Database",       Description = "SQL Server 전용 드라이버 및 벌크 삽입 지원.", Status = "stable", SortOrder = 64, IsVisible = true, Tags = ["database","sqlserver"] },
        new() { Id = "database-oracle",    Name = "Dreamine.Database.Oracle",    Category = "Database",       Description = "Oracle DB 전용 드라이버 및 시퀀스 지원.", Status = "stable", SortOrder = 65, IsVisible = true, Tags = ["database","oracle"] },
        new() { Id = "comm-abstractions",  Name = "Dreamine.Communication.Abstractions", Category = "Communication", Description = "통신 공통 추상화 — IClientProxy, IServerHub, IMessageSerializer 인터페이스.", Status = "stable", SortOrder = 70, IsVisible = true, Tags = ["communication","abstractions"] },
        new() { Id = "comm-core",          Name = "Dreamine.Communication.Core", Category = "Communication",  Description = "통신 코어 구현 — 연결 관리, 재연결, 메시지 라우팅.", Status = "stable", SortOrder = 71, IsVisible = true, Tags = ["communication","core"] },
        new() { Id = "comm-sockets",       Name = "Dreamine.Communication.Sockets", Category = "Communication", Description = "TCP/UDP 소켓 통신 구현 — 고성능 비동기 소켓 클라이언트/서버.", Status = "stable", SortOrder = 72, IsVisible = true, Tags = ["communication","sockets","tcp","udp"] },
        new() { Id = "comm-serial",        Name = "Dreamine.Communication.Serial", Category = "Communication", Description = "RS-232/485 시리얼 통신 — 자동 재연결, 프레임 파싱 지원.", Status = "stable", SortOrder = 73, IsVisible = true, Tags = ["communication","serial","rs232","rs485"] },
        new() { Id = "comm-rabbitmq",      Name = "Dreamine.Communication.RabbitMQ", Category = "Communication", Description = "RabbitMQ 메시지 브로커 통합 — Exchange/Queue/Routing 추상화.", Status = "stable", SortOrder = 74, IsVisible = true, Tags = ["communication","rabbitmq","messaging"] },
        new() { Id = "comm-wpf",           Name = "Dreamine.Communication.Wpf",  Category = "Communication",  Description = "WPF 통신 연동 패키지 — UI 바인딩 친화적 통신 상태 관리.", Status = "stable", SortOrder = 75, IsVisible = true, Tags = ["communication","wpf"] },
        new() { Id = "comm-fullkit",       Name = "Dreamine.Communication.FullKit", Category = "Communication", Description = "통신 라이브러리 전체 번들 — Sockets, Serial, RabbitMQ 일괄 참조.", Status = "stable", SortOrder = 76, IsVisible = true, Tags = ["communication","fullkit","bundle"] },
        new() { Id = "io-abstractions",    Name = "Dreamine.IO.Abstractions",    Category = "IO",             Description = "I/O 디바이스 공통 추상화 — IIoDevice, IIoChannel 인터페이스.", Status = "stable", SortOrder = 80, IsVisible = true, Tags = ["io","abstractions"] },
        new() { Id = "io-fastech-ethernet",Name = "Dreamine.IO.Fastech.Ethernet", Category = "IO",            Description = "Fastech EtherNet/IP 모션 컨트롤러 드라이버.", Status = "stable", SortOrder = 81, IsVisible = true, Tags = ["io","fastech","ethernet","motion"] },
        new() { Id = "plc-abstractions",   Name = "Dreamine.PLC.Abstractions",   Category = "PLC",            Description = "PLC 공통 추상화 — IPlcDriver, IPlcTag, IPlcMonitor 인터페이스.", Status = "stable", SortOrder = 90, IsVisible = true, Tags = ["plc","abstractions"] },
        new() { Id = "plc-core",           Name = "Dreamine.PLC.Core",           Category = "PLC",            Description = "PLC 코어 구현 — 태그 캐싱, 폴링 스케줄러, 연결 관리.", Status = "stable", SortOrder = 91, IsVisible = true, Tags = ["plc","core"] },
        new() { Id = "plc-mitsubishi-mc",  Name = "Dreamine.PLC.Mitsubishi.MC",  Category = "PLC",            Description = "미쓰비시 MELSEC MC 프로토콜 드라이버 (3E/4E 프레임).", Status = "stable", SortOrder = 92, IsVisible = true, Tags = ["plc","mitsubishi","mc","melsec"] },
        new() { Id = "plc-mitsubishi-mx",  Name = "Dreamine.PLC.Mitsubishi.MxComponent", Category = "PLC",   Description = "미쓰비시 MX Component COM 기반 드라이버.", Status = "stable", SortOrder = 93, IsVisible = true, Tags = ["plc","mitsubishi","mxcomponent"] },
        new() { Id = "plc-omron-cx",       Name = "Dreamine.PLC.Omron.CxComponent", Category = "PLC",        Description = "옴론 CX-One 기반 드라이버 — FINS/UDP 프로토콜.", Status = "stable", SortOrder = 94, IsVisible = true, Tags = ["plc","omron","cxone"] },
        new() { Id = "plc-omron-fins",     Name = "Dreamine.PLC.Omron.Fins",     Category = "PLC",            Description = "옴론 FINS 프로토콜 직접 구현 — UDP/TCP 이중 지원.", Status = "stable", SortOrder = 95, IsVisible = true, Tags = ["plc","omron","fins","udp","tcp"] },
        new() { Id = "plc-wpf",            Name = "Dreamine.PLC.Wpf",            Category = "PLC",            Description = "WPF PLC 모니터링 컨트롤 — 실시간 태그 바인딩, 알람 표시.", Status = "stable", SortOrder = 96, IsVisible = true, Tags = ["plc","wpf","monitoring"] },
    ];
}
