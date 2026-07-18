using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>Image Background Remover 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates image background remover functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ImageBackgroundRemover
{
    /// <summary>
    /// \if KO
    /// <para>Clear Distance 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the clear distance value.</para>
    /// \endif
    /// </summary>
    private const double ClearDistance = 42;
    /// <summary>
    /// \if KO
    /// <para>Soft Distance 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the soft distance value.</para>
    /// \endif
    /// </summary>
    private const double SoftDistance = 86;
    /// <summary>
    /// \if KO
    /// <para>Logo Canvas Width 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logo canvas width value.</para>
    /// \endif
    /// </summary>
    private const int LogoCanvasWidth = 640;
    /// <summary>
    /// \if KO
    /// <para>Logo Canvas Height 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logo canvas height value.</para>
    /// \endif
    /// </summary>
    private const int LogoCanvasHeight = 400;
    /// <summary>
    /// \if KO
    /// <para>Logo Padding 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the logo padding value.</para>
    /// \endif
    /// </summary>
    private const int LogoPadding = 42;

    /// <summary>
    /// \if KO
    /// <para>Data Url 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the data url value.</para>
    /// \endif
    /// </summary>
    /// <param name="imageBytes">
    /// \if KO
    /// <para>image Bytes에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for image bytes.</para>
    /// \endif
    /// </param>
    /// <param name="contentType">
    /// \if KO
    /// <para>content Type에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for content type.</para>
    /// \endif
    /// </param>
    /// <param name="removeBackground">
    /// \if KO
    /// <para>remove Background에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for remove background.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Data Url 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the create data url operation.</para>
    /// \endif
    /// </returns>
    public string CreateDataUrl(byte[] imageBytes, string contentType, bool removeBackground)
    {
        if (contentType.Contains("svg", StringComparison.OrdinalIgnoreCase))
        {
            return $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        }

        try
        {
            var png = NormalizeLogo(imageBytes, removeBackground);
            return $"data:image/png;base64,{Convert.ToBase64String(png)}";
        }
        catch
        {
            return $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Normalize Logo 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize logo operation.</para>
    /// \endif
    /// </summary>
    /// <param name="imageBytes">
    /// \if KO
    /// <para>image Bytes에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for image bytes.</para>
    /// \endif
    /// </param>
    /// <param name="removeBackground">
    /// \if KO
    /// <para>remove Background에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for remove background.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Logo 작업에서 생성한 <c>byte[]</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> result produced by the normalize logo operation.</para>
    /// \endif
    /// </returns>
    private static byte[] NormalizeLogo(byte[] imageBytes, bool removeBackground)
    {
        using var source = new MemoryStream(imageBytes);
        var decoder = BitmapDecoder.Create(source, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var bitmap = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];

        bitmap.CopyPixels(pixels, stride, 0);

        if (removeBackground)
        {
            RemoveBackgroundPixels(pixels, width, height, stride);
        }

        var prepared = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        var bounds = removeBackground
            ? FindVisibleBounds(pixels, width, height, stride)
            : new Int32Rect(0, 0, width, height);
        var cropped = new CroppedBitmap(prepared, bounds);
        var fitted = FitIntoCanvas(cropped);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(fitted));

        using var target = new MemoryStream();
        encoder.Save(target);
        return target.ToArray();
    }

    /// <summary>
    /// \if KO
    /// <para>Background Pixels 항목을 제거합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Removes the background pixels item.</para>
    /// \endif
    /// </summary>
    /// <param name="pixels">
    /// \if KO
    /// <para>pixels에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for pixels.</para>
    /// \endif
    /// </param>
    /// <param name="width">
    /// \if KO
    /// <para>width에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for width.</para>
    /// \endif
    /// </param>
    /// <param name="height">
    /// \if KO
    /// <para>height에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for height.</para>
    /// \endif
    /// </param>
    /// <param name="stride">
    /// \if KO
    /// <para>stride에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for stride.</para>
    /// \endif
    /// </param>
    private static void RemoveBackgroundPixels(byte[] pixels, int width, int height, int stride)
    {
        var background = SampleBackground(pixels, width, height, stride);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = y * stride + x * 4;
                var distance = ColorDistance(
                    pixels[i + 2],
                    pixels[i + 1],
                    pixels[i],
                    background.R,
                    background.G,
                    background.B);

                if (distance <= ClearDistance)
                {
                    pixels[i + 3] = 0;
                }
                else if (distance < SoftDistance)
                {
                    var keep = (distance - ClearDistance) / (SoftDistance - ClearDistance);
                    pixels[i + 3] = (byte)Math.Min(pixels[i + 3], Math.Round(255 * keep));
                }
            }
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Fit Into Canvas 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the fit into canvas operation.</para>
    /// \endif
    /// </summary>
    /// <param name="source">
    /// \if KO
    /// <para>source에 사용할 <c>BitmapSource</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>BitmapSource</c> value used for source.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Fit Into Canvas 작업에서 생성한 <c>BitmapSource</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>BitmapSource</c> result produced by the fit into canvas operation.</para>
    /// \endif
    /// </returns>
    private static BitmapSource FitIntoCanvas(BitmapSource source)
    {
        var availableWidth = LogoCanvasWidth - LogoPadding * 2;
        var availableHeight = LogoCanvasHeight - LogoPadding * 2;
        var scale = Math.Min((double)availableWidth / source.PixelWidth, (double)availableHeight / source.PixelHeight);
        var targetWidth = Math.Max(1, source.PixelWidth * scale);
        var targetHeight = Math.Max(1, source.PixelHeight * scale);
        var left = (LogoCanvasWidth - targetWidth) / 2;
        var top = (LogoCanvasHeight - targetHeight) / 2;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, LogoCanvasWidth, LogoCanvasHeight));
            context.DrawImage(source, new Rect(left, top, targetWidth, targetHeight));
        }

        var target = new RenderTargetBitmap(LogoCanvasWidth, LogoCanvasHeight, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }

    /// <summary>
    /// \if KO
    /// <para>Visible Bounds 항목을 찾습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Finds the visible bounds item.</para>
    /// \endif
    /// </summary>
    /// <param name="pixels">
    /// \if KO
    /// <para>pixels에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for pixels.</para>
    /// \endif
    /// </param>
    /// <param name="width">
    /// \if KO
    /// <para>width에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for width.</para>
    /// \endif
    /// </param>
    /// <param name="height">
    /// \if KO
    /// <para>height에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for height.</para>
    /// \endif
    /// </param>
    /// <param name="stride">
    /// \if KO
    /// <para>stride에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for stride.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Find Visible Bounds 작업에서 생성한 <c>Int32Rect</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Int32Rect</c> result produced by the find visible bounds operation.</para>
    /// \endif
    /// </returns>
    private static Int32Rect FindVisibleBounds(byte[] pixels, int width, int height, int stride)
    {
        var left = width;
        var top = height;
        var right = -1;
        var bottom = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var alpha = pixels[y * stride + x * 4 + 3];
                if (alpha <= 12)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left || bottom < top)
        {
            return new Int32Rect(0, 0, width, height);
        }

        return new Int32Rect(left, top, right - left + 1, bottom - top + 1);
    }

    /// <summary>
    /// \if KO
    /// <para>Sample Background 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the sample background operation.</para>
    /// \endif
    /// </summary>
    /// <param name="pixels">
    /// \if KO
    /// <para>pixels에 사용할 <c>byte[]</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte[]</c> value used for pixels.</para>
    /// \endif
    /// </param>
    /// <param name="width">
    /// \if KO
    /// <para>width에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for width.</para>
    /// \endif
    /// </param>
    /// <param name="height">
    /// \if KO
    /// <para>height에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for height.</para>
    /// \endif
    /// </param>
    /// <param name="stride">
    /// \if KO
    /// <para>stride에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for stride.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Sample Background 작업에서 생성한 <c>Color</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Color</c> result produced by the sample background operation.</para>
    /// \endif
    /// </returns>
    private static Color SampleBackground(byte[] pixels, int width, int height, int stride)
    {
        var points = new[]
        {
            (X: 0, Y: 0),
            (X: width - 1, Y: 0),
            (X: 0, Y: height - 1),
            (X: width - 1, Y: height - 1)
        };

        var r = 0;
        var g = 0;
        var b = 0;

        foreach (var point in points)
        {
            var i = point.Y * stride + point.X * 4;
            b += pixels[i];
            g += pixels[i + 1];
            r += pixels[i + 2];
        }

        return Color.FromRgb((byte)(r / points.Length), (byte)(g / points.Length), (byte)(b / points.Length));
    }

    /// <summary>
    /// \if KO
    /// <para>Color Distance 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the color distance operation.</para>
    /// \endif
    /// </summary>
    /// <param name="r1">
    /// \if KO
    /// <para>r1에 사용할 <c>byte</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte</c> value used for r1.</para>
    /// \endif
    /// </param>
    /// <param name="g1">
    /// \if KO
    /// <para>g1에 사용할 <c>byte</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte</c> value used for g1.</para>
    /// \endif
    /// </param>
    /// <param name="b1">
    /// \if KO
    /// <para>b1에 사용할 <c>byte</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte</c> value used for b1.</para>
    /// \endif
    /// </param>
    /// <param name="r2">
    /// \if KO
    /// <para>r2에 사용할 <c>byte</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte</c> value used for r2.</para>
    /// \endif
    /// </param>
    /// <param name="g2">
    /// \if KO
    /// <para>g2에 사용할 <c>byte</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte</c> value used for g2.</para>
    /// \endif
    /// </param>
    /// <param name="b2">
    /// \if KO
    /// <para>b2에 사용할 <c>byte</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>byte</c> value used for b2.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Color Distance 작업에서 생성한 <c>double</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>double</c> result produced by the color distance operation.</para>
    /// \endif
    /// </returns>
    private static double ColorDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var r = r1 - r2;
        var g = g1 - g2;
        var b = b1 - b2;
        return Math.Sqrt(r * r + g * g + b * b);
    }
}
