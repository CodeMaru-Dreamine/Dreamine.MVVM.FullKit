using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WeddingThankYou.Models;

namespace WeddingThankYou.Services
{
	/// <summary>
	/// \file IGuestbookStorage.cs
	/// \brief 슬러그별 방명록 저장/불러오기 추상화 인터페이스.
	/// \details 구현체 교체(CSV→DB 등)를 고려한 설계.
	/// </summary>
	public interface IGuestbookStorage
	{
		/// <summary>
		/// \brief 지정한 슬러그의 모든 항목을 로드합니다.
		/// </summary>
		Task<IReadOnlyList<GuestbookEntry>> LoadAsync(string slug, CancellationToken ct = default);

		/// <summary>
		/// \brief 지정한 슬러그의 전체 목록을 영구 저장(덮어쓰기)합니다.
		/// </summary>
		Task SaveAsync(string slug, IReadOnlyList<GuestbookEntry> entries, CancellationToken ct = default);
	}
}
