using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleEnterprise.Events.PageSub;
using SampleEnterprise.Models.PageSub;

namespace SampleEnterprise.ViewModels.PageSub
{
    /// <summary>
    /// \if KO
    /// <para>Page Sub View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates page sub view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class PageSubViewModel : ViewModelBase
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
        private PageSubModel _model;
        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private PageSubEvent _event;

        /// <summary>
        /// \if KO
        /// <para>화면에 표시할 메시지입니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the message value.</para>
        /// \endif
        /// </summary>
        public string Message => Model.Message;

        /// <summary>
        /// \if KO
        /// <para>Ok 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Ok() => Event.Ok();
        /// <summary>
        /// \if KO
        /// <para>Cancel 조건을 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Cancel() => Event.Cancel();

        /// <summary>
        /// \if KO
        /// <para>PageSubViewModel을 생성합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="PageSubViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="model">
        /// \if KO
        /// <para>페이지 서브 모델입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PageSubModel</c> value used for model.</para>
        /// \endif
        /// </param>
        /// <param name="event">
        /// \if KO
        /// <para>페이지 서브 이벤트입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>PageSubEvent</c> value used for event.</para>
        /// \endif
        /// </param>
        public PageSubViewModel(PageSubModel model, PageSubEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model; 
            _ = @event;
        }
    }
}
