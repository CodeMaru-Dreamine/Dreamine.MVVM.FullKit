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
/// \file JsonTenantStore.cs
/// \brief App_Data/Wedding/{slug}/config.json 기반 테넌트 설정 저장소.
/// </summary>
public sealed class JsonTenantStore : ITenantStore
{
    private readonly string _dataRoot;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new WeddingLayoutModeJsonConverter(), new JsonStringEnumConverter() }
    };

    public JsonTenantStore(WeddingOptions opts)
    {
        _dataRoot = opts.ResolvedDataPath;
        Directory.CreateDirectory(_dataRoot);
    }

    public string GetTenantDataPath(string slug) =>
        Path.Combine(_dataRoot, Sanitize(slug));

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

    public Task<bool> ExistsAsync(string slug, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(ConfigPath(slug)));

    public Task DeleteAsync(string slug, CancellationToken ct = default)
    {
        var dir = GetTenantDataPath(slug);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
        return Task.CompletedTask;
    }

    private string ConfigPath(string slug) =>
        Path.Combine(GetTenantDataPath(slug), "config.json");

    private static string Sanitize(string slug) =>
        string.Concat(slug.ToLowerInvariant().Split(Path.GetInvalidFileNameChars()));

    private static async Task<TenantConfig?> ReadTenantAsync(string path, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        LogLegacyLayoutModeFallback(path, json);
        return JsonSerializer.Deserialize<TenantConfig>(json, _jsonOpts);
    }

    private static void LogLegacyLayoutModeFallback(string path, string json)
    {
        var raw = TryGetDesignLayoutMode(json);
        if (raw is null || IsCanonicalLayoutMode(raw)) return;

        Console.Error.WriteLine($"[JsonTenantStore] Legacy DesignSettings.LayoutMode '{raw}' in '{path}' was loaded with compatibility fallback.");
    }

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

    private static bool IsCanonicalLayoutMode(string? value) =>
        value is not null
        && (string.Equals(value, "WebPage", StringComparison.Ordinal)
            || string.Equals(value, "TabMenu", StringComparison.Ordinal)
            || string.Equals(value, "Gallery", StringComparison.Ordinal)
            || string.Equals(value, "Story", StringComparison.Ordinal)
            || string.Equals(value, "Card", StringComparison.Ordinal)
            || string.Equals(value, "PhotoBook", StringComparison.Ordinal));

    private static void LogTenantReadError(string path, JsonException ex)
    {
        Console.Error.WriteLine($"[JsonTenantStore] Skipped invalid tenant JSON '{path}': {ex.Message}");
    }
}
