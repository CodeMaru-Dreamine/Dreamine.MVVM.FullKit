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
            var text = ParseExtractedText(raw);

            if (string.IsNullOrWhiteSpace(text))
            {
                LoopStatus = $"❌ [{albumName}] 응답 추출 실패 — AI가 아직 응답 중일 수 있습니다. 대기 시간을 늘려보세요.";
                return;
            }

            // 4. JSON 저장
            var post = new PostEntry
            {
                Title = $"[{albumName}] {DateTime.Now:MM/dd HH:mm}",
                Content = text,
                AlbumId = album?.Id ?? "",
                PostedAt = DateTime.Now,
                MediaPosition = MediaPosition,
            };

            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            LoopStatus = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm}) — 총 {text.Length}자";

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
        var text = ParseExtractedText(raw);

        if (string.IsNullOrWhiteSpace(text))
        {
            StatusMessage = "❌ AI 응답을 찾을 수 없습니다. AI 응답이 완료됐는지 확인하세요.";
            return;
        }

        var albumName = SelectedAlbum?.Name ?? "전체";
        var post = new PostEntry
        {
            Title = string.IsNullOrWhiteSpace(PostTitle)
                ? $"[{albumName}] {DateTime.Now:MM/dd HH:mm}" : PostTitle.Trim(),
            Content = text,
            AlbumId = SelectedAlbum?.Id ?? "",
            IsPinned = IsPinned,
            MediaPosition = MediaPosition,
            PostedAt = DateTime.Now,
        };

        try
        {
            await _writer.SavePostAsync(SelectedSlug, post, PendingPhotos);
            StatusMessage = $"✅ [{albumName}] 저장 완료! ({DateTime.Now:HH:mm})";
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
    private static string? ParseExtractedText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            var unescaped = JsonSerializer.Deserialize<string>(raw) ?? raw;
            var doc = JsonDocument.Parse(unescaped);
            return doc.RootElement.GetProperty("text").GetString();
        }
        catch
        {
            return raw.Trim('"').Replace("\\n", "\n").Replace("\\\"", "\"");
        }
    }

    private static string BuildDefaultPrompt() =>
        """
        {앨범} 주제로 가족 여행 블로그 포스트를 한 개 작성해줘.

        조건:
        - 제목 없이 본문만 작성 (제목은 앱에서 자동 설정)
        - 마크다운 형식 (## 소제목, **강조**, - 목록 사용)
        - 3~5 문단, 500~800자
        - 실제 경험처럼 자연스럽게, 구체적인 장소나 상황 포함
        - 마지막에 팁 또는 추천 한 줄로 마무리

        지금 바로 본문만 작성해줘.
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
