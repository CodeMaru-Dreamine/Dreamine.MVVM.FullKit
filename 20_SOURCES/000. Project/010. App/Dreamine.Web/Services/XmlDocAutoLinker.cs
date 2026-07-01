using System.IO;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// wwwroot/xmldocs/{LibraryName}/ 구조를 스캔해서
/// 라이브러리 Name과 일치하는 폴더의 XML 경로를 자동 연결합니다.
/// Directory.Build.targets가 빌드 시 파일을 해당 폴더에 복사합니다.
/// </summary>
public class XmlDocAutoLinker
{
    private readonly ILibraryStore _store;
    private readonly string _xmlDocRoot;

    public XmlDocAutoLinker(ILibraryStore store)
    {
        _store = store;
        _xmlDocRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "xmldocs");
    }

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
