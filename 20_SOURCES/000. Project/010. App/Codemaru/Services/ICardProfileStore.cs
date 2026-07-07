using Codemaru.Models;

namespace Codemaru.Services;

/// <summary>
/// \brief CardHybrid 스냅샷의 영속 저장소 계약입니다.
/// </summary>
/// <remarks>
/// 사용자 계정 자체는 SQLite <c>Users</c> 테이블 (OAuth) 이 관리하고,
/// 이 저장소는 CardHybrid 편집 상태(<see cref="CardHybridSnapshot"/>) 만 담당합니다.
/// Guest 는 파일로 저장하지 않고 서킷 메모리에만 유지합니다.
/// </remarks>
public interface ICardProfileStore
{
    /// <summary>
    /// \brief 지정된 사용자 ID 의 저장된 스냅샷을 로드합니다.
    /// </summary>
    Task<CardHybridSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief 지정된 사용자 ID 로 스냅샷을 저장합니다. Guest 는 무시됩니다.
    /// </summary>
    Task SaveAsync(string userId, CardHybridSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief 랜딩 슬러그 로 매칭되는 스냅샷을 검색합니다.
    /// </summary>
    Task<CardHybridSnapshot?> LoadBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief 지정된 사용자의 스냅샷을 삭제합니다.
    /// </summary>
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
