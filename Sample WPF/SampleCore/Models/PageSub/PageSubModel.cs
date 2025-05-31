using Dreamine.MVVM.Attributes;

namespace SampleCore.Models.PageSub
{
	public partial class PageSubModel
	{
		[DreamineProperty]
		private string _message = "이것은 Sub View Sample 입니다.";
	}
}
