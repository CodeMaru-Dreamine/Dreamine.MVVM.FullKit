using System.Windows.Media;

namespace Wedding.Layouts.Editor.Wpf.Preview;

public sealed record PreviewMediaSet
{
    public ImageSource? HeroImage { get; init; }

    public IReadOnlyList<ImageSource> GalleryImages { get; init; } =
        Array.Empty<ImageSource>();

    public ImageSource? MapImage { get; init; }

    public Uri? VideoSource { get; init; }

    public Uri? AudioSource { get; init; }

    public string VideoLabel { get; init; } = "선택된 영상 없음";

    public string AudioLabel { get; init; } = "선택된 음악 없음";

    public int PhotoCount => GalleryImages.Count;
}
