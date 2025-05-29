using Dreamine.MVVM.Attributes;

namespace DreamineApp.Pages.PageSub
{
	public partial class PageSubModel
	{
		[DreamineProperty]
		private string _message = "이것은 Sub View Sample 입니다.";
	}
}
