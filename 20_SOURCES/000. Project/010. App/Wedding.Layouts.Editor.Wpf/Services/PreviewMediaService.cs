using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wedding.Layouts.Editor.Wpf.Preview;

namespace Wedding.Layouts.Editor.Wpf.Services;

public static class PreviewMediaService
{
    private const int PreviewDecodeWidth = 720;
    private const int MaximumPreviewPhotos = 30;
    private const int MaximumImageDimension = 16_384;
    private const long MaximumImagePixels = 40_000_000;
    private const int MaximumDecodedHeight = 8_192;
    private const long MaximumImageFileBytes = 25L * 1024 * 1024;
    private const long MaximumTotalImageFileBytes = 300L * 1024 * 1024;
    private const long MaximumSourceDecodeBytes = 256L * 1024 * 1024;
    private const long MaximumDecodedImageBytes = 24L * 1024 * 1024;
    private const long MaximumTotalDecodedImageBytes = 192L * 1024 * 1024;

    public static PreviewMediaSet LoadBundledSample()
    {
        var directory = Path.Combine(
            AppContext.BaseDirectory,
            "Samples",
            "PreviewMedia");
        var heroPath = Path.Combine(directory, "hero.png");
        var galleryPaths = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "gallery-*.png")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();
        var videoPath = Path.Combine(directory, "wedding-film.mp4");
        var audioPath = Path.Combine(directory, "background-music.mp3");
        var mapPath = Path.Combine(directory, "venue-map.jpg");

        return new PreviewMediaSet
        {
            HeroImage = File.Exists(heroPath)
                ? LoadImage(heroPath)
                : null,
            GalleryImages = LoadImages(galleryPaths),
            MapImage = File.Exists(mapPath)
                ? LoadImage(mapPath)
                : null,
            VideoSource = CreateFileUri(videoPath),
            AudioSource = CreateFileUri(audioPath),
            VideoLabel = File.Exists(videoPath)
                ? "현우 · 지은 웨딩 필름"
                : "선택된 영상 없음",
            AudioLabel = File.Exists(audioPath)
                ? "Autumn Night"
                : "선택된 음악 없음",
        };
    }

    public static PreviewMediaSet WithPhotos(
        PreviewMediaSet current,
        IReadOnlyList<string> paths)
    {
        var images = LoadImages(paths);
        return current with
        {
            HeroImage = images.FirstOrDefault() ?? current.HeroImage,
            GalleryImages = images,
        };
    }

    public static PreviewMediaSet WithVideo(
        PreviewMediaSet current,
        string path) =>
        current with
        {
            VideoSource = CreateFileUri(path),
            VideoLabel = Path.GetFileNameWithoutExtension(path),
        };

    public static PreviewMediaSet WithAudio(
        PreviewMediaSet current,
        string path) =>
        current with
        {
            AudioSource = CreateFileUri(path),
            AudioLabel = Path.GetFileNameWithoutExtension(path),
        };

    private static IReadOnlyList<ImageSource> LoadImages(
        IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var requestedPaths = paths
            .Take(MaximumPreviewPhotos + 1)
            .ToArray();
        if (requestedPaths.Length > MaximumPreviewPhotos)
        {
            throw new InvalidOperationException(
                $"미리보기 사진은 최대 {MaximumPreviewPhotos}장까지 선택할 수 있습니다.");
        }

        var images = new List<ImageSource>();
        long totalFileBytes = 0;
        long totalDecodedBytes = 0;
        foreach (var path in requestedPaths)
        {
            images.Add(LoadImage(
                path,
                ref totalFileBytes,
                ref totalDecodedBytes));
        }

        return images;
    }

    private static BitmapImage LoadImage(string path)
    {
        long totalFileBytes = 0;
        long totalDecodedBytes = 0;
        return LoadImage(path, ref totalFileBytes, ref totalDecodedBytes);
    }

    private static BitmapImage LoadImage(
        string path,
        ref long totalFileBytes,
        ref long totalDecodedBytes)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "미리보기 이미지 경로가 비어 있습니다.",
                nameof(path));
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException)
        {
            throw new ArgumentException(
                $"미리보기 이미지 경로가 올바르지 않습니다: '{path}'",
                nameof(path),
                exception);
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"미리보기 이미지 파일을 찾을 수 없습니다: '{fullPath}'",
                fullPath);
        }

        try
        {
            using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.SequentialScan);
            var fileBytes = stream.Length;
            if (fileBytes <= 0)
            {
                throw ImageConstraint(fullPath, "파일이 비어 있습니다.");
            }

            if (fileBytes > MaximumImageFileBytes)
            {
                throw ImageConstraint(
                    fullPath,
                    $"파일 크기는 한 장당 {ToMegabytes(MaximumImageFileBytes)}MB 이하여야 합니다.");
            }

            var projectedFileBytes = checked(totalFileBytes + fileBytes);
            if (projectedFileBytes > MaximumTotalImageFileBytes)
            {
                throw ImageConstraint(
                    fullPath,
                    $"선택한 사진의 전체 파일 크기는 {ToMegabytes(MaximumTotalImageFileBytes)}MB 이하여야 합니다.");
            }

            var (pixelWidth, pixelHeight, bitsPerPixel) =
                ReadImageMetadata(stream, fullPath);
            ValidateSourceDimensions(
                fullPath,
                pixelWidth,
                pixelHeight,
                bitsPerPixel);

            var decodeWidth = Math.Min(PreviewDecodeWidth, pixelWidth);
            var decodeHeight = checked((int)Math.Max(
                1,
                ((long)pixelHeight * decodeWidth + pixelWidth - 1)
                / pixelWidth));
            if (decodeHeight > MaximumDecodedHeight)
            {
                throw ImageConstraint(
                    fullPath,
                    $"사진의 가로·세로 비율이 너무 깁니다. 미리보기 높이는 {MaximumDecodedHeight:N0}px 이하여야 합니다.");
            }

            // WPF commonly expands indexed images to 32bpp for rendering.
            // Preserve a higher source depth in the estimate when applicable.
            var bytesPerPixel = Math.Clamp(
                ((long)Math.Max(bitsPerPixel, 32) + 7) / 8,
                4,
                16);
            var decodedBytes = checked(
                (long)decodeWidth * decodeHeight * bytesPerPixel);
            if (decodedBytes > MaximumDecodedImageBytes)
            {
                throw ImageConstraint(
                    fullPath,
                    $"한 장의 미리보기 메모리는 {ToMegabytes(MaximumDecodedImageBytes)}MB 이하여야 합니다.");
            }

            var projectedDecodedBytes = checked(
                totalDecodedBytes + decodedBytes);
            if (projectedDecodedBytes > MaximumTotalDecodedImageBytes)
            {
                throw ImageConstraint(
                    fullPath,
                    $"선택한 사진의 예상 미리보기 메모리는 {ToMegabytes(MaximumTotalDecodedImageBytes)}MB 이하여야 합니다. 사진 수나 해상도를 줄여 주세요.");
            }

            stream.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.DecodePixelWidth = decodeWidth;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            // OnLoad copies all pixels before the using scope ends. The frozen
            // image therefore owns no file handle after this method returns.
            totalFileBytes = projectedFileBytes;
            totalDecodedBytes = projectedDecodedBytes;
            return image;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw ImageConstraint(
                fullPath,
                "파일을 읽을 권한이 없습니다.",
                exception);
        }
        catch (IOException exception)
        {
            throw ImageConstraint(
                fullPath,
                "파일을 읽는 중 오류가 발생했습니다.",
                exception);
        }
        catch (Exception exception) when (
            exception is FileFormatException
            or NotSupportedException
            or ArgumentException
            or System.Runtime.InteropServices.COMException)
        {
            throw ImageConstraint(
                fullPath,
                "지원하지 않거나 손상된 이미지입니다.",
                exception);
        }
    }

    private static (int Width, int Height, int BitsPerPixel) ReadImageMetadata(
        Stream stream,
        string fullPath)
    {
        try
        {
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.DelayCreation
                | BitmapCreateOptions.IgnoreColorProfile,
                BitmapCacheOption.None);
            if (decoder.Frames.Count == 0)
            {
                throw ImageConstraint(
                    fullPath,
                    "이미지 프레임을 찾을 수 없습니다.");
            }

            var frame = decoder.Frames[0];
            return (
                frame.PixelWidth,
                frame.PixelHeight,
                frame.Format.BitsPerPixel);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is FileFormatException
            or NotSupportedException
            or ArgumentException
            or System.Runtime.InteropServices.COMException)
        {
            throw ImageConstraint(
                fullPath,
                "이미지 크기 정보를 읽을 수 없습니다.",
                exception);
        }
    }

    private static void ValidateSourceDimensions(
        string fullPath,
        int pixelWidth,
        int pixelHeight,
        int bitsPerPixel)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            throw ImageConstraint(
                fullPath,
                "이미지 가로·세로 크기가 올바르지 않습니다.");
        }

        if (pixelWidth > MaximumImageDimension
            || pixelHeight > MaximumImageDimension)
        {
            throw ImageConstraint(
                fullPath,
                $"이미지 한 변은 {MaximumImageDimension:N0}px 이하여야 합니다. 현재 크기: {pixelWidth:N0}×{pixelHeight:N0}px.");
        }

        var pixels = checked((long)pixelWidth * pixelHeight);
        if (pixels > MaximumImagePixels)
        {
            throw ImageConstraint(
                fullPath,
                $"이미지는 {MaximumImagePixels / 1_000_000d:0.#}메가픽셀 이하여야 합니다. 현재 크기: {pixels / 1_000_000d:0.#}메가픽셀.");
        }

        var sourceBytesPerPixel = Math.Clamp(
            ((long)Math.Max(bitsPerPixel, 32) + 7) / 8,
            4,
            16);
        var sourceDecodeBytes = checked(pixels * sourceBytesPerPixel);
        if (sourceDecodeBytes > MaximumSourceDecodeBytes)
        {
            throw ImageConstraint(
                fullPath,
                $"원본 이미지의 예상 디코딩 메모리는 {ToMegabytes(MaximumSourceDecodeBytes)}MB 이하여야 합니다.");
        }
    }

    private static InvalidDataException ImageConstraint(
        string path,
        string message,
        Exception? innerException = null) =>
        new(
            $"'{Path.GetFileName(path)}': {message}",
            innerException);

    private static long ToMegabytes(long bytes) =>
        bytes / (1024 * 1024);

    private static Uri? CreateFileUri(string path) =>
        File.Exists(path)
            ? new Uri(Path.GetFullPath(path), UriKind.Absolute)
            : null;
}
