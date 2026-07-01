using System.IO;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public class XmlDocAutoLinker
{
    private readonly ILibraryStore _store;
    private readonly DreamineOptions _opts;

    public XmlDocAutoLinker(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _opts = opts;
    }

    /// <summary>
    /// ScanRoot 아래 bin 폴더를 재귀 탐색해 라이브러리 이름과 일치하는 .xml 파일을
    /// 찾아 XmlDocPath가 비어있는 라이브러리에 자동으로 연결합니다.
    /// </summary>
    /// <returns>연결된 라이브러리 수</returns>
    public async Task<int> LinkAsync()
    {
        var scanRoot = _opts.XmlDocScanRoot;
        if (string.IsNullOrWhiteSpace(scanRoot) || !Directory.Exists(scanRoot))
            return 0;

        // bin/Debug or bin/Release 아래의 .xml 파일 수집 (라이브러리명 → 경로)
        // 같은 이름이 여러 곳에 있으면 경로 깊이가 얕은 쪽(직접 빌드 산출물) 우선
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var xml in Directory.EnumerateFiles(scanRoot, "*.xml", SearchOption.AllDirectories))
        {
            // bin 폴더 산출물만 대상
            var parts = xml.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!parts.Any(p => p.Equals("bin", StringComparison.OrdinalIgnoreCase))) continue;

            var name = Path.GetFileNameWithoutExtension(xml);
            if (!map.ContainsKey(name))
                map[name] = xml;
            else if (xml.Length < map[name].Length)
                map[name] = xml; // 더 얕은 경로 우선
        }

        var libs = (await _store.GetAllAsync()).ToList(); // 캐시 리스트 복사본으로 순회
        int count = 0;

        foreach (var lib in libs)
        {
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
