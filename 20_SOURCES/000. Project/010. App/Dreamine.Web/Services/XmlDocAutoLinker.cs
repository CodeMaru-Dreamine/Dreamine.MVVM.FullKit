using System.IO;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>wwwroot/xmldocs/{LibraryName}/ 구조를 스캔해서 라이브러리 Name과 일치하는 폴더의 XML 경로를 자동 연결합니다. Directory.Build.targets가 빌드 시 파일을 해당 폴더에 복사합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates xml doc auto linker functionality and related state.</para>
/// \endif
/// </summary>
public class XmlDocAutoLinker
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
    /// <para>지정한 설정으로 <see cref="XmlDocAutoLinker"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="XmlDocAutoLinker"/> class with the specified settings.</para>
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
    public XmlDocAutoLinker(ILibraryStore store)
    {
        _store = store;
        _xmlDocRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "xmldocs");
    }

    /// <summary>
    /// \if KO
    /// <para>Link Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the link async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Link Async 작업에서 생성한 <c>Task&lt;int&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;int&gt;</c> result produced by the link async operation.</para>
    /// \endif
    /// </returns>
    public async Task<int> LinkAsync()
    {
        if (!Directory.Exists(_xmlDocRoot)) return 0;

        // wwwroot/xmldocs/{LibraryName}/{LibraryName}.xml 구조로 탐색
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in Directory.EnumerateDirectories(_xmlDocRoot))
        {
            var libName = Path.GetFileName(dir);
            var xmlPath = Path.Combine(dir, $"{libName}.xml");
            if (File.Exists(xmlPath))
                map[libName] = xmlPath;
        }

        var libs = (await _store.GetAllAsync()).ToList();
        int count = 0;

        foreach (var lib in libs)
        {
            // 이미 설정된 경우 건너뜀
            if (!string.IsNullOrEmpty(lib.XmlDocPath) && File.Exists(lib.XmlDocPath)) continue;

            if (map.TryGetValue(lib.Name, out var path))
            {
                lib.XmlDocPath = path;
                await _store.SaveAsync(lib);
                count++;
            }
        }

        return count;
    }
}
