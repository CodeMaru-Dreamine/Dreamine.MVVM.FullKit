using System.IO;
using Dreamine.MVVM.ViewModels;
using DreamineWeb.Models;
using DreamineWeb.Services;
using Markdig;

namespace DreamineWeb.ViewModels;

/// <summary>
/// \if KO
/// <para>Doc View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates doc view model functionality and related state.</para>
/// \endif
/// </summary>
public class DocViewModel : ViewModelBase
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
    /// <para>xml Doc Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the xml doc root value.</para>
    /// \endif
    /// </summary>
    private readonly string _xmlDocRoot;

    /// <summary>
    /// \if KO
    /// <para>pipeline 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the pipeline value.</para>
    /// \endif
    /// </summary>
    private static readonly MarkdownPipeline _pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>
    /// \if KO
    /// <para>Library 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the library value.</para>
    /// \endif
    /// </summary>
    public LibraryInfo? Library { get; private set; }
    /// <summary>
    /// \if KO
    /// <para>Members 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the members value.</para>
    /// \endif
    /// </summary>
    public List<DocMember> Members { get; private set; } = [];

    /// <summary>
    /// \if KO
    /// <para>Is Korean 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is korean value.</para>
    /// \endif
    /// </summary>
    public bool IsKorean { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>마크다운 → HTML 변환 결과. null이면 README 없음.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the readme html value.</para>
    /// \endif
    /// </summary>
    public string? ReadmeHtml { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Mermaid 다이어그램 원본 텍스트. null이면 다이어그램 없음.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the mermaid diagram value.</para>
    /// \endif
    /// </summary>
    public string? MermaidDiagram { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>Has Diagram 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the has diagram value.</para>
    /// \endif
    /// </summary>
    public bool HasDiagram { get; private set; }

    /// <summary>
    /// \if KO
    /// <para>wwwroot 기준 상대 URL (SVG fallback용)</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the diagram url value.</para>
    /// \endif
    /// </summary>
    public string DiagramUrl { get; private set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Types 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the types value.</para>
    /// \endif
    /// </summary>
    public IEnumerable<DocMember> Types =>
        Members.Where(m => m.Kind == DocMemberKind.Type).OrderBy(m => m.ShortName);

    /// <summary>
    /// \if KO
    /// <para>Members By Type 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the members by type value.</para>
    /// \endif
    /// </summary>
    public IEnumerable<IGrouping<string?, DocMember>> MembersByType =>
        Members.Where(m => m.Kind != DocMemberKind.Type)
               .OrderBy(m => m.TypeName).ThenBy(m => m.Kind).ThenBy(m => m.ShortName)
               .GroupBy(m => m.TypeName);

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="DocViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="DocViewModel"/> class with the specified settings.</para>
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
    public DocViewModel(ILibraryStore store)
    {
        _store = store;
        _xmlDocRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "xmldocs");
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <param name="libraryId">
    /// \if KO
    /// <para>library Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for library id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Refresh Readme 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh readme operation.</para>
    /// \endif
    /// </summary>
    public void RefreshReadme()
    {
        if (Library is not null)
            LoadReadme(Path.Combine(_xmlDocRoot, Library.Name));
    }

    /// <summary>
    /// \if KO
    /// <para>Readme 데이터를 불러옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads readme data.</para>
    /// \endif
    /// </summary>
    /// <param name="docDir">
    /// \if KO
    /// <para>doc Dir에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for doc dir.</para>
    /// \endif
    /// </param>
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
