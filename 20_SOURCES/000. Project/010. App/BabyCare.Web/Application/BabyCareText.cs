using System.Text.Json;
using Dreamine.UI.Blazor.Localization;
namespace BabyCare.Application;
public sealed class BabyCareText : DreamineLocalizationService
{
    public BabyCareText(IWebHostEnvironment env) : base(new DreamineLocalizationCatalog("ko",
        [new("en", "US", "English"), new("es", "ES", "Español"), new("fr", "FR", "Français"),
         new("it", "IT", "Italiano"), new("pt", "PT", "Português"), new("ko", "KR", "한국어"),
         new("ja", "JP", "日本語"), new("zh-hans", "CN", "简体中文", "zh-Hans"),
         new("zh-hant", "HK", "繁體中文", "zh-Hant"), new("vi", "VN", "Tiếng Việt")],
        JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "translations.json")))!)) { }
}
