namespace WeddingThankYou.Models;

/// <summary>
/// \brief 이미지/영상 업로드 정책의 기본값과 등급별 기본값을 보관합니다.
/// </summary>
public sealed class MediaPolicySettings
{
    /// <summary>시스템 전체 fallback 정책입니다.</summary>
    public MediaTierPolicy SystemDefault { get; set; } = MediaTierPolicy.CreateFreeDefault();

    /// <summary>무료 계정에 적용되는 기본 정책입니다.</summary>
    public MediaTierPolicy FreeTier { get; set; } = MediaTierPolicy.CreateFreeDefault();

    /// <summary>Premium 계정에 적용되는 기본 정책입니다.</summary>
    public MediaTierPolicy PremiumTier { get; set; } = MediaTierPolicy.CreatePremiumDefault();

    /// <summary>
    /// \brief 누락된 하위 속성을 안전한 기본값으로 보정합니다.
    /// </summary>
    public void Normalize()
    {
        SystemDefault ??= MediaTierPolicy.CreateFreeDefault();
        FreeTier ??= MediaTierPolicy.CreateFreeDefault();
        PremiumTier ??= MediaTierPolicy.CreatePremiumDefault();

        SystemDefault.Normalize(MediaTierPolicy.CreateFreeDefault());
        FreeTier.Normalize(MediaTierPolicy.CreateFreeDefault());
        PremiumTier.Normalize(MediaTierPolicy.CreatePremiumDefault());
    }

    /// <summary>
    /// \brief 새 설치와 레거시 설정 fallback에 사용할 기본 정책을 생성합니다.
    /// </summary>
    public static MediaPolicySettings CreateDefault()
    {
        var settings = new MediaPolicySettings();
        settings.Normalize();
        return settings;
    }
}

/// <summary>
/// \brief 계정 등급별 이미지/영상 정책입니다.
/// </summary>
public sealed class MediaTierPolicy
{
    /// <summary>이미지 업로드 최대 개수입니다. 0이면 무제한입니다.</summary>
    public int ImageMaxCount { get; set; } = 200;

    /// <summary>최적화 이미지 저장 용량 제한(MB)입니다. 0이면 무제한입니다.</summary>
    public int ImageOptimizedMaxStorageMb { get; set; } = 1024;

    /// <summary>원본 이미지 저장 용량 제한(MB)입니다. 0이면 원본 보관 없음 또는 무제한 정책에 따릅니다.</summary>
    public int ImageOriginalMaxStorageMb { get; set; } = 0;

    /// <summary>이미지 긴 변 최대 픽셀입니다.</summary>
    public int ImageMaxLongSidePx { get; set; } = 1920;

    /// <summary>이미지 출력 형식입니다. 기본값은 WebP입니다.</summary>
    public string ImageOutputFormat { get; set; } = "WebP";

    /// <summary>이미지 출력 품질입니다.</summary>
    public int ImageQuality { get; set; } = 80;

    /// <summary>EXIF 메타데이터 제거 여부입니다.</summary>
    public bool StripExif { get; set; } = true;

    /// <summary>업로드 원본 이미지 별도 보관 여부입니다.</summary>
    public bool KeepOriginalImages { get; set; } = false;

    /// <summary>영상 한 파일 최대 용량(MB)입니다. 0이면 무제한입니다.</summary>
    public int VideoMaxFileSizeMb { get; set; } = 50;

    /// <summary>영상 업로드 최대 개수입니다. 0이면 무제한입니다.</summary>
    public int VideoMaxCount { get; set; } = 1;

    /// <summary>영상 저장 용량 제한(MB)입니다. 0이면 무제한입니다.</summary>
    public int VideoMaxStorageMb { get; set; } = 0;

    /// <summary>업로드 원본 영상 별도 보관 여부입니다.</summary>
    public bool KeepOriginalVideos { get; set; } = false;

    /// <summary>
    /// \brief 무료 계정 기본 정책을 생성합니다.
    /// </summary>
    public static MediaTierPolicy CreateFreeDefault() => new()
    {
        ImageMaxCount = 200,
        ImageOptimizedMaxStorageMb = 1024,
        ImageOriginalMaxStorageMb = 0,
        ImageMaxLongSidePx = 1920,
        ImageOutputFormat = "WebP",
        ImageQuality = 80,
        StripExif = true,
        KeepOriginalImages = false,
        VideoMaxFileSizeMb = 50,
        VideoMaxCount = 1,
        VideoMaxStorageMb = 0,
        KeepOriginalVideos = false
    };

    /// <summary>
    /// \brief Premium 계정 기본 정책을 생성합니다.
    /// </summary>
    public static MediaTierPolicy CreatePremiumDefault() => new()
    {
        ImageMaxCount = 500,
        ImageOptimizedMaxStorageMb = 4096,
        ImageOriginalMaxStorageMb = 4096,
        ImageMaxLongSidePx = 2560,
        ImageOutputFormat = "WebP",
        ImageQuality = 85,
        StripExif = true,
        KeepOriginalImages = true,
        VideoMaxFileSizeMb = 200,
        VideoMaxCount = 6,
        VideoMaxStorageMb = 0,
        KeepOriginalVideos = true
    };

    /// <summary>
    /// \brief 잘못되거나 누락된 값을 fallback 정책 기준으로 보정합니다.
    /// </summary>
    public void Normalize(MediaTierPolicy fallback)
    {
        ImageMaxCount = Math.Max(0, ImageMaxCount);
        ImageOptimizedMaxStorageMb = Math.Max(0, ImageOptimizedMaxStorageMb);
        ImageOriginalMaxStorageMb = Math.Max(0, ImageOriginalMaxStorageMb);
        ImageMaxLongSidePx = ImageMaxLongSidePx <= 0 ? fallback.ImageMaxLongSidePx : ImageMaxLongSidePx;
        ImageOutputFormat = string.IsNullOrWhiteSpace(ImageOutputFormat) ? fallback.ImageOutputFormat : ImageOutputFormat.Trim();
        ImageQuality = Math.Clamp(ImageQuality <= 0 ? fallback.ImageQuality : ImageQuality, 1, 100);
        VideoMaxFileSizeMb = Math.Max(0, VideoMaxFileSizeMb);
        VideoMaxCount = Math.Max(0, VideoMaxCount);
        VideoMaxStorageMb = Math.Max(0, VideoMaxStorageMb);
    }
}

/// <summary>
/// \brief 개별 계정에만 적용되는 미디어 정책 override입니다.
/// null인 값은 등급 정책을 따릅니다.
/// </summary>
public sealed class MediaPolicyOverride
{
    public int? ImageMaxCount { get; set; }
    public int? ImageOptimizedMaxStorageMb { get; set; }
    public int? ImageOriginalMaxStorageMb { get; set; }
    public int? ImageMaxLongSidePx { get; set; }
    public string? ImageOutputFormat { get; set; }
    public int? ImageQuality { get; set; }
    public bool? StripExif { get; set; }
    public bool? KeepOriginalImages { get; set; }
    public int? VideoMaxFileSizeMb { get; set; }
    public int? VideoMaxCount { get; set; }
    public int? VideoMaxStorageMb { get; set; }
    public bool? KeepOriginalVideos { get; set; }

    /// <summary>설정된 override가 하나도 없는지 여부입니다.</summary>
    public bool IsEmpty =>
        ImageMaxCount is null
        && ImageOptimizedMaxStorageMb is null
        && ImageOriginalMaxStorageMb is null
        && ImageMaxLongSidePx is null
        && string.IsNullOrWhiteSpace(ImageOutputFormat)
        && ImageQuality is null
        && StripExif is null
        && KeepOriginalImages is null
        && VideoMaxFileSizeMb is null
        && VideoMaxCount is null
        && VideoMaxStorageMb is null
        && KeepOriginalVideos is null;
}

/// <summary>
/// \brief 최종 계산된 계정별 미디어 정책입니다.
/// </summary>
public sealed class EffectiveMediaPolicy
{
    public int ImageMaxCount { get; init; }
    public int ImageOptimizedMaxStorageMb { get; init; }
    public int ImageOriginalMaxStorageMb { get; init; }
    public int ImageMaxLongSidePx { get; init; }
    public string ImageOutputFormat { get; init; } = "WebP";
    public int ImageQuality { get; init; }
    public bool StripExif { get; init; }
    public bool KeepOriginalImages { get; init; }
    public int VideoMaxFileSizeMb { get; init; }
    public int VideoMaxCount { get; init; }
    public int VideoMaxStorageMb { get; init; }
    public bool KeepOriginalVideos { get; init; }

    /// <summary>영상 한 파일 최대 바이트 수입니다.</summary>
    public long VideoMaxFileSizeBytes => VideoMaxFileSizeMb <= 0 ? long.MaxValue : VideoMaxFileSizeMb * 1024L * 1024L;

    /// <summary>최적화 이미지 저장 용량 최대 바이트 수입니다.</summary>
    public long ImageOptimizedMaxStorageBytes => ImageOptimizedMaxStorageMb <= 0 ? long.MaxValue : ImageOptimizedMaxStorageMb * 1024L * 1024L;

    /// <summary>영상 저장 용량 최대 바이트 수입니다.</summary>
    public long VideoMaxStorageBytes => VideoMaxStorageMb <= 0 ? long.MaxValue : VideoMaxStorageMb * 1024L * 1024L;

    /// <summary>
    /// \brief 등급 정책과 계정 override를 병합합니다.
    /// </summary>
    public static EffectiveMediaPolicy From(MediaTierPolicy tier, MediaPolicyOverride? accountOverride, int? legacyVideoSizeMb, int? legacyVideoCount)
    {
        return new EffectiveMediaPolicy
        {
            ImageMaxCount = accountOverride?.ImageMaxCount ?? tier.ImageMaxCount,
            ImageOptimizedMaxStorageMb = accountOverride?.ImageOptimizedMaxStorageMb ?? tier.ImageOptimizedMaxStorageMb,
            ImageOriginalMaxStorageMb = accountOverride?.ImageOriginalMaxStorageMb ?? tier.ImageOriginalMaxStorageMb,
            ImageMaxLongSidePx = accountOverride?.ImageMaxLongSidePx ?? tier.ImageMaxLongSidePx,
            ImageOutputFormat = string.IsNullOrWhiteSpace(accountOverride?.ImageOutputFormat)
                ? tier.ImageOutputFormat
                : accountOverride!.ImageOutputFormat!.Trim(),
            ImageQuality = Math.Clamp(accountOverride?.ImageQuality ?? tier.ImageQuality, 1, 100),
            StripExif = accountOverride?.StripExif ?? tier.StripExif,
            KeepOriginalImages = accountOverride?.KeepOriginalImages ?? tier.KeepOriginalImages,
            VideoMaxFileSizeMb = accountOverride?.VideoMaxFileSizeMb ?? legacyVideoSizeMb ?? tier.VideoMaxFileSizeMb,
            VideoMaxCount = accountOverride?.VideoMaxCount ?? legacyVideoCount ?? tier.VideoMaxCount,
            VideoMaxStorageMb = accountOverride?.VideoMaxStorageMb ?? tier.VideoMaxStorageMb,
            KeepOriginalVideos = accountOverride?.KeepOriginalVideos ?? tier.KeepOriginalVideos,
        };
    }
}
