using System;

namespace WeddingThankYou.Models
{
	/// <summary>
	/// \if KO
	/// <para>\file GuestbookEntry.cs \brief 방명록 한 건의 데이터 모델. \details CSV 직렬화를 위한 POCO. 이름/연락처/메시지/작성시각을 포함합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates guestbook entry functionality and related state.</para>
	/// \endif
	/// </summary>
	public sealed class GuestbookEntry
	{
		/// <summary>
		/// \if KO
		/// <para>\brief 작성자 이름.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the name value.</para>
		/// \endif
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// \if KO
		/// <para>\brief 작성자 연락처(전화/카톡/이메일 등 자유 입력).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the contact value.</para>
		/// \endif
		/// </summary>
		public string Contact { get; set; } = string.Empty;

		/// <summary>
		/// \if KO
		/// <para>\brief 축하 메시지 본문.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the message value.</para>
		/// \endif
		/// </summary>
		public string Message { get; set; } = string.Empty;

		/// <summary>
		/// \if KO
		/// <para>\brief 작성 시각(로컬 시간).</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the created local value.</para>
		/// \endif
		/// </summary>
		public DateTime CreatedLocal { get; set; } = DateTime.Now;
	}
}
