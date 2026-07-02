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

    /// <summary>Mermaid 다이어그램 원본 텍스트. null이면 다이어그램 없음.</summary>
    public string? MermaidDiagram { get; private set; }

    public bool HasDiagram { get; private set; }

    /// <summary>wwwroot 기준 상대 URL (SVG fallback용)</summary>
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
        MermaidDiagram = null;
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

        // Mermaid 다이어그램 (우선)
        var mermaidPath = Path.Combine(docDir, "diagram.mermaid");
        if (File.Exists(mermaidPath))
        {
            MermaidDiagram = File.ReadAllText(mermaidPath).Trim();
            HasDiagram = true;
        }
        else
        {
            // SVG fallback
            var svgPath = Path.Combine(docDir, "diagram.svg");
            if (File.Exists(svgPath))
            {
                HasDiagram = true;
                DiagramUrl = $"/xmldocs/{Library.Name}/diagram.svg";
            }
        }
    }

    public void RefreshReadme()
    {
        if (Library is not null)
            LoadReadme(Path.Combine(_xmlDocRoot, Library.Name));
    }

    private void LoadReadme(string docDir)
    {
        string? mdPath = null;

        if (IsKorean)
        {
            var ko1 = Path.Combine(docDir, "README_KO.md");
            var ko2 = Path.Combine(docDir, "README_ko.md");
            if (File.Exists(ko1)) mdPath = ko1;
            else if (File.Exists(ko2)) mdPath = ko2;
        }

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
