using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>PageSub2 화면 ViewModel입니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub2 view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PageSub2ViewModel : ViewModelBase
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
        private PageSub2Model _model;

        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private PageSub2Event _event;

        /// <summary>
        /// \if KO
        /// <para>화면 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the message value.</para>
        /// \endif
        /// </summary>
        [DreamineProperty]
        private string _message = string.Empty;

        /// <summary>
        /// \if KO
        /// <para>PageSub2ViewModel 인스턴스를 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PageSub2ViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        public PageSub2ViewModel()
        {
            _model = null!;
            _event = null!;

            Message = Model.Message;
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

        /// <summary>
        /// \if KO
        /// <para>Notice 팝업을 엽니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the open notice operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand("Event.OpenNotice")]
        private partial void OpenNotice();
    }
}