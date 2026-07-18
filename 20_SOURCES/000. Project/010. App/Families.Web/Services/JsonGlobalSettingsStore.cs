using System.IO;
using System.Text.Json;
using FamiliesApp.Models;

namespace FamiliesApp.Services;

/// <summary>
/// \if KO
/// <para>\file JsonGlobalSettingsStore.cs \brief App_Data/global-settings.json 기반 전체 설정 저장소. 메모리 캐시 + 파일 영속화.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json global settings store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonGlobalSettingsStore : IGlobalSettingsStore
{
    /// <summary>
    /// \if KO
    /// <para>path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the path value.</para>
    /// \endif
    /// </summary>
    private readonly string _path;
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
    /// <para>cache 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the cache value.</para>
    /// \endif
    /// </summary>
    private GlobalSettings? _cache;

    /// <summary>
    /// \if KO
    /// <para>json Opts 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the json opts value.</para>
    /// \endif
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonGlobalSettingsStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonGlobalSettingsStore"/> class with the specified settings.</para>
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
    public JsonGlobalSettingsStore(FamilyOptions opts)
    {
        var dataRoot = Path.GetDirectoryName(opts.ResolvedDataPath.TrimEnd(Path.DirectorySeparatorChar))
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data");
        Directory.CreateDirectory(dataRoot);
        _path = Path.Combine(dataRoot, "global-settings.json");
    }

    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;GlobalSettings&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;GlobalSettings&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<GlobalSettings> GetAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_cache is not null) return _cache;

            if (!File.Exists(_path))
            {
                _cache = new GlobalSettings();
                return _cache;
            }

            await using var fs = File.OpenRead(_path);
            _cache = await JsonSerializer.DeserializeAsync<GlobalSettings>(fs, _jsonOpts, ct).ConfigureAwait(false)
                ?? new GlobalSettings();
            return _cache;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="settings">
    /// \if KO
    /// <para>settings에 사용할 <c>GlobalSettings</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>GlobalSettings</c> value used for settings.</para>
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
    public async Task SaveAsync(GlobalSettings settings, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var tmp = _path + ".tmp";
            await using (var fs = File.Create(tmp))
                await JsonSerializer.SerializeAsync(fs, settings, _jsonOpts, ct).ConfigureAwait(false);

            File.Copy(tmp, _path, overwrite: true);
            File.Delete(tmp);
            _cache = settings;
        }
        finally { _gate.Release(); }
    }
}
