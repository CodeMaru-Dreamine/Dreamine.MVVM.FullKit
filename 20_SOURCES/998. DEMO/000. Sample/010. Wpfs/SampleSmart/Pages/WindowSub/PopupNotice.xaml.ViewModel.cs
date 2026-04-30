using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// PopupNotice ViewModel입니다.
    /// </summary>
    public partial class PopupNoticeViewModel : ViewModelBase
    {
        [DreamineModel]
        private PopupNoticeModel _model;

        [DreamineEvent]
        private PopupNoticeEvent _event;

        /// <summary>
        /// 팝업 메시지입니다.
        /// </summary>
        public string Message => Model.Message;

        /// <summary>
        /// PopupNoticeViewModel 인스턴스를 생성합니다.
        /// </summary>
        public PopupNoticeViewModel()
        {
            _model = null!;
            _event = null!;
        }

        /// <summary>
        /// OK 동작을 실행합니다.
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();
    }
}
