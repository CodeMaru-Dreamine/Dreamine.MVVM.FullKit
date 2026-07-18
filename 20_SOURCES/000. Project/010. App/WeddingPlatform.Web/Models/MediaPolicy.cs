namespace WeddingPlatform.Models;

/// <summary>
/// \if KO
/// <para>\brief 이미지/영상 업로드 정책의 기본값과 등급별 기본값을 보관합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media policy settings functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaPolicySettings
{
    /// <summary>
    /// \if KO
    /// <para>시스템 전체 fallback 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the system default value.</para>
    /// \endif
    /// </summary>
    public MediaTierPolicy SystemDefault { get; set; } = MediaTierPolicy.CreateFreeDefault();

    /// <summary>
    /// \if KO
    /// <para>무료 계정에 적용되는 기본 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the free tier value.</para>
    /// \endif
    /// </summary>
    public MediaTierPolicy FreeTier { get; set; } = MediaTierPolicy.CreateFreeDefault();

    /// <summary>
    /// \if KO
    /// <para>Premium 계정에 적용되는 기본 정책입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the premium tier value.</para>
    /// \endif
    /// </summary>
    public MediaTierPolicy PremiumTier { get; set; } = MediaTierPolicy.CreatePremiumDefault();

    /// <summary>
    /// \if KO
    /// <para>\brief 누락된 하위 속성을 안전한 기본값으로 보정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize operation.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 새 설치와 레거시 설정 fallback에 사용할 기본 정책을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the default value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Default 작업에서 생성한 <c>MediaPolicySettings</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaPolicySettings</c> result produced by the create default operation.</para>
    /// \endif
    /// </returns>
    public static MediaPolicySettings CreateDefault()
    {
        var settings = new MediaPolicySettings();
        settings.Normalize();
        return settings;
    }
}

/// <summary>
/// \if KO
/// <para>\brief 계정 등급별 이미지/영상 정책입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media tier policy functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaTierPolicy
{
    /// <summary>
    /// \if KO
    /// <para>이미지 업로드 최대 개수입니다. 0이면 무제한입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image max count value.</para>
    /// \endif
    /// </summary>
    public int ImageMaxCount { get; set; } = 200;

    /// <summary>
    /// \if KO
    /// <para>최적화 이미지 저장 용량 제한(MB)입니다. 0이면 무제한입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image optimized max storage mb value.</para>
    /// \endif
    /// </summary>
    public int ImageOptimizedMaxStorageMb { get; set; } = 1024;

    /// <summary>
    /// \if KO
    /// <para>원본 이미지 저장 용량 제한(MB)입니다. 0이면 원본 보관 없음 또는 무제한 정책에 따릅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image original max storage mb value.</para>
    /// \endif
    /// </summary>
    public int ImageOriginalMaxStorageMb { get; set; } = 0;

    /// <summary>
    /// \if KO
    /// <para>이미지 긴 변 최대 픽셀입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image max long side px value.</para>
    /// \endif
    /// </summary>
    public int ImageMaxLongSidePx { get; set; } = 1920;

    /// <summary>
    /// \if KO
    /// <para>이미지 출력 형식입니다. 기본값은 WebP입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image output format value.</para>
    /// \endif
    /// </summary>
    public string ImageOutputFormat { get; set; } = "WebP";

    /// <summary>
    /// \if KO
    /// <para>이미지 출력 품질입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image quality value.</para>
    /// \endif
    /// </summary>
    public int ImageQuality { get; set; } = 80;

    /// <summary>
    /// \if KO
    /// <para>EXIF 메타데이터 제거 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the strip exif value.</para>
    /// \endif
    /// </summary>
    public bool StripExif { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>업로드 원본 이미지 별도 보관 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the keep original images value.</para>
    /// \endif
    /// </summary>
    public bool KeepOriginalImages { get; set; } = false;

    /// <summary>
    /// \if KO
    /// <para>영상 한 파일 최대 용량(MB)입니다. 0이면 무제한입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max file size mb value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxFileSizeMb { get; set; } = 50;

    /// <summary>
    /// \if KO
    /// <para>영상 업로드 최대 개수입니다. 0이면 무제한입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max count value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxCount { get; set; } = 1;

    /// <summary>
    /// \if KO
    /// <para>영상 저장 용량 제한(MB)입니다. 0이면 무제한입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max storage mb value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxStorageMb { get; set; } = 0;

    /// <summary>
    /// \if KO
    /// <para>업로드 원본 영상 별도 보관 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the keep original videos value.</para>
    /// \endif
    /// </summary>
    public bool KeepOriginalVideos { get; set; } = false;

    /// <summary>
    /// \if KO
    /// <para>\brief 무료 계정 기본 정책을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the free default value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Free Default 작업에서 생성한 <c>MediaTierPolicy</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaTierPolicy</c> result produced by the create free default operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief Premium 계정 기본 정책을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the premium default value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Create Premium Default 작업에서 생성한 <c>MediaTierPolicy</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaTierPolicy</c> result produced by the create premium default operation.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief 잘못되거나 누락된 값을 fallback 정책 기준으로 보정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize operation.</para>
    /// \endif
    /// </summary>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>MediaTierPolicy</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaTierPolicy</c> value used for fallback.</para>
    /// \endif
    /// </param>
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
/// \if KO
/// <para>\brief 개별 계정에만 적용되는 미디어 정책 override입니다. null인 값은 등급 정책을 따릅니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media policy override functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaPolicyOverride
{
    /// <summary>
    /// \if KO
    /// <para>Image Max Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image max count value.</para>
    /// \endif
    /// </summary>
    public int? ImageMaxCount { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Image Optimized Max Storage Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image optimized max storage mb value.</para>
    /// \endif
    /// </summary>
    public int? ImageOptimizedMaxStorageMb { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Image Original Max Storage Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image original max storage mb value.</para>
    /// \endif
    /// </summary>
    public int? ImageOriginalMaxStorageMb { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Image Max Long Side Px 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image max long side px value.</para>
    /// \endif
    /// </summary>
    public int? ImageMaxLongSidePx { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Image Output Format 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image output format value.</para>
    /// \endif
    /// </summary>
    public string? ImageOutputFormat { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Image Quality 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image quality value.</para>
    /// \endif
    /// </summary>
    public int? ImageQuality { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Strip Exif 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the strip exif value.</para>
    /// \endif
    /// </summary>
    public bool? StripExif { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Keep Original Images 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the keep original images value.</para>
    /// \endif
    /// </summary>
    public bool? KeepOriginalImages { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Video Max File Size Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max file size mb value.</para>
    /// \endif
    /// </summary>
    public int? VideoMaxFileSizeMb { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Video Max Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max count value.</para>
    /// \endif
    /// </summary>
    public int? VideoMaxCount { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Video Max Storage Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max storage mb value.</para>
    /// \endif
    /// </summary>
    public int? VideoMaxStorageMb { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Keep Original Videos 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the keep original videos value.</para>
    /// \endif
    /// </summary>
    public bool? KeepOriginalVideos { get; set; }

    /// <summary>
    /// \if KO
    /// <para>설정된 override가 하나도 없는지 여부입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the is empty value.</para>
    /// \endif
    /// </summary>
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
/// \if KO
/// <para>\brief 최종 계산된 계정별 미디어 정책입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates effective media policy functionality and related state.</para>
/// \endif
/// </summary>
public sealed class EffectiveMediaPolicy
{
    /// <summary>
    /// \if KO
    /// <para>Image Max Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image max count value.</para>
    /// \endif
    /// </summary>
    public int ImageMaxCount { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Image Optimized Max Storage Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image optimized max storage mb value.</para>
    /// \endif
    /// </summary>
    public int ImageOptimizedMaxStorageMb { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Image Original Max Storage Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image original max storage mb value.</para>
    /// \endif
    /// </summary>
    public int ImageOriginalMaxStorageMb { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Image Max Long Side Px 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image max long side px value.</para>
    /// \endif
    /// </summary>
    public int ImageMaxLongSidePx { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Image Output Format 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image output format value.</para>
    /// \endif
    /// </summary>
    public string ImageOutputFormat { get; init; } = "WebP";
    /// <summary>
    /// \if KO
    /// <para>Image Quality 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image quality value.</para>
    /// \endif
    /// </summary>
    public int ImageQuality { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Strip Exif 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the strip exif value.</para>
    /// \endif
    /// </summary>
    public bool StripExif { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Keep Original Images 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the keep original images value.</para>
    /// \endif
    /// </summary>
    public bool KeepOriginalImages { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Video Max File Size Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max file size mb value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxFileSizeMb { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Video Max Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max count value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxCount { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Video Max Storage Mb 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video max storage mb value.</para>
    /// \endif
    /// </summary>
    public int VideoMaxStorageMb { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Keep Original Videos 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the keep original videos value.</para>
    /// \endif
    /// </summary>
    public bool KeepOriginalVideos { get; init; }

    /// <summary>
    /// \if KO
    /// <para>영상 한 파일 최대 바이트 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video max file size bytes value.</para>
    /// \endif
    /// </summary>
    public long VideoMaxFileSizeBytes => VideoMaxFileSizeMb <= 0 ? long.MaxValue : VideoMaxFileSizeMb * 1024L * 1024L;

    /// <summary>
    /// \if KO
    /// <para>최적화 이미지 저장 용량 최대 바이트 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the image optimized max storage bytes value.</para>
    /// \endif
    /// </summary>
    public long ImageOptimizedMaxStorageBytes => ImageOptimizedMaxStorageMb <= 0 ? long.MaxValue : ImageOptimizedMaxStorageMb * 1024L * 1024L;

    /// <summary>
    /// \if KO
    /// <para>영상 저장 용량 최대 바이트 수입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the video max storage bytes value.</para>
    /// \endif
    /// </summary>
    public long VideoMaxStorageBytes => VideoMaxStorageMb <= 0 ? long.MaxValue : VideoMaxStorageMb * 1024L * 1024L;

    /// <summary>
    /// \if KO
    /// <para>\brief 등급 정책과 계정 override를 병합합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the from operation.</para>
    /// \endif
    /// </summary>
    /// <param name="tier">
    /// \if KO
    /// <para>tier에 사용할 <c>MediaTierPolicy</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaTierPolicy</c> value used for tier.</para>
    /// \endif
    /// </param>
    /// <param name="accountOverride">
    /// \if KO
    /// <para>account Override에 사용할 <c>MediaPolicyOverride?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>MediaPolicyOverride?</c> value used for account override.</para>
    /// \endif
    /// </param>
    /// <param name="legacyVideoSizeMb">
    /// \if KO
    /// <para>legacy Video Size Mb에 사용할 <c>int?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> value used for legacy video size mb.</para>
    /// \endif
    /// </param>
    /// <param name="legacyVideoCount">
    /// \if KO
    /// <para>legacy Video Count에 사용할 <c>int?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>int?</c> value used for legacy video count.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>From 작업에서 생성한 <c>EffectiveMediaPolicy</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>EffectiveMediaPolicy</c> result produced by the from operation.</para>
    /// \endif
    /// </returns>
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
