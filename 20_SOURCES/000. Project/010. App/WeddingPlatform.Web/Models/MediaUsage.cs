namespace WeddingPlatform.Models;

/// <summary>
/// \brief 테넌트별 이미지/영상 사용량 요약입니다.
/// </summary>
public sealed class TenantMediaUsageSummary
{
    public string Slug { get; set; } = "";
    public int ImageCount { get; set; }
    public long OptimizedImageBytes { get; set; }
    public long OriginalImageBytes { get; set; }
    public int VideoCount { get; set; }
    public long VideoBytes { get; set; }
    public long TotalBytes { get; set; }
    public MediaMigrationState MigrationState { get; set; } = MediaMigrationState.Skipped;
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// \brief 이미지 마이그레이션 처리 상태입니다.
/// </summary>
public enum MediaMigrationState
{
    Pending,
    Processing,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// \brief 테넌트별 이미지 마이그레이션 상태 저장 모델입니다.
/// </summary>
public sealed class MediaMigrationTenantStatus
{
    public string Slug { get; set; } = "";
    public MediaMigrationState State { get; set; } = MediaMigrationState.Pending;
    public string Message { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<MediaMigrationFileStatus> Files { get; set; } = new();
}

/// <summary>
/// \brief 개별 이미지 파일의 마이그레이션 상태입니다.
/// </summary>
public sealed class MediaMigrationFileStatus
{
    public string SourceFileName { get; set; } = "";
    public string TargetFileName { get; set; } = "";
    public MediaMigrationState State { get; set; } = MediaMigrationState.Pending;
    public string Message { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
