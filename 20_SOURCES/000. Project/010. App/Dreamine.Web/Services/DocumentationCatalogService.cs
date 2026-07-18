using DreamineWeb.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text.Json;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>생성된 프로젝트 문서 카탈로그를 웹 루트에서 읽어 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Loads and exposes the generated project documentation catalog from the web root.</para>
/// \endif
/// </summary>
public sealed class DocumentationCatalogService
{
    /// <summary>
    /// \if KO
    /// <para>카탈로그 파일이 위치한 웹 호스팅 환경입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The web hosting environment that contains the catalog file.</para>
    /// \endif
    /// </summary>
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// \if KO
    /// <para>처음 읽은 뒤 재사용하는 프로젝트 문서 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The project documentation list cached after its first load.</para>
    /// \endif
    /// </summary>
    private IReadOnlyList<DocumentationProjectInfo>? _projects;

    /// <summary>
    /// \if KO
    /// <para>문서 카탈로그 서비스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes the documentation catalog service.</para>
    /// \endif
    /// </summary>
    /// <param name="environment">
    /// \if KO
    /// <para>웹 루트 경로를 제공하는 호스팅 환경입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The hosting environment that provides the web-root path.</para>
    /// \endif
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// \if KO
    /// <para><paramref name="environment"/>가 <see langword="null"/>인 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when <paramref name="environment"/> is <see langword="null"/>.</para>
    /// \endif
    /// </exception>
    public DocumentationCatalogService(IWebHostEnvironment environment)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// \if KO
    /// <para>생성된 프로젝트 문서 목록을 읽어 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads and returns the generated project documentation list.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>이름순으로 정렬된 프로젝트 문서 목록입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The project documentation list sorted by name.</para>
    /// \endif
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// \if KO
    /// <para>지식 그래프 게시 스크립트가 아직 카탈로그를 생성하지 않은 경우 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Thrown when the knowledge-graph publishing script has not generated the catalog.</para>
    /// \endif
    /// </exception>
    public IReadOnlyList<DocumentationProjectInfo> GetProjects()
    {
        if (_projects is not null)
            return _projects;

        string catalogPath = Path.Combine(_environment.WebRootPath, "understand", "project-catalog.json");
        if (!File.Exists(catalogPath))
            throw new FileNotFoundException("Project documentation catalog was not generated. Run Publish-UnderstandDashboard.ps1.", catalogPath);

        using FileStream stream = File.OpenRead(catalogPath);
        DocumentationProjectCatalog catalog = JsonSerializer.Deserialize<DocumentationProjectCatalog>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new DocumentationProjectCatalog();
        _projects = catalog.Projects.OrderBy(project => project.Category).ThenBy(project => project.Name).ToArray();
        return _projects;
    }
}
