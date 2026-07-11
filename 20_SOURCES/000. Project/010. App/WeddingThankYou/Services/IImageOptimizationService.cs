using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \brief 이미지 리사이즈, 출력 형식 변환, EXIF 제거를 담당합니다.
/// </summary>
public interface IImageOptimizationService
{
    /// <summary>
    /// \brief 지정한 출력 형식을 현재 서버에서 인코딩할 수 있는지 확인합니다.
    /// </summary>
    bool CanEncode(string outputFormat);

    /// <summary>
    /// \brief 원본 이미지 파일을 정책에 맞춰 최적화한 뒤 대상 경로에 저장합니다.
    /// </summary>
    Task<ImageOptimizationResult> OptimizeAsync(string sourcePath, string destinationPath, EffectiveMediaPolicy policy, CancellationToken ct = default);
}

/// <summary>
/// \brief 이미지 최적화 결과입니다.
/// </summary>
public sealed class ImageOptimizationResult
{
    public bool Succeeded { get; set; }
    public bool Skipped { get; set; }
    public string FileName { get; set; } = "";
    public string Message { get; set; } = "";
}
