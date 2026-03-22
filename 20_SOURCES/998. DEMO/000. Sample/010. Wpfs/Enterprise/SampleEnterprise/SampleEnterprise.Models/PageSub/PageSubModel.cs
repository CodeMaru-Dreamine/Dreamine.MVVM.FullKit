using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleEnterprise.Models.PageSub
{
    public partial class PageSubModel
    {
        [DreamineProperty]
        private string _message = "이것은 Sub View Sample 입니다.";
    }
}
