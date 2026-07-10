namespace Wedding.Common;

public sealed class StoryChapter
{
    public int ChapterNumber { get; set; }
    public string Label { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string PhotoPath { get; set; } = "";
    public string PhotoId { get; set; } = "";
}

public static class WeddingStoryChapterDefaults
{
    private static readonly StoryChapter[] Defaults =
    [
        new() { ChapterNumber = 1, Label = "CHAPTER 01", Title = "우리의 이야기" },
        new() { ChapterNumber = 2, Label = "CHAPTER 02", Title = "함께한 시간" },
        new() { ChapterNumber = 3, Label = "CHAPTER 03", Title = "결혼식 날" },
        new() { ChapterNumber = 4, Label = "CHAPTER 04", Title = "마음을 전합니다" },
    ];

    public static List<StoryChapter> Create() =>
        Defaults.Select(Clone).ToList();

    public static List<StoryChapter> Normalize(IEnumerable<StoryChapter>? chapters)
    {
        var existing = chapters?
            .Where(x => x is not null)
            .GroupBy(x => x.ChapterNumber <= 0 ? Defaults.Length + 1 : x.ChapterNumber)
            .ToDictionary(x => x.Key, x => x.First())
            ?? new Dictionary<int, StoryChapter>();

        var normalized = new List<StoryChapter>(Defaults.Length);
        foreach (var defaults in Defaults)
        {
            existing.TryGetValue(defaults.ChapterNumber, out var current);
            normalized.Add(new StoryChapter
            {
                ChapterNumber = defaults.ChapterNumber,
                Label = FirstNonBlank(current?.Label, defaults.Label),
                Title = FirstNonBlank(current?.Title, defaults.Title),
                Body = current?.Body ?? "",
                PhotoPath = current?.PhotoPath ?? "",
                PhotoId = current?.PhotoId ?? "",
            });
        }

        foreach (var chapter in existing
            .Where(x => x.Key > Defaults.Length)
            .OrderBy(x => x.Key)
            .Select(x => x.Value))
        {
            var clone = Clone(chapter);
            clone.ChapterNumber = Math.Max(Defaults.Length + 1, clone.ChapterNumber);
            clone.Label = FirstNonBlank(clone.Label, $"CHAPTER {clone.ChapterNumber:00}");
            clone.Title = FirstNonBlank(clone.Title, "스토리");
            normalized.Add(clone);
        }

        return normalized;
    }

    public static StoryChapter Clone(StoryChapter chapter) => new()
    {
        ChapterNumber = chapter.ChapterNumber,
        Label = chapter.Label,
        Title = chapter.Title,
        Body = chapter.Body,
        PhotoPath = chapter.PhotoPath,
        PhotoId = chapter.PhotoId,
    };

    private static string FirstNonBlank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
