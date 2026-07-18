using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;

namespace DreamineWeb.ViewModels;

/// <summary>
/// \if KO
/// <para>Home View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates home view model functionality and related state.</para>
/// \endif
/// </summary>
public class HomeViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>store 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the store value.</para>
    /// \endif
    /// </summary>
    private readonly ILibraryStore _store;
    /// <summary>
    /// \if KO
    /// <para>opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the opts value.</para>
    /// \endif
    /// </summary>
    private readonly DreamineOptions _opts;

    /// <summary>
    /// \if KO
    /// <para>Site Title 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the site title value.</para>
    /// \endif
    /// </summary>
    public string SiteTitle => _opts.SiteTitle;
    /// <summary>
    /// \if KO
    /// <para>Site Description 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the site description value.</para>
    /// \endif
    /// </summary>
    public string SiteDescription => _opts.SiteDescription;
    /// <summary>
    /// \if KO
    /// <para>Git Hub Org Url 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the git hub org url value.</para>
    /// \endif
    /// </summary>
    public string GitHubOrgUrl => _opts.GitHubOrgUrl;

    /// <summary>
    /// \if KO
    /// <para>Libraries 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the libraries value.</para>
    /// \endif
    /// </summary>
    public List<LibraryInfo> Libraries { get; private set; } = [];

    /// <summary>
    /// \if KO
    /// <para>Grouped Libraries 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the grouped libraries value.</para>
    /// \endif
    /// </summary>
    public IEnumerable<IGrouping<string, LibraryInfo>> GroupedLibraries =>
        Libraries.Where(x => x.IsVisible)
                 .OrderBy(x => x.SortOrder)
                 .GroupBy(x => x.Category);

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="HomeViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="HomeViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="store">
    /// \if KO
    /// <para>store에 사용할 <c>ILibraryStore</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ILibraryStore</c> value used for store.</para>
    /// \endif
    /// </param>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public HomeViewModel(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _opts = opts;
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    public async Task LoadAsync()
    {
        Libraries = await _store.GetAllAsync();
    }
}
