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

/// <summary>세션 파싱 모드 — 탭마다 독립 설정</summary>
public enum SessionMode { Travel, Cooking }

/// <summary>실행 간격 옵션 (콤보박스 표시용)</summary>
public sealed record IntervalOption(int Minutes, string Label)
{
    public override string ToString() => Label;
}

/// <summary>AI 대기 시간 옵션 (콤보박스 표시용)</summary>
public sealed record WaitOption(int Seconds, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// 탭 하나에 대응하는 독립 세션 — 슬러그/앨범/프롬프트/루프 상태를 각자 보유
/// </summary>
public sealed partial class WriterSession : ViewModelBase
{
    private readonly PostWriterService    _writer  = new();
    private readonly PromptHistoryService _history = new();

    // ── 탭 모드 (Travel / Cooking) ─────────────────────────────
    public SessionMode Mode { get; set; } = SessionMode.Travel;

    // ── 브라우저 위임 (코드비하인드에서 주입) ───────────────────
    public Func<string, Task<string?>>? ExecuteScriptAsync { get; set; }
    public Action<string>?              NavigateTab        { get; set; }

    // ── 루프 태스크 ────────────────────────────────────────────
    private readonly System.Windows.Threading.Dispatcher _uiDispatcher;
    private DateTime _nextLoopAt = DateTime.MaxValue;
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private int _cycleCount;
    private int _albumIndex;

    // ── 컬렉션 ─────────────────────────────────────────────────
    public ObservableCollection<string>    Slugs  { get; } = [];
    public ObservableCollection<AlbumInfo> Albums { get; } = [];

    // ── 콤보박스 상수 옵션 ─────────────────────────────────────
    public static IntervalOption[] IntervalOptions { get; } =
    [
        new(5,   "5분"),    new(10, "10분"), new(15, "15분"),
        new(20,  "20분"),   new(30, "30분"), new(60, "1시간"),
        new(120, "2시간"),  new(180,"3시간"), new(360,"6시간"),
    ];
    public static WaitOption[] WaitOptions { get; } =
    [
        new(15,  "15초"),  new(20, "20초"),  new(30,  "30초"),
        new(45,  "45초"),  new(60, "1분"),   new(90,  "1분 30초"),
        new(120, "2분"),   new(150,"2분 30초"), new(180,"3분"),
        new(210, "3분 30초"),
    ];
    public static int[] NewChatOptions { get; } = [0, 3, 5, 10, 20, 30];

    // ── [DreamineProperty] — 단순 속성 ──────────────────────────
    [DreamineProperty] private bool          _albumRotationEnabled;
    [DreamineProperty] private int           _newChatEveryN = 5;
    [DreamineProperty] private string        _loopStatus    = "⏸ 루프 꺼짐";
    [DreamineProperty] private string        _loopCountdown = "";
    [DreamineProperty] private MediaPosition _mediaPosition = MediaPosition.Bottom;
    [DreamineProperty] private string        _promptText    = BuildTravelPrompt([]);
    [DreamineProperty] private string        _promptStatus  = "";
    [DreamineProperty] private string        _extractStatus = "";

    // ── 사이드이펙트 있는 속성 — 수동 구현 ──────────────────────
    private string _appDataRoot = DetectDefaultRoot();
    public string AppDataRoot
    {
        get => _appDataRoot;
        set { if (SetProperty(ref _appDataRoot, value)) { _writer.AppDataRoot = value; RefreshSlugs(); } }
    }

    private string _selectedSlug = "";
    public string SelectedSlug
    {
        get => _selectedSlug;
        set { if (SetProperty(ref _selectedSlug, value)) RefreshAlbums(); }
    }

    private AlbumInfo? _selectedAlbum;
    public AlbumInfo? SelectedAlbum
    {
        get => _selectedAlbum;
        set => SetProperty(ref _selectedAlbum, value);
    }

    private bool _loopEnabled;
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
    public string LoopLabel => LoopEnabled ? "▶ 실행 중" : "⏸ 루프 꺼짐";

    private IntervalOption _selectedInterval = null!;
    public IntervalOption SelectedInterval
    {
        get => _selectedInterval;
        set { if (SetProperty(ref _selectedInterval, value) && LoopEnabled) StartLoop(); }
    }

    private WaitOption _aiWaitOption = null!;
    public WaitOption AiWaitOption
    {
        get => _aiWaitOption;
        set => SetProperty(ref _aiWaitOption, value);
    }

    // ── 생성자 ─────────────────────────────────────────────────
    public WriterSession(string? initialPrompt = null)
    {
        _uiDispatcher     = System.Windows.Application.Current.Dispatcher;
        _selectedInterval = IntervalOptions[1]; // 10분
        _aiWaitOption     = WaitOptions[2];     // 30초
        if (initialPrompt != null) _promptText = initialPrompt;
        RefreshSlugs();
    }

    // ── 커맨드 ─────────────────────────────────────────────────
    [DreamineCommand] private void RefreshSlugList() { RefreshSlugs(); RefreshAlbums(); }
    [DreamineCommand] private void RunNow()          => _ = RunLoopCycleAsync(CancellationToken.None);
    [DreamineCommand] private void SendPromptNow()   => _ = SendPromptNowAsync();
    [DreamineCommand] private void ExtractAndSave()  => _ = ExtractAndSaveAsync();
    [DreamineCommand] private void ClearPromptHistory() { _history.Clear(); PromptStatus = "🗑 초기화"; }
    [DreamineCommand] private void BrowseAppData()
    {
        var dlg = new OpenFolderDialog { Title = "Families.Web App_Data 폴더 선택" };
        if (dlg.ShowDialog() == true) AppDataRoot = dlg.FolderName;
    }

    // ── 슬러그 / 앨범 ─────────────────────────────────────────
    private void RefreshSlugs()
    {
        _writer.AppDataRoot = AppDataRoot;
        Slugs.Clear();
        foreach (var s in _writer.GetSlugs()) Slugs.Add(s);
        if (Slugs.Count > 0 && string.IsNullOrEmpty(SelectedSlug))
            SelectedSlug = Slugs[0];
    }

    private void RefreshAlbums()
    {
        Albums.Clear();
        if (string.IsNullOrWhiteSpace(SelectedSlug)) return;
        foreach (var a in _writer.GetAlbums(SelectedSlug)) Albums.Add(a);
        _albumIndex   = 0;
        SelectedAlbum = Albums.Count > 0 ? Albums[0] : null;
        RefreshPromptWithExisting();
    }

    private void RefreshPromptWithExisting()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) return;
        var existing = _writer.GetExistingTitles(SelectedSlug);
        PromptText = Mode == SessionMode.Cooking
            ? BuildCookingPrompt(existing, _writer.GetCookingTimelineState(SelectedSlug))
            : BuildTravelPrompt(existing);
    }

    // ── 루프 제어 ─────────────────────────────────────────────
    private int IntervalMinutes => SelectedInterval?.Minutes ?? 10;

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

    private void StopLoop()
    {
        _loopCts?.Cancel();
        _loopCts?.Dispose();
        _loopCts = null;
        _nextLoopAt = DateTime.MaxValue;
        LoopCountdown = "";
        LoopStatus = "⏸ 루프 꺼짐";
    }

    public void StopAutomation()
    {
        if (LoopEnabled)
            LoopEnabled = false;
        else
            StopLoop();
    }

    // 백그라운드 루프 워커 — 각 탭이 독립적으로 돌아감
    private async Task LoopWorkerAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // 카운트다운
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

                if (ct.IsCancellationRequested) break;

                await RunLoopCycleAsync(ct).ConfigureAwait(false);

                if (!ct.IsCancellationRequested)
                    _nextLoopAt = DateTime.Now.AddMinutes(IntervalMinutes);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            await _uiDispatcher.InvokeAsync(() => LoopCountdown = "");
        }
    }

    // ── 루프 사이클 ───────────────────────────────────────────
    private async Task RunLoopCycleAsync(CancellationToken ct)
    {
        // 상태 읽기는 UI 스레드에서
        var (slug, album, prompt, execFn) = await _uiDispatcher.InvokeAsync(() =>
            (SelectedSlug, SelectedAlbum, PromptText, ExecuteScriptAsync));

        if (string.IsNullOrWhiteSpace(slug))   { await SetStatus("❌ 슬러그를 선택하세요."); return; }
        if (string.IsNullOrWhiteSpace(prompt)) { await SetStatus("❌ 프롬프트를 입력하세요."); return; }
        if (execFn == null)                    { await SetStatus("❌ 브라우저 준비 안 됨"); return; }

        var albumName = album?.Name ?? "전체 타임라인";
        var filled    = prompt.Replace("{앨범}", albumName).Replace("{album}", albumName);
        var key       = $"{slug}:{album?.Id ?? "all"}:{filled}";

        if (_history.IsDuplicate(key))
        {
            await SetStatus("⏭ 중복 스킵");
            await _uiDispatcher.InvokeAsync(() => RotateAlbum());
            return;
        }

        try
        {
            await SetStatus($"📤 [{albumName}] 전송 중...");
            await await _uiDispatcher.InvokeAsync(() => execFn(PromptInjector.BuildInjectScript(filled)));
            _history.MarkSent(key);

            var waitSec = await _uiDispatcher.InvokeAsync(() => AiWaitOption?.Seconds ?? 30);
            for (int i = waitSec; i > 0; i--)
            {
                if (ct.IsCancellationRequested) return;
                await SetStatus($"⏳ [{albumName}] {i}초...");
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }

            await SetStatus($"🔍 [{albumName}] 추출 중...");
            var raw = await await _uiDispatcher.InvokeAsync(() => execFn(ResponseExtractor.BuildExtractScript()));

            var mode       = await _uiDispatcher.InvokeAsync(() => Mode);
            var mediaPos   = await _uiDispatcher.InvokeAsync(() => MediaPosition);
            var albumId    = album?.Id ?? "";
            var newChatN   = await _uiDispatcher.InvokeAsync(() => NewChatEveryN);
            var rotEnabled = await _uiDispatcher.InvokeAsync(() => AlbumRotationEnabled);

            if (mode == SessionMode.Cooking)
            {
                var posts = await ParseCookingResponseAsync(raw);
                if (posts.Count == 0) { await SetStatus("❌ 요리 포맷 추출 실패"); return; }
                foreach (var p in posts) await _writer.SavePostAsync(slug, p, []);
                _cycleCount++;
                await SetStatus($"✅ [{albumName}] #{_cycleCount} {posts.Count}개 저장! ({DateTime.Now:HH:mm})");
            }
            else
            {
                var result = ParseTravelResponse(raw);
                if (result is null || string.IsNullOrWhiteSpace(result.Content))
                { await SetStatus("❌ 추출 실패"); return; }

                List<string> validPhotos = [];
                if (result.Photos.Count > 0)
                {
                    await SetStatus("🔍 이미지 검증 중...");
                    validPhotos = await ValidateImageUrlsAsync(result.Photos, msg => _ = SetStatus(msg));
                }
                var content = NormalizeTravelContent(RemoveInvalidInlineImages(result.Content, validPhotos));

                var post = new PostEntry
                {
                    Title          = !string.IsNullOrWhiteSpace(result.Title) ? result.Title : $"[{albumName}] {DateTime.Now:MM/dd HH:mm}",
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
            }

            if (rotEnabled) await _uiDispatcher.InvokeAsync(() => RotateAlbum());

            if (newChatN > 0 && _cycleCount % newChatN == 0)
            {
                await SetStatus(await _uiDispatcher.InvokeAsync(() => LoopStatus) + " → 새 채팅 이동...");
                await Task.Delay(2000, ct).ConfigureAwait(false);
                await _uiDispatcher.InvokeAsync(() => NavigateTab?.Invoke("__new_chat__"));
            }
        }
        catch (OperationCanceledException) { await SetStatus("⏹ 취소됨"); }
        catch (Exception ex)               { await SetStatus($"❌ {ex.Message}"); }
    }

    private Task SetStatus(string msg) => _uiDispatcher.InvokeAsync(() => LoopStatus = msg).Task;

    private void RotateAlbum()
    {
        if (Albums.Count == 0) return;
        _albumIndex   = (_albumIndex + 1) % Albums.Count;
        SelectedAlbum = Albums[_albumIndex];
    }

    // ── 수동 전송 / 추출 ──────────────────────────────────────
    private async Task SendPromptNowAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText)) { PromptStatus = "❌ 프롬프트를 입력하세요."; return; }
        if (ExecuteScriptAsync == null)            { PromptStatus = "❌ 브라우저 준비 안 됨"; return; }
        var albumName = SelectedAlbum?.Name ?? "전체";
        var prompt    = PromptText.Replace("{앨범}", albumName).Replace("{album}", albumName);
        await ExecuteScriptAsync(PromptInjector.BuildInjectScript(prompt));
        PromptStatus = $"✅ 전송 ({DateTime.Now:HH:mm})";
    }

    private async Task ExtractAndSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) { ExtractStatus = "❌ 슬러그를 선택하세요."; return; }
        if (ExecuteScriptAsync == null)               { ExtractStatus = "❌ 브라우저 준비 안 됨"; return; }

        ExtractStatus = "⏳ AI 응답 추출 중...";
        var raw = await ExecuteScriptAsync(ResponseExtractor.BuildExtractScript());

        try
        {
            if (Mode == SessionMode.Cooking)
            {
                var posts = await ParseCookingResponseAsync(raw);
                if (posts.Count == 0) { ExtractStatus = "❌ 요리 포맷 추출 실패 (===DETAIL_NNN_CONTENT=== 없음)"; return; }
                foreach (var p in posts) await _writer.SavePostAsync(SelectedSlug, p, []);
                ExtractStatus = $"✅ {posts.Count}개 저장! ({DateTime.Now:HH:mm})";
            }
            else
            {
                var result = ParseTravelResponse(raw);
                if (result is null || string.IsNullOrWhiteSpace(result.Content)) { ExtractStatus = "❌ 응답 없음"; return; }

                var albumName = SelectedAlbum?.Name ?? "전체";
                List<string> validPhotos = [];
                if (result.Photos.Count > 0)
                {
                    ExtractStatus = "🔍 이미지 검증...";
                    validPhotos = await ValidateImageUrlsAsync(result.Photos, msg => ExtractStatus = msg);
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
            }
        }
        catch (Exception ex) { ExtractStatus = $"❌ {ex.Message}"; }
    }

    // ── 파싱 / 검증 ───────────────────────────────────────────
    private sealed record AiPostResult(string Title, string Content, List<string> Photos, List<string> Videos);

    // AI 응답 raw JSON → 텍스트 변환
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
    private static AiPostResult? ParseTravelResponse(string? raw)
    {
        var text = UnwrapRaw(raw);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var content = GetSection(text, "CONTENT");
        if (string.IsNullOrWhiteSpace(content))
            return string.IsNullOrWhiteSpace(text) ? null : new AiPostResult("", text, [], []);

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
        var validImages = rawImageUrls.Count > 0
            ? await ValidateImageUrlsAsync(rawImageUrls, null)
            : [];
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

            // 이 포스트에 해당하는 이미지 (순번 기반)
            var postImage = imgIdx < imageUrls.Length ? imageUrls[imgIdx] : null;

            var substitutedSource = SubstituteImages(content, postImage != null ? [postImage] : []);
            var bodyImages = ExtractImageUrls(substitutedSource);
            var substituted = NormalizeCookingContent(substitutedSource);
            imgIdx++;

            posts.Add(new PostEntry
            {
                Title          = string.IsNullOrWhiteSpace(title) ? $"[1983 집밥육아 #{nextNumber}] {mealName} 집밥 기록" : title,
                Content        = substituted,
                AlbumId        = SelectedAlbum?.Id ?? "",
                PostedAt       = postedAt,
                MediaPosition  = MediaPosition,
                PhotoFileNames = MergeUrls(postImage != null ? [postImage] : [], bodyImages),
                VideoFileNames = [],
            });

            nextPostedAt = GetNextCookingSlot(postedAt);
            nextNumber++;
        }

        return posts;
    }

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

    private static string GetCookingMealName(DateTime postedAt) =>
        postedAt.Hour switch
        {
            < 12 => "아침",
            < 18 => "점심",
            _ => "저녁",
        };

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

    private static List<string> ExtractImageUrls(string content) =>
        Regex.Matches(content, @"<img\b[^>]*\bsrc\s*=\s*[""'](?<url>https?://[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase)
            .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups["url"].Value.Trim()))
            .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .ToList();

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
                return allowed.Contains(url) || IsLikelyDirectImageUrl(url) ? match.Value : "";
            },
            RegexOptions.IgnoreCase);
    }

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

    private static bool IsAllowedVideoUrl(string url)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;
        if (!url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) &&
            !url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
            return true;

        // 일반 watch/shorts 링크는 소유자 설정으로 앱 내 iframe 재생이 자주 막힌다.
        return url.Contains("/embed/", StringComparison.OrdinalIgnoreCase);
    }

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

    private static void AddBlankLineIfNeeded(List<string> lines)
    {
        if (lines.Count > 0 && lines[^1].Trim().Length > 0)
            lines.Add("");
    }

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

    private static List<string> SplitTableLine(string line) =>
        line.Contains('\t')
            ? line.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList()
            : Regex.Split(line.Trim(), @"\s{2,}")
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

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

    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

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

    private static async Task<List<string>> ValidateImageUrlsAsync(List<string> urls, Action<string>? onStatus = null)
    {
        var valid = new List<string>();
        foreach (var url in urls)
        {
            if (!IsPlausibleImageUrl(url))
                continue;

            try
            {
                onStatus?.Invoke($"🔍 {url[..Math.Min(60, url.Length)]}");
                if (await IsReachableImageAsync(url, HttpMethod.Head) ||
                    await IsReachableImageAsync(url, HttpMethod.Get))
                {
                    valid.Add(url);
                }
                else if (IsLikelyDirectImageUrl(url))
                {
                    valid.Add(url);
                }
            }
            catch { }
        }
        return valid;
    }

    private static async Task<bool> IsReachableImageAsync(string url, HttpMethod method)
    {
        try
        {
            using var req = new HttpRequestMessage(method, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0");
            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            return res.IsSuccessStatusCode &&
                   res.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch
        {
            return false;
        }
    }

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
        - IMAGES 섹션에 실제 접근 가능한 이미지 URL을 3개 출력한다. "없음" 금지.
        - 각 URL은 http 또는 https로 시작하는 이미지 직접 URL이어야 한다.
        - URL 줄에는 설명, 번호, 마크다운 이미지 문법을 붙이지 말고 URL만 쓴다.
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

    public static string BuildCookingPrompt(IReadOnlyList<string> existingTitles) =>
        BuildCookingPrompt(existingTitles, (null, 0));

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

    public static string BuildCookingPrompt(IReadOnlyList<string> existingTitles, (DateTime? LastPostedAt, int LastNumber) timeline) =>
        $$"""
        {앨범} 주제로 1983년 한국 가족 집밥·육아 기록 블로그 포스트를 1개 작성해줘.
        아래 형식 그대로만 출력해줘. ===섹션=== 구분자는 반드시 유지하고, 다른 설명은 쓰지 마.
        코드블록(```), JSON, 주석, 머리말, 맺음말을 절대 쓰지 마.

        AutoWriter 저장 계약:
        - 이 응답은 Families.Web App_Data/Family/{선택슬러그}/posts/*.json 으로 파싱 저장된다.
        - AutoWriter는 ===IMAGES===의 URL 1개와 ===DETAIL_NNN_TITLE===, ===DETAIL_NNN_DATE===, ===DETAIL_NNN_CONTENT=== 섹션만 실제 포스트로 저장한다.
        - DETAIL 번호 NNN은 숫자만 사용한다. 예: 231, 232, 233.
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

        레시피 작성 기준 (초보자가 실제로 따라 만들 수 있어야 함):
        - 재료는 g, ml, cm 단위로 정확히
        - 불 세기(약불/중불/강불)와 조리 시간(분 단위)을 명시
        - 익었는지 확인하는 기준을 눈으로 보이는 변화로 설명
        - "약간", "적당히", "노릇하게"만 쓰지 말고 반드시 기준 수치를 함께 적는다
        - 간이 맞지 않을 때, 탈 것 같을 때 복구 방법 포함

        이미지 조건:
        - IMAGES 섹션에 메뉴 음식 사진 URL 1개를 출력한다.
        - 각 URL은 http 또는 https로 시작하는 이미지 직접 URL이어야 한다.
        - URL 줄에는 설명, 번호, 마크다운 이미지 문법을 붙이지 말고 URL만 쓴다.
        - 메뉴와 다른 음식 사진 금지. 예: 고등어조림 글에 샐러드/덮밥/피자 사진 금지.
        - 가로 1000px 이상 선명한 음식 사진만 사용한다. 작은 썸네일, 흐릿한 사진, 생성 이미지 느낌의 사진 금지.
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
