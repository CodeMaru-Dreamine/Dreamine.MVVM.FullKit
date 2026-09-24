using BabyCare.Domain;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BabyCare.Application;

public static class CareLanguages
{
    public static readonly string[] Codes = ["ko", "en", "es", "fr", "it", "pt", "ja", "zh-hans", "zh-hant", "vi"];
    public static string Normalize(string language) => Codes.Contains(language) ? language : "ko";
    public static string DisplayNote(CareEntry entry, string language) =>
        entry.NoteTranslations.TryGetValue(Normalize(language), out var translated) && !string.IsNullOrWhiteSpace(translated)
            ? translated : entry.Note;

    // Only complete point commands without explicit times or corrections bypass the AI.
    private static readonly string[] FeedingPatterns =
    [
        @"^(?:지금)?(?:분유|모유)\s*(?<ml>\d{1,4})\s*(?:ml|미리|밀리)(?:\s*먹었어|\s*먹었어요)?[.!]?$",
        @"^(?:baby\s+)?(?:drank|had)\s+(?<ml>\d{1,4})\s*ml\s+(?:of\s+)?(?:milk|formula)[.!]?$",
        @"^(?:el\s+bebé\s+)?(?:tomó|bebió)\s+(?<ml>\d{1,4})\s*ml\s+de\s+leche[.!]?$",
        @"^(?:bébé\s+)?a\s+bu\s+(?<ml>\d{1,4})\s*ml\s+de\s+lait[.!]?$",
        @"^(?:il\s+bambino\s+)?ha\s+bevuto\s+(?<ml>\d{1,4})\s*ml\s+di\s+latte[.!]?$",
        @"^(?:o\s+bebé\s+)?bebeu\s+(?<ml>\d{1,4})\s*ml\s+de\s+leite[.!]?$",
        @"^ミルク(?:を)?\s*(?<ml>\d{1,4})\s*(?:ml|ミリ)(?:飲んだ|飲みました)[。.!]?$",
        @"^(?:宝宝|寶寶)?喝了?\s*(?<ml>\d{1,4})\s*(?:ml|毫升)(?:奶|牛奶)[。.!]?$",
        @"^(?:bé\s+)?(?:uống|đã\s+uống)\s+(?<ml>\d{1,4})\s*ml\s+sữa[.!]?$"
    ];
    public static CareEntry? TryFeeding(string text, DateTimeOffset now)
    {
        foreach (var pattern in FeedingPatterns)
        {
            var match = Regex.Match(text.Trim(), pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success) return new CareEntry { Kind = "feeding", StartedAt = now,
                AmountMl = int.Parse(match.Groups["ml"].Value, CultureInfo.InvariantCulture),
                FeedingMode = text.Contains("분유") || text.Contains("formula", StringComparison.OrdinalIgnoreCase) ? "formula" : text.Contains("모유") ? "expressed" : "" };
        }
        return null;
    }
}
