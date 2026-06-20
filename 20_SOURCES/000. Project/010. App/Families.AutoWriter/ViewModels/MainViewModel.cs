using System.Collections.ObjectModel;
using System.IO;
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

            // 4. JSON 저장
            var post = new PostEntry
            {
                Title = $"[{albumName}] {DateTime.Now:MM/dd HH:mm}",
                Content = result.Content,
                AlbumId = album?.Id ?? "",
                PostedAt = DateTime.Now,
                MediaPosition = MediaPosition,
            };
            // AI가 찾아준 이미지 URL 추가
            foreach (var img in result.Images)
                if (!post.PhotoFileNames.Contains(img)) post.PhotoFileNames.Add(img);
            // AI가 찾아준 유튜브 URL 추가
            if (!string.IsNullOrWhiteSpace(result.Video) && !post.VideoFileNames.Contains(result.Video))
                post.VideoFileNames.Add(result.Video);

            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            var mediaInfo = result.Images.Count > 0 || result.Video != null
                ? $" | 📷{result.Images.Count} 🎬{(result.Video != null ? 1 : 0)}" : "";
            LoopStatus = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm}) — {result.Content.Length}자{mediaInfo}";

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
        var post = new PostEntry
        {
            Title = string.IsNullOrWhiteSpace(PostTitle)
                ? $"[{albumName}] {DateTime.Now:MM/dd HH:mm}" : PostTitle.Trim(),
            Content = result.Content,
            AlbumId = SelectedAlbum?.Id ?? "",
            IsPinned = IsPinned,
            MediaPosition = MediaPosition,
            PostedAt = DateTime.Now,
        };
        foreach (var img in result.Images)
            if (!post.PhotoFileNames.Contains(img)) post.PhotoFileNames.Add(img);
        if (!string.IsNullOrWhiteSpace(result.Video) && !post.VideoFileNames.Contains(result.Video))
            post.VideoFileNames.Add(result.Video);

        try
        {
            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            var mediaInfo = result.Images.Count > 0 || result.Video != null
                ? $" (📷{result.Images.Count} 🎬{(result.Video != null ? 1 : 0)})" : "";
            StatusMessage = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm}){mediaInfo}";
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

    // AI 응답에서 구조화된 데이터 파싱
    private sealed record AiPostResult(string Content, List<string> Images, string? Video);

    private static AiPostResult? ParseStructuredResponse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // ResponseExtractor가 { source, text } 형태로 감쌈 → text 꺼내기
        string text = raw;
        try
        {
            var unescaped = JsonSerializer.Deserialize<string>(raw) ?? raw;
            var outer = JsonDocument.Parse(unescaped);
            if (outer.RootElement.TryGetProperty("text", out var textProp))
                text = textProp.GetString() ?? unescaped;
        }
        catch { text = raw.Trim('"').Replace("\\n", "\n").Replace("\\\"", "\""); }

        // text 안에서 ```json ... ``` 블록 추출
        var jsonBlock = ExtractJsonBlock(text);
        if (jsonBlock != null)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonBlock);
                var root = doc.RootElement;
                var content = root.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                var images = new List<string>();
                if (root.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array)
                    foreach (var img in imgs.EnumerateArray())
                    {
                        var u = img.GetString();
                        if (!string.IsNullOrWhiteSpace(u)) images.Add(u!);
                    }
                string? video = null;
                if (root.TryGetProperty("video", out var vid))
                    video = vid.GetString();
                if (!string.IsNullOrWhiteSpace(content))
                    return new AiPostResult(content, images, video);
            }
            catch { }
        }

        // JSON 파싱 실패 시 텍스트 전체를 content로
        return string.IsNullOrWhiteSpace(text) ? null : new AiPostResult(text, [], null);
    }

    private static string? ExtractJsonBlock(string text)
    {
        // ```json ... ``` 블록 찾기
        var start = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            start = text.IndexOf('\n', start) + 1;
            var end = text.IndexOf("```", start);
            if (end > start) return text[start..end].Trim();
        }
        // ``` ... ``` (언어 없음)
        start = text.IndexOf("```");
        if (start >= 0)
        {
            start = text.IndexOf('\n', start) + 1;
            var end = text.IndexOf("```", start);
            if (end > start)
            {
                var candidate = text[start..end].Trim();
                if (candidate.StartsWith('{')) return candidate;
            }
        }
        // { 로 시작하는 JSON 직접
        var brace = text.IndexOf('{');
        if (brace >= 0)
        {
            var lastBrace = text.LastIndexOf('}');
            if (lastBrace > brace) return text[brace..(lastBrace + 1)];
        }
        return null;
    }

    private static string BuildDefaultPrompt() =>
        """
        {앨범} 주제로 가족 여행 블로그 포스트를 한 개 작성하고, 관련 이미지와 유튜브 영상 URL도 찾아줘.

        반드시 아래 JSON 형식으로만 답변해줘 (다른 설명 없이):

        ```json
        {
          "content": "포스트 본문 (마크다운 형식, 500~800자)",
          "images": [
            "https://실제_이미지_URL_1.jpg",
            "https://실제_이미지_URL_2.jpg",
            "https://실제_이미지_URL_3.jpg"
          ],
          "video": "https://www.youtube.com/watch?v=실제_영상ID"
        }
        ```

        본문 조건:
        - 제목 없이 본문만 작성 (제목은 앱에서 자동 설정)
        - 마크다운 형식 (## 소제목, **강조**, - 목록 사용)
        - 3~5 문단, 실제 경험처럼 자연스럽게
        - 구체적인 장소나 식당, 활동 포함
        - 마지막에 팁 또는 추천 한 줄로 마무리

        이미지 조건:
        - {앨범} 주제와 관련된 실제 존재하는 이미지 URL 3~4개
        - .jpg, .jpeg, .png, .webp 로 끝나는 직접 링크

        영상 조건:
        - {앨범} 주제 관련 유튜브 영상 URL 1개
        - youtube.com/watch?v= 또는 youtu.be/ 형식

        지금 바로 JSON만 출력해줘.
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
