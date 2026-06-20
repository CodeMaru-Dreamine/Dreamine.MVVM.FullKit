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
                Title = !string.IsNullOrWhiteSpace(result.Title)
                    ? result.Title : $"[{albumName}] {DateTime.Now:MM/dd HH:mm}",
                Content = result.Content,
                AlbumId = album?.Id ?? "",
                PostedAt = DateTime.Now,
                MediaPosition = MediaPosition,
                PhotoFileNames = result.Photos,
                VideoFileNames = result.Videos,
            };

            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            LoopStatus = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm}) — {result.Content.Length}자 📷{result.Photos.Count} 🎬{result.Videos.Count}";

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
            PhotoFileNames = result.Photos,
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

        // ```json ... ``` 또는 { } 블록 추출
        var jsonBlock = ExtractJsonBlock(text);
        if (jsonBlock != null)
        {
            try
            {
                var doc = JsonDocument.Parse(jsonBlock);
                var root = doc.RootElement;

                var title  = GetStr(root, "Title") ?? GetStr(root, "title") ?? "";
                var content= GetStr(root, "Content") ?? GetStr(root, "content") ?? "";
                var photos = GetStrArray(root, "PhotoFileNames");
                var videos = GetStrArray(root, "VideoFileNames");

                if (!string.IsNullOrWhiteSpace(content))
                    return new AiPostResult(title, content, photos, videos);
            }
            catch { }
        }

        // JSON 실패 → 텍스트 전체를 content 로
        return string.IsNullOrWhiteSpace(text) ? null : new AiPostResult("", text, [], []);
    }

    private static string? GetStr(JsonElement el, string key) =>
        el.TryGetProperty(key, out var p) ? p.GetString() : null;

    private static List<string> GetStrArray(JsonElement el, string key)
    {
        var list = new List<string>();
        if (!el.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
        foreach (var item in arr.EnumerateArray())
        {
            var s = item.GetString();
            if (!string.IsNullOrWhiteSpace(s)) list.Add(s!);
        }
        return list;
    }

    private static string? ExtractJsonBlock(string text)
    {
        var start = text.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (start >= 0)
        {
            start = text.IndexOf('\n', start) + 1;
            var end = text.IndexOf("```", start);
            if (end > start) return text[start..end].Trim();
        }
        start = text.IndexOf("```");
        if (start >= 0)
        {
            start = text.IndexOf('\n', start) + 1;
            var end = text.IndexOf("```", start);
            if (end > start) { var c = text[start..end].Trim(); if (c.StartsWith('{')) return c; }
        }
        var brace = text.IndexOf('{');
        if (brace >= 0) { var last = text.LastIndexOf('}'); if (last > brace) return text[brace..(last + 1)]; }
        return null;
    }

    private static string BuildDefaultPrompt() =>
        """
        {앨범} 주제로 한국 가족 여행 블로그 포스트 1개를 작성해줘.
        관련 이미지 URL 3~4개와 유튜브 영상 URL 1개도 함께 찾아서 채워줘.

        **아래 JSON 형식 그대로** 출력해줘 (설명이나 다른 텍스트 없이 JSON만):

        ```json
        {
          "Title": "{앨범} 여행기 — 가족과 함께한 하루",
          "Content": "## 도착\n본문 내용...\n\n<img src=\"https://이미지URL1.jpg\" style=\"width:100%;border-radius:8px;margin:8px 0\" />\n\n## 점심\n내용...\n\n<img src=\"https://이미지URL2.jpg\" style=\"width:100%;border-radius:8px;margin:8px 0\" />\n\n## 오후\n내용...\n\n<iframe width=\"100%\" height=\"315\" src=\"https://www.youtube.com/embed/영상ID\" frameborder=\"0\" allowfullscreen style=\"border-radius:8px;margin:8px 0\"></iframe>\n\n## 마무리\n내용...\n\n> 💡 팁: ...",
          "PhotoFileNames": [
            "https://실제이미지URL1.jpg",
            "https://실제이미지URL2.jpg",
            "https://실제이미지URL3.jpg"
          ],
          "VideoFileNames": [
            "https://www.youtube.com/watch?v=실제영상ID"
          ],
          "IsPinned": false,
          "MediaPosition": 0
        }
        ```

        Content 조건:
        - 마크다운 + HTML 혼합 (## 소제목, **강조**, > 인용)
        - 이미지는 Content 중간중간에 <img> 태그로 자연스럽게 삽입
        - 유튜브는 <iframe> embed URL(youtube.com/embed/ID) 로 삽입
        - 500~800자, 구체적인 장소·식당·활동 포함
        - 마지막 문단은 > 💡 팁: 으로 마무리

        PhotoFileNames: .jpg/.jpeg/.png/.webp 직링크 3~4개
        VideoFileNames: youtube.com/watch?v= 형식 1개

        지금 바로 JSON 블록만 출력해줘.
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
