namespace WeddingThankYou.Models;

/// <summary>
/// \if KO
/// <para>\brief 테넌트별 이미지/영상 사용량 요약입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates tenant media usage summary functionality and related state.</para>
/// \endif
/// </summary>
public sealed class TenantMediaUsageSummary
{
    /// <summary>
    /// \if KO
    /// <para>Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the slug value.</para>
    /// \endif
    /// </summary>
    public string Slug { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Image Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the image count value.</para>
    /// \endif
    /// </summary>
    public int ImageCount { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Optimized Image Bytes 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the optimized image bytes value.</para>
    /// \endif
    /// </summary>
    public long OptimizedImageBytes { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Original Image Bytes 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the original image bytes value.</para>
    /// \endif
    /// </summary>
    public long OriginalImageBytes { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Video Count 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video count value.</para>
    /// \endif
    /// </summary>
    public int VideoCount { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Video Bytes 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the video bytes value.</para>
    /// \endif
    /// </summary>
    public long VideoBytes { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Total Bytes 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the total bytes value.</para>
    /// \endif
    /// </summary>
    public long TotalBytes { get; set; }
    /// <summary>
    /// \if KO
    /// <para>Migration State 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the migration state value.</para>
    /// \endif
    /// </summary>
    public MediaMigrationState MigrationState { get; set; } = MediaMigrationState.Skipped;
    /// <summary>
    /// \if KO
    /// <para>Last Modified 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the last modified value.</para>
    /// \endif
    /// </summary>
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// \if KO
/// <para>\brief 이미지 마이그레이션 처리 상태입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media migration state functionality and related state.</para>
/// \endif
/// </summary>
public enum MediaMigrationState
{
    /// <summary>
    /// \if KO
    /// <para>Pending 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the pending value.</para>
    /// \endif
    /// </summary>
    Pending,
    /// <summary>
    /// \if KO
    /// <para>Processing 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the processing value.</para>
    /// \endif
    /// </summary>
    Processing,
    /// <summary>
    /// \if KO
    /// <para>Completed 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the completed value.</para>
    /// \endif
    /// </summary>
    Completed,
    /// <summary>
    /// \if KO
    /// <para>Failed 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the failed value.</para>
    /// \endif
    /// </summary>
    Failed,
    /// <summary>
    /// \if KO
    /// <para>Skipped 값을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Represents the skipped value.</para>
    /// \endif
    /// </summary>
    Skipped
}

/// <summary>
/// \if KO
/// <para>\brief 테넌트별 이미지 마이그레이션 상태 저장 모델입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media migration tenant status functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaMigrationTenantStatus
{
    /// <summary>
    /// \if KO
    /// <para>Slug 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the slug value.</para>
    /// \endif
    /// </summary>
    public string Slug { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>State 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the state value.</para>
    /// \endif
    /// </summary>
    public MediaMigrationState State { get; set; } = MediaMigrationState.Pending;
    /// <summary>
    /// \if KO
    /// <para>Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Updated At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the updated at value.</para>
    /// \endif
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    /// <summary>
    /// \if KO
    /// <para>Files 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the files value.</para>
    /// \endif
    /// </summary>
    public List<MediaMigrationFileStatus> Files { get; set; } = new();
}

/// <summary>
/// \if KO
/// <para>\brief 개별 이미지 파일의 마이그레이션 상태입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates media migration file status functionality and related state.</para>
/// \endif
/// </summary>
public sealed class MediaMigrationFileStatus
{
    /// <summary>
    /// \if KO
    /// <para>Source File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the source file name value.</para>
    /// \endif
    /// </summary>
    public string SourceFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Target File Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the target file name value.</para>
    /// \endif
    /// </summary>
    public string TargetFileName { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>State 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the state value.</para>
    /// \endif
    /// </summary>
    public MediaMigrationState State { get; set; } = MediaMigrationState.Pending;
    /// <summary>
    /// \if KO
    /// <para>Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the message value.</para>
    /// \endif
    /// </summary>
    public string Message { get; set; } = "";
    /// <summary>
    /// \if KO
    /// <para>Updated At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the updated at value.</para>
    /// \endif
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
