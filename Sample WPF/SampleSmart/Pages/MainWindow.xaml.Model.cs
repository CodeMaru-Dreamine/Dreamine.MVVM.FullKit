using Dreamine.MVVM.Attributes;

namespace SampleSmart.Pages
{
	public partial class MainWindowModel
	{
		[DreamineProperty]
		private string _title = "타이틀 없는 드래그 가능한 창";

		[DreamineProperty]
		private string _message = "Dreamine 프레임워크로 구성된 MVVM 템플릿입니다.";
	}
}
