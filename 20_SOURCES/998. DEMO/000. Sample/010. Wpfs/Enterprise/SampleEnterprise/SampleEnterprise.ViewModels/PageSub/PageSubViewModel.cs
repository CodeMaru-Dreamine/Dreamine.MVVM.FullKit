using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleEnterprise.Events.PageSub;
using SampleEnterprise.Models.PageSub;

namespace SampleEnterprise.ViewModels.PageSub
{
    public partial class PageSubViewModel : ViewModelBase
    {
        [DreamineModel]
        private PageSubModel _model;
        [DreamineEvent]
        private PageSubEvent _event;

        /// <summary>
        /// 화면에 표시할 메시지입니다.
        /// </summary>
        public string Message => Model.Message;

        [RelayCommand] private void Ok() => Event.Ok();
        [RelayCommand] private void Cancel() => Event.Cancel();

        /// <summary>
        /// PageSubViewModel을 생성합니다.
        /// </summary>
        /// <param name="model">페이지 서브 모델입니다.</param>
        /// <param name="event">페이지 서브 이벤트입니다.</param>
        public PageSubViewModel(PageSubModel model, PageSubEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model; 
            _ = @event;
        }
    }
}
