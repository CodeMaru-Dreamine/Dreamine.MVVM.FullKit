using Dreamine.MVVM.Attributes;

namespace SampleSmart.Pages
{
	/// <summary>
	/// \if KO
	/// <para>Main Window Model 기능과 관련 상태를 캡슐화합니다.</para>
	/// \endif
	/// \if EN
	/// <para>Encapsulates main window model functionality and related state.</para>
	/// \endif
	/// </summary>
	public partial class MainWindowModel
	{
		/// <summary>
		/// \if KO
		/// <para>Title 값을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the title value.</para>
		/// \endif
		/// </summary>
		public string Title { get; set; } = "타이틀 없는 드래그 가능한 창";

		/// <summary>
		/// \if KO
		/// <para>Message 값을 가져오거나 설정합니다.</para>
		/// \endif
		/// \if EN
		/// <para>Gets or sets the message value.</para>
		/// \endif
		/// </summary>
		public string Message { get; set; } = "Dreamine 프레임워크로 구성된 MVVM 템플릿입니다.";
	}
}
