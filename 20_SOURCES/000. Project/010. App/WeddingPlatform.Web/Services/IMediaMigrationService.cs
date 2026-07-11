using WeddingPlatform.Models;

namespace WeddingPlatform.Services;

/// <summary>
/// \brief 기존 이미지 최적화 마이그레이션을 백그라운드 배치로 실행합니다.
/// </summary>
public interface IMediaMigrationService
{
    /// <summary>전체 테넌트의 마이그레이션 상태를 조회합니다.</summary>
    Task<IReadOnlyDictionary<string, MediaMigrationTenantStatus>> GetAllAsync(CancellationToken ct = default);

    /// <summary>단일 테넌트의 마이그레이션 상태를 조회합니다.</summary>
    Task<MediaMigrationTenantStatus?> GetTenantStatusAsync(string slug, CancellationToken ct = default);

    /// <summary>단일 테넌트 이미지 최적화 작업을 백그라운드 큐에 넣습니다.</summary>
    Task QueueTenantAsync(string slug, CancellationToken ct = default);

    /// <summary>실패한 항목을 재시도 대상으로 되돌리고 백그라운드 큐에 넣습니다.</summary>
    Task RetryTenantAsync(string slug, CancellationToken ct = default);
}
