namespace Wedding.Common;

public enum WeddingThemeTier
{
    Free = 0,
    Premium = 1,
}

public sealed record WeddingThemeOption(
    string Key,
    string DisplayName,
    string Description,
    WeddingThemeTier Tier,
    string CssClass,
    bool IsImplemented,
    string Primary,
    string Accent);

public interface IWeddingThemeCatalog
{
    IReadOnlyList<WeddingThemeOption> Themes { get; }
    WeddingThemeOption? Find(string? key);
    bool Exists(string? key);
}

public sealed class WeddingThemeCatalog : IWeddingThemeCatalog
{
    public const string DefaultThemeKey = "rose";
    public static readonly WeddingThemeCatalog Instance = new();
    public static IReadOnlyList<WeddingThemeOption> Options => Instance.Themes;

    public IReadOnlyList<WeddingThemeOption> Themes { get; } =
    [
        new(
            DefaultThemeKey,
            "로즈 골드 (기본)",
            "따뜻한 골드 톤의 기본 무료 테마입니다.",
            WeddingThemeTier.Free,
            "w-theme-rose",
            true,
            "#c8a882",
            "#a07850"),
        new(
            "ivory",
            "아이보리 크림",
            "부드러운 아이보리 톤의 프리미엄 테마입니다.",
            WeddingThemeTier.Premium,
            "w-theme-ivory",
            true,
            "#b8a99a",
            "#8a7060"),
        new(
            "forest",
            "포레스트 그린",
            "차분한 그린 톤의 프리미엄 테마입니다.",
            WeddingThemeTier.Premium,
            "w-theme-forest",
            true,
            "#6b8f71",
            "#4a6b50"),
        new(
            "navy",
            "네이비 & 골드",
            "깊은 네이비와 골드 포인트의 프리미엄 테마입니다.",
            WeddingThemeTier.Premium,
            "w-theme-navy",
            true,
            "#3d5a80",
            "#98c1d9"),
        new(
            "blush",
            "블러쉬 핑크",
            "은은한 핑크 톤의 프리미엄 테마입니다.",
            WeddingThemeTier.Premium,
            "w-theme-blush",
            true,
            "#d4a5a5",
            "#b07575"),
    ];

    public WeddingThemeOption? Find(string? key)
    {
        var normalized = NormalizeKey(key);
        return Themes.FirstOrDefault(x => string.Equals(x.Key, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public bool Exists(string? key) => Find(key) is not null;

    public static string NormalizeKey(string? key)
    {
        var normalized = key?.Trim().ToLowerInvariant();
        return normalized switch
        {
            null or "" => DefaultThemeKey,
            "default" or "classic" => DefaultThemeKey,
            "rose" or "rosegold" or "rose-gold" => DefaultThemeKey,
            "ivory" => "ivory",
            "forest" => "forest",
            "navy" => "navy",
            "blush" => "blush",
            _ => DefaultThemeKey,
        };
    }

    public static bool IsKnownKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var normalized = key.Trim().ToLowerInvariant();
        return Options.Any(x => string.Equals(x.Key, normalized, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class WeddingThemeAccessState
{
    public bool HasPremiumPlan { get; init; }
    public IReadOnlyCollection<string> UnlockedThemeKeys { get; init; } = [];

    public bool IsThemeUnlocked(string key)
    {
        var normalized = WeddingThemeCatalog.NormalizeKey(key);
        return UnlockedThemeKeys.Any(x => string.Equals(WeddingThemeCatalog.NormalizeKey(x), normalized, StringComparison.OrdinalIgnoreCase));
    }
}

public interface IWeddingThemeAccessPolicy
{
    bool CanUse(WeddingThemeOption option, WeddingThemeAccessState access);
    bool CanUse(string? key, WeddingThemeAccessState access);
}

public sealed class WeddingThemeAccessPolicy : IWeddingThemeAccessPolicy
{
    public bool CanUse(WeddingThemeOption option, WeddingThemeAccessState access) =>
        option.Tier == WeddingThemeTier.Free
        || access.HasPremiumPlan
        || access.IsThemeUnlocked(option.Key);

    public bool CanUse(string? key, WeddingThemeAccessState access)
    {
        var option = WeddingThemeCatalog.Instance.Find(key);
        return option is not null && CanUse(option, access);
    }
}
