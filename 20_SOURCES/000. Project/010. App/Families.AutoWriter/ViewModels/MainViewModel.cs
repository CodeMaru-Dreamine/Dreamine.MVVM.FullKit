using System.Collections.ObjectModel;
using System.IO;
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
    private readonly DispatcherTimer _autoTimer = new();

    // ── 브라우저/설정 ─────────────────────────────────────────────
    [ObservableProperty] private string _appDataRoot = DetectDefaultRoot();
    [ObservableProperty] private string _browserUrl = "https://claude.ai/new";
    [ObservableProperty] private string _selectedSlug = "";
    public ObservableCollection<string> Slugs { get; } = [];

    // ── 프롬프트 자동 전송 ────────────────────────────────────────
    [ObservableProperty] private string _promptText = "";
    [ObservableProperty] private bool _autoSendEnabled = false;
    public string AutoSendLabel => AutoSendEnabled ? "▶ 자동 전송 ON" : "⏸ 자동 꺼짐";
    [ObservableProperty] private int _autoIntervalMinutes = 30;
    [ObservableProperty] private string _promptStatus = "";
    [ObservableProperty] private string _nextSendIn = "";

    public int[] IntervalOptions { get; } = [5, 10, 15, 30, 60, 120];

    // 자동전송 남은 시간 카운트다운
    private DateTime _nextSendAt = DateTime.MaxValue;
    private readonly DispatcherTimer _countdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    // ── 포스트 편집 ───────────────────────────────────────────────
    [ObservableProperty] private string _postTitle = "";
    [ObservableProperty] private string _postContent = "";
    [ObservableProperty] private string _albumId = "";
    [ObservableProperty] private bool _isPinned = false;
    [ObservableProperty] private MediaPosition _mediaPosition = MediaPosition.Bottom;
    [ObservableProperty] private string _statusMessage = "";
    public ObservableCollection<string> PendingPhotos { get; } = [];

    // 외부에서 WebView2 실행 위임
    public Func<string, Task<string?>>? ExecuteScriptAsync { get; set; }

    // ── 초기화 ───────────────────────────────────────────────────
    public MainViewModel()
    {
        RefreshSlugs();
        _autoTimer.Tick += OnAutoTimerTick;
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();
    }

    partial void OnAppDataRootChanged(string value) { _writer.AppDataRoot = value; RefreshSlugs(); }

    partial void OnAutoSendEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(AutoSendLabel));
        if (value) StartAutoTimer();
        else StopAutoTimer();
    }

    partial void OnAutoIntervalMinutesChanged(int value)
    {
        if (AutoSendEnabled) StartAutoTimer();
    }

    private void StartAutoTimer()
    {
        _autoTimer.Stop();
        _autoTimer.Interval = TimeSpan.FromMinutes(AutoIntervalMinutes);
        _nextSendAt = DateTime.Now.AddMinutes(AutoIntervalMinutes);
        _autoTimer.Start();
        PromptStatus = $"⏱ 자동 전송 ON — {AutoIntervalMinutes}분 간격";
    }

    private void StopAutoTimer()
    {
        _autoTimer.Stop();
        _nextSendAt = DateTime.MaxValue;
        NextSendIn = "";
        PromptStatus = "⏸ 자동 전송 꺼짐";
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        if (_nextSendAt == DateTime.MaxValue || !AutoSendEnabled) return;
        var remaining = _nextSendAt - DateTime.Now;
        if (remaining < TimeSpan.Zero) { NextSendIn = "전송 중..."; return; }
        NextSendIn = $"다음 전송까지 {(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
    }

    private async void OnAutoTimerTick(object? sender, EventArgs e)
    {
        _nextSendAt = DateTime.Now.AddMinutes(AutoIntervalMinutes);
        await SendPromptAsync(isAuto: true);
    }

    private void RefreshSlugs()
    {
        _writer.AppDataRoot = AppDataRoot;
        Slugs.Clear();
        foreach (var s in _writer.GetSlugs()) Slugs.Add(s);
        if (Slugs.Count > 0 && string.IsNullOrEmpty(SelectedSlug))
            SelectedSlug = Slugs[0];
    }

    // ── 프롬프트 전송 ─────────────────────────────────────────────
    [RelayCommand]
    private async Task SendPromptNowAsync() => await SendPromptAsync(isAuto: false);

    private async Task SendPromptAsync(bool isAuto)
    {
        if (string.IsNullOrWhiteSpace(PromptText))
        {
            PromptStatus = "❌ 프롬프트를 입력하세요.";
            return;
        }

        if (_history.IsDuplicate(PromptText))
        {
            PromptStatus = isAuto
                ? $"⏭ 중복 프롬프트 스킵 ({DateTime.Now:HH:mm})"
                : "⚠️ 이미 전송한 프롬프트입니다. 히스토리를 초기화하거나 내용을 변경하세요.";
            return;
        }

        if (ExecuteScriptAsync == null)
        {
            PromptStatus = "❌ 브라우저가 준비되지 않았습니다.";
            return;
        }

        var script = Services.PromptInjector.BuildInjectScript(PromptText);
        var result = await ExecuteScriptAsync(script);
        _history.MarkSent(PromptText);

        PromptStatus = $"✅ 전송 완료 ({DateTime.Now:HH:mm}) — {result?.Trim('"') ?? "ok"}";
    }

    [RelayCommand]
    private void ClearPromptHistory()
    {
        _history.Clear();
        PromptStatus = "🗑 히스토리 초기화 완료. 다음 전송 시 중복 검사 없이 진행합니다.";
    }

    // ── 앱데이터 / 슬러그 ────────────────────────────────────────
    [RelayCommand]
    private void BrowseAppData()
    {
        var dlg = new OpenFolderDialog { Title = "Families.Web App_Data 폴더 선택" };
        if (dlg.ShowDialog() == true) AppDataRoot = dlg.FolderName;
    }

    [RelayCommand]
    private void RefreshSlugList() => RefreshSlugs();

    // ── 사진 ─────────────────────────────────────────────────────
    [RelayCommand]
    private void AddPhotos()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.gif;*.webp|모든 파일|*.*"
        };
        if (dlg.ShowDialog() == true)
            foreach (var f in dlg.FileNames)
                if (!PendingPhotos.Contains(f)) PendingPhotos.Add(f);
    }

    [RelayCommand]
    private void RemovePhoto(string path) => PendingPhotos.Remove(path);

    // ── 포스트 저장 ───────────────────────────────────────────────
    [RelayCommand]
    private async Task SavePostAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedSlug)) { StatusMessage = "❌ 가족 슬러그를 선택하세요."; return; }
        if (string.IsNullOrWhiteSpace(PostTitle) && string.IsNullOrWhiteSpace(PostContent))
        { StatusMessage = "❌ 제목 또는 내용을 입력하세요."; return; }

        var post = new PostEntry
        {
            Title = PostTitle.Trim(),
            Content = PostContent.Trim(),
            AlbumId = AlbumId.Trim(),
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

    [RelayCommand]
    private void ClearForm()
    {
        PostTitle = ""; PostContent = ""; AlbumId = "";
        IsPinned = false; MediaPosition = MediaPosition.Bottom;
        PendingPhotos.Clear();
    }

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
