using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using FamiliesAutoWriter.Models;
using FamiliesAutoWriter.Services;
using Microsoft.Win32;

namespace FamiliesAutoWriter.ViewModels;

/// <summary>
/// \if KO
/// <para>세션 파싱 모드 — 탭마다 독립 설정</para>
/// \endif
/// \if EN
/// <para>Encapsulates session mode functionality and related state.</para>
/// \endif
/// </summary>
public enum SessionMode
{
    /// <summary>
    /// \if KO
    /// <para>여행 콘텐츠 작성 모드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Uses travel-content writing mode.</para>
    /// \endif
    /// </summary>
    Travel,

    /// <summary>
    /// \if KO
    /// <para>요리 콘텐츠 작성 모드입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Uses cooking-content writing mode.</para>
    /// \endif
    /// </summary>
    Cooking
}

/// <summary>
/// \if KO
/// <para>실행 간격 옵션 (콤보박스 표시용)</para>
/// \endif
/// \if EN
/// <para>Encapsulates interval option functionality and related state.</para>
/// \endif
/// </summary>
public sealed record IntervalOption(int Minutes, string Label)
{
    /// <summary>
    /// \if KO
    /// <para>To String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to string operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>To String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to string operation.</para>
    /// \endif
    /// </returns>
    public override string ToString() => Label;
}

/// <summary>
/// \if KO
/// <para>AI 대기 시간 옵션 (콤보박스 표시용)</para>
/// \endif
/// \if EN
/// <para>Encapsulates wait option functionality and related state.</para>
/// \endif
/// </summary>
public sealed record WaitOption(int Seconds, string Label)
{
    /// <summary>
    /// \if KO
    /// <para>To String 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the to string operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>To String 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the to string operation.</para>
    /// \endif
    /// </returns>
    public override string ToString() => Label;
}

/// <summary>
/// \if KO
/// <para>탭 하나에 대응하는 독립 세션 — 슬러그/앨범/프롬프트/루프 상태를 각자 보유</para>
/// \endif
/// \if EN
/// <para>Encapsulates writer session functionality and related state.</para>
/// \endif
/// </summary>
public sealed partial class WriterSession : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>writer 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the writer value.</para>
    /// \endif
    /// </summary>
    private readonly PostWriterService    _writer  = new();
    /// <summary>
    /// \if KO
    /// <para>history 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the history value.</para>
    /// \endif
    /// </summary>
    private readonly PromptHistoryService _history = new();

    // ── 탭 모드 (Travel / Cooking) ─────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Mode 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mode value.</para>
    /// \endif
    /// </summary>
    public SessionMode Mode { get; set; } = SessionMode.Travel;

    // ── 브라우저 위임 (코드비하인드에서 주입) ───────────────────
    /// <summary>
    /// \if KO
    /// <para>Execute Script Async 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the execute script async value.</para>
    /// \endif
    /// </summary>
    public Func<string, Task<string?>>? ExecuteScriptAsync { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Navigate Tab 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the navigate tab value.</para>
    /// \endif
    /// </summary>
    public Action<string>?              NavigateTab        { get; set; }

    // ── 루프 태스크 ────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>ui Dispatcher 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the ui dispatcher value.</para>
    /// \endif
    /// </summary>
    private readonly System.Windows.Threading.Dispatcher _uiDispatcher;
    /// <summary>
    /// \if KO
    /// <para>next Loop At 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the next loop at value.</para>
    /// \endif
    /// </summary>
    private DateTime _nextLoopAt = DateTime.MaxValue;
    /// <summary>
    /// \if KO
    /// <para>loop Cts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the loop cts value.</para>
    /// \endif
    /// </summary>
    private CancellationTokenSource? _loopCts;
    /// <summary>
    /// \if KO
    /// <para>loop Task 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the loop task value.</para>
    /// \endif
    /// </summary>
    private Task? _loopTask;
    /// <summary>
    /// \if KO
    /// <para>cycle Count 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cycle count value.</para>
    /// \endif
    /// </summary>
    private int _cycleCount;
    /// <summary>
    /// \if KO
    /// <para>used Image Urls 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the used image urls value.</para>
    /// \endif
    /// </summary>
    private readonly HashSet<string> _usedImageUrls = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// \if KO
    /// <para>album Index 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the album index value.</para>
    /// \endif
    /// </summary>
    private int _albumIndex;
    /// <summary>
    /// \if KO
    /// <para>Extraction Retry Delay Seconds 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the extraction retry delay seconds value.</para>
    /// \endif
    /// </summary>
    private const int ExtractionRetryDelaySeconds = 30;
    /// <summary>
    /// \if KO
    /// <para>Extraction Retry Max Attempts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the extraction retry max attempts value.</para>
    /// \endif
    /// </summary>
    private const int ExtractionRetryMaxAttempts = 10;

    // ── 컬렉션 ─────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Slugs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the slugs value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<string>    Slugs  { get; } = [];
    /// <summary>
    /// \if KO
    /// <para>Albums 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the albums value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<AlbumInfo> Albums { get; } = [];

    // ── 콤보박스 상수 옵션 ─────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Interval Options 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the interval options value.</para>
    /// \endif
    /// </summary>
    public static IntervalOption[] IntervalOptions { get; } =
    [
        new(5,   "5분"),    new(10, "10분"), new(15, "15분"),
        new(20,  "20분"),   new(30, "30분"), new(60, "1시간"),
        new(120, "2시간"),  new(180,"3시간"), new(360,"6시간"),
    ];
    /// <summary>
    /// \if KO
    /// <para>Wait Options 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the wait options value.</para>
    /// \endif
    /// </summary>
    public static WaitOption[] WaitOptions { get; } =
    [
        new(15,  "15초"),  new(20, "20초"),  new(30,  "30초"),
        new(45,  "45초"),  new(60, "1분"),   new(90,  "1분 30초"),
        new(120, "2분"),   new(150,"2분 30초"), new(180,"3분"),
        new(210, "3분 30초"),
    ];
    /// <summary>
    /// \if KO
    /// <para>New Chat Options 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the new chat options value.</para>
    /// \endif
    /// </summary>
    public static int[] NewChatOptions { get; } = [0, 3, 5, 10, 20, 30];

    // ── [DreamineProperty] — 단순 속성 ──────────────────────────
    /// <summary>
    /// \if KO
    /// <para>album Rotation Enabled 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the album rotation enabled value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private bool          _albumRotationEnabled;
    /// <summary>
    /// \if KO
    /// <para>new Chat Every N 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the new chat every n value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private int           _newChatEveryN = 5;
    /// <summary>
    /// \if KO
    /// <para>loop Status 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the loop status value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string        _loopStatus    = "⏸ 루프 꺼짐";
    /// <summary>
    /// \if KO
    /// <para>loop Countdown 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the loop countdown value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string        _loopCountdown = "";
    /// <summary>
    /// \if KO
    /// <para>media Position 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the media position value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private MediaPosition _mediaPosition = MediaPosition.Bottom;
    /// <summary>
    /// \if KO
    /// <para>prompt Text 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the prompt text value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string        _promptText    = BuildTravelPrompt([]);
    /// <summary>
    /// \if KO
    /// <para>prompt Status 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the prompt status value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string        _promptStatus  = "";
    /// <summary>
    /// \if KO
    /// <para>extract Status 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the extract status value.</para>
    /// \endif
    /// </summary>
    [DreamineProperty] private string        _extractStatus = "";

    // ── 사이드이펙트 있는 속성 — 수동 구현 ──────────────────────
    /// <summary>
    /// \if KO
    /// <para>app Data Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the app data root value.</para>
    /// \endif
    /// </summary>
    private string _appDataRoot = DetectDefaultRoot();
    /// <summary>
    /// \if KO
    /// <para>App Data Root 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the app data root value.</para>
    /// \endif
    /// </summary>
    public string AppDataRoot
    {
        get => _appDataRoot;
        set { if (SetProperty(ref _appDataRoot, value)) { _writer.AppDataRoot = value; RefreshSlugs(); } }
    }

    /// <summary>
    /// \if KO
    /// <para>selected Slug 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected slug value.</para>
    /// \endif
    /// </summary>
    private string _selectedSlug = "";
    /// <summary>
    /// \if KO
    /// <para>Selected Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected slug value.</para>
    /// \endif
    /// </summary>
    public string SelectedSlug
    {
        get => _selectedSlug;
        set { if (SetProperty(ref _selectedSlug, value)) RefreshAlbums(); }
    }

    /// <summary>
    /// \if KO
    /// <para>selected Album 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected album value.</para>
    /// \endif
    /// </summary>
    private AlbumInfo? _selectedAlbum;
    /// <summary>
    /// \if KO
    /// <para>Selected Album 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected album value.</para>
    /// \endif
    /// </summary>
    public AlbumInfo? SelectedAlbum
    {
        get => _selectedAlbum;
        set => SetProperty(ref _selectedAlbum, value);
    }

    /// <summary>
    /// \if KO
    /// <para>loop Enabled 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the loop enabled value.</para>
    /// \endif
    /// </summary>
    private bool _loopEnabled;
    /// <summary>
    /// \if KO
    /// <para>Loop Enabled 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the loop enabled value.</para>
    /// \endif
    /// </summary>
    public bool LoopEnabled
    {
        get => _loopEnabled;
        set
        {
            if (SetProperty(ref _loopEnabled, value))
            {
                OnPropertyChanged(nameof(LoopLabel));
                if (value) StartLoop(); else StopLoop();
            }
        }
    }
    /// <summary>
    /// \if KO
    /// <para>Loop Label 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the loop label value.</para>
    /// \endif
    /// </summary>
    public string LoopLabel => LoopEnabled ? "▶ 실행 중" : "⏸ 루프 꺼짐";

    /// <summary>
    /// \if KO
    /// <para>selected Interval 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected interval value.</para>
    /// \endif
    /// </summary>
    private IntervalOption _selectedInterval = null!;
    /// <summary>
    /// \if KO
    /// <para>Selected Interval 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected interval value.</para>
    /// \endif
    /// </summary>
    public IntervalOption SelectedInterval
    {
        get => _selectedInterval;
        set { if (SetProperty(ref _selectedInterval, value) && LoopEnabled) StartLoop(); }
    }

    /// <summary>
    /// \if KO
    /// <para>ai Wait Option 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the ai wait option value.</para>
    /// \endif
    /// </summary>
    private WaitOption _aiWaitOption = null!;
    /// <summary>
    /// \if KO
    /// <para>Ai Wait Option 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the ai wait option value.</para>
    /// \endif
    /// </summary>
    public WaitOption AiWaitOption
    {
        get => _aiWaitOption;
        set => SetProperty(ref _aiWaitOption, value);
    }

    // ── 생성자 ─────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="WriterSession"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="WriterSession"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="initialPrompt">
    /// \if KO
    /// <para>initial Prompt에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for initial prompt.</para>
    /// \endif
    /// </param>
    public WriterSession(string? initialPrompt = null)
    {
        _uiDispatcher     = System.Windows.Application.Current.Dispatcher;
        _selectedInterval = IntervalOptions[1]; // 10분
        _aiWaitOption     = WaitOptions[2];     // 30초
        if (initialPrompt != null) _promptText = initialPrompt;
        RefreshSlugs();
    }

    // ── 커맨드 ─────────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Refresh Slug List 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh slug list operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand] private void RefreshSlugList() { RefreshSlugs(); RefreshAlbums(); }
    /// <summary>
    /// \if KO
    /// <para>Run Now 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run now operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand] private void RunNow()          => _ = RunLoopCycleAsync(CancellationToken.None);
    /// <summary>
    /// \if KO
    /// <para>Send Prompt Now 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send prompt now operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand] private void SendPromptNow()   => _ = SendPromptNowAsync();
    /// <summary>
    /// \if KO
    /// <para>Extract And Save 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract and save operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand] private void ExtractAndSave()  => _ = ExtractAndSaveAsync();
    /// <summary>
    /// \if KO
    /// <para>Clear Prompt History 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear prompt history operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand] private void ClearPromptHistory() { _history.Clear(); PromptStatus = "🗑 초기화"; }
    /// <summary>
    /// \if KO
    /// <para>Browse App Data 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the browse app data operation.</para>
    /// \endif
    /// </summary>
    [DreamineCommand] private void BrowseAppData()
    {
        var dlg = new OpenFolderDialog { Title = "Families.Web App_Data 폴더 선택" };
        if (dlg.ShowDialog() == true) AppDataRoot = dlg.FolderName;
    }

    // ── 슬러그 / 앨범 ─────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Refresh Slugs 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh slugs operation.</para>
    /// \endif
    /// </summary>
    private void RefreshSlugs()
    {
        _writer.AppDataRoot = AppDataRoot;
        Slugs.Clear();
        foreach (var s in _writer.GetSlugs()) Slugs.Add(s);
        if (Slugs.Count > 0 && string.IsNullOrEmpty(SelectedSlug))
            SelectedSlug = Slugs[0];
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh Albums 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh albums operation.</para>
    /// \endif
    /// </summary>
    private void RefreshAlbums()
    {
        Albums.Clear();
        if (string.IsNullOrWhiteSpace(SelectedSlug)) return;
        foreach (var a in _writer.GetAlbums(SelectedSlug)) Albums.Add(a);
        _albumIndex   = 0;
        SelectedAlbum = Albums.Count > 0 ? Albums[0] : null;
        RefreshPromptWithExisting();
    }

    /// <summary>
    /// \if KO
    /// <para>Refresh Prompt With Existing 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh prompt with existing operation.</para>
    /// \endif
    /// </summary>
    private void RefreshPromptWithExisting()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) return;
        var existing = _writer.GetExistingTitles(SelectedSlug);
        PromptText = Mode == SessionMode.Cooking
            ? BuildCookingPrompt(existing, _writer.GetCookingTimelineState(SelectedSlug))
            : BuildTravelPrompt(existing);
    }

    // ── 루프 제어 ─────────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Interval Minutes 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the interval minutes value.</para>
    /// \endif
    /// </summary>
    private int IntervalMinutes => SelectedInterval?.Minutes ?? 10;

    /// <summary>
    /// \if KO
    /// <para>Start Loop 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start loop operation.</para>
    /// \endif
    /// </summary>
    private void StartLoop()
    {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _loopCts = new CancellationTokenSource();
        _nextLoopAt = DateTime.Now.AddMinutes(IntervalMinutes);
        var ct = _loopCts.Token;
        _loopTask = Task.Run(() => LoopWorkerAsync(ct));
        var nc = NewChatEveryN > 0 ? $" | 🔄{NewChatEveryN}개" : "";
        LoopStatus = $"✅ {SelectedInterval?.Label} | AI대기 {AiWaitOption?.Label}{nc}";
    }

    /// <summary>
    /// \if KO
    /// <para>Stop Loop 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop loop operation.</para>
    /// \endif
    /// </summary>
    private void StopLoop()
    {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _loopCts = null;
        _nextLoopAt = DateTime.MaxValue;
        LoopCountdown = "";
        LoopStatus = "⏸ 루프 꺼짐";
    }

    /// <summary>
    /// \if KO
    /// <para>Stop Automation 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop automation operation.</para>
    /// \endif
    /// </summary>
    public void StopAutomation()
    {
        if (LoopEnabled)
            LoopEnabled = false;
        else
            StopLoop();
    }

    // 백그라운드 루프 워커 — 각 탭이 독립적으로 돌아감
    /// <summary>
    /// \if KO
    /// <para>Loop Worker Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the loop worker async operation.</para>
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
    /// <para>Loop Worker Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the loop worker async operation.</para>
    /// \endif
    /// </returns>
    private async Task LoopWorkerAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 카운트다운
                try
                {
                    while (!ct.IsCancellationRequested && DateTime.Now < _nextLoopAt)
                    {
                        var r = _nextLoopAt - DateTime.Now;
                        if (r > TimeSpan.Zero)
                        {
                            var text = r.TotalHours >= 1
                                ? $"다음 {(int)r.TotalHours}:{r.Minutes:D2}:{r.Seconds:D2}"
                                : $"다음 {r.Minutes:D2}:{r.Seconds:D2}";
                            await _uiDispatcher.InvokeAsync(() => LoopCountdown = text);
                        }
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { break; }

                if (ct.IsCancellationRequested) break;

                try
                {
                    await RunLoopCycleAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    await _uiDispatcher.InvokeAsync(() => LoopStatus = $"❌ 루프 오류: {ex.Message}");
                    await Task.Delay(3000, ct).ConfigureAwait(false);
                }

                if (!ct.IsCancellationRequested)
                    _nextLoopAt = DateTime.Now.AddMinutes(IntervalMinutes);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await _uiDispatcher.InvokeAsync(() => LoopStatus = $"❌ 루프 중단: {ex.Message}");
        }
        finally
        {
            await _uiDispatcher.InvokeAsync(() => LoopCountdown = "");
        }
    }

    // ── 루프 사이클 ───────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Run Loop Cycle Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the run loop cycle async operation.</para>
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
    /// <para>Run Loop Cycle Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the run loop cycle async operation.</para>
    /// \endif
    /// </returns>
    private async Task RunLoopCycleAsync(CancellationToken ct)
    {
        // 상태 읽기는 UI 스레드에서
        var (slug, album, prompt, execFn) = await _uiDispatcher.InvokeAsync(() =>
            (SelectedSlug, SelectedAlbum, PromptText, ExecuteScriptAsync));

        if (string.IsNullOrWhiteSpace(slug))   { await SetStatus("❌ 슬러그를 선택하세요."); return; }
        if (string.IsNullOrWhiteSpace(prompt)) { await SetStatus("❌ 프롬프트를 입력하세요."); return; }
        if (execFn == null)                    { await SetStatus("❌ 브라우저 준비 안 됨"); return; }

        var albumName = album?.Name ?? "전체 타임라인";
        var filled = prompt.Replace("{앨범}", albumName).Replace("{album}", albumName);


        try
        {
            // 전송 전 현재 마지막 응답을 베이스라인으로 캡처 — 이전 응답 오탐 방지
            var baselineRaw = await await _uiDispatcher.InvokeAsync(() => execFn(ResponseExtractor.BuildExtractScript()));
            string baselineText = "";
            try
            {
                using var bd = System.Text.Json.JsonDocument.Parse(baselineRaw ?? "{}");
                baselineText = bd.RootElement.TryGetProperty("text", out var bt) ? bt.GetString() ?? "" : "";
            }
            catch { }

            await SetStatus($"📤 [{albumName}] 전송 중...");
            await await _uiDispatcher.InvokeAsync(() => execFn(PromptInjector.BuildInjectScript(filled)));

            // AI 시작 대기 (10초)
            await SetStatus($"⏳ [{albumName}] AI 시작 대기...");
            await Task.Delay(10_000, ct).ConfigureAwait(false);

            // 완료 판정:
            //   1순위 — ===VIDEO=== 발견 시 즉시 완료 (completed 플래그)
            //   2순위 — 종료 마커가 있는 텍스트 안정화: 15초 간격으로 2회 연속 동일하면 완료
            string? raw = null;
            string prevText = "";
            int stableCount = 0;
            for (int retry = 0; retry <= 60; retry++)   // 최대 15분
            {
                if (ct.IsCancellationRequested) return;

                var extracted = await await _uiDispatcher.InvokeAsync(() => execFn(ResponseExtractor.BuildExtractScript()));
                string curText = "";
                bool completed = false;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(extracted ?? "{}");
                    curText = doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                    completed = doc.RootElement.TryGetProperty("completed", out var c) && c.GetBoolean();
                }
                catch { curText = extracted ?? ""; }

                // 이전 응답(베이스라인)과 동일하면 아직 새 응답이 아님 → 스킵
                bool isNewResponse = curText.Length > 20 && curText != baselineText;

                // 1순위: 종료 마커 확인 (새 응답일 때만)
                if (completed && isNewResponse)
                {
                    raw = extracted;
                    break;
                }

                // 2순위: 텍스트 안정화 (새 응답일 때만)
                var hasEndMarker = curText.Contains("===VIDEO===", StringComparison.OrdinalIgnoreCase);
                if (isNewResponse && hasEndMarker && curText == prevText)
                {
                    stableCount++;
                    if (stableCount >= 2)
                    {
                        raw = extracted;
                        break;
                    }
                }
                else
                {
                    stableCount = 0;
                }
                prevText = curText;

                var elapsed = 8 + retry * 15;
                await SetStatus($"⏳ [{albumName}] AI 생성 중... ({elapsed}s)");
                await Task.Delay(15_000, ct).ConfigureAwait(false);
            }

            if (raw == null)
            {
                await SetStatus("⏱ AI 응답 대기 시간 초과 (응답 없음)");
                return;
            }

            var mode       = await _uiDispatcher.InvokeAsync(() => Mode);
            var mediaPos   = await _uiDispatcher.InvokeAsync(() => MediaPosition);
            var albumId    = album?.Id ?? "";
            var newChatN   = await _uiDispatcher.InvokeAsync(() => NewChatEveryN);
            var rotEnabled = await _uiDispatcher.InvokeAsync(() => AlbumRotationEnabled);

            for (var extractAttempt = 1; extractAttempt <= ExtractionRetryMaxAttempts; extractAttempt++)
            {
                if (mode == SessionMode.Cooking)
                {
                    var posts = await ParseCookingResponseAsync(raw);
                    if (posts.Count == 0)
                    {
                        if (extractAttempt >= ExtractionRetryMaxAttempts)
                        {
                            await SetStatus("❌ 요리 포맷 추출 실패 (===DETAIL_NNN=== 섹션 없음)");
                            return;
                        }

                        await SetStatus($"⏳ [{albumName}] AI 응답 불완전 - {ExtractionRetryDelaySeconds}초 후 재추출 ({extractAttempt}/{ExtractionRetryMaxAttempts})");
                        await Task.Delay(TimeSpan.FromSeconds(ExtractionRetryDelaySeconds), ct).ConfigureAwait(false);
                        raw = await await _uiDispatcher.InvokeAsync(() => execFn(ResponseExtractor.BuildExtractScript()));
                        continue;
                    }

                    // 중복 제목 방지
                    var existingCookingTitles = _writer.GetExistingTitles(slug);
                    var existingCookingNumbers = existingCookingTitles
                        .Select(ExtractCookingNumberFromTitle)
                        .Where(n => n > 0)
                        .ToHashSet();
                    var newPosts = posts.Where(p =>
                        !existingCookingTitles.Any(t => string.Equals(t, p.Title, StringComparison.OrdinalIgnoreCase)) &&
                        !existingCookingNumbers.Contains(ExtractCookingNumberFromTitle(p.Title))
                    ).ToList();
                    if (newPosts.Count == 0) { await SetStatus($"⏭ [{albumName}] 모두 중복 제목 스킵"); return; }

                    foreach (var p in newPosts) await _writer.SavePostAsync(slug, p, []);
                    _cycleCount++;
                    await SetStatus($"✅ [{albumName}] #{_cycleCount} {newPosts.Count}개 저장! ({DateTime.Now:HH:mm})");
                    break;
                }
                else
                {
                    var result = ParseTravelResponse(raw);
                    if (result is null || string.IsNullOrWhiteSpace(result.Content))
                    {
                        if (extractAttempt >= ExtractionRetryMaxAttempts)
                        {
                            await SetStatus("❌ 추출 실패 (===CONTENT=== 섹션 없음 — AI 응답 불완전)");
                            return;
                        }

                        await SetStatus($"⏳ [{albumName}] AI 응답 불완전 - {ExtractionRetryDelaySeconds}초 후 재추출 ({extractAttempt}/{ExtractionRetryMaxAttempts})");
                        await Task.Delay(TimeSpan.FromSeconds(ExtractionRetryDelaySeconds), ct).ConfigureAwait(false);
                        raw = await await _uiDispatcher.InvokeAsync(() => execFn(ResponseExtractor.BuildExtractScript()));
                        continue;
                    }

                    List<string> validPhotos = [];
                    if (result.Photos.Count > 0)
                    {
                        await SetStatus("🔍 이미지 검증 중...");
                        var allValid = await ValidateImageUrlsAsync(result.Photos, msg => _ = SetStatus(msg));
                        validPhotos = allValid.Take(3).ToList();  // 여행: 최대 3개 사용
                    }
                    var content = NormalizeTravelContent(RemoveInvalidInlineImages(result.Content, validPhotos));

                    var postTitle = !string.IsNullOrWhiteSpace(result.Title) ? result.Title : $"[{albumName}] {DateTime.Now:MM/dd HH:mm}";

                    // 중복 제목 방지 — 같은 제목이 이미 저장됐으면 스킵
                    var existingTitles = _writer.GetExistingTitles(slug);
                    if (existingTitles.Any(t => string.Equals(t, postTitle, StringComparison.OrdinalIgnoreCase)))
                    {
                        await SetStatus($"⏭ [{albumName}] 중복 제목 스킵: {postTitle[..Math.Min(30, postTitle.Length)]}...");
                        return;
                    }

                    var post = new PostEntry
                    {
                        Title          = postTitle,
                        Content        = content,
                        AlbumId        = albumId,
                        PostedAt       = DateTime.Now,
                        MediaPosition  = mediaPos,
                        PhotoFileNames = validPhotos,
                        VideoFileNames = result.Videos,
                    };
                    await _writer.SavePostAsync(slug, post, []);
                    _cycleCount++;
                    await SetStatus($"✅ [{albumName}] #{_cycleCount} 저장! ({DateTime.Now:HH:mm}) 📷{validPhotos.Count} 🎬{result.Videos.Count}");
                    break;
                }
            }

            // 저장 완료 후 프롬프트 갱신 — 방금 쓴 제목이 다음 사이클 "제외 목록"에 반영됨
            await _uiDispatcher.InvokeAsync(() => RefreshPromptWithExisting());

            if (rotEnabled) await _uiDispatcher.InvokeAsync(() => RotateAlbum());

            if (newChatN > 0 && _cycleCount % newChatN == 0)
            {
                await SetStatus("🔄 새 채팅으로 이동 중...");
                await Task.Delay(2000, ct).ConfigureAwait(false);
                await _uiDispatcher.InvokeAsync(() => NavigateTab?.Invoke("__new_chat__"));
                // 새 채팅 페이지 로딩 완료까지 충분히 대기
                for (int i = 15; i > 0; i--)
                {
                    if (ct.IsCancellationRequested) return;
                    await SetStatus($"🔄 새 채팅 로딩 대기 {i}초...");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
                await SetStatus("✅ 새 채팅 준비 완료");
            }
        }
        catch (OperationCanceledException) { await SetStatus("⏹ 취소됨"); }
        catch (Exception ex)               { await SetStatus($"❌ {ex.Message}"); }
    }

    /// <summary>
    /// \if KO
    /// <para>Status 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the status value.</para>
    /// \endif
    /// </summary>
    /// <param name="msg">
    /// \if KO
    /// <para>msg에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for msg.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Set Status 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the set status operation.</para>
    /// \endif
    /// </returns>
    private Task SetStatus(string msg) => _uiDispatcher.InvokeAsync(() => LoopStatus = msg).Task;

    /// <summary>
    /// \if KO
    /// <para>Rotate Album 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the rotate album operation.</para>
    /// \endif
    /// </summary>
    private void RotateAlbum()
    {
        if (Albums.Count == 0) return;
        _albumIndex   = (_albumIndex + 1) % Albums.Count;
        SelectedAlbum = Albums[_albumIndex];
    }

    // ── 수동 전송 / 추출 ──────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Send Prompt Now Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the send prompt now async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Send Prompt Now Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the send prompt now async operation.</para>
    /// \endif
    /// </returns>
    private async Task SendPromptNowAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText)) { PromptStatus = "❌ 프롬프트를 입력하세요."; return; }
        if (ExecuteScriptAsync == null)            { PromptStatus = "❌ 브라우저 준비 안 됨"; return; }
        var albumName = SelectedAlbum?.Name ?? "전체";
        var prompt    = PromptText.Replace("{앨범}", albumName).Replace("{album}", albumName);
        await ExecuteScriptAsync(PromptInjector.BuildInjectScript(prompt));
        PromptStatus = $"✅ 전송 ({DateTime.Now:HH:mm})";
    }

    /// <summary>
    /// \if KO
    /// <para>Extract And Save Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract and save async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Extract And Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the extract and save async operation.</para>
    /// \endif
    /// </returns>
    private async Task ExtractAndSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) { ExtractStatus = "❌ 슬러그를 선택하세요."; return; }
        if (ExecuteScriptAsync == null)               { ExtractStatus = "❌ 브라우저 준비 안 됨"; return; }

        ExtractStatus = "⏳ AI 응답 추출 중...";
        var raw = await ExecuteScriptAsync(ResponseExtractor.BuildExtractScript());

        try
        {
            for (var extractAttempt = 1; extractAttempt <= ExtractionRetryMaxAttempts; extractAttempt++)
            {
                if (Mode == SessionMode.Cooking)
                {
                    var posts = await ParseCookingResponseAsync(raw);
                    if (posts.Count == 0)
                    {
                        if (extractAttempt >= ExtractionRetryMaxAttempts)
                        {
                            ExtractStatus = "❌ 요리 포맷 추출 실패 (===DETAIL_NNN_CONTENT=== 없음)";
                            return;
                        }

                        ExtractStatus = $"⏳ AI 응답 불완전 - {ExtractionRetryDelaySeconds}초 후 재추출 ({extractAttempt}/{ExtractionRetryMaxAttempts})";
                        await Task.Delay(TimeSpan.FromSeconds(ExtractionRetryDelaySeconds));
                        raw = await ExecuteScriptAsync(ResponseExtractor.BuildExtractScript());
                        continue;
                    }

                    var existingCookingTitles = _writer.GetExistingTitles(SelectedSlug);
                    var existingCookingNumbers = existingCookingTitles
                        .Select(ExtractCookingNumberFromTitle)
                        .Where(n => n > 0)
                        .ToHashSet();
                    var newPosts = posts.Where(p =>
                        !existingCookingTitles.Any(t => string.Equals(t, p.Title, StringComparison.OrdinalIgnoreCase)) &&
                        !existingCookingNumbers.Contains(ExtractCookingNumberFromTitle(p.Title))
                    ).ToList();
                    if (newPosts.Count == 0) { ExtractStatus = "⏭ 모두 중복 제목/번호 스킵"; return; }

                    foreach (var p in newPosts) await _writer.SavePostAsync(SelectedSlug, p, []);
                    ExtractStatus = $"✅ {newPosts.Count}개 저장! ({DateTime.Now:HH:mm})";
                    break;
                }
                else
                {
                    var result = ParseTravelResponse(raw);
                    if (result is null || string.IsNullOrWhiteSpace(result.Content))
                    {
                        if (extractAttempt >= ExtractionRetryMaxAttempts)
                        {
                            ExtractStatus = "❌ 응답 없음";
                            return;
                        }

                        ExtractStatus = $"⏳ AI 응답 불완전 - {ExtractionRetryDelaySeconds}초 후 재추출 ({extractAttempt}/{ExtractionRetryMaxAttempts})";
                        await Task.Delay(TimeSpan.FromSeconds(ExtractionRetryDelaySeconds));
                        raw = await ExecuteScriptAsync(ResponseExtractor.BuildExtractScript());
                        continue;
                    }

                    var albumName = SelectedAlbum?.Name ?? "전체";
                    List<string> validPhotos = [];
                    if (result.Photos.Count > 0)
                    {
                        ExtractStatus = "🔍 이미지 검증...";
                        var allValid = await ValidateImageUrlsAsync(result.Photos, msg => ExtractStatus = msg);
                        validPhotos = allValid.Take(3).ToList();  // 여행: 최대 3개
                    }
                    var content = NormalizeTravelContent(RemoveInvalidInlineImages(result.Content, validPhotos));

                    var post = new PostEntry
                    {
                        Title          = !string.IsNullOrWhiteSpace(result.Title) ? result.Title : $"[{albumName}] {DateTime.Now:MM/dd HH:mm}",
                        Content        = content,
                        AlbumId        = SelectedAlbum?.Id ?? "",
                        MediaPosition  = MediaPosition,
                        PostedAt       = DateTime.Now,
                        PhotoFileNames = validPhotos,
                        VideoFileNames = result.Videos,
                    };
                    await _writer.SavePostAsync(SelectedSlug, post, []);
                    ExtractStatus = $"✅ [{albumName}] 저장! ({DateTime.Now:HH:mm}) 📷{validPhotos.Count} 🎬{result.Videos.Count}";
                    break;
                }
            }
        }
        catch (Exception ex) { ExtractStatus = $"❌ {ex.Message}"; }
    }

    // ── 파싱 / 검증 ───────────────────────────────────────────
    /// <summary>
    /// \if KO
    /// <para>Ai Post Result 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates ai post result functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed record AiPostResult(string Title, string Content, List<string> Photos, List<string> Videos);

    // AI 응답 raw JSON → 텍스트 변환
    /// <summary>
    /// \if KO
    /// <para>Unwrap Raw 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the unwrap raw operation.</para>
    /// \endif
    /// </summary>
    /// <param name="raw">
    /// \if KO
    /// <para>raw에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for raw.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Unwrap Raw 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the unwrap raw operation.</para>
    /// \endif
    /// </returns>
    private static string UnwrapRaw(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        try
        {
            var u = JsonSerializer.Deserialize<string>(raw) ?? raw;
            var d = JsonDocument.Parse(u);
            if (d.RootElement.TryGetProperty("text", out var tp)) return tp.GetString() ?? u;
            return u;
        }
        catch { return raw.Trim('"').Replace("\\n", "\n").Replace("\\\"", "\""); }
    }

    // ===TAG=== 섹션 추출 공통 헬퍼
    /// <summary>
    /// \if KO
    /// <para>Section 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the section value.</para>
    /// \endif
    /// </summary>
    /// <param name="text">
    /// \if KO
    /// <para>text에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for text.</para>
    /// \endif
    /// </param>
    /// <param name="tag">
    /// \if KO
    /// <para>tag에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for tag.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Section 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get section operation.</para>
    /// \endif
    /// </returns>
    private static string GetSection(string text, string tag)
    {
        var marker = $"==={tag}===";
        var s = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (s < 0) return "";
        s += marker.Length;
        var n = text.IndexOf("===", s);
        return text[s..(n > s ? n : text.Length)].Trim();
    }

    // content 안의 [IMAGES N번째 URL] 플레이스홀더를 실제 URL로 치환
    // offset: 이 content가 몇 번째 이미지부터 시작하는지 (여행=0, 요리 각 포스트=해당 순번)
    /// <summary>
    /// \if KO
    /// <para>Substitute Images 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the substitute images operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <param name="urls">
    /// \if KO
    /// <para>urls에 사용할 <c>IList&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IList&lt;string&gt;</c> value used for urls.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Substitute Images 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the substitute images operation.</para>
    /// \endif
    /// </returns>
    private static string SubstituteImages(string content, IList<string> urls, int offset = 0)
    {
        // 한국어 순서 표현 매핑
        string[] ordinals = ["첫 번째", "두 번째", "세 번째", "네 번째", "다섯 번째"];
        var result = content;

        for (int i = 0; i < urls.Count; i++)
        {
            // "[IMAGES N번째 URL]" 형태 치환
            if (i < ordinals.Length)
                result = result.Replace($"[IMAGES {ordinals[i]} URL]", urls[i], StringComparison.OrdinalIgnoreCase);
        }

        // offset 기반 치환: 요리 포스트별로 "첫 번째"가 실제로는 offset번째 이미지를 가리킴
        if (offset > 0 && offset < ordinals.Length && offset < urls.Count + offset)
        {
            // 이미 치환됐을 수 있으니 남은 [IMAGES 첫 번째 URL] 형태만 처리
            result = result.Replace("[IMAGES 첫 번째 URL]", urls.Count > 0 ? urls[0] : "", StringComparison.OrdinalIgnoreCase);
        }

        // 치환 안 된 플레이스홀더 img 태그 제거 (깨진 이미지 방지)
        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            @"<img\s[^>]*src=""[^""]*\[IMAGES[^\]]*\][^""]*""[^>]*/?>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return result;
    }

    // ── 여행 모드 파싱 (===TITLE=== / ===CONTENT=== / ===IMAGES=== / ===VIDEO===) ──
    /// <summary>
    /// \if KO
    /// <para>Parse Travel Response 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse travel response operation.</para>
    /// \endif
    /// </summary>
    /// <param name="raw">
    /// \if KO
    /// <para>raw에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for raw.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Travel Response 작업에서 생성한 <c>AiPostResult?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>AiPostResult?</c> result produced by the parse travel response operation.</para>
    /// \endif
    /// </returns>
    private static AiPostResult? ParseTravelResponse(string? raw)
    {
        var text = UnwrapRaw(raw);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var content = GetSection(text, "CONTENT");
        if (string.IsNullOrWhiteSpace(content))
        {
            // ===CONTENT=== 마커를 AI가 빠뜨린 경우 — ===TITLE=== 다음 줄 ~ ===IMAGES=== 앞 텍스트로 fallback
            var titleEnd   = text.IndexOf("===TITLE===", StringComparison.OrdinalIgnoreCase);
            var imagesIdx  = text.IndexOf("===IMAGES===", StringComparison.OrdinalIgnoreCase);
            if (titleEnd >= 0 && imagesIdx > titleEnd)
            {
                var afterTitle = text.IndexOf('\n', titleEnd + "===TITLE===".Length);
                if (afterTitle >= 0)
                {
                    var afterTitleLine = text.IndexOf('\n', afterTitle + 1);
                    if (afterTitleLine >= 0 && afterTitleLine < imagesIdx)
                        content = text[afterTitleLine..imagesIdx].Trim();
                }
            }
            if (string.IsNullOrWhiteSpace(content)) return null;
        }

        var photos = GetSection(text, "IMAGES")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Where(IsPlausibleImageUrl)
            .ToList();
        var videos = GetSection(text, "VIDEO")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsAllowedVideoUrl).ToList();

        content = NormalizeTravelContent(SubstituteImages(content, photos));
        photos = MergeUrls(photos, ExtractImageUrls(content));
        return new AiPostResult(GetSection(text, "TITLE"), content, photos, videos);
    }

    // ── 요리 모드 파싱 (===DETAIL_NNN_TITLE=== / ===DETAIL_NNN_CONTENT===) ──
    /// <summary>
    /// \if KO
    /// <para>Parse Cooking Response Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the parse cooking response async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="raw">
    /// \if KO
    /// <para>raw에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for raw.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Parse Cooking Response Async 작업에서 생성한 <c>Task&lt;List&lt;PostEntry&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;PostEntry&gt;&gt;</c> result produced by the parse cooking response async operation.</para>
    /// \endif
    /// </returns>
    private async Task<List<PostEntry>> ParseCookingResponseAsync(string? raw)
    {
        var text = UnwrapRaw(raw);
        if (string.IsNullOrWhiteSpace(text)) return [];

        // IMAGES 섹션에서 이미지 URL 목록 — 실제 존재하는 것만 사용
        var rawImageUrls = GetSection(text, "IMAGES")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Where(IsPlausibleImageUrl)
            .ToList();
        // 이미 사용한 이미지 URL 제외하여 중복 이미지 방지
        var validImages = rawImageUrls.Count > 0
            ? await ValidateImageUrlsAsync(rawImageUrls, null, _usedImageUrls)
            : [];
        // 요리: 포스트당 최대 2개 사용 — 검증 통과한 전체 풀에서 순서대로 배분
        const int CookingImagesPerPost = 2;
        var imageUrls = validImages.ToArray();

        var posts = new List<PostEntry>();
        var timeline = _writer.GetCookingTimelineState(SelectedSlug);
        var nextPostedAt = GetNextCookingSlot(timeline.LastPostedAt);
        var nextNumber = timeline.LastNumber > 0 ? timeline.LastNumber + 1 : 1;

        // ===DETAIL_NNN_TITLE=== 패턴으로 포스트 수 감지
        // AI가 [번호1], [새숫자] 같은 자리표시자를 남겨도 가능한 범위에서 복구한다.
        var detailNums = Regex
            .Matches(text, @"===DETAIL_(?<num>\d+|\[[^\]]+\])_TITLE===", RegexOptions.IgnoreCase)
            .Select(m => m.Groups["num"].Value)
            .Distinct()
            .OrderBy(n => int.TryParse(n, out var i) ? i : int.MaxValue)
            .ThenBy(n => n)
            .ToList();

        int imgIdx = 0;
        foreach (var num in detailNums)
        {
            var title   = GetSection(text, $"DETAIL_{num}_TITLE");
            var date    = GetSection(text, $"DETAIL_{num}_DATE");
            var content = GetSection(text, $"DETAIL_{num}_CONTENT");

            if (string.IsNullOrWhiteSpace(content)) continue;
            var displayNum = int.TryParse(num, out _) ? num : $"{DateTime.Now:HHmm}{posts.Count + 1}";
            var postedAt = nextPostedAt;
            var mealName = GetCookingMealName(postedAt);
            title = NormalizeCookingTitle(title, nextNumber, mealName);

            // 이 포스트에 해당하는 이미지 — 검증 풀에서 최대 CookingImagesPerPost개 순서대로
            var postImages = imageUrls
                .Skip(imgIdx)
                .Take(CookingImagesPerPost)
                .ToList();
            imgIdx += CookingImagesPerPost;

            var substitutedSource = SubstituteImages(content, postImages);
            var bodyImages = ExtractImageUrls(substitutedSource);
            var substituted = NormalizeCookingContent(substitutedSource);

            // 사용된 이미지 URL 추적 (다음 사이클에서 중복 방지)
            foreach (var img in postImages) _usedImageUrls.Add(img);

            posts.Add(new PostEntry
            {
                Title          = string.IsNullOrWhiteSpace(title) ? $"[1983 집밥육아 #{nextNumber}] {mealName} 집밥 기록" : title,
                Content        = substituted,
                AlbumId        = SelectedAlbum?.Id ?? "",
                PostedAt       = postedAt,
                MediaPosition  = MediaPosition,
                PhotoFileNames = MergeUrls(postImages, bodyImages),
                VideoFileNames = [],
            });

            nextPostedAt = GetNextCookingSlot(postedAt);
            nextNumber++;
        }

        return posts;
    }

    /// <summary>
    /// \if KO
    /// <para>Next Cooking Slot 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the next cooking slot value.</para>
    /// \endif
    /// </summary>
    /// <param name="lastPostedAt">
    /// \if KO
    /// <para>last Posted At에 사용할 <c>DateTime?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime?</c> value used for last posted at.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Next Cooking Slot 작업에서 생성한 <c>DateTime</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime</c> result produced by the get next cooking slot operation.</para>
    /// \endif
    /// </returns>
    private static DateTime GetNextCookingSlot(DateTime? lastPostedAt)
    {
        if (lastPostedAt is null)
            return new DateTime(1983, 1, 1, 8, 0, 0);

        var last = lastPostedAt.Value;
        return last.Hour switch
        {
            < 12 => last.Date.AddHours(12),
            < 18 => last.Date.AddHours(18),
            _ => last.Date.AddDays(1).AddHours(8),
        };
    }

    /// <summary>
    /// \if KO
    /// <para>Cooking Meal Name 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the cooking meal name value.</para>
    /// \endif
    /// </summary>
    /// <param name="postedAt">
    /// \if KO
    /// <para>posted At에 사용할 <c>DateTime</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DateTime</c> value used for posted at.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Cooking Meal Name 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get cooking meal name operation.</para>
    /// \endif
    /// </returns>
    private static string GetCookingMealName(DateTime postedAt) =>
        postedAt.Hour switch
        {
            < 12 => "아침",
            < 18 => "점심",
            _ => "저녁",
        };

    /// <summary>
    /// \if KO
    /// <para>Normalize Cooking Title 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize cooking title operation.</para>
    /// \endif
    /// </summary>
    /// <param name="title">
    /// \if KO
    /// <para>title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for title.</para>
    /// \endif
    /// </param>
    /// <param name="number">
    /// \if KO
    /// <para>number에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for number.</para>
    /// \endif
    /// </param>
    /// <param name="mealName">
    /// \if KO
    /// <para>meal Name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for meal name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Cooking Title 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize cooking title operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeCookingTitle(string title, int number, string mealName)
    {
        var normalized = string.IsNullOrWhiteSpace(title)
            ? $"[1983 집밥육아 #{number}] {mealName} 집밥 기록"
            : title.Trim();

        normalized = Regex.Replace(normalized, @"#\d+|#번호\d+|#새숫자", $"#{number}", RegexOptions.IgnoreCase);
        normalized = normalized
            .Replace("[번호1]", number.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[번호2]", number.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[번호3]", number.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[새숫자]", number.ToString(), StringComparison.OrdinalIgnoreCase);

        if (!Regex.IsMatch(normalized, @"#\d+"))
            normalized = $"[1983 집밥육아 #{number}] {normalized}";

        normalized = Regex.IsMatch(normalized, @"아침|점심|저녁")
            ? Regex.Replace(normalized, @"아침|점심|저녁", mealName, RegexOptions.None, TimeSpan.FromMilliseconds(100))
            : Regex.Replace(normalized, @"(\]\s*)", "$1" + mealName + " ", RegexOptions.None, TimeSpan.FromMilliseconds(100));

        return normalized;
    }

    /// <summary>
    /// \if KO
    /// <para>Extract Cooking Number From Title 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract cooking number from title operation.</para>
    /// \endif
    /// </summary>
    /// <param name="title">
    /// \if KO
    /// <para>title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for title.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Extract Cooking Number From Title 작업에서 생성한 <c>int</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> result produced by the extract cooking number from title operation.</para>
    /// \endif
    /// </returns>
    private static int ExtractCookingNumberFromTitle(string title)
    {
        var match = Regex.Match(title ?? "", @"#(?<n>\d+)");
        return match.Success && int.TryParse(match.Groups["n"].Value, out var number) ? number : 0;
    }

    /// <summary>
    /// \if KO
    /// <para>Extract Image Urls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the extract image urls operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Extract Image Urls 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the extract image urls operation.</para>
    /// \endif
    /// </returns>
    private static List<string> ExtractImageUrls(string content) =>
        Regex.Matches(content, @"<img\b[^>]*\bsrc\s*=\s*[""'](?<url>https?://[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase)
            .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups["url"].Value.Trim()))
            .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>
    /// \if KO
    /// <para>Invalid Inline Images 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the invalid inline images item.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <param name="allowedUrls">
    /// \if KO
    /// <para>allowed Urls에 사용할 <c>IReadOnlyCollection&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyCollection&lt;string&gt;</c> value used for allowed urls.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Remove Invalid Inline Images 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the remove invalid inline images operation.</para>
    /// \endif
    /// </returns>
    private static string RemoveInvalidInlineImages(string content, IReadOnlyCollection<string> allowedUrls)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content;

        var allowed = allowedUrls
            .Select(u => System.Net.WebUtility.HtmlDecode(u).Trim())
            .Where(u => u.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Regex.Replace(
            content,
            @"<img\b[^>]*>",
            match =>
            {
                var src = Regex.Match(match.Value, @"\bsrc\s*=\s*[""'](?<url>https?://[^""']+)[""']", RegexOptions.IgnoreCase);
                if (!src.Success)
                    return "";

                var url = System.Net.WebUtility.HtmlDecode(src.Groups["url"].Value.Trim());
                return allowed.Contains(url) ? match.Value : "";  // 검증 통과 URL만 유지
            },
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// \if KO
    /// <para>Merge Urls 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the merge urls operation.</para>
    /// \endif
    /// </summary>
    /// <param name="primary">
    /// \if KO
    /// <para>primary에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for primary.</para>
    /// \endif
    /// </param>
    /// <param name="secondary">
    /// \if KO
    /// <para>secondary에 사용할 <c>IEnumerable&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IEnumerable&lt;string&gt;</c> value used for secondary.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Merge Urls 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the merge urls operation.</para>
    /// \endif
    /// </returns>
    private static List<string> MergeUrls(IEnumerable<string> primary, IEnumerable<string> secondary)
    {
        var merged = new List<string>();
        foreach (var url in primary.Concat(secondary))
        {
            var clean = url.Trim();
            if (clean.Length == 0) continue;
            if (!merged.Contains(clean, StringComparer.OrdinalIgnoreCase))
                merged.Add(clean);
        }
        return merged;
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Travel Content 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize travel content operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Travel Content 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize travel content operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeTravelContent(string content)
    {
        var text = NormalizeKnownHeadings(content, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["한눈에 보는 포인트"] = "## 한눈에 보는 포인트",
            ["이런 가족에게 추천"] = "## 이런 가족에게 추천",
            ["아이와 좋았던 점"] = "## 아이와 좋았던 점",
            ["동선과 시간표"] = "## 동선과 시간표",
            ["여행 경비 예상"] = "## 여행 경비 예상",
            ["주차·화장실·유모차 포인트"] = "## 주차·화장실·유모차 포인트",
            ["주차・화장실・유모차 포인트"] = "## 주차·화장실·유모차 포인트",
            ["준비물과 주의할 것"] = "## 준비물과 주의할 것",
            ["방문 전 확인할 것"] = "## 방문 전 확인할 것",
            ["총평"] = "## 총평",
        });

        return NormalizeImageBlockSpacing(NormalizeSimpleTables(text));
    }

    /// <summary>
    /// \if KO
    /// <para>Is Allowed Video Url 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is allowed video url.</para>
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
    /// <para>Is Allowed Video Url 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is allowed video url condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsAllowedVideoUrl(string url)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;
        if (!url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) &&
            !url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
            return true;

        // 일반 watch/shorts 링크는 소유자 설정으로 앱 내 iframe 재생이 자주 막힌다.
        return url.Contains("/embed/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Cooking Content 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize cooking content operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Cooking Content 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize cooking content operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeCookingContent(string content)
    {
        var text = Regex.Replace(
            content,
            @"<img\b[^>]*>",
            "",
            RegexOptions.IgnoreCase);

        text = NormalizeKnownHeadings(text, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["오늘의 아기 밥"] = "## 오늘의 아기 밥",
            ["오늘의 가족 집밥"] = "## 오늘의 가족 집밥",
            ["만드는 법"] = "## 만드는 법",
            ["재료: 어른 2~3인분"] = "### 재료: 어른 2~3인분",
            ["재료: 어른 2-3인분"] = "### 재료: 어른 2~3인분",
            ["손질하기"] = "### 손질하기",
            ["양념에 재우기"] = "### 양념에 재우기",
            ["볶기"] = "### 볶기",
            ["끓이기"] = "### 끓이기",
            ["조리기"] = "### 조리기",
            ["부치기"] = "### 부치기",
            ["초보자 메모"] = "### 초보자 메모",
            ["별점"] = "## 별점",
            ["파워블로그 한줄평"] = "## 파워블로그 한줄평",
        });

        return NormalizeImageBlockSpacing(NormalizeIngredientLines(text)).Trim();
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Known Headings 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize known headings operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <param name="headings">
    /// \if KO
    /// <para>headings에 사용할 <c>IReadOnlyDictionary&lt;string, string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyDictionary&lt;string, string&gt;</c> value used for headings.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Known Headings 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize known headings operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeKnownHeadings(string content, IReadOnlyDictionary<string, string> headings)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith("<", StringComparison.Ordinal) ||
                trimmed.Length == 0)
                continue;

            if (headings.TryGetValue(trimmed, out var heading))
                lines[i] = heading;
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Simple Tables 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize simple tables operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Simple Tables 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize simple tables operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeSimpleTables(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        for (var i = 0; i < lines.Count; i++)
        {
            var expanded = TryExpandCollapsedPipeTable(lines[i]);
            if (expanded.Count > 0)
            {
                if (i > 0 && !string.IsNullOrWhiteSpace(lines[i - 1]) && expanded[0].Length > 0)
                    expanded.Insert(0, "");
                if (i + 1 < lines.Count && !string.IsNullOrWhiteSpace(lines[i + 1]) && expanded[^1].Length > 0)
                    expanded.Add("");
                lines.RemoveAt(i);
                lines.InsertRange(i, expanded);
                i += expanded.Count - 1;
                continue;
            }

            var cols = SplitTableLine(lines[i]);
            if (cols.Count < 3 || !cols.Any(c => c.Contains("비용", StringComparison.OrdinalIgnoreCase))) continue;

            var rows = new List<List<string>> { cols };
            var j = i + 1;
            while (j < lines.Count)
            {
                var row = SplitTableLine(lines[j]);
                if (row.Count != cols.Count) break;
                rows.Add(row);
                j++;
            }

            if (rows.Count < 2) continue;

            var md = new List<string>
            {
                "| " + string.Join(" | ", rows[0]) + " |",
                "| " + string.Join(" | ", rows[0].Select((_, idx) => idx == 1 ? "---:" : "---")) + " |"
            };
            md.AddRange(rows.Skip(1).Select(row => "| " + string.Join(" | ", row) + " |"));
            lines.RemoveRange(i, rows.Count);
            lines.InsertRange(i, md);
            i += md.Count - 1;
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Image Block Spacing 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize image block spacing operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Image Block Spacing 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize image block spacing operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeImageBlockSpacing(string content)
    {
        var text = Regex.Replace(
            content.Replace("\r\n", "\n"),
            @"[ \t]*(<img\b)",
            "\n$1",
            RegexOptions.IgnoreCase);
        var lines = text.Split('\n');
        var normalized = new List<string>();
        var needsBlankBeforeNextContent = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            var isImage = line.TrimStart().StartsWith("<img", StringComparison.OrdinalIgnoreCase);

            if (isImage)
            {
                AddBlankLineIfNeeded(normalized);
                normalized.Add(line.Trim());
                needsBlankBeforeNextContent = true;
                continue;
            }

            if (needsBlankBeforeNextContent && line.Trim().Length > 0)
            {
                AddBlankLineIfNeeded(normalized);
                needsBlankBeforeNextContent = false;
            }
            else if (line.Trim().Length == 0)
            {
                needsBlankBeforeNextContent = false;
            }

            normalized.Add(line);
        }

        return string.Join("\n", normalized).Trim();
    }

    /// <summary>
    /// \if KO
    /// <para>Blank Line If Needed 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the blank line if needed item.</para>
    /// \endif
    /// </summary>
    /// <param name="lines">
    /// \if KO
    /// <para>lines에 사용할 <c>List&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> value used for lines.</para>
    /// \endif
    /// </param>
    private static void AddBlankLineIfNeeded(List<string> lines)
    {
        if (lines.Count > 0 && lines[^1].Trim().Length > 0)
            lines.Add("");
    }

    /// <summary>
    /// \if KO
    /// <para>Expand Collapsed Pipe Table 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to expand collapsed pipe table and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="line">
    /// \if KO
    /// <para>line에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for line.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Expand Collapsed Pipe Table 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the try expand collapsed pipe table operation.</para>
    /// \endif
    /// </returns>
    private static List<string> TryExpandCollapsedPipeTable(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.Contains('|') || !trimmed.Contains("---", StringComparison.Ordinal))
            return [];

        var cells = trimmed
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        var firstSeparator = cells.FindIndex(c => c.TrimStart().StartsWith("---", StringComparison.Ordinal));
        if (firstSeparator < 2) return [];

        var columnCount = firstSeparator;
        if (columnCount < 2 || cells.Count < columnCount * 3)
            return [];

        var separator = cells.Skip(columnCount).Take(columnCount).ToList();
        if (separator.Count != columnCount || separator.Any(c => !c.TrimStart().StartsWith("---", StringComparison.Ordinal)))
            return [];

        var header = cells.Take(columnCount).Select(c => c.Trim()).ToArray();
        var values = cells.Skip(columnCount * 2).Select(c => c.Trim()).ToList();
        var dataRows = values
            .Chunk(columnCount)
            .Where(row => row.Length == columnCount)
            .Select(row => row.Select(c => c.Trim()).ToArray())
            .ToList();

        if (dataRows.Count == 0)
            return [];

        var result = new List<string>
        {
            "| " + string.Join(" | ", header) + " |",
            "| " + string.Join(" | ", Enumerable.Range(0, columnCount).Select(idx => idx == 1 ? "---:" : "---")) + " |"
        };
        result.AddRange(dataRows.Select(row => "| " + string.Join(" | ", row) + " |"));
        return result;
    }

    /// <summary>
    /// \if KO
    /// <para>Split Table Line 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the split table line operation.</para>
    /// \endif
    /// </summary>
    /// <param name="line">
    /// \if KO
    /// <para>line에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for line.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Split Table Line 작업에서 생성한 <c>List&lt;string&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> result produced by the split table line operation.</para>
    /// \endif
    /// </returns>
    private static List<string> SplitTableLine(string line) =>
        line.Contains('\t')
            ? line.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList()
            : Regex.Split(line.Trim(), @"\s{2,}")
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

    /// <summary>
    /// \if KO
    /// <para>Normalize Ingredient Lines 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize ingredient lines operation.</para>
    /// \endif
    /// </summary>
    /// <param name="content">
    /// \if KO
    /// <para>content에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Ingredient Lines 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize ingredient lines operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeIngredientLines(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var inIngredients = false;
        var builder = new StringBuilder();

        foreach (var raw in lines)
        {
            var line = raw;
            var trimmed = line.Trim();

            if (trimmed.StartsWith("### 재료:", StringComparison.Ordinal))
            {
                inIngredients = true;
                builder.AppendLine(line);
                continue;
            }

            if (inIngredients && trimmed.StartsWith("### ", StringComparison.Ordinal))
                inIngredients = false;

            if (inIngredients &&
                trimmed.Length > 0 &&
                !trimmed.StartsWith("-", StringComparison.Ordinal) &&
                Regex.IsMatch(trimmed, @"\d+(\.\d+)?\s*(g|kg|ml|l|개|큰술|작은술|컵|cm)\b", RegexOptions.IgnoreCase))
            {
                line = "- " + trimmed;
            }

            builder.AppendLine(line);
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// \if KO
    /// <para>http 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the http value.</para>
    /// \endif
    /// </summary>
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    // HTTP 검증 없이 신뢰하는 도메인 — 실제 이미지 파일만 제공하는 공식 CDN
    /// <summary>
    /// \if KO
    /// <para>trusted Image Hosts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the trusted image hosts value.</para>
    /// \endif
    /// </summary>
    private static readonly string[] _trustedImageHosts =
    [
        "upload.wikimedia.org",
        "images.unsplash.com",
        "images.pexels.com",
        "cdn.imweb.me",
        "tong.visitkorea.or.kr",
        "korean.visitkorea.or.kr",
        "cdn.visitkorea.or.kr",
    ];

    /// <summary>
    /// \if KO
    /// <para>Is Plausible Image Url 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is plausible image url.</para>
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
    /// <para>Is Plausible Image Url 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is plausible image url condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsPlausibleImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        var value = url.Trim();
        if (value.Contains("...", StringComparison.Ordinal) ||
            value.Contains("{", StringComparison.Ordinal) ||
            value.Contains("}", StringComparison.Ordinal) ||
            value.Contains("[", StringComparison.Ordinal) ||
            value.Contains("]", StringComparison.Ordinal))
            return false;

        return true;
    }

    /// <summary>
    /// \if KO
    /// <para>Is Likely Direct Image Url 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is likely direct image url.</para>
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
    /// <para>Is Likely Direct Image Url 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is likely direct image url condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsLikelyDirectImageUrl(string url)
    {
        if (!IsPlausibleImageUrl(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath;
        if (Regex.IsMatch(path, @"\.(jpe?g|png|webp|gif|bmp)(/)?$", RegexOptions.IgnoreCase))
            return true;

        var host = uri.Host.ToLowerInvariant();
        return host.Contains("upload.wikimedia.org", StringComparison.Ordinal) ||
               host.Contains("images.unsplash.com", StringComparison.Ordinal) ||
               host.Contains("images.pexels.com", StringComparison.Ordinal) ||
               host.Contains("cdn.imweb.me", StringComparison.Ordinal);
    }

    /// <summary>
    /// \if KO
    /// <para>Image Urls Async 값의 유효성을 검사합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Validates the image urls async value.</para>
    /// \endif
    /// </summary>
    /// <param name="urls">
    /// \if KO
    /// <para>urls에 사용할 <c>List&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>List&lt;string&gt;</c> value used for urls.</para>
    /// \endif
    /// </param>
    /// <param name="onStatus">
    /// \if KO
    /// <para>on Status에 사용할 <c>Action&lt;string&gt;?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Action&lt;string&gt;?</c> value used for on status.</para>
    /// \endif
    /// </param>
    /// <param name="excludeUrls">
    /// \if KO
    /// <para>exclude Urls에 사용할 <c>ISet&lt;string&gt;?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ISet&lt;string&gt;?</c> value used for exclude urls.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Validate Image Urls Async 작업에서 생성한 <c>Task&lt;List&lt;string&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;string&gt;&gt;</c> result produced by the validate image urls async operation.</para>
    /// \endif
    /// </returns>
    private static async Task<List<string>> ValidateImageUrlsAsync(List<string> urls, Action<string>? onStatus = null, ISet<string>? excludeUrls = null)
    {
        var valid = new List<string>();
        foreach (var url in urls)
        {
            if (!IsPlausibleImageUrl(url))
                continue;
            if (excludeUrls != null && excludeUrls.Contains(url))
                continue;

            try
            {
                // 신뢰 도메인 + 이미지 확장자 → HTTP 검증 생략
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host.ToLowerInvariant();
                    var path = uri.AbsolutePath;
                    bool trustedHost = _trustedImageHosts.Any(h => host.Contains(h, StringComparison.Ordinal));
                    bool imageExt   = Regex.IsMatch(path, @"\.(jpe?g|png|webp|gif|bmp)$", RegexOptions.IgnoreCase);
                    if (trustedHost && imageExt)
                    {
                        valid.Add(url);
                        continue;
                    }
                }

                onStatus?.Invoke($"🔍 {url[..Math.Min(60, url.Length)]}");
                if (await IsReachableImageAsync(url, HttpMethod.Head) ||
                    await IsReachableImageAsync(url, HttpMethod.Get))
                {
                    valid.Add(url);
                }
            }
            catch { }
        }
        return valid;
    }

    /// <summary>
    /// \if KO
    /// <para>Is Reachable Image Async 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is reachable image async.</para>
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
    /// <param name="method">
    /// \if KO
    /// <para>method에 사용할 <c>HttpMethod</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>HttpMethod</c> value used for method.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Reachable Image Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the is reachable image async operation.</para>
    /// \endif
    /// </returns>
    private static async Task<bool> IsReachableImageAsync(string url, HttpMethod method)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!res.IsSuccessStatusCode) return false;
            // Content-Type이 있으면 image/* 인지 확인, 없으면 2xx 성공만으로 통과
            var ct = res.Content.Headers.ContentType?.MediaType;
            return ct == null || ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Travel Prompt 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the travel prompt value.</para>
    /// \endif
    /// </summary>
    /// <param name="existingTitles">
    /// \if KO
    /// <para>existing Titles에 사용할 <c>IReadOnlyList&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> value used for existing titles.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Travel Prompt 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build travel prompt operation.</para>
    /// \endif
    /// </returns>
    public static string BuildTravelPrompt(IReadOnlyList<string> existingTitles) =>
        $$"""
        {앨범} 주제로 한국 가족 나들이·여행 블로그 포스트 1개를 작성해줘.
        아래 형식 그대로만 출력해줘. ===섹션=== 구분자는 반드시 유지하고, 그 외 설명은 쓰지 마.
        코드블록(```), JSON, 주석, 머리말, 맺음말을 절대 쓰지 마.

        AutoWriter 저장 계약:
        - 이 응답은 Families.Web App_Data/Family/{선택슬러그}/posts/*.json 으로 파싱 저장된다.
        - 섹션 태그는 정확히 ===TITLE===, ===CONTENT===, ===IMAGES===, ===VIDEO=== 만 사용한다.
        - 모든 [대괄호 안내문]은 실제 값으로 바꾼다. 대괄호 안내문을 그대로 남기지 마.
        - TITLE은 파일 목록에 보이는 글 제목이므로 한 줄만 작성한다.
        - CONTENT는 Markdown 본문이며, 앱이 그대로 렌더링한다.

        작성 조건:
        - 실제 국내 장소 1곳을 정한다 (실내·실외 모두 가능).
        - 정보는 실제 공식 사이트나 현장 기준으로 구체적으로 적는다.
        - 확인이 불가한 정보는 "방문 전 공식 확인 필요"로 표시한다.
        - 아이 동반 가족 시선에서 실용적으로 쓴다 (유모차, 수유실, 주차, 대기 등).
        - 비용 표는 성인 2명 + 아이 1명 기준으로 작성한다.
        - 여행 경비 예상 표는 반드시 Markdown 표로 출력한다. 표 전체를 한 줄에 붙여 쓰지 말고, 헤더/구분선/각 항목 행을 각각 줄바꿈한다.

        이미지 조건:
        - IMAGES 섹션에 실제 접근 가능한 이미지 URL을 6개 출력한다. "없음" 금지. (많이 줄수록 좋음 — 검증 실패 대비 여유분)
        - 각 URL은 http 또는 https로 시작하는 이미지 직접 URL이어야 한다.
        - URL 줄에는 설명, 번호, 마크다운 이미지 문법을 붙이지 말고 URL만 쓴다.
        - 실제 브라우저에서 열리는 직접 이미지 URL만 사용한다. 엑박이 뜰 수 있는 URL은 쓰지 않는다.
        - URL은 가능하면 .jpg, .jpeg, .png, .webp 로 끝나는 주소를 고른다.
        - URL에 공백, 한글, 괄호, 따옴표가 있으면 반드시 퍼센트 인코딩된 정상 URL로 출력한다.
        - 블로그 글 페이지, 검색 결과 페이지, redirect URL, 썸네일 페이지 URL은 금지한다.
        - 가로 1000px 이상 선명한 대표 이미지를 고른다. 작은 썸네일, 흐릿한 사진, 야간 저화질 사진은 금지.
        - 해당 장소 공식 사이트의 큰 이미지 또는 장소 안내 이미지/지도/전시 이미지를 1순위로 사용한다.
        - 공식 이미지가 작거나 흐리면 Wikimedia, 공공기관 보도자료, Unsplash/Pexels의 실제 큰 이미지로 대체한다.
        - 이미지 URL은 반드시 해당 장소 자체를 보여줘야 한다. 장소와 무관한 교통수단, 음식, 사람, 일반 풍경 사진은 금지한다.
        - Wikimedia를 쓸 때는 upload.wikimedia.org의 실제 파일 URL을 사용한다. commons.wikimedia.org/wiki/... 페이지 URL은 금지한다.
        - Unsplash/Pexels를 쓸 때도 실제 검색으로 확인된 이미지 URL만 쓴다. photo-{실제ID}, {id}, ... 같은 템플릿 URL은 절대 쓰지 않는다.
        - CONTENT 안 <img> 태그 src에도 같은 URL을 그대로 사용한다.

        유튜브:
        - 기본값은 "없음"이다.
        - YouTube를 넣어야 한다면 embed 재생이 허용되는 공식 채널/공공기관 영상 1개만 사용한다.
        - 개인 방문 영상, Shorts, 광고성 영상, iframe 재생 차단 가능성이 큰 영상은 쓰지 말고 "없음"만 쓴다.

        {{(existingTitles.Count > 0 ? $"이미 작성된 장소 목록 (반드시 제외, 절대 반복 금지):\n{string.Join("\n", existingTitles.TakeLast(80).Select(t => $"- {t}"))}\n" : "")}}
        ===TITLE===
        실제 장소명, 아이 동반 핵심 팁 또는 주의사항

        ===CONTENT===

        ## 한눈에 보는 포인트

        이 장소가 아이 동반 가족에게 좋은 이유를 2~3줄로 작성한다.
        이동 동선, 체류 시간, 날씨 조건 등 실용 정보 위주로 쓴다.

        <img class="fa-inline-photo" src="[IMAGES 첫 번째 URL]" alt="[장소 대표 이미지 설명]">

        ## 이런 가족에게 추천

        아이 연령대, 이동 수단, 부모 성향 기준으로 추천 대상을 구체적으로 작성한다.

        ## 아이와 좋았던 점

        아이가 지루하지 않을 요소, 실제 체험 동선, 부모가 편했던 포인트를 작성한다.
        유모차 이용 여부, 수유실/화장실 위치, 그늘/쉬는 공간도 포함한다.

        <img class="fa-inline-photo" src="[IMAGES 두 번째 URL]" alt="[장소 내부 또는 체험 이미지 설명]">

        ## 동선과 시간표

        도착부터 출발까지 시간 흐름에 맞게 이동 순서를 작성한다.
        구체적인 시간대(예: 11시 도착, 12시 점심)와 이유를 함께 쓴다.

        ## 여행 경비 예상

        | 항목 | 예상 비용 | 메모 |
        | --- | ---: | --- |
        | 입장료 | [실제 금액 또는 무료] | 성인/아이 구분 |
        | 주차 | [실제 금액] | 시간당 기준 |
        | 식사 | [실제 금액] | 가족 3인 기준 |
        | 간식 | [실제 금액] | 선택 항목 |
        | 합계 | [실제 금액] | 주차 할인 전 기준 |

        <img class="fa-inline-photo" src="[IMAGES 세 번째 URL]" alt="[주변 풍경 또는 식사 관련 이미지 설명]">

        ## 주차·화장실·유모차 포인트

        주차 위치와 요금, 화장실과 수유실 위치, 유모차 반입/대여 여부를 구체적으로 쓴다.

        ## 준비물과 주의할 것

        챙기면 좋은 것, 주의해야 할 것을 항목별로 작성한다.

        ## 방문 전 확인할 것

        운영시간, 예약 여부, 계절적 제한, 바뀔 수 있는 정보를 명시한다.

        ## 총평

        가족 나들이 장소로서 솔직한 평가를 2~3줄로 작성한다. 추천 대상과 비추 상황도 포함한다.

        ===IMAGES===
        https://...
        https://...
        https://...

        ===VIDEO===
        없음
        """;

    /// <summary>
    /// \if KO
    /// <para>Cooking Prompt 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the cooking prompt value.</para>
    /// \endif
    /// </summary>
    /// <param name="existingTitles">
    /// \if KO
    /// <para>existing Titles에 사용할 <c>IReadOnlyList&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> value used for existing titles.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Cooking Prompt 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build cooking prompt operation.</para>
    /// \endif
    /// </returns>
    public static string BuildCookingPrompt(IReadOnlyList<string> existingTitles) =>
        BuildCookingPrompt(existingTitles, (null, 0));

    /// <summary>
    /// \if KO
    /// <para>Cooking Timeline Instruction 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the cooking timeline instruction value.</para>
    /// \endif
    /// </summary>
    /// <param name="timeline">
    /// \if KO
    /// <para>timeline에 사용할 <c>(DateTime? LastPostedAt, int LastNumber)</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>(DateTime? LastPostedAt, int LastNumber)</c> value used for timeline.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Cooking Timeline Instruction 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build cooking timeline instruction operation.</para>
    /// \endif
    /// </returns>
    private static string BuildCookingTimelineInstruction((DateTime? LastPostedAt, int LastNumber) timeline)
    {
        var nextAt = GetNextCookingSlot(timeline.LastPostedAt);
        var nextNumber = timeline.LastNumber > 0 ? timeline.LastNumber + 1 : 1;
        return $"""
        타임라인 강제 조건:
        - 마지막 생성물 이후에만 이어서 작성한다.
        - 이번 DETAIL 번호는 반드시 {nextNumber}로 쓴다.
        - 이번 식사구분은 반드시 {GetCookingMealName(nextAt)}로 쓴다.
        - 이번 날짜는 반드시 {nextAt:yyyy년 M월 d일}로 쓴다.
        - TITLE, DATE, CONTENT의 식사구분과 날짜가 위 조건과 어긋나면 안 된다.
        """;
    }

    /// <summary>
    /// \if KO
    /// <para>Cooking Prompt 값을 구성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Builds the cooking prompt value.</para>
    /// \endif
    /// </summary>
    /// <param name="existingTitles">
    /// \if KO
    /// <para>existing Titles에 사용할 <c>IReadOnlyList&lt;string&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IReadOnlyList&lt;string&gt;</c> value used for existing titles.</para>
    /// \endif
    /// </param>
    /// <param name="timeline">
    /// \if KO
    /// <para>timeline에 사용할 <c>(DateTime? LastPostedAt, int LastNumber)</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>(DateTime? LastPostedAt, int LastNumber)</c> value used for timeline.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Build Cooking Prompt 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the build cooking prompt operation.</para>
    /// \endif
    /// </returns>
    public static string BuildCookingPrompt(IReadOnlyList<string> existingTitles, (DateTime? LastPostedAt, int LastNumber) timeline) =>
        $$"""
        {앨범} 주제로 1983년 한국 가족 집밥·육아 기록 블로그 포스트를 1개 작성해줘.
        아래 형식 그대로만 출력해줘. ===섹션=== 구분자는 반드시 유지하고, 다른 설명은 쓰지 마.
        코드블록(```), JSON, 주석, 머리말, 맺음말을 절대 쓰지 마.

        AutoWriter 저장 계약:
        - 이 응답은 Families.Web App_Data/Family/{선택슬러그}/posts/*.json 으로 파싱 저장된다.
        - AutoWriter는 ===IMAGES===의 URL 1개와 ===DETAIL_NNN_TITLE===, ===DETAIL_NNN_DATE===, ===DETAIL_NNN_CONTENT=== 섹션만 실제 포스트로 저장한다.
        - DETAIL 번호 NNN은 숫자만 사용한다. 예: 231, 232, 233.
        - 이미 작성된 제목 목록에 있는 번호(#숫자), 식사구분, 메뉴명, 비슷한 메뉴는 절대 다시 쓰지 않는다.
        - 기존 제목에 "#220"이 있으면 새 제목은 #220을 절대 쓰지 말고, 타임라인 강제 조건의 새 번호만 사용한다.
        - 기존 메뉴가 "생선구이"면 고등어구이, 갈치구이, 조기구이처럼 거의 같은 생선구이류도 피한다.
        - 기존 메뉴가 "김밥"이면 주먹밥, 유부초밥처럼 같은 도시락/밥말이류 반복도 피한다.
        - ALBUMS, TIMELINE, 표 형태 요약은 출력하지 마. 저장되지 않으므로 금지.
        - 모든 [대괄호 안내문]은 실제 값으로 바꾼다. 대괄호 안내문을 그대로 남기지 마.
        - TITLE은 파일 목록에 보이는 글 제목이므로 한 줄만 작성한다.
        - CONTENT는 Markdown 본문이며, 앱이 그대로 렌더링한다.
        - CONTENT 안의 제목은 반드시 ##, ###, -, 1. 마크다운 문법을 유지한다.

        글쓰기 톤:
        - 아기 시점("저는 아직 수유만 해요"), 따뜻하고 구체적인 1983년 가정집 분위기
        - 할머니, 아버지, 어머니가 등장하고 각자의 행동과 대화를 자연스럽게 묘사한다
        - 도입부는 그날의 계절·날씨·집 분위기로 시작한다 (첫 문단, 섹션 헤더 없이)

        메뉴 선택:
        - 아침/점심/저녁 중 하나를 고르고, TITLE과 본문 전체에서 같은 식사구분을 유지한다.
        - 1983년 한국 집밥 제철 음식으로 쓴다.
        - 날짜는 1983년 중 자유롭게 (매 실행마다 다르게)
        - 아기에게 직접 먹이는 음식인지 어른 밥상인지 반드시 구분
        - TITLE의 식사구분(아침/점심/저녁), 메뉴명, 본문 음식, 이미지 음식은 반드시 서로 일치한다.
        - 이번 메뉴는 이미 작성된 메뉴 목록과 겹치지 않는 새 메뉴여야 한다.
        - 같은 주재료 반복을 피한다. 최근 목록에 생선 메뉴가 많으면 두부/나물/국/찌개/전/조림/볶음 등 다른 계열로 바꾼다.

        레시피 작성 기준 (초보자가 실제로 따라 만들 수 있어야 함):
        - 재료는 g, ml, cm 단위로 정확히
        - 불 세기(약불/중불/강불)와 조리 시간(분 단위)을 명시
        - 익었는지 확인하는 기준을 눈으로 보이는 변화로 설명
        - "약간", "적당히", "노릇하게"만 쓰지 말고 반드시 기준 수치를 함께 적는다
        - 간이 맞지 않을 때, 탈 것 같을 때 복구 방법 포함

        이미지 조건:
        - IMAGES 섹션에 메뉴 음식 사진 URL을 6개 출력한다. (검증 실패 대비 여유분)
        - 각 URL은 http 또는 https로 시작하는 이미지 직접 URL이어야 한다.
        - URL 줄에는 설명, 번호, 마크다운 이미지 문법을 붙이지 말고 URL만 쓴다.
        - 메뉴와 다른 음식 사진 금지. 예: 고등어조림 글에 샐러드/덮밥/피자 사진 금지.
        - 가로 1000px 이상 선명한 음식 사진만 사용한다. 작은 썸네일, 흐릿한 사진, 생성 이미지 느낌의 사진 금지.
        - 실제 브라우저에서 열리는 직접 이미지 URL만 사용한다. 엑박이 뜰 수 있는 URL은 쓰지 않는다.
        - URL은 가능하면 .jpg, .jpeg, .png, .webp 로 끝나는 주소를 고른다.
        - 우선순위는 upload.wikimedia.org, images.unsplash.com, images.pexels.com 같은 안정적인 이미지 CDN이다.
        - 블로그 글 페이지, 검색 결과 페이지, commons.wikimedia.org/wiki/... 페이지, redirect URL, 썸네일 페이지 URL은 금지한다.
        - URL에 공백, 한글, 괄호, 따옴표가 있으면 반드시 퍼센트 인코딩된 정상 URL로 출력한다.
        - Unsplash/Pexels/Wikimedia/공공 이미지 중 실제 존재하는 직접 이미지 URL만 사용. 가짜 URL 절대 금지.
        - 메뉴 사진을 확실히 찾기 어렵다면 그 메뉴를 쓰지 말고, 사진을 확실히 찾을 수 있는 한국 집밥 메뉴로 바꾼다.
        - Wikimedia를 쓸 때는 upload.wikimedia.org의 실제 파일 URL을 사용한다. commons.wikimedia.org/wiki/... 페이지 URL은 금지한다.
        - Unsplash/Pexels를 쓸 때도 실제 검색으로 확인된 이미지 URL만 쓴다. photo-{실제ID}, {id}, ... 같은 템플릿 URL은 절대 쓰지 않는다.
        - DETAIL CONTENT 안에는 <img> 태그를 넣지 마. 이미지는 AutoWriter가 글 상단 미디어로 따로 표시한다.

        유튜브: 출력하지 마. VIDEO 섹션에는 항상 "없음"만 쓴다.

        {{(existingTitles.Count > 0 ? $"이미 작성된 메뉴 목록 (반드시 제외, 절대 반복 금지):\n{string.Join("\n", existingTitles.TakeLast(120).Select(t => $"- {t}"))}\n" : "")}}
        {{BuildCookingTimelineInstruction(timeline)}}

        ===DETAIL_[새숫자]_TITLE===
        [1983 집밥육아 #새숫자] [아침/점심/저녁] [메뉴명]과 [아기 상태 한 줄]

        ===DETAIL_[새숫자]_DATE===
        1983년 [날짜]

        ===DETAIL_[새숫자]_CONTENT===
        [그날의 집 분위기·계절·가족 동선을 자연스럽게 묘사하는 도입 2~3문장. 헤더 없이.]

        ## 오늘의 아기 밥

        [아기 기록. 수유 또는 이유식 상태를 명확히. 어른 반찬을 직접 먹지 않았음을 명시. 2~3문단.]

        ## 오늘의 가족 집밥

        [가족 식사 분위기와 음식 이야기. 할머니·아버지·어머니 각자의 모습 묘사. 2~3문단.]

        ## 만드는 법

        ### 재료: 어른 2~3인분

        - [재료명] [정확한 수량 g/ml/개]
        - [재료명] [정확한 수량]

        ### 손질하기

        1. [씻는 방법 구체적으로]
        2. [몇 cm 크기로 자르는지]
        3. [초보자가 실수하기 쉬운 부분]

        ### [조리 방법에 맞는 제목 (볶기/끓이기/부치기 등)]

        1. [팬/냄비 예열 시간과 불 세기]
        2. [재료 넣는 순서, 불 세기, 시간]
        3. [익었는지 확인하는 눈에 보이는 기준]
        4. [실패 시 복구 방법]

        [간이 짤 때/싱거울 때/탈 것 같을 때 복구 방법을 자연스럽게 이어 쓴다. 헤더 없이 1~2문장.]

        ## 별점

        [★ 4~5개]

        ## 파워블로그 한줄평

        [한 문장. 그날의 분위기가 담긴 문장.]

        ===IMAGES===
        https://...
        https://...
        https://...

        ===VIDEO===
        없음
        """;

    /// <summary>
    /// \if KO
    /// <para>Detect Default Root 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the detect default root operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Detect Default Root 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the detect default root operation.</para>
    /// \endif
    /// </returns>
    private static string DetectDefaultRoot()
    {
        foreach (var c in new[]
        {
            @"D:\Work\Dreamine.MVVM.FullKit\20_SOURCES\000. Project\010. App\Families.Web\bin\Release\net8.0-windows\publish\App_Data",
            @"D:\Work\Dreamine.MVVM.FullKit\20_SOURCES\000. Project\010. App\Families.Web\bin\Debug\net8.0-windows\App_Data",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Families.Web\bin\Release\net8.0-windows\publish\App_Data"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Families.Web\bin\Debug\net8.0-windows\App_Data"),
        })
        {
            try { var f = Path.GetFullPath(c); if (Directory.Exists(Path.Combine(f, "Family"))) return f; }
            catch { }
        }
        return "";
    }
}
