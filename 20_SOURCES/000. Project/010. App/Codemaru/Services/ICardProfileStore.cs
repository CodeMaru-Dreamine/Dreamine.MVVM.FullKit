using Codemaru.Models;

namespace Codemaru.Services;

/// <summary>
/// \if KO
/// <para>\brief CardHybrid 스냅샷의 영속 저장소 계약입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i card profile store functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>사용자 계정 자체는 SQLite <c>Users</c> 테이블 (OAuth) 이 관리하고, 이 저장소는 CardHybrid 편집 상태(<see cref="CardHybridSnapshot"/>) 만 담당합니다. Guest 는 파일로 저장하지 않고 서킷 메모리에만 유지합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
public interface ICardProfileStore
{
    /// <summary>
    /// \if KO
    /// <para>\brief 지정된 사용자 ID 의 저장된 스냅샷을 로드합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads async data.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load Async 작업에서 생성한 <c>Task&lt;CardHybridSnapshot?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CardHybridSnapshot?&gt;</c> result produced by the load async operation.</para>
    /// \endif
    /// </returns>
    Task<CardHybridSnapshot?> LoadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 지정된 사용자 ID 로 스냅샷을 저장합니다. Guest 는 무시됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="snapshot">
    /// \if KO
    /// <para>snapshot에 사용할 <c>CardHybridSnapshot</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>CardHybridSnapshot</c> value used for snapshot.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the save async operation.</para>
    /// \endif
    /// </returns>
    Task SaveAsync(string userId, CardHybridSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 랜딩 슬러그 로 매칭되는 스냅샷을 검색합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Loads by slug async data.</para>
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
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Load By Slug Async 작업에서 생성한 <c>Task&lt;CardHybridSnapshot?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;CardHybridSnapshot?&gt;</c> result produced by the load by slug async operation.</para>
    /// \endif
    /// </returns>
    Task<CardHybridSnapshot?> LoadBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 지정된 사용자의 스냅샷을 삭제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="userId">
    /// \if KO
    /// <para>user Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for user id.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
