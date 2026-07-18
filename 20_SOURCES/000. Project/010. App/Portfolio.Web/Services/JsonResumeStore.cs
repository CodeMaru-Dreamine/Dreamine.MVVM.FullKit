using System.IO;
using System.Text.Json;
using PortfolioApp.Models;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>Json Resume Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json resume store functionality and related state.</para>
/// \endif
/// </summary>
public class JsonResumeStore : IResumeStore
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
    /// <para>지정한 설정으로 <see cref="JsonResumeStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonResumeStore"/> class with the specified settings.</para>
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
    public JsonResumeStore(PortfolioOptions opts) => _root = opts.ResolvedDataPath;

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
    /// <returns>
    /// \if KO
    /// <para>Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the path operation.</para>
    /// \endif
    /// </returns>
    private string Path_(string slug) => Path.Combine(_root, slug, "resume.json");

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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ResumeInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ResumeInfo&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<ResumeInfo> GetAsync(string slug)
    {
        var path = Path_(slug);
        if (!File.Exists(path)) return new ResumeInfo();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ResumeInfo>(json) ?? new ResumeInfo();
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
    /// <param name="resume">
    /// \if KO
    /// <para>resume에 사용할 <c>ResumeInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ResumeInfo</c> value used for resume.</para>
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
    public async Task SaveAsync(string slug, ResumeInfo resume)
    {
        var json = JsonSerializer.Serialize(resume, _json);
        await File.WriteAllTextAsync(Path_(slug), json);
    }
}
