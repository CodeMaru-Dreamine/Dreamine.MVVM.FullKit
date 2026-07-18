using System.IO;
using System.Text.Json;
using DreamineWeb.Models;

namespace DreamineWeb.Services;

/// <summary>
/// \if KO
/// <para>Site Settings Service 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates site settings service functionality and related state.</para>
/// \endif
/// </summary>
public class SiteSettingsService
{
    /// <summary>
    /// \if KO
    /// <para>settings 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the settings value.</para>
    /// \endif
    /// </summary>
    private SiteSettings _settings;
    /// <summary>
    /// \if KO
    /// <para>file Path 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the file path value.</para>
    /// \endif
    /// </summary>
    private readonly string _filePath;

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
    /// <para>Current 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the current value.</para>
    /// \endif
    /// </summary>
    public SiteSettings Current => _settings;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="SiteSettingsService"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="SiteSettingsService"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
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
    /// <para>settings에 사용할 <c>SiteSettings</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SiteSettings</c> value used for settings.</para>
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
    public async Task SaveAsync(SiteSettings settings)
    {
        _settings = settings;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(settings, _json));
    }

    /// <summary>
    /// \if KO
    /// <para>Default From 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the default from operation.</para>
    /// \endif
    /// </summary>
    /// <param name="opts">
    /// \if KO
    /// <para>opts에 사용할 <c>DreamineOptions</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>DreamineOptions</c> value used for opts.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Default From 작업에서 생성한 <c>SiteSettings</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>SiteSettings</c> result produced by the default from operation.</para>
    /// \endif
    /// </returns>
    private static SiteSettings DefaultFrom(DreamineOptions opts) => new()
    {
        Title = opts.SiteTitle,
        Description = opts.SiteDescription,
        IconUrl = string.Empty,
        GitHubUrl = opts.GitHubOrgUrl
    };
}
