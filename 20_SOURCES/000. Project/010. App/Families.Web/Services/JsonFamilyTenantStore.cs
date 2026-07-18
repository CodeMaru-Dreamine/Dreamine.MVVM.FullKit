using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>Json Family Tenant Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json family tenant store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonFamilyTenantStore : IFamilyTenantStore
{
    /// <summary>
    /// \if KO
    /// <para>data Root 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the data root value.</para>
    /// \endif
    /// </summary>
    private readonly string _dataRoot;
    /// <summary>
    /// \if KO
    /// <para>gate 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the gate value.</para>
    /// \endif
    /// </summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// \if KO
    /// <para>json Opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json opts value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonFamilyTenantStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonFamilyTenantStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>FamilyOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public JsonFamilyTenantStore(FamilyOptions opts)
    {
        _dataRoot = opts.ResolvedDataPath;
        Directory.CreateDirectory(_dataRoot);
    }

    /// <summary>
    /// \if KO
    /// <para>Tenant Data Path 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant data path value.</para>
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
    /// <para>Get Tenant Data Path 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the get tenant data path operation.</para>
    /// \endif
    /// </returns>
    public string GetTenantDataPath(string slug) =>
        Path.Combine(_dataRoot, Sanitize(slug));

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
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;FamilyConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;FamilyConfig?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<FamilyConfig?> GetAsync(string slug, CancellationToken ct = default)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<FamilyConfig>(fs, _jsonOpts, ct).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;FamilyConfig&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;FamilyConfig&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<FamilyConfig>> GetAllAsync(CancellationToken ct = default)
    {
        var list = new List<FamilyConfig>();
        if (!Directory.Exists(_dataRoot)) return list;

        foreach (var dir in Directory.GetDirectories(_dataRoot))
        {
            var cfg = Path.Combine(dir, "config.json");
            if (!File.Exists(cfg)) continue;

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var fs = File.OpenRead(cfg);
                var t = await JsonSerializer.DeserializeAsync<FamilyConfig>(fs, _jsonOpts, ct).ConfigureAwait(false);
                if (t != null) list.Add(t);
            }
            finally { _gate.Release(); }
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
    /// <para>config에 사용할 <c>FamilyConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>FamilyConfig</c> value used for config.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
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
    public async Task SaveAsync(FamilyConfig config, CancellationToken ct = default)
    {
        var dir = GetTenantDataPath(config.Slug);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "posts"));
        Directory.CreateDirectory(Path.Combine(dir, "albums"));
        Directory.CreateDirectory(Path.Combine(dir, "reactions"));
        Directory.CreateDirectory(Path.Combine(dir, "media"));

        var tmp = ConfigPath(config.Slug) + ".tmp";
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, config, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, ConfigPath(config.Slug), overwrite: true);
            File.Delete(tmp);
        }
        finally { _gate.Release(); }
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
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
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
    public Task<bool> ExistsAsync(string slug, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(ConfigPath(slug)));

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
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
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
    public Task DeleteAsync(string slug, CancellationToken ct = default)
    {
        var dir = GetTenantDataPath(slug);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
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
    private string ConfigPath(string slug) =>
        Path.Combine(GetTenantDataPath(slug), "config.json");

    /// <summary>
    /// \if KO
    /// <para>Sanitize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sanitize operation.</para>
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
    /// <para>Sanitize 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the sanitize operation.</para>
    /// \endif
    /// </returns>
    private static string Sanitize(string slug) =>
        string.Concat(slug.ToLowerInvariant().Split(Path.GetInvalidFileNameChars()));
}
