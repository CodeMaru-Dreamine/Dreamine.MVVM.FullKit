using System.IO;
using System.Text.RegularExpressions;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Version Sync Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates version sync service functionality and related state.</para>
/// \endif
/// </summary>
public class VersionSyncService
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
    /// <para>lib Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the lib root value.</para>
    /// \endif
    /// </summary>
    private readonly string _libRoot;

    /// <summary>
    /// \if KO
    /// <para>version Rx 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the version rx value.</para>
    /// \endif
    /// </summary>
    private static readonly Regex _versionRx =
        new(@"<Version>\s*([^<\s]+)\s*</Version>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="VersionSyncService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="VersionSyncService"/> class with the specified settings.</para>
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
    public VersionSyncService(ILibraryStore store, DreamineOptions opts)
    {
        _store = store;
        _libRoot = opts.LibrarySourceRoot;
    }

    /// <summary>
    /// \if KO
    /// <para>Is Configured 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is configured value.</para>
    /// \endif
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_libRoot) && Directory.Exists(_libRoot);

    /// <summary>
    /// \if KO
    /// <para>Sync Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sync async operation.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Sync Async 작업에서 생성한 <c>Task&lt;int&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;int&gt;</c> result produced by the sync async operation.</para>
    /// \endif
    /// </returns>
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
