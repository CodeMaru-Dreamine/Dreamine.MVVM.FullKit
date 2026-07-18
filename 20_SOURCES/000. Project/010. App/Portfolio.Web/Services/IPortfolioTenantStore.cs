using PortfolioApp.Models;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>I Portfolio Tenant Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i portfolio tenant store functionality and related state.</para>
/// \endif
/// </summary>
public interface IPortfolioTenantStore
{
    /// <summary>
    /// \if KO
    /// <para>All Async 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the all async value.</para>
    /// \endif
    /// </summary>
    /// <returns>
    /// \if KO
    /// <para>Get All Async 작업에서 생성한 <c>Task&lt;List&lt;PortfolioConfig&gt;&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;List&lt;PortfolioConfig&gt;&gt;</c> result produced by the get all async operation.</para>
    /// \endif
    /// </returns>
    Task<List<PortfolioConfig>> GetAllAsync();
    /// <summary>
    /// \if KO
    /// <para>Async 값을 가져옵니다.</para>
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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;PortfolioConfig?&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;PortfolioConfig?&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    Task<PortfolioConfig?> GetAsync(string slug);
    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
    /// \endif
    /// </summary>
    /// <param name="config">
    /// \if KO
    /// <para>config에 사용할 <c>PortfolioConfig</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>PortfolioConfig</c> value used for config.</para>
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
    Task SaveAsync(PortfolioConfig config);
    /// <summary>
    /// \if KO
    /// <para>Delete Async 작업을 수행합니다.</para>
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
}
