using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

/// <summary>
/// Stable identifiers for the layouts shipped with the application.
/// These values are persisted and must not be renamed when display labels change.
/// </summary>
public static class WeddingLayoutKeys
{
    public const string OnePage = "onepage";
    public const string Tabs = "tabs";
    public const string Gallery = "gallery";
    public const string Story = "story";
    public const string Card = "card";
    public const string PhotoBook = "photobook";

    public static string Normalize(string? key)
    {
        var normalized = key?.Trim().ToLowerInvariant();
        return normalized switch
        {
            null or "" => OnePage,
            "default" or "scroll" or "vertical" or "page" or "web" or "webpage" => OnePage,
            "tab" or "tabmenu" => Tabs,
            "photo-book" => PhotoBook,
            _ => normalized,
        };
    }

    public static bool IsValid(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return Regex.IsMatch(
            key,
            "^[a-z0-9][a-z0-9-]{0,63}$",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }
}

/// <summary>
/// Semantic-version helpers shared by built-in and uploaded layout packages.
/// </summary>
public static class WeddingLayoutVersion
{
    public const string Initial = "1.0.0";

    public static bool IsValid(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        return Regex.IsMatch(
            version,
            "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?(?:\\+[0-9A-Za-z-]+(?:\\.[0-9A-Za-z-]+)*)?$",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }
}

public readonly record struct WeddingLayoutReleaseId(string LayoutKey, string Version)
{
    public override string ToString() => $"{LayoutKey}@{Version}";
}

/// <summary>
/// Stable layout identity and the version currently published in a catalog snapshot.
/// </summary>
public sealed record WeddingLayoutDescriptor(
    string Key,
    WeddingLayoutMode LegacyMode,
    string Label,
    string Description,
    WeddingLayoutTier Tier,
    string CurrentVersion,
    bool IsBuiltIn);

/// <summary>
/// Immutable render metadata for one exact layout version.
/// Publishing an update creates another release instead of changing this record.
/// </summary>
public sealed record WeddingLayoutRelease(
    string LayoutKey,
    string Version,
    WeddingLayoutMode LegacyMode,
    bool IsImplemented,
    string CssClass,
    bool UsesBottomNavigation,
    IReadOnlyList<string> SupportedSections)
{
    public WeddingLayoutReleaseId Id => new(LayoutKey, Version);
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
    /// <summary>
    /// The catalog key overrides the enum-derived key for dynamically registered layouts.
    /// Existing direct construction keeps the legacy behavior.
    /// </summary>
    public string? CatalogKey { get; init; }

    public string Version { get; init; } = WeddingLayoutVersion.Initial;

    public string Key => string.IsNullOrWhiteSpace(CatalogKey)
        ? WeddingLayoutCatalog.ToLegacyKey(Mode)
        : CatalogKey;

    public WeddingLayoutReleaseId ReleaseId => new(Key, Version);
}

public interface IWeddingLayoutCatalog
{
    IReadOnlyList<WeddingLayoutOption> Layouts { get; }
    IReadOnlyList<WeddingLayoutDescriptor> Descriptors { get; }
    IReadOnlyList<WeddingLayoutRelease> Releases { get; }
    WeddingLayoutOption? Find(WeddingLayoutMode mode);
    WeddingLayoutOption? Find(string? key);
    WeddingLayoutDescriptor? FindDescriptor(WeddingLayoutMode mode);
    WeddingLayoutDescriptor? FindDescriptor(string? key);
    WeddingLayoutRelease? FindRelease(WeddingLayoutMode mode, string? version = null);
    WeddingLayoutRelease? FindRelease(string? key, string? version = null);
    WeddingLayoutRelease? FindRelease(WeddingLayoutReleaseId id);
    bool Exists(WeddingLayoutMode mode);
    bool Exists(string? key);
}

public sealed class WeddingLayoutCatalog : IWeddingLayoutCatalog
{
    public static readonly WeddingLayoutCatalog Instance = new();

    public static IReadOnlyList<WeddingLayoutOption> Options => Instance.Layouts;

    private readonly IReadOnlyDictionary<string, WeddingLayoutDescriptor> _descriptorsByKey;
    private readonly IReadOnlyDictionary<WeddingLayoutMode, WeddingLayoutDescriptor> _descriptorsByMode;
    private readonly IReadOnlyDictionary<string, WeddingLayoutRelease> _releasesById;
    private readonly IReadOnlyDictionary<string, WeddingLayoutOption> _optionsByKey;
    private readonly IReadOnlyDictionary<WeddingLayoutMode, WeddingLayoutOption> _optionsByMode;

    public WeddingLayoutCatalog()
        : this(CreateBuiltInDescriptors(), CreateBuiltInReleases())
    {
    }

    /// <summary>
    /// Creates an immutable catalog snapshot. A runtime registry can atomically replace
    /// a snapshot after a new package version is approved without restarting the server.
    /// </summary>
    public WeddingLayoutCatalog(
        IEnumerable<WeddingLayoutDescriptor> descriptors,
        IEnumerable<WeddingLayoutRelease> releases)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentNullException.ThrowIfNull(releases);

        var descriptorArray = descriptors
            .Select(NormalizeDescriptor)
            .ToArray();
        var releaseArray = releases
            .Select(NormalizeRelease)
            .ToArray();

        EnsureUnique(
            descriptorArray.Select(x => x.Key),
            "layout descriptor key",
            StringComparer.OrdinalIgnoreCase);
        EnsureUnique(
            descriptorArray
                .Where(x => x.LegacyMode != WeddingLayoutMode.Unknown)
                .Select(x => x.LegacyMode.ToString()),
            "legacy layout mode",
            StringComparer.Ordinal);
        EnsureUnique(
            releaseArray.Select(x => ReleaseLookupKey(x.LayoutKey, x.Version)),
            "layout release",
            StringComparer.Ordinal);

        var descriptorsByKey = descriptorArray.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var release in releaseArray)
        {
            if (!descriptorsByKey.TryGetValue(release.LayoutKey, out var descriptor))
            {
                throw new ArgumentException(
                    $"Layout release '{release.Id}' has no matching descriptor.",
                    nameof(releases));
            }

            if (descriptor.LegacyMode != release.LegacyMode)
            {
                throw new ArgumentException(
                    $"Layout release '{release.Id}' does not match descriptor legacy mode.",
                    nameof(releases));
            }
        }

        var releasesById = releaseArray.ToDictionary(
            x => ReleaseLookupKey(x.LayoutKey, x.Version),
            StringComparer.Ordinal);
        foreach (var descriptor in descriptorArray)
        {
            var currentReleaseKey = ReleaseLookupKey(descriptor.Key, descriptor.CurrentVersion);
            if (!releasesById.ContainsKey(currentReleaseKey))
            {
                throw new ArgumentException(
                    $"Layout descriptor '{descriptor.Key}' points to missing release '{descriptor.CurrentVersion}'.",
                    nameof(descriptors));
            }
        }

        var options = descriptorArray
            .Select(descriptor =>
            {
                var release = releasesById[ReleaseLookupKey(descriptor.Key, descriptor.CurrentVersion)];
                return new WeddingLayoutOption(
                    descriptor.LegacyMode,
                    descriptor.Label,
                    descriptor.Description,
                    descriptor.Tier,
                    release.IsImplemented,
                    release.CssClass,
                    release.UsesBottomNavigation,
                    release.SupportedSections)
                {
                    CatalogKey = descriptor.Key,
                    Version = release.Version,
                };
            })
            .ToArray();

        Descriptors = Array.AsReadOnly(descriptorArray);
        Releases = Array.AsReadOnly(releaseArray);
        Layouts = Array.AsReadOnly(options);
        _descriptorsByKey = descriptorsByKey;
        _descriptorsByMode = descriptorArray
            .Where(x => x.LegacyMode != WeddingLayoutMode.Unknown)
            .ToDictionary(x => x.LegacyMode);
        _releasesById = releasesById;
        _optionsByKey = options.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        _optionsByMode = options
            .Where(x => x.Mode != WeddingLayoutMode.Unknown)
            .ToDictionary(x => x.Mode);
    }

    public IReadOnlyList<WeddingLayoutOption> Layouts { get; }

    public IReadOnlyList<WeddingLayoutDescriptor> Descriptors { get; }

    public IReadOnlyList<WeddingLayoutRelease> Releases { get; }

    public WeddingLayoutOption? Find(WeddingLayoutMode mode) =>
        _optionsByMode.GetValueOrDefault(mode);

    public WeddingLayoutOption? Find(string? key)
    {
        var normalized = WeddingLayoutKeys.Normalize(key);
        return _optionsByKey.GetValueOrDefault(normalized)
            // Keep the previous fallback behavior for unknown legacy strings.
            ?? Find(FromLegacyKey(key));
    }

    public bool Exists(WeddingLayoutMode mode) => Find(mode) is not null;

    public bool Exists(string? key) => FindDescriptor(key) is not null;

    public WeddingLayoutDescriptor? FindDescriptor(WeddingLayoutMode mode) =>
        _descriptorsByMode.GetValueOrDefault(mode);

    public WeddingLayoutDescriptor? FindDescriptor(string? key) =>
        _descriptorsByKey.GetValueOrDefault(WeddingLayoutKeys.Normalize(key));

    public WeddingLayoutRelease? FindRelease(WeddingLayoutMode mode, string? version = null)
    {
        var descriptor = FindDescriptor(mode);
        return descriptor is null ? null : FindRelease(descriptor.Key, version);
    }

    public WeddingLayoutRelease? FindRelease(string? key, string? version = null)
    {
        var descriptor = FindDescriptor(key);
        if (descriptor is null)
        {
            return null;
        }

        var releaseVersion = string.IsNullOrWhiteSpace(version)
            ? descriptor.CurrentVersion
            : version.Trim();
        return _releasesById.GetValueOrDefault(ReleaseLookupKey(descriptor.Key, releaseVersion));
    }

    public WeddingLayoutRelease? FindRelease(WeddingLayoutReleaseId id) =>
        FindRelease(id.LayoutKey, id.Version);

    public static WeddingLayoutMode FromLegacyKey(string? key) =>
        WeddingLayoutKeys.Normalize(key) switch
        {
            WeddingLayoutKeys.Tabs => WeddingLayoutMode.TabMenu,
            WeddingLayoutKeys.Gallery => WeddingLayoutMode.Gallery,
            WeddingLayoutKeys.Story => WeddingLayoutMode.Story,
            WeddingLayoutKeys.Card => WeddingLayoutMode.Card,
            WeddingLayoutKeys.PhotoBook => WeddingLayoutMode.PhotoBook,
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
            WeddingLayoutMode.TabMenu => WeddingLayoutKeys.Tabs,
            WeddingLayoutMode.Gallery => WeddingLayoutKeys.Gallery,
            WeddingLayoutMode.Story => WeddingLayoutKeys.Story,
            WeddingLayoutMode.Card => WeddingLayoutKeys.Card,
            WeddingLayoutMode.PhotoBook => WeddingLayoutKeys.PhotoBook,
            _ => WeddingLayoutKeys.OnePage,
        };

    private static WeddingLayoutDescriptor NormalizeDescriptor(WeddingLayoutDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var key = WeddingLayoutKeys.Normalize(descriptor.Key);
        if (!WeddingLayoutKeys.IsValid(key))
        {
            throw new ArgumentException($"Invalid layout key '{descriptor.Key}'.", nameof(descriptor));
        }

        var version = descriptor.CurrentVersion?.Trim();
        if (!WeddingLayoutVersion.IsValid(version))
        {
            throw new ArgumentException(
                $"Layout '{key}' has invalid semantic version '{descriptor.CurrentVersion}'.",
                nameof(descriptor));
        }

        return descriptor with
        {
            Key = key,
            CurrentVersion = version!,
        };
    }

    private static WeddingLayoutRelease NormalizeRelease(WeddingLayoutRelease release)
    {
        ArgumentNullException.ThrowIfNull(release);

        var key = WeddingLayoutKeys.Normalize(release.LayoutKey);
        if (!WeddingLayoutKeys.IsValid(key))
        {
            throw new ArgumentException($"Invalid layout key '{release.LayoutKey}'.", nameof(release));
        }

        var version = release.Version?.Trim();
        if (!WeddingLayoutVersion.IsValid(version))
        {
            throw new ArgumentException(
                $"Layout release '{key}' has invalid semantic version '{release.Version}'.",
                nameof(release));
        }

        var sections = release.SupportedSections?.ToArray()
            ?? throw new ArgumentException(
                $"Layout release '{key}@{version}' has no supported-sections collection.",
                nameof(release));

        return release with
        {
            LayoutKey = key,
            Version = version!,
            SupportedSections = Array.AsReadOnly(sections),
        };
    }

    private static void EnsureUnique(
        IEnumerable<string> values,
        string itemName,
        IEqualityComparer<string> comparer)
    {
        var duplicate = values
            .GroupBy(x => x, comparer)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate {itemName} '{duplicate.Key}'.");
        }
    }

    private static string ReleaseLookupKey(string key, string version) => $"{key}\n{version}";

    private static IReadOnlyList<WeddingLayoutDescriptor> CreateBuiltInDescriptors() =>
    [
        new(
            WeddingLayoutKeys.OnePage,
            WeddingLayoutMode.WebPage,
            "웹페이지",
            "위에서 아래로 자연스럽게 읽는 기본 스크롤형 레이아웃입니다.",
            WeddingLayoutTier.Free,
            WeddingLayoutVersion.Initial,
            true),
        new(
            WeddingLayoutKeys.Tabs,
            WeddingLayoutMode.TabMenu,
            "탭 메뉴",
            "하단 메뉴로 주요 내용을 한 페이지씩 전환하는 레이아웃입니다.",
            WeddingLayoutTier.Free,
            WeddingLayoutVersion.Initial,
            true),
        new(
            WeddingLayoutKeys.Gallery,
            WeddingLayoutMode.Gallery,
            "갤러리",
            "사진을 중심으로 보여주는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            WeddingLayoutVersion.Initial,
            true),
        new(
            WeddingLayoutKeys.Story,
            WeddingLayoutMode.Story,
            "스토리",
            "두 사람의 이야기를 중심으로 풀어내는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            WeddingLayoutVersion.Initial,
            true),
        new(
            WeddingLayoutKeys.Card,
            WeddingLayoutMode.Card,
            "카드",
            "청첩장 면을 실제 카드처럼 입체적으로 넘겨 보는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            WeddingLayoutVersion.Initial,
            true),
        new(
            WeddingLayoutKeys.PhotoBook,
            WeddingLayoutMode.PhotoBook,
            "포토북",
            "책의 펼침면과 페이지 넘김으로 사진과 메시지를 감상하는 프리미엄 레이아웃입니다.",
            WeddingLayoutTier.Premium,
            WeddingLayoutVersion.Initial,
            true),
    ];

    private static IReadOnlyList<WeddingLayoutRelease> CreateBuiltInReleases() =>
    [
        new(
            WeddingLayoutKeys.OnePage,
            WeddingLayoutVersion.Initial,
            WeddingLayoutMode.WebPage,
            true,
            "w-layout-onepage",
            false,
            ["hero", "story", "info", "details", "message", "video", "gallery", "guestbook", "gift"]),
        new(
            WeddingLayoutKeys.Tabs,
            WeddingLayoutVersion.Initial,
            WeddingLayoutMode.TabMenu,
            true,
            "w-layout-tabs",
            true,
            ["hero", "story", "info", "details", "message", "video", "gallery", "guestbook", "gift"]),
        new(
            WeddingLayoutKeys.Gallery,
            WeddingLayoutVersion.Initial,
            WeddingLayoutMode.Gallery,
            true,
            "w-layout-gallery",
            true,
            ["hero", "info", "gallery", "video", "story", "details", "guestbook", "gift"]),
        new(
            WeddingLayoutKeys.Story,
            WeddingLayoutVersion.Initial,
            WeddingLayoutMode.Story,
            true,
            "w-layout-story",
            true,
            ["hero", "info", "story", "gallery", "video", "details", "guestbook", "gift"]),
        new(
            WeddingLayoutKeys.Card,
            WeddingLayoutVersion.Initial,
            WeddingLayoutMode.Card,
            true,
            "w-layout-card",
            true,
            ["hero", "story", "info", "details", "message", "video", "gallery", "guestbook", "gift"]),
        new(
            WeddingLayoutKeys.PhotoBook,
            WeddingLayoutVersion.Initial,
            WeddingLayoutMode.PhotoBook,
            true,
            "w-layout-photobook",
            true,
            ["hero", "info", "details", "message", "gallery", "story", "video", "guestbook", "gift"]),
    ];
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

    // Kept as a source-compatibility boundary for older callers while persisted
    // per-layout grants are migrated. They no longer grant runtime access.
    public IReadOnlyCollection<WeddingLayoutMode> UnlockedLayouts { get; init; } = [];
    public IReadOnlyCollection<string> UnlockedLayoutKeys { get; init; } = [];

    public bool IsLayoutUnlocked(WeddingLayoutMode mode) =>
        mode != WeddingLayoutMode.Unknown && UnlockedLayouts.Contains(mode);

    public bool IsLayoutUnlocked(string? key)
    {
        var normalized = WeddingLayoutKeys.Normalize(key);
        return UnlockedLayoutKeys.Any(x =>
            string.Equals(WeddingLayoutKeys.Normalize(x), normalized, StringComparison.OrdinalIgnoreCase));
    }
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
        || access.HasPremiumPlan;

    public bool CanUse(WeddingLayoutMode mode, WeddingLayoutAccessState access)
    {
        var option = WeddingLayoutCatalog.Instance.Find(mode);
        return option is not null && CanUse(option, access);
    }
}
