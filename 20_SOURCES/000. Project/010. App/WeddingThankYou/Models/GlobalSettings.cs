namespace WeddingThankYou.Models;

/// <summary>
/// \file GlobalSettings.cs
/// \brief 전체 사이트(모든 테넌트 공통) 설정. 슈퍼어드민에서 편집.
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>레거시 동영상 업로드 최대 용량(MB). 새 코드는 MediaPolicy.FreeTier.VideoMaxFileSizeMb를 사용합니다.</summary>
    public int MaxVideoSizeMb { get; set; } = 50;

    /// <summary>레거시 계정당 동영상 업로드 최대 개수. 새 코드는 MediaPolicy.FreeTier.VideoMaxCount를 사용합니다.</summary>
    public int MaxVideoCount { get; set; } = 1;

    /// <summary>이미지와 영상에 적용할 시스템/등급별 미디어 정책입니다.</summary>
    public MediaPolicySettings MediaPolicy { get; set; } = MediaPolicySettings.CreateDefault();

    /// <summary>
    /// \brief 레거시 JSON 또는 누락된 정책 값을 새 무료 기본 정책으로 보정합니다.
    /// </summary>
    public void Normalize()
    {
        MediaPolicy ??= MediaPolicySettings.CreateDefault();
        MediaPolicy.Normalize();

        MaxVideoSizeMb = MediaPolicy.FreeTier.VideoMaxFileSizeMb;
        MaxVideoCount = MediaPolicy.FreeTier.VideoMaxCount;
    }
}
