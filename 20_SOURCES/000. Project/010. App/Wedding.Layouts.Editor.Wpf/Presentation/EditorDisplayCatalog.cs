using Wedding.Layouts.Contracts;
using Wedding.Layouts.Editor.Wpf.Preview;
using System.Windows;
using System.Windows.Media;

namespace Wedding.Layouts.Editor.Wpf.Presentation;

public sealed record EditorChoice<T>(
    T Value,
    string Label,
    string Description = "");

public sealed record EditorSectionItem(
    int Index,
    int[] Path,
    LayoutSectionKey? SectionKey,
    string Icon,
    string Title,
    string Description,
    string PreviewTitle,
    string PreviewBody,
    string DataSourceLabel,
    ImageSource? HeroImage,
    IReadOnlyList<ImageSource> GalleryImages,
    ImageSource? MapImage,
    bool HasHeroImage,
    bool HasGallery,
    bool HasVideo,
    Brush SectionBackground,
    Brush SectionBorderBrush,
    Brush AccentBrush,
    Brush TextBrush,
    Brush MutedTextBrush,
    Thickness SectionBorderThickness,
    Thickness ContentMargin,
    int Columns,
    string LayoutSummary,
    bool IsComposite = false);

public static class EditorDisplayCatalog
{
    public static IReadOnlyList<EditorChoice<LayoutPresentationMode>>
        PresentationChoices { get; } =
    [
        new(
            LayoutPresentationMode.Flow,
            "세로 스크롤",
            "위에서 아래로 자연스럽게 읽는 기본 방식"),
        new(
            LayoutPresentationMode.FlipCard,
            "카드 넘김",
            "한 장씩 넘겨 보는 카드 방식"),
        new(
            LayoutPresentationMode.PagedBook,
            "포토북",
            "책장을 넘기듯 감상하는 방식"),
    ];

    public static IReadOnlyList<EditorChoice<LayoutVisualVariant>>
        VariantChoices { get; } =
    [
        new(LayoutVisualVariant.Default, "기본"),
        new(LayoutVisualVariant.Muted, "차분하게"),
        new(LayoutVisualVariant.Accent, "포인트 강조"),
        new(LayoutVisualVariant.Outlined, "테두리 카드"),
        new(LayoutVisualVariant.Elevated, "떠 있는 카드"),
        new(LayoutVisualVariant.Hero, "표지 강조"),
    ];

    public static IReadOnlyList<EditorChoice<LayoutGap>> GapChoices { get; } =
    [
        new(LayoutGap.None, "간격 없음"),
        new(LayoutGap.ExtraSmall, "아주 좁게"),
        new(LayoutGap.Small, "좁게"),
        new(LayoutGap.Medium, "보통"),
        new(LayoutGap.Large, "넓게"),
        new(LayoutGap.ExtraLarge, "아주 넓게"),
    ];

    public static string GetPresentationLabel(LayoutPresentationMode value) =>
        PresentationChoices.First(choice => choice.Value == value).Label;

    public static string GetBlockLabel(LayoutBlockKind value) => value switch
    {
        LayoutBlockKind.Page => "청첩장 전체",
        LayoutBlockKind.Section => "섹션",
        LayoutBlockKind.Container => "그룹",
        LayoutBlockKind.Stack => "세로 그룹",
        LayoutBlockKind.Grid => "격자",
        LayoutBlockKind.Card => "카드",
        LayoutBlockKind.Hero => "표지",
        LayoutBlockKind.Heading => "제목",
        LayoutBlockKind.Text => "글",
        LayoutBlockKind.Image => "사진",
        LayoutBlockKind.Gallery => "사진첩",
        LayoutBlockKind.Countdown => "예식일까지",
        LayoutBlockKind.Calendar => "달력",
        LayoutBlockKind.Map => "오시는 길",
        LayoutBlockKind.AccountList => "계좌 안내",
        LayoutBlockKind.ContactList => "연락하기",
        LayoutBlockKind.Guestbook => "방명록",
        LayoutBlockKind.VideoGallery => "영상",
        LayoutBlockKind.Navigation => "페이지 이동",
        LayoutBlockKind.Button => "버튼",
        LayoutBlockKind.Divider => "구분선",
        LayoutBlockKind.Spacer => "여백",
        _ => value.ToString(),
    };

    public static string GetBindingLabel(LayoutBindingKey value) => value switch
    {
        LayoutBindingKey.None => "직접 입력 문구",
        LayoutBindingKey.Invitation => "청첩장 기본 정보",
        LayoutBindingKey.CoupleName => "신랑 · 신부 이름",
        LayoutBindingKey.HeroTitle => "표지 문구",
        LayoutBindingKey.Subtitle => "초대 문구",
        LayoutBindingKey.WeddingDate => "예식 날짜",
        LayoutBindingKey.WeddingTime => "예식 시간",
        LayoutBindingKey.VenueName => "예식장 이름",
        LayoutBindingKey.VenueAddress => "예식장 주소",
        LayoutBindingKey.Story => "이야기 1",
        LayoutBindingKey.Story2 => "이야기 2",
        LayoutBindingKey.HeroImage => "대표 사진",
        LayoutBindingKey.Gallery => "사진첩",
        LayoutBindingKey.Accounts => "계좌 정보",
        LayoutBindingKey.Contacts => "연락처",
        LayoutBindingKey.Guestbook => "방명록",
        LayoutBindingKey.Videos => "영상",
        LayoutBindingKey.Map => "지도",
        LayoutBindingKey.Calendar => "달력",
        _ => value.ToString(),
    };

    public static string GetSectionTitle(LayoutSectionKey value) => value switch
    {
        LayoutSectionKey.Hero => "표지",
        LayoutSectionKey.Invitation => "초대 글",
        LayoutSectionKey.Calendar => "예식 안내",
        LayoutSectionKey.Gallery => "사진첩",
        LayoutSectionKey.Story => "우리 이야기",
        LayoutSectionKey.Video => "영상",
        LayoutSectionKey.Location => "오시는 길",
        LayoutSectionKey.Accounts => "마음 전하실 곳",
        LayoutSectionKey.Guestbook => "방명록",
        LayoutSectionKey.Contact => "연락하기",
        _ => value.ToString(),
    };

    public static string GetSectionIcon(LayoutSectionKey value) => value switch
    {
        LayoutSectionKey.Hero => "♡",
        LayoutSectionKey.Invitation => "✉",
        LayoutSectionKey.Calendar => "▣",
        LayoutSectionKey.Gallery => "▧",
        LayoutSectionKey.Story => "❦",
        LayoutSectionKey.Video => "▶",
        LayoutSectionKey.Location => "⌖",
        LayoutSectionKey.Accounts => "▤",
        LayoutSectionKey.Guestbook => "✎",
        LayoutSectionKey.Contact => "☎",
        _ => "•",
    };

    public static string GetSectionDescription(LayoutSectionKey value) =>
        value switch
        {
            LayoutSectionKey.Hero => "대표 사진과 두 분의 이름을 보여줍니다.",
            LayoutSectionKey.Invitation => "초대 문구와 인사말을 보여줍니다.",
            LayoutSectionKey.Calendar => "예식 날짜와 달력을 보여줍니다.",
            LayoutSectionKey.Gallery => "등록한 사진을 갤러리로 보여줍니다.",
            LayoutSectionKey.Story => "두 분의 이야기를 담습니다.",
            LayoutSectionKey.Video => "등록한 영상을 보여줍니다.",
            LayoutSectionKey.Location => "예식장 주소와 지도를 보여줍니다.",
            LayoutSectionKey.Accounts => "축하 마음을 전할 계좌를 보여줍니다.",
            LayoutSectionKey.Guestbook => "하객이 축하 글을 남길 수 있습니다.",
            LayoutSectionKey.Contact => "신랑·신부와 혼주 연락처를 보여줍니다.",
            _ => "청첩장 내용을 보여줍니다.",
        };

    public static EditorSectionItem CreateSectionItem(
        LayoutBlock block,
        int index,
        LayoutSectionKey? sectionKey,
        PreviewMediaSet previewMedia,
        IReadOnlyList<LayoutStyleTokenValue> styleTokens)
    {
        var title = sectionKey is { } known
            ? GetSectionTitle(known)
            : GetBlockLabel(block.Kind);
        var description = sectionKey is { } described
            ? GetSectionDescription(described)
            : "고급 도구에서 만든 사용자 섹션입니다.";
        var icon = sectionKey is { } iconKey
            ? GetSectionIcon(iconKey)
            : "◇";

        var contentBindings = Enumerate(block)
            .Select(item => item.Binding)
            .Where(binding => binding != LayoutBindingKey.None)
            .Distinct()
            .Select(GetBindingLabel)
            .Take(3)
            .ToArray();
        var bindingSummary = contentBindings.Length == 0
            ? "고정 문구와 디자인 요소"
            : string.Join(" · ", contentBindings);
        var directText = Enumerate(block)
            .Select(item => item.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        var (previewTitle, previewBody) = sectionKey is { } sampleKey
            ? GetSamplePreview(sampleKey, directText, bindingSummary)
            : (
                string.IsNullOrWhiteSpace(directText) ? title : directText,
                bindingSummary);
        var isHero = sectionKey == LayoutSectionKey.Hero;
        var isGallery = sectionKey == LayoutSectionKey.Gallery;
        var isVideo = sectionKey == LayoutSectionKey.Video;
        var surface = GetTokenBrush(
            styleTokens,
            LayoutStyleToken.SurfaceColor,
            "#FFFFFF");
        var background = GetTokenBrush(
            styleTokens,
            LayoutStyleToken.BackgroundColor,
            "#FFF9F2");
        var border = GetTokenBrush(
            styleTokens,
            LayoutStyleToken.BorderColor,
            "#DDCDBB");
        var accent = GetTokenBrush(
            styleTokens,
            LayoutStyleToken.PrimaryColor,
            "#B88A58");
        var text = GetTokenBrush(
            styleTokens,
            LayoutStyleToken.TextColor,
            "#2F2924");
        var mutedText = GetTokenBrush(
            styleTokens,
            LayoutStyleToken.MutedTextColor,
            "#776D64");
        var sectionBackground = block.Variant switch
        {
            LayoutVisualVariant.Muted => background,
            LayoutVisualVariant.Accent =>
                WithOpacity(accent, .16),
            _ => surface,
        };
        var sectionBorder = block.Variant is
            LayoutVisualVariant.Accent or LayoutVisualVariant.Outlined
                ? accent
                : border;
        var sectionBorderThickness =
            block.Variant == LayoutVisualVariant.Outlined
                ? new Thickness(2)
                : new Thickness(1);
        var gap = block.ContainerSettings?.Gap ?? LayoutGap.Medium;
        var columns = block.ContainerSettings?.Columns ?? 1;

        return new EditorSectionItem(
            index,
            [index],
            sectionKey,
            icon,
            title,
            description,
            previewTitle,
            previewBody,
            GetBindingLabel(block.Binding),
            isHero ? previewMedia.HeroImage : null,
            isGallery ? previewMedia.GalleryImages : Array.Empty<ImageSource>(),
            sectionKey == LayoutSectionKey.Location
                ? previewMedia.MapImage
                : null,
            isHero && previewMedia.HeroImage is not null,
            isGallery && previewMedia.GalleryImages.Count > 0,
            isVideo && previewMedia.VideoSource is not null,
            sectionBackground,
            sectionBorder,
            accent,
            text,
            mutedText,
            sectionBorderThickness,
            GapToMargin(gap),
            columns,
            $"{columns}칸 · {GapChoices.First(choice => choice.Value == gap).Label}");
    }

    private static (string Title, string Body) GetSamplePreview(
        LayoutSectionKey section,
        string? directText,
        string bindingSummary) =>
        section switch
        {
            LayoutSectionKey.Hero =>
                ("현우  ♥  지은", "2029년 10월 13일 · 그랜드 하얏트 서울"),
            LayoutSectionKey.Invitation =>
                (
                    string.IsNullOrWhiteSpace(directText)
                        ? "초대합니다"
                        : directText,
                    "소중한 분들을 저희의 가을밤에 초대합니다."),
            LayoutSectionKey.Calendar =>
                ("2029. 10. 13. SAT", "오후 5시 30분 · 그랜드 볼룸"),
            LayoutSectionKey.Gallery =>
                ("우리의 순간", "사진을 눌러 크게 감상할 수 있습니다."),
            LayoutSectionKey.Story =>
                ("Our Story", "처음 만난 날부터 함께 걷기로 약속한 오늘까지"),
            LayoutSectionKey.Video =>
                ("Wedding Film", "두 사람의 이야기를 영상으로 만나보세요."),
            LayoutSectionKey.Location =>
                ("오시는 길", "서울특별시 용산구 소월로 322"),
            LayoutSectionKey.Accounts =>
                ("마음 전하실 곳", "신랑측 · 신부측 계좌를 안전하게 안내합니다."),
            LayoutSectionKey.Guestbook =>
                ("축하의 마음을 남겨주세요", "방명록 쓰기"),
            LayoutSectionKey.Contact =>
                ("연락하기", "신랑 · 신부 · 혼주에게 연락하기"),
            _ => (
                string.IsNullOrWhiteSpace(directText)
                    ? GetSectionTitle(section)
                    : directText,
                bindingSummary),
        };

    private static IEnumerable<LayoutBlock> Enumerate(LayoutBlock block)
    {
        yield return block;
        foreach (var child in block.Children)
        {
            foreach (var descendant in Enumerate(child))
            {
                yield return descendant;
            }
        }
    }

    private static Brush GetTokenBrush(
        IReadOnlyList<LayoutStyleTokenValue> tokens,
        LayoutStyleToken token,
        string fallback)
    {
        var value = tokens.FirstOrDefault(item => item.Token == token)?.Value;
        try
        {
            if (ColorConverter.ConvertFromString(
                    string.IsNullOrWhiteSpace(value) ? fallback : value)
                is Color color)
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
        }
        catch (FormatException)
        {
            // The shared validator reports the invalid token. Preview stays safe.
        }

        var fallbackBrush = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(fallback));
        fallbackBrush.Freeze();
        return fallbackBrush;
    }

    private static Brush WithOpacity(Brush source, double opacity)
    {
        var clone = source.Clone();
        clone.Opacity = opacity;
        clone.Freeze();
        return clone;
    }

    private static Thickness GapToMargin(LayoutGap gap) => gap switch
    {
        LayoutGap.None => new Thickness(0),
        LayoutGap.ExtraSmall => new Thickness(6),
        LayoutGap.Small => new Thickness(10),
        LayoutGap.Medium => new Thickness(16),
        LayoutGap.Large => new Thickness(22),
        LayoutGap.ExtraLarge => new Thickness(30),
        _ => new Thickness(16),
    };
}
