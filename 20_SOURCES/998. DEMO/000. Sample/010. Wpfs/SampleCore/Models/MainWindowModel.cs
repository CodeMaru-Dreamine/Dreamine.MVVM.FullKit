using Dreamine.MVVM.Attributes;

namespace SampleCore.Models
{
    public partial class MainWindowModel
    {
        public string Title { get; set; } = "타이틀 없는 드래그 가능한 창";

        public string Message { get; set; } = "Dreamine 프레임워크로 구성된 MVVM 템플릿입니다.";
    }
}
