using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;

namespace DreamineWeb.ViewModels;

public class AdminViewModel : ViewModelBase
{
    private readonly ILibraryStore _store;
    private readonly DreamineOptions _opts;

    public bool IsAuthenticated { get; private set; }
    public string LoginPassword { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;

    public List<LibraryInfo> Libraries { get; private set; } = [];

    // 편집 중인 라이브러리
    public LibraryInfo? Editing { get; private set; }
    public bool IsEditing => Editing is not null;

    public AdminViewModel(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _opts = opts;
    }

    public Task<bool> LoginAsync()
    {
        if (LoginPassword == _opts.SuperAdminPassword)
        {
            IsAuthenticated = true;
            StatusMessage = string.Empty;
            return Task.FromResult(true);
        }
        StatusMessage = "비밀번호가 올바르지 않습니다.";
        return Task.FromResult(false);
    }

    public async Task LoadAsync()
    {
        Libraries = await _store.GetAllAsync();
        StatusMessage = string.Empty;
    }

    public void StartNew()
    {
        Editing = new LibraryInfo { Id = Guid.NewGuid().ToString("N")[..8] };
    }

    public void StartEdit(LibraryInfo lib)
    {
        Editing = new LibraryInfo
        {
            Id = lib.Id,
            Name = lib.Name,
            Description = lib.Description,
            Category = lib.Category,
            Status = lib.Status,
            Version = lib.Version,
            NuGetId = lib.NuGetId,
            RepoUrl = lib.RepoUrl,
            XmlDocPath = lib.XmlDocPath,
            Tags = [.. lib.Tags],
            SortOrder = lib.SortOrder,
            IsVisible = lib.IsVisible
        };
    }

    public void CancelEdit() => Editing = null;

    public async Task SaveEditAsync()
    {
        if (Editing is null) return;
        await _store.SaveAsync(Editing);
        Editing = null;
        await LoadAsync();
        StatusMessage = "✅ 저장됐습니다.";
    }

    public async Task DeleteAsync(string id)
    {
        await _store.DeleteAsync(id);
        await LoadAsync();
        StatusMessage = "✅ 삭제됐습니다.";
    }

    public void SetEditingTags(string raw)
    {
        if (Editing is null) return;
        Editing.Tags = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
