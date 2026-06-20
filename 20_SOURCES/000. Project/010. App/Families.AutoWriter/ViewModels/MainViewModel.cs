using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamiliesAutoWriter.Models;
using FamiliesAutoWriter.Services;
using Microsoft.Win32;

namespace FamiliesAutoWriter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PostWriterService _writer = new();

    // ── 설정 ──────────────────────────────────────────────────────
    [ObservableProperty] private string _appDataRoot = DetectDefaultRoot();
    [ObservableProperty] private string _browserUrl = "https://claude.ai/new";
    [ObservableProperty] private string _selectedSlug = "";

    public ObservableCollection<string> Slugs { get; } = [];
    public ObservableCollection<string> AiBookmarks { get; } =
    [
        "https://claude.ai/new",
        "https://chatgpt.com/",
        "https://gemini.google.com/app",
    ];

    // ── 포스트 편집 ───────────────────────────────────────────────
    [ObservableProperty] private string _postTitle = "";
    [ObservableProperty] private string _postContent = "";
    [ObservableProperty] private string _albumId = "";
    [ObservableProperty] private bool _isPinned = false;
    [ObservableProperty] private MediaPosition _mediaPosition = MediaPosition.Bottom;
    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<string> PendingPhotos { get; } = [];

    // ── 초기화 ───────────────────────────────────────────────────
    public MainViewModel() => RefreshSlugs();

    partial void OnAppDataRootChanged(string value)
    {
        _writer.AppDataRoot = value;
        RefreshSlugs();
    }

    private void RefreshSlugs()
    {
        _writer.AppDataRoot = AppDataRoot;
        Slugs.Clear();
        foreach (var s in _writer.GetSlugs()) Slugs.Add(s);
        if (Slugs.Count > 0 && string.IsNullOrEmpty(SelectedSlug))
            SelectedSlug = Slugs[0];
    }

    // ── 커맨드 ───────────────────────────────────────────────────
    [RelayCommand]
    private void BrowseAppData()
    {
        var dlg = new OpenFolderDialog { Title = "Families.Web App_Data 폴더 선택" };
        if (dlg.ShowDialog() == true)
            AppDataRoot = dlg.FolderName;
    }

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
            StatusMessage = $"✅ '{post.Title}' 포스트가 저장되었습니다!";
            ClearForm();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 오류: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        PostTitle = "";
        PostContent = "";
        AlbumId = "";
        IsPinned = false;
        MediaPosition = MediaPosition.Bottom;
        PendingPhotos.Clear();
    }

    [RelayCommand]
    private void RefreshSlugList() => RefreshSlugs();

    private static string DetectDefaultRoot()
    {
        // 같은 솔루션 구조에서 Families.Web App_Data를 자동 탐색
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\Families.Web\App_Data"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Families.Web\App_Data"),
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
