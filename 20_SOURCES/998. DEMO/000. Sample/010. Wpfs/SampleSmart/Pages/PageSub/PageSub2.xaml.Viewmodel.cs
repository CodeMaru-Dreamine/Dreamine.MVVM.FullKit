using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// PageSub2 화면 ViewModel입니다.
    /// </summary>
    public partial class PageSub2ViewModel : ViewModelBase
    {
        [DreamineModel]
        private PageSub2Model _model;

        [DreamineEvent]
        private PageSub2Event _event;

        /// <summary>
        /// 화면 메시지입니다.
        /// </summary>
        [DreamineProperty]
        private string _message = string.Empty;

        /// <summary>
        /// PageSub2ViewModel 인스턴스를 생성합니다.
        /// </summary>
        public PageSub2ViewModel()
        {
            _model = null!;
            _event = null!;

            Message = Model.Message;
        }

        /// <summary>
        /// OK 동작을 실행합니다.
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();

        /// <summary>
        /// Notice 팝업을 엽니다.
        /// </summary>
        [DreamineCommand("Event.OpenNotice")]
        private partial void OpenNotice();
    }
}