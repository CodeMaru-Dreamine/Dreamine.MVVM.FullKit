using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Wedding.Common;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \if KO
/// <para>\file JsonTenantStore.cs \brief App_Data/Wedding/{slug}/config.json 기반 테넌트 설정 저장소.</para>
/// \endif
/// \if EN
/// <para>Encapsulates json tenant store functionality and related state.</para>
/// \endif
/// </summary>
public sealed class JsonTenantStore : ITenantStore
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
        Converters = { new WeddingLayoutModeJsonConverter(), new JsonStringEnumConverter() }
    };

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="JsonTenantStore"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="JsonTenantStore"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>WeddingOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>WeddingOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    public JsonTenantStore(WeddingOptions opts)
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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;TenantConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;TenantConfig?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    public async Task<TenantConfig?> GetAsync(string slug, CancellationToken ct = default)
    {
        var path = ConfigPath(slug);
        if (!File.Exists(path)) return null;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ReadTenantAsync(path, ct).ConfigureAwait(false);
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
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;TenantConfig&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;TenantConfig&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    public async Task<IReadOnlyList<TenantConfig>> GetAllAsync(CancellationToken ct = default)
    {
        var list = new List<TenantConfig>();
        if (!Directory.Exists(_dataRoot)) return list;

        foreach (var dir in Directory.GetDirectories(_dataRoot))
        {
            var cfg = Path.Combine(dir, "config.json");
            if (!File.Exists(cfg)) continue;

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var t = await ReadTenantAsync(cfg, ct).ConfigureAwait(false);
                if (t != null) list.Add(t);
            }
            catch (JsonException ex)
            {
                LogTenantReadError(cfg, ex);
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
    /// <para>config에 사용할 <c>TenantConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>TenantConfig</c> value used for config.</para>
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
    public async Task SaveAsync(TenantConfig config, CancellationToken ct = default)
    {
        var dir = GetTenantDataPath(config.Slug);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "gallery"));
        Directory.CreateDirectory(Path.Combine(dir, "thumb"));

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

    /// <summary>
    /// \if KO
    /// <para>Tenant Async 데이터를 읽습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads tenant async data.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
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
    /// <para>Read Tenant Async 작업에서 생성한 <c>Task&lt;TenantConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;TenantConfig?&gt;</c> result produced by the read tenant async operation.</para>
    /// \endif
    /// </returns>
    private static async Task<TenantConfig?> ReadTenantAsync(string path, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        LogLegacyLayoutModeFallback(path, json);
        return JsonSerializer.Deserialize<TenantConfig>(json, _jsonOpts);
    }

    /// <summary>
    /// \if KO
    /// <para>Log Legacy Layout Mode Fallback 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the log legacy layout mode fallback operation.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="json">
    /// \if KO
    /// <para>json에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for json.</para>
    /// \endif
    /// </param>
    private static void LogLegacyLayoutModeFallback(string path, string json)
    {
        var raw = TryGetDesignLayoutMode(json);
        if (raw is null || IsCanonicalLayoutMode(raw)) return;

        Console.Error.WriteLine($"[JsonTenantStore] Legacy DesignSettings.LayoutMode '{raw}' in '{path}' was loaded with compatibility fallback.");
    }

    /// <summary>
    /// \if KO
    /// <para>Get Design Layout Mode 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to get design layout mode and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="json">
    /// \if KO
    /// <para>json에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for json.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Get Design Layout Mode 작업에서 생성한 <c>string?</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> result produced by the try get design layout mode operation.</para>
    /// \endif
    /// </returns>
    private static string? TryGetDesignLayoutMode(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!TryGetProperty(doc.RootElement, "DesignSettings", out var design)) return null;
            if (!TryGetProperty(design, "LayoutMode", out var layout)) return null;

            return layout.ValueKind switch
            {
                JsonValueKind.String => layout.GetString(),
                JsonValueKind.Null => "",
                JsonValueKind.Number => layout.GetRawText(),
                _ => layout.GetRawText(),
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Get Property 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to get property and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="element">
    /// \if KO
    /// <para>element에 사용할 <c>JsonElement</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>JsonElement</c> value used for element.</para>
    /// \endif
    /// </param>
    /// <param name="name">
    /// \if KO
    /// <para>name에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for name.</para>
    /// \endif
    /// </param>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Get Property 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the try get property condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// \if KO
    /// <para>Is Canonical Layout Mode 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether is canonical layout mode.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Is Canonical Layout Mode 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the is canonical layout mode condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    private static bool IsCanonicalLayoutMode(string? value) =>
        value is not null
        && (string.Equals(value, "WebPage", StringComparison.Ordinal)
            || string.Equals(value, "TabMenu", StringComparison.Ordinal)
            || string.Equals(value, "Gallery", StringComparison.Ordinal)
            || string.Equals(value, "Story", StringComparison.Ordinal)
            || string.Equals(value, "Card", StringComparison.Ordinal)
            || string.Equals(value, "PhotoBook", StringComparison.Ordinal));

    /// <summary>
    /// \if KO
    /// <para>Log Tenant Read Error 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the log tenant read error operation.</para>
    /// \endif
    /// </summary>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="ex">
    /// \if KO
    /// <para>ex에 사용할 <c>JsonException</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>JsonException</c> value used for ex.</para>
    /// \endif
    /// </param>
    private static void LogTenantReadError(string path, JsonException ex)
    {
        Console.Error.WriteLine($"[JsonTenantStore] Skipped invalid tenant JSON '{path}': {ex.Message}");
    }
}
