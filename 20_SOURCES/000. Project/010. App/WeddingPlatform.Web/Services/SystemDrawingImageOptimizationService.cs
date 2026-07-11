using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \brief System.Drawing 기반 이미지 최적화 서비스입니다.
/// WebP는 기본 Windows/.NET 인코더에 없으므로 호출부에서 지원 가능한 형식으로 fallback합니다.
/// </summary>
public sealed class SystemDrawingImageOptimizationService : IImageOptimizationService
{
    /// <inheritdoc />
    public bool CanEncode(string outputFormat)
    {
        var format = NormalizeFormat(outputFormat);
        return format is "jpg" or "jpeg" or "png";
    }

    /// <inheritdoc />
    public Task<ImageOptimizationResult> OptimizeAsync(string sourcePath, string destinationPath, EffectiveMediaPolicy policy, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var format = NormalizeFormat(Path.GetExtension(destinationPath));
        if (string.IsNullOrWhiteSpace(format))
        {
            format = NormalizeFormat(policy.ImageOutputFormat);
        }

        if (!CanEncode(format))
        {
            return Task.FromResult(new ImageOptimizationResult
            {
                Skipped = true,
                Message = $"{format} 인코더를 사용할 수 없어 원본 표시 파일을 유지했습니다."
            });
        }

        using var source = Image.FromFile(sourcePath);
        using var output = CreateResizedBitmap(source, policy.ImageMaxLongSidePx);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        if (format is "jpg" or "jpeg")
        {
            SaveJpeg(output, destinationPath, policy.ImageQuality);
        }
        else
        {
            output.Save(destinationPath, ImageFormat.Png);
        }

        return Task.FromResult(new ImageOptimizationResult
        {
            Succeeded = File.Exists(destinationPath) && new FileInfo(destinationPath).Length > 0,
            FileName = Path.GetFileName(destinationPath),
            Message = "이미지 최적화 완료"
        });
    }

    private static Bitmap CreateResizedBitmap(Image source, int maxLongSide)
    {
        var boundedLongSide = maxLongSide <= 0 ? 1920 : maxLongSide;
        var scale = Math.Min(1d, boundedLongSide / (double)Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        var bitmap = new Bitmap(width, height);
        bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.White);
        graphics.DrawImage(source, 0, 0, width, height);
        return bitmap;
    }

    private static void SaveJpeg(Image image, string path, int quality)
    {
        var codec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(x => x.FormatID == ImageFormat.Jpeg.Guid);
        if (codec is null)
        {
            image.Save(path, ImageFormat.Jpeg);
            return;
        }

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, Math.Clamp(quality, 1, 100));
        image.Save(path, codec, parameters);
    }

    private static string NormalizeFormat(string? outputFormat)
    {
        var normalized = outputFormat?.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "" : normalized;
    }
}
