using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.WindowSub
{
    /// <summary>
    /// \if KO
    /// <para>PopupNotice ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates popup notice view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PopupNoticeViewModel : ViewModelBase
    {
        /// <summary>
        /// \if KO
        /// <para>model 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the model value.</para>
        /// \endif
        /// </summary>
        [DreamineModel]
        private PopupNoticeModel _model;

        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private PopupNoticeEvent _event;

        /// <summary>
        /// \if KO
        /// <para>팝업 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the message value.</para>
        /// \endif
        /// </summary>
        public string Message => Model.Message;

        /// <summary>
        /// \if KO
        /// <para>PopupNoticeViewModel 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PopupNoticeViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public PopupNoticeViewModel()
        {
            _model = null!;
            _event = null!;
        }

        /// <summary>
        /// \if KO
        /// <para>OK 동작을 실행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();
    }
}
