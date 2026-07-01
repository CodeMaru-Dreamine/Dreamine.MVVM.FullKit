using System.IO;
using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;
using Markdig;

namespace DreamineWeb.ViewModels;

public class DocViewModel : ViewModelBase
{
    private readonly ILibraryStore _store;
    private readonly string _xmlDocRoot;

    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public LibraryInfo? Library { get; private set; }
    public List<DocMember> Members { get; private set; } = [];

    public bool IsKorean { get; set; } = true;

    /// <summary>마크다운 → HTML 변환 결과. null이면 README 없음.</summary>
    public string? ReadmeHtml { get; private set; }

    public bool HasDiagram { get; private set; }

    /// <summary>wwwroot 기준 상대 URL (img src로 사용)</summary>
    public string DiagramUrl { get; private set; } = string.Empty;

    public IEnumerable<DocMember> Types =>
        Members.Where(m => m.Kind == DocMemberKind.Type).OrderBy(m => m.ShortName);

    public IEnumerable<IGrouping<string?, DocMember>> MembersByType =>
        Members.Where(m => m.Kind != DocMemberKind.Type)
               .OrderBy(m => m.TypeName).ThenBy(m => m.Kind).ThenBy(m => m.ShortName)
               .GroupBy(m => m.TypeName);

    public DocViewModel(ILibraryStore store)
    {
        _store = store;
        _xmlDocRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "xmldocs");
    }

    public async Task LoadAsync(string libraryId)
    {
        Library = await _store.GetAsync(libraryId);
        Members = [];
        ReadmeHtml = null;
        HasDiagram = false;

        if (Library is null) return;

        var docDir = Path.Combine(_xmlDocRoot, Library.Name);

        // XML API 문서
        var xmlPath = Path.Combine(docDir, $"{Library.Name}.xml");
        if (File.Exists(xmlPath))
            Members = XmlDocParser.Parse(xmlPath);
        else if (!string.IsNullOrEmpty(Library.XmlDocPath) && File.Exists(Library.XmlDocPath))
            Members = XmlDocParser.Parse(Library.XmlDocPath);

        // README
        LoadReadme(docDir);

        // 다이어그램
        var svgPath = Path.Combine(docDir, "diagram.svg");
        if (File.Exists(svgPath))
        {
            HasDiagram = true;
            DiagramUrl = $"/xmldocs/{Library.Name}/diagram.svg";
        }
    }

    public void ToggleLanguage()
    {
        IsKorean = !IsKorean;
        if (Library is not null)
            LoadReadme(Path.Combine(_xmlDocRoot, Library.Name));
    }

    private void LoadReadme(string docDir)
    {
        string? mdPath = null;

        if (IsKorean)
        {
            // 대소문자 두 가지 관례 처리
            var ko1 = Path.Combine(docDir, "README_KO.md");
            var ko2 = Path.Combine(docDir, "README_ko.md");
            if (File.Exists(ko1)) mdPath = ko1;
            else if (File.Exists(ko2)) mdPath = ko2;
        }

        // 한글 없거나 영문 모드
        if (mdPath is null)
        {
            var en = Path.Combine(docDir, "README.md");
            if (File.Exists(en)) mdPath = en;
        }

        ReadmeHtml = mdPath is not null
            ? Markdown.ToHtml(File.ReadAllText(mdPath), _pipeline)
            : null;
    }
}
