using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>\brief System.Drawing 기반 이미지 최적화 서비스입니다. WebP는 기본 Windows/.NET 인코더에 없으므로 호출부에서 지원 가능한 형식으로 fallback합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates system drawing image optimization service functionality and related state.</para>
/// \endif
/// </summary>
public sealed class SystemDrawingImageOptimizationService : IImageOptimizationService
{
    /// <summary>
    /// \if KO
    /// <para>Can Encode 조건을 확인합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Determines whether can encode.</para>
    /// \endif
    /// </summary>
    /// <param name="outputFormat">
    /// \if KO
    /// <para>output Format에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for output format.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Can Encode 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the can encode condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    public bool CanEncode(string outputFormat)
    {
        var format = NormalizeFormat(outputFormat);
        return format is "jpg" or "jpeg" or "png";
    }

    /// <summary>
    /// \if KO
    /// <para>Optimize Async 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the optimize async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="sourcePath">
    /// \if KO
    /// <para>source Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for source path.</para>
    /// \endif
    /// </param>
    /// <param name="destinationPath">
    /// \if KO
    /// <para>destination Path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for destination path.</para>
    /// \endif
    /// </param>
    /// <param name="policy">
    /// \if KO
    /// <para>policy에 사용할 <c>EffectiveMediaPolicy</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>EffectiveMediaPolicy</c> value used for policy.</para>
    /// \endif
    /// </param>
    /// <param name="ct">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Optimize Async 작업에서 생성한 <c>Task&lt;ImageOptimizationResult&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ImageOptimizationResult&gt;</c> result produced by the optimize async operation.</para>
    /// \endif
    /// </returns>
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
        // 카메라(특히 스마트폰) JPEG는 픽셀 방향이 아니라 EXIF Orientation(0x0112) 태그로 회전을 표시한다.
        // System.Drawing은 이 태그를 자동 해석하지 않고, 저장 시 EXIF도 함께 복사하지 않으므로
        // 회전을 픽셀에 반영하지 않으면 최적화 후 이미지가 옆으로 눕거나 뒤집혀 보이는 문제가 발생한다.
        NormalizeOrientation(source);
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

    /// <summary>
    /// \if KO
    /// <para>Resized Bitmap 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the resized bitmap value.</para>
    /// \endif
    /// </summary>
    /// <param name="source">
    /// \if KO
    /// <para>source에 사용할 <c>Image</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Image</c> value used for source.</para>
    /// \endif
    /// </param>
    /// <param name="maxLongSide">
    /// \if KO
    /// <para>max Long Side에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for max long side.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Resized Bitmap 작업에서 생성한 <c>Bitmap</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Bitmap</c> result produced by the create resized bitmap operation.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Jpeg 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves jpeg data.</para>
    /// \endif
    /// </summary>
    /// <param name="image">
    /// \if KO
    /// <para>image에 사용할 <c>Image</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Image</c> value used for image.</para>
    /// \endif
    /// </param>
    /// <param name="path">
    /// \if KO
    /// <para>path에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for path.</para>
    /// \endif
    /// </param>
    /// <param name="quality">
    /// \if KO
    /// <para>quality에 사용할 <c>int</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int</c> value used for quality.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Normalize Format 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize format operation.</para>
    /// \endif
    /// </summary>
    /// <param name="outputFormat">
    /// \if KO
    /// <para>output Format에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for output format.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Format 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize format operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeFormat(string? outputFormat)
    {
        var normalized = outputFormat?.Trim().TrimStart('.').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "" : normalized;
    }

    /// <summary>
    /// \if KO
    /// <para>EXIF Orientation(0x0112) 값을 읽어 원본 이미지 픽셀을 정방향으로 회전합니다. System.Drawing 은 EXIF 를 자동 해석하지 않고, 저장 시 EXIF 도 복사하지 않기 때문에 최적화 이전에 픽셀 자체를 정회전해 두지 않으면 결과 이미지가 옆으로 눕는 문제가 발생합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Reads the EXIF Orientation tag (0x0112) and rotates the source pixels to the natural upright direction. System.Drawing does not honor EXIF orientation nor copy it on save, so the pixels must be pre-rotated to avoid mis-rotated output.</para>
    /// \endif
    /// </summary>
    /// <param name="image">
    /// \if KO
    /// <para>회전을 적용할 원본 <c>Image</c> 값입니다. in-place 로 방향이 변경됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>The source <c>Image</c> to rotate in place.</para>
    /// \endif
    /// </param>
    private static void NormalizeOrientation(Image image)
    {
        const int ExifOrientationTagId = 0x0112;
        try
        {
            if (!image.PropertyIdList.Contains(ExifOrientationTagId))
            {
                return;
            }

            var property = image.GetPropertyItem(ExifOrientationTagId);
            if (property?.Value is null || property.Value.Length == 0)
            {
                return;
            }

            var orientation = property.Value[0];
            var rotation = orientation switch
            {
                2 => RotateFlipType.RotateNoneFlipX,
                3 => RotateFlipType.Rotate180FlipNone,
                4 => RotateFlipType.Rotate180FlipX,
                5 => RotateFlipType.Rotate90FlipX,
                6 => RotateFlipType.Rotate90FlipNone,
                7 => RotateFlipType.Rotate270FlipX,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone,
            };

            if (rotation == RotateFlipType.RotateNoneFlipNone)
            {
                return;
            }

            image.RotateFlip(rotation);
            // 회전을 픽셀에 반영했으므로 EXIF 태그는 정방향(1)로 재기록한다.
            // 저장 파이프라인에서 PropertyItems 를 복사하지는 않지만,
            // 동일 인스턴스를 다시 참조할 가능성에 대비해 값을 정합화한다.
            property.Value[0] = 1;
            image.SetPropertyItem(property);
        }
        catch
        {
            // EXIF 파싱 실패는 방향 정보를 신뢰할 수 없는 원본이라는 뜻이므로
            // 회전 없이 원본 픽셀을 그대로 사용한다.
        }
    }
}
