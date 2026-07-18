using System.Text.Json;
using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>파일시스템 기반 테넌트 스토어. App_Data/Shops/{slug}/config.json.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json shop tenant store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonShopTenantStore : IShopTenantStore
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
    /// <para>지정한 설정으로 <see cref="JsonShopTenantStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonShopTenantStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>ShopOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public JsonShopTenantStore(ShopOptions opts) => _root = opts.ResolvedDataPath;

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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ShopConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ShopConfig?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ShopConfig?> GetAsync(string slug)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ShopConfig>(stream, _json);
    }

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
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;ShopConfig&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;ShopConfig&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<ShopConfig>> GetAllAsync()
    {
        if (!Directory.Exists(_root)) return Array.Empty<ShopConfig>();
        var list = new List<ShopConfig>();
        foreach (var dir in Directory.EnumerateDirectories(_root))
        {
            var path = Path.Combine(dir, "config.json");
            if (!File.Exists(path)) continue;
            await using var stream = File.OpenRead(path);
            var cfg = await JsonSerializer.DeserializeAsync<ShopConfig>(stream, _json);
            if (cfg != null) list.Add(cfg);
        }
        return list;
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
    /// <para>config에 사용할 <c>ShopConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopConfig</c> value used for config.</para>
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
    public async Task SaveAsync(ShopConfig config)
    {
        var dir = ShopDir(config.Slug);
        Directory.CreateDirectory(dir);
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
        var dir = ShopDir(slug);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>Exists Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the exists async operation.</para>
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
    /// <para>Exists Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the exists async operation.</para>
    /// \endif
    /// </returns>
    public Task<bool> ExistsAsync(string slug) =>
        Task.FromResult(File.Exists(ConfigPath(slug)));

    /// <summary>
    /// \if KO
    /// <para>Shop Dir 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the shop dir operation.</para>
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
    /// <para>Shop Dir 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the shop dir operation.</para>
    /// \endif
    /// </returns>
    private string ShopDir(string slug) => Path.Combine(_root, slug);
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
    private string ConfigPath(string slug) => Path.Combine(ShopDir(slug), "config.json");
}
