using ShopPlatform.Models;

namespace ShopPlatform.Services;

/// <summary>
/// \if KO
/// <para>I Shop Tenant Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i shop tenant store functionality and related state.</para>
/// \endif
/// </summary>
public interface IShopTenantStore
{
    /// <summary>
    /// \if KO
    /// <para>슬러그로 샵 설정 로드. 없으면 null.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the async value.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ShopConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ShopConfig?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    Task<ShopConfig?> GetAsync(string slug);

    /// <summary>
    /// \if KO
    /// <para>샵 목록 (슈퍼어드민용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;ShopConfig&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;IReadOnlyList&lt;ShopConfig&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    Task<IReadOnlyList<ShopConfig>> GetAllAsync();

    /// <summary>
    /// \if KO
    /// <para>저장 (생성 + 수정 모두).</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>ShopConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ShopConfig</c> value used for config.</para>
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
    Task SaveAsync(ShopConfig config);

    /// <summary>
    /// \if KO
    /// <para>삭제.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete async operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Delete Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the delete async operation.</para>
    /// \endif
    /// </returns>
    Task DeleteAsync(string slug);

    /// <summary>
    /// \if KO
    /// <para>슬러그 사용 가능 여부.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the exists async operation.</para>
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
    /// <returns>
    /// \if KO
    /// <para>Exists Async 작업에서 생성한 <c>Task&lt;bool&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;bool&gt;</c> result produced by the exists async operation.</para>
    /// \endif
    /// </returns>
    Task<bool> ExistsAsync(string slug);
}
