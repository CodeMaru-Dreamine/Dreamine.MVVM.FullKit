using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services
{
	/// <summary>
	/// \if KO
	/// <para>\file IGuestbookStorage.cs \brief 슬러그별 방명록 저장/불러오기 추상화 인터페이스. \details 구현체 교체(CSV→DB 등)를 고려한 설계.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates i guestbook storage functionality and related state.</para>
	/// \endif
	/// </summary>
	public interface IGuestbookStorage
	{
		/// <summary>
		/// \if KO
		/// <para>\brief 지정한 슬러그의 모든 항목을 로드합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Loads async data.</para>
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
		/// <para>Load Async 작업에서 생성한 <c>Task&lt;IReadOnlyList&lt;GuestbookEntry&gt;&gt;</c> 결과입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>Task&lt;IReadOnlyList&lt;GuestbookEntry&gt;&gt;</c> result produced by the load async operation.</para>
		/// \endif
		/// </returns>
		Task<IReadOnlyList<GuestbookEntry>> LoadAsync(string slug, CancellationToken ct = default);

		/// <summary>
		/// \if KO
		/// <para>\brief 지정한 슬러그의 전체 목록을 영구 저장(덮어쓰기)합니다.</para>
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
		/// <param name="entries">
		/// \if KO
		/// <para>entries에 사용할 <c>IReadOnlyList&lt;GuestbookEntry&gt;</c> 값입니다.</para>
		/// \endif
		/// \if EN
		/// <para>The <c>IReadOnlyList&lt;GuestbookEntry&gt;</c> value used for entries.</para>
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
		Task SaveAsync(string slug, IReadOnlyList<GuestbookEntry> entries, CancellationToken ct = default);
	}
}
