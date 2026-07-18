using System.IO;
using System.Text.Json;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Json Portfolio Tenant Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json portfolio tenant store functionality and related state.</para>
/// \endif
/// </summary>
public class JsonPortfolioTenantStore : IPortfolioTenantStore
{
    /// <summary>
    /// \if KO
    /// <para>root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the root value.</para>
    /// \endif
    /// </summary>
    private readonly string _root;
    /// <summary>
    /// \if KO
    /// <para>json 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonPortfolioTenantStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonPortfolioTenantStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>PortfolioOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public JsonPortfolioTenantStore(PortfolioOptions opts)
    {
        _root = opts.ResolvedDataPath;
        Directory.CreateDirectory(_root);
    }

    /// <summary>
    /// \if KO
    /// <para>Config Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the config path operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Config Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the config path operation.</para>
    /// \endif
    /// </returns>
    private string ConfigPath(string slug) => Path.Combine(_root, slug, "config.json");

    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;List&lt;PortfolioConfig&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;PortfolioConfig&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<List<PortfolioConfig>> GetAllAsync()
    {
        var list = new List<PortfolioConfig>();
        if (!Directory.Exists(_root)) return list;
        foreach (var dir in Directory.GetDirectories(_root))
        {
            var cfg = Path.Combine(dir, "config.json");
            if (!File.Exists(cfg)) continue;
            try
            {
                var json = await File.ReadAllTextAsync(cfg);
                var obj = JsonSerializer.Deserialize<PortfolioConfig>(json);
                if (obj != null) list.Add(obj);
            }
            catch { }
        }
        return list;
    }

    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;PortfolioConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;PortfolioConfig?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<PortfolioConfig?> GetAsync(string slug)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<PortfolioConfig>(json);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    public async Task SaveAsync(PortfolioConfig config)
    {
        var dir = Path.Combine(_root, config.Slug);
        Directory.CreateDirectory(Path.Combine(dir, "projects"));
        Directory.CreateDirectory(Path.Combine(dir, "media"));
        Directory.CreateDirectory(Path.Combine(dir, "contacts"));
        var json = JsonSerializer.Serialize(config, _json);
        await File.WriteAllTextAsync(ConfigPath(config.Slug), json);
    }

    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    public Task DeleteAsync(string slug)
    {
        var dir = Path.Combine(_root, slug);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }
}
