using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamiliesAutoWriter.Models;
using FamiliesAutoWriter.Services;
using Microsoft.Win32;

namespace FamiliesAutoWriter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PostWriterService _writer = new();
    private readonly PromptHistoryService _history = new();

    // ── 브라우저 ──────────────────────────────────────────────────
    [ObservableProperty] private string _appDataRoot = DetectDefaultRoot();
    [ObservableProperty] private string _browserUrl = "https://claude.ai/new";
    [ObservableProperty] private string _selectedSlug = "";
    public ObservableCollection<string> Slugs { get; } = [];
    public Func<string, Task<string?>>? ExecuteScriptAsync { get; set; }

    // ── 앨범 ──────────────────────────────────────────────────────
    public ObservableCollection<AlbumInfo> Albums { get; } = [];
    [ObservableProperty] private AlbumInfo? _selectedAlbum;
    [ObservableProperty] private bool _albumRotationEnabled = false;
    private int _albumIndex = 0;

    // ── 자동화 루프 ───────────────────────────────────────────────
    // 상태: Idle → Sending → WaitingAI → Extracting → Saving → Idle
    [ObservableProperty] private bool _loopEnabled = false;
    [ObservableProperty] private int _loopIntervalMinutes = 10;
    [ObservableProperty] private int _aiWaitSeconds = 30;      // AI 응답 대기 시간
    [ObservableProperty] private string _loopStatus = "⏸ 자동 루프 꺼짐";
    [ObservableProperty] private string _loopCountdown = "";
    public string LoopLabel => LoopEnabled ? "▶ 루프 실행 중" : "⏸ 자동 루프 꺼짐";

    public int[] IntervalOptions { get; } = [5, 10, 15, 20, 30, 60];
    public int[] WaitOptions { get; } = [15, 20, 30, 45, 60, 90];

    private readonly DispatcherTimer _loopTimer = new();
    private readonly DispatcherTimer _countdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _nextLoopAt = DateTime.MaxValue;
    private CancellationTokenSource? _loopCts;

    // ── 프롬프트 ─────────────────────────────────────────────────
    [ObservableProperty] private string _promptText = BuildDefaultPrompt();
    [ObservableProperty] private string _promptStatus = "";

    // ── 포스트 편집 ───────────────────────────────────────────────
    [ObservableProperty] private string _postTitle = "";
    [ObservableProperty] private string _postContent = "";
    [ObservableProperty] private bool _isPinned = false;
    [ObservableProperty] private MediaPosition _mediaPosition = MediaPosition.Bottom;
    [ObservableProperty] private string _statusMessage = "";
    public ObservableCollection<string> PendingPhotos { get; } = [];

    // ── 초기화 ───────────────────────────────────────────────────
    public MainViewModel()
    {
        RefreshSlugs();
        _loopTimer.Tick += OnLoopTick;
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();
    }

    partial void OnAppDataRootChanged(string value) { _writer.AppDataRoot = value; RefreshSlugs(); }
    partial void OnSelectedSlugChanged(string value) => RefreshAlbums();
    partial void OnLoopEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(LoopLabel));
        if (value) StartLoop();
        else StopLoop();
    }
    partial void OnLoopIntervalMinutesChanged(int value) { if (LoopEnabled) StartLoop(); }

    // ── 슬러그/앨범 ───────────────────────────────────────────────
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
        _albumIndex = 0;
        SelectedAlbum = Albums.FirstOrDefault();
    }

    [RelayCommand] private void RefreshSlugList() { RefreshSlugs(); RefreshAlbums(); }

    // ── 자동화 루프 ───────────────────────────────────────────────
    private void StartLoop()
    {
        _loopCts?.Cancel();
        _loopTimer.Stop();
        _loopTimer.Interval = TimeSpan.FromMinutes(LoopIntervalMinutes);
        _nextLoopAt = DateTime.Now.AddMinutes(LoopIntervalMinutes);
        _loopTimer.Start();
        LoopStatus = $"✅ 루프 시작 — {LoopIntervalMinutes}분 간격 | AI 대기 {AiWaitSeconds}초 | 앨범 로테이션 {(AlbumRotationEnabled ? "ON" : "OFF")}";
    }

    private void StopLoop()
    {
        _loopCts?.Cancel();
        _loopTimer.Stop();
        _nextLoopAt = DateTime.MaxValue;
        LoopCountdown = "";
        LoopStatus = "⏸ 자동 루프 꺼짐";
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        if (_nextLoopAt == DateTime.MaxValue || !LoopEnabled) return;
        var r = _nextLoopAt - DateTime.Now;
        if (r < TimeSpan.Zero) { LoopCountdown = "실행 중..."; return; }
        LoopCountdown = $"다음 실행까지 {(int)r.TotalMinutes:D2}:{r.Seconds:D2}";
    }

    private async void OnLoopTick(object? sender, EventArgs e)
    {
        _nextLoopAt = DateTime.Now.AddMinutes(LoopIntervalMinutes);
        await RunLoopCycleAsync();
    }

    [RelayCommand]
    private async Task RunNowAsync() => await RunLoopCycleAsync();

    private async Task RunLoopCycleAsync()
    {
        _loopCts = new CancellationTokenSource();
        var ct = _loopCts.Token;

        if (string.IsNullOrWhiteSpace(SelectedSlug)) { LoopStatus = "❌ 슬러그를 선택하세요."; return; }
        if (string.IsNullOrWhiteSpace(PromptText)) { LoopStatus = "❌ 프롬프트를 입력하세요."; return; }
        if (ExecuteScriptAsync == null) { LoopStatus = "❌ 브라우저가 준비되지 않았습니다."; return; }

        // 현재 앨범 결정
        var album = SelectedAlbum;
        var albumName = album?.Name ?? "전체 타임라인";

        // 프롬프트에 앨범명 치환
        var prompt = PromptText.Replace("{앨범}", albumName).Replace("{album}", albumName);

        // 중복 체크
        var promptKey = $"{SelectedSlug}:{album?.Id ?? "all"}:{prompt}";
        if (_history.IsDuplicate(promptKey))
        {
            LoopStatus = $"⏭ [{albumName}] 중복 스킵 ({DateTime.Now:HH:mm}) — 다음 앨범으로";
            RotateAlbum();
            return;
        }

        try
        {
            // 1. 프롬프트 전송
            LoopStatus = $"📤 [{albumName}] 프롬프트 전송 중...";
            var injectScript = PromptInjector.BuildInjectScript(prompt);
            await ExecuteScriptAsync(injectScript);
            _history.MarkSent(promptKey);

            // 2. AI 응답 대기
            for (int i = AiWaitSeconds; i > 0; i--)
            {
                if (ct.IsCancellationRequested) return;
                LoopStatus = $"⏳ [{albumName}] AI 응답 대기 중... {i}초";
                await Task.Delay(1000, ct);
            }

            // 3. 응답 추출
            LoopStatus = $"🔍 [{albumName}] 응답 추출 중...";
            var raw = await ExecuteScriptAsync(ResponseExtractor.BuildExtractScript());
            var result = ParseStructuredResponse(raw);

            if (result is null || string.IsNullOrWhiteSpace(result.Content))
            {
                LoopStatus = $"❌ [{albumName}] 응답 추출 실패 — AI가 아직 응답 중일 수 있습니다. 대기 시간을 늘려보세요.";
                return;
            }

            // 4. 이미지 URL 실제 로드 검증
            List<string> validPhotos = [];
            if (result.Photos.Count > 0)
            {
                LoopStatus = $"🔍 [{albumName}] 이미지 URL 검증 중...";
                validPhotos = await ValidateImageUrlsAsync(result.Photos, msg => LoopStatus = msg);
            }

            // 5. 저장
            var post = new PostEntry
            {
                Title = !string.IsNullOrWhiteSpace(result.Title)
                    ? result.Title : $"[{albumName}] {DateTime.Now:MM/dd HH:mm}",
                Content = result.Content,
                AlbumId = album?.Id ?? "",
                PostedAt = DateTime.Now,
                MediaPosition = MediaPosition,
                PhotoFileNames = validPhotos,
                VideoFileNames = result.Videos,
            };

            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            LoopStatus = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm}) — {result.Content.Length}자 📷{validPhotos.Count}/{result.Photos.Count} 🎬{result.Videos.Count}";

            // 5. 앨범 로테이션
            if (AlbumRotationEnabled) RotateAlbum();
        }
        catch (OperationCanceledException) { LoopStatus = "⏹ 루프 취소됨"; }
        catch (Exception ex) { LoopStatus = $"❌ 오류: {ex.Message}"; }
    }

    private void RotateAlbum()
    {
        if (Albums.Count == 0) return;
        _albumIndex = (_albumIndex + 1) % Albums.Count;
        SelectedAlbum = Albums[_albumIndex];
        LoopStatus += $" → 다음: {SelectedAlbum.Name}";
    }

    // ── 프롬프트 수동 전송 ────────────────────────────────────────
    [RelayCommand]
    private async Task SendPromptNowAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText)) { PromptStatus = "❌ 프롬프트를 입력하세요."; return; }
        if (ExecuteScriptAsync == null) { PromptStatus = "❌ 브라우저 준비 안 됨"; return; }
        var albumName = SelectedAlbum?.Name ?? "전체";
        var prompt = PromptText.Replace("{앨범}", albumName).Replace("{album}", albumName);
        var script = PromptInjector.BuildInjectScript(prompt);
        var result = await ExecuteScriptAsync(script);
        PromptStatus = $"✅ 전송 완료 ({DateTime.Now:HH:mm}) — {result?.Trim('"') ?? "ok"}";
    }

    // ── AI 응답 추출 → 바로 저장 ─────────────────────────────────
    [RelayCommand]
    private async Task ExtractAndSaveAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) { StatusMessage = "❌ 가족 슬러그를 선택하세요."; return; }
        if (ExecuteScriptAsync == null) { StatusMessage = "❌ 브라우저 준비 안 됨"; return; }

        StatusMessage = "⏳ AI 응답 추출 중...";
        var raw = await ExecuteScriptAsync(ResponseExtractor.BuildExtractScript());
        var result = ParseStructuredResponse(raw);

        if (result is null || string.IsNullOrWhiteSpace(result.Content))
        {
            StatusMessage = "❌ AI 응답을 찾을 수 없습니다. AI 응답이 완료됐는지 확인하세요.";
            return;
        }

        var albumName = SelectedAlbum?.Name ?? "전체";

        // 이미지 검증
        List<string> validPhotos = [];
        if (result.Photos.Count > 0)
        {
            StatusMessage = "🔍 이미지 URL 검증 중...";
            validPhotos = await ValidateImageUrlsAsync(result.Photos, msg => StatusMessage = msg);
        }

        var autoTitle = !string.IsNullOrWhiteSpace(result.Title) ? result.Title
            : !string.IsNullOrWhiteSpace(PostTitle) ? PostTitle.Trim()
            : $"[{albumName}] {DateTime.Now:MM/dd HH:mm}";
        var post = new PostEntry
        {
            Title = autoTitle,
            Content = result.Content,
            AlbumId = SelectedAlbum?.Id ?? "",
            IsPinned = IsPinned,
            MediaPosition = MediaPosition,
            PostedAt = DateTime.Now,
            PhotoFileNames = validPhotos,
            VideoFileNames = result.Videos,
        };

        try
        {
            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            StatusMessage = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm}) 📷{result.Photos.Count} 🎬{result.Videos.Count}";
            PostTitle = "";
            PendingPhotos.Clear();
        }
        catch (Exception ex) { StatusMessage = $"❌ 저장 오류: {ex.Message}"; }
    }

    // ── 포스트 직접 저장 ─────────────────────────────────────────
    [RelayCommand]
    private async Task SavePostAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) { StatusMessage = "❌ 슬러그 선택"; return; }
        if (string.IsNullOrWhiteSpace(PostTitle) && string.IsNullOrWhiteSpace(PostContent))
        { StatusMessage = "❌ 제목 또는 내용을 입력하세요."; return; }

        var post = new PostEntry
        {
            Title = PostTitle.Trim(),
            Content = PostContent.Trim(),
            AlbumId = SelectedAlbum?.Id ?? "",
            IsPinned = IsPinned,
            MediaPosition = MediaPosition,
            PostedAt = DateTime.Now,
        };

        try
        {
            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            StatusMessage = $"✅ 저장 완료! ({DateTime.Now:HH:mm})";
            ClearForm();
        }
        catch (Exception ex) { StatusMessage = $"❌ 오류: {ex.Message}"; }
    }

    // ── 기타 커맨드 ──────────────────────────────────────────────
    [RelayCommand] private void ClearPromptHistory() { _history.Clear(); PromptStatus = "🗑 히스토리 초기화 완료"; }
    [RelayCommand] private void BrowseAppData()
    {
        var dlg = new OpenFolderDialog { Title = "Families.Web App_Data 폴더 선택" };
        if (dlg.ShowDialog() == true) AppDataRoot = dlg.FolderName;
    }
    [RelayCommand] private void AddPhotos()
    {
        var dlg = new OpenFileDialog { Multiselect = true, Filter = "이미지|*.jpg;*.jpeg;*.png;*.gif;*.webp|모든 파일|*.*" };
        if (dlg.ShowDialog() == true)
            foreach (var f in dlg.FileNames)
                if (!PendingPhotos.Contains(f)) PendingPhotos.Add(f);
    }
    [RelayCommand] private void RemovePhoto(string path) => PendingPhotos.Remove(path);
    [RelayCommand] private void ClearForm()
    {
        PostTitle = ""; PostContent = ""; IsPinned = false;
        MediaPosition = MediaPosition.Bottom; PendingPhotos.Clear();
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────

    private sealed record AiPostResult(string Title, string Content, List<string> Photos, List<string> Videos);

    // ===SECTION=== 구분자 방식으로 파싱 (JSON보다 훨씬 안정적)
    private static AiPostResult? ParseStructuredResponse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // ResponseExtractor 가 { source, text } 로 감쌈 → text 꺼내기
        string text = raw;
        try
        {
            var unescaped = JsonSerializer.Deserialize<string>(raw) ?? raw;
            var outer = JsonDocument.Parse(unescaped);
            if (outer.RootElement.TryGetProperty("text", out var tp))
                text = tp.GetString() ?? unescaped;
        }
        catch { text = raw.Trim('"').Replace("\\n", "\n").Replace("\\\"", "\""); }

        // ===SECTION=== 방식 파싱
        var result = ParseSectioned(text);
        if (result != null) return result;

        // fallback: 텍스트 전체를 content 로 (JSON 포함 어떤 형태든 그냥 받음)
        return string.IsNullOrWhiteSpace(text) ? null : new AiPostResult("", text, [], []);
    }

    private static AiPostResult? ParseSectioned(string text)
    {
        // ===TITLE===, ===CONTENT===, ===IMAGES===, ===VIDEO=== 구분자로 섹션 분리
        string GetSection(string tag)
        {
            var marker = $"==={tag}===";
            var s = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (s < 0) return "";
            s += marker.Length;
            // 다음 ===...=== 까지
            var nextMarker = text.IndexOf("===", s);
            var end = nextMarker > s ? nextMarker : text.Length;
            return text[s..end].Trim();
        }

        var title   = GetSection("TITLE");
        var content = GetSection("CONTENT");
        var imgBlock= GetSection("IMAGES");
        var vidBlock= GetSection("VIDEO");

        if (string.IsNullOrWhiteSpace(content)) return null;

        var photos = imgBlock
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Trim())
            .ToList();

        var videos = vidBlock
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Trim())
            .ToList();

        return new AiPostResult(title, content, photos, videos);
    }

    // 이미지 URL 실제 로드 여부 검증 (저작권 없는 이미지, 직접 링크만 허용)
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    private static async Task<List<string>> ValidateImageUrlsAsync(List<string> urls, Action<string>? onStatus = null)
    {
        var valid = new List<string>();
        foreach (var url in urls)
        {
            try
            {
                onStatus?.Invoke($"🔍 이미지 확인 중: {url[..Math.Min(50, url.Length)]}...");
                var req = new HttpRequestMessage(HttpMethod.Head, url);
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0");
                var res = await _http.SendAsync(req);
                if (res.IsSuccessStatusCode &&
                    res.Content.Headers.ContentType?.MediaType?.StartsWith("image/") == true)
                {
                    valid.Add(url);
                    onStatus?.Invoke($"✅ 이미지 OK: {url[..Math.Min(50, url.Length)]}");
                }
                else
                {
                    onStatus?.Invoke($"❌ 이미지 불량 ({(int)res.StatusCode}): 제외");
                }
            }
            catch
            {
                onStatus?.Invoke($"❌ 이미지 접속 실패: 제외");
            }
        }
        return valid;
    }

    private static string BuildDefaultPrompt() =>
        """
        {앨범} 주제로 한국 가족 여행 블로그 포스트 1개를 작성해줘.
        저작권 없는 무료 이미지(Unsplash, Pexels, Pixabay)의 직링크 URL 3개와
        유튜브 영상 URL 1개도 찾아줘.

        아래 형식 그대로 출력해줘 (===섹션=== 구분자 포함, 다른 설명 없이):

        ===TITLE===
        [장소/활동명], [실용적인 팁 또는 핵심 주의사항]을 한 문장으로

        예시 제목 스타일:
        - 수원화성 주말 반나절 산책, 행궁과 성곽길은 오전에 나누어 보는 게 편합니다
        - 서울어린이대공원 아이와 반나절 코스, 무료 입장이라도 주차 시간은 먼저 봐야 합니다
        - 순천만국가정원 아이와 걷는 1박 2일 가족여행, 정원은 오전에 보는 게 편합니다

        ===CONTENT===
        ## 한눈에 보는 포인트
        이 장소/여행의 핵심 특징 2~3줄 요약

        ## [소제목1 - 장소 특성에 맞게]
        구체적인 내용 (실제 가격, 시간, 주소, 팁 포함)...

        <img src="[IMAGES에서 첫 번째 URL 그대로]" style="width:100%;border-radius:8px;margin:12px 0" />

        ## [소제목2]
        내용...

        <img src="[IMAGES에서 두 번째 URL 그대로]" style="width:100%;border-radius:8px;margin:12px 0" />

        ## [소제목3]
        내용...

        <img src="[IMAGES에서 세 번째 URL 그대로]" style="width:100%;border-radius:8px;margin:12px 0" />

        ## 여행 경비 예상
        | 항목 | 예상 비용 | 메모 |
        |------|----------|------|
        | 입장료 | 0원~얼마 | 내용 |
        | 식사 | 얼마 | 내용 |
        | 교통 | 얼마 | 내용 |

        ## 주차/화장실/유모차 포인트
        실용 정보...

        ## 이런 가족에게 추천
        어떤 연령대, 어떤 성향의 가족에게 맞는지...

        > 💡 팁: 방문 전 꼭 알아야 할 핵심 한 줄

        ===IMAGES===
        https://images.unsplash.com/photo-실제ID?w=800
        https://images.pexels.com/photos/실제ID/pexels-photo-실제ID.jpeg
        https://cdn.pixabay.com/photo/실제경로.jpg

        ===VIDEO===
        https://www.youtube.com/watch?v=실제영상ID

        조건:
        - TITLE: "[장소], [팁]" 형태의 구체적이고 실용적인 제목
        - CONTENT: 마크다운 형식, 이미지는 각 소제목 바로 아래에 삽입
          IMAGES의 URL을 순서대로 <img src=""> 태그에 넣을 것
        - IMAGES: Unsplash/Pexels/Pixabay 직링크 3개, 실제 존재하는 URL만
        - VIDEO: {앨범} 관련 유튜브 1개
        - 지금 바로 위 형식대로만 출력
        """;

    private static string DetectDefaultRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\Families.Web\App_Data"),
            @"D:\Work\Dreamine.MVVM.FullKit\20_SOURCES\000. Project\010. App\Families.Web\App_Data",
        };
        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (Directory.Exists(full)) return full;
        }
        return "";
    }
}
