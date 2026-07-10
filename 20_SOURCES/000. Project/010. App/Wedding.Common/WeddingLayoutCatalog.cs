using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wedding.Common;

[JsonConverter(typeof(WeddingLayoutModeJsonConverter))]
public enum WeddingLayoutMode
{
    Unknown = 0,
    WebPage = 1,
    TabMenu = 2,
    Gallery = 10,
    Story = 11,
    Card = 12,
    PhotoBook = 13,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WeddingLayoutTier
{
    Free = 0,
    Premium = 1,
}

public sealed record WeddingLayoutOption(
    WeddingLayoutMode Mode,
    string Label,
    string Description,
    WeddingLayoutTier Tier,
    bool IsImplemented,
    string CssClass,
    bool UsesBottomNavigation,
    IReadOnlyList<string> SupportedSections)
{
    public string Key => WeddingLayoutCatalog.ToLegacyKey(Mode);
}

public interface IWeddingLayoutCatalog
{
    IReadOnlyList<WeddingLayoutOption> Layouts { get; }
    WeddingLayoutOption? Find(WeddingLayoutMode mode);
    WeddingLayoutOption? Find(string? key);
    bool Exists(WeddingLayoutMode mode);
}

public sealed class WeddingLayoutCatalog : IWeddingLayoutCatalog
{
    public static readonly WeddingLayoutCatalog Instance = new();

    public static IReadOnlyList<WeddingLayoutOption> Options => Instance.Layouts;

    public IReadOnlyList<WeddingLayoutOption> Layouts { get; } =
    [
        new(
            WeddingLayoutMode.WebPage,
            "웹페이지",
            "위에서 아래로 자연스럽게 읽는 기본 스크롤형 레이아웃입니다.",
            WeddingLayoutTier.Free,
            true,
            "w-layout-onepage",
            false,
            ["hero", "story", "info", "details", "message", "video", "gallery", "guestbook", "gift"]),
        new(
            WeddingLayoutMode.TabMenu,
            "탭 메뉴",
            "하단 메뉴로 주요 내용을 한 페이지씩 전환하는 레이아웃입니다.",
            WeddingLayoutTier.Free,
            true,
            "w-layout-tabs",
            true,
            ["hero", "story", "info", "details", "message", "video", "gallery", "guestbook", "gift"]),
        new(
            WeddingLayoutMode.Gallery,
            "갤러리",
            "사진을 중심으로 보여주는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            true,
            "w-layout-gallery",
            true,
            ["hero", "gallery", "video", "story", "guestbook", "gift"]),
        new(
            WeddingLayoutMode.Story,
            "스토리",
            "두 사람의 이야기를 중심으로 풀어내는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            true,
            "w-layout-story",
            true,
            ["hero", "story", "gallery", "video", "guestbook", "gift"]),
        new(
            WeddingLayoutMode.Card,
            "카드",
            "핵심 정보를 카드처럼 간결하게 보여주는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            false,
            "w-layout-card",
            true,
            ["hero", "info", "details", "message", "gallery", "guestbook", "gift"]),
        new(
            WeddingLayoutMode.PhotoBook,
            "포토북",
            "앨범처럼 사진과 메시지를 넘겨보는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            false,
            "w-layout-photobook",
            true,
            ["hero", "gallery", "story", "video", "guestbook"]),
    ];

    public WeddingLayoutOption? Find(WeddingLayoutMode mode) =>
        Layouts.FirstOrDefault(x => x.Mode == mode);

    public WeddingLayoutOption? Find(string? key) =>
        Find(FromLegacyKey(key));

    public bool Exists(WeddingLayoutMode mode) => Find(mode) is not null;

    public static WeddingLayoutMode FromLegacyKey(string? key) =>
        key?.Trim().ToLowerInvariant() switch
        {
            null or "" => WeddingLayoutMode.WebPage,
            "default" or "scroll" or "vertical" or "page" or "web" => WeddingLayoutMode.WebPage,
            "tabs" or "tabmenu" => WeddingLayoutMode.TabMenu,
            "tab" => WeddingLayoutMode.TabMenu,
            "gallery" => WeddingLayoutMode.Gallery,
            "story" => WeddingLayoutMode.Story,
            "card" => WeddingLayoutMode.Card,
            "photobook" or "photo-book" => WeddingLayoutMode.PhotoBook,
            "onepage" or "webpage" => WeddingLayoutMode.WebPage,
            _ => WeddingLayoutMode.WebPage,
        };

    public static bool IsKnownKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return true;
        return key.Trim().ToLowerInvariant() is
            "tabs" or "tabmenu" or
            "gallery" or
            "story" or
            "card" or
            "photobook" or "photo-book" or
            "onepage" or "webpage";
    }

    public static string ToLegacyKey(WeddingLayoutMode mode) =>
        mode switch
        {
            WeddingLayoutMode.TabMenu => "tabs",
            WeddingLayoutMode.Gallery => "gallery",
            WeddingLayoutMode.Story => "story",
            WeddingLayoutMode.Card => "card",
            WeddingLayoutMode.PhotoBook => "photobook",
            _ => "onepage",
        };
}

public sealed class WeddingLayoutModeJsonConverter : JsonConverter<WeddingLayoutMode>
{
    public override WeddingLayoutMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => WeddingLayoutMode.WebPage,
            JsonTokenType.String => ReadString(reader.GetString()),
            JsonTokenType.Number => reader.TryGetInt32(out var value) && Enum.IsDefined(typeof(WeddingLayoutMode), value)
                ? (WeddingLayoutMode)value
                : WeddingLayoutMode.WebPage,
            _ => WeddingLayoutMode.WebPage,
        };
    }

    public override void Write(Utf8JsonWriter writer, WeddingLayoutMode value, JsonSerializerOptions options)
    {
        var normalized = value == WeddingLayoutMode.Unknown ? WeddingLayoutMode.WebPage : value;
        writer.WriteStringValue(normalized.ToString());
    }

    private static WeddingLayoutMode ReadString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return WeddingLayoutMode.WebPage;
        }

        return WeddingLayoutCatalog.FromLegacyKey(value);
    }
}

public sealed class WeddingLayoutAccessState
{
    public bool HasPremiumPlan { get; init; }
    public IReadOnlyCollection<WeddingLayoutMode> UnlockedLayouts { get; init; } = [];

    public bool IsLayoutUnlocked(WeddingLayoutMode mode) => UnlockedLayouts.Contains(mode);
}

public interface IWeddingLayoutAccessPolicy
{
    bool CanUse(WeddingLayoutOption option, WeddingLayoutAccessState access);
    bool CanUse(WeddingLayoutMode mode, WeddingLayoutAccessState access);
}

public sealed class WeddingLayoutAccessPolicy : IWeddingLayoutAccessPolicy
{
    public bool CanUse(WeddingLayoutOption option, WeddingLayoutAccessState access) =>
        option.Tier == WeddingLayoutTier.Free
        || access.HasPremiumPlan
        || access.IsLayoutUnlocked(option.Mode);

    public bool CanUse(WeddingLayoutMode mode, WeddingLayoutAccessState access)
    {
        var option = WeddingLayoutCatalog.Instance.Find(mode);
        return option is not null && CanUse(option, access);
    }
}
