using WeddingThankYou.Models;

namespace WeddingThankYou.Services;

/// <summary>
/// \if KO
/// <para>\brief 기존 이미지 최적화 마이그레이션을 백그라운드 배치로 실행합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i media migration service functionality and related state.</para>
/// \endif
/// </summary>
public interface IMediaMigrationService
{
    /// <summary>
    /// \if KO
    /// <para>전체 테넌트의 마이그레이션 상태를 조회합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
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
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyDictionary&lt;string, MediaMigrationTenantStatus&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyDictionary&lt;string, MediaMigrationTenantStatus&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    Task<IReadOnlyDictionary<string, MediaMigrationTenantStatus>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// \if KO
    /// <para>단일 테넌트의 마이그레이션 상태를 조회합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the tenant status async value.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
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
    /// <para>Get Tenant Status Async 작업에서 생성한 <c>Task&lt;MediaMigrationTenantStatus?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;MediaMigrationTenantStatus?&gt;</c> result produced by the get tenant status async operation.</para>
    /// \endif
    /// </returns>
    Task<MediaMigrationTenantStatus?> GetTenantStatusAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// \if KO
    /// <para>단일 테넌트 이미지 최적화 작업을 백그라운드 큐에 넣습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the queue tenant async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
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
    /// <para>Queue Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the queue tenant async operation.</para>
    /// \endif
    /// </returns>
    Task QueueTenantAsync(string slug, CancellationToken ct = default);

    /// <summary>
    /// \if KO
    /// <para>실패한 항목을 재시도 대상으로 되돌리고 백그라운드 큐에 넣습니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the retry tenant async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="slug">
    /// \if KO
    /// <para>slug에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for slug.</para>
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
    /// <para>Retry Tenant Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the retry tenant async operation.</para>
    /// \endif
    /// </returns>
    Task RetryTenantAsync(string slug, CancellationToken ct = default);
}
