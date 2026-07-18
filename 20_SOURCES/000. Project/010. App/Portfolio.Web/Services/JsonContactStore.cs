using System.IO;
using System.Text.Json;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Json Contact Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json contact store functionality and related state.</para>
/// \endif
/// </summary>
public class JsonContactStore : IContactStore
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
    /// <para>지정한 설정으로 <see cref="JsonContactStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonContactStore"/> class with the specified settings.</para>
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
    public JsonContactStore(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

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
    private string Dir(string slug) => Path.Combine(_root, slug, "contacts");
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
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;List&lt;ContactMessage&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;ContactMessage&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<List<ContactMessage>> GetAllAsync(string slug)
    {
        var dir = Dir(slug);
        if (!Directory.Exists(dir)) return [];
        var list = new List<ContactMessage>();
        foreach (var f in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(f);
                var msg = JsonSerializer.Deserialize<ContactMessage>(json);
                if (msg != null) list.Add(msg);
            }
            catch { }
        }
        return [.. list.OrderByDescending(m => m.SentAt)];
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
    /// <param name="msg">
    /// \if KO
    /// <para>msg에 사용할 <c>ContactMessage</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ContactMessage</c> value used for msg.</para>
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
    public async Task SaveAsync(string slug, ContactMessage msg)
    {
        Directory.CreateDirectory(Dir(slug));
        var json = JsonSerializer.Serialize(msg, _json);
        await File.WriteAllTextAsync(Path_(slug, msg.Id), json);
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
    /// <param name="msgId">
    /// \if KO
    /// <para>msg Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for msg id.</para>
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
    public Task DeleteAsync(string slug, string msgId)
    {
        var path = Path_(slug, msgId);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>
    /// \if KO
    /// <para>Mark Read Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the mark read async operation.</para>
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
    /// <param name="msgId">
    /// \if KO
    /// <para>msg Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for msg id.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Mark Read Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the mark read async operation.</para>
    /// \endif
    /// </returns>
    public async Task MarkReadAsync(string slug, string msgId)
    {
        var path = Path_(slug, msgId);
        if (!File.Exists(path)) return;
        var json = await File.ReadAllTextAsync(path);
        var msg = JsonSerializer.Deserialize<ContactMessage>(json);
        if (msg is null) return;
        msg.IsRead = true;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(msg, _json));
    }
}
