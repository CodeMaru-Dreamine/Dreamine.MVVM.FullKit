namespace Wedding.Common;

/// <summary>
/// \if KO
/// <para>카드 레이아웃의 히어로 아래에 표시되는 강조 카드 한 장을 나타냅니다. 아이콘·제목·본문 3요소로 구성되며 청첩장의 포인트 메시지 전달용으로 사용됩니다.</para>
/// \endif
/// \if EN
/// <para>Represents a single highlight card shown below the hero of the card layout. Each entry has an icon, title, and body, and is used for delivering key point messages on the invitation.</para>
/// \endif
/// </summary>
public sealed class CardHighlight
{
    /// <summary>표시 순서(1-based). 저장 및 정렬 키로 사용됩니다.</summary>
    public int Order { get; set; }

    /// <summary>강조가 연결될 대상 섹션 키(info, details, gift, message, video, gallery, story, guestbook). 비어 있으면 렌더러가 이 항목을 무시합니다.</summary>
    public string SectionKey { get; set; } = "";

    /// <summary>선두에 표시할 아이콘 또는 이모지(예: "💒", "📍", "🎁"). 비어 있으면 섹션 기본 아이콘을 사용합니다.</summary>
    public string Icon { get; set; } = "";

    /// <summary>제목 오버라이드. 비어 있으면 섹션 기본 라벨을 사용합니다.</summary>
    public string Title { get; set; } = "";

    /// <summary>부가 설명(2~3줄 권장). 카드 프레임 헤더 아래에 얹혀 표시됩니다.</summary>
    public string Body { get; set; } = "";
}

/// <summary>
/// \if KO
/// <para>카드 강조 항목의 기본 세트와 정규화 유틸리티를 제공합니다.</para>
/// \endif
/// \if EN
/// <para>Provides default card highlight entries and normalization utilities.</para>
/// \endif
/// </summary>
public static class WeddingCardHighlightDefaults
{
    private static readonly CardHighlight[] Defaults =
    [
        new() { Order = 1, SectionKey = "info",    Icon = "💒", Title = "",  Body = "" },
        new() { Order = 2, SectionKey = "details", Icon = "📍", Title = "",  Body = "" },
        new() { Order = 3, SectionKey = "gift",    Icon = "💌", Title = "",  Body = "" },
    ];

    /// <summary>기본 강조 카드 세트를 새 인스턴스로 복제해 반환합니다.</summary>
    public static List<CardHighlight> Create() =>
        Defaults.Select(Clone).ToList();

    /// <summary>
    /// 저장된 강조 카드 목록을 정규화합니다. Order 가 비정상인 항목은 뒤로 밀어 재번호화하고, 기본 세트의 빈 슬롯은 껍질로 보충합니다.
    /// </summary>
    public static List<CardHighlight> Normalize(IEnumerable<CardHighlight>? highlights)
    {
        var existing = highlights?
            .Where(x => x is not null)
            .GroupBy(x => x.Order <= 0 ? Defaults.Length + 1 : x.Order)
            .ToDictionary(x => x.Key, x => x.First())
            ?? new Dictionary<int, CardHighlight>();

        var normalized = new List<CardHighlight>(Defaults.Length);
        foreach (var defaults in Defaults)
        {
            existing.TryGetValue(defaults.Order, out var current);
            normalized.Add(new CardHighlight
            {
                Order = defaults.Order,
                SectionKey = string.IsNullOrWhiteSpace(current?.SectionKey) ? defaults.SectionKey : current!.SectionKey.Trim().ToLowerInvariant(),
                Icon = current?.Icon?.Trim() ?? defaults.Icon,
                Title = current?.Title?.Trim() ?? "",
                Body = current?.Body?.TrimEnd() ?? "",
            });
        }

        foreach (var highlight in existing
            .Where(x => x.Key > Defaults.Length)
            .OrderBy(x => x.Key)
            .Select(x => x.Value))
        {
            var clone = Clone(highlight);
            clone.Order = Math.Max(Defaults.Length + 1, clone.Order);
            clone.SectionKey = clone.SectionKey?.Trim().ToLowerInvariant() ?? "";
            normalized.Add(clone);
        }

        return normalized;
    }

    /// <summary>강조 카드 인스턴스를 얕은 복제합니다.</summary>
    public static CardHighlight Clone(CardHighlight highlight) => new()
    {
        Order = highlight.Order,
        SectionKey = highlight.SectionKey,
        Icon = highlight.Icon,
        Title = highlight.Title,
        Body = highlight.Body,
    };

    private static string FirstNonBlank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
