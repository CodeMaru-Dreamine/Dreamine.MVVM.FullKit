using System;

namespace WeddingThankYou.Models
{
	/// <summary>
	/// \file GuestbookEntry.cs
	/// \brief 방명록 한 건의 데이터 모델.
	/// \details CSV 직렬화를 위한 POCO. 이름/연락처/메시지/작성시각을 포함합니다.
	/// </summary>
	public sealed class GuestbookEntry
	{
		/// <summary>\brief 작성자 이름.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>\brief 작성자 연락처(전화/카톡/이메일 등 자유 입력).</summary>
		public string Contact { get; set; } = string.Empty;

		/// <summary>\brief 축하 메시지 본문.</summary>
		public string Message { get; set; } = string.Empty;

		/// <summary>\brief 작성 시각(로컬 시간).</summary>
		public DateTime CreatedLocal { get; set; } = DateTime.Now;
	}
}
