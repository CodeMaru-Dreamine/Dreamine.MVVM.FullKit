namespace Wedding.Common;

/// <summary>
/// \if KO
/// <para>포토북 레이아웃 전용 페이지 한 장의 정보를 담습니다. 이미 업로드된 갤러리 사진을 참조하고, 그 위에 캡션과 문구를 얹는 형태입니다.</para>
/// \endif
/// \if EN
/// <para>Represents a single photobook page shown by the photobook layout. Each page references an already uploaded gallery photo and overlays a caption and a short body.</para>
/// \endif
/// </summary>
public sealed class PhotoBookPage
{
    /// <summary>페이지 번호(1-based). 저장 및 정렬 키로 사용됩니다.</summary>
    public int PageNumber { get; set; }

    /// <summary>사용할 사진의 원본 파일명. 비어 있으면 렌더러가 갤러리에서 순서대로 자동 보충합니다.</summary>
    public string PhotoFileName { get; set; } = "";

    /// <summary>이미지 상단·하단에 붙는 짧은 캡션(예: "첫 만남").</summary>
    public string Caption { get; set; } = "";

    /// <summary>사진 아래에 보이는 본문 문구(2~4문장 권장).</summary>
    public string Body { get; set; } = "";
}

/// <summary>
/// \if KO
/// <para>포토북 페이지의 기본 세트와 정규화 유틸리티를 제공합니다. StoryChapter 와 동일한 패턴으로 결손 필드를 보완합니다.</para>
/// \endif
/// \if EN
/// <para>Provides default photobook pages and normalization utilities. Mirrors the story chapter pattern to backfill missing fields.</para>
/// \endif
/// </summary>
public static class WeddingPhotoBookPageDefaults
{
    private static readonly PhotoBookPage[] Defaults =
    [
        new() { PageNumber = 1, Caption = "PAGE 01", Body = "" },
        new() { PageNumber = 2, Caption = "PAGE 02", Body = "" },
        new() { PageNumber = 3, Caption = "PAGE 03", Body = "" },
        new() { PageNumber = 4, Caption = "PAGE 04", Body = "" },
    ];

    /// <summary>기본 페이지 세트를 새 인스턴스로 복제해 반환합니다.</summary>
    public static List<PhotoBookPage> Create() =>
        Defaults.Select(Clone).ToList();

    /// <summary>
    /// 저장된 페이지 목록을 정규화합니다. 페이지 번호가 비정상인 항목은 뒤로 밀어 재번호화하고, 기본 세트의 빠진 페이지는 빈 껍질로 보충합니다.
    /// </summary>
    public static List<PhotoBookPage> Normalize(IEnumerable<PhotoBookPage>? pages)
    {
        var existing = pages?
            .Where(x => x is not null)
            .GroupBy(x => x.PageNumber <= 0 ? Defaults.Length + 1 : x.PageNumber)
            .ToDictionary(x => x.Key, x => x.First())
            ?? new Dictionary<int, PhotoBookPage>();

        var normalized = new List<PhotoBookPage>(Defaults.Length);
        foreach (var defaults in Defaults)
        {
            existing.TryGetValue(defaults.PageNumber, out var current);
            normalized.Add(new PhotoBookPage
            {
                PageNumber = defaults.PageNumber,
                PhotoFileName = current?.PhotoFileName ?? "",
                Caption = FirstNonBlank(current?.Caption, defaults.Caption),
                Body = current?.Body ?? "",
            });
        }

        foreach (var page in existing
            .Where(x => x.Key > Defaults.Length)
            .OrderBy(x => x.Key)
            .Select(x => x.Value))
        {
            var clone = Clone(page);
            clone.PageNumber = Math.Max(Defaults.Length + 1, clone.PageNumber);
            clone.Caption = FirstNonBlank(clone.Caption, $"PAGE {clone.PageNumber:00}");
            normalized.Add(clone);
        }

        return normalized;
    }

    /// <summary>페이지 인스턴스를 얕은 복제합니다.</summary>
    public static PhotoBookPage Clone(PhotoBookPage page) => new()
    {
        PageNumber = page.PageNumber,
        PhotoFileName = page.PhotoFileName,
        Caption = page.Caption,
        Body = page.Body,
    };

    private static string FirstNonBlank(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
