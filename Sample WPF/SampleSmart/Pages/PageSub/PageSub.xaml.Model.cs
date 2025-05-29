using Dreamine.MVVM.Attributes;

namespace SampleSmart.Pages.PageSub
{
	public partial class PageSubModel
	{
		[DreamineProperty]
		private string _message = "이것은 Sub View Sample 입니다.";
	}
}
