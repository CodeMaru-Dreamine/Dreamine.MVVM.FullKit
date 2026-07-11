namespace Wedding.Common;

/// <summary>
/// Wedding invitation/thank-you section ordering helpers shared by admin and public renderers.
/// </summary>
public static class WeddingSectionOrderCatalog
{
    public static readonly IReadOnlyList<string> InvitationRecommendedOrder =
        ["hero", "info", "story", "gallery", "video", "details", "gift", "guestbook"];

    public static readonly IReadOnlyList<string> ThankYouRecommendedOrder =
        ["hero", "message", "gallery", "video", "guestbook"];

    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hero"] = "홈",
        ["info"] = "예식 안내",
        ["story"] = "이야기",
        ["message"] = "감사 인사",
        ["gallery"] = "사진",
        ["video"] = "영상",
        ["details"] = "지도",
        ["guestbook"] = "방명록",
        ["gift"] = "계좌 안내",
    };

    private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hero"] = "⌂",
        ["info"] = "ⓘ",
        ["story"] = "♡",
        ["message"] = "✉",
        ["gallery"] = "▧",
        ["video"] = "▷",
        ["details"] = "⌖",
        ["guestbook"] = "✎",
        ["gift"] = "▭",
    };

    private static readonly HashSet<string> InvitationSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "hero", "info", "story", "gallery", "video", "details", "guestbook", "gift",
    };

    private static readonly HashSet<string> ThankYouSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "hero", "message", "gallery", "video", "guestbook",
    };

    public static string GetLabel(string key) =>
        Labels.TryGetValue(key, out var label) ? label : key;

    public static string GetIcon(string key) =>
        Icons.TryGetValue(key, out var icon) ? icon : "•";

    public static List<string> NormalizeInvitationOrder(
        IEnumerable<string>? order,
        IEnumerable<string> supportedSections) =>
        Normalize(order, supportedSections.Where(IsInvitationSection), InvitationRecommendedOrder);

    public static List<string> NormalizeThankYouOrder(IEnumerable<string>? order) =>
        Normalize(order, ThankYouRecommendedOrder.Where(IsThankYouSection), ThankYouRecommendedOrder);

    public static bool IsInvitationSection(string? key) =>
        !string.IsNullOrWhiteSpace(key) && InvitationSections.Contains(NormalizeKey(key));

    public static bool IsThankYouSection(string? key) =>
        !string.IsNullOrWhiteSpace(key) && ThankYouSections.Contains(NormalizeKey(key));

    public static List<string> Normalize(
        IEnumerable<string>? order,
        IEnumerable<string> supportedSections,
        IEnumerable<string> recommendedOrder)
    {
        var supported = supportedSections
            .Where(IsKnown)
            .Select(NormalizeKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<string>();
        foreach (var key in order ?? [])
        {
            var normalized = NormalizeKey(key);
            if (supported.Contains(normalized)
                && result.All(x => !string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(normalized);
            }
        }

        foreach (var key in recommendedOrder.Select(NormalizeKey))
        {
            if (supported.Contains(key)
                && result.All(x => !string.Equals(x, key, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(key);
            }
        }

        foreach (var key in supported.OrderBy(x => x))
        {
            if (result.All(x => !string.Equals(x, key, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(key);
            }
        }

        return result;
    }

    public static bool IsKnown(string? key) =>
        !string.IsNullOrWhiteSpace(key) && Labels.ContainsKey(NormalizeKey(key));

    private static string NormalizeKey(string? key) =>
        key?.Trim().ToLowerInvariant() ?? "";
}
