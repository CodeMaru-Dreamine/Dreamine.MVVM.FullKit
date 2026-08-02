namespace WeddingThankYou.Models;

/// <summary>
/// 원본 히어로 이미지에서 사용할 정규화된 선택 영역(백분율 좌표)입니다.
/// </summary>
public sealed class HeroImageCropRegion
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 100;
    public double Height { get; set; } = 100;
}
