using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;

namespace DreamineWeb.ViewModels;

public class DocViewModel : ViewModelBase
{
    private readonly ILibraryStore _store;

    public LibraryInfo? Library { get; private set; }
    public List<DocMember> Members { get; private set; } = [];

    public IEnumerable<DocMember> Types =>
        Members.Where(m => m.Kind == DocMemberKind.Type).OrderBy(m => m.ShortName);

    public IEnumerable<IGrouping<string?, DocMember>> MembersByType =>
        Members.Where(m => m.Kind != DocMemberKind.Type)
               .OrderBy(m => m.TypeName).ThenBy(m => m.Kind).ThenBy(m => m.ShortName)
               .GroupBy(m => m.TypeName);

    public DocViewModel(ILibraryStore store) => _store = store;

    public async Task LoadAsync(string libraryId)
    {
        Library = await _store.GetAsync(libraryId);
        if (Library?.XmlDocPath is { } path)
            Members = XmlDocParser.Parse(path);
        else
            Members = [];
    }
}
