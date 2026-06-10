using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Codemaru.Services;

public sealed class ImageBackgroundRemover
{
    private const double ClearDistance = 42;
    private const double SoftDistance = 86;
    private const int LogoCanvasWidth = 640;
    private const int LogoCanvasHeight = 400;
    private const int LogoPadding = 42;

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

    private static double ColorDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        var r = r1 - r2;
        var g = g1 - g2;
        var b = b1 - b2;
        return Math.Sqrt(r * r + g * g + b * b);
    }
}
