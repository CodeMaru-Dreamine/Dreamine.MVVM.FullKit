using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Json Project Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json project store functionality and related state.</para>
/// \endif
/// </summary>
public class JsonProjectStore : IProjectStore
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
    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonProjectStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonProjectStore"/> class with the specified settings.</para>
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
    public JsonProjectStore(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

    /// <summary>
    /// \if KO
    /// <para>Dir 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the dir operation.</para>
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
    /// <para>Dir 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the dir operation.</para>
    /// \endif
    /// </returns>
    private string Dir(string slug) => Path.Combine(_root, slug, "projects");
    /// <summary>
    /// \if KO
    /// <para>Path 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the path operation.</para>
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
    /// <param name="id">
    /// \if KO
    /// <para>id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the path operation.</para>
    /// \endif
    /// </returns>
    private string Path_(string slug, string id) => Path.Combine(Dir(slug), $"{id}.json");

    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
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
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;List&lt;ProjectItem&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;ProjectItem&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<List<ProjectItem>> GetAllAsync(string slug)
    {
        var dir = Dir(slug);
        if (!Directory.Exists(dir)) return [];
        var list = new List<ProjectItem>();
        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(f);
                var item = JsonSerializer.Deserialize<ProjectItem>(json, _json);
                if (item != null) list.Add(item);
            }
            catch { }
        }
        return [.. list.OrderBy(p => p.SortOrder == 0 ? int.MaxValue : p.SortOrder)
                       .ThenByDescending(p => p.Year)];
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ProjectItem?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ProjectItem?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ProjectItem?> GetAsync(string slug, string projectId)
    {
        var path = Path_(slug, projectId);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ProjectItem>(json, _json);
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
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
    /// <param name="item">
    /// \if KO
    /// <para>item에 사용할 <c>ProjectItem</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ProjectItem</c> value used for item.</para>
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
    public async Task SaveAsync(string slug, ProjectItem item)
    {
        Directory.CreateDirectory(Dir(slug));
        var json = JsonSerializer.Serialize(item, _json);
        await File.WriteAllTextAsync(Path_(slug, item.Id), json);
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
    /// <param name="projectId">
    /// \if KO
    /// <para>project Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for project id.</para>
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
    public Task DeleteAsync(string slug, string projectId)
    {
        var path = Path_(slug, projectId);
        if (File.Exists(path)) File.Delete(path);
        var mediaDir = System.IO.Path.Combine(_root, slug, "media", projectId);
        if (Directory.Exists(mediaDir)) Directory.Delete(mediaDir, true);
        return Task.CompletedTask;
    }
}
