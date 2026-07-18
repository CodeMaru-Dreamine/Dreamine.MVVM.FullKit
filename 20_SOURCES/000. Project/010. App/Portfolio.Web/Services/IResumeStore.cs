using PortfolioApp.Models;

namespace PortfolioApp.Services;

/// <summary>
/// \if KO
/// <para>I Resume Store 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i resume store functionality and related state.</para>
/// \endif
/// </summary>
public interface IResumeStore
{
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
    /// <para>Get Async 작업에서 생성한 <c>Task&lt;ResumeInfo&gt;</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task&lt;ResumeInfo&gt;</c> result produced by the get async operation.</para>
    /// \endif
    /// </returns>
    Task<ResumeInfo> GetAsync(string slug);
    /// <summary>
    /// \if KO
    /// <para>Async 데이터를 저장합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Saves async data.</para>
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
    /// <param name="resume">
    /// \if KO
    /// <para>resume에 사용할 <c>ResumeInfo</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>ResumeInfo</c> value used for resume.</para>
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
    Task SaveAsync(string slug, ResumeInfo resume);
}
