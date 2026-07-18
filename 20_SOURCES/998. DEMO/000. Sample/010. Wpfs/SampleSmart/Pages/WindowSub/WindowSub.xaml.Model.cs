using Dreamine.MVVM.Attributes;

namespace SampleSmart.Pages.WindowSub
{
	/// <summary>
	/// \if KO
	/// <para>드리마인 페이지의 기본 모델 클래스입니다. ViewModel에서 바인딩 가능한 속성은 Source Generator로 자동 생성됩니다.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates window sub model functionality and related state.</para>
	/// \endif
	/// </summary>
	public partial class WindowSubModel
	{
		/// <summary>
		/// \if KO
		/// <para>페이지 소개 텍스트</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the readme value.</para>
		/// \endif
		/// </summary>
		public string Readme { get; set; } = "드리마인은 사용자 친화적인 플랫폼입니다.";
	}
}
