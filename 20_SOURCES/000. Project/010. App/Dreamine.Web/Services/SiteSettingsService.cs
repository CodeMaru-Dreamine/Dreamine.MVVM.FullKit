using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

public class SiteSettingsService
{
    private SiteSettings _settings;
    private readonly string _filePath;

    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public SiteSettings Current => _settings;

    public SiteSettingsService(DreamineOptions opts)
    {
        _filePath = Path.Combine(opts.ResolvedDataPath, "site_settings.json");

        var defaults = DefaultFrom(opts);
        if (File.Exists(_filePath))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<SiteSettings>(File.ReadAllText(_filePath));
                if (loaded is null)
                {
                    _settings = defaults;
                }
                else
                {
                    // 저장된 파일에 없는 필드는 기본값으로 채움
                    loaded.OgImageUrl  = string.IsNullOrEmpty(loaded.OgImageUrl)  ? defaults.OgImageUrl  : loaded.OgImageUrl;
                    loaded.OgSiteName  = string.IsNullOrEmpty(loaded.OgSiteName)  ? defaults.OgSiteName  : loaded.OgSiteName;
                    loaded.OgUrl       = string.IsNullOrEmpty(loaded.OgUrl)       ? defaults.OgUrl       : loaded.OgUrl;
                    loaded.TwitterCard = string.IsNullOrEmpty(loaded.TwitterCard) ? defaults.TwitterCard : loaded.TwitterCard;
                    _settings = loaded;
                }
            }
            catch { _settings = defaults; }
        }
        else
        {
            _settings = defaults;
        }
    }

    public async Task SaveAsync(SiteSettings settings)
    {
        _settings = settings;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(settings, _json));
    }

    private static SiteSettings DefaultFrom(DreamineOptions opts) => new()
    {
        Title = opts.SiteTitle,
        Description = opts.SiteDescription,
        IconUrl = string.Empty,
        GitHubUrl = opts.GitHubOrgUrl
    };
}
