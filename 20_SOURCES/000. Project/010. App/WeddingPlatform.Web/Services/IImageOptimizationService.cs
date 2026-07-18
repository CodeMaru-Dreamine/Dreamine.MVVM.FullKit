using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \if KO
/// <para>\brief 이미지 리사이즈, 출력 형식 변환, EXIF 제거를 담당합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i image optimization service functionality and related state.</para>
/// \endif
/// </summary>
public interface IImageOptimizationService
{
    /// <summary>
    /// \if KO
    /// <para>\brief 지정한 출력 형식을 현재 서버에서 인코딩할 수 있는지 확인합니다.</para>
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
    bool CanEncode(string outputFormat);

    /// <summary>
    /// \if KO
    /// <para>\brief 원본 이미지 파일을 정책에 맞춰 최적화한 뒤 대상 경로에 저장합니다.</para>
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
    Task<ImageOptimizationResult> OptimizeAsync(string sourcePath, string destinationPath, EffectiveMediaPolicy policy, CancellationToken ct = default);
}

/// <summary>
/// \if KO
/// <para>\brief 이미지 최적화 결과입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates image optimization result functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ImageOptimizationResult
{
    /// <summary>
    /// \if KO
    /// <para>Succeeded 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the succeeded value.</para>
    /// \endif
    /// </summary>
    public bool Succeeded { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Skipped 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the skipped value.</para>
    /// \endif
    /// </summary>
    public bool Skipped { get; set; }
    /// <summary>
    /// \if KO
    /// <para>File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the file name value.</para>
    /// \endif
    /// </summary>
    public string FileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; set; } = "";
}
