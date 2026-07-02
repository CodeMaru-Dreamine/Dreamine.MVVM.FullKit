using System.IO;
using System.Text.RegularExpressions;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public class VersionSyncService
{
    private readonly ILibraryStore _store;
    private readonly string _libRoot;

    private static readonly Regex _versionRx =
        new(@"<Version>\s*([^<\s]+)\s*</Version>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public VersionSyncService(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _libRoot = opts.LibrarySourceRoot;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_libRoot) && Directory.Exists(_libRoot);

    public async Task<int> SyncAsync()
    {
        if (!IsConfigured) return -1;

        // 어셈블리명 → 버전 매핑
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Directory.EnumerateFiles(_libRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(f);
            if (name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)) continue;
            var m = _versionRx.Match(await File.ReadAllTextAsync(f));
            if (m.Success) map[name] = m.Groups[1].Value;
        }

        var libs = (await _store.GetAllAsync()).ToList();
        int count = 0;
        foreach (var lib in libs)
        {
            if (!map.TryGetValue(lib.Name, out var ver)) continue;
            if (lib.Version == ver) continue;
            lib.Version = ver;
            await _store.SaveAsync(lib);
            count++;
        }
        return count;
    }
}
