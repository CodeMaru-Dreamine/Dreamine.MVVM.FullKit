using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;

namespace DreamineWeb.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly ILibraryStore _store;
    private readonly DreamineOptions _opts;

    public string SiteTitle => _opts.SiteTitle;
    public string SiteDescription => _opts.SiteDescription;
    public string GitHubOrgUrl => _opts.GitHubOrgUrl;

    public List<LibraryInfo> Libraries { get; private set; } = [];

    public IEnumerable<IGrouping<string, LibraryInfo>> GroupedLibraries =>
        Libraries.Where(x => x.IsVisible)
                 .OrderBy(x => x.SortOrder)
                 .GroupBy(x => x.Category);

    public HomeViewModel(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _opts = opts;
    }

    public async Task LoadAsync()
    {
        Libraries = await _store.GetAllAsync();
    }
}
