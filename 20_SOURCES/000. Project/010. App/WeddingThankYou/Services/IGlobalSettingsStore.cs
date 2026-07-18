using System.Threading;
using System.Threading.Tasks;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services
{
	/// <summary>
	/// \if KO
	/// <para>\file IGlobalSettingsStore.cs \brief 전체 사이트 공통 설정(GlobalSettings) 저장소 추상화.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates i global settings store functionality and related state.</para>
	/// \endif
	/// </summary>
	public interface IGlobalSettingsStore
	{
		/// <summary>
		/// \if KO
		/// <para>Async 값을 가져옵니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets the async value.</para>
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
		/// <para>Get Async 작업에서 생성한 <c>Task&lt;GlobalSettings&gt;</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task&lt;GlobalSettings&gt;</c> result produced by the get async operation.</para>
		/// \endif
		/// </returns>
		Task<GlobalSettings> GetAsync(CancellationToken ct = default);
		/// <summary>
		/// \if KO
		/// <para>Async 데이터를 저장합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Saves async data.</para>
		/// \endif
		/// </summary>
		/// <param name="settings">
		/// \if KO
		/// <para>settings에 사용할 <c>GlobalSettings</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>GlobalSettings</c> value used for settings.</para>
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
		/// <para>Save Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task</c> result produced by the save async operation.</para>
		/// \endif
		/// </returns>
		Task SaveAsync(GlobalSettings settings, CancellationToken ct = default);
	}
}
