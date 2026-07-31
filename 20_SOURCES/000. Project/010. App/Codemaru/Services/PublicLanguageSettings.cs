using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Codemaru.Services;

public sealed class PublicLanguageSettings
{
    private static readonly HashSet<string> Supported =
    [
        "en", "es", "fr", "it", "pt", "ko", "ja", "zh-hans", "zh-hant", "vi"
    ];

    private readonly string path;
    private readonly object sync = new();
    private string defaultLanguage;

    public PublicLanguageSettings(IConfiguration configuration)
    {
        path = Path.Combine(AppContext.BaseDirectory, "App_Data", "public-language.json");
        defaultLanguage = Normalize(configuration["Localization:DefaultLanguage"]) ?? "ko";

        try
        {
            if (File.Exists(path))
            {
                var saved = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(path));
                defaultLanguage = Normalize(saved?.DefaultLanguage) ?? defaultLanguage;
            }
        }
        catch
        {
            // Invalid or inaccessible settings retain the configured safe default.
        }
    }

    public string DefaultLanguage
    {
        get
        {
            lock (sync)
            {
                return defaultLanguage;
            }
        }
    }

    public async Task SaveAsync(string language)
    {
        var normalized = Normalize(language)
            ?? throw new ArgumentException("Unsupported language.", nameof(language));

        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(new SettingsFile(normalized), new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);

        lock (sync)
        {
            defaultLanguage = normalized;
        }
    }

    private static string? Normalize(string? language)
    {
        var value = language?.Trim().ToLowerInvariant().Replace('_', '-');
        return value is not null && Supported.Contains(value) ? value : null;
    }

    private sealed record SettingsFile(string DefaultLanguage);
}
